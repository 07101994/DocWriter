
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Xml.Linq;
using System.IO;

namespace DocWriter
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		public string WindowPath { get; private set; }

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
			WindowPath = path;
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

		void InsertHtml (string html, params object [] args)
		{
			Window.InsertSpan (String.Format (html, args));
		}


		void AppendEcmaNode (XElement ecmaXml)
		{
			Window.AppendNode (ecmaXml);
		}

		void AppendPara ()
		{
			AppendEcmaNode (new XElement ("para", new XText (".")));

		}

		[Export ("saveDocument:")]
		void SaveCurrentDocument (NSObject sender)
		{
			Window.SaveCurrentObject ();
		}

		[Export ("insertCode:")]
		void insertCode (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "c#"), new XText ("class Sample {")));

			AppendEcmaNode (example);
			AppendPara ();
		}

		[Export ("insertExample:")]
		void insertExample (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "c#"), new XText ("class Sample {"))));

			AppendEcmaNode (example);
			AppendPara ();
		}

		[Export ("insertFCode:")]
		void insertFCode (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "F#"), new XText ("let sample = ")));

			AppendEcmaNode (example);
			AppendPara ();
		}

		[Export ("insertFExample:")]
		void insertFExample (NSObject sender)
		{
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "F#"), new XText ("let sample = "))));

			AppendEcmaNode (example);
			AppendPara ();
		}

		[Export ("insertH2:")]
		void InsertH2 (NSObject sender)
		{
			AppendEcmaNode (new XElement ("host", new XElement ("format", new XAttribute ("type", "text/html"), new XElement ("h2", new XText ("Header")))));
		}

		[Export ("insertImage:")]
		void insertImage (NSObject sender)
		{
			var nodePath = Window.CurrentNodePath;
			if (nodePath == null)
				return;
			var nodeImageDir = Path.Combine (nodePath, "_images");

			var dlg = NSOpenPanel.OpenPanel;
			dlg.CanChooseFiles = true;
			dlg.CanChooseDirectories = false;
			dlg.AllowsMultipleSelection = false;
			dlg.AllowedFileTypes = new string[] { "png", "jpg", "gif" };

			if (dlg.RunModal () != 1)
				return;

			if (dlg.Urls.Length == 0) 
				return;

			var path = dlg.Urls.FirstOrDefault ().Path;
			var target = Path.Combine (nodeImageDir, Path.GetFileName (path));

			if (File.Exists (target)) {
				var alert = new NSAlert () {
					MessageText = "Overwrite the existing image?",
					InformativeText = "There is already a file with the same name in the images folder, do you want to overwrite, or automatically rename the file?",
					AlertStyle = NSAlertStyle.Warning
				};
				alert.AddButton ("Overwrite");
				alert.AddButton ("Rename");
				var code = alert.RunModal ();
				switch (code){
				case 1000: // Overwrite
					break;
				case 1001: // Rename
					int i = 0;
					do {
						target = Path.Combine (nodeImageDir, Path.GetFileNameWithoutExtension (path) + i + Path.GetExtension (path));
						i++;
					} while (File.Exists (target));
					break;
				}
			}

			try {
				File.Copy (path, target);
			} catch (Exception e){
				var a = new NSAlert () {
					MessageText = "Failure to copy the file",
					InformativeText = e.ToString (),
					AlertStyle = NSAlertStyle.Critical
				};
				a.RunModal ();
				return;
			}
			InsertHtml ("<img src='{0}'>", target);
		}

		[Export ("insertList:")]
		void insertList (NSObject sender)
		{
			var list = new XElement ("list", new XAttribute ("type", "bullet"),
				new XElement ("item", new XElement ("term", new XText ("Text1"))),
				new XElement ("item", new XElement ("term", new XText ("Text2"))));

			AppendEcmaNode (new XElement ("host", list));
		}

		public void InsertReference (string text)
		{
			InsertHtml ("<a href=''>T:" + Window.SuggestTypeRef () + "</a>");
		}

		[Export ("insertReference:")]
		void insertReference (NSObject sender)
		{
			#if false
			// Work in progress
			if (mec == null)
			mec = new MemberEntryController (this);
			mec.ShowWindow (this);
			#else
			InsertHtml ("<a href=''>T:{0}</a>", Window.SuggestTypeRef ());
			#endif
		}

		[Export ("insertTable:")]
		void insertTable (NSObject sender)
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

			AppendEcmaNode (new XElement ("Host", table));
		}

		public void InsertUrl (string caption, string url)
		{
			InsertHtml (string.Format ("<div class='verbatim'><a href='{0}'>{1}</a></div>", url, caption));
		}

		[Export ("insertUrl:")]
		void insertUrl (NSObject sender)
		{
			string url = "http://www.xamarin.com";
			string caption = "Xamarin";

			var urlController = new InsertUrlController (this);
			urlController.ShowWindow (this);
		}

		[Export ("selectionToLang:")]
		void selectionToLang (NSObject sender)
		{
			Window.RunJS ("selectionToCode('langword')");
		}

		[Export ("selectionToParam:")]
		void selectionToParam (NSObject sender)
		{
			Window.RunJS ("selectionToCode('paramref')");
		}

		[Export ("selectionToType:")]
		void selectionToType (NSObject sender)
		{
			Window.RunJS ("selectionToCode('typeparamref')");
		}

	}
}

