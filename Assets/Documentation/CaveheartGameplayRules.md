# Caveheart Gameplay Rules

This document is the current source of truth for gameplay values. When scripts change any value below, update this file in the same change.

## Core Experience

The demo should make the player move from controlling the little caveheart to reading why he cannot get up yet.

The game should explain less through text and more through consequences:

- Urging wakes him quickly, but makes the room colder and his body more defensive.
- Observing costs a little morning time, but lets the player notice signals before acting.
- Gentle help works only when his body is ready for it.
- Window mainly wakes the body; it should not be a reliable trust-builder that automatically unlocks water.
- Water responds to bodily cues and stress, not only to a trust threshold.
- Scratch is a middle-awake playful push, not an early soothing tool.
- Current testing uses one stable neutral morning. Random morning variants are removed from the active loop.
- Observe should describe the body state, not mark a next action as buffed, debuffed, or forbidden.
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

When time runs out, the morning closes as "today stops here." This is not presented as a fail state or punishment; it is feedback for the next attempt.

| Action | Time Cost |
|---|---:|
| Urge / Alarm | 1m |
| Touch | 2m |
| Water | 2m |
| Window | 1m |
| Scratch | 1m |
| Observe | 1m |

Retired or inactive actions still have code values:

| Retired Action | Time Cost | Current Status |
|---|---:|---|
| Shake Bed | 1m | Removed from foreground UI and runtime scene props |
| Blanket | 2m | Removed from foreground UI and runtime scene props |

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
| `gentleTouchUses >= 2`, previous action was Observe, `waitStreak > 0`, and `trust < 4` | accepted, trust +1, stress -1 |
| `gentleTouchUses >= 2` | rejected, stress +1, trust -1 |
| previous action was Touch | rejected softly, stress +1 |
| Otherwise | accepted, trust +1, stress -1 |

Time cost: 2m.

Design note: Touch should not be the best move twice in a row. The first Touch can create safety, and an Observe-separated Touch can repair trust up to the sit-up threshold. Repeating Touch without reading him still ignores his boundary.

### Water

Code interaction: `OfferWater`

| Condition | Result |
|---|---|
| `waterUses >= 3` | rejected, stress +1 |
| previous action was Water | rejected, stress +1 |
| `stress > 4` | rejected, stress +1 |
| `trust >= 3` and body is ready to drink | accepted, awake +2, trust +1, stress -1 |
| Settled, `trust >= 3`, `stress <= 1`, previous action was not Window | accepted weakly, awake +1, stress -1 |
| `trust >= 4`, `stress <= 3`, but no clear body cue | accepted weakly, awake +1, stress -1 |
| `stress <= 3`, and previous action was Observe | rejected gently, no stat change |
| `stress <= 3` without an observed cue | rejected gently, no stat change |
| Otherwise | rejected, stress +1 |

Time cost: 2m.

Body ready to drink currently means `awake >= 4` from something other than the immediately previous Window. Generic opening Observe does not make Water strong; this prevents first Observe from implying Water. When the next Water would enter the strong success branch, the feedback should say it directly after any action: "He looks toward the cup." A refused Water offer should not become a default trust-building move. Consecutive Water never adds trust; repeating a refused offer turns care into pressure.

### Window

Code interaction: `OpenCurtain`

| Condition | Result |
|---|---|
| `curtainUses >= 1` | rejected, stress +1 |
| opening / `awake == 0` | accepted, awake +1, stress +1, no trust change |
| `trust >= 3`, `stress <= 4`, and state is Settled or previous action was Observe | accepted, awake +2, stress -1 |
| Otherwise | too bright, awake +1 net, stress +2, trust -1 |

Time cost: 1m.

Design note: Window is primarily an awake/environment action. As an opener, it gives a faster but slightly tense start without damaging trust. It should not reliably increase trust, because that creates a fixed "Window -> Water" combo.

### Scratch

Code interaction: `Scratch`

