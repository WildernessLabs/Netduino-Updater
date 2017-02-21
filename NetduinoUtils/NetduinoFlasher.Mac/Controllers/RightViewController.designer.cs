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
	[Register ("RightViewController")]
	partial class RightViewController
	{
		[Outlet]
		AppKit.NSTextField ApplicationText { get; set; }

		[Outlet]
		AppKit.NSTextField BuildDateText { get; set; }

		[Outlet]
		AppKit.NSTextField FirmwareText { get; set; }

		[Outlet]
		AppKit.NSButton FirmwareUpdateButtonButton { get; set; }

		[Outlet]
		AppKit.NSTextField FirmwareVersionText { get; set; }

		[Outlet]
		AppKit.NSTextField ManufacturerText { get; set; }

		[Outlet]
		AppKit.NSTextField ModelText { get; set; }

		[Action ("EraseApplicationButton:")]
		partial void EraseApplicationButton (Foundation.NSObject sender);

		[Action ("ExportApplicationImageButton:")]
		partial void ExportApplicationImageButton (Foundation.NSObject sender);

		[Action ("FirmwareUpdateButton:")]
		partial void FirmwareUpdateButton (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (ApplicationText != null) {
				ApplicationText.Dispose ();
				ApplicationText = null;
			}

			if (BuildDateText != null) {
				BuildDateText.Dispose ();
				BuildDateText = null;
			}

			if (FirmwareText != null) {
				FirmwareText.Dispose ();
				FirmwareText = null;
			}

			if (FirmwareVersionText != null) {
				FirmwareVersionText.Dispose ();
				FirmwareVersionText = null;
			}

			if (ManufacturerText != null) {
				ManufacturerText.Dispose ();
				ManufacturerText = null;
			}

			if (ModelText != null) {
				ModelText.Dispose ();
				ModelText = null;
			}

			if (FirmwareUpdateButtonButton != null) {
				FirmwareUpdateButtonButton.Dispose ();
				FirmwareUpdateButtonButton = null;
			}
		}
	}
}
