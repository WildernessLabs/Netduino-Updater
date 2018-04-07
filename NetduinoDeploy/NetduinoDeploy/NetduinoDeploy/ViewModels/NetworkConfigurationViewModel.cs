using NetduinoDeploy.Managers;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace NetduinoDeploy
{
    public class NetworkConfigurationViewModel : ViewModelBase
    {
        #region properties

        //OTP
        public List<string> Devices { get; private set; } = new List<string>();
        public int SelectedDeviceIndex { get; private set; }

        public string SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                ValidateDevice(_selectedDevice);
                OnPropertyChanged("SelectedDevice");
            }
        }
        string _selectedDevice;

        public string MacAddress { get; private set; }

        public string Status { get; private set; }

        public bool CanSave
        {
            get => _canSave;
            set
            {
                _canSave = value;
                OnPropertyChanged("CanSave");
            }
        }
        bool _canSave = false;


        public Command CommitSettingsSelected { get; set; }

        Regex _macAddressRegex = new Regex("^ ([0 - 9A - Fa - f]{2}[:]){5}([0 - 9A - Fa - f]{2})$");

        string ADD_DEVICE_TYPE = "[Select Device Type]";


        //network config
        public List<string> NetworkKeyTypes { get; set; } = new List<string> { "64-bit", "128-bit", "256-bit", "512-bit", "1024-bit", "2048-bit" };
        public List<string> EncryptionTypes { get; set; } = new List<string> { "None", "WEP", "WPA", "WPAPSK", "Certificate" };
        public List<string> AuthenticationTypes { get; set; } = new List<string> { "None", "EAP", "PEAP", "WCN", "Open", "Shared" };

        public string StaticIPAddress
        {
            get => networkConfig?.StaticIPAddress.ToString();
            set => networkConfig.StaticIPAddress = networkConfig.ParseAddress(value);
        }

        public string SubnetMask
        {
            get => networkConfig?.SubnetMask.ToString();
            set => networkConfig.SubnetMask = networkConfig.ParseAddress(value);
        }

        public string PrimaryDNS
        {
            get => networkConfig?.PrimaryDNS.ToString();
            set => networkConfig.PrimaryDNS = networkConfig.ParseAddress(value);
        }

        public string SecondaryDNS
        {
            get => networkConfig?.SecondaryDNS.ToString();
            set => networkConfig.SecondaryDNS = networkConfig.ParseAddress(value);
        }

        public string DefaultGateway
        {
            get => networkConfig?.DefaultGateway.ToString();
            set => networkConfig.DefaultGateway = networkConfig.ParseAddress(value);
        }

        public bool IsDHCPEnabled
        {
            get => networkConfig == null?false: networkConfig.EnableDHCP;
            set => networkConfig.EnableDHCP = value;
        }

        public bool Is80211aEnabled
        {
            get => IsRadioEnabled(MFWirelessConfiguration.RadioTypes.a);
            set
            {
                UpdateNetworkConfigRadio(MFWirelessConfiguration.RadioTypes.a, value);
                OnPropertyChanged(nameof(Is80211aEnabled));
            }
        }

        public bool Is80211bEnabled
        {
            get => IsRadioEnabled(MFWirelessConfiguration.RadioTypes.b);
            set
            {
                UpdateNetworkConfigRadio(MFWirelessConfiguration.RadioTypes.b, value);
                OnPropertyChanged(nameof(Is80211bEnabled));
            }
        }

        public bool Is80211gEnabled
        {
            get => IsRadioEnabled(MFWirelessConfiguration.RadioTypes.g);
            set
            {
                UpdateNetworkConfigRadio(MFWirelessConfiguration.RadioTypes.g, value);
                OnPropertyChanged(nameof(Is80211gEnabled));
            }
        }

        public bool Is80211nEnabled
        {
            get => IsRadioEnabled(MFWirelessConfiguration.RadioTypes.n);
            set
            {
                UpdateNetworkConfigRadio(MFWirelessConfiguration.RadioTypes.n, value);
                OnPropertyChanged(nameof(Is80211nEnabled));
            }
        }

        public string SSID
        {
            get => networkConfig?.SSID;
            set { if (networkConfig != null) networkConfig.SSID = value; }
        }

        public string SelectedAuthenticationType
        {
            get => AuthenticationTypes[networkConfig.Authentication];
            set => networkConfig.Authentication = AuthenticationTypes.IndexOf(value);
        }

        public string SelectedEncryptionType
        {
            get => EncryptionTypes[networkConfig.Encryption];
            set => networkConfig.Encryption = EncryptionTypes.IndexOf(value);
        }

        public string SelectedNetworkKeyType
        {
            get => NetworkKeyTypes[GetNetworkTypeIndex(networkConfig.NetworkKeyLength)];
            set => networkConfig.NetworkKeyLength = NetworkKeyTypes.IndexOf(value);
        }

        public string PassPhrase
        {
            get => networkConfig?.Passphrase;
            set { if (networkConfig != null) networkConfig.Passphrase = value; }
        }

        public string NetworkKey
        {
            get => networkConfig?.NetworkKey;
            set { if (networkConfig != null) networkConfig.NetworkKey = value; }
        }

        public string ReKeyInterval
        { 
            get => networkConfig?.ReKeyInternal;
            set { if (networkConfig != null) networkConfig.ReKeyInternal = value; }
        }

        public bool UseEncryptConfig
        {
            get => (networkConfig == null) ? false : networkConfig.EncryptConfig;
            set { if (networkConfig != null) networkConfig.EncryptConfig = value; }
        }

        public bool IsWireless
        {
            get => (networkConfig == null)?false:networkConfig.IsWireless;
            set { if (networkConfig != null) networkConfig.IsWireless = value; }
        }

        public bool IsNetworkCapable => GetIsNetworkCapable();
              
        public Command UpdateSelected { get; set; }

        
        NetworkManager networkManager;

        NetworkConfig networkConfig;

         static Lazy<NetworkConfigurationViewModel> Instance = new Lazy<NetworkConfigurationViewModel>();

        public static NetworkConfigurationViewModel GetInstance ()
        {
            return Instance.Value;
        }

        #endregion

        public NetworkConfigurationViewModel()
        {
            Initialize();

            UpdateSelected = new Command(SaveNetworkSettings);
            CommitSettingsSelected = new Command(OnCommitSettings);

            RaiseAllPropertiesChanged();
        }

        void Initialize()
        {
            var deviceCount = DfuContext.Current?.GetDevices().Count;

            if (deviceCount == 1)
            {
                var settings = new OtpManager().GetOtpSettings();

                LoadDeviceList(Globals.ConnectedDeviceId = settings.ProductID);

                networkManager = new NetworkManager();

                if (settings.ProductID > 0)
                    LoadNetworkSettings();
                else
                    networkConfig = new NetworkConfig();

                if(networkConfig.NetworkMacAddress != null)
                    MacAddress = BitConverter.ToString(networkConfig.NetworkMacAddress).Replace('-', ':');

                Status = string.Format("Device settings can be saved {0} more time{1}", settings.FreeSlots, settings.FreeSlots > 1 ? "s" : "");

                //lazy but probably about right
                RaiseAllPropertiesChanged();
            }
            else
            {
                App.SendConsoleMessage("No conected devices found");

                networkConfig = new NetworkConfig() { IsWireless = false, NetworkMacAddress = null };

                LoadDeviceList();
            }
        }

        void LoadNetworkSettings()
        {
            ReadNetworkSettingsFromDevice();

            if (networkConfig.NetworkMacAddress == null ||
                networkConfig.NetworkMacAddress.Length == 0)
            {
                var otpManager = new OtpManager();
                var settings = otpManager.GetOtpSettings();

                if (settings.MacAddress != null && settings.MacAddress.Length > 0)
                    networkConfig.NetworkMacAddress = settings.MacAddress;
            }
        }

        void ReadNetworkSettingsFromDevice()
        {
            var mfNetConfig = new MFNetworkConfiguration();

            mfNetConfig.Load(networkManager);

            networkConfig = new NetworkConfig()
            {
                EnableDHCP = mfNetConfig.EnableDhcp,
                StaticIPAddress = mfNetConfig.IpAddress,
                SubnetMask = mfNetConfig.SubNetMask,
                DefaultGateway = mfNetConfig.Gateway,
                NetworkMacAddress = mfNetConfig.MacAddress,
                PrimaryDNS = mfNetConfig.PrimaryDns,
                SecondaryDNS = mfNetConfig.SecondaryDns
            };

            if (mfNetConfig.ConfigurationType == MFNetworkConfiguration.NetworkConfigType.Wireless)
            {
                var mfWifiConfig = new MFWirelessConfiguration();

                mfWifiConfig.Load(networkManager);

                networkConfig.IsWireless = true;
                networkConfig.Authentication = mfWifiConfig.Authentication;
                networkConfig.Encryption = mfWifiConfig.Encryption;
                networkConfig.Radio = mfWifiConfig.Radio;
                networkConfig.Passphrase = mfWifiConfig.PassPhrase;
                networkConfig.EncryptConfig = mfWifiConfig.UseEncryption;
                networkConfig.NetworkKey = mfWifiConfig.NetworkKey;
                networkConfig.NetworkKeyLength = mfWifiConfig.NetworkKeyLength;
                networkConfig.ReKeyInternal = mfWifiConfig.ReKeyInternal;
                networkConfig.SSID = mfWifiConfig.SSID;
            }
        }

        void SaveNetworkSettings()
        {
            var mfNetConfig = new MFNetworkConfiguration();
            mfNetConfig.Load(networkManager);

            mfNetConfig.IpAddress   = networkConfig.StaticIPAddress;
            mfNetConfig.SubNetMask  = networkConfig.SubnetMask;
            mfNetConfig.PrimaryDns  = networkConfig.PrimaryDNS;
            mfNetConfig.SecondaryDns = networkConfig.SecondaryDNS;
            mfNetConfig.Gateway     = networkConfig.DefaultGateway;
            mfNetConfig.MacAddress  = networkConfig.NetworkMacAddress;
            mfNetConfig.EnableDhcp  = networkConfig.EnableDHCP;
            mfNetConfig.ConfigurationType = networkConfig.IsWireless ? MFNetworkConfiguration.NetworkConfigType.Wireless : MFNetworkConfiguration.NetworkConfigType.Generic;

            if (networkConfig.IsWireless)
            {
                var mfWifiConfig = new MFWirelessConfiguration();
                mfWifiConfig.Load(networkManager);

                mfWifiConfig.Authentication = networkConfig.Authentication;
                mfWifiConfig.Encryption     = networkConfig.Encryption;
                mfWifiConfig.Radio          = networkConfig.Radio;
                mfWifiConfig.PassPhrase     = networkConfig.Passphrase;
                mfWifiConfig.UseEncryption  = networkConfig.EncryptConfig;
                mfWifiConfig.NetworkKeyLength = (int)Math.Pow(2, networkConfig.NetworkKeyLength) * 64;
                mfWifiConfig.NetworkKey     = networkConfig.NetworkKey;
                mfWifiConfig.ReKeyLength    = networkConfig.ReKeyInternal.Length / 2;
                mfWifiConfig.ReKeyInternal  = networkConfig.ReKeyInternal;
                mfWifiConfig.SSID           = networkConfig.SSID;

                mfWifiConfig.Save(networkManager);
            }
            mfNetConfig.Save(networkManager);

            App.SendConsoleMessage("Network configuration saved");
        }

        bool IsRadioEnabled(MFWirelessConfiguration.RadioTypes radioType)
        {
            if (networkConfig == null)
                return false;

            return (networkConfig.Radio & (int)radioType) != 0 ? true : false;
        }

        void UpdateNetworkConfigRadio (MFWirelessConfiguration.RadioTypes type, bool isEnabled) //TODO!
        {
            if (networkConfig == null)
                return;

            if (isEnabled == IsRadioEnabled(type))
                return;

            networkConfig.Radio += (isEnabled ? (int)type : -(int)type);
        }

        int GetNetworkTypeIndex(int keyLength)
        {
            var index = NetworkKeyTypes.IndexOf(networkConfig.NetworkKeyLength + "-bit");
            if(index < 0 || index >= NetworkKeyTypes.Count)
                index = 0;

            return index;
        }

        void OnCommitSettings()
        {
            var settings = new OtpSettings();

            settings.ProductID = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == SelectedDevice).ProductID);
            settings.MacAddress = MacAddress.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();

            var manager = new OtpManager();
            manager.SaveOtpSettings(settings);
        }

        bool ValidateMacAddress(string macAddress)
        {
            var result = _macAddressRegex.Match(macAddress);

            return result.Success;
        }

        void LoadDeviceList(int productId = 0)
        {
            Devices.Clear();
            Devices.Add(ADD_DEVICE_TYPE);

            foreach (var device in Globals.DeviceTypes)
            {
                Devices.Add(device.Name);
            }

            if (productId > 0)
            {
                var deviceType = Globals.GetDeviceFromId(productId);

                SelectedDevice = deviceType.Name;
                App.SendConsoleMessage($"Device connected: {deviceType.Name}");

                CanSave = deviceType.HasMacAddress;
            }
        }

        void ValidateDevice(string deviceName)
        {
            Device device = null;

            if (deviceName != ADD_DEVICE_TYPE && Globals.DeviceTypes.Count > 0)
            {
                device = Globals.DeviceTypes.Single(x => x.Name == deviceName);
            }

            CanSave = (device != null) ? device.HasMacAddress : false;
        }

        bool GetIsNetworkCapable()
        {
            if (networkConfig == null || 
                networkConfig.NetworkMacAddress == null)
                return false;

            bool isCapable = false;

            for (int i = 0; i < networkConfig.NetworkMacAddress.Length; i++)
            {   //check for default (all 255)
                if (networkConfig.NetworkMacAddress[i] != 255)
                {
                    isCapable = true;
                    break;
                }
            }
            return isCapable;
        }

    }
}