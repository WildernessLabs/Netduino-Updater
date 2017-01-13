using System;

using AppKit;
using Foundation;

namespace NetduinoFlasher.Mac
{
	public partial class ViewController : NSViewController
	{
		public ViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Do any additional setup after loading the view.
		}

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

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			// Create the Product Table Data Source and populate it
			var DataSource = new DeviceTableDataSource();

			//DataSource.Devices.Add(new Device("Netduino 2"));
			//DataSource.Devices.Add(new Device("Netduino 3"));

			DataSource.Devices.AddRange(DeviceHelper.GetAttachedDevices());

			// Populate the Product Table
			DeviceTable.DataSource = DataSource;
			DeviceTable.Delegate = new DeviceTableDelegate(DataSource);
		}
	}
}
