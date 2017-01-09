//
// MainWindowController.cs: contains the handling of the main shell.
//
// Author:
//   Miguel de Icaza (miguel@xamarin.com)
//   Matthew Leibowitz (matthew.leibowitz@xamarin.com)
//
// Copyright 2016 Xamarin Inc
//
//
using System;
using System.Linq;
using System.IO;

using AppKit;
using Foundation;

namespace DocWriter
{
	public partial class MainWindowController : AppKit.NSWindowController
	{
		public string WindowPath { get; private set; }
		
		public DocModel DocModel { get; private set; }

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
			DocModel = new DocModel (WindowPath);

			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		//strongly typed window accessor
		public IEditorWindow EditorWindow {
			get {
				return (MainWindow)base.Window;
			}
		}

		[Export ("saveDocument:")]
		void SaveCurrentDocument (NSObject sender) => EditorWindow.SaveCurrentObject ();

		[Export ("insertCode:")]
		void insertCode (NSObject sender) => EditorWindow.InsertCSharpCode();

		[Export ("insertExample:")]
		void insertExample (NSObject sender) => EditorWindow.InsertCSharpExample ();

		[Export ("insertFCode:")]
		void insertFCode (NSObject sender) => EditorWindow.InsertFSharpCode ();

		[Export ("insertFExample:")]
		void insertFExample (NSObject sender) => EditorWindow.InsertFSharpExample ();

		[Export ("insertH2:")]
		void InsertH2 (NSObject sender) => EditorWindow.InsertHtmlH2 ();

		[Export ("insertImage:")]
		void insertImage (NSObject sender)
		{
			var nodePath = EditorWindow.CurrentObject?.DocumentDirectory;
			if (nodePath == null)
				return;
			var nodeImageDir = Path.Combine (nodePath, "_images");

			try {
				if (!Directory.Exists (nodeImageDir))
					Directory.CreateDirectory (nodeImageDir);
			} catch { }

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

			EditorWindow.InsertImage (target);
		}

		[Export ("insertList:")]
		void insertList (NSObject sender) => EditorWindow.InsertList ();

		[Export("insertReference:")]
		void insertReference(NSObject sender)
		{
			#if false
			// Work in progress
			if (mec == null)
			mec = new MemberEntryController (this);
			mec.ShowWindow (this);
			#else
			EditorWindow.InsertReference ();
			#endif
		}


		[Export("insertTable:")]
		void insertTable (NSObject sender) => EditorWindow.InsertTable ();

		[Export ("insertUrl:")]
		void insertUrl (NSObject sender)
		{
			string url = "http://www.xamarin.com";
			string caption = "Xamarin";

			var urlController = new InsertUrlController (this);
			urlController.ShowWindow (this);
		}

		[Export("selectionToLang:")]
		void selectionToLang (NSObject sender) => EditorWindow.SelectionToCode( SelectionToCodeType.LangWord);

		[Export ("selectionToParam:")]
		void selectionToParam (NSObject sender) => EditorWindow.SelectionToCode (SelectionToCodeType.ParamRef);

		[Export ("selectionToType:")]
		void selectionToType (NSObject sender) => EditorWindow.SelectionToCode (SelectionToCodeType.TypeParamRef);

	}
}

