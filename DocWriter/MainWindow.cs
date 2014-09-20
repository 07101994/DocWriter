//
// MainWindow.cs: contains the handling of the main shell.
//
// Author:
//   Miguel de Icaza (miguel@xamarin.com)
//
// Copyright 2014 Xamarin Inc
//
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Text;
using System.Xml.Linq;

namespace DocWriter
{
	public partial class MainWindow : MonoMac.AppKit.NSWindow, IWebView
	{
		public DocModel DocModel;
		bool loading;

		// Called when created from unmanaged code
		public MainWindow (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindow (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		public string Path {
			get {
				return (WindowController as MainWindowController).WindowPath;
			}
		}

		// Shared initialization code
		void Initialize ()
		{
			DocModel = new DocModel (Path);
		}


		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			outline.DataSource = new DocumentTreeDataSource (DocModel);
			outline.Delegate = new DocumentTreeDelegate (this);
			Title = "DocWriter - " + Path;

			// Restore the last node
			var lastOpenedNode = NSUserDefaults.StandardUserDefaults.StringForKey ("currentNode"+Path);
			if (lastOpenedNode != null) {
				int nsidx = -1;
				int typeidx = -1;
				var components = lastOpenedNode.Split ('/');
				if (components.Length > 0)
					nsidx = SelectNamespace (components [0]);
				if (components.Length > 1) 
					typeidx = SelectType (nsidx, components [1]);
				if (components.Length > 2)
					SelectMember (nsidx, typeidx, components [2]);
			}

			NSTimer.CreateRepeatingScheduledTimer (1, CheckContents);
		}
	
		int SelectNamespace (string name)
		{
			for (int n = DocModel.NodeCount, i = 0; i < n; i++) {
				if (DocModel [i].CName == name) {
					outline.SelectRow (i, false);
					return i;
				}
			}
			return 0;
		}

		int SelectType (int nsidx, string type)
		{
			DocNamespace ns = DocModel [nsidx];
			outline.ExpandItem (ns);
			for (int n = ns.NodeCount, i = 0; i < n; i++){
				if (ns [i].CName == type) {
					var r = outline.RowForItem (ns [i]);
					outline.SelectRow (r, false);
					return i;
				}
			}
			return 0;
		}

		void SelectMember (int nsidx, int typeidx, string name)
		{
			DocType type = DocModel [nsidx][typeidx];
			outline.ExpandItem (type);
			for (int n = type.NodeCount, i = 0; i < n; i++){
				if (type [i].CName == name) {
					var r = outline.RowForItem (type [i]);
					outline.SelectRow (r, false);
					return;
				}
			}
		}


		public string RunJS (string code)
		{
			return webView.StringByEvaluatingJavaScriptFromString (code);
		}

		public string Fetch (string id)
		{
			var element = RunJS ("getHtml(\"" + id + "\")");
			if (element.StartsWith ("<<<<")) {
				Console.WriteLine ("Failure to fetch contents of {0}", id);
			}
			return element;
		}

		NSObject currentObject;

		void CheckContents ()
		{
			var dirtyNodes = RunJS ("getDirtyNodes ()").Split (new char [] {' '}, StringSplitOptions.RemoveEmptyEntries);
			if (dirtyNodes.Length == 0)
				return;

			if (currentObject != null && currentObject is IEditableNode) {
				string result;

				try {
					result = (currentObject as IEditableNode).ValidateChanges (this, dirtyNodes);
				} catch (Exception e){
					result = e.ToString ();
				}

				if (result != null)
					statusLabel.Cell.StringValue = result;
				else
					statusLabel.Cell.StringValue = "OK";
			}
		}

		public void SaveCurrentObject ()
		{
			var editable = currentObject as IEditableNode;
			if (editable != null) {
				string error;

				CheckContents ();

				if (!editable.Save (this, out error)) {
					// FIXME: popup a window or something.
				}
			}
		}

		// Suggests a name for the reference based on the current context
		public string SuggestTypeRef ()
		{
			var cns = currentObject as DocNamespace;
			if (cns != null)
				return cns.Name;
			var ctype = currentObject as DocType;
			if (ctype != null)
				return ctype.Namespace.Name + "." + ctype.Name;
			var cm = currentObject as DocMember;
			if (cm != null)
				return cm.Type.Namespace.Name + "." + cm.Type.Name;
			return "MonoTouch.";
		}

		void SelectionChanged ()
		{
			SaveCurrentObject ();
			currentObject = null;

			currentObject = outline.ItemAtRow (outline.SelectedRow);
			var docNode = currentObject as DocNode;
			if (docNode != null) {
				NSUserDefaults.StandardUserDefaults.SetString (docNode.DocPath, "currentNode"+Path);
			}

			var ihtml = currentObject as IHtmlRender;
			if (ihtml == null)
				return;

			string contents;
			try {
				contents = ihtml.Render ();
			} catch (Exception e){
				contents = String.Format ("<body><p>Error Loading the contents for the new node<p>Exception:<p><pre>{0}</pre>", System.Web.HttpUtility.HtmlEncode (e.ToString ()));
			}
			webView.MainFrame.LoadHtmlString (contents, NSBundle.MainBundle.ResourceUrl);
		}

		static string EscapeHtml (string html)
		{
			var sb = new StringBuilder ();

			foreach (char c in html) {
				if (c == '\n')
					sb.Append ("\\\n");
				else
					sb.Append (c);
			}
			return sb.ToString ();
		}

		public void InsertSpan (string html)
		{
			webView.StringByEvaluatingJavaScriptFromString ("insertSpanAtCursor(\"" + EscapeHtml (html) + "\")");
		}

		//
		// Turns the provided ECMA XML node into HTML and appends it to the current node on the
		// rendered HTML.
		//
		public void AppendNode (XElement ecmaXml)
		{
			DocNode docNode = currentObject as DocNode;
			if (docNode != null) {
				var html = DocConverter.ToHtml (ecmaXml, "", docNode.DocumentDirectory);
				webView.StringByEvaluatingJavaScriptFromString ("insertHtmlAfterCurrentNode(\"" + EscapeHtml (html) + "\")");
			}
		}

		public string CurrentNodePath {
			get {
				DocNode docNode = currentObject as DocNode;
				if (docNode == null)
					return null;
				return docNode.DocumentDirectory;
			}
		}

		class DocumentTreeDelegate : NSOutlineViewDelegate {
			MainWindow win;

			public DocumentTreeDelegate (MainWindow win)
			{
				this.win = win;
			}

			public override void SelectionDidChange (NSNotification notification)
			{
				win.SelectionChanged ();
			}
		}
	}

	class DocumentTreeDataSource : NSOutlineViewDataSource {
		DocModel docModel;

		public DocumentTreeDataSource (DocModel docModel)
		{
			this.docModel = docModel;
		}

		public override int GetChildrenCount (NSOutlineView outlineView, NSObject item)
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

		public override NSObject GetChild (NSOutlineView outlineView, int childIndex, NSObject item)
		{

			if (item == null) 
				return docModel [childIndex];

			if (item is DocNamespace) {
				var ds = item as DocNamespace;
				return ds [childIndex];
			}
			if (item is DocType) {
				var dt = item as DocType;
				return dt [childIndex];
			}
			return null;
		}

		public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			if (item is DocNode)
				return (item as DocNode).Name;
			return new NSString ("Should not happen");
		}
	}
}

