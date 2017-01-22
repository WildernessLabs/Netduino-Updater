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
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSTableColumn DeviceName { get; set; }

		[Outlet]
		AppKit.NSTableView DeviceTable { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator UpdateProgress { get; set; }

		[Outlet]
		AppKit.NSTextField UpdateStatus { get; set; }

		[Action ("ClickedUpdateFirmware:")]
		partial void ClickedUpdateFirmware (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (DeviceName != null) {
				DeviceName.Dispose ();
				DeviceName = null;
			}

			if (DeviceTable != null) {
				DeviceTable.Dispose ();
				DeviceTable = null;
			}

			if (UpdateProgress != null) {
				UpdateProgress.Dispose ();
				UpdateProgress = null;
			}

			if (UpdateStatus != null) {
				UpdateStatus.Dispose ();
				UpdateStatus = null;
			}
		}
	}
}
