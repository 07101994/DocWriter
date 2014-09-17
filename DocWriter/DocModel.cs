//
// DocModel.cs: Contains the various nodes that know how to render and edit pages
//
// Author:
//   Miguel de Icaza (miguel@xamarin.com)
//
// Copyright 2014 Xamarin Inc
//
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using MonoMac.Foundation;
using System.Xml;
using System.Text;
using System.Web;

namespace DocWriter
{
	// Interface implemented by nodes that can render themselves as HTML
	// typically this renders the editable content as well.
	interface IHtmlRender {
		string Render ();
	}

	// Interface implemented to lookup the contents of a node
	public interface IWebView {
		string Fetch (string id);
		string RunJS (string code);
	}


	// Interface implemented by nodes that can provide editing functionality
	interface IEditableNode {
		string ValidateChanges (IWebView webView, string [] nodes);
		bool Save (IWebView webView, out string error);
	}

	// It is an NSObject, because we conveniently stash these in the NSOutlineView
	public class DocNode : NSObject {
		public string CName;

		// This is an NSString because we use it as a value that we store in a NSOutlineView
		NSString _name;
		public NSString Name { 
			get { return _name; }
			set {
				_name = value;
				CName = value.ToString ();
			}
		}

		public string GetHtml (XElement element, string xpath)
		{
			return DocConverter.ToHtml (element.XPathSelectElement (xpath), Name.ToString ());
		}
			
		//
		// Updates the target XElement with the edited text.   
		// The first pass uses the provided xpath expression to remove all the nodes that match
		// this expression from the target (this cleans the nodes that we are about to replace)
		// then the named htmlElement is used to lookup the contents on WebKit and the results
		// are converted back to XML which is stashed in the target specified by xpath.
		//
		// Returns true on success
		public bool UpdateNode (IWebView webView, XElement target, string xpath, string htmlElement, out string error)
		{
			error = null;
			var node = target.XPathSelectElement (xpath);
			node.RemoveNodes ();

			try {
				var str = webView.Fetch (htmlElement);
				foreach (var ret in DocConverter.ToXml (str, canonical: true))
					node.Add (ret);
			} catch (UnsupportedElementException e){
				error = e.Message;
				return false;
			}
			return true;
		}

		// Validates that the elements named in 'args' in WebKit can be converted
		// from HTML back into ECMA XML.
		//
		// Returns null on success, otherwise a string with the error details
		//
		public string ValidateElements (IWebView web, params string [] args)
		{
			foreach (var arg in args) {
				try {
					var str = web.Fetch (arg);
					DocConverter.ToXml (str, canonical: true);
					web.RunJS ("postOk('" + arg + "')");
				} catch (UnsupportedElementException e){
					web.RunJS ("postError('" + arg + "')");
					return "Parsing error: " + e.Message;
				} catch (StackOverflowException e){
					return "Exception " + e.GetType ().ToString ();
				}
			}
			return null;
		}

		// Returns this node path, useful on the IDe to remember which node to reload
		public virtual string DocPath {
			get {
				return "";
			}
		}
	}

	// DocMember: renders an ECMA type member (methods, properties, properties, fields)
	public class DocMember : DocNode, IHtmlRender, IEditableNode {
		public DocType Type { get; private set; }
		public XElement MemberElement;
		public IEnumerable<XElement> Params;
		public XElement Value;
		public XElement ReturnValue;
		public XElement Remarks;
		public string Kind;

		public DocMember (DocType type, XElement e)
		{
			this.Type = type;
			this.MemberElement = e;
			Name = new NSString (e.Attribute ("MemberName").Value);
			Remarks = e.XPathSelectElement ("Docs/remarks");
			Params = e.XPathSelectElements ("Docs/param");
			Value = e.XPathSelectElement ("Docs/value");
			ReturnValue = e.XPathSelectElement ("Docs/returns");
			Kind = e.XPathSelectElement ("MemberType").Value;
		}

		public string Render ()
		{
			var ret = new MemberTemplate () { Model = this }.GenerateString ();
			return ret;
		}

		public string SummaryHtml {
			get {
				return GetHtml (MemberElement, "Docs/summary");
			}
		}

