
// 
// List of modified elements
//
dirty = {}

function track(element)
{
    var edit = $(element).closest (".edit");
	if (edit.length > 0){
	    dirty [edit [0].id] = edit [0].id;
	    console.log ("Dirty: " + edit [0].id);
    }
}

//
// This should insert a-span like HTML inside the stuff we are adding
//	
function insertSpanAtCursor(html)
{
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
        track (range.startContainer);
    }
}

//
function selectionToCode(cname)
{
	sel = window.getSelection ();
    if (sel.type == "Range") {    	
        range = sel.getRangeAt(0);
	    var c = document.createElement ("code");
		$(c).addClass (cname);

        range.surroundContents (c);
        track (range.startContainer);
	}
}

//
// Returns true if the specified node is in a editable content section
// either itself, or one of its parents
//
function editableNode (jnode)
{
    for (;jnode; jnode = jnode.parent ()){
    	if ($(jnode).hasClass ("edit")){
    		return true;
    	} 
    	if (jnode.is ("body"))
    	    return false;
    }
    return false;

}

// Use this to add stuff after the current node, for example to
// add a table, or add a div with an example, since it shoudl not 
// get inlined

function insertHtmlAfterCurrentNode (html)
{
	sel = window.getSelection ()
    if (sel.type == "Caret"	) {
    	
    	if (editableNode ($(sel.focusNode)))
    	   appendon = sel.focusNode;
    	else {
	        cl = sel.focusNode.parentNode.attributes ["class"]; 
	    	if (cl == undefined || $(sel.focusNode.parentNode).hasClass ("edit")){
	    	   appendon = sel.focusNode.parentNode;
	    	} else {
			   appendon = sel.focusNode.parentNode.parentNode;
	        }
        }
        range = sel.getRangeAt(0);
        appendon.appendChild (range.createContextualFragment (html));
        track (appendon);
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

function getText(xid)
{
    if (document == null)
    	return "<>Odd";
    element = document.getElementById (xid);
	if (element === null)
		return "<<<<It is null and id=" + xid;

    return element.text;
}

// Tracks changes on a content editable node whose name is 'node'
// and stores the id of the node on chages in the dirty dictionary
function trackDirty (node)
{
    var domNode = document.getElementById (node);
    domNode.addEventListener ("input", function () {
	    dirty [domNode.id] = domNode.id;
	    if ($(domNode).html () == "To be added."){
	    	$(domNode).addClass ("to-be-added");
	    } else {
			$(domNode).removeClass ("to-be-added");
		}
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
      if ($(element).html () == "To be added."){
	    	$(element).addClass ("to-be-added");
      }
   });
   $(".edit").on ("keypress", keypressHandler);
});

// Returns a space separate list of nodes that have been modified by the user
function getDirtyNodes ()
{
   var r = ""; 
   for (var k in dirty) 
       r = k + " " + r;
   dirty = {};
	return r;
}

// Annodates a nodeId-status with a parse error, used to signal parsing problems in a content editable
function postError (nodeId)
{
	var n = $("#" + nodeId + "-status");

	n.html ("Parse Error");
	n.addClass ("perror");
}
// Annodates a nodeId-status with a clean bill of health, parsing-wise
function postOk (nodeId)
{
	n = $("#" + nodeId + "-status");
	n.html ("");
	n.removeClass ("perror");
}

