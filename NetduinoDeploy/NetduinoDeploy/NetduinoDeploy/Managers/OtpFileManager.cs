using NetduinoDeploy.Managers;

namespace NetduinoFirmware.Managers
{
    public static class OtpFileManager 
    {
        static void SaveConfiguration(byte productId, byte[] macAddress)
        {
            OtpSettings settings = new OtpSettings();
            settings.ProductID = productId;
            settings.MacAddress = macAddress;

            new OtpManager().SaveOtpSettings(settings);
        }
    }
}
