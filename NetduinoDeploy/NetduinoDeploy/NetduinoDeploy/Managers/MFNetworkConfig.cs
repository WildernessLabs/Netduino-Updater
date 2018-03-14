using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;

namespace NetduinoDeploy.Managers
{
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct HAL_NetworkConfiguration : IHAL_CONFIG_BASE
	{
		public const int c_maxMacBufferLength = 64;

		public HAL_CONFIG_BLOCK Header;
		public Int32 NetworkCount;
		public Int32 Enabled;
		public UInt32 flags;
		public UInt32 ipaddr;
		public UInt32 subnetmask;
		public UInt32 gateway;
		public UInt32 dnsServer1;
		public UInt32 dnsServer2;
		public UInt32 networkInterfaceType;
		public UInt32 macAddressLen;
		public fixed byte macAddressBuffer[c_maxMacBufferLength];

		public HAL_CONFIG_BLOCK ConfigHeader
		{
			get { return Header; }
			set { Header = value; }
		}

		public int Size
		{
			get
			{
				int size = 0;

				unsafe
				{
					size = sizeof(HAL_NetworkConfiguration);
				}

				return size;
			}
		}
	}

	public class MFNetworkConfiguration
	{
		HAL_NetworkConfiguration m_cfg = new HAL_NetworkConfiguration();
		//MFConfigHelper m_cfgHelper;

		const uint c_SOCK_NETWORKCONFIGURATION_FLAGS_DHCP = 1;
		const uint c_SOCK_NETWORKCONFIGURATION_INTERFACETYPE_ETHERNET = 6;

		const string c_CfgName = "NETWORK";
		const int c_ConfigTypeFlagShift = 16;

		public MFNetworkConfiguration()
		{
			m_cfg.networkInterfaceType = c_SOCK_NETWORKCONFIGURATION_INTERFACETYPE_ETHERNET;
			//m_cfgHelper = new MFConfigHelper(dev);
		}

		public bool EnableDhcp
		{
			get { return (m_cfg.flags & c_SOCK_NETWORKCONFIGURATION_FLAGS_DHCP) != 0; }
			set
			{
				uint hiBits = m_cfg.flags & 0xFFFF0000;
				m_cfg.flags = value ? c_SOCK_NETWORKCONFIGURATION_FLAGS_DHCP : 0;
				m_cfg.flags |= hiBits; /// Keep this same.
			}
		}

		public IPAddress IpAddress
		{
			get
			{
				return new IPAddress(m_cfg.ipaddr);
			}
			set
			{
				if (value.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
				{
					//throw new MFInvalidNetworkAddressException();
				}

				m_cfg.ipaddr = BitConverter.ToUInt32(value.GetAddressBytes(), 0);
			}
		}
		public IPAddress SubNetMask
		{
			get
			{
				return new IPAddress(m_cfg.subnetmask);
			}
			set
			{
				if (value.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
				{
					//throw new MFInvalidNetworkAddressException();
				}

				m_cfg.subnetmask = BitConverter.ToUInt32(value.GetAddressBytes(), 0);
			}
		}
		public IPAddress Gateway
		{
			get
			{
				return new IPAddress(m_cfg.gateway);
			}
			set
			{
				if (value.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
				{
					//throw new MFInvalidNetworkAddressException();
				}

				m_cfg.gateway = BitConverter.ToUInt32(value.GetAddressBytes(), 0);
			}
		}
		public IPAddress PrimaryDns
		{
			get
			{
				return new IPAddress(m_cfg.dnsServer1);
			}
			set
			{
				if (value.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
				{
					//throw new MFInvalidNetworkAddressException();
				}

				m_cfg.dnsServer1 = BitConverter.ToUInt32(value.GetAddressBytes(), 0);
			}
		}
		public IPAddress SecondaryDns
		{
			get
			{
				return new IPAddress(m_cfg.dnsServer2);
			}
			set
			{
				if (value.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
				{
					//throw new MFInvalidNetworkAddressException();
				}

				m_cfg.dnsServer2 = BitConverter.ToUInt32(value.GetAddressBytes(), 0);
			}
		}
		public byte[] MacAddress
		{
			get
			{
				uint len = m_cfg.macAddressLen;

				if (len > HAL_NetworkConfiguration.c_maxMacBufferLength)
				{
					len = HAL_NetworkConfiguration.c_maxMacBufferLength;
				}

				byte[] data = new byte[len];

				unsafe
				{
					fixed (byte* pData = m_cfg.macAddressBuffer)
					{
						for (int i = 0; i < len; i++)
						{
							data[i] = pData[i];
						}
					}
				}
				return data;
			}
			set
			{
				if (value.Length > 64)
				{
					//throw new MFInvalidMacAddressException();
				}

				unsafe
				{
					fixed (byte* pData = m_cfg.macAddressBuffer)
					{
						for (int i = 0; i < value.Length; i++)
						{
							pData[i] = value[i];
						}
					}
				}
				m_cfg.macAddressLen = (uint)value.Length;
			}
		}

		public int MaxMacAddressLength { get { return 64; } }

		public enum NetworkConfigType
		{
			Generic = 0,
			Wireless = 1,
		}

		public NetworkConfigType ConfigurationType
		{
			get
			{
				uint type = (m_cfg.flags >> c_ConfigTypeFlagShift) & 0xF;

				return (NetworkConfigType)(type);
			}

			set
			{
				uint type = (uint)value;
				m_cfg.flags |= ((type & 0xF) << c_ConfigTypeFlagShift);
			}
		}

		public void Load(NetworkManager manager)
		{
			//NetworkManager networkManager = new NetworkManager();
			byte[] data = manager.FindConfig(c_CfgName);

			if (data != null)
			{
				m_cfg = (HAL_NetworkConfiguration)NetworkManager.UnmarshalData(data, typeof(HAL_NetworkConfiguration));
			}

			//byte[] data = m_cfgHelper.FindConfig(c_CfgName);

			//if (data != null)
			//{
			//	m_cfg = (HAL_NetworkConfiguration)MFConfigHelper.UnmarshalData(data, typeof(HAL_NetworkConfiguration));
			//}
		}

		public void Save(NetworkManager manager)
		{
			//NetworkManager networkManager = new NetworkManager();

			m_cfg.NetworkCount = 1;
			m_cfg.Enabled = 1;
			manager.WriteConfig(c_CfgName, m_cfg);
		}

		//internal void SwapConfigBuffer(MFConfigHelper srcConfig)
		//{
		//	m_cfgHelper.SwapAllConfigData(srcConfig);
		//}
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct HAL_WirelessConfiguration : IHAL_CONFIG_BASE
	{
		public const int c_PassPhraseLength = 64;
		public const int c_NetworkKeyLength = 256;
		public const int c_ReKeyInternalLength = 32;
		public const int c_SSIDLength = 32;

		public HAL_CONFIG_BLOCK Header;
		public Int32 WirelessNetworkCount;
		public Int32 Enabled;
		public UInt32 WirelessFlags;
		public fixed byte PassPhrase[c_PassPhraseLength];
		public Int32 NetworkKeyLength;
		public fixed byte NetworkKey[c_NetworkKeyLength];
		public Int32 ReKeyLength;
		public fixed byte ReKeyInternal[c_ReKeyInternalLength];
		public fixed byte SSID[c_SSIDLength];

		public HAL_CONFIG_BLOCK ConfigHeader
		{
			get { return Header; }
			set { Header = value; }
		}

		public int Size
		{
			get
			{
				int size = 0;

				unsafe
				{
					size = sizeof(HAL_WirelessConfiguration);
				}

				return size;
			}
		}
	}

	
}