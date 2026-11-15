# Seghani Tak — draft status and final-project plans

Seghani Tak is a first-person indoor earthquake game. Each attempt starts with orientation. During shaking, the player reads physical hazards, chooses a route, and tries to be alive and fully crouched in shelter when the active countdown reaches zero. Level 1’s active quake lasts 15 seconds. Later rooms use different durations and cover layouts.

## Main features

- Preplaced props warn through local movement and sound during shaking. Dangerous collisions can end the attempt. Fallen objects can affect navigation.
- The shelter check drives protection, feedback, and the countdown result. The player can leave and return to shelter while the round continues.
- How to Play, a compact objective and countdown, and cause-specific results communicate what happened. The settings expose Master Volume and Camera Shake, and retain the selected values across sessions.
- Play starts the campaign in the Living Room. Level Select opens any room. Retry Level resets the current room and Next Level advances after victory when a room remains.
- The Two-Floor House has an optional downstairs shortcut. Players can take the brass key from the entry wall during orientation and open the shortcut with E. Checking the jammed upstairs door points players to alternate passages.

## Rooms and pacing

| Room | Orientation | Shaking | Layout and shelter |
| --- | ---: | ---: | --- |
| Living Room | 3 s | 15 s | Twin passages lead to the study desk. |
| Home Office | 4 s | 18 s | A short shelving aisle and longer loop lead to the office desk. |
| Kitchen & Dining | 3 s | 20 s | A wide opening and side passage lead to the dining table. |
| Upstairs Study | 12 s | 22 s | Upstairs study desk near the bedroom, with a downstairs sitting-room table for players exploring that floor. |
| Two-Floor House | 14 s | 26 s | Dining table downstairs and desk upstairs. Optional locked shortcut and a jammed upstairs door with side approaches. |

The two-floor rooms place cover on both floors so the player has a shelter option nearby after orientation. The countdown and route challenge are gameplay abstractions. [CDC earthquake guidance](https://www.cdc.gov/earthquakes/safety/stay-safe-during-an-earthquake.html)

## Local build checks

Unity built the macOS and Windows x64 versions. The local macOS check opened all five rooms, paused and resumed the Living Room, displayed the green in-cover state, and reached both a full-cover victory and an outside-cover countdown defeat. The How to Play cards fit in the captured view.

## Media

The media kit includes eleven captures from the macOS build, with gameplay images from all five rooms. It also shows the main menu, level selector, help, and victory and defeat states.

`gameplay-sampled-preview.mp4` is a silent, roughly 25-second preview made from 27 frames of a real Living Room retry. Frames retain their original capture timing at approximately one frame per second. The attempt ends with an outside-cover timeout. A smooth gameplay recording with audio is pending.

## Checks still needed

- Record a current draft outside-class playtest and a second round. Look for recoverable route decisions, reliable cover recognition, and a result players can explain.
- Observe how first-time players position themselves under the Living Room desk. Full cover requires a narrow placement along the desk’s depth. Use their experience to tune the physical clearance or improve how the interface communicates when the player is fully under cover.
- Compare how often new and returning players reach shelter during orientation, particularly in the two-floor rooms where orientation lasts longer. Adjust the routes or timing based on the observed attempts.
- Test both keyboard and mouse through menus, pauses, results, and repeated retries. Compare audio at zero, low, and typical settings on speakers and headphones. Check that zero shake retains mouse look.
- Run the packaged executable on another computer. Record its operating system and build, whether the game launches, and any issues loading or completing each room.
- Capture a continuous gameplay recording with sound for the final media kit.

## Plans for the final project

- Adjust warning lead time, shelter access, and route difficulty using the two playtest rounds. Start with the rooms where testers miss a cue or feel forced onto one route.
- Refine the silhouettes, materials, and sound cues that distinguish sturdy cover from other furniture. Recheck clearances after replacing any models.
- Refine progression across the levels. Evaluate lightweight debris clearing and a separate post-earthquake exit task after tuning the shelter loop.
- Verify the final packaged build on its intended platforms and include the matching source and media.
