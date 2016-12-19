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
	[Register ("InsertUrlController")]
	partial class InsertUrlController
	{
		[Action ("cancel:")]
		partial void cancel (Foundation.NSObject sender);

		[Action ("ok:")]
		partial void ok (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
		}
	}

	[Register ("InsertUrl")]
	partial class InsertUrl
	{
		[Outlet]
		AppKit.NSTextField caption { get; set; }

		[Outlet]
		AppKit.NSTextField targetUrl { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (caption != null) {
				caption.Dispose ();
				caption = null;
			}

			if (targetUrl != null) {
				targetUrl.Dispose ();
				targetUrl = null;
			}
		}
	}
}
