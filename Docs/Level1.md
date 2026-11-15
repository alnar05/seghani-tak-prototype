# Level 1 — Living Room

The starting room connects to the study by two passages. Use the **3-second orientation** to look for a route to the sturdy desk. Once the earthquake starts, the active countdown lasts **15 seconds**. Jumping is disabled.

## How an attempt ends

- **Victory** requires staying alive and being fully crouched in the desk’s shelter at zero seconds.
- **Impact defeat** occurs when a dangerous physical impact ends the attempt during shaking. The result gives the cause.
- **Countdown defeat** occurs if a living player is outside full crouched cover at zero. The result explains the shelter requirement.

Entering cover changes protection and feedback while the active countdown continues. Leaving cover resumes normal exposure and allows the player to return before zero. The same shelter check supplies protection, the “In cover” feedback, and the final result. Future hazard releases are cancelled when the attempt ends.

## What to watch for

Look at the preplaced shelves, lights, and furniture as the earthquake begins. Notice each warned prop’s movement and local sound, then decide whether to continue, wait, or switch passages. A moving prop can deal damage on contact. Its settled geometry can affect traversal for the rest of the attempt. Fall direction is set when an event commits and its warning starts after quake onset.

The earlier light snag was reproduced on the upper bookshelf collider. That repair moved fixtures away from shelf sweep and used matching colliders for the fixture housings. The bookshelf was also changed to a single compound body that settles against the floor and surrounding geometry. The [earlier route illustration](Level1Layout.svg), [validation notes](Level1Validation.md), and [bookshelf repair record](BookshelfRepair.md) document that prototype pass.

## Current manual checks

1. Use both passages on separate attempts. Verify that a cautious player can notice the cue, get clear, and reach the desk.
2. Check the shelter boundary. Stand nearby, crouch outside, crouch partly under the desk, and crouch fully inside. Check the feedback and end an attempt in both full and partial cover.
3. Enter, leave, and re-enter during shaking. Check that the countdown keeps running until the deadline or a dangerous impact.
4. Observe a full shelf fall and fixtures settling on the floor. Check for hovering, clipping, unexpected obstructions, and post-result hazard motion.
5. Retry from a result and from Pause. Confirm the room and timer reset.
