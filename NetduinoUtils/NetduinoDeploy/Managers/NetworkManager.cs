using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DfuSharp;

namespace NetduinoDeploy.Managers
{
	public class NetworkManager
	{
		public NetworkManager()
		{
		}

		private bool m_init = false;
		private HAL_CONFIGURATION_SECTOR m_StaticConfig;
		private byte[] m_all_cfg_data = null;
		private int m_lastCfgIndex = -1;
		private Hashtable m_cfgHash = new Hashtable();
		private const UInt32 c_Version_V2 = 0x324C4148; // HAL2
		private const UInt32 c_Seed = 1;          // HAL_STRUCT_VERSION
		private const UInt32 c_EnumerateAndLaunchAddr = 0x0;
		private const int c_MaxDriverNameLength = 63;
		int configAddress = 0x0800C000;
		int configSize = 16384;

		internal struct ConfigIndexData
		{
			internal ConfigIndexData(int idx, int size)
			{
				Index = idx;
				Size = size;
			}
			internal int Index;
			internal int Size;
		}

		public byte[] FindConfig(string configName)
		{
			byte[] retVal = null;

			if (!m_init)
			{
				InitializeConfigData();
			}

			// see if we have seen this configuration.
			if (m_cfgHash.ContainsKey(configName))
			{
				ConfigIndexData cid = (ConfigIndexData)m_cfgHash[configName];

				retVal = new byte[cid.Size];

				Array.Copy(m_all_cfg_data, cid.Index, retVal, 0, cid.Size);
			}

			return retVal;   
		}
		
		// Initialize the internal structures.
		public void InitializeConfigData()
		{
			uint hal_config_block_size = 0;
			uint hal_config_static_size = 0;
			int index = 0;

			unsafe
			{
				hal_config_block_size = (uint)sizeof(HAL_CONFIG_BLOCK);
				hal_config_static_size = (uint)sizeof(HAL_CONFIGURATION_SECTOR) - hal_config_block_size;
			}

			// read in the configuration data if the config sector was found
			if ((uint)configAddress != uint.MaxValue)
			{
				int hal_static_cfg_size = 0;
				unsafe
				{
					hal_static_cfg_size = sizeof(HAL_CONFIGURATION_SECTOR);
				}

				var devices = DfuContext.Current.GetDevices();
				DfuDevice device = devices[0];

				m_all_cfg_data = new byte[hal_static_cfg_size];

				device.Download(m_all_cfg_data, configAddress);

				m_StaticConfig = (HAL_CONFIGURATION_SECTOR)UnmarshalData(m_all_cfg_data, typeof(HAL_CONFIGURATION_SECTOR));

				// uninitialized config sector, lets try to fix it
				if (m_StaticConfig.ConfigurationLength == 0xFFFFFFFF)
				{
					m_StaticConfig.ConfigurationLength = (uint)hal_static_cfg_size;
					m_StaticConfig.Version.Major = 3;
					m_StaticConfig.Version.Minor = 0;
					m_StaticConfig.Version.Extra = 0;
					m_StaticConfig.Version.TinyBooter = 4;
				}

				// move to the dynamic configuration section
				index = (int)m_StaticConfig.ConfigurationLength;

				m_lastCfgIndex = index;

				while (true)
				{
					byte[] data = new byte[hal_config_block_size];

					// read the next configuration block
					device.Download(data, configAddress + index);

					HAL_CONFIG_BLOCK cfg_header = (HAL_CONFIG_BLOCK)UnmarshalData(data, typeof(HAL_CONFIG_BLOCK));

					// out of memory or last record
					if (cfg_header.Size > configSize)
					{
						// last record or bogus entry
						m_lastCfgIndex = index;

						// save the configuration data for later use
						m_all_cfg_data = new byte[m_lastCfgIndex];

						int idx = 0;
						byte[] tmp = null;

						while (idx < index)
						{
							int size = 512;

							if ((index - idx) < size) size = index - idx;
							tmp = new byte[size];

							device.Download(tmp, configAddress + idx);
							Array.Copy(tmp, 0, m_all_cfg_data, idx, tmp.Length);

							idx += size;
						}
						break; // no more configs
					}

					// move to the next configuration block
					if (cfg_header.Size + hal_config_block_size + index > configSize)
					{
						// end of config sector
						break;
					}

					m_cfgHash[cfg_header.DriverNameString] = new ConfigIndexData(index, (int)(cfg_header.Size + hal_config_block_size));

					index += (int)(cfg_header.Size + hal_config_block_size);

					while (0 != (index % 4))
					{
						index++;
					}
				}
			}
			m_init = true;
		}

