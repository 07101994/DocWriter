// 
// List of modified elements
//
dirty = {}

//
// This should insert a-span like HTML inside the stuff we are adding
//	
function insertSpanAtCursor(html){
	sel = window.getSelection ();
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
    	
    	attrs = sel.focusNode.attributes;
    	if (attrs && attrs ["class"] && attrs ["class"].value == "edit")
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
		return "<<<<It is null and id=" + xid;

    return element.innerHTML;
}

// Tracks changes on a content editable node whose name is 'node'
// and stores the id of the node on chages in the dirty dictionary
function trackDirty (node)
{
    var domNode = document.getElementById (node);
    domNode.addEventListener ("input", function () {
	    dirty [domNode.id] = domNode.id;
        console.log ("Logging dirty " + domNode.id);
    }, false);
}

function inTable ()
{
    sel = window.getSelection ();

    for (node = sel.focusNode; node; node = node.parentNode){
    	if ($(node).hasClass ("edit")){
    		return false;
    	} 
    	if (node.nodeName == "TABLE")
			return true;
    }
    return false;
}

function moveCursorTo (node,event)
{
    var sel = document.getSelection ();
    var range = document.createRange ();
    range.setStart (node, 0);
    range.collapse (true);
    sel.removeAllRanges ();
    sel.addRange (range);
	event.preventDefault();
	event.stopPropagation();
}

function keypressHandler (event)
{
    console.log (event.which)
    ee = event;

    //
    // On return, inside tables, we move from cell to cell, and at the end, we add more cells on demand
    //
    if (event.which == 13){
    	if (inTable ()){
    		var sel = document.getSelection ();
    		var n = sel.focusNode;
    		inHeader = $(n).closest ("th");
    		if (inHeader.length > 0){
    			if (inHeader.next ().length == 1){
    				moveCursorTo (inHeader.next ()[0], event);
    			} else {
					moveCursorTo (inHeader.parent ().next ().children ()[0], event);
				}
    		} else {
	    		next = $(n).closest ("td").next ("td");
	    		if (next.length > 0){
	    			moveCursorTo (next[0], event);
	    		} else {
					// right side of the table.
					if ($(n).closest ("tr").next ().length == 0)
						$($(n).closest ("td")[0].parentNode).after ("<tr><td>foo</td><td>bar</td></tr>");
					
					moveCursorTo ($(n).closest ("tr").next ().children ()[0], event);
				}
			}
        }
    }
}

$(document).ready(function(){
   $(".edit").each (function (idx, element){ 
      trackDirty (element.id);
   });
   $(".edit").on ("keypress", keypressHandler);
});

function getDirtyNodes ()
{
   var r = ""; 
   for (var k in dirty) 
       r = k + " " + r;
   dirty = {};
	return r;
}

function postError (nodeId)
{
	var n = $("#" + nodeId + "-status");

	n.html ("Parse Error");
	n.addClass ("perror");
}

function postOk (nodeId)
{
	n = $("#" + nodeId + "-status");
	n.html ("");
	n.removeClass ("perror");
}

