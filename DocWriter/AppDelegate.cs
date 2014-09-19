using System;
using System.Collections.Generic;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using System.Xml.Linq;
using System.Linq;
using System.IO;

namespace DocWriter
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		List<MainWindowController> windows = new List<MainWindowController> ();
		MainWindowController mainWindowController;

		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{
			NSUserDefaults.StandardUserDefaults.SetBool (true, "WebKitDeveloperExtras");
			NSUserDefaults.StandardUserDefaults.Synchronize ();

			var dirs = NSUserDefaults.StandardUserDefaults.StringArrayForKey ("LoadedDirectories");
			if (dirs == null || dirs.Length == 0)
				OpenDialog (this);
			else {
				foreach (var d in dirs.Distinct ()) {
					OpenDir (d);
				}
			}

			NSWindow.Notifications.ObserveWillClose ((a,b)=>{
				var target = b.Notification.Object as MainWindow;

				if (target != null){
					windows.Remove ((target.WindowController) as MainWindowController);
					SaveStatus ();
				}
			});

			NSWindow.Notifications.ObserveDidBecomeKey ((a, b) => {
				var target = b.Notification.Object as MainWindow;
				if (target != null){
					mainWindowController = target.WindowController as MainWindowController;
					Console.WriteLine ("Switching to {0}", target.Path);
				}
			});
		}

		void AppendNodeHtml (string html)
		{
			mainWindowController.Window.AppendNodeHtml (html);
		}

		void InsertHtml (string html, params object [] args)
		{
			mainWindowController.Window.InsertSpan (String.Format (html, args));
		}

		public DocModel DocModel {
			get {
				return mainWindowController.Window.DocModel;
			}
		}

		public MainWindow MainWindow {
			get {
				return mainWindowController.Window;
			}
		}


		partial void insertExample (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "c#"), new XText ("class Sample {"))));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		partial void insertCode (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "c#"), new XText ("class Sample {")));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		partial void insertFExample (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "F#"), new XText ("let sample = "))));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		partial void insertFCode (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "F#"), new XText ("let sample = ")));

			AppendNodeHtml (DocConverter.ToHtml (example, ""));
		}

		MemberEntryController mec;

		partial void insertReference (NSObject sender)
		{
			#if false
			// Work in progress
			if (mec == null)
				mec = new MemberEntryController (this);
			mec.ShowWindow (this);
			#else
			InsertHtml ("<a href=''>T:{0}</a>", MainWindow.SuggestTypeRef ());
			#endif
		}

		public void InsertReference (string text)
		{
			InsertHtml ("<a href=''>T:" + MainWindow.SuggestTypeRef () + "</a>");
		}

		[Export ("saveDocument:")]
		void SaveCurrentDocument (NSObject sender)
		{
			MainWindow.SaveCurrentObject ();
		}

		partial void insertList (NSObject sender)
		{
			var list = new XElement ("list", new XAttribute ("type", "bullet"),
				           new XElement ("item", new XElement ("term", new XText ("Text1"))),
				           new XElement ("item", new XElement ("term", new XText ("Text2"))));
				
			AppendNodeHtml (DocConverter.ToHtml (new XElement ("host", list), ""));
		}

		partial void insertUrl (NSObject sender)
		{
			string url = "http://www.xamarin.com";
			string caption = "Xamarin";
				
			InsertHtml (string.Format ("<div class='verbatim'><a href='{0}'>{1}</a></div>", url, caption));

		}

		partial void insertTable (NSObject sender)
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

		partial void selectionToLang (NSObject sender)
		{
			MainWindow.RunJS ("selectionToCode('langword')");
		}

		partial void selectionToParam (NSObject sender)
		{
			MainWindow.RunJS ("selectionToCode('paramref')");
		}

		partial void selectionToType (NSObject sender)
		{
			MainWindow.RunJS ("selectionToCode('typeparamref')");
		}

		public override void WillTerminate (NSNotification notification)
		{
			foreach (var wc in windows) {
				wc.Window.SaveCurrentObject ();
			}
		}

		void SaveStatus ()
		{
			NSUserDefaults.StandardUserDefaults ["LoadedDirectories"] = NSArray.FromStrings (windows.Select (wc => wc.Path).ToArray ());
		}

		void OpenDir (string dir)
		{
			var controller = new MainWindowController (dir);
			controller.Window.MakeKeyAndOrderFront (this);
			windows.Add (controller);
		}

		[Export ("openDocument:")]
		void OpenDialog (NSObject sender)
		{
			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = false;
			dlg.CanChooseDirectories = true;

			if (dlg.RunModal () == 1) {
				var url = dlg.Urls.FirstOrDefault ();
				if (url != null) {
					var path = url.Path;

					if (Directory.Exists (Path.Combine (path, "en")) && File.Exists (Path.Combine (path, "en", "index.xml"))) {
						OpenDir (Path.Combine (path, "en"));
						SaveStatus ();
					} else {
						var alert = new NSAlert () {
							AlertStyle = NSAlertStyle.Critical,
							InformativeText = "The selected directory is not the toplevel directory for ECMA XML documentation.   Those should contain a subdirectory en and a file en/index.xml",
							MessageText = "Not an ECMA XML Documentation Directory",
						};
						alert.RunModal ();
					}
				}
			}

		}
	}
}

