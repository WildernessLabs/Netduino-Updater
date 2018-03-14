using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DfuSharp;

namespace NetduinoDeploy.Managers
{
	public class FirmwareManager
	{
		public FirmwareManager()
		{ }

		private double CurrentProgress
		{
			get => Math.Round((eraseProgress + uploadProgress) / 2, 0);
		}

		private double eraseProgress = 0;
		private double uploadProgress = 0;
		private uint bootloaderBaseAddress = 0x08000000;

		private List<Firmware> LoadFirmwareFiles()
		{
			List<Firmware> results = new List<Firmware>();

			// find all folders containing firmware
			string appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string firmwareRootFolder = Path.Combine(appPath, "Netduino", "Firmware");
			List<string> firmwareFolders = Directory.GetDirectories(firmwareRootFolder, "*.*", SearchOption.AllDirectories).ToList<string>();
			firmwareFolders.Add(firmwareRootFolder); // be sure to search the root firmware folder, in case the user only has one firmware option.

			// enumerate the firmware in each folder
			foreach (string firmwareFolder in firmwareFolders)
			{
				string[] firmwareFiles = Directory.GetFiles(firmwareFolder, "*.xml");
				foreach (string firmwareFile in firmwareFiles)
				{
					try
					{
						Firmware firmware = new Firmware(firmwareFile);
						results.Add(firmware);
					}
					catch
					{
					}
				}
			}

			return results;
		}

		public Task EraseAndUploadDevice(int deviceIndex, int productID, string configPath, string flashPath, string bootFile)
		{
			int uploadedByteCount = 0;
			int totalBytes = 0;


			var devices = DfuContext.Current.GetDevices();

			if (devices.Count == 0)
			{
				throw new Exception("Device not found");
			}

			DfuDevice device = devices[deviceIndex];
			device.ClaimInterface();
			device.SetInterfaceAltSetting(0);
			device.Clear();

			var deviceConfig = Globals.DeviceTypes.SingleOrDefault(x => x.ProductID == productID);

			int sectorCount = 0;
			foreach (var sector in deviceConfig.Sectors)
			{
				device.EraseSector((int)sector);
				eraseProgress = (sectorCount + 1) * 100 / deviceConfig.Sectors.Count();
				Debug.WriteLine(CurrentProgress.ToString());
                RaiseFirmwareUpdateProgress(CurrentProgress.ToString());
				sectorCount++;
			}

			// get bytes for progress

			using (System.IO.StreamReader streamReader = new System.IO.StreamReader(configPath))
			{
				string hexFileString = streamReader.ReadToEnd();
				byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, deviceConfig.ConfigBaseAddress);
				totalBytes += hexFileBytes.Length;
			}

			using (System.IO.StreamReader streamReader = new System.IO.StreamReader(flashPath))
			{
				string hexFileString = streamReader.ReadToEnd();
				byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, deviceConfig.FlashBaseAddress);
				totalBytes += hexFileBytes.Length;
			}

			// load tinybooter

