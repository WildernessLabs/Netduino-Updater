using System;
using System.Collections.Generic;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace NetduinoFlasher.Mac
{
	public class DeviceHelper
	{
		public DeviceHelper()
		{
		}

		public static UsbDevice MyUsbDevice;

		public static List<Device> GetAttachedDevices()
		{
			List<Device> devices = new List<Device>();

			List<short> validVendorIDs = new List<short>
			{
				0x22B1, // secret labs
				0x1B9F, // ghi
				0x05A // who knows
			};

			// Dump all devices and descriptor information to console output.
			UsbRegDeviceList allDevices = UsbDevice.AllDevices;
			foreach (UsbRegistry usbRegistry in allDevices)
			{
				if (usbRegistry.Open(out MyUsbDevice))
				{
					if (!validVendorIDs.Contains(MyUsbDevice.Info.Descriptor.VendorID)) { continue; }

					devices.Add(new Device(MyUsbDevice.Info.ProductString));

					//Console.WriteLine(MyUsbDevice.Info.ToString());

					//for (int iConfig = 0; iConfig < MyUsbDevice.Configs.Count; iConfig++)
					//{
					//	UsbConfigInfo configInfo = MyUsbDevice.Configs[iConfig];
					//	//Console.WriteLine(configInfo.ToString());

					//	ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.InterfaceInfoList;
					//	for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
					//	{
					//		UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
					//		//Console.WriteLine(interfaceInfo.ToString());

					//		ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.EndpointInfoList;
					//		for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
					//		{
					//			Console.WriteLine(endpointList[iEndpoint].ToString());
					//		}
					//	}
					//}
				}
			}

			//UsbDevice.Exit();

			return devices;
		}
	}
}