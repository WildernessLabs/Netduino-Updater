using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NetduinoUpdate
{
    public class DeviceInfo : DependencyObject
    {
        public static readonly DependencyProperty CanUpdateProperty =
            DependencyProperty.Register("CanUpdate", typeof(bool), typeof(DeviceInfo));
        public static readonly DependencyProperty CheckBoxVisibilityProperty =
            DependencyProperty.Register("CheckBoxVisibility", typeof(Visibility), typeof(DeviceInfo));
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(DeviceInfo));
        public static readonly DependencyProperty DevicePathProperty =
            DependencyProperty.Register("DevicePath", typeof(string), typeof(DeviceInfo));
        public static readonly DependencyProperty MacAddressProperty =
            DependencyProperty.Register("MacAddress", typeof(byte[]), typeof(DeviceInfo));
        public static readonly DependencyProperty OtpSlotsFreeProperty =
            DependencyProperty.Register("OtpSlotsFree", typeof(byte), typeof(DeviceInfo));
        public static readonly DependencyProperty ProductIDProperty =
            DependencyProperty.Register("ProductID", typeof(byte), typeof(DeviceInfo));
        public static readonly DependencyProperty ProductNameProperty =
            DependencyProperty.Register("ProductName", typeof(string), typeof(DeviceInfo));
        public static readonly DependencyProperty UpgradeFirmwareProperty =
            DependencyProperty.Register("UpgradeFirmware", typeof(Firmware), typeof(DeviceInfo));
        public static readonly DependencyProperty UpgradeVersionProperty =
            DependencyProperty.Register("UpgradeVersion", typeof(string), typeof(DeviceInfo));

        public bool CanUpdate
        {
            get
            {
                return (bool)GetValue(CanUpdateProperty);
            }
            set
            {
                SetValue(CanUpdateProperty, value);
                SetValue(CheckBoxVisibilityProperty, (value == true ? Visibility.Visible : Visibility.Collapsed));
            }
        }
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }
        public string DevicePath
        {
            get { return (string)GetValue(DevicePathProperty); }
            set { SetValue(DevicePathProperty, value); }
        }
        public byte[] MacAddress
        {
            get { return (byte[])GetValue(MacAddressProperty); }
            set { SetValue(MacAddressProperty, value); }
        }
        public byte OtpSlotsFree
        {
            get { return (byte)GetValue(OtpSlotsFreeProperty); }
            set { SetValue(OtpSlotsFreeProperty, value); }
        }
        public byte ProductID
        {
            get { return (byte)GetValue(ProductIDProperty); }
            set { SetValue(ProductIDProperty, value); }
        }
        public string ProductName
        {
            get { return (string)GetValue(ProductNameProperty); }
            set { SetValue(ProductNameProperty, value); }
        }
        public Firmware UpgradeFirmware
        {
            get { return (Firmware)GetValue(UpgradeFirmwareProperty); }
            set { SetValue(UpgradeFirmwareProperty, value); }
        }
        public string UpgradeVersion
        {
            get { return (string)GetValue(UpgradeVersionProperty); }
            set { SetValue(UpgradeVersionProperty, value); }
        }
    }
}
