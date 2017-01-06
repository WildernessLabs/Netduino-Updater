using System;
namespace WildernessLabs.Netduino.Device
{
	public class Device
	{
		public Device()
		{
		}

		public bool CanUpdate { get; set; }
		public string DevicePath { get; set; }
		public byte[] MacAddress { get; set; }
		public byte OtpSlotsFree { get; set; }
		public byte ProductID { get; set; }
		public byte VendorID { get; set; }
		public string ProductName { get; set; }
		public string UpgradeVersion { get; set; }


		public static Device FromUsbDevice(LibUsbDotNet.UsbDevice)
		{
			Device d = new Device();


			return d;

		}
	}
}
