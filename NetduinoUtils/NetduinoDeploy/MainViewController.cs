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
		string _firmwareStatusUrl = "http://downloads.wildernesslabs.co/firmware_version.json";
		string _configFile = string.Empty;
		string _flashFile = string.Empty;
		string _bootFile = string.Empty;
		Device _selectedDeviceType;
		int maxFileStringLen = 60;
		Regex _macAddressRegex = new Regex("^([0-9A-Fa-f]{2}[:]){5}([0-9A-Fa-f]{2})$");
		NetworkManager _networkManager;
		NetworkConfig _networkConfig;

		private List<ushort> validVendorIDs = new List<ushort>
		{
			0x22B1, // secret labs
			0x1B9F, // ghi
			0x05A, // who knows
			0x0483 // bootloader
		};

		public string ConfigFile
		{
			get
			{
				return _configFile;
			}
			set
			{
				_configFile = value;
				if (_configFile.Length > maxFileStringLen)
				{
					ConfigFileLabel.StringValue = "..." + _configFile.Substring(_configFile.Length - maxFileStringLen);
				}
				else
				{
					ConfigFileLabel.StringValue = value;
				}

			}
		}

		public string FlashFile
		{
			get
			{
				return _flashFile;
			}
			set
			{
				_flashFile = value;
				if (_flashFile.Length > maxFileStringLen)
				{
					FlashFileLabel.StringValue = "..." + _flashFile.Substring(_flashFile.Length - maxFileStringLen);
				}
				else
				{
					FlashFileLabel.StringValue = value;
				}
			}
		}

		public string BootFile
		{
			get
			{
				return _bootFile;
			}
			set
			{
				_bootFile = value;
				if (_bootFile.Length > maxFileStringLen)
				{
					BootFileLabel.StringValue = "..." + _bootFile.Substring(_bootFile.Length - maxFileStringLen);
				}
				else
				{
					BootFileLabel.StringValue = value;
				}
			}
		}

		public ViewController(IntPtr handle) : base(handle)
		{
		}

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

		// FORM STUFFS

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

				if (deviceType.HasMacAddress)
				{
					_networkManager = new NetworkManager();
					LoadAuthenticationTypes();
					LoadEncryptionTypes();
					LoadNetworkKeyTypes();
					LoadNetworkSettings();
				}
			}
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
				_selectedDeviceType = Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.ItemAtIndex(_selectedDeviceIndex).Title);
				DeviceType.SelectItem(_selectedDeviceIndex);
			}
		}

		partial void DeviceTypeChanged(NSObject sender)
		{
			_selectedDeviceIndex = (int)DeviceType.IndexOfSelectedItem;
			_selectedDeviceType = Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.ItemAtIndex(_selectedDeviceIndex).Title);
			LoadForm();
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

		// OTP STUFFS

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

		void MacAddress_Changed(object sender, EventArgs e)
		{
			var result = _macAddressRegex.Match(MacAddress.StringValue);
			SaveConfigurationButton.Enabled = result.Success;
		}

		// FIRMWARE STUFFS

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
					var json = await client.GetStringAsync(_firmwareStatusUrl);
					var firmwareUpdate = JObject.Parse(json);
					firmwareDownloadUrl = firmwareUpdate["url"].ToString();
					firmwareFilename = Path.GetFileName(firmwareDownloadUrl);
					firmwareVersion = firmwareUpdate["version"].ToString();
					break;
				}
				catch (Exception)
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
					catch (Exception)
					{
						OutputToConsole("HTTP connection timeout, retrying...");
						retryCount++;
						await Task.Delay(10000);
					}
				}
			}

			InvokeOnMainThread(() => FirmwareStatus.StringValue = string.Format("Firmware downloaded ({0})", firmwareVersion));
		}

		partial void UpdateFirmwareAction(NSObject sender)
		{
			var productId = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.SelectedItem.Title).ProductID);

			FirmwareManager manager = new FirmwareManager();
			manager.FirmwareUpdateProgress += (string status) =>
			{
				if (status != "100")
				{
					InvokeOnMainThread(() => UpdateFirmwareButton.Title = "Updating... " + status + "%");
				}
			};

			Task.Run(() =>
			{
				InvokeOnMainThread(() =>
				{
					OutputToConsole("Started firmware update");
					UpdateFirmwareButton.Enabled = false;
					NetworkUpdateButton.Enabled = false;
				});

				try
				{
					manager.EraseAndUploadDevice(0, productId);
				}
				catch (Exception e)
				{
					InvokeOnMainThread(() => OutputToConsole("Firmware update failed:\n" + e));
					return;
				}
				finally
				{
					InvokeOnMainThread(() =>
					{
						UpdateFirmwareButton.Enabled = true;
						NetworkUpdateButton.Enabled = true;
						UpdateFirmwareButton.Title = "Update Firmware";
						_networkConfig = new NetworkConfig();
						LoadNetworkSettings(true);
						OutputToConsole("Finished firmware update");
					});
				}
			});
		}

		partial void DeployAction(NSObject sender)
		{
			var productId = Convert.ToByte(Globals.DeviceTypes.SingleOrDefault(x => x.Name == DeviceType.SelectedItem.Title).ProductID);

			FirmwareManager manager = new FirmwareManager();
			manager.FirmwareUpdateProgress += (string status) =>
			{
				InvokeOnMainThread(() => { DeployButton.Title = "Deploying... " + status + "%"; });
			};

			Task.Run(() =>
			{
				if (string.IsNullOrEmpty(_configFile) || string.IsNullOrEmpty(_flashFile) || string.IsNullOrEmpty(_bootFile))
				{
					return;
				}

				InvokeOnMainThread(() =>
				{
					DeployButton.Enabled = false;
					UpdateFirmwareButton.Enabled = false;
					NetworkUpdateButton.Enabled = false;
					OutputToConsole("Started deploy");
				});

				try
				{
					manager.EraseAndUploadDevice(0, productId, _configFile, _flashFile, _bootFile);
				}
				catch (Exception e)
				{
					InvokeOnMainThread(() => OutputToConsole("Firmware update failed:\n" + e));
				}
				finally
				{
					InvokeOnMainThread(() =>
					{
						_networkConfig = new NetworkConfig();
						LoadNetworkSettings(true);

						DeployButton.Title = "Deploy";
						DeployButton.Enabled = true;

						NetworkUpdateButton.Enabled = true;
						UpdateFirmwareButton.Enabled = true;

						BootFile = string.Empty;
						ConfigFile = string.Empty;
						FlashFile = string.Empty;

						OutputToConsole("Finished deploy");
					});
				}
			});
		}

		partial void SelectFolderAction(NSObject sender)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.CanChooseDirectories = false;
			dlg.AllowedFileTypes = new string[] { "hex", "s19" };
			dlg.AllowsMultipleSelection = true;

			if (dlg.RunModal() == 1)
			{
				if (dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_config"))?.Path != null)
				{
					ConfigFile = dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_config"))?.Path;
				}

				if (dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_flash"))?.Path != null)
				{
					FlashFile = dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("er_flash"))?.Path;
				}

				if (dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("tinybooter"))?.Path != null)
				{
					BootFile = dlg.Urls.SingleOrDefault(x => x.Path.ToLower().Contains("tinybooter"))?.Path;
				}

				DeployButton.Enabled = (!string.IsNullOrEmpty(ConfigFile) && !string.IsNullOrEmpty(FlashFile) && !string.IsNullOrEmpty(BootFile));
			}
		}

		// NETWORK STUFFS

		private void LoadNetworkSettings(bool skipReadFromDevice = false)
		{
			if (!skipReadFromDevice)
			{
				ReadNetworkSettingsFromDevice();
			}

			if (_networkConfig.NetworkMacAddress == null || _networkConfig.NetworkMacAddress.Length == 0)
			{
				OtpManager otpManager = new OtpManager();
				var settings = otpManager.GetOtpSettings();

				_networkConfig = new NetworkConfig();
				_networkConfig.NetworkMacAddress = settings.MacAddress;
				_networkConfig.IsWireless = _selectedDeviceType.IsWirelessCapable;
				SaveNetworkSettings();
				ReadNetworkSettingsFromDevice();
			}

			EnableManualIpSettings(!_networkConfig.EnableDHCP);
			EnableDHCP.State = _networkConfig.EnableDHCP ? NSCellStateValue.On : NSCellStateValue.Off;
			StaticIPAddress.StringValue = _networkConfig.StaticIPAddress.ToString();
			SubnetMask.StringValue = _networkConfig.SubnetMask.ToString();
			DefaultGateway.StringValue = _networkConfig.DefaultGateway.ToString();
			NetworkMacAddress.StringValue = BitConverter.ToString(_networkConfig.NetworkMacAddress).Replace("-", ":");
			PrimaryDNS.StringValue = _networkConfig.PrimaryDNS.ToString();
			SecondaryDNS.StringValue = _networkConfig.SecondaryDNS.ToString();

			EnableWifiSettings();

			if (_networkConfig.IsWireless)
			{
				Authentication.SelectItem(_networkConfig.Authentication);
				Encryption.SelectItem(_networkConfig.Encryption);

				RadioA.State = (_networkConfig.Radio & (int)MFWirelessConfiguration.RadioTypes.a) != 0 ? NSCellStateValue.On : NSCellStateValue.Off;
				RadioB.State = (_networkConfig.Radio & (int)MFWirelessConfiguration.RadioTypes.b) != 0 ? NSCellStateValue.On : NSCellStateValue.Off;
				RadioG.State = (_networkConfig.Radio & (int)MFWirelessConfiguration.RadioTypes.g) != 0 ? NSCellStateValue.On : NSCellStateValue.Off;
				RadioN.State = (_networkConfig.Radio & (int)MFWirelessConfiguration.RadioTypes.n) != 0 ? NSCellStateValue.On : NSCellStateValue.Off;

				Passphrase.StringValue = _networkConfig.Passphrase;
				EncryptConfig.State = _networkConfig.EncryptConfig ? NSCellStateValue.On : NSCellStateValue.Off;
				int index = (int)Math.Log(_networkConfig.NetworkKeyLength / 8.0, 2);
				NetworkKey.SelectItem(index < NetworkKey.ItemCount && index > 0 ? index : NetworkKey.ItemCount - 1);
				NetworkValue.StringValue = _networkConfig.NetworkKey;
				ReKeyInternal.StringValue = _networkConfig.ReKeyInternal;
				SSID.StringValue = _networkConfig.SSID;
			}
		}

		partial void NetworkUpdate(NSObject sender)
		{
			ReadNetworkSettingsFromForm();
			SaveNetworkSettings();
		}

		private void SaveNetworkSettings(bool logToConsole = true)
		{
			MFNetworkConfiguration mfNetConfig = new MFNetworkConfiguration();
			mfNetConfig.Load(_networkManager);

			mfNetConfig.IpAddress = _networkConfig.StaticIPAddress;
			mfNetConfig.SubNetMask = _networkConfig.SubnetMask;
			mfNetConfig.PrimaryDns = _networkConfig.PrimaryDNS;
			mfNetConfig.SecondaryDns = _networkConfig.SecondaryDNS;
			mfNetConfig.Gateway = _networkConfig.DefaultGateway;
			mfNetConfig.MacAddress = _networkConfig.NetworkMacAddress;
			mfNetConfig.EnableDhcp = _networkConfig.EnableDHCP;
			mfNetConfig.ConfigurationType = _networkConfig.IsWireless ? MFNetworkConfiguration.NetworkConfigType.Wireless : MFNetworkConfiguration.NetworkConfigType.Generic;

			if (_networkConfig.IsWireless)
			{
				MFWirelessConfiguration mfWifiConfig = new MFWirelessConfiguration();
				mfWifiConfig.Load(_networkManager);

				mfWifiConfig.Authentication = _networkConfig.Authentication;
				mfWifiConfig.Encryption = _networkConfig.Encryption;
				mfWifiConfig.Radio = _networkConfig.Radio;
				mfWifiConfig.PassPhrase = _networkConfig.Passphrase;
				mfWifiConfig.UseEncryption = _networkConfig.EncryptConfig;
				mfWifiConfig.NetworkKeyLength = _networkConfig.NetworkKeyLength;
				mfWifiConfig.NetworkKey = _networkConfig.NetworkKey;
				mfWifiConfig.ReKeyLength = _networkConfig.ReKeyInternal.Length / 2;
				mfWifiConfig.ReKeyInternal = _networkConfig.ReKeyInternal;
				mfWifiConfig.SSID = _networkConfig.SSID;

				mfWifiConfig.Save(_networkManager);
			}
			mfNetConfig.Save(_networkManager);

			if (logToConsole)
			{
				OutputToConsole("Network settings saved.");
			}
		}

		private void ReadNetworkSettingsFromDevice()
		{
			MFNetworkConfiguration mfNetConfig = new MFNetworkConfiguration();
			mfNetConfig.Load(_networkManager);

			_networkConfig = new NetworkConfig();

			_networkConfig.EnableDHCP = mfNetConfig.EnableDhcp;
			_networkConfig.StaticIPAddress = mfNetConfig.IpAddress;
			_networkConfig.SubnetMask = mfNetConfig.SubNetMask;
			_networkConfig.DefaultGateway = mfNetConfig.Gateway;
			_networkConfig.NetworkMacAddress = mfNetConfig.MacAddress;
			_networkConfig.PrimaryDNS = mfNetConfig.PrimaryDns;
			_networkConfig.SecondaryDNS = mfNetConfig.SecondaryDns;

			if (mfNetConfig.ConfigurationType == MFNetworkConfiguration.NetworkConfigType.Wireless)
			{
				MFWirelessConfiguration mfWifiConfig = new MFWirelessConfiguration();
				mfWifiConfig.Load(_networkManager);

				_networkConfig.IsWireless = true;
				_networkConfig.Authentication = mfWifiConfig.Authentication;
				_networkConfig.Encryption = mfWifiConfig.Encryption;
				_networkConfig.Radio = mfWifiConfig.Radio;
				_networkConfig.Passphrase = mfWifiConfig.PassPhrase;
				_networkConfig.EncryptConfig = mfWifiConfig.UseEncryption;
				_networkConfig.NetworkKey = mfWifiConfig.NetworkKey;
				_networkConfig.NetworkKeyLength = mfWifiConfig.NetworkKeyLength;
				_networkConfig.ReKeyInternal = mfWifiConfig.ReKeyInternal;
				_networkConfig.SSID = mfWifiConfig.SSID;
			}
		}

		private void ReadNetworkSettingsFromForm()
		{
			_networkConfig = new NetworkConfig();

			_networkConfig.IsWireless = _selectedDeviceType.IsWirelessCapable;
			_networkConfig.StaticIPAddress = _networkConfig.ParseAddress(StaticIPAddress.StringValue);
			_networkConfig.SubnetMask = _networkConfig.ParseAddress(SubnetMask.StringValue);
			_networkConfig.PrimaryDNS = _networkConfig.ParseAddress(PrimaryDNS.StringValue);
			_networkConfig.SecondaryDNS = _networkConfig.ParseAddress(SecondaryDNS.StringValue);
			_networkConfig.DefaultGateway = _networkConfig.ParseAddress(DefaultGateway.StringValue);
			_networkConfig.NetworkMacAddress = NetworkMacAddress.StringValue.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
			_networkConfig.EnableDHCP = EnableDHCP.State == NSCellStateValue.On;

			if (_networkConfig.IsWireless)
			{
				_networkConfig.Authentication = (int)Authentication.IndexOfSelectedItem;
				_networkConfig.Encryption = (int)Encryption.IndexOfSelectedItem;
				_networkConfig.Radio = (RadioA.State == NSCellStateValue.On ? (int)MFWirelessConfiguration.RadioTypes.a : 0) |
									  (RadioB.State == NSCellStateValue.On ? (int)MFWirelessConfiguration.RadioTypes.b : 0) |
									  (RadioG.State == NSCellStateValue.On ? (int)MFWirelessConfiguration.RadioTypes.g : 0) |
									  (RadioN.State == NSCellStateValue.On ? (int)MFWirelessConfiguration.RadioTypes.n : 0);
				_networkConfig.Passphrase = Passphrase.StringValue;
				_networkConfig.EncryptConfig = EncryptConfig.State == NSCellStateValue.On;
				_networkConfig.NetworkKeyLength = (int)(Math.Pow(2.0, NetworkKey.IndexOfSelectedItem) * 8);
				_networkConfig.NetworkKey = NetworkValue.StringValue;
				_networkConfig.ReKeyInternal = ReKeyInternal.StringValue;
				_networkConfig.SSID = SSID.StringValue;
			}
		}

		private void EnableWifiSettings()
		{
			Authentication.Enabled = _networkConfig.IsWireless;
			Encryption.Enabled = _networkConfig.IsWireless;
			RadioA.Enabled = _networkConfig.IsWireless;
			RadioB.Enabled = _networkConfig.IsWireless;
			RadioG.Enabled = _networkConfig.IsWireless;
			RadioN.Enabled = _networkConfig.IsWireless;
			EncryptConfig.Enabled = _networkConfig.IsWireless;
			Passphrase.Enabled = _networkConfig.IsWireless;
			NetworkKey.Enabled = _networkConfig.IsWireless;
			NetworkValue.Enabled = _networkConfig.IsWireless;
			ReKeyInternal.Enabled = _networkConfig.IsWireless;
			SSID.Enabled = _networkConfig.IsWireless;
		}

		partial void EnableDHCPChanged(NSObject sender)
		{
			EnableManualIpSettings(EnableDHCP.State == NSCellStateValue.Off);
		}

		private void EnableManualIpSettings(bool enableManualIpSettings)
		{
			StaticIPAddress.Enabled = enableManualIpSettings;
			SubnetMask.Enabled = enableManualIpSettings;
			DefaultGateway.Enabled = enableManualIpSettings;
		}

		private void LoadAuthenticationTypes()
		{
			Authentication.RemoveAllItems();
			Authentication.AddItem("None");
			Authentication.AddItem("EAP");
			Authentication.AddItem("PEAP");
			Authentication.AddItem("WCN");
			Authentication.AddItem("Open");
			Authentication.AddItem("Shared");
		}

		public void LoadEncryptionTypes()
		{
			Encryption.RemoveAllItems();
			Encryption.AddItem("None");
			Encryption.AddItem("WEP");
			Encryption.AddItem("WPA");
			Encryption.AddItem("WPAPSK");
			Encryption.AddItem("Certificate");
		}

		public void LoadNetworkKeyTypes()
		{
			NetworkKey.RemoveAllItems();
			NetworkKey.AddItem("64-bit");
			NetworkKey.AddItem("128-bit");
			NetworkKey.AddItem("256-bit");
			NetworkKey.AddItem("512-bit");
			NetworkKey.AddItem("1024-bit");
			NetworkKey.AddItem("2048-bit");
		}
	}
}
