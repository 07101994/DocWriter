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
- [X] Force a save on quit.
- [X] Add try/catch around the validation timer, and catch errors there
- [X] Hookup Command-S to save, for the paranoid in us.
- [ ] Hotkeys to insert various kinds of markup
- [X] Implement an "Insert HTML text" option as we can currently only add text at the current location.
- [ ] Member Lookup UI
- [ ] Allow documentation to be loaded from another location
- [X] Render parse errors as part of the HTML (add a div that we can use to inject errors/colors on parse errors, so that the message is shown in the correct context (specially important for long lists).
- [ ] Namespace functionality editing

Commands:
- [x] Insert Table
- [ ] Insert row to table
- [ ] Add Column
- [ ] Remove Column
- [ ] Insert number list
- [x] Insert bullet list
- [ ] Insert HTTP url

Focus Next cell
==============

Use this to find the next cell:

    $(sel.focusNode).closest ("td").next ("td")

Use this to set the focus:

    dom = $(s).get (0)
    sel.setStart (dom, 0);
    sel.setEnd (dom, 0);
    .. do the rest of the dance

Use this to add a row:

$($(sel.focusNode).closest ("td")[0].parentNode).after ("<tr><td>foo</td><td>bar</td></tr>")


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

