using NetduinoDeploy;
using LibUsbDotNet.DeviceNotify;

namespace NetduinoFirmware.UWP
{
    public sealed partial class MainPage
    {
        public static IDeviceNotifier UsbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();

        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new NetduinoDeploy.App());

            // DfuContext.Init();

            UsbDeviceNotifier.OnDeviceNotify += UsbDeviceNotifier_OnDeviceNotify;
        }

        void UsbDeviceNotifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.ToString());
        }
    }
}
