//
// WebView.cs: Contains the interface and logic for working with a Web view
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
using NSObject = Foundation.NSObject;
#else
using NSObject = System.Object;
#endif

namespace DocWriter
{
	// Interface implemented to lookup the contents of a node
	public interface IWebView {
		string RunJS (string functionName, params string[] args);
	}
	
	// Extensions to add features and operations
	public static class WebViewExtensions {

		public static string EscapeHtml (string html) {
			var sb = new StringBuilder ();
			foreach (char c in html) {
				if (c == '\n')
					sb.Append ("\\\n");
				else
					sb.Append (c);
			}
			return sb.ToString ();
		}
		
		public static string Fetch (this IWebView webView, string id) {
			var element = webView.RunJS ("getHtml", id);
			if (element.StartsWith ("<<<<", StringComparison.OrdinalIgnoreCase)) {
				Console.WriteLine ("Failure to fetch contents of {0}", id);
			}
			return element;
		}

		public static void InsertSpan (this IWebView webView, string html) {
			webView.RunJS ("insertSpanAtCursor", EscapeHtml (html));
		}
		
		public static void InsertHtml (this IWebView webView, string html, params object[] args) {
			webView.InsertSpan (string.Format (html, args));
		}
		
		public static void InsertUrl (this IWebView webView, string caption, string url)
		{
			webView.InsertHtml (string.Format ("<div class='verbatim'><a href='{0}'>{1}</a></div>", url, caption));
		}

		public static void SelectionToCode (this IWebView webView, SelectionToCodeType type) {
			string codeType = null;
			switch (type) {
				case SelectionToCodeType.LangWord:
					codeType = "langword";
					break;
				case SelectionToCodeType.ParamRef:
					codeType = "paramref";
					break;
				case SelectionToCodeType.TypeParamRef:
					codeType = "typeparamref";
					break;
			}
			if (codeType != null) {
				webView.RunJS ($"selectionToCode", codeType);
			}
		}

		public static void InsertReference (this IWebView webView, DocNode docNode, string text = null) {
			webView.InsertHtml ("<a href=''>" + docNode.ReferenceString + "</a>");
		}
		
		public static void InsertImage (this IWebView webView, string target) {
			webView.InsertHtml("<img src='{0}'>", target);
		}
	}
}
