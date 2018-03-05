using NetduinoDeploy.Managers;
using NetduinoFirmware;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace NetduinoDeploy
{
    public class FirmwareViewModel : ViewModelBase
    {
        int maxFileStringLen = 60;

        public string ConfigFile
        {
            get => GetDisplayString(_configFile, maxFileStringLen);
            set => _configFile = value;
        }
        string _configFile;

        public string FlashFile
        {
            get => GetDisplayString(_flashFile, maxFileStringLen);
            set => _flashFile = value;
        }
        string _flashFile;

        public string BootFile
        {
            get => GetDisplayString(_bootFile, maxFileStringLen);
            set => _bootFile = value;
        }
        string _bootFile;

        public string FirmwareVersion
        {
            get => firmwareDownloader.FirmwareVersion;
        }

        public Command InstallSelected { get; set; }
        public Command ChooseSelected { get; set; }
        public Command DeploySelected { get; set; }

        FirmwareDownloadManager firmwareDownloader;
        Task firmwareTask;

        bool isUpdating = false;

        public FirmwareViewModel()
        {
            firmwareDownloader = new FirmwareDownloadManager();

            firmwareTask = firmwareDownloader.DownloadFirmware();

            InstallSelected = new Command(OnInstall, OnCanInstall);
            ChooseSelected = new Command(OnChoose, OnCanChoose);
            DeploySelected = new Command(OnDeploy, OnCanDeploy);

            InitUI();
        }

        async void InitUI()
        {
            SendConsoleMessage("Checking for firmware updates");

            await firmwareTask;
            
            if(firmwareDownloader.IsNewFirmwareAvailable)
            {
                SendConsoleMessage($"Firmware version {firmwareDownloader.FirmwareVersion} available");
            }
            else
            {
                SendConsoleMessage($"No new firmware available");
            }

            RaiseAllPropertiesChanged();
        }

        string GetDisplayString(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (text.Length <= maxLength)
                return text;
            return "..." + text.Substring(text.Length - maxLength);
        }

        async void OnInstall ()
        {
            var firmwareManager = new FirmwareManager();

            isUpdating = true;
            SendConsoleMessage("Staring firmware update");

            firmwareManager.FirmwareUpdateProgress += (status) => SendConsoleMessage($"Updating firmware {status}%");

            try
            {
                await firmwareManager.EraseAndUploadDevice(0, (byte)Globals.ConnectedDeviceId);
                SendConsoleMessage("Firmware update successful");
            }
            catch (Exception e)
            {
                SendConsoleMessage($"Firmware update failed: {e}");
            }
        }

        bool OnCanInstall ()
        {
            if (isUpdating)
                return false;

            return (Globals.ConnectedDeviceId != -1);
        }

        void OnChoose ()
        {
            //move to dependency service later 
            IOpenFileDialog dialog = new NetduinoDeploy.WPF.OpenFileDialog();
            
            var filter = "Netduino firmware (*.s19)|*.s19|Netduino firmware (*.hex)|*.hex|All files (*.*)|*.*";

            dialog.ShowDialog(filter);
        }

        bool OnCanChoose()
        {
            return true;
        }

        void OnDeploy ()
        {

        }

        bool OnCanDeploy ()
        {
            return false;
        }
    }
}