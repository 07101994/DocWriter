function insertTextAtCursor(text) {
    var sel, range, html;
    if (window.getSelection) {
        sel = window.getSelection();
        if (sel.type == "Caret" && sel.getRangeAt && sel.rangeCount) {
            range = sel.getRangeAt(0);
            range.deleteContents();
            range.insertNode( document.createTextNode(text) );
        }
    } else if (document.selection && document.selection.createRange) {
        document.selection.createRange().text = text;
    }
}

function getCaretPosition(editableDiv) {
    var caretPos = 0, containerEl = null, sel, range;
    if (window.getSelection) {
        sel = window.getSelection();
        if (sel.rangeCount) {
            range = sel.getRangeAt(0);
	    return range;

            if (range.commonAncestorContainer.parentNode == editableDiv) {
                caretPos = range.endOffset;
            }
        }
    } else if (document.selection && document.selection.createRange) {
        range = document.selection.createRange();
        if (range.parentElement() == editableDiv) {
            var tempEl = document.createElement("span");
            editableDiv.insertBefore(tempEl, editableDiv.firstChild);
            var tempRange = range.duplicate();
            tempRange.moveToElementText(tempEl);
            tempRange.setEndPoint("EndToEnd", range);
	    return tempRange;
            caretPos = tempRange.text.length;
        }
    }
    return caretPos;
}

function foo ()
{
    p = getCaretPosition ($("#edit"));
    p.insertNode (document.createTextNode ("foo"));
}

function getHtml(xid)
{
    if (document == null)
    	return "<>Odd";
    element = document.getElementById (xid);
	if (element === null)
		return "<>It is null and id=" + xid;

    return element.innerHTML;
}

function insertHtmlAtCursor (html)
{
    insertTextAtCursor (html);
}
