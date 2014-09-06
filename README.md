DocWriter
=========

Desktop Editor for the ECMA XML Documentation.

Currently the documentation it loads is hardcoded to /cvs/mt/ios-api-docs

TODO: 
=====

- [ ] Javascript API to inject HTML at the cursor position (we have
      inject text, just not inject HTML, needed for inserting actual ECMA
      snippets)
- [ ] Add image loading support
- [ ] Allow editing of the enums at the type level, without having to go element by element
- [ ] Force a save on quit.
- [ ] Add try/catch around the validation timer, and catch errors there
- [ ] Hookup Command-S to save, for the paranoid in us.
- [ ] Hotkeys to insert various kinds of markup
- [ ] Implement an "Insert HTML text" option as we can currently only add text at the current location.
- [ ] Member Lookup UI

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
