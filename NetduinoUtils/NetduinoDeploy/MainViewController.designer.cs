// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace NetduinoDeploy
{
	[Register ("MainViewController")]
	partial class MainViewController
	{
		[Outlet]
		AppKit.NSTextField BootFileLabel { get; set; }

		[Outlet]
		AppKit.NSTextField ConfigFileLabel { get; set; }

		[Outlet]
		AppKit.NSButton DeployButton { get; set; }

		[Outlet]
		AppKit.NSPopUpButton DeviceType { get; set; }

		[Outlet]
		AppKit.NSTextField FirmwareStatus { get; set; }

		[Outlet]
		AppKit.NSTextField FlashFileLabel { get; set; }

		[Outlet]
		AppKit.NSTextField FreeSlots { get; set; }

		[Outlet]
		AppKit.NSTextField MacAddress { get; set; }

		[Outlet]
		AppKit.NSTextView Output { get; set; }

		[Outlet]
		AppKit.NSButton SaveConfigurationButton { get; set; }

		[Outlet]
		AppKit.NSButtonCell UpdateFirmwareButton { get; set; }

		[Action ("DeployAction:")]
		partial void DeployAction (Foundation.NSObject sender);

		[Action ("DeviceTypeChanged:")]
		partial void DeviceTypeChanged (Foundation.NSObject sender);

		[Action ("SaveConfiguration:")]
		partial void SaveConfiguration (Foundation.NSObject sender);

		[Action ("SelectFolderAction:")]
		partial void SelectFolderAction (Foundation.NSObject sender);

		[Action ("UpdateFirmwareAction:")]
		partial void UpdateFirmwareAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (ConfigFileLabel != null) {
				ConfigFileLabel.Dispose ();
				ConfigFileLabel = null;
			}

			if (DeployButton != null) {
				DeployButton.Dispose ();
				DeployButton = null;
			}

			if (DeviceType != null) {
				DeviceType.Dispose ();
				DeviceType = null;
			}

			if (FirmwareStatus != null) {
				FirmwareStatus.Dispose ();
				FirmwareStatus = null;
			}

			if (FlashFileLabel != null) {
				FlashFileLabel.Dispose ();
				FlashFileLabel = null;
			}

			if (FreeSlots != null) {
				FreeSlots.Dispose ();
				FreeSlots = null;
			}

			if (MacAddress != null) {
				MacAddress.Dispose ();
				MacAddress = null;
			}

			if (Output != null) {
				Output.Dispose ();
				Output = null;
			}

			if (SaveConfigurationButton != null) {
				SaveConfigurationButton.Dispose ();
				SaveConfigurationButton = null;
			}

			if (UpdateFirmwareButton != null) {
				UpdateFirmwareButton.Dispose ();
				UpdateFirmwareButton = null;
			}

			if (BootFileLabel != null) {
				BootFileLabel.Dispose ();
				BootFileLabel = null;
			}
		}
	}
}
