# Chamber Logic — Level 1

An original, Buckshot-inspired 3D probability duel for Unity. Every shell count is public; the tension comes from deciding with changing conditional odds.

## Run

Open this folder in Unity 6, open `Assets/Scenes/Chamber.unity`, and press Play. The editable scene is also the configured build scene.

The scene is directly authored and contains a first-person horror duel room, persistent materials, three animated humanoid characters, one properly scaled shotgun, a freestanding target, six shell props, weapon recoil, muzzle flash, layered gun audio, ambient sound, and a looping horror score. Use `1` to aim at the house, `2` to aim at yourself, and `R` to restart.

## Lesson

`P(live next | revealed shells) = live shells remaining / shells remaining`.

The player gets counts during the challenge. Once the round ends, the after-action report walks through every probability update. A blank fired at yourself retains your turn; every other result hands the turn to the dealer.

## Art license

The environment props come from [Kenney's City Kit Industrial](https://kenney.nl/assets/city-kit-industrial), the target props come from [Kenney's Blaster Kit](https://kenney.nl/assets/blaster-kit), the firearm comes from [Quaternius' Animated Guns Pack](https://quaternius.com/packs/animatedguns.html), and the dealer/watchers and their animations come from [Quaternius' Ultimate Modular Men](https://quaternius.com/packs/ultimatemodularcharacters.html). All are CC0; license files are included beside each pack.
