using NetduinoDeploy.Managers;
using System.Threading.Tasks;

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

        FirmwareDownloadManager firmwareDownloader;
        Task firmwareTask;

        public FirmwareViewModel()
        {
            firmwareDownloader = new FirmwareDownloadManager();

            firmwareTask = firmwareDownloader.DownloadFirmware();

            InitUI();
        }

        async void InitUI()
        {
            await firmwareTask;

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
    }
}