//
// EmbeddedResourceReader.cs: Support for reading embedded resources
//
// Author:
//   Matthew Leibowitz (matthew.leibowitz@xamarin.com)
//
// Copyright 2016 Xamarin Inc
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DocWriter
{
	public static class EmbeddedResourceReader
	{
		private readonly static Dictionary<string, string> resources = new Dictionary<string, string>();

		public static string Get(string path)
		{
			var cleaned = "." + path.Replace('/', '.').Replace('\\', '.').ToLowerInvariant();
			if (resources.ContainsKey(cleaned))
			{
				return resources[cleaned];
			}

			var assembly = typeof(EmbeddedResourceReader).Assembly;
			var name = assembly.GetManifestResourceNames().First(n => n.EndsWith(cleaned, StringComparison.OrdinalIgnoreCase));
			using (var resource = assembly.GetManifestResourceStream(name))
			using (var reader = new StreamReader(resource))
			{
				return resources[cleaned] = reader.ReadToEnd();
			}
		}
	}
}