		public void WriteConfig(string configName, IHAL_CONFIG_BASE config)
		{
			WriteConfig(configName, config, true);
		}

		public void WriteConfig(string configName, IHAL_CONFIG_BASE config, bool updateConfigSector)
		{
			uint hal_config_block_size = 0;

			HAL_CONFIG_BLOCK header = config.ConfigHeader;

			unsafe
			{
				hal_config_block_size = (uint)sizeof(HAL_CONFIG_BLOCK);

				header.DriverNameString = configName;
			}

			// set up the configuration data
			header.HeaderCRC = 0;
			header.DataCRC = 0;
			header.Size = (uint)config.Size - hal_config_block_size;
			header.Signature = c_Version_V2;

			config.ConfigHeader = header;

			// calculate the data crc 
			byte[] data = MarshalData(config);
			header.DataCRC = CRC.ComputeCRC(data, (int)hal_config_block_size, (int)(header.Size/* - hal_config_block_size*/), 0);
			// this enables the data type to update itself with the crc (required because there is no class inheritence in structs and therefore no polymorphism)
			config.ConfigHeader = header;

			// calculate the header crc
			data = MarshalData(config);
			header.HeaderCRC = CRC.ComputeCRC(data, 2 * sizeof(UInt32), (int)hal_config_block_size - (2 * sizeof(UInt32)), c_Seed);
			// this enables the data type to update itself with the crc (required because there is no class inheritence in structs and therefore no polymorphism)
			config.ConfigHeader = header;

			data = MarshalData(config);

			WriteConfig(configName, data, true, updateConfigSector);
		}

		/// <summary>
		/// The WriteConfig method is used to update or create a device configuration.  If the name of the configuration exists 
		/// on the device, then the configuration is updated.  Otherwise, a new configuration is added.
		/// </summary>
		/// <param name="configName">Unique case-sensitive name of the configuration</param>
		/// <param name="data">Data to be written for the given name (not including the header)</param>
		public void WriteConfig(string configName, byte[] data)
		{
			uint hal_config_block_size = 0;
			HAL_CONFIG_BLOCK header = new HAL_CONFIG_BLOCK();

			// Create a header for the configuration data
			unsafe
			{
				hal_config_block_size = (uint)sizeof(HAL_CONFIG_BLOCK);

				header.DriverNameString = configName;
			}
			header.HeaderCRC = 0;
			header.DataCRC = CRC.ComputeCRC(data, 0, data.Length, 0); ;
			header.Size = (uint)data.Length;
			header.Signature = c_Version_V2;

			// Calculate CRC information for header and data
			header.DataCRC = CRC.ComputeCRC(data, 0, (int)data.Length, 0);

			byte[] headerBytes = MarshalData(header);
			header.HeaderCRC = CRC.ComputeCRC(headerBytes, (2 * sizeof(UInt32)), (int)hal_config_block_size - (2 * sizeof(UInt32)), c_Seed);
			headerBytes = MarshalData(header);

			// Concatonate the header and data
			byte[] allData = new byte[hal_config_block_size + data.Length];

			Array.Copy(headerBytes, allData, hal_config_block_size);
			Array.Copy(data, 0, allData, hal_config_block_size, data.Length);

			WriteConfig(configName, allData, false, true);
		}

