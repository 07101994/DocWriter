using System;
using MonoMac.Foundation;

namespace DocWriter
{
	public partial class DocNode : NSObject
	{
		// This is an NSString because we use it as a value that we store in a NSOutlineView
		NSString _name;
		public NSString Name {
			get { return _name; }
			set {
				_name = value;
				CName = value.ToString ();
			}
		}
	}
}

