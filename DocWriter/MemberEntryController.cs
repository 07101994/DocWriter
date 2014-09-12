
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace DocWriter
{
	public partial class MemberEntryController : MonoMac.AppKit.NSWindowController
	{
		#region Constructors

		// Called when created from unmanaged code
		public MemberEntryController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MemberEntryController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		internal AppDelegate appDelegate;

		// Call to load from the XIB/NIB file
		public MemberEntryController (AppDelegate appDelegate) : base ("MemberEntry")
		{
			this.appDelegate = appDelegate;
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		//strongly typed window accessor
		public new MemberEntry Window {
			get {
				return (MemberEntry)base.Window;
			}
		}
	}
}

