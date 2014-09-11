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

		void AppendNodeHtml (string html)
		{
			mainWindowController.Window.AppendNodeHtml (html);
		}

		void InsertHtml (string html)
		{
			mainWindowController.Window.InsertSpan (html);
		}

		[Export ("insertExample:")]
		void InsertExample (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "c#"), new XText ("class Sample {"))));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		[Export ("insertCode:")]
		void InsertCode (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "c#"), new XText ("class Sample {")));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		[Export ("insertFExample:")]
		void InsertFExample (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "F#"), new XText ("let sample = "))));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		[Export ("insertFCode:")]
		void InsertFCode (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "F#"), new XText ("let sample = ")));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		[Export ("insertReference:")]
		void InsertReference (NSObject sender)
		{
			InsertHtml ("<a href=''>T:Type.Name</a>");
		}

		[Export ("saveDocument:")]
		void SaveCurrentDocument (NSObject sender)
		{
			mainWindowController.Window.SaveCurrentObject ();
		}

		[Export ("insertList:")]
		void InsertList (NSObject sender)
		{
			var list = new XElement ("list", new XAttribute ("type", "bullet"),
				           new XElement ("item", new XElement ("term", new XText ("Text1"))),
				           new XElement ("item", new XElement ("term", new XText ("Text2"))));
				
			AppendNodeHtml (DocConverter.ToHtml (new XElement ("host", list), ""));
		}

		[Export ("insertTable:")]
		void InsertTable (NSObject sender)
		{
			var table = new XElement ("list", new XAttribute ("type", "table"),
				           new XElement ("listheader", 
					           new XElement ("term", new XText ("Term")),
					           new XElement ("description", new XText ("Description"))),
				           new XElement ("item", 
					           new XElement ("term", new XText ("Term1")),
					           new XElement ("description", new XText ("Description1"))),
				           new XElement ("item", 
					           new XElement ("term", new XText ("Term2")),
					           new XElement ("description", new XText ("Description2"))));

			string a = DocConverter.ToHtml (new XElement ("Host", table), "");
			AppendNodeHtml (a);
		}

		public override void WillTerminate (NSNotification notification)
		{
			mainWindowController.Window.SaveCurrentObject ();
		}
	}
}

