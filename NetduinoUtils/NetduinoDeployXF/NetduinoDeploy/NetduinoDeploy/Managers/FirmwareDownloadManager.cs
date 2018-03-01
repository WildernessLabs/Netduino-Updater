using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using System.Net;
using Ionic.Zip;

namespace NetduinoDeploy.Managers
{
    public class FirmwareDownloadManager
    {
        string firmwareStatusUrl = "http://downloads.wildernesslabs.co/firmware_version.json";

        public string FirmwareVersion { get; private set; }
        public string FirmwareFilename { get; private set; }

        public string FirmwareDownloadUrl { get; private set; }

        public async Task DownloadFirmware()
        {
            var client = new HttpClient();

            int retryCount = 0;

            Debug.WriteLine("Checking for firmware updates");

            // check for firmware update
            while (true)
            {
                try
                {
                    var json = await client.GetStringAsync(firmwareStatusUrl);

                    var firmwareUpdate = JObject.Parse(json);

                    FirmwareDownloadUrl = firmwareUpdate["url"].ToString();
                    FirmwareFilename = Path.GetFileName(FirmwareDownloadUrl);
                    FirmwareVersion = firmwareUpdate["version"].ToString();

                    break;
                }
                catch (Exception)
                {
                    Debug.WriteLine("HTTP connection timeout, retrying...");
                    retryCount++;
                    await Task.Delay(10000);
                }
            }

            var appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var workingPath = Path.Combine(appPath, "Netduino");

            if (!Directory.Exists(workingPath))
            {
                Directory.CreateDirectory(workingPath);
            }

            if (File.Exists(Path.Combine(workingPath, FirmwareFilename)))
            {
                Debug.WriteLine("No firmware updates found");
            }
            else
            {
                Debug.WriteLine("Started firmware download");
                // download firmware update
                while (true)
                {
                    retryCount = 0;
                    try
                    {
                        var webClient = new WebClient();

                        await webClient.DownloadFileTaskAsync(new Uri(FirmwareDownloadUrl), Path.Combine(workingPath, FirmwareFilename));

                        Debug.WriteLine("Finished firmware download");

                        using (var zip = ZipFile.Read(Path.Combine(workingPath, FirmwareFilename)))
                        {
                            zip.ExtractAll(workingPath);
                        }

                        break;
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("HTTP connection timeout, retrying...");
                        retryCount++;
                        await Task.Delay(10000);
                    }
                }
            }
         //   InvokeOnMainThread(() => FirmwareStatus.StringValue = string.Format("Firmware downloaded ({0})", firmwareVersion));
        }
    }
}
