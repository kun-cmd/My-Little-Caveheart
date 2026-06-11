# Caveheart Gameplay Rules

This document is the source of truth for the current neutral-morning rules. Update it with every gameplay value change.

## Core Loop

The player is not solving hidden arithmetic. The loop is:

1. Observe evidence in his body and relationship.
2. Form a hypothesis about what he needs.
3. Act and read the immediate response.
4. Correct the hypothesis or repair the relationship.

The goal shown at the start of every morning is:

> Help him sit up gently. Watch if he is awake, safe, and calm.

Random morning profiles and random Observe hints are disabled. The current build first establishes one readable neutral morning.

## Hidden Values

The player reads these through animation, environment, sound, action feedback, and Observe text. Whitebox bars may expose them for testing.

| Value | Range | Start | Meaning |
|---|---:|---:|---|
| awake | 0-8 | 0 | Physical wakefulness |
| trust | 0-8 | 1 | Willingness to receive help |
| stress | 0-8 | 1 | How strongly the body is bracing |

He sits up when:

- `awake >= 6`
- `trust >= 4`
- `stress <= 4`

## Time

The morning window is 14 game minutes. Real seconds do not consume it.

| Action | Time |
|---|---:|
| Urge | 1m |
| Touch | 2m |
| Window | 1m |
| Water | 2m |
| Scratch | 1m |
| Observe | 1m |

When the window closes, the result is "Today stops here", not a failure or game-over state.

## Action Roles

Each foreground action has one primary role.

| Action | Primary role | What it should not replace |
|---|---|---|
| Urge | Ignore readiness and wake him immediately | Safety or trust |
| Touch | Build safety and relationship | Physical waking |
| Window | One-time environmental waking | Trust building |
| Water | Answer a visible body need | Opening strategy |
| Scratch | Playful waking with stimulation risk | General trust farming |
| Observe | Read body stage and relationship state | Free progress or a random answer generator |

Rejected Touch, Water, and Scratch attempts do not consume their accepted-use limits. A wrong hypothesis still costs time, stress when specified, and eligibility for Very Good, but it does not permanently remove the player's ability to repair the morning.

## Foreground Actions

### Urge

Code interaction: `Alarm`

| awake | trust | stress | accepted | time |
|---:|---:|---:|---|---:|
| +2 | -1 | +3 | yes | 1m |

Urge is the direct-control route: fast physical progress with clear relationship damage. It increments `forcefulUseCount`.

### Touch

Code interaction: `GentleTouch`

Normal result:

| awake | trust | stress | accepted | time |
|---:|---:|---:|---|---:|
| 0 | +1 | -1 | yes | 2m |

Exceptions:

| Condition | Result |
|---|---|
| Previous action was Touch | rejected, stress +1 |
| Two accepted Touch uses already occurred | rejected, stress +1 |
| `stress >= 6` | rejected, trust -1, stress +1 |

Touch is the safest relationship action, but it is slow, never wakes him, cannot be repeated immediately, and has only two accepted uses. Limiting it to two prevents Touch from becoming the default opening in nearly every Very Good route.

### Window

Code interaction: `OpenCurtain`

| Condition | awake | trust | stress | accepted |
|---|---:|---:|---:|---|
| Previous action was Observe and `stress <= 3` | +3 | 0 | 0 | yes |
| Opening while `awake == 0` | +2 | 0 | +1 | yes |
| Other first use | +2 | 0 | +1 | yes |
| Already used | 0 | 0 | +1 | no |

Time: 1m.

Window is the strongest non-forceful pure wake-up action because it is irreversible and available only once. Observe gives clearer timing and avoids the stress cost. Window never creates trust and does not immediately create a Water cue.

### Water

Code interaction: `OfferWater`

Water is ready when all are true:

- `awake >= 3`
- `stress <= 3`
- the immediately previous action was not Window
- `trust >= 2` to accept the cup from the player

Strong result:

| awake | trust | stress | accepted | time |
|---:|---:|---:|---|---:|
| +2 | +1 | -1 | yes | 2m |

Exceptions:

| Condition | Result |
|---|---|
| Two accepted Water uses already occurred | rejected, stress +1 |
| Previous action was Water | rejected, stress +1 |
| `stress > 4` | rejected, stress +1 |
| Body cue exists but `trust < 2` | rejected, no stat change |
| No body cue | rejected, no stat change |

Whenever the next Water can enter its strong branch, feedback states: "He looks toward the cup." This cue can appear after any action. If Scratch is also strongly available, feedback shows both the cup and exposed foot instead of presenting Water as the single answer. Water is a response to visible thirst, not a generic care button or opening move.

### Scratch

Code interaction: `Scratch`

| Condition | awake | trust | stress | accepted |
|---|---:|---:|---:|---|
| Play signal: `awake 2-5` and `stress <= 1` | +2 | +1 | +1 | yes |
| Half-awake without play signal | +1 | 0 | +1 | yes |
| Deep sleep: `awake == 0` | 0 | 0 | +1 | no |
| Tense: `stress >= 4` | 0 | 0 | +1 | no |
| One accepted Scratch already occurred, or immediate repeat | 0 | 0 | +1 | no |

Time: 1m.

