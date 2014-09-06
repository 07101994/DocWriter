using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using MonoMac.Foundation;
using System.Xml;
using System.Text;

namespace DocWriter
{
	interface IHtmlRender {
		string Render ();
	}

	// Interface implemented to lookup the contents of a node
	public interface ILookup {
		string Fetch (string id);
	}

	interface IEditableNode {
		string ValidateChanges (ILookup lookup);
		bool Save (ILookup lookup, out string error);
	}

	// It is an NSObject, because we conveniently stash these in the NSOutlineView
	public class DocNode : NSObject {
		// This is an NSString because we use it as a value that we store in a NSOutlineView
		public NSString Name { get; internal set; }

		public string GetHtml (XElement element, string xpath)
		{
			return DocConverter.ToHtml (element.XPathSelectElement (xpath), Name.ToString ());
		}
			
		public bool UpdateNode (ILookup lookup, XElement target, string xpath, string htmlElement, out string error)
		{
			error = null;
			var node = target.XPathSelectElement (xpath);
			node.RemoveAll ();

			try {
				var str = lookup.Fetch (htmlElement);
				foreach (var ret in DocConverter.ToXml (str).ToArray ())
					node.Add (ret);
			} catch (UnsupportedElementException e){
				error = e.Message;
				return false;
			}
			return true;
		}

		public XNode[] xParse (ILookup lookup, string element, out string error)
		{
			error = null;
			try {
				var str = lookup.Fetch (element);
				return DocConverter.ToXml (str).ToArray ();
			} catch (UnsupportedElementException e){
				error = "Parsing error: " + e.Message;
			} catch (StackOverflowException e){
				error = "Exception " + e.GetType ().ToString ();
			}
			return null;
		}

		// Returns null on success, otherwise a string with the error details
		public string ValidateElements (ILookup lookup, params string [] args)
		{
			foreach (var arg in args) {
				try {
					var str = lookup.Fetch (arg);
					DocConverter.ToXml (str).ToArray ();
				} catch (UnsupportedElementException e){
					return "Parsing error: " + e.Message;
				} catch (StackOverflowException e){
					return "Exception " + e.GetType ().ToString ();
				}
			}
			return null;
		}
	}

	public class DocMember : DocNode, IHtmlRender, IEditableNode {
		public DocType Type { get; private set; }
		XElement e;
		public IEnumerable<XElement> Params;
		public XElement ReturnValue;
		public XElement Remarks;
		public string Kind;

		public DocMember (DocType type, XElement e)
		{
			this.Type = type;
			this.e = e;
			Name = new NSString (e.Attribute ("MemberName").Value);
			Remarks = e.XPathSelectElement ("Docs/remarks");
			Params = e.XPathSelectElements ("Docs/params");
			ReturnValue = e.XPathSelectElement ("Docs/value");
			Kind = e.XPathSelectElement ("MemberType").Value;
		}

		public string Render ()
		{
			var ret = new MemberTemplate () { Model = this }.GenerateString ();
			return ret;
		}

		public string GetHtml (string xpath)
		{
			return GetHtml (e, xpath);
		}
			
		public string ValidateChanges (ILookup lookup)
		{
			var err = ValidateElements (lookup, "summary", "remarks");
			if (err != null)
				return err;
			if (ReturnValue != null)
				err = ValidateElements (lookup, "return");
			if (err != null)
				return err;
			if (Params != null) {
				foreach (var p in Params) {
					string name = "param-" + p.Attribute ("name");
					err = ValidateElements (lookup, name);
					if (err != null)
						return err;
				}
			}
			return null;
		}

		public bool Save (ILookup lookup, out string error)
		{
			error = null;
			if (!UpdateNode (lookup, e, "Docs/summary", "summary", out error))
				return false;
			if (Remarks != null && !UpdateNode (lookup, e, "Docs/remarks", "remarks", out error))
				return false;
			if (ReturnValue != null && !UpdateNode (lookup, e, "Docs/value", "return", out error))
				return false;
			if (Params != null) {
				foreach (var p in Params) {
					string name = "param-" + p.Attribute ("name");
					if (!UpdateNode (lookup, e, ".", name, out error))
						return false;
				}
			}
			return Type.SaveDoc (out error);
			return true;
		}

	}

	public class DocType : DocNode, IHtmlRender, IEditableNode {
		XDocument doc;
		public DocNamespace Namespace { get; private set; }
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
				xml_members = doc.XPathSelectElements ("/Type/Members/Member").ToArray ();
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

		public string SummaryHtml {
			get {
				return GetHtml (doc.Root, "/Type/Docs/summary");
			}
		}

		public string RemarksHtml {
			get {
				var x =  GetHtml (doc.Root, "/Type/Docs/remarks");
				return x;
			}
		}

		public string ValidateChanges (ILookup lookup)
		{
			return ValidateElements (lookup, "summary", "remarks");
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

		public bool Save (ILookup lookup, out string error)
		{
			if (UpdateNode (lookup, doc.Root, "/Type/Docs/summary", "summary", out error) &&
			    UpdateNode (lookup, doc.Root, "/Type/Docs/remarks", "remarks", out error)) {

				if (SaveDoc (out error))
					return true;
				error = "Error while saving the XML file to " + path;
			}
			return false;
		}
	}

	public class DocNamespace : DocNode {
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
	}

	public class DocModel
	{
		List<DocNamespace> namespaces = new List<DocNamespace> ();

		public DocModel (string path = "/cvs/mt/ios-api-docs/en")
		{
			var dirs = from p in Directory.GetDirectories (path)
			           orderby p
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

