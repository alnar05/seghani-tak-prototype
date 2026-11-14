# Instructions, Settings, sound and comfort follow-up

## Implementation

A compact HUD goal remains visible during exploration and shaking; the latter
shows remaining seconds and cover status. Detailed instructions are in How to play,
opened from Main Menu, Pause, the HUD button or H. Opening it during play freezes
scaled gameplay time and fades gameplay audio. Esc/Back restores the caller's time,
audio pause state, cursor and selected control. Opening Settings from Pause hides
that menu; Back returns to Pause without resuming. Opening it from Main Menu returns
to Main Menu. Instructions and Settings support arrows, Tab/Shift-Tab, Enter/Space
and Esc, with explicit focus inside the modal screens.

Settings contains Master, Earthquake, Warning, Impact and Feedback volume sliders,
persistent mute, category-selectable preview playback, Reset audio and Camera Shake.
All values use PlayerPrefs. Previews respect master/mute/category values but play
while the round is paused. Reset audio is explicit and does not reset camera choice.
Closing a preview fades it out using unscaled time. Audio diagnostics are logged
when previewing: enabled listener count, listener volume/pause, master/mute,
output sample rate, source clip/playback/gain/mute/pause mode and mixer route.

## Audio diagnosis: evidence and limits

Code/file inspection found:
- ProjectSettings/AudioManager.asset has audio enabled and global volume 1.
- The room scene has an enabled camera listener; gameplay replaces its camera with
  exactly one generated camera/listener. Main Menu retains the scene camera.
- The inspected local preference file had master .678965, with no quake/mute
  override. This was not a saved mute. Other devices may have different preferences.
- Every gameplay sound has a generated clip. Sources use the direct listener route;
  there is no AudioMixer asset or muted mixer group to restore.
- Previous low caps, multiplied category/master levels, bass-only synthesis and
  spatial rolloff were plausible causes of inaudibility. They do not establish
  the actual output-device cause of the reported silence.

Changes increase usable signal without returning to the original loud/clipped noise:
- Rumble has 72/126/213 Hz harmonics and a 650 Hz low pass, on its own child source;
  the filter no longer also attenuates impact sounds on the director object.
- Category caps are .45 ambience, .5 warning, .28 impact and .2 feedback. Generated
  tones retain edge envelopes and .6 peak normalization; crash/feedback rate limits
  remain. Rumble bound at inspected master and default category is about .127 peak,
  versus about .032 previously. These numbers do not establish listening comfort.
- Short effects are initialized with their intended gain instead of missing their
  attack while fading up from zero. Sample edges still provide a smooth attack.
- Warnings use clearer 260/340 Hz tones, partial spatialization, linear rolloff and
  a four-metre near range to retain an audible cue at room distances.
- Gameplay sources use unscaled fades and explicitly managed listener-pause gain;
  previews bypass gameplay pause only. Scene transitions reset listener volume/pause,
  time scale and cursor. Category/mute preferences remain intentional settings.

Code tracing: Play/Retry/Main Menu reset audio pause to false; Pause sets it true;
Resume clears it; modal Back restores the saved state. Results set it true and fade
sources. Restarts construct new sources, clips, gain components and listener.
No actual listening test, device output test, or new Unity play session was performed.
Check Editor Mute Audio, system output device/volume and platform audio initialization
if previews are silent despite nonzero gains and an active listener in diagnostics.

## Camera and compact room

Camera translation is .02 versus .04, pitch/yaw .8 versus 2.5 degrees, roll multiplier
1.5 versus 3, and frequency 10 versus 14. The persistent setting multiplies both
continuous shake and impact kicks; zero yields identity camera shake transforms.
It also removes the cosmetic shelter FOV pulse. Parent camera-pivot mouse look is
unchanged. Physical props, timing, collision damage, movement slowdown and physical
player sway do not depend on the camera setting.

Room remains 10 x 10. Couch width is reduced from 2.2 to 2.0 m and shifted .1 m east,
widening the western approach without narrowing the southern approach. Numerical
capsule-width checks against major static furniture gave .50 m west and .40 m south
remaining clearance beyond the .3 m player radius. These checks exclude actual
fall trajectories, desk entry/standing clearance and dynamic step-up behavior.
Existing runtime capsule checks and cumulative hazard route masks remain.

## Required Unity checks

- Keyboard-only Main Menu -> Help/Settings -> Back; gameplay H -> Back resumes;
  Pause -> Help/Settings -> Back stays paused. Test Esc in all screens, Tab/Shift-Tab,
  slider arrows and submit. Confirm no input leaks into mouse look or movement.
- Preview all categories at zero/low/default/max; toggle mute; reset audio; restart
  and relaunch to verify persistence. Preview from Pause must work without advancing
  earthquake time. Compare diagnostics with actual output.
- Listen on headphones and speakers: onset, warning range, crash/feedback, pause,
  resume, results and restart. Check comfort and warnings against loud ambience.
- Camera setting 0/50/100%: check impact kicks, FOV, responsive mouse look and comfort;
  verify prop trajectories/damage are unaffected by camera preference.
- Walk/crouch both approaches; evade the spawn light and shelf warnings. Repeat after
  actual furniture falls. Verify the remaining approach and escape spaces stay usable,
  including settled-light step-up, before considering any room enlargement.
