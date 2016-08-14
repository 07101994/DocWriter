//
// EditprWindow.cs: Contains the the interface and logic for editing the docs
//
// Author:
//   Miguel de Icaza (miguel@xamarin.com)
//   Matthew Leibowitz (matthew.leibowitz@xamarin.com)
//
// Copyright 2016 Xamarin Inc
//
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using System.Text;
using System.Web;

#if __XAMARIN_MAC__
using NSObject = MonoMac.Foundation.NSObject;
#else
using NSObject = System.Object;
#endif

namespace DocWriter
{
	// Interface implemented to handle operations
	public interface IEditorWindow : IWebView {
		// the path and model for these docs
		string WindowPath { get; }
		DocModel DocModel { get; }

		// the currently editing object
		DocNode CurrentObject { get; set; }

		// update the editor
		void UpdateStatus (string status);
	}

	public enum SelectionToCodeType {
		ParamRef,
		TypeParamRef,
		LangWord
	}

	// Extensions to add features and operations
	public static class EditorWindowExtensions {
		
		public static void SaveCurrentObject (this IEditorWindow editorWindow) {
			var editable = editorWindow.CurrentObject as IEditableNode;
			if (editable != null) {
				string error;

				editorWindow.CheckContents ();

				if (!editable.Save (editorWindow, out error)) {
					// FIXME: popup a window or something.
				}
			}
		}

		public static void InsertTable (this IEditorWindow editorWindow)
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
			
			editorWindow.AppendEcmaNode (new XElement ("Host", table));
		}
		
		public static void InsertReference (this IEditorWindow editorWindow, string text = null) {
			editorWindow.InsertReference (editorWindow.CurrentObject, text);
		}

		public static void InsertList (this IEditorWindow editorWindow) {
			var list = new XElement ("list", new XAttribute ("type", "bullet"),
				new XElement ("item", new XElement ("term", new XText ("Text1"))),
				new XElement ("item", new XElement ("term", new XText ("Text2"))));

			editorWindow.AppendEcmaNode (new XElement ("host", list));
		}
		
		public static void InsertHtmlH2 (this IEditorWindow editorWindow) {
			editorWindow.AppendEcmaNode (new XElement ("host", new XElement ("format", new XAttribute ("type", "text/html"), new XElement ("h2", new XText ("Header")))));
		}
		
		public static void InsertCSharpCode (this IEditorWindow editorWindow) {
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "C#"), new XCData ("class Sample {")));

			editorWindow.AppendEcmaNode (example);
			editorWindow.AppendPara ();
		}

		public static void InsertCSharpExample (this IEditorWindow editorWindow) {
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "C#"), new XCData ("class Sample {"))));

			editorWindow.AppendEcmaNode (example);
			editorWindow.AppendPara ();
		}

		public static void InsertFSharpCode (this IEditorWindow editorWindow) {
			var example = new XElement ("host", new XElement ("code", new XAttribute ("lang", "F#"), new XCData ("let sample = ")));

			editorWindow.AppendEcmaNode (example);
			editorWindow.AppendPara ();
		}

		public static void InsertFSharpExample (this IEditorWindow editorWindow) {
			var example = new XElement ("host", new XElement ("example", new XElement ("code", new XAttribute ("lang", "F#"), new XCData ("let sample = "))));

			editorWindow.AppendEcmaNode (example);
			editorWindow.AppendPara ();
		}

		public static void AppendPara (this IEditorWindow editorWindow) {
			editorWindow.AppendEcmaNode (new XElement ("para", new XText (".")));
		}
		
		// Turns the provided ECMA XML node into HTML and appends it to the current node on the rendered HTML
		public static void AppendEcmaNode (this IEditorWindow editorWindow, XElement ecmaXml) {
			if (editorWindow.CurrentObject != null) {
				var html = DocConverter.ToHtml (ecmaXml, "", editorWindow.CurrentObject.DocumentDirectory);
				editorWindow.RunJS ("insertHtmlAfterCurrentNode", WebViewExtensions.EscapeHtml (html));
			}
		}
		
		public static void CheckContents (this IEditorWindow editorWindow)
		{
			var dirtyNodes = editorWindow.RunJS ("getDirtyNodes").Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (dirtyNodes.Length == 0)
				return;

			if (editorWindow.CurrentObject != null && editorWindow.CurrentObject is IEditableNode) {
				string result;
				try {
					result = (editorWindow.CurrentObject as IEditableNode).ValidateChanges (editorWindow, dirtyNodes);
				} catch (Exception e) {
					result = e.ToString ();
				}

				editorWindow.UpdateStatus (result ?? "OK");
			}
		}
	}
}
