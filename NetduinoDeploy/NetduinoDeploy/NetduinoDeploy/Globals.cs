using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace NetduinoDeploy
{
    public static class Globals
    {
        static Globals()
		{
            try
            {
                using (TextReader tr = File.OpenText(Path.Combine(Environment.CurrentDirectory, "devices.json")))
                {
                    var data = tr.ReadToEnd();
                    DeviceTypes = JsonConvert.DeserializeObject<List<Device>>(data);
                }
            }
            catch
            {
                DeviceTypes = new List<Device>();
            }
        }

        public static List<Device> DeviceTypes { get; set; }

        public static Device GetDeviceFromId (int productId)
        {
            return DeviceTypes.Single(x => x.ProductID == productId);
        }

        public static int ConnectedDeviceId { get; set; } = -1;

        public static Device ConnectedDevice => GetDeviceFromId(ConnectedDeviceId);
    }
}