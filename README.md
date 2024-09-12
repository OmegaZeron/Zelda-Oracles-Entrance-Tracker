# Oracles AP Entrance Tracker
This is an entrance tracker for the Oracles Entrance Randomizer. Currently Seasons only. No logic is included, this is just for entrances.
## Features
- Map-based entrance tracking
- Click to follow entrance chains
- Swap between Natzu/Nuun maps on the fly
- Decoupled entrance support
- Auto-save
- Mark entrances as "useless"
- Map zoom and pan
## Coming Soon
- Multiple save files and load menu
- Ages support
## Known Bugs
- When zooming the camera, the very top of the map can get slightly cut off or show an extra gray band
## Controls
- All entrances start as red. Left click an entrance to start the linking process. Left click another entrance to complete the link. This will turn both entrances green
- Left clicking on a linked (green) entrance will jump the camera to its partner entrance
  - In decoupled mode, linking entrances will turn the entrance green, but the exit yellow (if the exit doesn't have its own link)
  - Ctrl+Click on a decoupled exit to jump to the linked entrance
- Shift+Click an entrance to turn it gray, locking it in a "useless" state. Prevents linking to that entrance
- Right click any entrance to undo all modifications, turning it red again. Right click also stops the current link process
- WASD, Arrow keys, and MMB+drag will pan the map. Scroll wheel will zoom in and out
- Buttons at the top of the screen will change the state of the map between overworld and inners, Holodrum and Subrosia, Present and Past, and which Natzu/Nuun companion area to display. Changing the Natzu/Nuun area will keep previous links
- Entrances are auto-saved. Save file is in the same location as the .exe in seasons.bin, ages.bin, seasons_d.bin, and ages_d.bin, the latter two being for decoupled mode. Multiple save files are planned, but for now if you're playing multiple of the same mode, you'll have to move/rename the files