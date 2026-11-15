# Homework 5 feedback follow-up

> Historical follow-up for the earlier digital prototype. See [Level1.md](Level1.md) and [SettingsAndAudio.md](SettingsAndAudio.md) for current rules, settings, and warnings.

## Audio implemented

- Rumble is a smooth sum of low-frequency integer harmonics, replacing clipped noise.
  A 220 Hz low-pass filter and a much lower source gain reduce harshness.
- Master and separate earthquake volume sliders appear in Main Menu and Pause.
  Values persist in PlayerPrefs, support standard slider keyboard navigation, show
  percentages, and allow zero volume. Earthquake volume does not reduce warnings.
- Per-source category caps leave mixing headroom. Unscaled source fades smooth starts,
  pause, resume and result transitions. Generated tones have 25 ms edge envelopes
  and a .6 peak ceiling; impact/feedback retriggering is rate-limited.
- Audible warning creaks and pulsing floor markers remain. Master mute also mutes
  warnings, as its label explains. Physical output comfort cannot be inferred from
  sample values or volume percentages; listen on headphones and speakers at typical
  device volumes, including pause/resume and late-round impacts.

## Goal implemented

Main Menu and the pre-earthquake exploration phase display:
"Survive until the earthquake stops. Crouch under the sturdy desk for protection. Watch for falling furniture."
During shaking the objective says "Survive the earthquake" and shows rounded-up
remaining seconds, including while sheltered. Results accurately report shelter use.
No cover deadline, cover-exit loss, or automatic doorway damage has been introduced.

## Challenge implemented

The former invariant that every fall avoided both routes made exposure too safe.
A preplaced entry light now threatens spawn after a fixed, readable warning. Its
fallen fixture is only .25 m tall (controller step offset .3 m). The relocated
bookshelf can physically occupy the south approach. Selection tracks route masks
across all committed obstacles and forbids closing both approaches. Small entry-light
debris is excluded from that obstacle mask because it is designed to be stepped over.
Actual trajectories, rotations and step-up behavior still need physics verification.

Player-aware selection is stronger, with a retained 30% independent environmental
selection chance and 1–2 second intervals after the previous prop settles. Directions
remain authored and frozen before warning; no spawning/aiming at the player occurs.
Near-player selection still requires an unobstructed escape cast within the warning
travel budget. These checks are conservative authoring aids, not a proof of fairness
for every movement input or physical bounce.

## Four requested scenarios: code evaluation and playtest criteria

| Scenario | Code evaluation | Needs Play Mode |
| --- | --- | --- |
| Stand at spawn | Entry light's fall area covers spawn; selection favors it nearby. No timeout death. Warned fixture falls physically. | Observe selection across seeds; ignoring warning should risk an identifiable light impact; stepping aside must work. Confirm idle-controller collision delivery. |
| Run straight to desk | Existing couch blocks a direct diagonal. Player must route around it and crouch; reaching the desk before onset is intentionally safe. | Confirm couch detour is clear, briefing readable, and desk protection reliable. Do not manufacture a death for correctly taking cover. |
| Take alternate route | Both approaches begin clear. Bookcase envelope can close the south approach while west stays reserved. | Verify actual shelf trajectory matches warning and west escape remains accessible after settlement; check spawn fixture can be stepped over. |
| Leave shelter during shaking | No automatic loss. Exposure and warning selection resume at the new position; only dangerous moving-prop contact can kill. | Check readable nearby warnings, impact explanations, escape space and encounter frequency without repeatedly walking into hazards deliberately. |

Run each scenario with seed 1701, then several other seeds and repeat the same inputs.
Record warning source/time, selected prop, response, physical cause of any injury,
and whether an escape remained available. Commitments are logged in the Console.
Pause/resume during warning and impact; test volume at 0/low/default/max and both
restart paths. A seed reproduces random choices given the same input/timing, not
bit-exact Unity physics.

## Checks performed

- Generated rumble numerical peak: .4289; conservative default output peak bound
  .03251 before device amplification. No generated rumble clipping.
- Expanded bookshelf envelope route mask: 2 (south only).
- Warning travel budget at minimum quake crouch speed: 3.24 m versus .95 m expanded
  spawn-fixture half-width. Escape casts still check actual surrounding colliders.
- Player and Editor C# compilation and whitespace checks are recorded in the task
  report. No new Unity launch/listening test was performed in this follow-up.
