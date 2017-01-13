using System;
namespace NetduinoFlasher.Mac
{
	public class Device
	{
		public Device()
		{
		}

		public Device(string name)
		{
			this.Name = name;
		}

		public string Name { get; set; }
	}
}
