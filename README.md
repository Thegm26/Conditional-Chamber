# Chamber Logic — Level 1

An original, Buckshot-inspired 3D probability duel for Unity. Every shell count is public; the tension comes from deciding with changing conditional odds.

## Run

Open this folder in Unity 6, open `Assets/Scenes/Chamber.unity`, and press Play. The editable scene is also the configured build scene.

The saved scene is directly authored: a tall hanging shroud with a large cracked ritual-mask face and two independent severed-hand meshes waits across a proportionally scaled wooden duel table. Six approximately 69 mm shells rest on a three-crate stand beside the table. During the slow reveal, the shotgun action opens and remains open while every shell travels from that stand into the breech, then closes. Shooting uses direct hand-to-grip transforms with no humanoid rig or IK chain, separate self-aim contacts, visible fore-end travel, a distorted apparition hit reaction, recoil, muzzle flash, camera impact, layered gun audio, ambient sound, and two overlapping CC0 horror-music loops. The room shell is almost black with restrained cold table light and red apparition light.

Use `1` to aim at the dealer, `2` to aim at yourself, and `R` to restart. In Scene view, `ChamberPoseDebug` draws colored grip points and muzzle-to-face lines for all four authored firing poses; its context menu can log positions, distances, and angular error.

## Lesson

`P(live next | revealed shells) = live shells remaining / shells remaining`.

The player gets counts during the challenge. Once the round ends, the after-action report walks through every probability update. A blank fired at yourself retains your turn; every other result hands the turn to the dealer.

## Art license

The industrial environment and shell-crate assets come from Kenney's CC0 packs, the firearm comes from [Quaternius' Animated Guns Pack](https://quaternius.com/packs/animatedguns.html), and the shroud, detached hands, coffin, body bag, and bottles come from loafbrr's [Halloween Props](https://opengameart.org/content/halloween-props). The matching duel table/chairs come from [Wooden Furnitures](https://opengameart.org/content/wooden-furnitures). The music layers are [Creepy Ambient Loop](https://opengameart.org/content/creepy-ambient-loop) and [Abandoned Passages](https://opengameart.org/content/abandoned-passages-horror-ambience-loop), both CC0. License files are included beside each imported pack.
