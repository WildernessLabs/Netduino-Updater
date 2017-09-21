using System;
using System.Net;

namespace NetduinoDeploy.Managers
{
	public class NetworkConfig
	{
		public NetworkConfig()
		{
			StaticIPAddress = ParseAddress("192.168.5.100");
			SubnetMask = ParseAddress("255.255.255.0");
			DefaultGateway = ParseAddress("192.168.5.1");
			PrimaryDNS = ParseAddress("0.0.0.0");
			SecondaryDNS = ParseAddress("0.0.0.0");
		}

		public bool EnableDHCP { get; set; } = true;
		public IPAddress StaticIPAddress { get; set; }
		public IPAddress SubnetMask { get; set; }
		public IPAddress DefaultGateway { get; set; }
		public byte[] NetworkMacAddress { get; set; }
		public IPAddress PrimaryDNS { get; set; }
		public IPAddress SecondaryDNS { get; set; }
		public int Authentication { get; set; }
		public int Encryption { get; set; }
		public int Radio { get; set; } = 15; // a,b,g,n 

		public string Passphrase { get; set; } = "";
		public bool EncryptConfig { get; set; }
		public int NetworkKeyLength { get; set; }
		public string NetworkKey { get; set; } = string.Empty;
		public string ReKeyInternal { get; set; } = string.Empty;
		public string SSID { get; set; } = string.Empty;

		public bool IsWireless { get; set; }

		public IPAddress ParseAddress(string address)
		{
			IPAddress newAddress;
			IPAddress.TryParse(address, out newAddress);
			return newAddress;
		}
	}
}
