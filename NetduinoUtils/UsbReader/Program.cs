using System;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MonoLibUsb;
using MonoLibUsb.Profile;
using MonoLibUsb.Descriptors;
using LibUsbDotNet.LudnMonoLibUsb;

namespace UsbReader
{
	class MainClass
	{
		//public static UsbDevice MyUsbDevice;

		public static UsbDevice MyUsbDevice;

		public static void Main(string[] args)
		{
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
					//if (validVendorIDs.Contains(MyUsbDevice.Info.Descriptor.VendorID))
					//{
						Console.WriteLine(MyUsbDevice.Info.ToString());

						//for (int iConfig = 0; iConfig < MyUsbDevice.Configs.Count; iConfig++)
						//{
						//	UsbConfigInfo configInfo = MyUsbDevice.Configs[iConfig];
						//	Console.WriteLine(configInfo.ToString());

						//	ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.InterfaceInfoList;
						//	for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
						//	{
						//		UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
						//		Console.WriteLine(interfaceInfo.ToString());

						//		ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.EndpointInfoList;
						//		for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
						//		{
						//			Console.WriteLine(endpointList[iEndpoint].ToString());
						//		}
						//	}
						//}
					//}
					MyUsbDevice.Close();
				}
			}

			// Free usb resources.
			// This is necessary for libusb-1.0 and Linux compatibility.
			UsbDevice.Exit();
		}
	}
}
