# Settings, audio, and navigation

Settings contains **Master Volume**, **Camera Shake**, and **Back**. The sliders display percentages, apply immediately, and save when the panel closes. Master Volume uses the listener’s global volume, so zero silences the rumble, warnings, impacts, and feedback together. Camera Shake zero restores the camera’s rest position, rotation, and field of view. Mouse look, movement, and physical hazards still work at zero.

The quake rumble is synthesized from a continuous loop with several low-frequency partials. The game mixes that sound below brief local creaks, with separate levels for impacts and cover feedback. The settings keep the volume selected by the player through room and menu changes. The listener suspends gameplay audio during Pause and Help, then restores the caller state on return. Results end the round’s sound and movement.

## Menu controls

- The Main Menu offers Play, Level Select, Settings, How to Play, and Quit.
- Pause offers Resume, How to Play, Settings, Restart Level, and Main Menu.
- Use Up/Down or Tab/Shift+Tab to change selection and Enter to activate a button. Use Left/Right to change sliders.
- Esc pauses in a level and backs out of menu overlays. H opens How to Play during an attempt. R retries from a result.

How to Play uses four cards for the goal, movement, cover, and hazards. The cover card shows a crouched player underneath a table. The HUD presents the objective, countdown, and shelter state. Result actions show Retry Level and Main Menu, with Next Level after a win when another room remains.

Earlier scripted engineering checks and renders are logged in [Level1Validation.md](Level1Validation.md). Record current-build listening comfort and keyboard navigation in [Playtest_Notes.md](Playtest_Notes.md).