		// Write the concatonated header and configuration data to the Flash config sector
		private void WriteConfig(string configName, byte[] data, bool staticSize, bool updateConfigSector)
		{
			
			if (!m_init)
			{
				InitializeConfigData();
			}

			// updating the config
			if (m_cfgHash.ContainsKey(configName))
			{
				ConfigIndexData cid = (ConfigIndexData)m_cfgHash[configName];

				// If old and new data are different sizes
				if (cid.Size != data.Length)
				{
					// If data comes from a well defined structure, its size cannot vary
					//if (staticSize) throw new MFInvalidConfigurationDataException();

					uint newNextIndex, oldNextIndex;
					byte[] temp;
					int diff = 0;

					// Figure out where any following configuration data will start
					newNextIndex = (uint)(cid.Index + data.Length);
					while (0 != (newNextIndex % 4))
					{
						newNextIndex++;        // Force a 4 byte boundary
					}

					// Figure out where any following configuration data previously started
					oldNextIndex = (uint)(cid.Index + cid.Size);
					while (0 != (oldNextIndex % 4))
					{
						oldNextIndex++;        // Force a 4 byte boundary
					}


					diff = (int)newNextIndex - (int)oldNextIndex;           // Find the adjusted difference in size between old and new config data
					temp = new byte[m_lastCfgIndex + diff];                 // Create a new byte array to contain all the configuration data

					Array.Copy(m_all_cfg_data, temp, cid.Index);            // Copy all preceding data to new array
					Array.Copy(data, 0, temp, cid.Index, data.Length);      // Copy new configuration to new array
					if (oldNextIndex < m_lastCfgIndex)                      // Copy all following data (if it exists) to new array
					{
						Array.Copy(m_all_cfg_data, oldNextIndex, temp, newNextIndex, (m_all_cfg_data.Length - oldNextIndex));
					}

					// Update the local copy of the configuration list
					m_all_cfg_data = temp;
					m_lastCfgIndex += diff;
				}
				else
				{
					// Copy the new configuration data on top of the old
					Array.Copy(data, 0, m_all_cfg_data, cid.Index, data.Length);
				}
			}
			else        // adding a new configuration to the end of the current list
			{
				uint newLastIndex;

				if (m_lastCfgIndex == -1) throw new OutOfMemoryException();

				// Find the new size of the whole configuration list
				newLastIndex = (uint)(m_lastCfgIndex + data.Length);

				while (0 != (newLastIndex % 4))
				{
					newLastIndex++;        // Force a 4 byte boundary
				}

				byte[] temp = new byte[m_lastCfgIndex >= m_all_cfg_data.Length ? m_lastCfgIndex + data.Length : m_all_cfg_data.Length];

				Array.Copy(m_all_cfg_data, 0, temp, 0, m_all_cfg_data.Length);
				Array.Copy(data, 0, temp, m_lastCfgIndex, data.Length);

				// Update the local copy of the configuration list
				m_all_cfg_data = temp;
				m_lastCfgIndex = (int)newLastIndex;
			}

			if (!updateConfigSector) return;

			// Rewrite entire configuration list to Flash
			var devices = DfuContext.Current.GetDevices();
			DfuDevice device = devices[0];
			byte[] clear = new byte[m_all_cfg_data.Length];

			device.EraseSector(configAddress);
			device.Upload(m_all_cfg_data, configAddress);

			// Rebuild hash table
			m_cfgHash.Clear();
			uint hal_config_block_size = 0;
			unsafe
			{
				hal_config_block_size = (uint)sizeof(HAL_CONFIG_BLOCK);
			}
			int index = (int)m_StaticConfig.ConfigurationLength;
			byte[] headerData = new byte[hal_config_block_size];
			HAL_CONFIG_BLOCK cfg_header;
			while (index < m_lastCfgIndex)
			{
				// Read in next configuration header
				Array.Copy(m_all_cfg_data, index, headerData, 0, hal_config_block_size);
				cfg_header = (HAL_CONFIG_BLOCK)UnmarshalData(headerData, typeof(HAL_CONFIG_BLOCK));

				m_cfgHash[cfg_header.DriverNameString] = new ConfigIndexData(index, (int)(cfg_header.Size + hal_config_block_size));

				// Index of next configuration header must lie on a 4 byte boundary
				index += (int)(cfg_header.Size + hal_config_block_size);
				while (0 != (index % 4))
				{
					index++;        // Force a 4 byte boundary
				}
			}

		}

		private byte[] MarshalData(object obj)
		{
			int cBytes = Marshal.SizeOf(obj);
			byte[] data = new byte[cBytes];
			GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);

			Marshal.StructureToPtr(obj, gch.AddrOfPinnedObject(), false);

			gch.Free();
			return data;   
		}
		
		public static object UnmarshalData(byte[] data, Type type)
		{
			GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);

			object obj = Marshal.PtrToStructure(gch.AddrOfPinnedObject(), type);

			gch.Free();

