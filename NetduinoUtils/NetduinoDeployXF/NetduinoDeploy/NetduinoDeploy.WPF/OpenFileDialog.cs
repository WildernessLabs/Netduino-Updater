using NetduinoFirmware;

namespace NetduinoDeploy.WPF
{
    public class OpenFileDialog : IOpenFileDialog
    {
        public bool ShowDialog(string filter = null)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Multiselect = true
            };

            if (!string.IsNullOrWhiteSpace(filter))
                openFileDialog.Filter = filter;

            openFileDialog.ShowDialog();

            return true;
        }
    }
}