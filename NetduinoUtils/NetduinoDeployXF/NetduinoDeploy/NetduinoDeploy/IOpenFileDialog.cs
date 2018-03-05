using System;
using System.Collections.Generic;
using System.Text;

namespace NetduinoDeploy
{
    interface IOpenFileDialog
    {
        bool ShowDialog(string filter);
    }
}
