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
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSPopUpButton Authentication { get; set; }

		[Outlet]
		AppKit.NSTextField BootFileLabel { get; set; }

		[Outlet]
		AppKit.NSTextField ConfigFileLabel { get; set; }

		[Outlet]
		AppKit.NSTextField DefaultGateway { get; set; }

		[Outlet]
		AppKit.NSButton DeployButton { get; set; }

		[Outlet]
		AppKit.NSPopUpButton DeviceType { get; set; }

		[Outlet]
		AppKit.NSButton EnableDHCP { get; set; }

		[Outlet]
		AppKit.NSButton EncryptConfig { get; set; }

		[Outlet]
		AppKit.NSPopUpButton Encryption { get; set; }

		[Outlet]
		AppKit.NSTextField FirmwareStatus { get; set; }

		[Outlet]
		AppKit.NSTextField FlashFileLabel { get; set; }

		[Outlet]
		AppKit.NSTextField FreeSlots { get; set; }

		[Outlet]
		AppKit.NSTextField MacAddress { get; set; }

		[Outlet]
		AppKit.NSPopUpButton NetworkKey { get; set; }

		[Outlet]
		AppKit.NSTextField NetworkMacAddress { get; set; }

		[Outlet]
		AppKit.NSButton NetworkUpdateButton { get; set; }

		[Outlet]
		AppKit.NSTextField NetworkValue { get; set; }

		[Outlet]
		AppKit.NSTextView Output { get; set; }

		[Outlet]
		AppKit.NSTextField Passphrase { get; set; }

		[Outlet]
		AppKit.NSTextField PrimaryDNS { get; set; }

		[Outlet]
		AppKit.NSButton RadioA { get; set; }

		[Outlet]
		AppKit.NSButton RadioB { get; set; }

		[Outlet]
		AppKit.NSButton RadioG { get; set; }

		[Outlet]
		AppKit.NSButton RadioN { get; set; }

		[Outlet]
		AppKit.NSTextField ReKeyInternal { get; set; }

		[Outlet]
		AppKit.NSButton SaveConfigurationButton { get; set; }

		[Outlet]
		AppKit.NSTextField SecondaryDNS { get; set; }

		[Outlet]
		AppKit.NSTextField SSID { get; set; }

		[Outlet]
		AppKit.NSTextField StaticIPAddress { get; set; }

		[Outlet]
		AppKit.NSTextField SubnetMask { get; set; }

		[Outlet]
		AppKit.NSButtonCell UpdateFirmwareButton { get; set; }

		[Action ("AuthenticationChanged:")]
		partial void AuthenticationChanged (Foundation.NSObject sender);

		[Action ("DeployAction:")]
		partial void DeployAction (Foundation.NSObject sender);

		[Action ("DeviceTypeChanged:")]
		partial void DeviceTypeChanged (Foundation.NSObject sender);

		[Action ("EnableDHCPChanged:")]
		partial void EnableDHCPChanged (Foundation.NSObject sender);

		[Action ("EncryptionChanged:")]
		partial void EncryptionChanged (Foundation.NSObject sender);

		[Action ("NetworkCancel:")]
		partial void NetworkCancel (Foundation.NSObject sender);

		[Action ("NetworkKeyChanged:")]
		partial void NetworkKeyChanged (Foundation.NSObject sender);

		[Action ("NetworkUpdate:")]
		partial void NetworkUpdate (Foundation.NSObject sender);

		[Action ("SaveConfiguration:")]
		partial void SaveConfiguration (Foundation.NSObject sender);

		[Action ("SelectFolderAction:")]
		partial void SelectFolderAction (Foundation.NSObject sender);

		[Action ("UpdateFirmwareAction:")]
		partial void UpdateFirmwareAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (NetworkUpdateButton != null) {
				NetworkUpdateButton.Dispose ();
				NetworkUpdateButton = null;
			}

			if (Authentication != null) {
				Authentication.Dispose ();
				Authentication = null;
			}

			if (BootFileLabel != null) {
				BootFileLabel.Dispose ();
				BootFileLabel = null;
			}

			if (ConfigFileLabel != null) {
				ConfigFileLabel.Dispose ();
				ConfigFileLabel = null;
			}

			if (DefaultGateway != null) {
				DefaultGateway.Dispose ();
				DefaultGateway = null;
			}

			if (DeployButton != null) {
				DeployButton.Dispose ();
				DeployButton = null;
			}

			if (DeviceType != null) {
				DeviceType.Dispose ();
				DeviceType = null;
			}

			if (EnableDHCP != null) {
				EnableDHCP.Dispose ();
				EnableDHCP = null;
			}

			if (EncryptConfig != null) {
				EncryptConfig.Dispose ();
				EncryptConfig = null;
			}

			if (Encryption != null) {
				Encryption.Dispose ();
				Encryption = null;
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

			if (NetworkKey != null) {
				NetworkKey.Dispose ();
				NetworkKey = null;
			}

			if (NetworkMacAddress != null) {
				NetworkMacAddress.Dispose ();
				NetworkMacAddress = null;
			}

			if (NetworkValue != null) {
				NetworkValue.Dispose ();
				NetworkValue = null;
			}

			if (Output != null) {
				Output.Dispose ();
				Output = null;
			}

			if (Passphrase != null) {
				Passphrase.Dispose ();
				Passphrase = null;
			}

			if (PrimaryDNS != null) {
				PrimaryDNS.Dispose ();
				PrimaryDNS = null;
			}

			if (RadioA != null) {
				RadioA.Dispose ();
				RadioA = null;
			}

			if (RadioB != null) {
				RadioB.Dispose ();
				RadioB = null;
			}

			if (RadioG != null) {
				RadioG.Dispose ();
				RadioG = null;
			}

			if (RadioN != null) {
				RadioN.Dispose ();
				RadioN = null;
			}

			if (ReKeyInternal != null) {
				ReKeyInternal.Dispose ();
				ReKeyInternal = null;
			}

			if (SaveConfigurationButton != null) {
				SaveConfigurationButton.Dispose ();
				SaveConfigurationButton = null;
			}

			if (SecondaryDNS != null) {
				SecondaryDNS.Dispose ();
				SecondaryDNS = null;
			}

			if (SSID != null) {
				SSID.Dispose ();
				SSID = null;
			}

			if (StaticIPAddress != null) {
				StaticIPAddress.Dispose ();
				StaticIPAddress = null;
			}

			if (SubnetMask != null) {
				SubnetMask.Dispose ();
				SubnetMask = null;
			}

			if (UpdateFirmwareButton != null) {
				UpdateFirmwareButton.Dispose ();
				UpdateFirmwareButton = null;
			}
		}
	}
}
