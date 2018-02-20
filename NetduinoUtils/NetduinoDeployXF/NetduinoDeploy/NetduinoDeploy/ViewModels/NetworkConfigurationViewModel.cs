using NetduinoDeploy.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NetduinoFirmware.Managers;

namespace NetduinoDeploy
{
    public class NetworkConfigurationViewModel : ViewModelBase
    {
        public List<string> NetworkKeyTypes { get; private set; } = new List<string>();
        public List<string> EncryptionTypes { get; private set; } = new List<string>();
        public List<string> AuthenticationTypes { get; private set; } = new List<string>();

        public string StaticIPAddress
        {
            get => networkConfig.StaticIPAddress.ToString();
            set => networkConfig.StaticIPAddress = networkConfig.ParseAddress(value);
        }

        public string SubnetMask
        {
            get => networkConfig.SubnetMask.ToString();
            set => networkConfig.SubnetMask = networkConfig.ParseAddress(value);
        }

        public string PrimaryDNS
        {
            get => networkConfig.PrimaryDNS.ToString();
            set => networkConfig.PrimaryDNS = networkConfig.ParseAddress(value);
        }

        public string SecondaryDNS
        {
            get => networkConfig.SecondaryDNS.ToString();
            set => networkConfig.SecondaryDNS = networkConfig.ParseAddress(value);
        }

        public string DefaultGateway
        {
            get => networkConfig.DefaultGateway.ToString();
            set => networkConfig.DefaultGateway = networkConfig.ParseAddress(value);
        }

        public bool IsDHCPEnabled
        {
            get => networkConfig.EnableDHCP;
            set => networkConfig.EnableDHCP = value;
        }

      /*  public bool Is80211aEnabled
        {
            get => networkConfig.
            set => networkConfig.EnableDHCP = value;
        }*/




        NetworkManager networkManager;


        NetworkConfig networkConfig;

        Device selectedDeviceType = Globals.DeviceTypes[0]; //

        public NetworkConfigurationViewModel ()
        {
            networkManager = new NetworkManager();

            Initialize();
            
            LoadNetworkSettings();

            RaiseAllPropertiesChanged();
        }

        void LoadNetworkSettings (bool skipReadFromDevice = false)
        {
            if (!skipReadFromDevice)
                ReadNetworkSettingsFromDevice();

            if(networkConfig.NetworkMacAddress == null || 
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
                MFWirelessConfiguration mfWifiConfig = new MFWirelessConfiguration();

                mfWifiConfig.Load(networkManager);

                networkConfig.IsWireless = true;
                networkConfig.Authentication    = mfWifiConfig.Authentication;
                networkConfig.Encryption        = mfWifiConfig.Encryption;
                networkConfig.Radio             = mfWifiConfig.Radio;
                networkConfig.Passphrase        = mfWifiConfig.PassPhrase;
                networkConfig.EncryptConfig     = mfWifiConfig.UseEncryption;
                networkConfig.NetworkKey        = mfWifiConfig.NetworkKey;
                networkConfig.NetworkKeyLength  = mfWifiConfig.NetworkKeyLength;
                networkConfig.ReKeyInternal     = mfWifiConfig.ReKeyInternal;
                networkConfig.SSID              = mfWifiConfig.SSID;
            }
        }

        void SaveNetworkSettings(bool logToConsole = true)
        {
            MFNetworkConfiguration mfNetConfig = new MFNetworkConfiguration();
            mfNetConfig.Load(networkManager);

            mfNetConfig.IpAddress = networkConfig.StaticIPAddress;
            mfNetConfig.SubNetMask = networkConfig.SubnetMask;
            mfNetConfig.PrimaryDns = networkConfig.PrimaryDNS;
            mfNetConfig.SecondaryDns = networkConfig.SecondaryDNS;
            mfNetConfig.Gateway = networkConfig.DefaultGateway;
            mfNetConfig.MacAddress = networkConfig.NetworkMacAddress;
            mfNetConfig.EnableDhcp = networkConfig.EnableDHCP;
            mfNetConfig.ConfigurationType = networkConfig.IsWireless ? MFNetworkConfiguration.NetworkConfigType.Wireless : MFNetworkConfiguration.NetworkConfigType.Generic;

            if (networkConfig.IsWireless)
            {
                MFWirelessConfiguration mfWifiConfig = new MFWirelessConfiguration();
                mfWifiConfig.Load(networkManager);

                mfWifiConfig.Authentication =networkConfig.Authentication;
                mfWifiConfig.Encryption = networkConfig.Encryption;
                mfWifiConfig.Radio = networkConfig.Radio;
                mfWifiConfig.PassPhrase = networkConfig.Passphrase;
                mfWifiConfig.UseEncryption = networkConfig.EncryptConfig;
                mfWifiConfig.NetworkKeyLength = networkConfig.NetworkKeyLength;
                mfWifiConfig.NetworkKey = networkConfig.NetworkKey;
                mfWifiConfig.ReKeyLength = networkConfig.ReKeyInternal.Length / 2;
                mfWifiConfig.ReKeyInternal = networkConfig.ReKeyInternal;
                mfWifiConfig.SSID = networkConfig.SSID;

                mfWifiConfig.Save(networkManager);
            }
            mfNetConfig.Save(networkManager);

            if (logToConsole)
            {
                Debug.WriteLine("Network settings saved.");
            }
        }

        void Initialize ()
        {
            var keyTypes = new string[]{ "64-bit", "128-bit", "256-bit", "512-bit", "1024-bit", "2048-bit" };
            var encryptionTypes = new string[] { "None", "WEP", "WPA", "WPAPSK", "Certificate" };
            var authenticationTypes = new string[] { "None", "EAP", "PEAP", "WCN", "Open", "Shared" };

            foreach(var key in keyTypes)
                NetworkKeyTypes.Add(key);

            foreach (var enc in encryptionTypes)
                EncryptionTypes.Add(enc);

            foreach (var auth in authenticationTypes)
                AuthenticationTypes.Add(auth);
        }

        void EnableWifiSettings()
        {
            



        }
    }
}
