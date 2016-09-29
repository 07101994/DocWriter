using System.Linq;
using System.Text;

namespace DocWriter
{
	public partial class NamespaceTemplate : TemplateBase<DocNamespace>
	{
		public override string GenerateString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($@"<html>");

			sb.AppendLine($@"<head>");
			sb.AppendLine($@"    <meta http-equiv='X-UA-Compatible' content='IE=edge' />");
			sb.AppendLine($@"    <script type='text/javascript'>{EmbeddedResourceReader.Get("jquery.min.js")}</script>");
			sb.AppendLine($@"    <script type='text/javascript'>{EmbeddedResourceReader.Get("editor.js")}</script>");
			sb.AppendLine($@"    <style>@{EmbeddedResourceReader.Get("style.css")}</style>");
			sb.AppendLine($@"</head>");

			sb.AppendLine($@"<body>");

			sb.AppendLine($@"    <div class='caption'>{Model.Name}</div>");

			sb.AppendLine($@"    <div class='title'>Summary:<span id='summary-status'></span></div>");
			sb.AppendLine($@"    <div class='edit' id='summary' contenteditable='true'>{Model.SummaryHtml}</div>");

			sb.AppendLine($@"    <div class='title'>Remarks:<span id='remarks-status'></span></div>");
			sb.AppendLine($@"    <div class='edit' id='remarks' contenteditable='true'>{Model.RemarksHtml}</div>");

			if (Model.NodeCount > 0)
			{
				sb.AppendLine($@"    <div class='caption'>Types</div>");
				for (int n = 0; n < Model.NodeCount; n++)
				{
					var node = Model[n];
					sb.AppendLine($@"    <div class='title'>{node.KindHtml}  <a href='ecma://T:{node.FullName}'>{node.Name}</a><span id='summary-{n}-status'></span></div>");
					sb.AppendLine($@"    <div class='edit' id='summary-{n}' contenteditable='true'>{node.SummaryHtml}</div>");
				}
			}

			sb.AppendLine($@"</body>");

			sb.AppendLine($@"</html>");

			return sb.ToString();
		}
	}
}
