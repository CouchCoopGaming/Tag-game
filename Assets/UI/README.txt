The couch menu is built in code (uGUI) from Assets/Scripts/UI/Menu.
Display type is Liberation Sans Bold (SIL OFL 1.1). The font and license are in Fonts/.
Arena overview photos used as card thumbs are in ArenaThumbs/.
Runtime copies also live under Assets/Resources/UI so a player build can load them.
If Resources is empty, the menu falls back to LegacyRuntime.ttf, then Arial.ttf.
Opening the project bakes the Hier catalog, arena thumbs, and mesh portraits when those files are missing or older than the source. A player build does the same step before it packs. Until a portrait exists, the character grid draws a bust in that color.
Tag → Menu → Capture Screens writes Docs/UiStills/captures/. The same walk is Tag.Ui.Menu.MenuScreenCapture.Capture with -screenshot.
