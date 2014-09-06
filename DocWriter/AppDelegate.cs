using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using System.Xml.Linq;

namespace DocWriter
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		MainWindowController mainWindowController;

		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{
			NSUserDefaults.StandardUserDefaults.SetBool (true, "WebKitDeveloperExtras");
			NSUserDefaults.StandardUserDefaults.Synchronize ();

			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);
		}

		[Export ("insertExample:")]
		void InsertExample (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "c#"), new XText ("..."))));

			mainWindowController.Window.InsertHtml (DocConverter.ToHtml (example, ""));
		}

		[Export ("insertReference:")]
		void InsertReference (NSObject sender)
		{
		}
	}
}

