// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace DocWriter
{
	[Register ("MemberEntry")]
	partial class MemberEntry
	{
		[Outlet]
		AppKit.NSTableView tableView { get; set; }

		[Outlet]
		AppKit.NSTextField textField { get; set; }

		[Action ("cancel:")]
		partial void cancel (Foundation.NSObject sender);

		[Action ("ok:")]
		partial void ok (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
			}

			if (textField != null) {
				textField.Dispose ();
				textField = null;
			}
		}
	}

	[Register ("MemberEntryController")]
	partial class MemberEntryController
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
