namespace NetduinoDeploy.WPF
{
    public class OpenFileDialog : IOpenFileDialog
    {
        public string[] FileNames { get; private set; }

        public bool ShowDialog(string filter = null)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Multiselect = true
            };

            if (!string.IsNullOrWhiteSpace(filter))
                openFileDialog.Filter = filter;

            openFileDialog.ShowDialog();

            FileNames = new string[openFileDialog.FileNames.Length];

            for(int i = 0; i < openFileDialog.FileNames.Length; i++)
                FileNames[i] = openFileDialog.FileNames[i];

            return (FileNames.Length > 0) ? true : false;
        }
    }
}