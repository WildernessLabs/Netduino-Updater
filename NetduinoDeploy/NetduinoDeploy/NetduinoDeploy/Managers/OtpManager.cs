using System;
using DfuSharp;

namespace NetduinoDeploy.Managers
{
    public class OtpManager
    {
        byte TOTAL_OTP_SLOTS = 4;
        byte CONFIGURATION_SIZE = 8;

        public OtpSettings GetOtpSettings()
        {
            OtpSettings settings = new OtpSettings();

            var configuration = ReadOtp();

            int iConfiguration;
            for (iConfiguration = 0; iConfiguration < TOTAL_OTP_SLOTS; iConfiguration++)
            {
                bool configurationIsEmpty = true;
                // make sure that the configuration is not all 0xFF
                for (int iConfigurationByteCheck = 0; iConfigurationByteCheck < CONFIGURATION_SIZE; iConfigurationByteCheck++)
                {
                    if (configuration[(iConfiguration * CONFIGURATION_SIZE) + iConfigurationByteCheck] != 0xFF)
                        configurationIsEmpty = false;
                }

                bool configurationValid = false;

                if (!configurationIsEmpty)
                {
                    for (int iConfigurationByteCheck = 0; iConfigurationByteCheck < CONFIGURATION_SIZE; iConfigurationByteCheck++)
                    {
                        if (configuration[(iConfiguration * CONFIGURATION_SIZE) + iConfigurationByteCheck] != 0)
                            configurationValid = true;
                    }

                    // make sure that the leading byte is 0xFF
                    if (configuration[(iConfiguration * CONFIGURATION_SIZE) + 0] != 0xFF)
                        configurationValid = false;
                }
                else
                {
                    settings.FreeSlots = (byte)(TOTAL_OTP_SLOTS - iConfiguration);
                    break;
                }

                if (configurationValid)
                {
                    settings.ProductID = configuration[(iConfiguration * CONFIGURATION_SIZE) + 1];
                    Array.Copy(configuration, (iConfiguration * CONFIGURATION_SIZE) + 2, settings.MacAddress, 0, 6);
                    settings.FreeSlots = (byte)(TOTAL_OTP_SLOTS - iConfiguration - 1);
                    break;
                }
            }

            return settings;
        }

        public bool SaveOtpSettings(OtpSettings settings)
        {
            var deviceConfiguration = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            deviceConfiguration[1] = Convert.ToByte(settings.ProductID);
            var macAddress = settings.MacAddress;
            Array.Copy(macAddress, 0, deviceConfiguration, 2, macAddress.Length);

            byte[] otpConfiguration = ReadOtp();

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

            //// create our configuration (8 bytes)
            //byte[] deviceConfiguration = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            //deviceConfiguration[1] = productID;
            //Array.Copy(macAddress, 0, deviceConfiguration, 2, macAddress.Length);

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

                    return WriteOtp(newOtpConfiguration);
                }
            }
            return false;
        }

        byte[] ReadOtp()
        {
            var devices = DfuContext.Current.GetDevices();
            DfuDevice device = devices[0];

            var configuration = new byte[32];

            device.Download(configuration, 0x1FFF7800, 2);
            return configuration;
        }

        bool WriteOtp(byte[] data)
        {
            var devices = DfuContext.Current.GetDevices();
            DfuDevice device = devices[0];

            device.Upload(data, 0x1FFF7800, 2);
            return true;
        }

        public bool SaveConfiguration(byte productId, byte[] macAddress)
        {
            var settings = new OtpSettings()
            {
                ProductID = productId,
                MacAddress = macAddress
            };

            return SaveOtpSettings(settings);
        }
    }

    public class OtpSettings
    {
        public OtpSettings()
        {
            MacAddress = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        }
        public byte ProductID { get; set; }
        public byte[] MacAddress { get; set; }
        public byte FreeSlots { get; set; }
    }
}