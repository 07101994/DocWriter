
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace DocWriter
{
	public partial class InsertUrlController : AppKit.NSWindowController
	{
		MainWindowController mwc;
		#region Constructors

		public InsertUrlController (MainWindowController mwc) : base ("InsertUrl")
		{
			this.mwc = mwc;
			Initialize ();
		}
		 
		// Called when created from unmanaged code
		public InsertUrlController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public InsertUrlController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		//strongly typed window accessor
		public new InsertUrl Window {
			get {
				return (InsertUrl)base.Window;
			}
		}

		partial void cancel (NSObject sender)
		{
			Window.OrderOut (Window);
		}

		partial void ok (NSObject sender)
		{
			string title, url;

			Window.GetParams (out title, out url);
			mwc.EditorWindow.InsertUrl (title, url);
			Close ();
		}
	}
}

