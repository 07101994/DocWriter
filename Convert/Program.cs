//
// Convert.cs: Routines to turn ECMA XML into an HTML string and this subset of HTML back into ECMA XML
//
// Author:
//   Miguel de Icaza (miguel@xamarin.com)
//
// Copyright 2014 Xamarin Inc
//
// FIXME:
// See 'copied' below, paragraph style not handled.

using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;
using System.Text;
using System.Collections.Generic;
using System.IO;
using HtmlAgilityPack;
using System.Web;
using Microsoft.XmlDiffPatch;

class X {
	static string currentFile;

	static void Main (string [] args)
	{
		var path = "/cvs/mt/ios-api-docs/en";

		if (args.Length > 0)
			path = args [0];
		int col = 0;
		string start = "UITableView.xml";
		bool started = true;
#if debug || true
		var e = XDocument.Load ("/tmp/fox1");
		var he = Convert (e.Root);
		var xe = ToXml (he).ToArray ();
		Console.WriteLine (e.ToString ());
		foreach (var x in xe)
			Console.Write (x.ToString ());
		Console.WriteLine ();
#endif

		foreach (var dir in Directory.GetDirectories (path)) {
			foreach (var file in Directory.GetFiles (dir, "*.xml")){
				if (file.EndsWith (start))
					started = true;
				if (!started)
					continue;

				currentFile = file;
				var d = XDocument.Load (file);
				if (d.Element ("Type") == null)
					continue;

				Process (d, "/Type/Members/Member/Docs/remarks");

				Process (d, "/Type/Docs/remarks");
				Process (d, "/Type/Docs/summary");

				Process (d, "/Type/Members/Member/Docs/summary");
				Process (d, "/Type/Members/Member/Docs/remarks");
				Process (d, "/Type/Members/Member/Docs/param");
				Process (d, "/Type/Members/Member/Docs/value");

				if ((col % 10) == 0)
					Console.Write (".");
				if (col++ > 600) {
					col = 0;
					Console.WriteLine ();
				}

				//Console.WriteLine (c);
			}
		}
		Console.WriteLine ("Done");
	}

	static void Process (XDocument d, string path)
	{
		var elements = d.XPathSelectElements (path);
		foreach (var element in elements) { 
			var str = element.ToString ();
			//Console.WriteLine (str);
			var html = Convert (element);
			var ret = ToXml (html);

			var sb = new StringBuilder ();
			foreach (var c in element.Nodes ()) {
				sb.Append (c.ToString ());
			}
			var expected = sb.ToString ();
			//estr = estr.Replace (" />", "/>");
			sb.Clear ();
			foreach (var c in ret) {
				try {
					if (c is XComment)
						sb.Append ((c as XComment).Value);
					else
						sb.Append (c.ToString ());
				} catch (ArgumentException e){
					// An XML comment cannot end with "-" looks like a bug
				}
			}
			var result = sb.ToString ();

			if (expected != result) {
				var diff = new XmlDiff (XmlDiffOptions.IgnoreWhitespace);
				var xexpected = new XmlTextReader (new StringReader ("<group>" + expected + "</group>"));
				var xresult = new XmlTextReader (new StringReader ("<group>" + result + "</group>"));
			
				var equal = diff.Compare (xexpected, xresult); //, new XmlTextWriter (Console.Out));


				if (!equal && expected != result) {
					bool found = false;
					for (int i = 0; i < expected.Length && i < result.Length; i++) {
						if (expected [i] != result [i]) {
							Report (expected, result, i);
							html = Convert (element);
							ret = ToXml (html);
							found = true;
							break;
						}
					}
					if (!found)
						Report (expected, result, Math.Min (expected.Length, result.Length));
				}
			}
		}
	}

	static void Report (string original, string news, int p)
	{
		int c;
		if (p > 10)
			c = p-10;
		else
			c = 0;
		Console.WriteLine (currentFile);
		return;
		Console.WriteLine ("\n{2}\nOriginal: {0}\nNew: {1}\n", OneLine (original.Substring (c)), OneLine (news.Substring (c)), currentFile);
	}

	static string OneLine (string l)
	{
		return l.Substring (0, Math.Min (40, l.Length));
	}

	static IEnumerable<XNode> ToXml (string htmlstr)
	{
		var doc = new HtmlDocument ();
		doc.LoadHtml (htmlstr);
		return ParseRoot (doc.DocumentNode).ToArray ();
	}

