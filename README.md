DocWriter
=========

Desktop Editor for the ECMA XML Documentation.

Currently the documentation it loads is hardcoded to /cvs/mt/ios-api-docs

TODO: 
=====

- [X] Javascript API to inject HTML at the cursor position (we have
      inject text, just not inject HTML, needed for inserting actual ECMA
      snippets)
- [ ] Add image loading support
- [ ] Allow editing of the enums at the type level, without having to go element by element
- [ ] Force a save on quit.
- [ ] Add try/catch around the validation timer, and catch errors there
- [X] Hookup Command-S to save, for the paranoid in us.
- [ ] Hotkeys to insert various kinds of markup
- [X] Implement an "Insert HTML text" option as we can currently only add text at the current location.
- [ ] Member Lookup UI
- [ ] Allow documentation to be loaded from another location

Input Events
============

Use the Input events like this:

     document.getElementById("editor").addEventListener("input", function() {
         alert("input event fired");
     }, false);

To track when an element has been changed.   This way we should only need to validate
and parse the function on changes, not on a continuous timer.

This will also allow the efficient handling of being able to render large lists of
summaries for types/members for example.

Rendering of Code
=================

Currently the "shell" around rendering is done by the Example, not by
the <code> sections, so standalone code sections look a little
strange.  This requires a little tooling on the CSS and the generated
output to fix.

Inserting HTML snippets
=======================

This is going to be harder than I expected.

There are two kinds of things we want to insert: entire blocks and
inline elements.

Blocks are things like <code>, <example>, <block>, <related>.  And
inline elements are the <c>System.String</c> or <see cref="T:Int"/> references.

The former requires adding new nodes after the current node.   

The latter requires splitting the current node in two, and inserting
the new element in between.   I have not found an easy way of doing that.

The best I could come up with is to use jQuery's $(node).html (),
inserting the elements inside the resulting string, and then set the
$(node).html to the resulting value.    Then, you must use something like:

	     range = document.createRange ()
	     range.setStart (findTheSecondElementAfterSnippet, 0)
	     range.setEnd (findTheSecondElementAfterSnippet, 0);
	     range.collapse (true)
	     window.getSelection ().removeAllRanges ();
	     window.getSelection ().addRange (range)


TODO HTML handling
==================

Currently when we edit members of the form <summary>text</summary> we
turn that into "text" and if the user adds a line, it becomes
"text<p>newstuff</p>".  This is not suitable for ECMA XML, so we now
have a wrapper function that will detect things like this and produce
"<para>text</para><para>newstuff</para>".

This is an opt-in feature during the editing, as it can alter existing
text, so it is not being used in the conversion test suite.

Tests
=====

Currently I use the body of documentation from ios-api-docs as a test.    Use the
Convert solution to load, it will try to convert all the docs to HTML and then back
from HTML into ECMA XML.

Then it uses an XML diff that ignores whitespace to determine if the
roundtripping works.  The code asserts aggressively to detect cases
that it can not handle.


Member Lookup UI
================

This is the user interface that pops up when you hit a keystroke to
add a link to a member.  It should offer member completion as you
type, to ensure that we actually insert references to valid targets.

It should have the ability to complete both the members in the
currently edited document as well as members from the system installed
documents (using MonoDoc.dll to resolve that)

Wanted:
=======

- [ ] Flag members that are auto-documented as such, to now waste documenters time on it.


Save Strategy
=============

Current save strategy is not great: it saves the content of the page on quit, but this would
not work very well when we render a page that aggragatges many children (a namespace or all members)
since it would only trigger a save on switch, and that might be too late (specially during the debugging
stages of this).

For something like that to work, it might be nice to save dirty elements on focus change.

Debating
=========

Render all summaries at type or namespace level, and provide a
dedicated editor to just fill those out quickly?