			using (System.IO.StreamReader streamReader = new System.IO.StreamReader(bootFile))
			{
				string hexFileString = streamReader.ReadToEnd();
				byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, bootloaderBaseAddress);
				device.Upload(hexFileBytes, (int)bootloaderBaseAddress);
			}

			device.Uploading += (sender, e) =>
			{
				uploadedByteCount += e.BytesUploaded;
				uploadProgress = uploadedByteCount * 100 / totalBytes;
				Debug.WriteLine(CurrentProgress.ToString());
                RaiseFirmwareUpdateProgress(CurrentProgress.ToString());
			};

			// load config
			using (System.IO.StreamReader streamReader = new System.IO.StreamReader(configPath))
			{
				string hexFileString = streamReader.ReadToEnd();
				byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, deviceConfig.ConfigBaseAddress);
				device.Upload(hexFileBytes, (int)deviceConfig.ConfigBaseAddress);
			}

			// load flash
			using (System.IO.StreamReader streamReader = new System.IO.StreamReader(flashPath))
			{
				string hexFileString = streamReader.ReadToEnd();
				byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, deviceConfig.FlashBaseAddress);
				device.Upload(hexFileBytes, (int)deviceConfig.FlashBaseAddress);
			}

			//// step 4: restart board
			device.SetAddress(0x08000001); // NOTE: for thumb2 instructinos, we added 1 to the "base address".  Otherwise our board will not restart properly.
			RaiseFirmwareUpdateProgress("Update Complete");

			//// leave DFU mode.
			////device.LeaveDfuMode();
			return Task.CompletedTask;
		}

		public Task EraseAndUploadDevice(int deviceIndex, byte productID)
		{
			int uploadedByteCount = 0;
			int totalBytes = 0;

			List<Firmware> firmwares = LoadFirmwareFiles();
			Firmware firmware = firmwares.SingleOrDefault(x => x.ProductID == productID);


			var devices = DfuContext.Current.GetDevices();

			if (devices.Count == 0)
			{
				throw new Exception("Device not found");
			}

			DfuDevice device = devices[deviceIndex];
			device.ClaimInterface();
			device.SetInterfaceAltSetting(0);
			device.Clear();

			// TODO: make sure we are in DFU mode; if we are in app mode (runtime) then we need to detach and re-enumerate.

			// get our total sectors and block counts
			List<uint> allSectorBaseAddresses = new List<uint>();
			foreach (Firmware.FirmwareRegion region in firmware.FirmwareRegions)
			{
				allSectorBaseAddresses.AddRange(region.SectorBaseAddresses);
			}

			// erase each sector
			for (int iSector = 0; iSector < allSectorBaseAddresses.Count; iSector++)
			{
				device.EraseSector((int)allSectorBaseAddresses[iSector]);
				eraseProgress = (iSector + 1) * 100 / allSectorBaseAddresses.Count();
				Debug.WriteLine(CurrentProgress.ToString());
				RaiseFirmwareUpdateProgress(CurrentProgress.ToString());
			}

			device.Uploading += (sender, e) =>
			{
				uploadedByteCount += e.BytesUploaded;
				uploadProgress = uploadedByteCount * 100 / totalBytes;
				Debug.WriteLine(CurrentProgress.ToString());
				RaiseFirmwareUpdateProgress(CurrentProgress.ToString());
			};

			for (int i = 0; i < firmware.FirmwareRegions.Count; i++)
			{
				var region = firmware.FirmwareRegions[i];
				if (region.Filename != null)
				{
					using (System.IO.StreamReader streamReader = new System.IO.StreamReader(firmware.FolderPath + "/" + region.Filename))
					{
						string hexFileString = streamReader.ReadToEnd();
						byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, region.BaseAddress);
						totalBytes += hexFileBytes.Length;
					}
				}
			}

			for (int i = 0; i < firmware.FirmwareRegions.Count; i++)
			{
				var region = firmware.FirmwareRegions[i];
				if (region.Filename != null)
				{
					using (System.IO.StreamReader streamReader = new System.IO.StreamReader(firmware.FolderPath + "/" + region.Filename))
					{
						string hexFileString = streamReader.ReadToEnd();
						byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, region.BaseAddress);
						device.Upload(hexFileBytes, (int)region.BaseAddress);
					}
				}
			}

			//// step 4: restart board
			device.SetAddress(0x08000001); // NOTE: for thumb2 instructinos, we added 1 to the "base address".  Otherwise our board will not restart properly.
			RaiseFirmwareUpdateProgress("Update Complete");

			//                                    // leave DFU mode.
			////device.LeaveDfuMode();
			return Task.CompletedTask;
		}

		public delegate void FirmwareUpdateProgressDelegate(string version);
		public event FirmwareUpdateProgressDelegate FirmwareUpdateProgress;

		internal void RaiseFirmwareUpdateProgress(string status)
		{
			if (this.FirmwareUpdateProgress != null)
				this.FirmwareUpdateProgress(status);
		}
	}
}