		public string SignatureHtml {
			get {
				return HttpUtility.HtmlEncode (MemberElement.XPathSelectElement ("MemberSignature").Attribute ("Value").Value);
			}
		}

		public string GetHtml (string xpath)
		{
			return GetHtml (MemberElement, xpath);
		}
			
		public string ValidateChanges (IWebView webView, string [] nodes)
		{
			var err = ValidateElements (webView, nodes);
			if (err != null)
				return err;
			return null;
		}

		public bool Save (IWebView webView, out string error)
		{
			error = null;
			if (!UpdateNode (webView, MemberElement, "Docs/summary", "summary", out error))
				return false;
			if (Remarks != null && !UpdateNode (webView, MemberElement, "Docs/remarks", "remarks", out error))
				return false;
			if (Value != null && !UpdateNode (webView, MemberElement, "Docs/value", "value", out error))
				return false;
			if (ReturnValue != null && !UpdateNode (webView, MemberElement, "Docs/returns", "return", out error))
				return false;
			if (Params != null) {
				foreach (var p in Params) {
					string name = "param-" + p.Attribute ("name").Value;
					if (!UpdateNode (webView, p, ".", name, out error))
						return false;
				}
			}
			return Type.SaveDoc (out error);
		}

		public override string DocPath {
			get {
				return Type.DocPath + "/" + CName;
			}
		}
	}

	// DocType: renders an ECMA type
	public class DocType : DocNode, IHtmlRender, IEditableNode {
		public DocNamespace Namespace { get; private set; }
		public IEnumerable<XElement> Params;
		public XElement Root;

		// Keeps track of altered nodes in the summaries
		Dictionary<string,string> dirtyNodes = new Dictionary<string, string>();

		// The contents of the XML document, as read from disk
		XDocument doc;
		XElement [] xml_members;
		DocMember [] members;
		string path;

		public DocType (DocNamespace ns, string path)
		{
			this.path = path;
			Namespace = ns;
			Name = new NSString (Path.GetFileNameWithoutExtension (path));
			try {
				doc = XDocument.Load (path);
				Root = doc.Root;
				xml_members = doc.XPathSelectElements ("/Type/Members/Member").ToArray ();
				Params = doc.XPathSelectElements ("/Type/Docs/param");
				members = new DocMember[xml_members.Length];
			} catch {
			}
		}

		public int NodeCount {
			get {
				return xml_members.Length;
			}
		}

		public DocMember this [int idx]{
			get {
				if (members [idx] == null) 
					members [idx] = new DocMember (this, xml_members [idx]);
				return members [idx];
			}
		}

		public string Render ()
		{
			var ret = new TypeTemplate () { Model = this }.GenerateString ();
			return ret;
		}

		public string KindHtml {
			get {
				var x = doc.XPathSelectElement ("/Type/Base/BaseTypeName");
				if (x == null)
					return "interface";

				switch (x.Value) {
				case "System.Enum":
					return "enum";
				case "System.ValueType":
					return "struct";
				case "System.Delegate":
					return "delegate";
				default:
					return "class";
				}
			}
		}

		public string ToBeAddedClass {
			get {
				if (Root.XPathSelectElement ("/Type/Docs/summary").Value == "To be added.")
					return "to-be-added";
				return "";
			}
		}

		public string SummaryHtml {
			get {
				return GetHtml (Root, "/Type/Docs/summary");
			}
		}

		public string RemarksHtml {
			get {
				var x =  GetHtml (Root, "/Type/Docs/remarks");
				return x;
			}
		}

		public string ValidateChanges (IWebView webView, string [] nodes)
		{
			var ret = ValidateElements (webView, nodes);
			if (ret != null)
				return ret;
			foreach (var n in nodes) {
				if (dirtyNodes.ContainsKey (n))
					continue;
				dirtyNodes [n] = n;
			}
			return null;
		}

		public bool SaveDoc (out string error)
		{
			error = null;
			try {
				var s = new XmlWriterSettings () {
					Indent = true,
					Encoding = new UTF8Encoding (false),
					OmitXmlDeclaration = true,
					NewLineChars = Environment.NewLine
				};
				using (var stream = File.Create (path)){
					using (var xmlw = XmlWriter.Create (stream, s)){
						doc.Save (xmlw);
					}
					stream.Write (new byte [] { 10 }, 0, 1);
				} 
				return true;
			} catch (Exception e){
				error = e.ToString ();
				return false;
			}
		}

