using System.Linq;
using System.Text;

namespace DocWriter
{
	public partial class MemberTemplate : TemplateBase<DocMember>
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

			sb.AppendLine($@"    <div class='caption'>{Model.Name}<br>");
			sb.AppendLine($@"        <div style='font-size:80%;'>{Model.Kind}</div>");
			sb.AppendLine($@"        <div style='font-size:80%;' class='title-code'>{Model.SignatureHtml}</div>");
			sb.AppendLine($@"    </div>");

			if (Model.IsAutodocumented)
			{
				sb.AppendLine($@"    <div class='autodocumented'>");
				sb.AppendLine($@"        This documentation is automatically generated. Do not override without cause.");
				sb.AppendLine($@"    </div>");
			}

			sb.AppendLine($@"    <div class='title'>Summary:<span id='summary-status'></span></div>");
			sb.AppendLine($@"    <div class='edit' id='summary' contenteditable='true'>{Model.GetHtmlForNode("Docs/summary")}</div>");

			var parameters = Model.Params.ToArray();
			if (parameters.Length > 0)
			{
				sb.AppendLine($@"    <div class='title'>Parameters:</div>");
				foreach (var p in parameters)
				{
					var name = p.Attribute("name").Value;
					var paramid = "param-" + name;
					sb.AppendLine($@"    <div class='parameter-name'>{name}<span id={paramid}-status'></span></div>");
					sb.AppendLine($@"    <div class='edit parameter-doc' contenteditable='true' id='{paramid}'>{Model.ToHtml(p)}</div>");
				}
			}

			if (Model.Value != null)
			{
				sb.AppendLine($@"    <div class='title'>Return Value:<span id='value-status'></span></div>");
				sb.AppendLine($@"    <div class='edit value' contenteditable='true' id='value'>{Model.ToHtml(Model.Value)}</div>");
			}
			if (Model.ReturnValue != null)
			{
				sb.AppendLine($@"    <div class='title'>Return Value:<span id='return-status'></span></div>");
				sb.AppendLine($@"    <div class='edit return' contenteditable='true' id='return'>{Model.ToHtml(Model.ReturnValue)}</div>");
			}
			if (Model.Remarks != null)
			{
				sb.AppendLine($@"    <div class='title'>Remarks:<span id='remarks-status'></span></div>");
				sb.AppendLine($@"    <div class='edit' id='remarks' contenteditable='true'>{Model.GetHtmlForNode("Docs/remarks")}</div>");
			}

			sb.AppendLine($@"</body>");

			sb.AppendLine($@"</html>");

			return sb.ToString();
		}
	}
}
