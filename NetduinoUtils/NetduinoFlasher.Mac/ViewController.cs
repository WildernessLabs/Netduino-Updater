using System;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Collections;
using System.Collections.Generic;
using AppKit;
using Foundation;
using DfuSharp;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NetduinoFlasher.Mac
{
	public partial class ViewController : NSViewController
	{
		List<Firmware> _firmwares;

		public ViewController(IntPtr handle) : base(handle)
		{
			_firmwares = new List<Firmware>();
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Do any additional setup after loading the view.
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

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			// Create the Product Table Data Source and populate it
			var DataSource = new DeviceTableDataSource();

			DataSource.Devices.AddRange(DeviceHelper.GetAttachedDevices());

			// Populate the Product Table
			DeviceTable.DataSource = DataSource;
			DeviceTable.Delegate = new DeviceTableDelegate(DataSource);
		}

		private void EraseAndUploadDevice(byte productID)
		{
			UpdateStatus.StringValue = "Initializing device...";

			Firmware firmware = _firmwares.SingleOrDefault(x => x.ProductID == productID);

			DfuSharp.Context dfuContext = new Context();
			var devices = dfuContext.GetDfuDevices(0x0483, 0xDF11);

			if (devices.Count == 0)
			{
				throw new Exception("Device not found");
			}

			DfuDevice device = devices[0];
			device.ClaimInterface();
			device.SetInterfaceAltSetting(0);
			device.Clear();

			// TODO: make sure we are in DFU mode; if we are in app mode (runtime) then we need to detach and re-enumerate.

			UpdateStatus.StringValue = "Erasing device...";

			// get our total sectors and block counts
			List<uint> allSectorBaseAddresses = new List<uint>();
			//uint totalBlockCount = 0;
			foreach (Firmware.FirmwareRegion region in firmware.FirmwareRegions)
			{
				allSectorBaseAddresses.AddRange(region.SectorBaseAddresses);
			}

			// erase each sector
			for (int iSector = 0; iSector < allSectorBaseAddresses.Count; iSector++)
			{
				device.EraseSector((int)allSectorBaseAddresses[iSector]);
			}

			int totalBytes = 0;
			string currentFile = string.Empty;

			device.Uploading += (sender, e) =>
			{
				UpdateStatus.StringValue = string.Format("Uploading {0} ({1}/{2}", currentFile, e.BytesUploaded, totalBytes);
				Debug.WriteLine("uploading " + e.BytesUploaded + "/" + totalBytes);
			};

			foreach (Firmware.FirmwareRegion region in firmware.FirmwareRegions)
			{
				if (region.Filename != null)
				{
					
					System.IO.StreamReader streamReader = new System.IO.StreamReader(firmware.FolderPath + "/" + region.Filename);
					string hexFileString = streamReader.ReadToEnd();
					streamReader.Dispose();
					byte[] hexFileBytes = SrecHexEncoding.GetBytes(hexFileString, region.BaseAddress);

					totalBytes = hexFileBytes.Length;
					currentFile = region.Filename;

					Debug.WriteLine("uploading " + region.Filename + " " + region.BaseAddress);
					device.Upload(hexFileBytes, (int)region.BaseAddress);
				}
			}

			//// step 4: restart board
			device.SetAddress(0x08000001); // NOTE: for thumb2 instructinos, we added 1 to the "base address".  Otherwise our board will not restart properly.

			UpdateStatus.StringValue = "Firmware update complete";

			//									  // leave DFU mode.
			////device.LeaveDfuMode();
		}

		private void LoadFirmwareFiles()
		{
			UpdateStatus.StringValue = "Loading files...";			// search for all XML files in our firmware subdirectory, recursively

			// find all folders containing firmware
			string firmwareRootFolder = @"Firmware";
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
						_firmwares.Add(firmware);
					}
					catch
					{
					}
				}
			}
		}

		partial void ClickedUpdateFirmware(Foundation.NSObject sender)
		{
			LoadFirmwareFiles();
			EraseAndUploadDevice(9);
		}
	}
}
