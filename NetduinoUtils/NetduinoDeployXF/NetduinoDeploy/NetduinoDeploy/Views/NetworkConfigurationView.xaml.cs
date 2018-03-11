using NetduinoDeploy.Managers;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetduinoDeploy
{
	public partial class NetworkConfigurationView : ContentView
	{
		public NetworkConfigurationView ()
		{
            BindingContext = NetworkConfigurationViewModel.GetInstance();

			InitializeComponent ();
		}
	}
}