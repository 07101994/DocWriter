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
using MonoMac.WebKit;

namespace DocWriter
{
	public partial class MainWindow : MonoMac.AppKit.NSWindow, IEditorWindow
	{
		bool ready;

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

		public DocNode CurrentObject { get; set; }

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
			outline.Delegate = new DocumentTreeDelegate (this);
			Title = "DocWriter - " + WindowPath;

			// Restore the last node
			var lastOpenedNode = NSUserDefaults.StandardUserDefaults.StringForKey ("currentNode"+WindowPath);
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
			webView.DecidePolicyForNavigation += HandleDecidePolicyForNavigation;
			NSTimer.CreateRepeatingScheduledTimer (1, () => this.CheckContents ());
			ready = true;
		}
	
		void HandleDecidePolicyForNavigation (object sender, MonoMac.WebKit.WebNavigationPolicyEventArgs e)
		{
			switch (e.OriginalUrl.Scheme){
			case "ecma":
				WebView.DecideIgnore (e.DecisionToken);
				NavigateTo (e.OriginalUrl.AbsoluteString.Substring (7));
				return;

				// This is one of our rendered ecma links, we want to extract the target
				// from the text, not the href attribute value (since this is not easily
				// editable, and the text is.
			case "goto":
				NavigateTo (RunJS ("getText", e.OriginalUrl.Host));
				break;
			}
			WebView.DecideUse (e.DecisionToken);
		}

		int SelectNamespace (string name)
		{
			for (int n = DocModel.NodeCount, i = 0; i < n; i++) {
				if (DocModel [i].CName == name) {
					outline.SelectRow (i, false);
					outline.ScrollRowToVisible (i);
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
					outline.ScrollRowToVisible (r);
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
					outline.ScrollRowToVisible (r);
					return;
				}
			}
		}

		void SplitType (string rest, out string ns, out string type)
		{
			var p = rest.LastIndexOf ('.');
			if (p == -1) {
				ns = "";
				type = rest;
			} else {
				ns = rest.Substring (0, p);
				type = rest.Substring (p + 1);
			}
		}

		void SplitMember (string rest, out string ns, out string type, out string method, bool search_open_parens = false)
		{
			int l = rest.Length-1;

			// Look for M:System.Console.WriteLine(object)?
			if (search_open_parens) {
				var p = rest.IndexOf ('(');
				if (p != -1)
					l = p;
			}
			int pp = rest.LastIndexOf ('.', l);
			SplitType (rest.Substring (0, pp), out ns, out type);
			method = rest.Substring (pp + 1);
		}

		// This is not currently very precise, for now, it is just a convenience function, later we need to enforce method call parameters
		public bool NavigateTo (string url)
		{
			if (url.Length < 3)
				return false;
			if (url [1] != ':')
				return false;
			var rest = url.Substring (2);
			string ns, type, member;
	
			switch (url [0]) {
			case 'N':
				SelectNamespace (rest);
				return true;
			case 'T': 
				SplitType (rest, out ns, out type);
				var idx = SelectNamespace (ns);
				SelectType (idx, type);
				return true;
			case 'M':
			case 'P':
			case 'E':
				SplitMember (rest, out ns, out type, out member, search_open_parens: true);
				idx = SelectNamespace (ns);
				var tidx = SelectType (idx, type);
				SelectMember (idx, tidx, member);
				return true;
			case 'F':
				SplitMember (rest, out ns, out type, out member);
				idx = SelectNamespace (ns);
				SelectType (idx, type);
				return true;
			}
			return false;
		}

		void SelectionChanged ()
		{
			if (ready) {
				this.SaveCurrentObject ();
			}
			CurrentObject = null;

			CurrentObject = outline.ItemAtRow (outline.SelectedRow) as DocNode;
			if (CurrentObject != null) {
				NSUserDefaults.StandardUserDefaults.SetString (CurrentObject.DocPath, "currentNode"+WindowPath);
			}

			var ihtml = CurrentObject as IHtmlRender;
			if (ihtml == null)
				return;

			string contents;
			try {
				contents = ihtml.Render ();
			} catch (Exception e){
				contents = String.Format ("<body><p>Error Loading the contents for the new node<p>Exception:<p><pre>{0}</pre>", System.Web.HttpUtility.HtmlEncode (e.ToString ()));
			}
			webView.MainFrame.LoadHtmlString (contents, null);
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
			if (item is DocMember)
			return (NSString) ($"{(item as DocMember).Name} ({string.Join(", ", (item as DocMember).MemberParams.Select(p => p.Attribute("Type").Value))})");
			if (item is DocNode)
				return (NSString) (item as DocNode).Name;
			return new NSString ("Should not happen");
		}
	}
}

