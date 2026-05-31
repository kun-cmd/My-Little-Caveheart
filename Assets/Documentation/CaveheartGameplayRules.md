# Caveheart Gameplay Rules

This document is the current source of truth for gameplay values. When scripts change any value below, update this file in the same change.

## Core Experience

The demo should make the player move from controlling the little caveheart to reading why he cannot get up yet.

The game should explain less through text and more through consequences:

- Urging wakes him quickly, but makes the room colder and his body more defensive.
- Observing costs a little morning time, but lets the player notice signals before acting.
- Gentle help works only when his body is ready for it.
- The room mirrors his body: pressure makes it colder and shakier; trust makes it warmer and steadier.

## Hidden Values

All three values are hidden from the player as numbers. The HUD may show bars in whitebox builds.

| Value | Range | Start | Meaning |
|---|---:|---:|---|
| awake | 0-8 | 0 | How physically awake he is |
| trust | 0-8 | 1 | Whether he is willing to cooperate |
| stress | 0-8 | 1 | How much his body is bracing |

## Time

Morning time limit: 14 minutes.

Actions consume game minutes only when the player uses a bottom action button. Real seconds do not directly consume morning time.

| Action | Time Cost |
|---|---:|
| Urge / Alarm | 1m |
| Touch | 2m |
| Water | 2m |
| Window | 1m |
| Observe | 1m |

Retired or inactive actions still have code values:

| Retired Action | Time Cost | Current Status |
|---|---:|---|
| Shake Bed | 1m | Removed from foreground UI and runtime scene props |
| Blanket | 2m | Removed from foreground UI and runtime scene props |
| Scratch | 1m | Kept in code, scene object inactive |

## Foreground Actions

Only these actions should be shown on the bottom action bar.

### Urge

Code interaction: `Alarm`

| Change | Value |
|---|---:|
| awake | +2 |
| stress | +3 |
| trust | -1 |
| time | 1m |
| forcefulUseCount | +1 |

Design note: short-term progress, long-term relationship damage. Three or more forceful uses force the tense/bad outcome.

### Touch

Code interaction: `GentleTouch`

If `stress >= 6`:

| Condition | Result |
|---|---|
| Previous action was Observe and `waitStreak > 0` | accepted, stress -2, trust +1 |
| Otherwise | rejected, stress +1, trust -1 |

If `stress < 6`:

| Condition | Result |
|---|---|
| `gentleTouchUses >= 2` | rejected, stress +1, trust -1 |
| Otherwise | accepted, trust +1, stress -1 |

Time cost: 2m.

### Water

Code interaction: `OfferWater`

| Condition | Result |
|---|---|
| `waterUses >= 3` | rejected, stress +1 |
| `trust >= 4` | accepted, awake +2, trust +1, stress -1 |
| `trust >= 3`, `stress <= 4`, previous action was Observe | accepted, awake +2, trust +1, stress -1 |
| `stress <= 3` | rejected but respectful, trust +1 |
| Otherwise | rejected, stress +1 |

Time cost: 2m.

### Window

Code interaction: `OpenCurtain`

| Condition | Result |
|---|---|
| `curtainUses >= 1` | rejected, stress +1 |
| `trust >= 3`, `stress <= 4`, and state is Settled or previous action was Observe | accepted, awake +2, trust +1, stress -1 |
| Otherwise | too bright, awake +1 net, stress +2, trust -1 |

Time cost: 1m.

### Observe

Code interaction: `Wait`

| Condition | Result |
|---|---|
| `stress >= 7` | stress -2; if previous action was forceful/rejected and this is first Observe streak, trust +1 |
| `stress >= 4` | stress -2; if first Observe streak, trust +1 |
| Previous action was rejected and this is first Observe streak | stress -1, trust +1 |
| Otherwise | stress -1 |

Time cost: 1m.

Design note: Observe is not a free infinite repair. It consumes time and repeated Observe does not keep adding trust.

## Retired / Inactive Action Rules

These remain documented because the code still contains them for testing or future experiments.

### Shake Bed

Current status: removed from foreground UI and runtime scene props.

| Change | Value |
|---|---:|
| awake | +3 |
| stress | +4 |
| trust | -2 |
| time | 1m |
| forcefulUseCount | +1 |

### Blanket

Current status: removed from foreground UI and runtime scene props.

| Condition | Result |
|---|---|
| `blanketUses >= 2`, `stress >= 7`, previous action was Observe | rejected but calming, stress -1 |
| `blanketUses >= 2`, otherwise | rejected, stress +1, trust -1 |
| `blanketUses == 0` | accepted, stress -2, trust +1 |
| `blanketUses == 1` | accepted, stress -1 |

Time cost: 2m.

### Scratch

Current status: kept in code, scene object inactive.

| Condition | Result |
|---|---|
| `stress >= 7` | rejected, awake +1, stress +1, trust -1 |
| `scratchUses >= 2` | rejected, awake +1, stress +1, trust -1 |
| `awake >= 4` or state is Settled | accepted, awake +1, trust +1, stress -1 |
| Otherwise | rejected, awake +1, stress +1, trust -1 |

Time cost: 1m.

## State Rules

State is evaluated after each action.

| State | Condition |
|---|---|
| SittingUp | `awake >= 6`, `trust >= 4`, `stress <= 4` |
| Resisting | `stress >= 7` |
| Settled | `trust >= 3`, `stress <= 3` |
| Startled | `stress >= 4` or `awake > 0` |
| Sleeping | fallback state |

## Outcome Rules

Current implementation shows the outcome as soon as he reaches `SittingUp`, or when time runs out.

### Bad / Tense Outcome

Any of these:

- Time runs out before he sits up.
- `forcefulUseCount >= 3`.
- `trust <= 4` at sit-up.
- `stress >= 4` at sit-up.

### Good Outcome

All of these:

- He reaches `SittingUp`.
- Time has not run out.
- `trust >= 4`.
- `stress <= 4`.
- Does not qualify for Very Good.

### Very Good Outcome

All of these:

- He reaches `SittingUp`.
- Time has not run out.
- `forcefulUseCount <= 1`.
- `trust >= 5`.
- `stress <= 3`.
- `usedMorningMinutes <= 12`.

## Environment Feedback

Environment should show consequences rather than explain them.

| Signal | Rule |
|---|---|
| Cold overlay | Increases with stress and time pressure |
| Camera shake | Increases when stress is above low/mid range; stops after SittingUp |
| Door shadow | Increases with time pressure and stress |
| Warm light | Increases with trust and Settled/SittingUp states |
| Red pulse | Only appears at extreme pressure: `stress >= 7` |

## Current UI Rules

- Bottom action bar is the only input source for gameplay actions.
- Scene props are visual signals only and must not respond to direct mouse clicks.
- Hovering a bottom action button may flash the matching scene prop.
- HUD should show bars, not numeric stat values. Numeric values may stay available in the Inspector and Console for whitebox debugging.
