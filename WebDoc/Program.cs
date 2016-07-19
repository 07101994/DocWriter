using System;
using System.Threading;
using DocWriter;
using Nancy;
using Nancy.Hosting.Self;

namespace WebDoc
{
	public class EditModule : NancyModule
	{
		DocModel model;

		static EditModule ()
		{
			model = new DocModel ("/cvs/urho/docs");
		}

		public EditModule ()
		{
			
			Get ["/"] = parameters => "Hello World";
		}
	}

	class MainClass
	{
		public static void Main (string [] args)
		{
			using (var host = new NancyHost (new Uri ("http://localhost:1234"))) {
				host.Start ();
				Console.WriteLine ("Running on http://localhost:1234");
				while (true) {
					Thread.Sleep (1000);
				}

			}
		}
	}
}
