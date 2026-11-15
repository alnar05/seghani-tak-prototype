# Bookshelf sequence and physics repair

> Historical bookshelf-only pass. The current challenge, fixture and menu changes are documented in [Level1.md](Level1.md) and [Level1Validation.md](Level1Validation.md).

This pass changes only event timing, the bookcase assembly and supporting checks.
Layout, walking speeds, jump configuration, reaction durations, menus, Settings,
audio gains, physical survival rules and camera settings remain unchanged. No new
hazards or difficulty features were added.

## Reproduction and diagnosed causes

Before changing either assembly, an isolated Unity physics run compared both variants:
- World pivots and connected anchors matched, with correctly mirrored axis and impulse.
  No initial penetration greater than 1 mm was detected.
- Both hinges stopped at 90 degrees with the upper section's bottom about .45 m above
  the floor. The world anchor and hard limit held the upper section in an unsupported
  pose. The static base stayed separate from the moving upper section.
- The baseline did **not** reproduce a right-only explosion. North/south angular and
  linear behavior was approximately mirrored in isolation. A visual reproduction of
  that specific reported artifact remains necessary; it was not diagnosed as a sign error.
- The previous clearance test accepted the joint-held pose as settled. That did not
  establish a plausible, grounded fall.

The premature cue was explicit: `HazardScheduler.Start()` called `Warn()` during calm.
The prop coroutine advanced its own elapsed time, the scheduler used `Time.time` plus
an accumulated settle timer, and the director independently tracked round elapsed time.
This could legitimately warn before the earthquake; it was not an audio/settings issue.

## Focused fixes

`EarthquakeDirector.RoundTime` is the authoritative round clock. The director initiates
countdown, rumble playback and visual shaking at quake onset, then ticks the scheduler
on that same clock. It runs before camera LateUpdate. The existing physical sway and
slowdown curve is unchanged; only a small sensory onset envelope starts the already
configured audio/visual effect immediately. Sources explicitly disable play-on-awake.

Scheduler Start only initializes seeded selection. During calm it cannot commit any
hazard. Opening warning is eligible .75 s after onset. The same clock drives its
2.75 s reaction interval, release, subsequent settlement hold, and event spacing.
Normal 3 s warning durations remain unchanged. No coroutine, `Time.time` deadline,
or second elapsed accumulator controls a prop's event sequence. At default settings:
calm 0..2.5 s; onset 2.5 s; earliest warning 3.25 s; earliest release 6.0 s (frame-quantized).
Pause freezes the round clock, cue animation and native physics. Results cancel cues;
scene reload constructs fresh timers, seeded selection and bodies.

Both bookcases now use one unscaled rigidbody and two contiguous compound box colliders
matching their visible lower/upper sections. The lower section moves with the rest.
There is no hinge, static pedestal, position snapping, final-pose teleport, or added
route blocker. A modest, committed impulse tips the body from its placed position.
It settles on the actual floor and passage divider rather than a joint limit. Collision
bounds aggregate both colliders, so route checks no longer see only the lower section.

During warning, the visual assembly has a small eased rocking motion and the existing
localized creak/floor cue. The physical body remains kinematic until release. Rocking
returns smoothly to neutral before physical motion. Warning marker colliders are
immediately disabled; cues do not introduce transient physical obstacles.

## Executed checks

[Recorded output](BookshelfRepairResults.txt) distinguishes the baseline, isolated
repair and automated Play Mode runs.

- Player/Editor C# compilation and whitespace checks.
- Independent native `Physics.Simulate` tests for both variants: zero joints, no initial
  overlap >2 mm, grounded settlement and bounded speed/frame displacement. Maximum
  measured speed was about 1.824 m/s and maximum displacement .0365 m per .02 s step.
  Both settled leaning naturally against the divider at about 57 degrees, with floor
  contact within .2 mm. They were not forced into a horizontal pose.
- Existing native controller tests for seeds 1701, 7 and 42: initial routes and desk
  access, selected-route obstruction without jumping, alternate access after its light,
  and fresh object rebuilds.
- **Automated Unity Play Mode**, seeds 1701 and 42: actual full rounds through the scene
  and normal lifecycle callbacks. All calm-period props remained uncommitted/kinematic;
  no warning occurred early. At onset the countdown, nonzero shake request and playing
  rumble source were present. Warning, release, actual collision callback and grounded
  settlement all occurred. Pause/resume froze clock, rigidbody pose and warning model
  during warning, moving fall, impact and settlement. Sheltered rounds reached survival.
  Actual Retry and Main Menu -> Play reset round time, prop commitment and physics state.
  Maximum measured floor/wall penetration was .00093 m, within contact-solver tolerance.
- Play Mode tests ran accelerated (8x), with a scripted stationary sheltered player.
  They establish sequence/reset/physics behavior, not human reaction timing or perceived
  realism. The first combined test launch failed because its test object was included
  in an Editor scene backup and duplicated on reload; the isolated runtime bootstrap
  fixed the harness. No production menu change was needed.
- Final explicit play-on-awake and marker-collider guards received compilation/static
  checks after those Play Mode runs; they do not alter release timing or body dynamics.

Test sources are outside Assets under Tools. Copy them only into the disposable
`/tmp/seghani-level1-check` project using the filenames in their headers. They contain
path guards and are not game features.

## Remaining manual visual/audio checks

1. Run each seed at normal speed. Calm should be still/quiet; onset should start the
   survival display, rumble and environmental shake together, followed later by a
   readable rocking/creak warning.
2. Watch each bookcase from both ends of its passage. Verify motion starts from its
   placed pose, remains coherent, contacts floor/divider naturally and rests without
   visible penetration, hovering, snapping, jitter or implausible bounce.
3. Pause/resume while warning, falling, impacting and resting. Confirm the creak and
   impact sound fade/resume comfortably and the visible motion does not jump.
4. Retry, then Main Menu -> Play repeatedly. Check initial pose, warning phase and
   seed selection reset. Confirm remaining route and crouched shelter stay usable.

No rendered visual gameplay verification or listening test was performed in this pass.
Do not describe the mechanic's appearance or feel as verified from these tests.
