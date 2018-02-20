using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Linq;
using System.Text.RegularExpressions;
using NetduinoDeploy.Managers;

namespace NetduinoDeploy
{
    public class OneTimeSettingsViewModel : ViewModelBase
    {
        public List<string> Models { get; private set; } = new List<string>();
        public int SelectedDeviceIndex { get; private set; }

        public string SelectedDeviceType
        {
            get => _selectedDeviceType;
            set
            {
                _selectedDeviceType = value;
                OnPropertyChanged("SelectedDeviceType");
            }
        }
        string _selectedDeviceType;

        public string SelectedDevice { get; private set; }

        public string MacAddress { get; private set; }
     //   {
         //   get => _macAddress;
         //   set => ValidateMacAddress(value);
      //  }
        //string _macAddress = "00:00:00:00:00";

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

        bool ValidateMacAddress (string macAddress)
        {
            var result = _macAddressRegex.Match(macAddress);

            return result.Success;
        }

        void Initialize()
        {
            var deviceCount = DfuContext.Current?.GetDevices().Count;

            byte productId = 0;
            
            if (deviceCount == 1)
            {
                var settings = new OtpManager().GetOtpSettings();
                productId = settings.ProductID;

                LoadDeviceList(productId);

                MacAddress = BitConverter.ToString(settings.MacAddress).Replace('-', ':');
                Status = string.Format("Device settings can be saved {0} more time{1}", settings.FreeSlots, settings.FreeSlots > 1 ? "s" : "");

                //lazy but probably the right call
                OnPropertyChanged(null);
            }
            else
            {
                LoadDeviceList();
            }
        }

        void LoadDeviceList(int productId = 0)
        {
            Models.Clear();
            Models.Add("[Select Device Type]");

            foreach (var device in Globals.DeviceTypes)
            {
                Models.Add(device.Name);
            }

            if (productId > 0)
            {
                string productName = Globals.DeviceTypes.Single(x => x.ProductID == productId).Name;

                //Not needed for Forms picker but I'll leave it here just in case the UI changes in the future 
                //SelectedDeviceIndex = Models.IndexOf(productName);

                SelectedDeviceType = productName; // Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.ItemAtIndex(SelectedDeviceIndex).Title);
            }
        }
    }
}