		public bool Save (IWebView webView, out string error)
		{
			error = null;
			if (UpdateNode (webView, Root, "/Type/Docs/summary", "summary", out error) &&
			    UpdateNode (webView, Root, "/Type/Docs/remarks", "remarks", out error)) {

				foreach (var dirty in dirtyNodes.Keys) {
					if (dirty.StartsWith ("summary-")) {
						int idx;
						if (Int32.TryParse (dirty.Substring (8), out idx)) {
							var node = this [idx];
							if (!node.UpdateNode (webView, this [idx].MemberElement, "Docs/summary", dirty, out error))
								return false;
						} else {
							throw new Exception ("You introduced a new summary-XX id that we can not save");
						}
					}
				}
				if (Params != null) {
					foreach (var p in Params) {
						string name = "param-" + p.Attribute ("name").Value;
						if (!UpdateNode (webView, p, ".", name, out error))
							return false;
					}
				}

				dirtyNodes.Clear ();

				if (SaveDoc (out error))
					return true;
				error = "Error while saving the XML file to " + path;
			}

			return false;
		}
		public override string DocPath {
			get {
				return Namespace.DocPath + "/" + CName;
			}
		}
	}

	// Provides a host to show the namespace on the tree.
	public class DocNamespace : DocNode, IHtmlRender, IEditableNode {
		Dictionary<string,string> dirtyNodes = new Dictionary<string, string>();
		SortedList<string,DocType> docs = new SortedList<string, DocType> ();
		string path;

		public DocNamespace (string path)
		{
			this.path = path;
			Name = new NSString (Path.GetFileName (path));
			foreach (var file in Directory.GetFiles (path, "*.xml")){
				docs [Path.GetFileName (file)] = null;
			}
		}
		public int NodeCount {
			get {
				return docs.Count;
			}
		}

		public DocType this [int idx] {
			get {
				if (docs.Values [idx] == null) {
					var key = docs.Keys [idx];
					docs [key] = new DocType (this, Path.Combine (path, key));
				}
				return docs.Values [idx];
			}
		}

		public string Render ()
		{
			var ret = new NamespaceTemplate () { Model = this }.GenerateString ();
			return ret;
		}

		public string ValidateChanges (IWebView webView, string[] nodes)
		{			
			var ret = ValidateElements (webView, nodes);
			if (ret != null)
				return ret;
			foreach (var n in nodes) {
				if (dirtyNodes.ContainsKey (n))
					continue;
				dirtyNodes [n] = n;
			}
			return null;
		}

		public bool Save (IWebView webView, out string error)
		{
			error = null;
			foreach (var dirty in dirtyNodes.Keys) {
				if (dirty.StartsWith ("summary-")) {
					int idx;
					if (Int32.TryParse (dirty.Substring (8), out idx)) {
						var node = this [idx];
						if (!node.UpdateNode (webView, node.Root,"/Type/Docs/summary", dirty, out error))
							return false;
						Console.WriteLine ("SAVING NODE {0}", node.Name);
						if (!node.SaveDoc (out error))
							return false;
					} else {
						throw new Exception ("You introduced a new summary-XX id that we can not save");
					}
				}
			}
			dirtyNodes.Clear ();
			return true;
		}

		public override string DocPath {
			get {
				return CName;
			}
		}
	}

	// Loads the documentation from disk, on demand.
	public class DocModel
	{
		List<DocNamespace> namespaces = new List<DocNamespace> ();

		public DocModel (string path = "/cvs/mt/ios-api-docs/en")
		{
			var dirs = 
				from p in Directory.GetDirectories (path)
				orderby p
					where Path.GetFileName (p) != "Mono.Options"
				select p;

			foreach (var dir in dirs) {
				namespaces.Add (new DocNamespace (dir));
			}
		}

		public int NodeCount {
			get {
				return namespaces.Count;
			}
		}

		public DocNamespace this [int idx] {
			get {
				return namespaces [idx];
			}
		}


	}
}

