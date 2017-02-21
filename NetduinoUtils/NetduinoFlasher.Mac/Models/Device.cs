using System;
namespace NetduinoFlasher.Mac.Models
{
	public class Device
	{
		public Device()
		{
		}

		public Device(string manufacturer, string product)
		{
			this.Manufacturer = manufacturer;
			this.Product = product;
		}

		public ushort VendorID { get; set; }
		public ushort ProductID { get; set; }

		public string Manufacturer { get; set; }
		public string Product { get; set; }
	}
}
