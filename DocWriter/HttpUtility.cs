using HtmlAgilityPack;

namespace System.Web
{
	static class HttpUtility
	{
		public static string HtmlDecode (string text)
			=> HtmlEntity.DeEntitize (text);

		public static string HtmlEncode (string text)
			=> HtmlEntity.Entitize (text, true, true);
	}
}