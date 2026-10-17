# Celesphonia Delta Launcher v1.0.14 - Interface redesign

This release reorganizes the launcher interface without changing saved-game formats, game archives, translation dictionaries or installed game contents.

## Changes

- Manage games from the library: right-click an installed version to Play, Open Game, Open Saves, Back Up, English, Upgrade, Remove from launcher or Uninstall managed game files.
- Clearer Install flow: target and source selectors, concise source status, one primary action and More options for uncommon paths. Default selection is v1.052, not v1.01.
- Simplified Play, Files, Saves, English and Help pages with common actions promoted and duplicated instructions removed.
- Consistent controls and menus: keyboard focus, readable text, checked states, submenu arrows and themed popups.
- Responsive window layouts and safeguards against hidden navigation or inaccessible actions.

## Installation

Close the existing launcher and extract the verified portable ZIP over its runtime files. Keep user profiles.json, original ZIP/RAR/7z downloads, Game Profiles, saves, Save Backups and custom files unchanged.

## Verification scope

Native Windows compiled UI tests across six pages, window sizes, themes, context menus and non-destructive removal previews. The 35-file archive contains only runtime and English support assets; game and save data are not distributed. Underlying v1.0.13 interoperability and English translation coverage are unchanged.
`n