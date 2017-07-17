using System;
using DfuSharp;

namespace NetduinoFlasher.Mac
{
	class OtpSettings
	{
		//string _devicePath = "";

		byte TOTAL_OTP_SLOTS = 4;
		byte CONFIGURATION_SIZE = 8;

		//public OtpSettings(string devicePath)
		//{
		//	_devicePath = devicePath;
		//}

		public OtpSettings(DfuDevice device)
		{
			_device = device;
		}

		DfuDevice _device;

		public bool ReadSettings(out byte productID, out byte[] macAddress, out byte otpSlotsFree)
		{
			macAddress = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }; // default to empty address
			otpSlotsFree = 0; // default to "no slots free"
			productID = 0;

			//STDfuDevice device;
			//try
			//{
			//	device = new STDfuDevice(_devicePath);
			//}
			//catch
			//{
			//	return false;
			//}

			try
			{
				// set pointer to the OTP memory space
				_device.SetAddress(0x1FFF7800);
				//if (!success) return false;
				// request the first 32 bytes of OTP; this will contain our board type and MAC address
				// OTP format: {unused, boardType, macAddress5, macAddress4, macAddress3, macAddress2, macAddress1, macAddress0}
				// NOTE: the first eight bytes are the data; if those bytes are all 0x00 then the next step of eight bytes are the data.  Repeat up to 4 tries total to retrieve data.
				// netduino plus 2
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x05, 0x5C, 0x86, 0x4A, 0x00, 0xD0, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				// netduino plus 2 -- no MAC
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				// netduino 2
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x06, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				// netduino go
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x04, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				//device.WriteMemoryBlock(0, otpConfiguration);
				byte[] otpConfiguration = new byte[32];
				_device.Download(otpConfiguration);

				//if (!success) return false;
				int iConfiguration;
				for (iConfiguration = 0; iConfiguration < TOTAL_OTP_SLOTS; iConfiguration++)
				{
					bool configurationIsEmpty = true;
					// make sure that the configuration is not all 0xFF
					for (int iConfigurationByteCheck = 0; iConfigurationByteCheck < CONFIGURATION_SIZE; iConfigurationByteCheck++)
					{
						if (otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + iConfigurationByteCheck] != 0xFF)
							configurationIsEmpty = false;
					}

					bool configurationValid = false;
					if (!configurationIsEmpty)
					{
						for (int iConfigurationByteCheck = 0; iConfigurationByteCheck < CONFIGURATION_SIZE; iConfigurationByteCheck++)
						{
							if (otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + iConfigurationByteCheck] != 0)
								configurationValid = true;
						}
						// make sure that the leading byte is 0xFF
						if (otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + 0] != 0xFF)
							configurationValid = false;
					}
					else
					{
						otpSlotsFree = (byte)(TOTAL_OTP_SLOTS - iConfiguration);
						break;
					}

