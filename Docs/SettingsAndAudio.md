# Settings and navigation

Settings has exactly two sliders, **Master Volume** and **Camera Shake**, plus **Back**.
Both use PlayerPrefs, save on change and apply immediately, including while paused.
Old mute/category preferences no longer silently override the master control.
Internal bus headroom remains .45 earthquake, .50 warning, .28 impact and .20 feedback,
with a fixed .8 internal category balance. Master zero immediately zeros bound sources.
Normal unscaled pause/result fades remain; there are no preview sources or test buttons.

Camera Shake scales continuous rotation, translation, low-frequency sway, impact kicks
and the shelter FOV pulse. Sway previously moved the CharacterController independently
of the slider, which made zero look ineffective and also moved the player. It now
belongs to the camera's child transform. Zero immediately restores its identity
transform and base FOV; the parent pivot retains mouse look and crouch height. The
preference does not change hazard physics, shelter, movement speeds or round timing.

Main Menu, Pause and results use shared button colors, explicit selected states and
MenuNavigation (Up/Down, Tab/Shift-Tab, Enter/Space). Sliders retain Left/Right adjustment.
How to Play and Settings save and restore the precise caller state. Opening Settings
from Pause hides that panel; Back/Esc restores it with time and audio still paused.
Opening Help during gameplay returns to gameplay. Esc also returns Level Select to
Main Menu. A consumed-frame guard prevents the closing Esc from also resuming Pause.
Canvas scaling balances width and height across aspect ratios.

See [Level1Validation.md](Level1Validation.md) for actual physics, Play Mode and rendered
checks. Scripted navigation and captures are not a listening or keyboard-only human
playtest.
