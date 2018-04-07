using NetduinoDeploy.Managers;
using System;
using System.Linq;
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
            set
            {
                _configFile = value;
                OnPropertyChanged();
            }
                
        }
        string _configFile;

        public string FlashFile
        {
            get => GetDisplayString(_flashFile, maxFileStringLen);
            set
            {
                _flashFile = value;
                OnPropertyChanged();
            }
        }
        string _flashFile;

        public string BootFile
        {
            get => GetDisplayString(_bootFile, maxFileStringLen);
            set
            {
                _bootFile = value;
                OnPropertyChanged();
            }
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
            App.SendConsoleMessage("Checking for firmware updates");

            await firmwareTask;
            
            if(firmwareDownloader.IsNewFirmwareAvailable)
            {
                App.SendConsoleMessage($"Firmware version {firmwareDownloader.FirmwareVersion} available");
            }
            else
            {
                App.SendConsoleMessage($"No new firmware available");
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
            App.SendConsoleMessage("Staring firmware update");

            firmwareManager.FirmwareUpdateProgress += (status) => App.SendConsoleMessage($"Updating firmware: {status}");

            try
            {
                await Task.Run(()=> firmwareManager.EraseAndUploadDevice(0, (byte)Globals.ConnectedDeviceId) );

                App.SendConsoleMessage("Firmware update successful");
            }
            catch (Exception e)
            {
                App.SendConsoleMessage($"Firmware update failed: {e}");
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
            IOpenFileDialog dialog = GetFileDialog();

            var filter = "Netduino firmware (*.s19)|*.s19|Netduino firmware (*.hex)|*.hex";

            ConfigFile = FlashFile = BootFile = string.Empty;

            if (dialog.ShowDialog(filter))
            {
                if (dialog.FileNames.SingleOrDefault(x => x.ToLower().Contains("er_config")) != null)
                    ConfigFile = dialog.FileNames.SingleOrDefault(x => x.ToLower().Contains("er_config"));

                if (dialog.FileNames.SingleOrDefault(x => x.ToLower().Contains("er_flash")) != null)
                    FlashFile = dialog.FileNames.SingleOrDefault(x => x.ToLower().Contains("er_flash"));

                if (dialog.FileNames.SingleOrDefault(x => x.ToLower().Contains("tinybooter")) != null)
                    BootFile = dialog.FileNames.SingleOrDefault(x => x.ToLower().Contains("tinybooter"));
            }

            DeploySelected.ChangeCanExecute();
        }

        IOpenFileDialog GetFileDialog()
        {
#if __WPF__
            return new NetduinoDeploy.WPF.OpenFileDialog();
#elif __MACOS__
            return new NetduinoDeploy.macOS.OpenFileDialog();
#else
            return null;
#endif
        }

        bool OnCanChoose()
        {
            return true;
        }

        async void OnDeploy ()
        {
            var firmwareManager = new FirmwareManager();

            isUpdating = true;
            App.SendConsoleMessage("Staring firmware update");

            firmwareManager.FirmwareUpdateProgress += (status) => App.SendConsoleMessage($"Updating firmware {status}%");

            try
            {
                await firmwareManager.EraseAndUploadDevice(0, (byte)Globals.ConnectedDeviceId, _configFile, _flashFile, _bootFile);
                App.SendConsoleMessage("Firmware update successful");
            }
            catch (Exception e)
            {
                App.SendConsoleMessage($"Firmware update failed: {e}");
            }
        }

        bool OnCanDeploy ()
        {
            if (!string.IsNullOrEmpty(ConfigFile) &&
                !string.IsNullOrEmpty(FlashFile) &&
                !string.IsNullOrEmpty(BootFile))
                return true;

            return false;
        }
    }
}