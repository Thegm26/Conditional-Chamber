# Chamber Logic — Level 1

An original, Buckshot-inspired 3D probability duel for Unity. Every shell count is public; the tension comes from deciding with changing conditional odds.

## Run

Open this folder in Unity 6, open `Assets/Scenes/Chamber.unity`, and press Play. The editable scene is also the configured build scene.

The saved scene is directly authored: one animated dealer sits across a proportionally scaled, textured wooden duel table with a matching chair set, and one shared shotgun rests at the measured center of the tabletop. Six realistically scaled shells begin on a separate side table before the slow loading reveal. Shooting includes dealer hold/point/fire/constrained-hit states, recoil, muzzle flash, camera impact, layered gun audio, ambient sound, and a looping horror score.

Use `1` to aim at the dealer, `2` to aim at yourself, and `R` to restart. In Scene view, `ChamberPoseDebug` draws colored grip points and muzzle-to-face lines for all four authored firing poses; its context menu can log positions, distances, and angular error.

## Lesson

`P(live next | revealed shells) = live shells remaining / shells remaining`.

The player gets counts during the challenge. Once the round ends, the after-action report walks through every probability update. A blank fired at yourself retains your turn; every other result hands the turn to the dealer.

## Art license

The environment props come from [Kenney's City Kit Industrial](https://kenney.nl/assets/city-kit-industrial), the firearm comes from [Quaternius' Animated Guns Pack](https://quaternius.com/packs/animatedguns.html), and the dealer and animations come from [Quaternius' Ultimate Modular Men](https://quaternius.com/packs/ultimatemodularcharacters.html). The matching duel table/chairs come from [Wooden Furnitures](https://opengameart.org/content/wooden-furnitures), while the shell side table comes from [Quaternius' Furniture Pack](https://quaternius.com/packs/furniture.html). These assets are CC0; license files are included beside each imported pack.
