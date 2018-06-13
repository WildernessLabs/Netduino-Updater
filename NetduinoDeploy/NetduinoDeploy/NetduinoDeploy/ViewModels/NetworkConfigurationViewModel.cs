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
                OnPropertyChanged(nameof(SelectedDevice));
            }
        }
        string _selectedDevice;

        public string MacAddress
        {
            get;
            set;
        }

        public string Status { get; private set; }

        public bool CanSave
        {
            get => _canSave;
            set
            {
                _canSave = value;
                OnPropertyChanged(nameof(CanSave));
            }
        }
        bool _canSave = false;

        public bool CanEditMacAddress
        {
            get => CanSave && _canEditMacAddress;
            set
            {
                _canEditMacAddress = value;
                OnPropertyChanged(nameof(CanEditMacAddress));
            }
        }
        bool _canEditMacAddress = false;

        public bool IsOneDeviceConnected => (DfuContext.Current?.GetDevices().Count == 1);

        public Command CommitSettingsSelected { get; set; }

        Regex _macAddressRegex = new Regex("^([0-9A-Fa-f]{2}[:]){5}([0-9A-Fa-f]{2})$");

        const string ADD_DEVICE_TYPE = "[Select Device Type]";

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
            set
            {
                networkConfig.EnableDHCP = value;
                OnPropertyChanged(nameof(IsNetworkManualConfig));
            }
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
            get
            {
                if (networkConfig != null & networkConfig.NetworkKey.Length > 50)
                    return networkConfig.NetworkKey.Substring(0, 30) + "...";
                return networkConfig?.NetworkKey ?? string.Empty;
            }
            set
            {
                if (networkConfig != null) networkConfig.NetworkKey = value;
            }
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

        public bool IsNetworkManualConfig
        {
            get => IsNetworkCapable && !IsDHCPEnabled;
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

                var optSettings = new OtpManager().GetOtpSettings();

                if(networkConfig.NetworkMacAddress != null)
                    MacAddress = BitConverter.ToString(optSettings.MacAddress).Replace('-', ':');

                CanSave = settings.FreeSlots > 0;

                Status = string.Format("Device settings can be saved {0} more time{1}", settings.FreeSlots, settings.FreeSlots > 1 ? "s" : "");
            }
            else
            {
                if (deviceCount == 0)
                {
                    App.SendConsoleMessage("Please connect a Netduino device in bootloader mode and restart the application");
                }
                else if (deviceCount > 1)
                {
                    App.SendConsoleMessage("Please connect only one Netduino device in bootloader mode and restart the application");
                }

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

        void UpdateNetworkConfigRadio (MFWirelessConfiguration.RadioTypes type, bool isEnabled)
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
            var address = MacAddress;
            if (ValidateMacAddress(ref address) == false)
            {
                App.SendConsoleMessage("Invalid MAC Address, unable to update");
                return;
            }
            else
            {
                MacAddress = address;
            }

            var settings = new OtpSettings
            {
                ProductID = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == SelectedDevice).ProductID),
                MacAddress = MacAddress.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray()
            };

            var manager = new OtpManager();

            manager.StatusUpdated += Manager_StatusUpdated;
            
            if (manager.SaveOtpSettings(settings))
                App.SendConsoleMessage("Settings updated succesfully");
            else
                App.SendConsoleMessage("Unable to update device settings");

            manager.StatusUpdated += Manager_StatusUpdated;

        }

        private void Manager_StatusUpdated(object sender, string e)
        {
            App.SendConsoleMessage(e);
        }

        bool ValidateMacAddress(ref string macAddress)
        {
            if (macAddress.Length == 12)
            {
                for (int i = 10; i > 1; i -= 2)
                    macAddress = macAddress.Insert(i, ":");
            }

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
            }
            else
            {
                SelectedDevice = ADD_DEVICE_TYPE;
            }
        }

        void ValidateDevice(string deviceName)
        {
            Device device = null;

            if (deviceName != ADD_DEVICE_TYPE && Globals.DeviceTypes.Count > 0)
            {
                device = Globals.DeviceTypes.Single(x => x.Name == deviceName);

                CanSave = true;
                CanEditMacAddress = device.HasMacAddress;
            }
            else
            {
                CanSave = false;
                CanEditMacAddress = false;
            }
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