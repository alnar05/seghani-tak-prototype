# Current bookshelf sequence repair

See [BookshelfRepair.md](BookshelfRepair.md) for the current timing/physics diagnosis,
fixes, executed tests and remaining visual checks. That focused pass supersedes the
orientation-period warning and jointed bookcase details in the historical iteration below.

## Historical observed-gameplay iteration

The two-room/two-passage footprint, shared menus, Settings and saved preferences,
audio, camera settings, pause, results and scene reload flow are retained.

## Why the previous version was easy

A direct run took roughly three seconds, but the opening fall occurred around six
seconds. The short branch always received the bookshelf, the other branch was then
reserved, and peripheral events did not challenge it. The .4 m fallen shelf was close
to the .3 m controller step offset and jumping could bypass it. These were structural
bypasses; louder audio or stronger shake would not fix them.

## Focused changes

- The large desk sign is removed. The wood desk and restrained existing light identify
  cover, with the concise HUD goal and expandable instructions supplying guidance.
- Desk parts are authored in desk-local space. The tabletop is 2 x 1.2 m, with its back
  .3 m from the actual generated east-wall bounds. At the default wall x=6 the desk
  center is x=5.1 and its tabletop/legs end at x=5.7. If the wall is at x=5, placement
  derives x=4.1 instead. Routes update to the generated desk position.
- The shelter trigger is the leg-to-leg interior: 1.84 x 1 x 1.04 m in desk-local axes.
  Protection requires the entire crouched controller bounds to fit inside it, rather
  than testing only the player's origin. Geometry checks examine world-space wall
  penetration and crouch access during construction.
- Level 1 disables jumping through `allowJump=false`; other uses of the controller
  default to allowing it. Gameplay hints follow the level/player setting. UI Space
  submit remains available. Level 1 walking/crouching speeds are 2.2/1.5 m/s.
- Two bookcases are preplaced. A seed chooses the opening south or north bookcase
  before any warning. Its 2.75-second warning begins during the 2.5-second orientation
  phase; physical release occurs only while the earthquake is active. Warning positions
  and impulses are fixed before cue playback. The former six-second uncontested lead
  is removed, rather than adding damage for being outside cover.
- Each bookcase has a visible .45 m solid base and a hinged 1.55 m upper section,
  .7 m deep. At rest the upper section occupies about y=.45..1.15 and reaches across
  its passage. The base covers the rear gap. Both colliders match visible furniture;
  no invisible route wall is added. Neither part can be stepped over at .3 m, and
  the under-gap is too low for a .9 m crouched player.
- The opposite bookcase remains upright. After the opening event settles, the other
  passage can receive a separately warned falling light. Its flat .25 m settled
  fixture is designed to be stepped over without jumping. Events remain serialized,
  with escape checks, a one-second settlement hold and a 1–2 second interval.
  Subsequent events retain environmental draws and player-aware selection. No warning
  retargets; no automatic cover deadline, cover-exit death or proximity damage exists.

Same seed plus the same inputs/timing reproduces scheduler choices; physics is not
bit-exact. Repeating a fixed seed intentionally repeats its opening route. Test across
seeds to evaluate fixed-passage habits. The north passage is no longer always reserved.

## Checks actually performed

- Player and Editor C# compilation against the installed Unity assemblies; whitespace.
- Independent numerical initial-route, desk setback and expected resting-pose checks.
- **Headless Unity 2023.1.22f1 native physics/controller checks** in a disposable copy:
  both initial routes navigated without jumping; desk/trigger cleared the actual wall;
  a crouched capsule at the desk was safe. For seeds 1701/7 (south) and 42 (north),
  the real hinge settled near 90 degrees, the fallen bookcase obstructed attempted
  no-jump traversal, and the other route remained navigable. That route was also
  navigated after its caution light physically fell and settled. Three fresh level
  object builds passed, establishing geometry reset for that harness.
- Test source: [Level1PhysicsChecks.cs](../Tools/Level1PhysicsChecks.cs).
  Output summary: [Level1PhysicsResults.txt](Level1PhysicsResults.txt).
- Code tracing confirms actual restarts still reload the room scene and recreate
  props, joints, scheduler/RNG, controller and shelter state. Actual menu-driven restart
  repetition was not exercised by the headless object-rebuild harness.

## What is not verified

No input-driven Unity gameplay or listening test was performed. Warning visibility,
comfort, perceived walking pace, deaths from moving-prop contact, direct sprint/hold-W
outcomes, and difficulty across repeated passage choices remain playtest questions.
Do not treat successful geometry tests as verified challenge or fairness.

The timing analysis places an immediate south walk inside the gate near the 2.75 s
release, rather than beyond it; the longer north path arrives later. The seed can
threaten either branch. Those are code/timing observations, not measured human outcomes.
Menus and Settings were preserved, but need their normal interactive regression test.

## Short playtest checklist

1. **Desk and jumping:** approach from both entrances, crouch in/out, release crouch
   under the tabletop, and test the edges of the shelter indicator. Press Space while
   moving; no Level 1 jump should occur. Look for any tabletop/leg/wall clipping.
2. **Direct movement and fixed choices:** use seeds 1701, 7 and 42. Hold forward toward
   the desk at the fastest allowed pace; then repeat always taking south, then north.
   Record warning time, whether the gate can be crossed uninterrupted, any physical
   impact, and whether stopping/retreating/choosing the other branch works. If a fixed
   choice reliably bypasses interactions across seeds, pacing still needs tuning.
3. **Hesitation and final layout:** stop at a warning, retreat from either side, take
   the other passage and time its separate light warning. Test actual walk/crouch
   obstruction around and under the fallen bookcase. The remaining passage and desk
   must stay accessible without jumping; waiting outside or leaving cover alone is legal.
4. **Repeatability/regression:** retry through Pause, results and R, then Main Menu/Play.
   Verify both bookcases reset upright, lights reset, seed selection repeats correctly,
   and instructions, Settings, mute/preview, saved preferences and pause restoration work.
