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
            try
            {
                using (TextReader tr = File.OpenText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Resources), "devices.json")))
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

        
    }
}