					if (configurationValid)
					{
						productID = otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + 1];
						Array.Copy(otpConfiguration, (iConfiguration * CONFIGURATION_SIZE) + 2, macAddress, 0, 6);
						otpSlotsFree = (byte)(TOTAL_OTP_SLOTS - iConfiguration - 1);
						break;
					}
				}
			}
			finally
			{
				//_device.Dispose();
			}

			return true;
		}

		public bool WriteSettings(byte productID, byte[] macAddress)
		{
			// validate arguments
			if (productID == 0xFF)
				throw new ArgumentOutOfRangeException("productID");
			if (macAddress == null || macAddress.Length != 6)
				throw new ArgumentOutOfRangeException("macAddress");

			//STDfuDevice device = new STDfuDevice(_devicePath);
			try
			{
				// set pointer to the OTP memory space
				_device.SetAddress(0x1FFF7800);
				// request the first 32 bytes of OTP; this will contain our board type and MAC address
				// OTP format: {unused, boardType, macAddress5, macAddress4, macAddress3, macAddress2, macAddress1, macAddress0}
				// NOTE: the first eight bytes are the data; if those bytes are all 0x00 then the next set of eight bytes are the data.  Repeat up to 4 tries total to retrieve data.
				// netduino plus 2
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x05, 0x5C, 0x86, 0x4A, 0x00, 0xD0, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				// netduino plus 2 -- no MAC
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x05, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				// netduino 2
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x06, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				// netduino go
				//byte[] otpConfiguration = new byte[32] { 0xFF, 0x04, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
				//                                         0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
				byte[] otpConfiguration = new byte[32];
				_device.Download(otpConfiguration);

				// now determine which slot is our current slot
				int otpCurrentSlot = -1; // -1 = no slot used yet
				byte otpSlotsFree = 0; // default to "no slots free"

				int iConfiguration;
				for (iConfiguration = 0; iConfiguration < TOTAL_OTP_SLOTS; iConfiguration++)
				{
					bool configurationIsEmpty = true;
					// make sure that the configuration is not all 0xFF
					for (int iConfigurationByteCheck = 0; iConfigurationByteCheck < CONFIGURATION_SIZE; iConfigurationByteCheck++)
					{
						if (otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + iConfigurationByteCheck] != 0xFF)
							configurationIsEmpty = false;
					}

					bool configurationValid = false;
					if (!configurationIsEmpty)
					{
						for (int iConfigurationByteCheck = 0; iConfigurationByteCheck < CONFIGURATION_SIZE; iConfigurationByteCheck++)
						{
							if (otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + iConfigurationByteCheck] != 0)
								configurationValid = true;
						}
						// make sure that the leading byte is 0xFF
						if (otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + 0] != 0xFF)
							configurationValid = false;
					}
					else
					{
						otpSlotsFree = (byte)(TOTAL_OTP_SLOTS - iConfiguration);
						otpCurrentSlot = iConfiguration;
						break;
					}

					if (configurationValid)
					{
						otpSlotsFree = (byte)(TOTAL_OTP_SLOTS - iConfiguration - 1);
						otpCurrentSlot = iConfiguration;
						break;
					}
				}

				// if we don't have any slots available, return false
				if (otpCurrentSlot == -1)
					return false;

				// create our configuration (8 bytes)
				byte[] deviceConfiguration = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
				deviceConfiguration[1] = productID;
				Array.Copy(macAddress, 0, deviceConfiguration, 2, macAddress.Length);

				// determine if we can re-use the current OTP block.  If our new settings set any bits to "1" then we will copy to the next configuration slot.
				for (iConfiguration = otpCurrentSlot; iConfiguration < TOTAL_OTP_SLOTS; iConfiguration++)
				{
					byte[] newOtpConfiguration = new byte[otpConfiguration.Length];
					Array.Copy(otpConfiguration, newOtpConfiguration, otpConfiguration.Length);
					Array.Copy(deviceConfiguration, 0, newOtpConfiguration, iConfiguration * CONFIGURATION_SIZE, deviceConfiguration.Length);
					// fill all previous slots with zeros (so they are marked as unused)
					for (int i = 0; i < iConfiguration * CONFIGURATION_SIZE; i++)
					{
						newOtpConfiguration[i] = 0;
					}
					bool slotCanBeUsed = true;
					// find out if we can write the new configuration in this slot (because it's either empty or we are only setting bits in a used region to 0s)
					for (int i = 0; i < deviceConfiguration.Length; i++)
					{
						byte oldOtpConfigurationByte = otpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + i];
						byte newOtpConfigurationByte = newOtpConfiguration[(iConfiguration * CONFIGURATION_SIZE) + i];
						if ((oldOtpConfigurationByte & newOtpConfigurationByte) != newOtpConfigurationByte)
						{
							// new configuration byte cannot be set by turning "1" bits to "0" bits.
							slotCanBeUsed = false;
						}
					}
					if (slotCanBeUsed)
					{
						// TODO BK
						//return _device.WriteMemoryBlock(0, newOtpConfiguration);
					}
				}
			}
			finally
			{
				//device.Dispose();
			}

			// if we get here, we could not find a slot to write the configuration.
			return false;
		}
	}
}
