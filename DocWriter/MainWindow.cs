//
// Convert.cs: Routines to turn ECMA XML into an HTML string and this subset of HTML back into ECMA XML
//
// Author:
//   Miguel de Icaza (miguel@xamarin.com)
//
// Copyright 2014 Xamarin Inc
//
//
// TODO: 
//   Add image loading support
//   Allow editing of the enums at the type level, without having to go element by element
//   Force a save on quit.
//   Add try/catch around the validation timer, and catch errors there
//   Hookup Command-S to save, for the paranoid in us.
//   Hotkeys to insert various kinds of markup
//
// Wanted:
//   Flag members that are auto-documented as such, to now waste documenters time on it.
//
// Debate:
//   Render all summaries at type or namespace level, and provide a dedicated editor to just fill those out quickly?
//
// Current save strategy is not great: it saves the content of the page on quit, but this would
// not work very well when we render a page that aggragatges many children (a namespace or all members)
// since it would only trigger a save on switch, and that might be too late (specially during the debugging
// stages of this).
//
// For something like that to work, it might be nice to save dirty elements on focus change.
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
			if (currentObject != null && currentObject is IEditableNode) {
				string result;

				try {
					result = (currentObject as IEditableNode).ValidateChanges (this);
				} catch (Exception e){
					result = e.ToString ();
				}

				if (result != null)
					statusLabel.Cell.StringValue = result;
				else
					statusLabel.Cell.StringValue = "OK";
			}
		}

		void SelectionChanged ()
		{
			if (currentObject != null) {
				var editable = currentObject as IEditableNode;
				currentObject = null;

				if (editable != null) {
					string error;
					if (!editable.Save (this, out error)) {
						// FIXME: popup a window or something.
					}
				}
			}


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

