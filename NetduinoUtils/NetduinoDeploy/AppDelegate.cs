using AppKit;
using Foundation;
using NetduinoDeploy.Managers;

namespace NetduinoDeploy
{
	[Register("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		public AppDelegate()
		{
		}

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            DfuContext.Init();
        }

		public override void DidFinishLaunching(NSNotification notification)
		{
            // Insert code here to initialize your application
		}

		public override void WillTerminate(NSNotification notification)
		{
            // Insert code here to tear down your application
            //DeviceManager.Exit();
            DfuContext.Dispose();
		}
	}
}
