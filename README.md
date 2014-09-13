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
- [x] Allow editing of the enums at the type level, without having to go element by element
- [X] Force a save on quit.
- [X] Add try/catch around the validation timer, and catch errors there
- [X] Hookup Command-S to save, for the paranoid in us.
- [ ] Hotkeys to insert various kinds of markup
- [X] Implement an "Insert HTML text" option as we can currently only add text at the current location.
- [ ] Member Lookup UI (see below)
- [ ] Allow documentation to be loaded from another location
- [X] Render parse errors as part of the HTML (add a div that we can use to inject errors/colors on parse errors, so that the message is shown in the correct context (specially important for long lists).
- [ ] Namespace functionality editing
- [ ] Support for editing delegates 

Commands:
- [ ] Hot key to insert common elements that might be referenced from the current item.   On a class, those might be members for example, on a member, those could be parameters.
- [x] Insert Table
- [x] Insert row to table
- [ ] Add Column
- [ ] Remove Column
- [ ] Insert number list
- [x] Insert bullet list
- [ ] Insert HTTP url (done, need UI to enter the Link/Caption for URL links)

Bugs
====

Inserting an Example after an Example nests the example. (Command-E twice)

It is not possible to add text after an Example.  Perhaps we need to
insert a spare div to allow editing after an example.

Big Ideas
=========

Perhaps we could host more than one Web View, host couple of side web
views that would dynamically get the contents from Googling on
StackOverflow side-by-side.

Perhaps show the source code for the binding for a particular API.

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
documents (using MonoDoc.dll to resolve that).

It currently supports filtering the namespaces (only), and provides
simple code completion when pressing TAB, but does not currently have
support for inserting the result or for going beyond the namespace.

While we extract the prefix, we are not currently using it.

Ideally it should detect that it has a complete namespace, and if it
does, and you press ".", then it shows types, and the process is
repeated, when it knows you have a full type and you press "." it
should display members.

Currently, there is no really support for inserting the proper prefix,
it is just hardcoded to "N:".

Wanted:
=======

- [ ] Flag members that are auto-documented as such, to now waste documenters time on it.