	static IEnumerable<XNode> ParseRoot (HtmlNode node)
	{
		foreach (var element in node.ChildNodes){
			switch (element.Name) {
			case "p":
				if (element.OuterHtml == "<p/>")
					yield return new XElement ("para");
				else
					yield return ParseP (element);
				break;
			case "div":
				foreach (var div in ParseDiv (element))
					yield return div;
				break;
			case "table":
				yield return ParseTable (element);
				break;
			case "ul":
			case "ol":
				yield return ParseList (element, element.Name == "ul" ? "bullet" : "number");
				break;
			case "a":
				yield return new XElement ("see", new XAttribute ("cref", element.Attributes ["href"].Value));
				break;
			case "code":
				var kind = element.Attributes ["class"].Value;
				switch (kind) {
				case "code":
					var c = new XElement ("c");
					foreach (var n in ParseRoot (element))
						c.Add (n);
					yield return c;
					break;
				case "langword":
					yield return new XElement ("see", new XAttribute ("langword", element.InnerText));
					break;
				case "paramref":
					yield return new XElement ("paramref", new XAttribute ("name", element.InnerText));
					break;
				case "typeparamref":
					yield return new XElement ("typeparamref", new XAttribute ("name", element.InnerText));
					break;
				default:
					throw new NotImplementedException ("Do not know how to handle <code class='" + kind);
				}
				break;
			case "img":
				yield return new XElement ("img", new XAttribute ("href", element.Attributes ["src"].Value));
				break;

			default:
				if (element is HtmlTextNode) {
					yield return new XText (HttpUtility.HtmlDecode ((element as HtmlTextNode).Text));
				} else if (element is HtmlCommentNode) {
					yield return new XComment ((element as HtmlCommentNode).Comment);
				} else
					throw new NotImplementedException ("Do not have support for " + element.Name);
				break;
			}
		}
	}

	static XElement ParseList (HtmlNode node, string kind)
	{
		var list = new XElement ("list", new XAttribute ("type", kind));

		foreach (var li in node.Elements ("li")) {
			list.Add (new XElement ("item", new XElement ("term", ParseRoot (li))));
		}
		return list;
	}

	static XElement ParseTable (HtmlNode node)
	{
		var ftr = node.ChildNodes ["tr"];
		var nodes = ftr.SelectNodes ("th");
		var term = new XElement ("term", ParseRoot (nodes [0]));
		var header = new XElement ("listheader", term);
		foreach (var desc in nodes.Skip (1))
			header.Add (new XElement ("description", ParseRoot (desc)));

		var list = new XElement ("list", new XAttribute ("type", "table"), header);

		int tr = 0;
		foreach (var child in node.SelectNodes ("tr").Skip (1)){

			var tds = child.SelectNodes ("td");
			term = new XElement ("term", ParseRoot (tds [0]));
			var item = new XElement ("item", term);

			foreach (var desc in tds.Skip (1))
				item.Add (new XElement ("description", ParseRoot (desc)));

			list.Add (item);
		}

		return list;
	}

	static XElement ParseP (HtmlNode node)
	{
		var xp = new XElement ("para");

		// tool=nullallowed -> tool=nullallowed
		// id=tool-remark -> tool=remark
		var tool = node.Attributes ["tool"];
		if (tool != null) {
			XAttribute a;

			var v = tool.Value;
			if (v == "tool-remark")
				a = new XAttribute ("id", "tool-remark");
			else
				a = new XAttribute ("tool", tool.Value);

			xp.Add (a);
		}
		var copied = node.Attributes ["copied"];
		if (copied != null) 
			xp.Add (new XAttribute ("copied", "true"));


		if (node.ChildNodes.Count == 0)
			xp.SetValue (string.Empty);
		foreach (var child in node.ChildNodes) {
			if (child is HtmlTextNode) {
				var tn = child as HtmlTextNode;
				xp.Add (new XText (HttpUtility.HtmlDecode (tn.Text)));
			} else if (child is HtmlNode) {
				var childName = child.Name;

				switch (child.Name) {
				case "a":
					xp.Add (new XElement ("see", new XAttribute ("cref", child.Attributes ["href"].Value)));
					break;
				case "img":
					xp.Add (new XElement ("img", new XAttribute ("href", child.Attributes ["src"].Value)));
					break;
				case "code":
					var kind = child.Attributes ["class"].Value;
					switch (kind) {
					case "code":
						var c = new XElement ("c");
						foreach (var n in ParseRoot (child))
							c.Add (n);
						xp.Add (c);
						break;
					case "langword":
						xp.Add (new XElement ("see", new XAttribute ("langword", child.InnerHtml)));
						break;
					case "paramref":
						xp.Add (new XElement ("paramref", new XAttribute ("name", child.InnerHtml)));
						break;
					case "typeparamref":
						xp.Add (new XElement ("typeparamref", new XAttribute ("name", child.InnerHtml)));
						break;
					default:
						throw new NotImplementedException ("Do not know how to handle <code class='" + kind);
					}
					break;
				case "div":
					foreach (var divNode in ParseDiv (child))
						xp.Add (divNode);
					break;
				case "table":
					xp.Add (ParseTable (child));
					break;
				case "ul":
				case "ol":
					xp.Add (ParseList (child, childName == "ul" ? "bullet" : "number"));
					break;
				default:
					throw new NotImplementedException ("Do not know how to handle " + child.Name);
				}
			}
		}
		return xp;
	}

