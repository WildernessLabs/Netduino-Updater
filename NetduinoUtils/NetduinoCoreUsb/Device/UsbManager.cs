using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace WildernessLabs.Netduino.Device
{
	public static class UsbManager
	{
		private static List<int> _knownVendorIDs = new List<int>();

		static UsbManager()
		{
			_knownVendorIDs.Add(0x05A); // Secret Labs
			_knownVendorIDs.Add(0x1B9F); // GHI Electronics
		}

		public static List<Device> EnumerateAttachedKnownDevices()
		{
			List<Device> devices = new List<Device>();
			UsbDevice usbDevice;

			// attempt to enumerate all the devices
			try
			{
				UsbRegDeviceList allDevices = UsbDevice.AllDevices;
				foreach (UsbRegistry usbRegistry in allDevices)
				{
					if (usbRegistry.Open(out usbDevice))
					{
						Debug.WriteLine(usbDevice.Info.ToString());

						// HACK
						foreach (var id in _knownVendorIDs)
						{
							if (usbDevice.Info.ToString().Contains("VendorID:" + id.ToString()){
								devices.Add(Device.FromUsbDevice(usbDevice));
								Debug.WriteLine("Found a device");
							}
						}
						//if (_knownVendorIDs.Contains(usbDevice.Profile.DeviceDescriptor.VendorID)


						//for (int iConfig = 0; iConfig < usbDevice.Configs.Count; iConfig++)
						//{
						//	UsbConfigInfo configInfo = usbDevice.Configs[iConfig];
						//	//Console.WriteLine(configInfo.ToString());

						//	ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.InterfaceInfoList;
						//	for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
						//	{
						//		UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
						//		//Console.WriteLine(interfaceInfo.ToString());

						//		ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.EndpointInfoList;
						//		for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
						//		{
						//			var endpoint = endpointList[iEndpoint];
						//			if (endpoint.Descriptor.DescriptorType == LibUsbDotNet.Descriptors.DescriptorType.Device)
						//			{
						//				Debug.WriteLine(endpointList[iEndpoint].ToString());

						//			}

						//		}
						//	}
						//}
					}
				}
			}
			finally
			{
				// Free usb resources.
				// This is necessary for libusb-1.0 and Linux compatibility.
				UsbDevice.Exit();
			}


			return devices;
		}
	}
}
