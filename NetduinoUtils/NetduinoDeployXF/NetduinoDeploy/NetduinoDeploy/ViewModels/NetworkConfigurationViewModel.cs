using NetduinoDeploy.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetduinoDeploy
{
    public class NetworkConfigurationViewModel : ViewModelBase
    {
        public List<string> NetworkKeyTypes { get; private set; } = new List<string>();
        public List<string> EncryptionTypes { get; private set; } = new List<string>();
        public List<string> AuthenticationTypes { get; private set; } = new List<string>();



        NetworkConfig networkConfig;

        public NetworkConfigurationViewModel (NetworkConfig networkConfig)
        {
            Initialize();

            this.networkConfig = networkConfig;
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
