//
// DocModel.cs: Contains the various nodes that know how to render and edit pages
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
	// Interface implemented by nodes that can render themselves as HTML
	// typically this renders the editable content as well.
	interface IHtmlRender {
		string Render ();
	}

	// Interface implemented by nodes that can provide editing functionality
	interface IEditableNode {
		string ValidateChanges (IWebView webView, string [] nodes);
		bool Save (IWebView webView, out string error);
	}

	// It is an NSObject, because we conveniently stash these in the NSOutlineView
	public class DocNode : NSObject {
		
		public string Name { get; set; }

		public string GetHtmlForNode (XElement element, string xpath)
		{
			return DocConverter.ToHtml (element.XPathSelectElement (xpath), Name.ToString (), DocumentDirectory);
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

		public static bool SaveXDocument (XDocument doc, string path, out string error)
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
					web.RunJS ("postOk", arg);
				} catch (UnsupportedElementException e){
					web.RunJS ("postError", arg);
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

		public virtual string DocumentDirectory {
			get {
				return "";

			}
		}

		public virtual bool IsAutodocumented {
			get {
				return false;
			}
		}

		public string ToHtml (XElement root)
		{
			return DocConverter.ToHtml (root, "inmemory", DocumentDirectory);
		}

		// Suggests a name for the reference based on the current context
		public virtual string ReferenceString {
			get {
				return "!";
			}
		}
	}

	// DocMember: renders an ECMA type member (methods, properties, properties, fields)
	public class DocMember : DocNode, IHtmlRender, IEditableNode {
		public DocType Type { get; private set; }
		public XElement MemberElement;
		public IEnumerable<XElement> MemberParams;
		public IEnumerable<XElement> Params;
		public XElement Value;
		public XElement ReturnValue;
		public XElement Remarks;
		public string Kind;

		public DocMember (DocType type, XElement e)
		{
			this.Type = type;
			this.MemberElement = e;
			Name = e.Attribute ("MemberName").Value;
			Remarks = e.XPathSelectElement ("Docs/remarks");
			MemberParams = e.XPathSelectElements("Parameters/Parameter");
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
				return GetHtmlForNode (MemberElement, "Docs/summary");
			}
		}

		public string SignatureHtml {
			get {
				return HttpUtility.HtmlEncode (MemberElement.XPathSelectElement ("MemberSignature").Attribute ("Value").Value);
			}
		}

		public string GetHtmlForNode (string xpath)
		{
			return GetHtmlForNode (MemberElement, xpath);
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
				return Type.DocPath + "/" + Name;
			}
		}

		public override string DocumentDirectory {
			get {
				return Type.DocumentDirectory;
			}
		}

		private bool IsAutodocumentedConstructor()
		{
			if (Name == ".ctor") {
				var argCount = Enumerable.Count (MemberElement.XPathSelectElements ("Parameters/Parameter"));
				switch (argCount) {
				case 0: //Default .ctor
					return true;
				case 1:
					//IntPtr
					if (Enumerable.Count (MemberElement.XPathSelectElements ("Parameters/Parameter[@Type='System.IntPtr']")) == 1) {
						return true;
					}
					//NSCoder
					if (Enumerable.Count (MemberElement.XPathSelectElements ("Parameters/Parameter[@Type='MonoTouch.Foundation.NSCoder']")) == 1) {
						return true;
					}
					//NSObjectFlag
					if (Enumerable.Count (MemberElement.XPathSelectElements ("Parameters/Parameter[@Type='MonoTouch.Foundation.NSObjectFlag']")) == 1) {
						return true;
					}
					return false;
				default : 
					return false;
				}
			}
			return false;
		}

		private bool IsClassHandle ()
		{
			return Name == "ClassHandle";;
		}

		private bool IsAutodocumentedAppearance ()
		{
			return Name == "Appearance" | Name == "AppearanceWhenContainedIn";
		}

		private bool IsNotificationProperty ()
		{
			return 
				Name.EndsWith("Notification") 
				&& MemberElement.XPathSelectElement("ReturnValue/ReturnType").Value == "MonoTouch.Foundation.NSString";
		}

		private bool IsDelegateProperty ()
		{
			return Name == "Delegate";
		}

		private bool IsWeakDelegateProperty ()
		{
			return Name == "WeakDelegate";
		}

		private bool IsDisposeOrFinalizer ()
		{
			//TODO: Confirm Finalizer name
			return Name == "Dispose" | Name == "Finalize"; 
		}

		public override bool IsAutodocumented {
			get {
				return
				IsAutodocumentedConstructor ()
				| IsClassHandle ()
				| IsAutodocumentedAppearance ()
				| IsNotificationProperty ()
				| IsDelegateProperty ()
				| IsWeakDelegateProperty ()
				| IsDisposeOrFinalizer ();
			}
		}

		public override string ReferenceString {
			get {
				var kind = Kind [0];
				if (kind == 'C')
					kind = 'M';
				return $"{kind}:{Type.FullName}.{Name}";
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
			Name = Path.GetFileNameWithoutExtension (path);
			try {
				doc = XDocument.Load (path);
				Root = doc.Root;
				xml_members = doc.XPathSelectElements ("/Type/Members/Member").ToArray ();
				Params = doc.XPathSelectElements ("/Type/Docs/param");
				members = new DocMember[xml_members.Length];
			} catch {
			}
		}

		public string FullName {
			get {
				return Namespace.Name + "." + Name;
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

		public IEnumerable<DocMember> Members {
			get {
				return Enumerable.Range (0, NodeCount).Select (idx => this [idx]);
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
				return GetHtmlForNode (Root, "/Type/Docs/summary");
			}
		}

		public string RemarksHtml {
			get {
				var x =  GetHtmlForNode (Root, "/Type/Docs/remarks");
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
			return SaveXDocument (doc, path, out error);
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
				return Namespace.DocPath + "/" + Name;
			}
		}

		public override string DocumentDirectory {
			get {
				return Namespace.DocumentDirectory;
			}
		}

		public override string ReferenceString {
			get {
				return $"T:{FullName}";
			}
		}
	}

	// Provides a host to show the namespace on the tree.
	public class DocNamespace : DocNode, IHtmlRender, IEditableNode {
		Dictionary<string,string> dirtyNodes = new Dictionary<string, string>();
		SortedList<string,DocType> docs = new SortedList<string, DocType> ();
		XDocument nsDoc;
		XElement root;
		string path, nsPath;

		public DocNamespace (string path)
		{
			this.path = path;
			Name = Path.GetFileName (path);
			foreach (var file in Directory.GetFiles (path, "*.xml")){
				docs [Path.GetFileName (file)] = null;
			}
			nsPath = Path.Combine (Path.GetDirectoryName (path), "ns-" + Path.GetFileName (path) + ".xml");
			if (File.Exists (nsPath)) {
				Console.WriteLine ("Loading {0}", nsPath);
				try {
					nsDoc = XDocument.Load (nsPath);
					root = nsDoc.Root;
				} catch (Exception e) {
					Console.WriteLine ("Failed to load " + nsPath + e);
					nsDoc = null;
				}
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

		public IEnumerable<DocType> Types {
			get {
				return Enumerable.Range (0, NodeCount).Select (idx => this [idx]);
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
			if (nsDoc != null) {
				bool dirtyNS = false;
				if (dirtyNodes.ContainsKey ("summary")) {
					dirtyNS = true;
					if (!UpdateNode (webView, root, "/Namespace/Docs/summary", "summary", out error))
						return false;
				}
				if (dirtyNodes.ContainsKey ("remarks")) {
					dirtyNS = true;
					if (!UpdateNode (webView, root, "/Namespace/Docs/remarks", "remarks", out error))
						return false;
				}
				if (dirtyNS) {
					if (!SaveXDocument (nsDoc, nsPath, out error))
						return false;
				}
			}
			dirtyNodes.Clear ();
			return true;
		}

		public override string DocPath {
			get {
				return Name;
			}
		}

		public override string DocumentDirectory {
			get {
				return path;
			}
		}

		public string SummaryHtml {
			get {
				if (root == null)
					return "Missing namespace documentation file";
				return GetHtmlForNode (root, "/Namespace/Docs/summary");
			}
		}

		public string RemarksHtml {
			get {
				if (root == null)
					return "Missing namespace documentation file";
				var x =  GetHtmlForNode (root, "/Namespace/Docs/remarks");
				return x;
			}
		}

		public override string ReferenceString {
			get {
				return $"N:{Name}";
			}
		}
	}

	// Loads the documentation from disk, on demand.
	public class DocModel
	{
		List<DocNamespace> namespaces = new List<DocNamespace> ();

		public DocModel (string path)
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

		public IEnumerable<DocNamespace> Namespaces {
			get {
				return namespaces;
			}
		}

		private static void SplitType (string rest, out string ns, out string type)
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

		private static void SplitMember (string rest, out string ns, out string type, out string method, bool search_open_parens = false)
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
		public DocNode ParseReference (string url)
		{
			if (url.Length < 3)
				return null;
		
			if (url [1] != ':')
				return null;
		
			var rest = url.Substring (2);
			string ns, type, member;

			switch (url[0])
			{
				case 'N':
					var docN = Namespaces.FirstOrDefault (n => n.Name == rest);
					return docN;
				case 'T':
					SplitType (rest, out ns, out type);
					docN = Namespaces.FirstOrDefault (n => n.Name == ns);
					var docT = docN?.Types.FirstOrDefault (t => t.Name == type);
					return docT;
				case 'M':
				case 'P':
				case 'E':
					SplitMember (rest, out ns, out type, out member, search_open_parens: true);
					docN = Namespaces.FirstOrDefault (n => n.Name == ns);
					docT = docN?.Types.FirstOrDefault (t => t.Name == type);
					var docM = docT?.Members.FirstOrDefault (t => t.Name == member);
					return docM;
				case 'F':
					SplitMember (rest, out ns, out type, out member);
					docN = Namespaces.FirstOrDefault (n => n.Name == ns);
					docT = docN?.Types.FirstOrDefault (t => t.Name == type);
					return docT;
			}

			return null;
		}
	}
}

