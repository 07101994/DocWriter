//
// MemberEntry.cs: Data entry for URLs
//
// Author:
//   Miguel de Icaza (miguel@xamarin.com)
//
// Copyright 2014 Xamarin Inc
//
//
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace DocWriter
{
	public partial class MemberEntry : MonoMac.AppKit.NSWindow
	{
		DocModel docModel;
		MainWindowController mainWindowController;

		// Called when created from unmanaged code
		public MemberEntry (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MemberEntry (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
			
		// Shared initialization code
		void Initialize ()
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			textField.Changed += HandleChanged;;
			textField.EditingEnded += HandleEditingEnded;

			mainWindowController = (this.WindowController as MemberEntryController).mainWindowController;
			docModel = mainWindowController.Window.DocModel;

			PerformFilter ("");
			tableView.Delegate = new CompleteTableViewDelegate (this);
			tableView.DataSource = new CompleteTableViewDataSource (this);
		}

		void HandleEditingEnded (object sender, EventArgs e)
		{
			Ok ();
		}

		void HandleChanged (object sender, EventArgs e)
		{
			PerformFilter (textField.StringValue);
		}

		string JustUri (string text)
		{
			if (text.Length > 2 && text [1] == ':')
				return text.Substring (2);
			return text;
		}

		void PerformFilter (string filter)
		{
			char kind = (char)0;

			if (filter.Length > 2) {
				if (filter [1] == ':') {
					kind = filter [0];
					filter = filter.Substring (2);

				}
			}
			results.Clear ();
			for (int i = 0; i < docModel.NodeCount; i++) {
				var name = docModel [i].CName;

				if (name.StartsWith (filter)) {
					results.Add (docModel [i]);
				}
			}
			tableView.ReloadData ();
		}

		[Export ("ok:")]
		void Ok ()
		{
			var v = textField.StringValue;

			if (v.Length > 2 && v [1] == ':')
				;
			else
				v = "N:" + v;

			mainWindowController.InsertReference (v);
			Close ();
		}

		partial void cancel (NSObject sender)
		{
			Close ();
		}

		void Close ()
		{
			OrderOut (this);
			results.Clear ();
			textField.StringValue = "";
		}

		string FindCommonPrefix ()
		{
			Console.WriteLine ("Searching");
			for (int pl = 0; pl < results [0].CName.Length; pl++) {
				char c = results [0].CName [pl];

				for (int i = 1; i < results.Count; i++) {
					if (pl >= results[i].CName.Length || results[i].CName [pl] != c ) {
						return results [i].CName.Substring (0, pl);
					}
				}
			}
			return results [0].CName;
		}

		public override void SendEvent (NSEvent theEvent)
		{
			if (theEvent.Type == NSEventType.KeyDown) {
				if (FirstResponder is NSText && theEvent.KeyCode == 48){
					if (results.Count > 0  && results [0].CName.Length > 0) {

						string commonPrefix = FindCommonPrefix ();
						var url = textField.StringValue;

						if (commonPrefix != JustUri (url)) {
							if (url.Length > 2 && url [1] == ':')
								textField.StringValue = string.Format ("{0}:{1}", url [0], commonPrefix);
							else 
								textField.StringValue = commonPrefix;
						}
					}
					return;
				}
			}

			base.SendEvent (theEvent);
		}

		List<DocNode> results = new List<DocNode> ();

		public class CompleteTableViewDelegate : NSTableViewDelegate {
			MemberEntry me;

			public CompleteTableViewDelegate (MemberEntry me)
			{ 
				this.me = me;
			}
		}

		public class CompleteTableViewDataSource : NSTableViewDataSource {
			MemberEntry me;

			public CompleteTableViewDataSource (MemberEntry me)
			{
				this.me = me;
			}

			public override int GetRowCount (NSTableView tableView)
			{
				return (me.results.Count);
			}

			public override NSObject GetObjectValue (NSTableView tableView, NSTableColumn tableColumn, int row)
			{
				return me.results [row].Name;
			}
		}
	}
}

