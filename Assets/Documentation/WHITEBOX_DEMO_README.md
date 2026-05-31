# My Little Caveheart Whitebox Demo

This is the first no-art playable demo for the "getting up is hard" scene.

## Build the scene

Open Unity and run:

`Tools > My Little Caveheart > Rebuild Whitebox Demo`

The tool creates or rebuilds the durable play scene:

- `Assets/level1.unity`
- a camera, controller, UI, six clickable objects, whitebox character parts, overlays, and generated square sprite
- `EditorBuildSettings` entry pointing at the whitebox scene

## Play

Click the room objects:

- `Alarm`: fast awake gain, more stress, less trust
- `Shake Bed`: fast awake gain, heavy stress, trust loss
- `Gentle Touch`: trust gain and stress relief unless stress is too high
- `Water`: accepted only after enough trust
- `Curtain`: helps more after the little caveheart is settled
- `Blanket`: small trust gain and stress relief

The foreground UI intentionally shows observed state and reactions, not raw numbers.
Press `D` in Play Mode to toggle the hidden whitebox debug values.

## Tests

Run Edit Mode tests in Unity Test Runner. They cover:

- force waking causes short-term awake gain but damages safety
- repeated force reaches resistance
- gentle sequencing can reach sitting up
- water rejection when trust is too low
- touch rejection when stress is too high
