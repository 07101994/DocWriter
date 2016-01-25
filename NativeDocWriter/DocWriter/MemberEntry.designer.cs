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
		MonoMac.AppKit.NSTableView tableView { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField textField { get; set; }

		[Action ("cancel:")]
		partial void cancel (MonoMac.Foundation.NSObject sender);

		[Action ("ok:")]
		partial void ok (MonoMac.Foundation.NSObject sender);
		
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
