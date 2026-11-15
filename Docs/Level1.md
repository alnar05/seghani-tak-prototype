# Level 1 challenge and menus

The room has two corridors leading to a protective desk. Survive until the earthquake
stops; shelter is protective but never mandatory on a timer. Damage comes from moving
props contacting the player. Settled debris and doorways do not inflict proximity damage.

## Geometry diagnosis

Native Unity reproduction of the old layout released each passage light while its
bookcase stood upright. Both lights stopped at center y≈2.03, bottom y≈1.90. The
support query identified the corresponding bookcase's **upper BoxCollider**: center
(1,1.125,±2.325), size (1.7,1.55,.65). The light and upper collider overlapped in
horizontal footprint. There was no joint, hidden shelf or warning-marker collider.
The existing bookshelf is one unscaled rigidbody with two contiguous colliders;
its lower section moves with it. Those colliders represent the visible cabinet.

The small repeated squares in the model are `Book_Front` render meshes with their
colliders removed, representing book spines. No runtime debug renderer or gizmo
code exists in Assets. Editor selection/gizmo overlays are not player-build geometry.
The deliberate pulsing floor warning is gameplay information and has no collider.
The other square visual source was `SafeBurst`: green billboard confetti emitted on
entering shelter. Those particles had no colliders; that distracting effect is removed.
HUD and shelter vignette now hide behind Pause, Help, Settings and results.

The passage lights now hang beyond the exit lintels at x=3.05, away from both
bookcase swept envelopes. Each has a metal housing and warm inset diffuser. The
single .16 m housing collider matches its visible body. A committed, vertical
gravity fall ends on the real floor, below the .3 m controller step offset. The
new approach light ends at x=3.7, leaving .8 m to the desk front so the controller
can step down before entering the low shelter. No joint,
position correction, final-pose teleport or invisible route wall is added.

A released prop must actually move before it reports settlement: an AddForce waiting
for the first physics step previously looked like a settled zero-velocity body.
Controller impact handling also preserves pre-step incoming velocity, as rigidbody
collision handling already did, so collision resolution cannot erase a real strike.

## Encounter sequence

The former scheduler serialized the opening shelf, settlement and the other
corridor's light. Players could reach shelter before the second warning. The former
2.5 s calm period also let an immediate run cover much of the level before onset.

- Default calm lasts .8 s. All warnings remain after earthquake onset. A seed chooses
  the initial threatened bookcase; its 2.75 s warning becomes eligible .15 s after
  onset. The opposite bookcase stays upright.
- Each corridor exit has a preplaced ceiling fixture. Approaching x=-1 on that side
  starts its own configurable warning. The player can stop outside the pulsing area,
  retreat, or judge the opening. Waiting for the bookshelf to settle no longer delays
  the entire encounter sequence.
- Approaching x=.15 makes the final light eligible. Its fall footprint is before the
  desk; settled housing can be stepped over, and the desk remains protective.
- Existing picture, window, TV, entry light and lamp events remain as sparse later
  environmental draws. They use the same commitment and safety checks.

Warning commitment freezes position, push, torque and release deadline. Events never
follow or retarget the player. Seed affects route choice, rather than promising
bit-identical PhysX results. Inspector values on `Level1Configuration` control the
onset, warning lengths, approach thresholds, reserve and ambient intervals.

Before commitment, a capsule-based floor search checks the player's current location,
actual compound debris geometry, and other committed warning/fall footprints. It
requires a reachable refuge within a crouched response budget for temporary fixtures,
and a complete remaining path to shelter before adding a permanent route obstruction.
An unsafe event is deferred. This is conservative scheduling, not collision geometry.
Warnings use a floor pulse, local creak, visual rocking and a concise HUD message.

Movement retains the configured 2.2/1.5 m/s speeds during shaking. Earthquake sway is
cosmetic camera motion; it no longer displaces the controller. Difficulty should come
from observing hazards, not input slowdown, audio levels or camera strength.

## Menus and settings

See [SettingsAndAudio.md](SettingsAndAudio.md). The menu backdrop renders this same
room, including the desk and installed fixtures, under a dark overlay. How to Play
has Goal, Movement, Take Cover and Watch for Hazards cards, with controls and a small
desk/warning illustration. Pause offers Resume, How to play, Settings, Restart Level
and Main Menu. Panels share typography, spacing, button states and vertical/Tab
navigation. Modal Back/Esc restores its caller's clock, audio, cursor and selection.

## Validation

See [Level1Validation.md](Level1Validation.md) for the executed checks, evidence and
remaining limits. Historical bookshelf-only work is in [BookshelfRepair.md](BookshelfRepair.md).