	static void Verbatim (XElement target, HtmlNodeCollection nodes)
	{
		foreach (var n in nodes) {
			if (n is HtmlTextNode)
				target.Add (new XText ((n as HtmlTextNode).Text));
			else {
				var nt = new XElement (n.Name);
				foreach (var a in n.Attributes) {
					nt.Add (new XAttribute (a.Name, a.Value));
				}
				Verbatim (nt, n.ChildNodes);
				target.Add (nt);
			}
		}
	}

	static IEnumerable<XNode> ParseDiv (HtmlNode node)
	{
		var dclass = node.Attributes ["class"].Value;
		if (dclass.StartsWith ("lang-")) {
			var code = new XElement ("code", new XAttribute ("lang", dclass.Substring (5).Replace ("sharp", "#")));

			var cdata = node.Attributes ["data-cdata"];
			if (cdata != null) {
				code.Add (new XCData (HttpUtility.HtmlDecode (node.FirstChild.InnerText)));
			} else {
				if (node.FirstChild != null)
					code.Add (new XText (HttpUtility.HtmlDecode (node.FirstChild.InnerHtml)));
			}
			yield return code;
		} else if (dclass == "example") {
			var example = new XElement ("example");
			foreach (var child in node.ChildNodes) {
				if (child is HtmlTextNode)
					example.Add (new XText (HttpUtility.HtmlDecode ((child as HtmlTextNode).Text)));
				else if (child.Name != "div" || !child.Attributes ["class"].Value.StartsWith ("lang-"))
					throw new NotImplementedException ("Do not know how to handle " + child.OuterHtml);
				else
					example.Add (ParseDiv (child));
			}
			yield return example;
		} else if (dclass == "verbatim") {
			var format = new XElement ("format", new XAttribute ("type", "text/html"));
			Verbatim (format, node.ChildNodes);
			yield return format;
		} else if (dclass.StartsWith ("block")) {
			var kind = dclass.Substring (6);
			var block = new XElement ("block", new XAttribute ("subset", "none"), new XAttribute ("type", kind));
			foreach (var child in ParseRoot (node))
				block.Add (child);
			yield return block;
		} else if (dclass == "related") {
			var link = node.ChildNodes ["a"];
			if (link != null) {
				var href = link.Attributes ["href"].Value;
				var type = link.Attributes ["data-type"].Value;
				var text = (link.FirstChild as HtmlTextNode).Text;
				// this could be more robust, perhaps we should put the type not inside the child, but as an attribute in the <a>

				var related = new XElement ("related", new XAttribute ("type", type), new XAttribute ("href", href));
				if (text != null && text != "")
					related.Add (new XText (HttpUtility.HtmlDecode (text)));
				yield return related;
			}
		} else if (dclass == "cdata") {
			yield return new XCData (HttpUtility.HtmlDecode (node.InnerText));
		} else
			throw new NotImplementedException ("Unknown div style: " + dclass);
	}

	static int warnCount = 0;
	static void WarningDangling (XElement root, XNode node)
	{
		Console.WriteLine ("Warning {2}, dangling text at {1}\n{3} =>\n {0}", currentFile, root, warnCount++, node);
	}

