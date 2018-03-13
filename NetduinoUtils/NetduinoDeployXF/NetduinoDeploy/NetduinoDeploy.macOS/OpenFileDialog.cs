using System.Linq;
using System.Collections.Generic;
using AppKit;

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
            var extensions = GetFileExtensionsFromFilter(filter).ToArray();

            var dlg = NSOpenPanel.OpenPanel;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;
            dlg.AllowedFileTypes = extensions;
            dlg.AllowsMultipleSelection = true;

            if(dlg.RunModal() == 1)
            {
                FileNames = dlg.Urls.Select(u => u.Path).ToArray();
            }
            else
            {
                FileNames = new string[0];
            }

            return (FileNames.Length > 0) ? true : false;
        }

        List<string> GetFileExtensionsFromFilter(string filter)
        {
            var results = new List<string>();

            int index = 0;

            while(true)
            {
                index = filter.IndexOf("|*.", index);

                if (index == -1)
                    break;

                var extension = filter.Substring(index += 3, 3);
                results.Add(extension);
            }

            return results;
        }
    }
}
