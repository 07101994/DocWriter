// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;
using System.CodeDom.Compiler;

namespace DocWriter
{
	[Register ("MemberEntry")]
	partial class MemberEntry
	{
		[Outlet]
		MonoMac.AppKit.NSButton cancel { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTableView tableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField textField { get; set; }

		[Action ("ok:")]
		partial void ok (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (cancel != null) {
				cancel.Dispose ();
				cancel = null;
			}

			if (textField != null) {
				textField.Dispose ();
				textField = null;
			}

			if (tableView != null) {
				tableView.Dispose ();
				tableView = null;
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
