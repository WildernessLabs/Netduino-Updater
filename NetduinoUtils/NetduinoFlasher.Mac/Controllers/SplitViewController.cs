using System;
using System.Collections.Generic;
using System.Diagnostics;
using AppKit;
using Foundation;

namespace NetduinoFlasher.Mac
{
	public partial class SplitViewController : NSSplitViewController
	{
		public Dictionary<int, RightViewController> DeviceViewControllers;
		public string LatestFirmwareVersion { get; set; }

		#region Computed Properties
		public override NSObject RepresentedObject
		{
			get
			{
				return base.RepresentedObject;
			}
			set
			{
				base.RepresentedObject = value;
				// Update the view, if already loaded.
			}
		}
		#endregion

		#region Constructors
		public SplitViewController(IntPtr handle) : base(handle)
		{
			DeviceViewControllers = new Dictionary<int, RightViewController>();
			LatestFirmwareVersion = "Checking for update";
		}
		#endregion

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			if (DeviceViewControllers.Count > 0)
			{
				this.RemoveSplitViewItem(this.SplitViewItems[1]);

				this.AddSplitViewItem(new NSSplitViewItem()
				{
					ViewController = DeviceViewControllers[0]
				});
				DeviceViewControllers[0].LoadGeneralDeviceInfo();
			}

			// Grab elements
			var left = LeftController.ViewController as LeftViewController;
			//var right = RightController.ViewController as RightViewController;

			// Wireup events
			left.SelectedDeviceChanged += (index) =>
			{
				this.RemoveSplitViewItem(this.SplitViewItems[1]);
				this.AddSplitViewItem(new NSSplitViewItem()
				{
					ViewController = DeviceViewControllers[index]
				});
			};

			left.FirmwareVersionChecked += (version) =>
			{
				LatestFirmwareVersion = version;
				foreach (var controller in DeviceViewControllers.Values)
				{
					controller.UpdateFirmwareVersionText(version);
				}
				Debug.WriteLine(version);
			};
		}
	}
}