Scratch is playful waking. It is faster and more relational than Window when the play signal exists, but it always adds stimulation. It no longer grants an exceptional `trust +2`; one accepted use is the limit.

### Observe

Code interaction: `Wait`

| Condition | trust | stress | time |
|---|---:|---:|---:|
| `stress >= 4` | +1 only on the first Observe immediately after a forceful action | -2 | 1m |
| Immediately after a rejected action | 0 | -1 | 1m |
| Otherwise | 0 | -1 | 1m |

Observe always reports two stable dimensions after its calming effect:

Body stage:

| awake | Report |
|---:|---|
| 0 | Deep sleep |
| 1-2 | Starting to wake; eyes still heavy |
| 3-5 | Half-awake |
| 6-8 | Awake enough to sit up |

Relationship state:

| Condition | Report |
|---|---|
| `stress >= 6` | Defensive |
| `stress >= 4` | Guarded |
| `trust >= 5` | Actively expressing needs |
| `trust >= 3` | Accepting help |
| Otherwise | Hesitant |

Observe then adds a physical cue only when the state supports one:

| Priority | Condition | Cue meaning |
|---:|---|---|
| 1 | Water and Scratch are both strongly available | Eyes move between cup and hand; one foot remains exposed |
| 2 | Only Water is strongly available | His gaze returns to the cup |
| 3 | Previous action was forceful | Feet remain protected; head permits slow Touch |
| 4 | `stress >= 4` | More stimulation would crowd him |
| 5 | Only Scratch is strongly available | Feet shift toward the player's hand |
| 6 | Window unused, `awake < 3`, `stress <= 3` | Calm body, heavy eyes, no cup gaze |
| 7 | `trust < 3`, `stress <= 3` | Head remains within reach; he waits |

The first Observe of a fresh morning reports the baseline body and relationship only. It never says "no clear signal".

There is no generic candidate scoring, 30/70 split, state hash, temporary action buff, or random best/worst-action hint. When several actions are reasonable, Observe describes the state and lets the player choose a style.

The same physical cue appears immediately when a non-Observe action creates it. The player never needs to know that a hidden threshold was crossed in order to understand that Water or playful Scratch has become reasonable.

## State Rules

State is evaluated after each action.

| State | Condition |
|---|---|
| SittingUp | `awake >= 6`, `trust >= 4`, `stress <= 4` |
| Resisting | `stress >= 7` |
| Settled | `trust >= 3`, `stress <= 3` |
| Startled | `stress >= 4` or `awake > 0` |
| Sleeping | fallback |

## Outcome Rules

### Today Stops Here

The 14-minute window closes before SittingUp. The text describes which need was still unmet without calling the attempt a failure.

### Tense Morning

Any of these:

- `forcefulUseCount >= 3`
- `trust < 4` at sit-up
- `stress > 4` at sit-up

### Good

He reaches SittingUp within 14 minutes with `trust >= 4` and `stress <= 4`, but does not meet Very Good.

### Very Good

No-force version:

- `forcefulUseCount == 0`
- `rejectedActionCount == 0`
- `trust >= 5`
- `stress <= 3`
- `usedMorningMinutes <= 12`

Repaired-after-one-Urge version:

- `forcefulUseCount == 1`
- `rejectedActionCount == 0`
- `trust >= 5`
- `stress <= 2`
- `usedMorningMinutes <= 12`

Any rejected action disqualifies Very Good. This prevents a route from mistreating him early and erasing the mistake through later arithmetic, while still allowing a Good recovery.

## Required Route Coverage

The rule tests must keep all three styles viable:

| Style | Example |
|---|---|
| Safe | Touch -> Observe -> Window -> Touch -> Observe -> Water -> Observe -> Water |
| Play | Window -> Observe -> Scratch -> Touch -> Observe -> Touch -> Water |
| Repair | Urge -> Observe -> Touch -> Window -> Observe -> Touch -> Water |

At least one Very Good route must use no Scratch. Scratch is an optional style, not a hidden required answer.

## Feedback Rules

Every interaction presents:

1. What the player did.
2. How he reacted.
3. Plain-language impact on waking, safety, or tension.

The scene, animation, and sound reinforce this feedback but never carry the entire explanation alone. A rejection should reveal why the hypothesis failed whenever the body language can support that conclusion.

## Current UI

- Actions are spatial `Collider2D` zones in the scene, not bottom buttons.
- Touch is on the head; Scratch is on the feet.
- Window and Water use their matching scene props.
- Observe is the lowest-priority scene `BoxCollider2D`. Empty scene space uses the Observe eye cursor, while specific action zones take priority over it.
- Hovering an available interaction replaces the mouse with its 64 px action icon.
- After 0.35 seconds, the hover label shows the action name and time cost.
- Observe shows `Observe 1m` through the same scene hover label as other actions.
- Used-up actions return to the normal cursor and show a physical state sentence instead of a numeric charge counter.
- Touch stops after two accepted uses, Water after two, Scratch after one, and Window after opening.
- Clicking an unavailable action never consumes morning time.
- Zone bounds and curtain sprites are exposed in the Inspector.
- Hidden values remain available through whitebox bars and logs.

## Retired Actions

`ShakeBed` and `TuckBlanket` remain in code for compatibility but are not foreground actions or active scene props.