	static string Convert (XElement root)
	{
		var sb = new StringBuilder ();
		bool seenPara = false;
		bool first = true;
		bool renderedText = false;
		int nodeCount = root.Nodes ().Count ();
		foreach (var node in root.Nodes ()) {
			if (node is XText){
				if (first) {
					if (root.IsEmpty)
						sb.Append ("<p/>");
					else {
						// Should we really RenderPara here?   Should we not just add the text, and continue?
						// The warning should just be about a para started if we had text initially:
						// IMPORTANT: If you uncomment the next line, change the "continue" for a "break"
						//sb.AppendFormat ("{0}", RenderPara (root.Nodes ()));
						sb.Append (EncodeText (node as XText));
					}
					renderedText = true;
				} else if (seenPara && currentFile.IndexOf ("MonoTouch.Dialog") == -1)
					WarningDangling (root, node);
				else
					sb.Append (EncodeText (node as XText));
				continue; // break
			}
			first = false;

			if (node is XElement) {
				var el = node as XElement;

				switch (el.Name.ToString ()) {
				case "para":
					seenPara = true;
					if (renderedText)
						WarningDangling (root, node);
					string attr = "";
					var toola = el.Attribute ("tool");
					if (toola != null) {
						attr = " tool='" + toola.Value + "'";
					} 
					var id = el.Attribute ("id");
					if (id != null) {
						attr = " tool='tool-remark'";
					}
					var copied = el.Attribute ("copied");
					if (copied != null) {
						attr += " copied='true'";
					}
					if (el.IsEmpty)
						sb.AppendFormat ("<p{0}/>", attr);
					else
						sb.AppendFormat ("<p{0}>{1}</p>", attr, RenderPara (el.Nodes ()));
					break;
				case "list":
					sb.Append (RenderList (el));
					break;
				case "code":
					sb.Append (RenderCode (el));
					break;
				case "format":
					sb.Append (RenderFormat (el));
					break;
				case "example":
					sb.Append (RenderExample (el));
					break;
				case "see":
					sb.Append (RenderSee (el));
					break;
				case "block":
					sb.Append (RenderBlock (el));
					break;
				case "related":
					sb.Append (RenderRelated (el));
					break;
				case "img":
					sb.Append (RenderImage (el));
					break;
				case "paramref":
					sb.Append (RenderParamRef (el));
					break;
				case "typeparamref":
					sb.Append (RenderTypeParamRef (el));
					break;
				case "c":
					sb.Append (RenderC (el));
					break;
				default:
					Console.WriteLine ("File: {0} node: {1}", currentFile, el);
					throw new NotImplementedException ("No support for handling nodes of type " + el.Name);
				}
			} else if (node is XComment) {
				var xc = node as XComment;
				sb.AppendFormat ("<!--{0}-->", xc.Value);
			} else
				throw new NotImplementedException ("Do not know how to handle " + node.GetType ());
		}
		return sb.ToString ();
	}

	static string EncodeText (XText text)
	{
		if (text.NodeType == XmlNodeType.CDATA)
			return string.Format ("<div class='cdata'>{0}</div>", HttpUtility.HtmlEncode (text.Value));
		else
			return HttpUtility.HtmlEncode (text.Value);
	}

	static string RenderRelated (XElement el)
	{
		var type = el.Attribute ("type").Value;
		var href = el.Attribute ("href").Value;
		return string.Format ("<div class='related'>Related: <a data-type='{0}' href='{1}'>{2}</a></div>", type, href, HttpUtility.HtmlEncode (el.Value));
	}

	static string RenderList (XElement el)
	{
		var sb = new StringBuilder ();
		var kind = el.Attribute ("type").Value;
		switch (kind) {
		case "bullet":
			sb.AppendFormat ("<ul>{0}</ul>", RenderListBullet (el.Elements ()));
			break;
		case "number":
			sb.AppendFormat ("<ol>{0}</ol>", RenderListBullet (el.Elements ()));
			break;
		case "table":
			sb.AppendFormat ("<table>\n{0}\n{1}\n</table>", 
				RenderTableElement ("th", el.Element ("listheader")), 
				string.Join ("\n", el.Elements ("item").Select (x => RenderTableElement ("td", x)).ToArray ()));
			break;
		default:
			throw new NotImplementedException ("list type not supported: " + kind);
		}
		return sb.ToString ();
	}

	// Renders a table element, the kind is used to render th or td
	// <term>..</term><description>..</description>+
	static string RenderTableElement (string kind, XElement top)
	{
		var sb = new StringBuilder ();
		sb.AppendFormat ("<tr>\n  <{0}>{1}</{0}>", kind, Convert (top.Element ("term")));

		foreach (var desc in top.Elements ("description"))
			sb.AppendFormat ("  <{0}>{1}</{0}>", kind, Convert (desc));
		sb.Append ("</tr>");
		return sb.ToString ();
	}

	// Renders <format>..</format>
	static string RenderFormat (XElement el)
	{
		var kind = el.Attribute ("type").Value;
		if (kind == "text/html") {
			return string.Format ("<div class='verbatim'>{0}</div>", Verbatim (el.Nodes ()));
		} else
			throw new NotImplementedException ("Do not support anything but <format type='text/html'>");
	}

	// Renders <example>...</example>
	static string RenderExample (XElement el)
	{
		return string.Format ("<div class='example'>{0}</div>", RenderPara (el.Nodes ()));
	}

