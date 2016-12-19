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
	[Register ("AppDelegate")]
	partial class AppDelegate
	{
		[Outlet]
		AppKit.NSMenuItem open { get; set; }

		[Action ("insertCode:")]
		partial void insertCode (Foundation.NSObject sender);

		[Action ("insertExample:")]
		partial void insertExample (Foundation.NSObject sender);

		[Action ("insertFCode:")]
		partial void insertFCode (Foundation.NSObject sender);

		[Action ("insertFExample:")]
		partial void insertFExample (Foundation.NSObject sender);

		[Action ("insertImage:")]
		partial void insertImage (Foundation.NSObject sender);

		[Action ("insertList:")]
		partial void insertList (Foundation.NSObject sender);

		[Action ("insertReference:")]
		partial void insertReference (Foundation.NSObject sender);

		[Action ("insertTable:")]
		partial void insertTable (Foundation.NSObject sender);

		[Action ("insertUrl:")]
		partial void insertUrl (Foundation.NSObject sender);

		[Action ("selectionToLang:")]
		partial void selectionToLang (Foundation.NSObject sender);

		[Action ("selectionToParam:")]
		partial void selectionToParam (Foundation.NSObject sender);

		[Action ("selectionToType:")]
		partial void selectionToType (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (open != null) {
				open.Dispose ();
				open = null;
			}
		}
	}
}
