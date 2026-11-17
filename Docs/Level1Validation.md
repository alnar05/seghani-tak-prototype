# Historical Level 1 validation — 15 November 2026

These runs cover the earlier single-room implementation, including its previous warning presentation and menu copy. Current draft gameplay and human playtesting are tracked separately in [Game_Status_and_Final_Plans.md](Game_Status_and_Final_Plans.md) and [Playtest_Notes.md](Playtest_Notes.md).

Unity 2023.1.22f1 runs used a disposable `/tmp/seghani-level1-check` project. Test scripts
remain outside production Assets in Tools. No test driver ships in gameplay.
[Selected output](Level1ValidationResults.txt) records the successful checks separately
from visual inspection. These checks were executed in stages, not one uninterrupted run.

## Code and native physics

- Compiled the Player/Editor scripts in Unity; `git diff --check` passed.
- Reproduced both old passage lights resting on the corresponding upper bookshelf
  collider (bottom ≈1.9 m above floor). Inspected compound bounds, hierarchy and joints.
- Native Physics.Simulate and CharacterController.Move checks for seeds 1701, 7 and 42:
  initial routes, desk/wall setback, crouched protection, actual fallen-bookcase
  obstruction and the remaining corridor after **both** its fixture and the approach
  fixture settle all passed.
- Both bookcases passed independent grounded-assembly tests: no retained joints,
  no initial penetration >2 mm, bounded motion, grounded settlement and intended
  route occupancy.
- Released each corridor fixture with its bookshelf upright and with it fallen.
  In all four combinations, the only support collider was Floor. Both corridor
  fixtures and the approach fixture settled at center y≈.08, top y≈.16, within the
  .3 m step offset. No final-position adjustment was applied.

## Automated Unity Play Mode

- Seeds 1701, 42 and 7: no warning before quake onset; seeded opening warning,
  physical release and grounded shelf settlement; protected survival through the
  full earthquake; Retry reset the round clock.
- Settings contains exactly two sliders and one Back button. The actual slider
  callbacks updated stored Master/Shake values at zero/max and restored them.
  Pause -> Settings -> Back preserved pause, frozen round time and listener pause.
  Pause hid the gameplay HUD. Main/Pause navigation links and selected colors passed.
- Camera zero restored identity child position/rotation and base FOV while retaining
  the parent look orientation. Maximum produced translation and rotation across a
  ten-frame sample, and the FOV response. Master zero immediately zeroed all source
  gains. This establishes state/gain behavior, not a listening test.
- Eight scripted route runs: seeds 1701 and 42 × south/north initial strategy ×
  blind/attentive behavior. All **four blind runs took physical hits**. All **four
  attentive runs reached the desk alive** by selecting the other opening and
  waiting outside warned footprints. Final Main Menu transition removed the round.
- Route runs used normal timeline speed, native physics and CharacterController.Move.
  Hardware input was replaced by a scripted driver with an explicit crouch state;
  a test-only relay forwarded actual controller collisions through FallingProp's
  normal contact method. Protected observation rounds used accelerated time.

## Rendered inspection

Inspected actual Unity renders, separately from the checks above:

| Capture | Observation |
| --- | --- |
| [Main Menu, 1920×1080](Validation/main-1920.png) | Same room/desk/fixtures behind dark overlay; readable goal and buttons. |
| [How to Play, 1280×720](Validation/help-1280.png) | Four concise cards; controls, shelter illustration and warning sign fit. |
| [Settings, 1024×768](Validation/settings-1024.png) | Two sliders and Back; labels and controls fit a 4:3 viewport. |
| [Pause, 1366×768](Validation/pause-42.png) | Five clear actions; no HUD, vignette or square confetti overlap. |
| [South bookshelf](Validation/bookshelf-rest-south.png), [north bookshelf](Validation/bookshelf-rest-north.png) | Coherent tipped assemblies occupy their intended corridors. |
| [Installed light](Validation/fixture-mounted-north.png) | Visible housing and warm diffuser identify its purpose. |
| [South fixture](Validation/fixture-floor-south.png), [north fixture](Validation/fixture-floor-north.png) | Flat physical housings rest on the floor, clear of the bookshelves. |
| [Approach clearance](Validation/approach-clearance-north.png) | Space between settled housing and the desk permits stepping down before crouched entry. |
| [Gameplay warning](Validation/warning-1701-0-False.png) | Floor footprint and concise hazard instruction are visible from the corridor. |

Geometry captures use an observer camera; no prop was repositioned for the image.
They are native-physics endpoints, not a continuous animation recording. The gameplay
warning frame was captured during a scripted Play Mode run.

## Corrections made during validation and limits

Earlier harness attempts incorrectly accepted release-frame zero velocity as settlement,
left physics in manual mode, or fought scripted crouch with hardware-input processing.
The settlement property and harness were corrected. One camera-angle assertion rounded
small rotations to zero; it was replaced with component measurements across ten frames.
An early approach placement kept a controller elevated against the desk; moving the
fixture back created a real .8 m step-down gap, followed by native traversal checks.
One graphical Editor launch crashed in Mono initialization; subsequent headless Play
Mode checks and standalone graphical captures completed successfully.

Two earlier route-driver attempts stopped at the desk entrance; the final isolated
route run completed all eight cases. Keep normal-speed human repetition in the manual
checklist: scripted drivers do not establish keyboard/mouse feel or subjective fairness.
No hands-on human gameplay, full animation review, hardware keyboard-only navigation,
or listening test is claimed. Human tests should repeat both routes/seeds, stop and
retreat during warnings, crouch into shelter after debris settles, compare shake 0/100,
and exercise Esc/Tab/arrow keys and restart from Pause/results.

## Reproduce

Copy Assets, Packages and ProjectSettings into `/tmp/seghani-level1-check`; never run
these destructive scene-building harnesses in the working project. Copy:

- Tools/BookshelfRepairPhysics.cs -> Assets/Editor/RepairCheck.cs
- Tools/Level1PhysicsChecks.cs -> Assets/Editor/Verify.cs
- Tools/FixtureSupportPhysics.cs -> Assets/Editor/FixtureSupportCheck.cs
- Tools/Level1ChallengePlayMode.cs -> Assets/ChallengeRunner.cs
- Tools/Level1ValidationLauncher.cs -> Assets/Editor/Level1ValidationLauncher.cs
- Tools/Level1RenderChecks.cs -> Assets/Editor/Level1RenderChecks.cs

Run Unity with `-batchmode -nographics -projectPath /tmp/seghani-level1-check
-executeMethod Level1ValidationLauncher.Run` (without `-quit`; the runner exits itself).
For graphical endpoint captures, run with graphics enabled and
`-executeMethod Level1RenderChecks.Run -quit`. Captures write to `/tmp/seghani-captures`.