	static string RenderBlock (XElement el)
	{
		var attr = el.Attribute ("type").Value;
		return string.Format ("<div class='block {0}'>{1}</div>", attr, Convert (el));
	}


	// REnders a <c>...</c> optionally with a CData
	static string RenderC (XElement el)
	{
		return string.Format ("<code class='code'>{0}</code>", Convert (el));
	}

	// Renders <para>...</para>
	static string RenderPara (IEnumerable<XNode> nodes)
	{
		var sb = new StringBuilder ();
		foreach (var node in nodes) {
			if (node is XText)
				sb.Append (HttpUtility.HtmlEncode ((node as XText).Value));
			else if (node is XElement) {
				var xel = node as XElement;
				if (xel.Name == "see") {
					sb.Append (RenderSee (xel));
				} else if (xel.Name == "img") {
					sb.Append (RenderImage (xel));
				} else if (xel.Name == "c") {
					sb.Append (RenderC (xel));
				} else if (xel.Name == "format") {
					sb.Append (RenderFormat (xel));
				} else if (xel.Name == "list") {
					sb.AppendFormat ("{0}", RenderList (xel));
				} else if (xel.Name == "paramref") {
					sb.Append (RenderParamRef (xel));
				} else if (xel.Name == "typeparamref") {
					sb.Append (RenderTypeParamRef (xel));
				} else if (xel.Name == "example") {
					Console.WriteLine ("EXAMPLE at {0}", currentFile);
				} else if (xel.Name == "code") {
					sb.Append (RenderCode (xel));
				} else {
					Console.WriteLine ("File: {0}, Node: {1}", currentFile, node);
					throw new NotImplementedException ("Unsupported element in RenderPara: " + xel.Name);
				}
			} else {
				throw new NotSupportedException ("Unknown node type: " + node.GetType ());
			}
		}
		return sb.ToString ();
	}

	static string RenderImage (XElement xel)
	{
		var target = xel.Attribute ("href").Value;
		return string.Format ("<img src='{0}'>", target);
	}

	static string RenderParamRef (XElement xel)
	{
		return string.Format ("<code class='paramref'>{0}</code>", xel.Attribute ("name").Value);
	}

	static string RenderTypeParamRef (XElement xel)
	{
		return string.Format ("<code class='typeparamref'>{0}</code>", xel.Attribute ("name").Value);
	}

	static string RenderSee (XElement xel)
	{
		var target = xel.Attribute ("cref");
		if (target != null)
			return string.Format ("<a href='{0}'>{0}</a>", target.Value);
		var lang = xel.Attribute ("langword").Value;
		return string.Format ("<code class='langword'>{0}</code>", lang);
	}

	static string RenderListBullet (IEnumerable<XElement> elements)
	{
		var sb = new StringBuilder ();
		foreach (var xel in elements) {
			if (xel.HasAttributes)
				throw new NotImplementedException ("Do not support attributes on list[bullet]/items");
			var children = xel.Elements ();
			if (children.Count () > 1) 
				throw new NotImplementedException ("Do not support more than one item inside list[bullet]/item");
			var first = children.FirstOrDefault ();
			if (first == null)
				sb.Append ("<li></li>");
			else if (first.Name == "term")
				sb.AppendFormat ("<li>{0}</li>", Convert (first));
			else
				throw new NotImplementedException ("Do not support anything but a term inside a list[bullet]/item");
		}
		return sb.ToString ();
	}

	static string RenderExample (IEnumerable<XElement> elements)
	{
		var sb = new StringBuilder ();
		foreach (var c in elements) {
			if (c.Name == "code") {
				sb.Append (RenderCode (c));
			} else
				throw new NotImplementedException ("Do not know how to handle inside an example a node of type " + c.Name);
		}
		return sb.ToString ();
	}

	static string RenderCode (XElement code)
	{
		var lang = code.Attribute ("lang").Value.Replace ("#", "sharp");
		if (lang == "")
			throw new NotImplementedException ("No language specified for <code> tag inside example");
		int count = code.Nodes ().Count ();

		if (count > 1) {
			Console.WriteLine ("NODES: {0}", code.Nodes ().Count ());
		}
		string value;
		var cdata = "";
		if (count > 0) {
			var child = code.FirstNode;

			if (child is XCData) {
				cdata = " data-cdata='true'";
				value = HttpUtility.HtmlEncode ((child as XCData).Value);
			} else
				value = child.ToString ();
		} else
			value = "";
		return "<div class='lang-" + lang + "'" + cdata + ">" + value + "</div>";
	}

	static string Verbatim (IEnumerable<XNode> nodes)
	{
		return String.Join (" ", nodes.Select (x => x.ToString ()).ToArray ());
	}
}
