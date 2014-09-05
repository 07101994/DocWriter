using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using MonoMac.Foundation;

namespace DocWriter
{
	interface IHtmlRender {
		string Render ();
	}

	public interface ILookup {
		string Fetch (string id);
	}

	interface ILoader {
		string Load (ILookup lookup);
	}

	// It is an NSObject, because we conveniently stash these in the NSOutlineView
	public class DocNode : NSObject {
		// This is an NSString because we use it as a value that we store in a NSOutlineView
		public NSString Name { get; internal set; }

		public string GetHtml (XDocument doc, string xpath)
		{
			return GetHtml (doc.Root, xpath);
		}

		public string GetHtml (XElement element, string xpath)
		{
			return DocConverter.ToHtml (element.XPathSelectElement (xpath), Name.ToString ());
		}

		public string Parse (ILookup lookup, params string [] args)
		{
			foreach (var arg in args) {
				try {
					var str = lookup.Fetch (arg);
					DocConverter.ToXml (str).ToArray ();
				} catch {
					return "Failure loading " + arg;
				}
			}
			return null;
		}
	}

	public class DocMember : DocNode {
		public DocType Type { get; private set; }
		XElement e;

		public DocMember (DocType type, XElement e)
		{
			this.Type = type;
			this.e = e;
			Name = new NSString (e.Attribute ("MemberName").Value);
		}
	}

	public class DocType : DocNode, IHtmlRender, ILoader {
		XDocument doc;
		public DocNamespace Namespace { get; private set; }
		XElement [] xml_members;
		DocMember [] members;

		public DocType (DocNamespace ns, string path)
		{
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
				return GetHtml (doc, "/Type/Docs/summary");
			}
		}

		public string RemarksHtml {
			get {
				var x =  GetHtml (doc, "/Type/Docs/remarks");
				return x;
			}
		}

		public string Load (ILookup lookup)
		{
			return Parse (lookup, "summary", "remarks");
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

