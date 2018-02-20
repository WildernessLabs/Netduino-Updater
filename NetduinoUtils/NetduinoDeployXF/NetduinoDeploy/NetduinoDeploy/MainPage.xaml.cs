using System.Diagnostics;
using Xamarin.Forms;

namespace NetduinoDeploy
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
            DfuContext.Init();

            InitializeComponent();

            bool hasCapabilities = DfuContext.Current.HasCapability(DfuSharp.Capabilities.HasCapabilityAPI);
            Debug.WriteLine($"Has capabilities: {hasCapabilities}");

            if (hasCapabilities)
            {
                bool hasHotPlug = DfuContext.Current.HasCapability(DfuSharp.Capabilities.SupportsHotplug);

                Debug.WriteLine($"Has hotplug support: {hasHotPlug}");
                DfuContext.Current.BeginListeningForHotplugEvents();
            }
        }
	}
}