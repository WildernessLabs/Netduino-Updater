using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetduinoUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Guid DEVICE_INTERFACE_GUID_STDFU = new Guid(0x3fe809ab, 0xfb91, 0x4cb5, 0xa6, 0x43, 0x69, 0x67, 0x0d, 0x52, 0x36, 0x6e);

        enum DeviceUpdateOperationMethod
        {
            EraseAndUpload = 1
        }

        struct DeviceUpdateOperation
        {
            public string DevicePath;
            public Firmware UpdateFirmware;
            public DeviceUpdateOperationMethod OperationMethod;
        }

        System.Collections.ObjectModel.ObservableCollection<DeviceInfo> _devices = new System.Collections.ObjectModel.ObservableCollection<DeviceInfo>();

        BackgroundWorker _updateWorker;

        List<Firmware> _firmwares = new List<Firmware>();

        double _progressValue = 0.0;

        bool _hideOneOtpSlot = true;

        MACAddressService _macAddresses = new MACAddressService();

        public MainWindow()
        {
            InitializeComponent();
            //_macAddresses.WriteAddresses(0x60d7e3a00000, 1048576);
        }

        public void RegisterForPnpEvents()
        {
            Win32Api.DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new Win32Api.DEV_BROADCAST_DEVICEINTERFACE();
            IntPtr devBroadcastDeviceInterfaceBuffer;
            IntPtr deviceNotificationHandle;

            System.Windows.Interop.HwndSource hwndSource = System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            hwndSource.AddHook(new System.Windows.Interop.HwndSourceHook(WndProc));

            int size = Marshal.SizeOf(devBroadcastDeviceInterface);
            devBroadcastDeviceInterface.dbcc_size = size;
            devBroadcastDeviceInterface.dbcc_devicetype = Win32Api.DBT_DEVTYP_DEVICEINTERFACE;
            devBroadcastDeviceInterface.dbcc_reserved = 0;
            devBroadcastDeviceInterface.dbcc_classguid = DEVICE_INTERFACE_GUID_STDFU;

            devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true);
            deviceNotificationHandle = Win32Api.RegisterDeviceNotification(hwndSource.Handle, devBroadcastDeviceInterfaceBuffer, Win32Api.DEVICE_NOTIFY_WINDOW_HANDLE);
            Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer);
        }

        public void DeregisterFromPnpEvents()
        {
            System.Windows.Interop.HwndSource hwndSource = System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            hwndSource.RemoveHook(new System.Windows.Interop.HwndSourceHook(WndProc));

            Win32Api.UnregisterDeviceNotification(hwndSource.Handle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle whatever Win32 message it is we feel like handling
            if (msg == Win32Api.WM_DEVICECHANGE)
            {
                if (wParam.ToInt32() == Win32Api.DBT_DEVICEARRIVAL)
                {
                    Win32Api.DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new Win32Api.DEV_BROADCAST_DEVICEINTERFACE();
                    Marshal.PtrToStructure(lParam, devBroadcastDeviceInterface);
                    OnDeviceArrival(devBroadcastDeviceInterface.dbcc_classguid, devBroadcastDeviceInterface.dbcc_name);
                }
                else if (wParam.ToInt32() == Win32Api.DBT_DEVICEREMOVECOMPLETE)
                {
                    Win32Api.DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new Win32Api.DEV_BROADCAST_DEVICEINTERFACE();
                    Marshal.PtrToStructure(lParam, devBroadcastDeviceInterface);
                    OnDeviceRemoveComplete(devBroadcastDeviceInterface.dbcc_classguid, devBroadcastDeviceInterface.dbcc_name);
                }
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void OnDeviceArrival(Guid classGuid, string devicePath)
        {
            if (classGuid == DEVICE_INTERFACE_GUID_STDFU)
            {
                bool alreadyFound = false;
                foreach (DeviceInfo deviceInfo in _devices)
                {
                    if (deviceInfo.DevicePath.ToUpper()==devicePath.ToUpper())
                    {
                        alreadyFound = true;
                        break;
                    }
                }
                if (!alreadyFound)
                {
                    // get the board's device type and settings
                    try
                    {
                        byte productID;
                        byte[] macAddress;
                        byte otpSlotsFree;

                        OtpSettings otpSettings = new OtpSettings(devicePath);
                        bool success = otpSettings.ReadSettings(out productID, out macAddress, out otpSlotsFree);
                        if (!success)
                        {
                            _devices.Add(new DeviceInfo()
                            {
                                CanUpdate = false,
                                OtpSlotsFree = 0,
                                IsChecked = false,
                                DevicePath = devicePath,
                                MacAddress = macAddress,
                                ProductID = productID,
                                ProductName = "Unknown (please reconnect)",
                                UpgradeFirmware = null,
                                UpgradeVersion = "Unknown",
                            });

                            return;
                        }
                        /* BEGIN: ADD THIS IN IF WE WANT TO AUTO-SET OUR OTP PRODUCT ID EN MASSE */
                        if (otpSlotsFree > 2)
                        {
                            otpSettings.ReadSettings(out productID, out macAddress, out otpSlotsFree);
                            if (productID == 0)
                            {
                                // 9=Netduino3Wifi
                                // 8=Netduino3Ethernet
                                // 5=NetduinoPlus2
                                // 7=Netduino3
                                // 6=Netduino2

                                otpSettings.WriteSettings(9, _macAddresses.GetNextAddress());
                                //otpSettings.WriteSettings(6, macAddress);
                            }

                            success = otpSettings.ReadSettings(out productID, out macAddress, out otpSlotsFree);
                            if (!success) return;
                        }
                        /* END: ADD THIS IN IF WE WANT TO AUTO-SET OUR OTP PRODUCT ID EN MASSE */

                        // by default, we hide one OTP slot.  This is so that users cannot accidentally use up all slots--and have one final chance to change the cnofiguration.
                        if (otpSlotsFree >= 1 && _hideOneOtpSlot)
                            otpSlotsFree--;

                        // now find the latest firmware for this product
                        Firmware upgradeFirmware = null;
                        if (productID != 0)
                            upgradeFirmware = GetLatestFirmwareForProduct(productID);
                        string productName = (upgradeFirmware != null ? upgradeFirmware.ProductName : "STM Device in DFU Mode");

                        _devices.Add(new DeviceInfo()
                        {
                            CanUpdate = (upgradeFirmware != null),
                            OtpSlotsFree = otpSlotsFree,
                            IsChecked = false,
                            DevicePath = devicePath,
                            MacAddress = macAddress,
                            ProductID = productID,
                            ProductName = productName,
                            UpgradeFirmware = upgradeFirmware,
                            UpgradeVersion = (upgradeFirmware != null ? upgradeFirmware.Version.ToString() + (upgradeFirmware.VersionBetaSuffix != null ? " " + upgradeFirmware.VersionBetaSuffix : "") : "Unknown")
                        });
                    }
                    catch
                    {
                    }
                }
            }
        }
        
        private Firmware GetLatestFirmwareForProduct(byte productID)
        {
            Firmware newestFirmware = null;
            foreach (Firmware firmware in _firmwares)
            {
                if (firmware.ProductID == productID)
                {
                    if (newestFirmware == null || (newestFirmware.Version.CompareTo(firmware.Version) < 0))
                        newestFirmware = firmware;
                }
            }

            return newestFirmware;
        }

        private void OnDeviceRemoveComplete(Guid classGuid, string devicePath)
        {
            if (classGuid == DEVICE_INTERFACE_GUID_STDFU)
            {
                foreach(DeviceInfo deviceInfo in _devices)
                {
                    if (deviceInfo.DevicePath.ToUpper()==devicePath.ToUpper())
                    {
                        _devices.Remove(deviceInfo);
                        break;
                    }
                }
            }

            EnableEraseAndUploadButton();
        }

        private void EnableEraseAndUploadButton()
        {
            bool devicesChecked = false;
            foreach (DeviceInfo deviceInfo in _devices)
            {
                if (deviceInfo.IsChecked)
                    devicesChecked = true;
            }
            eraseAndUploadButton.IsEnabled = devicesChecked;
        }

        private void LoadFirmwareFiles()
        {
            // search for all XML files in our firmware subdirectory, recursively

            // find all folders containing firmware
            string firmwareRootFolder = Environment.CurrentDirectory + @"\Firmware";
            List<string> firmwareFolders = Directory.GetDirectories(firmwareRootFolder, "*.*", SearchOption.AllDirectories).ToList<string>();
            firmwareFolders.Add(firmwareRootFolder); // be sure to search the root firmware folder, in case the user only has one firmware option.

            // enumerate the firmware in each folder
            foreach (string firmwareFolder in firmwareFolders)
            {
                string[] firmwareFiles = Directory.GetFiles(firmwareFolder, "*.xml");
                foreach (string firmwareFile in firmwareFiles)
                {
                    try
                    {
                        Firmware firmware = new Firmware(firmwareFile);
                        _firmwares.Add(firmware);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // add our version to the title
            Version appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Title += " (v" + appVersion.Major + "." + appVersion.Minor + "." + appVersion.Build + ")";

            // create our update worker
            _updateWorker = new BackgroundWorker();
            _updateWorker.DoWork += _updateWorker_DoWork;
            _updateWorker.ProgressChanged += _updateWorker_ProgressChanged;
            _updateWorker.RunWorkerCompleted += _updateWorker_RunWorkerCompleted;
            _updateWorker.WorkerSupportsCancellation = true;
            _updateWorker.WorkerReportsProgress = true;

            // load all firmware files
            LoadFirmwareFiles();

            // enumerate all attached devices
            string[] stdfuDevicePaths = GetAllStdfuDevicePaths();
            foreach (string devicePath in stdfuDevicePaths)
            {
                OnDeviceArrival(DEVICE_INTERFACE_GUID_STDFU, devicePath);
            }
            this.devicesListView.ItemsSource = _devices;

            // register for PnP events (attach/detach of devices)
            RegisterForPnpEvents();
        }

        private string[] GetAllStdfuDevicePaths()
        {
            List<string> devicePaths = new List<string>();

            // enumerate all ST DFU devices
            //
            // get a deviceInfoSet containing entries for all present STDFU devices
            IntPtr deviceInfoSet = Win32Api.SetupDiGetClassDevs(ref DEVICE_INTERFACE_GUID_STDFU, IntPtr.Zero, IntPtr.Zero, (uint)Win32Api.SetupDiGetClassFlags.DIGCF_PRESENT | (uint)Win32Api.SetupDiGetClassFlags.DIGCF_DEVICEINTERFACE);
            if (deviceInfoSet != Win32Api.INVALID_HANDLE_VALUE)
            {
                try
                {
                    bool success = true;
                    uint memberIndex = 0;

                    // enumerate each present device
                    while (success)
                    {
                        // request device at index memberIndex; this function call with return false when memberIndex is beyond the bounds of the deviceInfoSet entries.
                        Win32Api.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new Win32Api.SP_DEVICE_INTERFACE_DATA();
                        deviceInterfaceData.cbSize = (uint)Marshal.SizeOf(deviceInterfaceData);
                        success = Win32Api.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref DEVICE_INTERFACE_GUID_STDFU, memberIndex, ref deviceInterfaceData);
                        if (success)
                        {
                            // we found a device at this index; get its DeviceInterfaceDetails (e.g. device path)
                            uint bufferSize = 0;
                            IntPtr detailDataBuffer;

                            Win32Api.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, out bufferSize, IntPtr.Zero);
                            detailDataBuffer = Marshal.AllocHGlobal((int)bufferSize);
                            // fill in the cbSize value; our buffer could contain a TCHAR[ANYSIZE_ARRAY] string of arbitrary length so this will allocate the proper size and read out the string via Marshal methods.
                            Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                            try
                            {
                                if (Win32Api.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailDataBuffer, bufferSize, out bufferSize, IntPtr.Zero))
                                {
                                    // retrieve device path and add it to our devicePaths list
                                    IntPtr devicePathName = new IntPtr(detailDataBuffer.ToInt32() + 4);
                                    devicePaths.Add(Marshal.PtrToStringAuto(devicePathName));
                                }
                            }
                            finally
                            {
                                // free our manually-allocated DeviceInterfaceDetails buffer
                                Marshal.FreeHGlobal(detailDataBuffer);
                            }

                            memberIndex++;
                        }
                    }
                }
                finally
                {
                    // clean up deviceInfoSet
                    Win32Api.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return devicePaths.ToArray();
        }

        private void devicesListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            bool hitValidArea = true;

            // retrieve parent listViewItem and associated DeviceInfo
            ListViewItem listViewItem = FindVisualParent<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem != null)
            {
                DeviceInfo deviceInfo = (DeviceInfo)listViewItem.Content;

                // get our mouse position
                Point position = e.GetPosition(devicesListView);

                // make sure we are not clicking on our checkbox.
                if (HitTestAvoidControlByType<System.Windows.Controls.CheckBox>(listViewItem, position, System.Windows.Controls.CheckBox.NameProperty, "DeviceSelectedCheckBox"))
                    hitValidArea = false;
                //// make sure we are not clicking on our combo box.
                //if (HitTestAvoidControlByType<System.Windows.Controls.ComboBox>(listViewItem, position, System.Windows.Controls.ComboBox.NameProperty, "VersionComboBox"))
                //    hitValidArea = false;
                // make sure we are not clicking on our "options" hyperlink.
                if (HitTestAvoidControlByType<System.Windows.Controls.TextBlock>(listViewItem, position, System.Windows.Controls.TextBlock.NameProperty, "OptionsHyperlink"))
                    hitValidArea = false;
                // make sure our hit area is valid at all
                HitTestResult hitTestResult = VisualTreeHelper.HitTest(devicesListView, position);
                if (hitTestResult == null || FindSelfOrVisualParent<ListViewItem>(hitTestResult.VisualHit, null, null) != listViewItem)
                    hitValidArea = false;

                if (hitValidArea && deviceInfo.CanUpdate == true)
                {
                    // invert our item's checkbox
                    deviceInfo.IsChecked = !deviceInfo.IsChecked;
                }
            }
        }

        private bool HitTestAvoidControlByType<T>(ListViewItem listViewItem, Point position) where T : DependencyObject
        {
            return HitTestAvoidControlByType<T>(listViewItem, position, null, null);
        }

        private bool HitTestAvoidControlByType<T>(ListViewItem listViewItem, Point position, DependencyProperty property, object value) where T : DependencyObject
        {
            object targetTestObject = FindVisualChild<T>(listViewItem, property, value);

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(devicesListView, position);
            if (hitTestResult == null)
                return false;
            object hitTestObject = FindSelfOrVisualParent<T>(hitTestResult.VisualHit, property, value);

            return (targetTestObject == hitTestObject);
        }

        private void AllDevices_CheckChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)((CheckBox)(sender)).IsChecked;
            foreach (DeviceInfo item in devicesListView.Items)
            {
                if (item.CanUpdate)
                    item.IsChecked = isChecked;
            }
        }

        private void Device_CheckChanged(object sender, RoutedEventArgs e)
        {
            EnableEraseAndUploadButton();
        }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            return FindVisualChild<T>(obj, null, null);
        }

        private T FindVisualChild<T>(DependencyObject obj, DependencyProperty property, object value) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is T && (property == null || value == null || value.Equals(child.GetValue(property))))
                {
                    return (T)child;
                }
                else if (child != null)
                {
                    T childOfChild = FindVisualChild<T>(child, property, value);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private T FindSelfOrVisualParent<T>(DependencyObject obj, DependencyProperty property, object value) where T : DependencyObject
        {
            if (obj is T)
                return (T)obj;
            else
                return FindVisualParent<T>(obj, property, value);
        }

        private T FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            return FindVisualParent<T>(obj, null, null);
        }

        private T FindVisualParent<T>(DependencyObject obj, DependencyProperty property, object value) where T : DependencyObject
        {
            while (obj != null)
            {
                ContentElement contentElement = obj as ContentElement;
                if (contentElement != null)
                {
                    // if our object is a content element, find its parent via ContentOperations.GetParent(...)
                    obj = ContentOperations.GetParent(contentElement);

                    // if that fails, try FrameworkContentElement.Parent instead.
                    FrameworkContentElement frameworkContentElement = contentElement as FrameworkContentElement;
                    if (frameworkContentElement != null)
                    {
                        obj = frameworkContentElement.Parent;
                    }
                    else
                    {
                        obj = null;
                    }
                }
                else
                {
                    // if our object is not a content element, find its parent via VisualTreeHelper.GetParent(...)
                    obj = VisualTreeHelper.GetParent(obj);
                }

                if (obj != null && obj is T && (property == null || value == null || value.Equals(obj.GetValue(property))))
                {
                    return (T)obj;
                }
            }

            return null;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            RegisterForPnpEvents();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DeregisterFromPnpEvents();
        }

        private void eraseAndUploadButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonContent = ((Button)sender).Content.ToString();
            if (string.Compare(buttonContent, "Upgrade", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // create a list of all devices selected for upgrade
                List<DeviceUpdateOperation> updateOperations = new List<DeviceUpdateOperation>();
                foreach (DeviceInfo deviceInfo in _devices)
                {
                    if (deviceInfo.IsChecked)
                    {
                        DeviceUpdateOperation operation = new DeviceUpdateOperation();
                        operation.DevicePath = deviceInfo.DevicePath;
                        operation.OperationMethod = DeviceUpdateOperationMethod.EraseAndUpload;
                        operation.UpdateFirmware = deviceInfo.UpgradeFirmware;
                        updateOperations.Add(operation);
                    }
                }
                _updateWorker.RunWorkerAsync(updateOperations);
            }
            else if (string.Compare(buttonContent, "Cancel", StringComparison.OrdinalIgnoreCase) == 0)
            {
                eraseAndUploadButton.IsEnabled = false;
                _updateWorker.CancelAsync();
            }
        }

        void _updateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<DeviceUpdateOperation> updateOperations = (List<DeviceUpdateOperation>)e.Argument;

            this.Dispatcher.Invoke(new Action(delegate()
                {
                    // change our button to a Cancel button
                    eraseAndUploadButton.Content = "Cancel";

                    // reset and show the progress bar
                    progressBar.Value = 0.0;
                    progressBar.Visibility = System.Windows.Visibility.Visible;

                    // shrink and disable the listview
                    devicesListView.Height -= progressBar.Height;
                    devicesListView.IsEnabled = false;
                }));

            // upgrade the firmware on each of the selected devices
            for (int iDevice = 0; iDevice < updateOperations.Count; iDevice++)
            {
                if (_updateWorker.CancellationPending)
                    return;

                DeviceUpdateOperation operation = updateOperations[iDevice];
                _progressValue = ((double)iDevice / (double)updateOperations.Count);
                try
                {
                    switch (operation.OperationMethod)
                    {
                        case DeviceUpdateOperationMethod.EraseAndUpload:
                            EraseAndUploadDevice(operation.DevicePath, operation.UpdateFirmware, _progressValue, ((double)1 / (double)updateOperations.Count));
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
        }

        void _updateWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = (double)e.ProgressPercentage;
        }

        void _updateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // rename and enable the Upgrade button
            eraseAndUploadButton.Content = "Upgrade";
            EnableEraseAndUploadButton();
            
            // expand and enable the listview
            devicesListView.Height += progressBar.Height;
            devicesListView.IsEnabled = true;
            
            // hide the progress bar
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void EraseAndUploadDevice(string devicePath, Firmware upgradeFirmware, double startProgressPercent, double operationTotalProgressPercent)
        {
            double localProgressPercent = 0.0;

            STDfuDevice device = new STDfuDevice(devicePath);

            // TODO: make sure we are in DFU mode; if we are in app mode (runtime) then we need to detach and re-enumerate.

            // get our total sectors and block counts
            List<uint> allSectorBaseAddresses = new List<uint>();
            uint totalBlockCount = 0;
            foreach (Firmware.FirmwareRegion region in upgradeFirmware.FirmwareRegions)
            {
                allSectorBaseAddresses.AddRange(region.SectorBaseAddresses);

                if (region.Filename != null)
                {
                    System.IO.StreamReader streamReader = new System.IO.StreamReader(upgradeFirmware.FolderPath + "\\" + region.Filename);
                    string hexFileString = streamReader.ReadToEnd();
                    streamReader.Dispose();
                    byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, region.BaseAddress);
                    totalBlockCount += (uint)Math.Ceiling((double)hexFileBytes.Length / (double)device.BlockTransferSize);
                }
            }

            // erase each sector
            for (int iSector = 0; iSector < allSectorBaseAddresses.Count; iSector++)
            {
                if (!device.EraseSector(allSectorBaseAddresses[iSector]))
                    throw new Exception("Could not erase sector.");
                localProgressPercent = (((double)iSector + 1) / (double)allSectorBaseAddresses.Count) * 0.5;
                _updateWorker.ReportProgress(CalculateWorkerProgress(localProgressPercent, startProgressPercent, operationTotalProgressPercent));
            }

            // do "read unprotect"
            //Debug.Print("Unprotecting all sectors, erasing...");
            //Debug.Print("operation " + (stdfuDeviceReadUnprotect() ? "SUCCESS" : "FAILED"));

            //// do mass erase
            //Debug.Print("erasing all sectors...");
            //Debug.Print("operation " + (device.EraseAllSectors() ? "SUCCESS" : "FAILED"));
            //localProgressPercent = 0.3;

            // now flash the board!
            ushort blockSize = device.BlockTransferSize;
            ushort completedBlocks = 0;

            foreach (Firmware.FirmwareRegion region in upgradeFirmware.FirmwareRegions)
            {
                if (region.Filename != null)
                {
                    System.IO.StreamReader streamReader = new System.IO.StreamReader(upgradeFirmware.FolderPath + "\\" + region.Filename);
                    string hexFileString = streamReader.ReadToEnd();
                    streamReader.Dispose();
                    byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, region.BaseAddress);

                    // set our download address pointer
                    if (device.SetAddressPointer(region.BaseAddress) == false)
                    {
                        throw new Exception("Could not set base address for flash operation.");
                    }

                    // write blocks to the board and verify; we must have already erased our sectors before this point
                    for (ushort index = 0; index <= (hexFileBytes.Length / blockSize); index++)
                    {
                        // write block to the board
                        byte[] buffer = new byte[Math.Min(hexFileBytes.Length - (index * blockSize), blockSize)];
                        Array.Copy(hexFileBytes, index * blockSize, buffer, 0, buffer.Length);
                        bool success = device.WriteMemoryBlock(index, buffer);
                        if (!success)
                            throw new Exception("Write failed.");

                        // verify written block
                        byte[] verifyBuffer = new byte[buffer.Length];
                        success = device.ReadMemoryBlock(index, verifyBuffer);
                        if (!success || !buffer.SequenceEqual(verifyBuffer))
                            throw new Exception("Verify failed.");

                        completedBlocks++;

                        localProgressPercent = 0.5 + (((double)(completedBlocks + 1) / (double)totalBlockCount) * 0.5);
                        _updateWorker.ReportProgress(CalculateWorkerProgress(localProgressPercent, startProgressPercent, operationTotalProgressPercent));
                    }
                }
            }

            // step 4: restart board
            device.SetAddressPointer(0x08000001); // NOTE: for thumb2 instructinos, we added 1 to the "base address".  Otherwise our board will not restart properly.
            // leave DFU mode.
            device.LeaveDfuMode();
        }

        int CalculateWorkerProgress(double localProgressPercent, double startProgressPercent, double operationTotalProgressPercent)
        {
            return (int)((startProgressPercent + localProgressPercent * operationTotalProgressPercent) * 100);
        }

        private void DeviceOptionsHyperlink_Click(object sender, RoutedEventArgs e)
        {
            ListViewItem listViewItem = FindVisualParent<ListViewItem>((DependencyObject)e.OriginalSource);
            DeviceInfo deviceInfo = (DeviceInfo)(listViewItem.Content);

            DeviceOptionsWindow optionsWindow = new DeviceOptionsWindow(deviceInfo, _firmwares);
            optionsWindow.Owner = this;
            optionsWindow.ShowDialog();

            optionsWindow.Close();

            // re-query the device
            string devicePath = deviceInfo.DevicePath;
            OnDeviceRemoveComplete(DEVICE_INTERFACE_GUID_STDFU, devicePath);
            OnDeviceArrival(DEVICE_INTERFACE_GUID_STDFU, devicePath);
        }
    }
}
