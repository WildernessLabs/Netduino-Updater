using System;
using System.Collections.Generic;
using AppKit;
using NetduinoFlasher.Mac.Models;

namespace NetduinoFlasher.Mac
{
	public class DeviceTableDataSource : NSTableViewDataSource
	{
		public DeviceTableDataSource()
		{
		}

		public List<Device> Devices = new List<Device>();


		public override nint GetRowCount(NSTableView tableView)
		{
			return Devices.Count;
		}

	}
}
