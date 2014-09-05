//
// TODO: 
// * Cope with <br>
// * Cope with nodes that did not start with a <p> surrounding the thing.
//    => If we fail parsing, we try by surrounding with <p>
//
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace DocWriter
{
	public partial class MainWindow : MonoMac.AppKit.NSWindow, ILookup
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
	
		string ILookup.Fetch (string id)
		{
			var element = webView.StringByEvaluatingJavaScriptFromString ("getHtml(\"" + id + "\")");
			return element;
		}

		NSObject currentObject;

		void CheckContents ()
		{
			if (currentObject != null && currentObject is ILoader) {
				var result = (currentObject as ILoader).Load (this);
				if (result != null)
					statusLabel.Cell.StringValue = result;
				else
					statusLabel.Cell.StringValue = "OK";
			}
		}

		void SelectionChanged ()
		{
			currentObject = outline.ItemAtRow (outline.SelectedRow);
			var ihtml = currentObject as IHtmlRender;
			if (ihtml == null)
				return;

			webView.MainFrame.LoadHtmlString (ihtml.Render (), NSBundle.MainBundle.ResourceUrl);
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
				Console.WriteLine ("changing");
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

