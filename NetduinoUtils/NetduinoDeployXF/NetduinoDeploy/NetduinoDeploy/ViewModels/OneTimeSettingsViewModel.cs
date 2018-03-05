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

        public OneTimeSettingsViewModel ()
        {
            Initialize();

            CommitSettingsSelected = new Command(OnCommitSettings);
        }

        void OnCommitSettings()
        {
            var settings = new OtpSettings();
            settings.ProductID = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == SelectedDevice).ProductID);
            settings.MacAddress = MacAddress.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();

            var manager = new OtpManager();
            manager.SaveOtpSettings(settings);
        }

        bool ValidateMacAddress (string macAddress)
        {
            var result = _macAddressRegex.Match(macAddress);

            return result.Success;
        }

        void Initialize()
        {
            var deviceCount = DfuContext.Current?.GetDevices().Count;

            if (deviceCount == 1)
            {
                var settings = new OtpManager().GetOtpSettings();

                LoadDeviceList(Globals.ConnectedDeviceId = settings.ProductID);

                MacAddress = BitConverter.ToString(settings.MacAddress).Replace('-', ':');
                Status = string.Format("Device settings can be saved {0} more time{1}", settings.FreeSlots, settings.FreeSlots > 1 ? "s" : "");

                //lazy but probably about right
                RaiseAllPropertiesChanged();
            }
            else
            {
                SendConsoleMessage("No conected devices found");
                LoadDeviceList();
            }
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
                SendConsoleMessage($"Device connected: {deviceType.Name}");

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
    }
}