			return obj;  
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct CONFIG_SECTOR_VERSION
	{
		internal const int c_CurrentTinyBooterVersion = 4;

		internal byte Major;
		internal byte Minor;
		internal byte TinyBooter;
		internal byte Extra;
	};

	[StructLayout(LayoutKind.Sequential)]
	internal struct OEM_MODEL_SKU
	{
		internal byte OEM;
		internal byte Model;
		internal UInt16 SKU;
	};

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct OEM_SERIAL_NUMBERS
	{
		internal fixed byte module_serial_number[32];
		internal fixed byte system_serial_number[16];
	};

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct TINYBOOTER_KEY_CONFIG
	{
		internal const int c_KeySignatureLength = 128;
		internal const int c_RSAKeyLength = 260;

		//internal fixed byte KeySignature[c_KeySignatureLength];
		internal fixed byte SectorKey[c_RSAKeyLength]; //RSAKey 4 bytes (exponent) + 128 bytes (module) + 128 bytes (exponent)
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct SECTOR_BIT_FIELD
	{
		const int c_MaxSectorCount = 287; // pxa271 has 259 sectors, 287 == 9 * sizeof(UINT32) - 1, which is the next biggest whole 
		const int c_MaxFieldUnits = (c_MaxSectorCount + 1) / (8 * sizeof(UInt32)); // bits

		internal fixed UInt32 BitField[c_MaxFieldUnits];
	}

	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct SECTOR_BIT_FIELD_TB
	{
		const int c_MaxBitCount = 8640;
		const int c_MaxFieldUnits = (c_MaxBitCount / (8 * sizeof(UInt32))); // bits

		internal fixed UInt32 BitField[c_MaxFieldUnits]; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal unsafe struct HAL_CONFIGURATION_SECTOR
	{
		const int c_MaxBootEntryFlags = 50;
		const int c_BackwardsCompatibilityBufferSize = 88;

		internal UInt32 ConfigurationLength;

		internal CONFIG_SECTOR_VERSION Version;

		internal fixed byte Buffer[c_BackwardsCompatibilityBufferSize];

		internal fixed UInt32 BooterFlagArray[c_MaxBootEntryFlags];

		internal SECTOR_BIT_FIELD SignatureCheck1; // 8 changes before erase
		internal SECTOR_BIT_FIELD SignatureCheck2; // 8 changes before erase
		internal SECTOR_BIT_FIELD SignatureCheck3; // 8 changes before erase
		internal SECTOR_BIT_FIELD SignatureCheck4; // 8 changes before erase
		internal SECTOR_BIT_FIELD SignatureCheck5; // 8 changes before erase
		internal SECTOR_BIT_FIELD SignatureCheck6; // 8 changes before erase
		internal SECTOR_BIT_FIELD SignatureCheck7; // 8 changes before erase
		internal SECTOR_BIT_FIELD SignatureCheck8; // 8 changes before erase

		internal TINYBOOTER_KEY_CONFIG PublicKeyFirmware;
		internal TINYBOOTER_KEY_CONFIG PublicKeyDeployment;

		internal OEM_MODEL_SKU OEM_Model_SKU;

		internal OEM_SERIAL_NUMBERS OemSerialNumbers;

		internal SECTOR_BIT_FIELD_TB CLR_ConfigData;

		internal HAL_CONFIG_BLOCK FirstConfigBlock;
	};

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct HAL_CONFIG_BLOCK
	{
		public UInt32 Signature;
		public UInt32 HeaderCRC;
		public UInt32 DataCRC;
		public UInt32 Size;
		public fixed byte DriverName[64];

		public string DriverNameString
		{
			get
			{
				StringBuilder sb = new StringBuilder(66);

				fixed (byte* data = DriverName)
				{
					for (int i = 0; i < 64; i++)
					{
						if ((char)data[i] == '\0') break;
						sb.Append((char)data[i]);
					}
				}

				return sb.ToString();
			}

			set
			{
				fixed (byte* data = DriverName)
				{
					int len = value.Length;

					if (len > 64) len = 64;

					for (int i = 0; i < len; i++)
					{
						data[i] = (byte)value[i];
					}
				}
			}
		}
	}

	public interface IHAL_CONFIG_BASE
	{
		HAL_CONFIG_BLOCK ConfigHeader
		{
			get;
			set;
		}

		int Size
		{
			get;
		}
	}

}
