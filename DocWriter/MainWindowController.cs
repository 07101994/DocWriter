
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace DocWriter
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		public string Path { get; private set; }

		public MainWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public MainWindowController (string path) : base ("MainWindow")
		{
			Path = path;
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		//strongly typed window accessor
		public new MainWindow Window {
			get {
				return (MainWindow)base.Window;
			}
		}
	}
}

