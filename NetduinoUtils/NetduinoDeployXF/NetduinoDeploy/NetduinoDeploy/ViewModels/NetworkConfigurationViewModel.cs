using NetduinoDeploy.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;

namespace NetduinoDeploy
{
    public class NetworkConfigurationViewModel : ViewModelBase
    {
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

        public bool IsDHCPEnabled => networkConfig.EnableDHCP;

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

        public string SSID => networkConfig?.SSID;
        

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

        public string PassPhrase => networkConfig?.Passphrase;
        public string NetworkKey => networkConfig?.NetworkKey;

        public string ReKeyInterval => networkConfig?.ReKeyInternal;
        public bool UseEncryptConfig => networkConfig.EncryptConfig;

        public bool IsWireless => networkConfig.IsWireless;

        public Command UpdateSelected { get; set; }

        
        NetworkManager networkManager;

        NetworkConfig networkConfig;

        Device selectedDeviceType = Globals.DeviceTypes[0];

        public NetworkConfigurationViewModel()
        {
            networkManager = new NetworkManager();

            if (Globals.ConnectedDeviceId > 0)
                LoadNetworkSettings();
            else
                networkConfig = new NetworkConfig();

            Initialize();

            UpdateSelected = new Command(OnUpdate);

            RaiseAllPropertiesChanged();
        }

        void OnUpdate ()
        {
            SaveNetworkSettings(true);
        }

        void LoadNetworkSettings(bool skipReadFromDevice = false)
        {
            if (!skipReadFromDevice)
                ReadNetworkSettingsFromDevice();

            if (networkConfig.NetworkMacAddress == null ||
                networkConfig.NetworkMacAddress.Length == 0)
            {
                var otpManager = new OtpManager();
                var settings = otpManager.GetOtpSettings();

                networkConfig = new NetworkConfig
                {
                    NetworkMacAddress = settings.MacAddress,
                    IsWireless = selectedDeviceType.IsWirelessCapable
                };

                // SaveNetworkSettings();
                ReadNetworkSettingsFromDevice();
            }


            var count = DfuContext.Current.GetDevices().Count;

            if (count == 0)
            {
                networkConfig = new NetworkConfig();
                return;
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

        void SaveNetworkSettings(bool logToConsole = true)
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

            if (logToConsole)
            {
                Debug.WriteLine("Network settings saved.");
            }
        }

        void Initialize()
        {

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
    }
}