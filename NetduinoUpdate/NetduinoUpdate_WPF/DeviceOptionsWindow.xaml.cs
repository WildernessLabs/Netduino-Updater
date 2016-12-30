using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetduinoUpdate
{
    /// <summary>
    /// Interaction logic for DeviceOptionsWindow.xaml
    /// </summary>
    public partial class DeviceOptionsWindow : Window
    {
        enum DeviceUpdateOperationMethod
        {
            EraseDeploymentSectors = 1
        }

        struct DeviceUpdateOperation
        {
            public string DevicePath;
            public Firmware UpdateFirmware;
            public DeviceUpdateOperationMethod OperationMethod;
        }

        NetduinoUpdate.DeviceInfo _deviceInfo;
        List<NetduinoUpdate.Firmware> _firmwares;

        byte _otpSlotsFree;
        bool _isEditingGeneralOtpSettings = false;

        BackgroundWorker _updateWorker;

        class ProductNameComboBoxItem : IComparable<ProductNameComboBoxItem>
        {
            public ushort ProductID { get; set; }
            public string ProductName { get; set; }
            public bool HasMacAddress { get; set; }

            public int CompareTo(ProductNameComboBoxItem other)
            {
                return ProductName.CompareTo(other.ProductName);
            }
        }

        public DeviceOptionsWindow(NetduinoUpdate.DeviceInfo deviceInfo, List<NetduinoUpdate.Firmware> firmwares)
        {
            InitializeComponent();

            _deviceInfo = deviceInfo;
            _firmwares = firmwares;
        }

        private void ChangeMacAddressHyperlink_Click(object sender, RoutedEventArgs e)
        {
            ChangeMacAddressHyperlink.Visibility = System.Windows.Visibility.Collapsed;
            MacAddressTextBox.IsEnabled = true;

            if (_isEditingGeneralOtpSettings == false)
            {
                _otpSlotsFree--;
                UpdateOtpCautionText();
            }

            _isEditingGeneralOtpSettings = true;
        }

        private void ChangeProductHyperlink_Click(object sender, RoutedEventArgs e)
        {
            ChangeProductHyperlink.Visibility = System.Windows.Visibility.Collapsed;
            ProductNameComboBox.IsEnabled = true;

            if (_isEditingGeneralOtpSettings == false)
            {
                _otpSlotsFree--;
                UpdateOtpCautionText();
            }

            _isEditingGeneralOtpSettings = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // create our update worker
            _updateWorker = new BackgroundWorker();
            _updateWorker.DoWork += _updateWorker_DoWork;
            _updateWorker.ProgressChanged += _updateWorker_ProgressChanged;
            _updateWorker.RunWorkerCompleted += _updateWorker_RunWorkerCompleted;
            _updateWorker.WorkerSupportsCancellation = true;
            _updateWorker.WorkerReportsProgress = true;

            // fill our ProductName combobox
            List<ProductNameComboBoxItem> productNames = new List<ProductNameComboBoxItem>();
            foreach (NetduinoUpdate.Firmware firmware in _firmwares)
            {
                productNames.Add(new ProductNameComboBoxItem() { ProductID = firmware.ProductID, ProductName = firmware.ProductName, HasMacAddress = firmware.HasMacAddress });
            }
            // sort our combo box by product name
            productNames.Sort();

            for (int index = 0; index < productNames.Count; index++)
            {
                ProductNameComboBox.Items.Add(productNames[index].ProductName);
            }
            if (_deviceInfo.ProductID != 0)
            {
                for (int index = 0; index < productNames.Count; index++)
                {
                    if (productNames[index].ProductID == _deviceInfo.ProductID)
                    {
                        ProductNameComboBox.SelectedIndex = index;
                        break;
                    }
                }
            }

            if (_deviceInfo.MacAddress.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }) == false)
            {
                MacAddressTextBox.Text = FormatMacAddress(_deviceInfo.MacAddress);
            }

            _otpSlotsFree = _deviceInfo.OtpSlotsFree;

            UpdateOtpCautionText();
            ChangeMacAddressHyperlink.Visibility = ((_deviceInfo.OtpSlotsFree > 0) ? Visibility.Visible : Visibility.Hidden);
            ChangeProductHyperlink.Visibility = ((_deviceInfo.OtpSlotsFree > 0) ? Visibility.Visible : Visibility.Hidden);

            // if the configuration settings have not been set previously, put them in "editable" mode by default.
            if (_deviceInfo.ProductID == 0)
            {
                ChangeProductHyperlink.Visibility = Visibility.Collapsed;
                ProductNameComboBox.IsEnabled = true;
                _isEditingGeneralOtpSettings = true;
            }
            if (_deviceInfo.UpgradeFirmware != null && _deviceInfo.UpgradeFirmware.HasMacAddress && _deviceInfo.MacAddress.SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }))
            {
                ChangeMacAddressHyperlink.Visibility = Visibility.Collapsed;
                MacAddressTextBox.IsEnabled = true;
                _isEditingGeneralOtpSettings = true;
            }

            UpdateNetworkTabVisibility();
            UpdateToolsTabVisibility();
        }

        void UpdateNetworkTabVisibility()
        {
            bool foundFirmware = false;
            for (int index = 0; index < _firmwares.Count; index++)
            {
                if (_firmwares[index].ProductID == _deviceInfo.ProductID)
                {
                    NetworkTab.Visibility = (_firmwares[index].HasMacAddress ? Visibility.Visible : Visibility.Collapsed);
                    foundFirmware = true;
                    break;
                }
            }
            if (!foundFirmware)
                NetworkTab.Visibility = Visibility.Collapsed;
        }

        void UpdateToolsTabVisibility()
        {
            ToolsTab.Visibility = (_deviceInfo.ProductID > 0 ? Visibility.Visible : Visibility.Collapsed);
        }

        void UpdateOtpCautionText()
        {
            ConfigurationOtpSlotsRemainingLabel1.Text = GetOtpCautionText(_otpSlotsFree);
            ConfigurationOtpSlotsRemainingLabel2.Text = GetOtpCautionText(_otpSlotsFree);
        }

        string GetOtpCautionText(byte otpSlotsFree)
        {
            return "NOTE: These settings, stored in non-erasable storage, may be changed up to " + otpSlotsFree + " more time" + (otpSlotsFree == 1 ? "" : "s") + ".";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MacAddressTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                case Key.A:
                case Key.B:
                case Key.C:
                case Key.D:
                case Key.E:
                case Key.F:
                case Key.OemMinus:
                    // do nothing; these are fine.
                    e.Handled = false;
                    break;
                default:
                    // cancel this key press
                    e.Handled = true; 
                    break;
            }
        }

        string FormatMacAddress(byte[] macAddress)
        {
            StringBuilder formattedMacAddress = new StringBuilder();
            for (int i = 0; i < macAddress.Length; i++)
            {
                formattedMacAddress.Append(macAddress[i].ToString("X2").ToUpper());
                formattedMacAddress.Append("-");
            }
            if (formattedMacAddress.Length > 0)
                formattedMacAddress.Remove(formattedMacAddress.Length - 1, 1);

            return formattedMacAddress.ToString();
        }

        bool ValidateMacAddressString(string value)
        {
            return (StripMacAddressString(value).Length == 12);
        }

        string StripMacAddressString(string value)
        {
            string strippedMacAddress = "";
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '-')
                {
                    strippedMacAddress += value[i];
                }
            }

            return strippedMacAddress;
        }

        byte[] ConvertMacAddressStringToByteArray(string value)
        {
            int VALID_MAC_ADDRESS_HEX_STRING_LENGTH = 12;

            string strippedMacAddress = StripMacAddressString(value);

            // now grow or shrink the mac address to 12 hexadecimal digits.
            if (strippedMacAddress.Length > VALID_MAC_ADDRESS_HEX_STRING_LENGTH)
            {
                strippedMacAddress = strippedMacAddress.Substring(0, 12);
            }
            while (strippedMacAddress.Length < VALID_MAC_ADDRESS_HEX_STRING_LENGTH)
            {
                strippedMacAddress += "0";
            }

            byte[] macAddress = new byte[6];
            for (int i = 0; i < strippedMacAddress.Length; i += 2)
            {
                macAddress[i / 2] = (byte)Convert.ToInt16(strippedMacAddress.Substring(i, 2), 16);
            }

            return macAddress;
        }

        private void ProductNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < _firmwares.Count; i++)
            {
                if (_firmwares[i].ProductName == ProductNameComboBox.SelectedValue.ToString())
                {
                    _deviceInfo.ProductID = _firmwares[i].ProductID;
                }
            }

            UpdateNetworkTabVisibility();
            UpdateToolsTabVisibility();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // make sure we validate and save any final settings changes
            ValidateMacAddressTextBox();

            if (_isEditingGeneralOtpSettings)
            {
                // save updated settings to OTP
                try
                { 
                    OtpSettings otpSettings = new OtpSettings(_deviceInfo.DevicePath);
                    bool success = otpSettings.WriteSettings(_deviceInfo.ProductID, _deviceInfo.MacAddress);

                    if (!success)
                        MessageBox.Show("Could not save configuration.");
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Could not save configuration.\r\n\r\nError: " + ex.Message);
                }
            }
        }

        private void MacAddressTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ValidateMacAddressTextBox();
        }

        private void ValidateMacAddressTextBox()
        {
            // check content
            if (MacAddressTextBox.Text != "")
            {
                if (!ValidateMacAddressString(MacAddressTextBox.Text))
                {
                    MessageBox.Show("MAC Address " + MacAddressTextBox.Text + " is not valid.");
                }
                else
                {
                    _deviceInfo.MacAddress = ConvertMacAddressStringToByteArray(MacAddressTextBox.Text);
                }
                MacAddressTextBox.Text = FormatMacAddress(_deviceInfo.MacAddress);
            }
        }

        private void EraseDeploymentSectorButton_Click(object sender, RoutedEventArgs e)
        {
            EraseDeploymentSectorButton.IsEnabled = false;
            this.IsHitTestVisible = false;
            
            DeviceUpdateOperation operation = new DeviceUpdateOperation();
            operation.DevicePath = _deviceInfo.DevicePath;
            operation.UpdateFirmware = _deviceInfo.UpgradeFirmware;
            operation.OperationMethod = DeviceUpdateOperationMethod.EraseDeploymentSectors;
            _updateWorker.RunWorkerAsync(operation);
        }

        void _updateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DeviceUpdateOperation operation = (DeviceUpdateOperation)e.Argument;

            this.Dispatcher.Invoke(new Action(delegate()
                {
                    // reset and show the progress bar
                    ProgressBar.Value = 0.0;
                    ProgressBar.Visibility = System.Windows.Visibility.Visible;
                }));

            if (_updateWorker.CancellationPending)
                return;

            try
            {
                switch (operation.OperationMethod)
                {
                    case DeviceUpdateOperationMethod.EraseDeploymentSectors:
                        EraseDeploymentSectors(operation.DevicePath, operation.UpdateFirmware);
                        break;
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(new Action(delegate()
                {
                    MessageBox.Show("Error: " + ex.Message);
                }));
            }
        }

        void _updateWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = (double)e.ProgressPercentage;
        }

        void _updateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // hide the progress bar
            ProgressBar.Visibility = System.Windows.Visibility.Collapsed;

            EraseDeploymentSectorButton.IsEnabled = true;
            this.IsHitTestVisible = true;
        }

        private void EraseDeploymentSectors(string devicePath, Firmware upgradeFirmware)
        {
            STDfuDevice device = new STDfuDevice(devicePath);

            // TODO: make sure we are in DFU mode; if we are in app mode (runtime) then we need to detach and re-enumerate.

            // get our total deployment sectors count
            List<uint> allSectorBaseAddresses = new List<uint>();
            foreach (Firmware.FirmwareRegion region in upgradeFirmware.FirmwareRegions)
            {
                if (region.Name.ToUpper()=="DEPLOYMENT")
                    allSectorBaseAddresses.AddRange(region.SectorBaseAddresses);
            }

            // erase each sector
            for (int iSector = 0; iSector < allSectorBaseAddresses.Count; iSector++)
            {
                if (!device.EraseSector(allSectorBaseAddresses[iSector]))
                    throw new Exception("Could not erase sector.");
                _updateWorker.ReportProgress((int)((((double)iSector + 1) / (double)allSectorBaseAddresses.Count) * 100));
            }

            //// step 4: restart board
            //device.SetAddressPointer(0x08000001); // NOTE: for thumb2 instructinos, we added 1 to the "base address".  Otherwise our board will not restart properly.
            //// leave DFU mode.
            //device.LeaveDfuMode();
        }
    }
}
