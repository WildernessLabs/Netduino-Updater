using Xamarin.Forms;

namespace NetduinoDeploy
{
	public partial class OneTimeSettingsView : ContentView
	{
		public OneTimeSettingsView ()
		{
            var vm = NetworkConfigurationViewModel.GetInstance();

            BindingContext = vm;

            InitializeComponent ();
		}
	}
}