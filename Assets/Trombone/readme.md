# Pink Trombone for Unity

This is a port of the core of [Pink Trombone](https://dood.al/pinktrombone/) to Unity.
Pink Trombone simulates the nose and throat to create procedural humanlike audio.

Please note that this is *not* a speech synthesizer. You could theoretically extract lexemes
from a string and relate those to configurations of the simulation to produce something
approaching speech (which I plan to do), but it won't sound very good. I would use this
for goofy "speech", kind of like how characters speak in Banjo-Kazooie.

## Usage

Create an empty GameObject and drag the `Trombone` script onto it.

If you're getting popping or clicking in the generated audio, go into the `Trombone` class and set 
`DO_THREADING` to `false`. Note that this will negatively affect performance, as it places all
computation on Unity's audio thread.

## Caveats

This is a straightforward port of the original code, with some refactoring.
There are a lot of variables with magic names like `A` and `n`, and very few of them were commented,
so quite frankly there's a lot I don't understand about the code. If you know more, I'd be thrilled
if you'd like to contribute.

Performance isn't great yet. Remaking this as a [native plugin](https://docs.unity3d.com/Manual/AudioMixerNativeAudioPlugin.html) would help immensely.