using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetduinoDeploy
{
	public partial class FirmwareView : ContentView
	{
		public FirmwareView ()
		{
			InitializeComponent ();

            BindingContext = new FirmwareViewModel();
		}
	}
}