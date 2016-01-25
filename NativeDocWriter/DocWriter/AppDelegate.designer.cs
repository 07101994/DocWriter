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
	[Register ("AppDelegate")]
	partial class AppDelegate
	{
		[Outlet]
		MonoMac.AppKit.NSMenuItem open { get; set; }

		[Action ("insertCode:")]
		partial void insertCode (MonoMac.Foundation.NSObject sender);

		[Action ("insertExample:")]
		partial void insertExample (MonoMac.Foundation.NSObject sender);

		[Action ("insertFCode:")]
		partial void insertFCode (MonoMac.Foundation.NSObject sender);

		[Action ("insertFExample:")]
		partial void insertFExample (MonoMac.Foundation.NSObject sender);

		[Action ("insertImage:")]
		partial void insertImage (MonoMac.Foundation.NSObject sender);

		[Action ("insertList:")]
		partial void insertList (MonoMac.Foundation.NSObject sender);

		[Action ("insertReference:")]
		partial void insertReference (MonoMac.Foundation.NSObject sender);

		[Action ("insertTable:")]
		partial void insertTable (MonoMac.Foundation.NSObject sender);

		[Action ("insertUrl:")]
		partial void insertUrl (MonoMac.Foundation.NSObject sender);

		[Action ("selectionToLang:")]
		partial void selectionToLang (MonoMac.Foundation.NSObject sender);

		[Action ("selectionToParam:")]
		partial void selectionToParam (MonoMac.Foundation.NSObject sender);

		[Action ("selectionToType:")]
		partial void selectionToType (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (open != null) {
				open.Dispose ();
				open = null;
			}
		}
	}
}
