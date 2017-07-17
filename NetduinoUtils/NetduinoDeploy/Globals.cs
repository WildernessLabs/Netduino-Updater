using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NetduinoDeploy
{
    public static class Globals
    {
        static Globals()
		{
			using (TextReader tr = File.OpenText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Resources), "devices.json")))
			{
				var data = tr.ReadToEnd();
				DeviceTypes = JsonConvert.DeserializeObject<List<Device>>(data);
			}
        }

        public static List<Device> DeviceTypes { get; set; }
    }
}
