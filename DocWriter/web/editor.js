
//
// This should insert a-span like HTML inside the stuff we are adding
//	
function insertSpanAtCursor(html){
	sel = window.getSelection ()
    if (sel.type == "Caret") {    	
        range = sel.getRangeAt(0);
        range.deleteContents();
	    newfrag = range.createContextualFragment (html);

        range.insertNode(newfrag);

        // now set the cursor
        range2 = range.cloneRange ();
        range2.setStartAfter (range.startContainer.nextSibling);
        range2.collapse (true);
        sel.removeAllRanges ();
        sel.addRange (range2);
    }
}

// Use this to add stuff after the current node, for example to
// add a table, or add a div with an example, since it shoudl not 
// get inlined

function insertHtmlAfterCurrentNode (html)
{
	sel = window.getSelection ()
    if (sel.type == "Caret"	) {
    	cl = sel.focusNode.attributes ["class"];
    	if (cl.value == "edit")
    	   appendon = sel.focusNode;
    	else {
	        cl = sel.focusNode.parentNode.attributes ["class"]; 
	    	if (cl == undefined || cl.value == "edit"){
	    	   appendon = sel.focusNode.parentNode;
	    	} else {
			   appendon = sel.focusNode.parentNode.parentNode;
	        }
        }
        range = sel.getRangeAt(0);
        appendon.appendChild (range.createContextualFragment (html));
    }
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
