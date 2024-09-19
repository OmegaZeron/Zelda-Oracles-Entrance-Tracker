# Oracles Entrance Randomizer Tracker
This is an entrance tracker for the Oracles Entrance Randomizer. Currently Seasons only. No logic is included, this is just for entrances.
## Features
- Map-based entrance tracking
- Click to follow entrance chains
- Swap between Natzu/Nuun maps on the fly
- Decoupled entrance support
- Auto-save and multiple save files
- Mark entrances as "useless"
- Map zoom and pan
## Coming Soon
- Ages support
## Known Bugs
- When zooming the camera, the very top of the map can get slightly cut off or show an extra gray band
## Controls
- All entrances start as red. Left click an entrance to start the linking process. Left click another entrance to complete the link
- Left clicking on a linked (green) entrance will jump the camera to its partner entrance
  - In decoupled mode, each entrance has 2 colors, left side representing as an entrance, and the right side representing as an exit
    - Green - Linked to an exit
	- Yellow - An entrance links to this exit
	- Red - Not linked
	- Gray - Useless
  - Ctrl+LMB on a decoupled exit to jump to the linked entrance
- Shift+LMB an entrance to turn it gray, locking it in a "useless" state. Prevents linking to that entrance
  - Shift+RMB in decoupled mode marks the exit side as "useless"
- Right click any entrance to undo all modifications, turning it red again. Right click also stops the current link process
  - Won't undo decoupled exit data
- WASD, Arrow keys, and MMB+drag will pan the map. Scroll wheel will zoom in and out
- Buttons at the top of the screen will change the state of the map between overworld and inners, Holodrum and Subrosia, Present and Past, and which Natzu/Nuun companion area to display. Changing the Natzu/Nuun area will keep previous links
- Entrances are auto-saved by default, but can be changed in the settings menu
  - Default save files are in the same location as the .exe in seasons.bin, ages.bin, seasons_d.bin, and ages_d.bin, the latter two being for decoupled mode
  - Ctrl+S to Save, Ctrl+Shift+S to Save As
  - Ctrl+O to Load. Can also drag a save file onto the tracker window to Load