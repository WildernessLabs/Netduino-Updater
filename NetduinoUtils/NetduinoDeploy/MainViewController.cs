using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppKit;
using DfuSharp;
using Foundation;
using Ionic.Zip;
using NetduinoDeploy.Managers;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace NetduinoDeploy
{
	public partial class ViewController : NSViewController
	{
		int _selectedDeviceIndex = 0;
		string firmwareStatusUrl = "http://www.netduino.com/firmware_version.json";
		string configFile = string.Empty;
		string flashFile = string.Empty;
		string bootFile = string.Empty;

		Regex macAddressRegex = new Regex("^([0-9A-Fa-f]{2}[:]){5}([0-9A-Fa-f]{2})$");

		public ViewController(IntPtr handle) : base(handle)
		{
		}

		private List<ushort> validVendorIDs = new List<ushort>
		{
			0x22B1, // secret labs
			0x1B9F, // ghi
			0x05A, // who knows
			0x0483 // bootloader
		};

		async public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			var deviceCount = DfuContext.Current.GetDevices().Count;
			DeployButton.Enabled = false;

			var productId = 0;
			if (deviceCount == 1)
			{
				OtpManager otpManager = new OtpManager();
				var settings = otpManager.GetOtpSettings();
				productId = settings.ProductID;
				LoadDeviceList(productId);
			}
			else
			{
				LoadDeviceList();
			}

			LoadForm();
			MacAddress.Changed += MacAddress_Changed;
			await DownloadFirmware();
		}

		partial void SaveConfiguration(NSObject sender)
		{
			OtpSettings settings = new OtpSettings();
			settings.ProductID = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.SelectedItem.Title).ProductID);
			settings.MacAddress = MacAddress.StringValue.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
			OtpManager manager = new OtpManager();
			manager.SaveOtpSettings(settings);
			OutputToConsole("Configuration saved successfully");
			LoadForm();
		}

		partial void DeviceTypeChanged(NSObject sender)
		{
			_selectedDeviceIndex = (int)DeviceType.IndexOfSelectedItem;
			LoadForm();
		}

		partial void UpdateFirmwareAction(NSObject sender)
		{
			var productId = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.SelectedItem.Title).ProductID);

			FirmwareManager manager = new FirmwareManager();
			manager.FirmwareUpdateProgress += (string status) =>
			{
				InvokeOnMainThread(() => UpdateFirmwareButton.Title = "Updating... " + status + "%");
			};

			Task.Run(() =>
			{
                InvokeOnMainThread(() => OutputToConsole("Started firmware update"));
				InvokeOnMainThread(() => UpdateFirmwareButton.Enabled = false);
				try {
					manager.EraseAndUploadDevice(0, productId);
				} catch (Exception e) {
					InvokeOnMainThread(() => OutputToConsole("Firmware update failed:\n" + e));
					return;
				} finally {
					InvokeOnMainThread(() => UpdateFirmwareButton.Title = "Update Firmware");
					InvokeOnMainThread(() => UpdateFirmwareButton.Enabled = true);
				}
				InvokeOnMainThread(() => OutputToConsole("Finished firmware update"));
			});
		}

		private void LoadDeviceList(int productId = 0)
		{
			DeviceType.RemoveAllItems();
			DeviceType.AddItem("[Select Device Type]");

			foreach (var device in Globals.DeviceTypes)
			{
				DeviceType.AddItem(device.Name);
			}

			if (productId > 0)
			{
				string productName = Globals.DeviceTypes.Single(x => x.ProductID == productId).Name;
				_selectedDeviceIndex = (int)DeviceType.IndexOfItem(productName);
				DeviceType.SelectItem(_selectedDeviceIndex);
			}

		}

		private void LoadForm()
		{

			var deviceCount = DfuContext.Current.GetDevices().Count;

			MacAddress.Enabled = false;
			SaveConfigurationButton.Enabled = false;
			UpdateFirmwareButton.Enabled = false;

			if (deviceCount == 0)
			{
				OutputToConsole("Please connect a device in bootloader mode and restart the application");
			}
			else if (deviceCount > 1)
			{
				OutputToConsole("Please connect only one device in bootloader mode and restart the application");
			}
			else if (_selectedDeviceIndex == 0)
			{
				DeviceType.Enabled = true;
			}
			else
			{
				DeviceType.Enabled = true;

				var deviceType = Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.ItemAtIndex(_selectedDeviceIndex).Title);

				if (deviceType != null)
				{
					OtpManager otpManager = new OtpManager();
					var settings = otpManager.GetOtpSettings();
					MacAddress.StringValue = BitConverter.ToString(settings.MacAddress).Replace('-', ':');
					FreeSlots.StringValue = string.Format("Device settings can be saved {0} more time{1}", settings.FreeSlots, settings.FreeSlots > 1 ? "s" : "");

					SaveConfigurationButton.Enabled = settings.FreeSlots > 0;

					MacAddress.Enabled = deviceType.HasMacAddress;

					if (settings.ProductID > 0)
					{
						UpdateFirmwareButton.Enabled = true;
					}
				}
			}
		}


		void MacAddress_Changed(object sender, EventArgs e)
		{
			var result = macAddressRegex.Match(MacAddress.StringValue);
			SaveConfigurationButton.Enabled = result.Success;
		}

		partial void SelectFolderAction(NSObject sender)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.CanChooseDirectories = false;
			dlg.AllowedFileTypes = new string[] { "hex", "s19" };
			dlg.AllowsMultipleSelection = true;

			if (dlg.RunModal () == 1) {

				if (dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_config"))?.Path != null)
				{
					configFile = dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_config"))?.Path;
				}

				if (dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_flash"))?.Path != null)
				{
					flashFile = dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_flash"))?.Path;
				}

				if (dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("tinybooter"))?.Path != null)
				{
					bootFile = dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("tinybooter"))?.Path;
				}

				ConfigFileLabel.StringValue = configFile ?? string.Empty;
				FlashFileLabel.StringValue = flashFile ?? string.Empty;
				BootFileLabel.StringValue = bootFile ?? string.Empty;

				int maxLen = 45;
				if (ConfigFileLabel.StringValue.Length > maxLen)
				{
					ConfigFileLabel.StringValue = "..." + configFile.Substring(configFile.Length - maxLen);
				}

				if (FlashFileLabel.StringValue.Length > maxLen)
				{
					FlashFileLabel.StringValue = "..." + flashFile.Substring(flashFile.Length - maxLen);
				}

				if (BootFileLabel.StringValue.Length > maxLen)
				{
					BootFileLabel.StringValue = "..." + bootFile.Substring(flashFile.Length - maxLen);
				}

				DeployButton.Enabled = (!string.IsNullOrEmpty(configFile) && !string.IsNullOrEmpty(flashFile) && !string.IsNullOrEmpty(bootFile));
			}
		}

		public override NSObject RepresentedObject
		{
			get
			{
				return base.RepresentedObject;
			}
			set
			{
				base.RepresentedObject = value;
				// Update the view, if already loaded.
			}
		}

		async private Task DownloadFirmware()
		{
			HttpClient client = new HttpClient();
			int retryCount = 0;
			string firmwareDownloadUrl = string.Empty;
			string firmwareVersion = string.Empty;
			string firmwareFilename = string.Empty;

            OutputToConsole("Checking for firmware updates");
			// check for firmware update
			while (true)
			{
				try
				{
					var json = await client.GetStringAsync(firmwareStatusUrl);
					var firmwareUpdate = JObject.Parse(json);
					firmwareDownloadUrl = firmwareUpdate["url"].ToString();
					firmwareFilename = Path.GetFileName(firmwareDownloadUrl);
					firmwareVersion = firmwareUpdate["version"].ToString();
					break;
				}
				catch (Exception ex)
				{
                    OutputToConsole("HTTP connection timeout, retrying...");
					retryCount++;
					await Task.Delay(10000);
				}
			}

			string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string workingPath = Path.Combine(appPath, "Netduino");
			if (!Directory.Exists(workingPath))
			{
				Directory.CreateDirectory(workingPath);
			}

			if (File.Exists(Path.Combine(workingPath, firmwareFilename)))
			{
                OutputToConsole("No firmware updates found");
  			}
			else
			{
				OutputToConsole("Started firmware download");
				// download firmware update
				while (true)
				{
					retryCount = 0;
					try
					{
						WebClient webClient = new WebClient();
						await webClient.DownloadFileTaskAsync(new Uri(firmwareDownloadUrl), Path.Combine(workingPath, firmwareFilename));
						OutputToConsole("Finished firmware download");
						using (ZipFile zip = ZipFile.Read(Path.Combine(workingPath, firmwareFilename)))
						{
							zip.ExtractAll(workingPath);
						}

						break;
					}
					catch (Exception ex)
					{
                        OutputToConsole("HTTP connection timeout, retrying...");
						retryCount++;
						await Task.Delay(10000);
					}
				}
			}

			InvokeOnMainThread(() => FirmwareStatus.StringValue = string.Format("Firmware downloaded ({0})", firmwareVersion) );
		}

		private void OutputToConsole(string message)
		{
			string dateFormat = "MMM d h:mm:ss tt";
			if (string.IsNullOrEmpty(Output.Value))
			{
				Output.Value = DateTime.Now.ToString(dateFormat) + " - " + message;
			}
			else
			{
				Output.Value = Output.Value + System.Environment.NewLine + DateTime.Now.ToString(dateFormat) + " - " + message;
			}

		}

		partial void DeployAction(NSObject sender)
		{
			var productId = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.SelectedItem.Title).ProductID);

			FirmwareManager manager = new FirmwareManager();
			manager.FirmwareUpdateProgress += (string status) =>
			{
				InvokeOnMainThread(() => DeployButton.Title = "Deploying... " + status + "%");
			};

			Task.Run(() =>
			{
				InvokeOnMainThread(() => DeployButton.Enabled = false);
                InvokeOnMainThread(() => OutputToConsole("Started deploy"));

				if (!string.IsNullOrEmpty(configFile) && !string.IsNullOrEmpty(flashFile))
				{
					manager.EraseAndUploadDevice(0, productId, configFile, flashFile, bootFile);
				}

				InvokeOnMainThread(() => DeployButton.Title = "Deploy");
				InvokeOnMainThread(() => DeployButton.Enabled = true);
				InvokeOnMainThread(() => OutputToConsole("Finished deploy"));

			});
		}
	}
}
