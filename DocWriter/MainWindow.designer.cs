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
	[Register ("MainWindow")]
	partial class MainWindow
	{
		[Outlet]
		MonoMac.AppKit.NSTextField lookupEntry { get; set; }

		[Outlet]
		MonoMac.AppKit.NSOutlineView outline { get; set; }

		[Outlet]
		MonoMac.AppKit.NSTextField statusLabel { get; set; }

		[Outlet]
		MonoMac.WebKit.WebView webView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (lookupEntry != null) {
				lookupEntry.Dispose ();
				lookupEntry = null;
			}

			if (outline != null) {
				outline.Dispose ();
				outline = null;
			}

			if (webView != null) {
				webView.Dispose ();
				webView = null;
			}

			if (statusLabel != null) {
				statusLabel.Dispose ();
				statusLabel = null;
			}
		}
	}

	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		
		void ReleaseDesignerOutlets ()
		{
		}
	}
}
