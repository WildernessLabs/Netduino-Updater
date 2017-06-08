using System;
using System.Collections.Generic;

namespace NetduinoDeploy
{
    public static class Globals
    {
        static Globals()
		{
			DeviceTypes = new List<Device>();
			DeviceTypes.Add(new Device() { ProductID = 4, Name = "Netduino Go" });
            DeviceTypes.Add(new Device() { ProductID = 5, Name = "Netduino Plus 2" });
            DeviceTypes.Add(new Device() { ProductID = 6, Name = "Netduino 2" });
            DeviceTypes.Add(new Device() { ProductID = 7, Name = "Netduino 3" });
            DeviceTypes.Add(new Device() { ProductID = 8, Name = "Netduino 3 Ethernet" });
            DeviceTypes.Add(new Device() { ProductID = 9, Name = "Netduino 3 Wifi" });
        }

        public static List<Device> DeviceTypes { get; set; }
    }
}