| Condition | Result |
|---|---|
| `stress >= 6` | rejected, awake +1, stress +1, trust -1 |
| previous action was Scratch | rejected, awake +1, stress +2, trust -1 |
| `scratchUses >= 2` | rejected, awake +1, stress +1, trust -1 |
| opening / `awake == 0` | rejected, awake +2, stress +1, trust -1 |
| `trust < 3`, `awake >= 1`, `stress <= 2`, previous action was Observe | accepted, awake +1, trust +2, stress +2 |
| `awake >= 3` and `trust >= 2` | accepted, awake +2, stress +1; trust +1 only if `trust >= 3` and previous action was Observe |
| Settled, `trust >= 3`, `stress <= 1` | accepted lightly, awake +1, stress +1, no trust change |
| Otherwise | rejected, awake +1, stress +1, trust -1 |

Time cost: 1m.

Design note: Scratch is a timing tool. As an opener, it creates a faster but rougher route: more awake than Window, less damaging than Urge, but it still costs trust. After Observe, early Scratch can become the risky trust route: more trust than Touch, but it raises stress instead of lowering it. Once he is half-awake, it can replace some need for Urge, but the strong awake gain always stimulates him. At high trust it is still risky because it raises stress, and using it twice in a row clearly backfires.

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

Observe feedback should not say "do not use X next" or imply a temporary debuff. If an action was rejected, Observe can show that giving space helped his body settle, but it should not become a rule hint that a specific button is forbidden. The player should infer broad state: tense, calmer, still unclear, more awake, or ready for gentle help.

After the opening Observe, the rule layer evaluates the next non-Urge foreground actions: Touch, Water, Window, and Scratch. It scores each action by awake gain, trust gain/loss, stress gain/loss, rejection, resistance risk, and sit-up progress.

If the next Water would enter the strong success branch, the rule layer prioritizes the direct Water cue after any action: "He looks toward the cup." This is not a random need or temporary buff; it is the visible expression of the same body-ready condition used by the Water rule.

When one action is clearly better, about 30% of Observe feedback can strongly imply that next action through body language, such as being ready for a small sip. When one action is clearly worse, about 70% can strongly imply what would crowd, interrupt, overstimulate, or pressure him. The wording should stay diegetic and physical, not system-like: "More light now would be too sudden" is acceptable; "Window is debuffed" or "Do not press Window" is not.

For the current rule implementation, a next action counts as clearly better only when its score is at least 4 and it leads the second-best action by at least 2 points. A next action counts as clearly worse only when its score is at most -5 and it is at least 2 points worse than the second-worst action. If neither threshold is met, the score gap is treated as unclear and Observe stays vague. The first Observe from a fresh morning also stays vague so the game does not immediately collapse into a single instructed route.

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

Current implementation shows a morning reflection as soon as he reaches `SittingUp`, or when the time window closes.

### Bad / Tense Outcome

This is a tense version of getting up, not a failure label. Any of these:

- `forcefulUseCount >= 3`.
- `trust < 4` at sit-up.
- `stress > 4` at sit-up.

### Today Stops Here

If time runs out before he sits up, the attempt ends with "Today stops here." The game should avoid language like failure, lose, or game over. The state is still useful feedback about what his body could not receive today.

### Good Outcome

All of these:

- He reaches `SittingUp`.
- Time has not run out.
- `trust >= 4`.
- `stress <= 4`.
- Does not qualify for Very Good.

### Very Good Outcome

No-force version:

- He reaches `SittingUp`.
- Time has not run out.
- `forcefulUseCount == 0`.
- `trust >= 5`.
- `stress <= 3`.
- `usedMorningMinutes <= 12`.

Repaired-after-one-Urge version:

- He reaches `SittingUp`.
- Time has not run out.
- `forcefulUseCount == 1`.
- `trust >= 6`.
- `stress <= 2`.
- `usedMorningMinutes <= 10`.

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
