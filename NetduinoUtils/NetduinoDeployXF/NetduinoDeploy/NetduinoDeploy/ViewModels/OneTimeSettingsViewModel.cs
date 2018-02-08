using System;
using System.Collections.Generic;
using Xamarin.Forms;
using NetduinoDeploy.Managers;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetduinoDeploy
{
    public class OneTimeSettingsViewModel : ViewModelBase
    {
        public List<string> Models { get; private set; }

        public string SelectedDevice { get; private set; }

        public string MacAddress
        {
            get => _macAddress;
            set => ValidateMacAddress(value);
        }
        string _macAddress = "00:00:00:00:01";

        public string Status { get; private set; }

        public Command CommitSettingsSelected { get; set; }

        Regex _macAddressRegex = new Regex("^ ([0 - 9A - Fa - f]{2}[:]){5}([0 - 9A - Fa - f]{2})$");

        public OneTimeSettingsViewModel ()
        {
            Initialize();

            CommitSettingsSelected = new Command(OnCommitSettings);
        }

        void OnCommitSettings()
        {
            Status = $"OnCommitSettings pressed at {DateTime.Now.TimeOfDay}";
            OnPropertyChanged("Status");
        }

        void ValidateMacAddress (string macAddress)
        {
            var result = _macAddressRegex.Match(macAddress);

            if (result.Success)
                _macAddress = macAddress;
        }

        void Initialize()
        {
            Models = new List<string>();
            // Models.Add("[Select Device Type]");

            //replace with devices.json
            Models = new List<string>() { "[Select Device Type]", "Netduino 2", "Netduino 2 Plus", "Netduino 3", "Netduino 3 Ethernet", "Netduino 3 WiFi" };

            foreach (var device in Globals.DeviceTypes)
                Models.Add(device.Name);

        /*    if (DfuContext.Current.GetDevices().Count == 1)
            {
                var productId = new OtpManager().GetOtpSettings().ProductID;

                SelectedDevice = Globals.DeviceTypes.Single(d => d.ProductID == productId).Name;
            }*/
        }
    }
}