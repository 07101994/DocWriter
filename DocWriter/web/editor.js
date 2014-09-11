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
    console.log ("node: " + node + " and  " + node.nodeValue);
    	console.log ("node: " + node.nodeName);
    	if ($(node).hasClass ("edit")){
    	console.log ("Got an edit");
    		return false;
    	} 
    	if (node.nodeName == "TABLE")
			return true;
		if (!node.parentNode)
console.log ("Got a null parent");
    }
    return false;
}
function keypressHandler (event)
{
    console.log (event.which)
    ee = event;
    if (event.which == 13){
    	if (inTable ()){
    		
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

