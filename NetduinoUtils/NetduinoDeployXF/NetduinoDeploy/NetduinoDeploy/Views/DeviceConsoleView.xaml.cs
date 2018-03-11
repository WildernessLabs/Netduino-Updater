using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetduinoDeploy
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DeviceConsoleView : ContentView
	{
        DeviceConsoleViewModel vm = new DeviceConsoleViewModel();

		public DeviceConsoleView ()
		{
            this.BindingContext = vm;

            InitializeComponent ();
		}
	}
}