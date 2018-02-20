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

        Task firmwareTask;

        public FirmwareViewModel()
        {
            var firmwareDownloader = new FirmwareDownloadManager();

            firmwareTask = firmwareDownloader.DownloadFirmware();
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
