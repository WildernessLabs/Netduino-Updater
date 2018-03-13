using System;
namespace NetduinoDeploy.macOS
{
    public class OpenFileDialog : IOpenFileDialog
    {
        public OpenFileDialog()
        {
        }

        public string[] FileNames { get; set; }

        public bool ShowDialog(string filter)
        {
            return false;
        }
    }
}
