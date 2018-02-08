using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NetduinoDeploy
{
    public class Device
    {
		[JsonProperty("productId")]
        public int ProductID { get; set; }
		[JsonProperty("productName")]
        public string Name { get; set; }
		[JsonProperty("configBaseAddress")]
		public uint ConfigBaseAddress { get; set; }
		[JsonProperty("flashBaseAddress")]
		public uint FlashBaseAddress { get; set; }
		[JsonProperty("hasMacAddress")]
		public bool HasMacAddress { get; set; }
		[JsonProperty("isWirelessCapable")]
		public bool IsWirelessCapable { get; set; }
		[JsonProperty("sectors")]
		public IEnumerable<uint> Sectors { get; set; }
    }
}