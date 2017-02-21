using System;
using AppKit;

namespace NetduinoFlasher.Mac
{
	public class DeviceTableDelegate : NSTableViewDelegate
	{
		public DeviceTableDelegate()
		{
		}

		private const string CellIdentifier = "ProdCell";

		private DeviceTableDataSource DataSource;

		public DeviceTableDelegate(DeviceTableDataSource datasource)
		{
			this.DataSource = datasource;
		}

		public override NSView GetViewForItem(NSTableView tableView, NSTableColumn tableColumn, nint row)
		{
			// This pattern allows you reuse existing views when they are no-longer in use.
			// If the returned view is null, you instance up a new view
			// If a non-null view is returned, you modify it enough to reflect the new data
			NSTextField view = (NSTextField)tableView.MakeView(CellIdentifier, this);
			if (view == null)
			{
				view = new NSTextField();
				view.Identifier = CellIdentifier;
				view.BackgroundColor = NSColor.Clear;
				view.Bordered = false;
				view.Selectable = false;
				view.Editable = false;
			}

			// Setup view based on the column selected
			switch (tableColumn.Title)
			{
				case "Name":
					view.StringValue = DataSource.Devices[(int)row].Product;
					break;
				case "VendorID":
					view.IntValue = DataSource.Devices[(int)row].VendorID;
					break;
				case "ProductID":
					view.IntValue = DataSource.Devices[(int)row].ProductID;
					break;

			}

			return view;
		}
	}
}
