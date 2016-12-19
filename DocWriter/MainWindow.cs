//
// MainWindow.cs: contains the handling of the main shell.
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

using AppKit;
using Foundation;
using WebKit;

namespace DocWriter
{
	public partial class MainWindow : NSWindow, INSOutlineViewDelegate, IEditorWindow
	{
		// Called when created from unmanaged code
		public MainWindow (IntPtr handle) : base (handle)
		{
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindow (NSCoder coder) : base (coder)
		{
		}

		public string WindowPath {
			get {
				return (WindowController as MainWindowController).WindowPath;
			}
		}
		
		public DocModel DocModel {
			get {
				return (WindowController as MainWindowController).DocModel;
			}
		}

		DocNode currentObject;
		public DocNode CurrentObject {
			get { return currentObject; }
			set {
				if (currentObject != value) {
					if (currentObject != null) {
						this.SaveCurrentObject ();
					}

					currentObject = value;

					if (currentObject != null) {
						NSUserDefaults.StandardUserDefaults.SetString (currentObject.ReferenceString, "currentNode" + WindowPath);
					}

					SelectItem (currentObject);

					var ihtml = currentObject as IHtmlRender;
					if (ihtml != null) {
						string contents;
						try {
							contents = ihtml.Render ();
						} catch (Exception ex) {
							var text = System.Web.HttpUtility.HtmlEncode (ex.ToString ());
							contents = $"<body><p>Error Loading the contents for the new node<p>Exception:<p><pre>{text}</pre>";
						}
						webView.MainFrame.LoadHtmlString (contents, null);
					}
				}
			}
		}

		public void UpdateStatus (string status)
		{
			statusLabel.Cell.StringValue = status;
		}

		public string RunJS (string functionName, params string [] args)
		{
			return webView.StringByEvaluatingJavaScriptFromString ($"{functionName}({string.Join (", ", args.Select (a => $"\"{a}\""))})");
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			outline.DataSource = new DocumentTreeDataSource (DocModel);
			outline.WeakDelegate = this;
			Title = "DocWriter - " + WindowPath;

			// Restore the last node
			var lastOpenedNode = NSUserDefaults.StandardUserDefaults.StringForKey ("currentNode"+WindowPath);
			if (lastOpenedNode != null) {
				CurrentObject = DocModel.ParseReference (lastOpenedNode);
			}
			webView.DecidePolicyForNavigation += HandleDecidePolicyForNavigation;
			NSTimer.CreateRepeatingScheduledTimer (1, timer => this.CheckContents ());
		}
	
		void HandleDecidePolicyForNavigation (object sender, WebNavigationPolicyEventArgs e)
		{
			switch (e.OriginalUrl.Scheme){
			case "ecma":
				WebView.DecideIgnore (e.DecisionToken);
				var url = e.OriginalUrl.AbsoluteString.Substring (7);
				CurrentObject = DocModel.ParseReference (url);
				return;

				// This is one of our rendered ecma links, we want to extract the target
				// from the text, not the href attribute value (since this is not easily
				// editable, and the text is.
			case "goto":
				url = RunJS ("getText", e.OriginalUrl.Host);
				CurrentObject = DocModel.ParseReference (url);
				break;
			}
			WebView.DecideUse (e.DecisionToken);
		}

		bool SelectItem (DocNode docNode)
		{
			var docN = docNode as DocNamespace;
			if (docN != null) {
				outline.ExpandItem (docN);

				var row = outline.RowForItem (docN);
				outline.SelectRow (row, false);
				outline.ScrollRowToVisible (row);

				return true;
			}
			
			var docT = docNode as DocType;
			if (docT != null) {
				outline.ExpandItem (docT.Namespace);
				outline.ExpandItem (docT);

				var row = outline.RowForItem (docT);
				outline.SelectRow (row, false);
				outline.ScrollRowToVisible (row);

				return true;
			}

			var docM = docNode as DocMember;
			if (docM != null) {
				outline.ExpandItem (docM.Type.Namespace);
				outline.ExpandItem (docM.Type);
				outline.ExpandItem (docM);

				var row = outline.RowForItem (docM);
				outline.SelectRow (row, false);
				outline.ScrollRowToVisible (row);

				return true;
			}

			return false;
		}

		[Export("outlineViewSelectionDidChange:")]
		void SelectionDidChange (NSNotification notification)
		{
			var outlineView = (NSOutlineView) notification.Object;
			CurrentObject = (DocNode) outlineView.ItemAtRow (outlineView.SelectedRow);
		}
	}

	class DocumentTreeDataSource : NSOutlineViewDataSource {
		DocModel docModel;

		public DocumentTreeDataSource (DocModel docModel)
		{
			this.docModel = docModel;
		}

		public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			if (item == null)
				return docModel.NodeCount;

			if (item is DocNamespace)
				return (item as DocNamespace).NodeCount;

			if (item is DocType) 
				return ((item as DocType).NodeCount);

			return 0;
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			if (item == null || item is DocNamespace || item is DocType)
				return true;
			return false;
		}

		public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
		{

			if (item == null) 
				return docModel [(int)childIndex];

			if (item is DocNamespace) {
				var ds = item as DocNamespace;
				return ds [(int)childIndex];
			}
			if (item is DocType) {
				var dt = item as DocType;
				return dt [(int)childIndex];
			}
			return null;
		}

		public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			if (item is DocMember)
				return (NSString) ($"{(item as DocMember).Name} ({string.Join(", ", (item as DocMember).MemberParams.Select(p => p.Attribute("Type").Value))})");
			if (item is DocNode)
				return (NSString) (item as DocNode).Name;
			return new NSString ("Should not happen");
		}
	}
}

