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

namespace DocWriter
{
	public partial class MainWindow : MonoMac.AppKit.NSWindow, IWebView
	{
		DocModel docModel;

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
		
		// Shared initialization code
		void Initialize ()
		{
			docModel = new DocModel ();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			outline.DataSource = new DocumentTreeDataSource (docModel);
			outline.Delegate = new DocumentTreeDelegate (this);
			NSTimer.CreateRepeatingScheduledTimer (1, CheckContents);
		}
	
		public string Run (string code)
		{
			return webView.StringByEvaluatingJavaScriptFromString (code);
		}

		public string Fetch (string id)
		{
			var element = Run ("getHtml(\"" + id + "\")");
			if (element.StartsWith ("<<<<")) {
				Console.WriteLine ("Failure to fetch contents of {0}", id);
			}
			return element;
		}

		NSObject currentObject;

		void CheckContents ()
		{
			var dirtyNodes = Run ("getDirtyNodes ()").Split (new char [] {' '}, StringSplitOptions.RemoveEmptyEntries);
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

				if (!editable.Save (this, out error)) {
					// FIXME: popup a window or something.
				}
			}
		}

		void SelectionChanged ()
		{
			SaveCurrentObject ();
			currentObject = null;

			currentObject = outline.ItemAtRow (outline.SelectedRow);
			var ihtml = currentObject as IHtmlRender;
			if (ihtml == null)
				return;

			webView.MainFrame.LoadHtmlString (ihtml.Render (), NSBundle.MainBundle.ResourceUrl);
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

		public void AppendNodeHtml (string html)
		{
			webView.StringByEvaluatingJavaScriptFromString ("insertHtmlAfterCurrentNode(\"" + EscapeHtml (html) + "\")");
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

