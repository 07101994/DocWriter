using System;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using System.Text;
using Microsoft.XmlDiffPatch;
using System.Xml;

public class TestDriver
{
	internal static string currentFile = "none";

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
		var he = DocConverter.ToHtml (e.Root, "fox1");
		var xe = DocConverter.ToXml (he).ToArray ();
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
			var html = DocConverter.ToHtml (element, currentFile);
			var ret = DocConverter.ToXml (html);

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

							// We redo the steps above, purely as it is easier to debug what happened right after 
							// the error is reported.
							html = DocConverter.ToHtml (element, currentFile);
							ret = DocConverter.ToXml (html);
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
		Console.WriteLine ("\n{2}\nOriginal: {0}\nNew: {1}\n", OneLine (original.Substring (c)), OneLine (news.Substring (c)), currentFile);
	}

	static string OneLine (string l)
	{
		return l.Substring (0, Math.Min (40, l.Length));
	}
}