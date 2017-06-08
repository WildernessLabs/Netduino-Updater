using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

			DeviceType.RemoveAllItems();

			var deviceCount = DfuContext.Current.GetDevices().Count;

			var productId = 0;
			if (deviceCount == 1)
			{
				OtpManager otpManager = new OtpManager();
				var settings = otpManager.GetOtpSettings();
				productId = settings.ProductID;
                LoadDeviceList(productId);
			}

			LoadForm();
			await DownloadFirmware();
		}

		partial void SaveConfiguration(NSObject sender)
		{
			OtpSettings settings = new OtpSettings();
			settings.ProductID = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.SelectedItem.Title).ProductID);
			settings.MacAddress = MacAddress.StringValue.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
			OtpManager manager = new OtpManager();
			manager.SaveOtpSettings(settings);
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
			//manager.FirmwareUpdateProgress += (string status) =>
			//{
			//	InvokeOnMainThread(() => UpdateFirmwareButton.Title = "Updating... " + status + "%");
			//};

			//var productId = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.SelectedItem.Title).ProductID);

			//Task.Run(() =>
			//{
			//	InvokeOnMainThread(() => UpdateFirmwareButton.Enabled = false);
			//	manager.EraseAndUploadDevice(0, productId);
			//	InvokeOnMainThread(() => UpdateFirmwareButton.Title = "Update Firmware");
			//	InvokeOnMainThread(() => UpdateFirmwareButton.Enabled = true);
			//});
			UpdateFirmwareButton.Enabled = false;
			manager.EraseAndUploadDevice(0, productId);
			UpdateFirmwareButton.Enabled = true;
		}

		private void LoadDeviceList(int productId = 0)
		{
			DeviceLabel.StringValue = string.Empty;

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
				DeviceLabel.StringValue = "Please connect a device in bootloader mode";

			}
			else if (deviceCount > 1)
			{
				DeviceLabel.StringValue = "Please connect only one device in bootloader mode.";
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

					SaveConfigurationButton.Enabled = true;
					MacAddress.Enabled = true;

					if (settings.ProductID > 0)
					{
						UpdateFirmwareButton.Enabled = true;
					}

				}

			}
		}

		partial void SelectFolderAction(NSObject sender)
		{

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

            InvokeOnMainThread(() => FirmwareStatus.StringValue = "Checking for firmware updates" );
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
					//RaiseFirmwareVersionChecked(firmwareVersion);
					break;
				}
				catch (Exception ex)
				{
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
                //RaiseFirmwareVersionChecked(firmwareVersion);
			}
			else
			{
                InvokeOnMainThread(() => FirmwareStatus.StringValue = "Downloading latest firmware" );
				// download firmware update
				while (true)
				{
					retryCount = 0;
					try
					{
						WebClient webClient = new WebClient();
						await webClient.DownloadFileTaskAsync(new Uri(firmwareDownloadUrl), Path.Combine(workingPath, firmwareFilename));
						Debug.WriteLine("downloaded");

						using (ZipFile zip = ZipFile.Read(Path.Combine(workingPath, firmwareFilename)))
						{
							zip.ExtractAll(workingPath);
						}

						//RaiseFirmwareVersionChecked(firmwareVersion);
						break;
					}
					catch (Exception ex)
					{
						Debug.WriteLine("retrying download" + retryCount);
						retryCount++;
						await Task.Delay(10000);
					}
				}
			}

			InvokeOnMainThread(() => FirmwareStatus.StringValue = string.Format("Latest firmware downloaded ({0})", firmwareVersion) );
		}
	}
}
