
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace DocWriter
{
	public partial class InsertUrl : AppKit.NSWindow
	{
		#region Constructors

		// Called when created from unmanaged code
		public InsertUrl (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public InsertUrl (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			targetUrl.StringValue = "http://";
		}
		#endregion

		public void GetParams (out string caption, out string url)
		{
			caption = this.caption.StringValue;
			url = this.targetUrl.StringValue;
		}
	}
}

