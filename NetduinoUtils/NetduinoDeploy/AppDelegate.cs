using AppKit;
using Foundation;
using NetduinoDeploy.Managers;
using System.Diagnostics;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

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


			bool hasCapsYo = DfuContext.Current.HasCapability(DfuSharp.Capabilities.HasCapabilityAPI);
			Debug.WriteLine("Has capabilities: " + hasCapsYo.ToString());

			if (hasCapsYo)
			{
				bool hazHotPrug = DfuContext.Current.HasCapability(DfuSharp.Capabilities.SupportsHotplug);
				Debug.WriteLine("Haz Hotprug? " + hazHotPrug.ToString());

                DfuContext.Current.BeginListeningForHotplugEvents();
			}


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
