// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace NetduinoFlasher.Mac
{
	[Register ("LeftViewController")]
	partial class LeftViewController
	{
		[Outlet]
		AppKit.NSTableView DeviceList { get; set; }

		[Action ("ClickDeviceList:")]
		partial void ClickDeviceList (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (DeviceList != null) {
				DeviceList.Dispose ();
				DeviceList = null;
			}
		}
	}
}
