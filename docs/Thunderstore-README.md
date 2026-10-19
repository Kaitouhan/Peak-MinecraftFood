![Minecraft Foods for PEAK](https://raw.githubusercontent.com/Kaitouhan/Peak-MinecraftFood/main/docs/media/banner-1.25.png)

# Minecraft Foods

**Bring familiar Minecraft food on your next PEAK expedition.**

Find apples, bread, meat and rare treats in luggage. Cook your dinner over a campfire, share it with a teammate, or save a golden apple for a difficult climb. Minecraft Foods adds **34 foods** with pixel-style models, eating sounds, individual cooking rules and special effects designed around PEAK's hunger and extra stamina.

*The banner is promotional artwork. Item icons below are included with the mod.*

## Find food in luggage

Ordinary, cursed and clown luggage have separate loot settings. Respawn coffins do not receive Minecraft food.

| Chest | Default mod-food share | Common | Uncommon | Rare | Special |
| --- | ---: | ---: | ---: | ---: | ---: |
| Ordinary luggage | 25% | 60 | 25 | 12 | 3 |
| Cursed luggage | 35% | 35 | 25 | 30 | 10 |
| Clown luggage | 30% | 45 | 30 | 18 | 7 |
| Respawn coffin | 0% | — | — | — | — |

The share is a **weight fraction in the loot pool**, not a guarantee that every chest contains mod food. Tier numbers are relative weights within Minecraft food; the defaults add up to 100. Individual foods are selected by their base weight and configurable multiplier within that tier.

Golden apples, enchanted golden apples and chorus fruit belong to the special tier. Rotten flesh moves into the uncommon tier in cursed luggage.

## Food for the climb

| Everyday supplies | Campfire meals | Special treats |
| --- | --- | --- |
| ![Apple](https://raw.githubusercontent.com/Kaitouhan/Peak-MinecraftFood/main/assets/textures/apple_icon.png) | ![Steak](https://raw.githubusercontent.com/Kaitouhan/Peak-MinecraftFood/main/assets/textures/cooked_beef_icon.png) | ![Golden Apple](https://raw.githubusercontent.com/Kaitouhan/Peak-MinecraftFood/main/assets/textures/golden_apple_icon.png) |
| Apples, bread, cookies and berries | Cooked meat, fish and hearty stews | Golden apples, chorus fruit and suspicious stew |

| Example food | Hunger removed per use | Extra stamina per use |
| --- | ---: | ---: |
| Cookie | 12 | 12 |
| Apple | 18 | 12 |
| Bread | 18 | 23 |
| Steak, after cooking raw beef once | 36 | 33 |
| Mushroom stew, before heating | 45 | 45 |
| Rabbit stew, before heating | 48 | 46 |

These are PEAK values. Status caps, immunity and difficulty rules still apply.

## Cook in place

- Raw meat and fish change their name, texture and inventory icon after their first cook, without being replaced by a dropped item.
- The first heating improves nutrition; repeated heating does not increase it forever.
- All foods follow the same cooking sequence: the first cook improves nutrition; the second cook chars the food, reduces nutrition and adds poison. Charred food retains 50% nutrition on the second cook and 10% from the third cook onwards. Further cooking increases poison up to its cap, until the food reaches its individual destruction threshold and becomes inedible. Naturally harmful foods retain their risks after the first cook. Charring also removes special effects such as golden apple buffs, chorus fruit teleportation and milk cleansing.
- Tooltips show the **change caused by the next cook**, including lost nutrition or added poison. Unchanged effects are not repeated as a new benefit.

## Special food, real consequences

- **Golden apple:** 5 seconds of invincibility, 20 injury recovery over 5 seconds, and a 20-point injury shield lasting up to 60 seconds. When invincibility ends, drowsiness gradually rises over 6 seconds. After the native falling-asleep transition, sleep is maintained for 6 seconds measured from actual unconsciousness. The added drowsiness is then removed down to the pre-apple baseline; other statuses can delay waking.
- **Enchanted golden apple:** 12 seconds of invincibility, 80 injury recovery over 20 seconds, an 80-point shield, 20% injury reduction and protection against heat gain for 60 seconds. Drowsiness rises over 12 seconds, followed by 12 seconds of sleep measured from actual unconsciousness. Milk can interrupt either apple aftereffect.
- **Chorus fruit:** attempts to find a landing within 30 metres horizontally. Each successful teleport adds either 25 hunger or 15 spores (50% each). If no suitable location is found, you stay where you are and pay no teleport penalty. Successful teleports at most 120 seconds apart accumulate exposure: the fourth starts a 5-second warning, then you die and spawn a hostile PEAK mushroom zombie. Drink milk before the countdown ends to clear exposure; waiting 120 seconds without another success clears exposure before the fourth use. Honey clears spores, but does not clear this exposure counter.
- **Honey:** clears poison, spores and ongoing food poison. **Milk** clears temporary effects, poison, spores and drowsiness, including buffs, pending golden-apple aftereffects and chorus exposure.
- **Suspicious stew:** one of eight random outcomes, each shown in the tooltip with its probability.
- **Pufferfish, rotten flesh, spider eyes and poisonous potatoes:** retain their individual risks. Cooking does not detoxify them. Raw meat and fish can also cause poisoning.

Repeated effects refresh their remaining budget and retain the stronger rate instead of adding unlimited stacks. The seven-serving cake is rare to account for its total recovery. Separate sources of poison may coexist. Drowsiness and poison already accumulated do not instantly disappear when a timer ends.

## Readable effects and language support

The mod follows your game's language automatically: **Simplified Chinese, English and Japanese** are supported. Traditional Chinese uses the Simplified Chinese translation; other languages fall back to English. You can override this in settings.

Food names, eating prompts, effect text, cooking previews, timers and settings are localized. Tooltips use PEAK-style coloured status values, separate effect lines, a list of random outcomes and next-cook changes. Multi-use foods show their remaining uses.

Optional on-screen timers show food buffs, shield points, ongoing negative effects and pending drowsiness. Held-food information also works without an extra display mod. **ItemInfoDisplayForkedCNPlus 1.1.3** is optional.

Eating sounds play for the local holder while eating or feeding and stop on completion or cancellation. SoundAPI replacements only target this mod's eating sound. Remote positional eating audio is not included.

## F10 settings

Press **F10** to open or close the in-game panel. Choose your values and click **Apply & save**. Closing without applying discards the draft. **Reload file** reads manual changes from disk. The panel uses the game's menu input and cursor handling; the multiplayer world keeps running.

All exposed settings are stored in:

`BepInEx/plugins/MinecraftFoods/settings.cfg`

The file is generated on first launch, with Chinese, English and Japanese comments. Existing settings from `BepInEx/config/codex.peak.minecraftfoods.cfg` are copied on first migration if the new file does not exist. Afterwards, edit the file beside the plugin. The package includes `settings.example.cfg` for reference and does not overwrite your actual configuration.

| Section | What you can change |
| --- | --- |
| `UI` | Automatic or fixed language, food information, detailed cooking thresholds and effect timers |
| `Effects` | Food effects in the airport and golden-apple drowsiness aftereffects |
| `Visuals` | TrailMix or SmallEgg grip template; requires a game restart |
| `Loot` | Independent mod-food shares for ordinary, cursed and clown luggage; 0 disables mod food, 1 requests mod food only |
| `Tiers.Ordinary`, `Tiers.Cursed`, `Tiers.Clown` | Relative common, uncommon, rare and special weights per chest type |
| `FoodWeights.Ordinary`, `FoodWeights.Cursed`, `FoodWeights.Clown` | A multiplier for each food in each chest type; 1 is default, 0 disables that food |

**The host controls loot.** Loot controls are read-only for clients in another player's room. Changes affect future loot generation, not existing contents. Some contents may already be determined when a level loads, so start a new level or run when testing a new distribution.

A tier weight of 0 disables that tier. Nonempty enabled tiers are normalized. If all mod-food choices in a pool are disabled, the original loot is retained rather than creating an empty pool. Doubling an individual weight does not double its final probability, because all weights compete.

## Installation

Use **r2modman** or **Thunderstore Mod Manager** to install the package and its dependencies.

For manual installation, install the required dependencies, then copy the package's `plugins/MinecraftFoods` folder into `PEAK/BepInEx/plugins/`. Code, textures and sounds are kept in one mod folder. Do not keep duplicate MinecraftFoods DLLs when updating.

Required dependencies:

- [BepInExPack_PEAK 5.4.2403](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)
- [PEAKLib Core 1.7.2](https://thunderstore.io/c/peak/p/PEAKModding/PEAKLib_Core/)
- [PEAKLib Items 1.6.2](https://thunderstore.io/c/peak/p/PEAKModding/PEAKLib_Items/)
- [MonoDetour_BepInEx_5 0.6.7](https://thunderstore.io/c/peak/p/MonoDetour/MonoDetour_BepInEx_5/)
- [loaforcsSoundAPI 2.0.13](https://thunderstore.io/c/peak/p/loaforc/loaforcsSoundAPI/)

Host and clients should use the same mod version and dependencies. Minecraft Foods is independent of the author's other custom-item mods.

## Feedback and source

Report issues on [GitHub Issues](https://github.com/Kaitouhan/Peak-MinecraftFood/issues), including your game and mod versions, language, reproduction steps and relevant `BepInEx/LogOutput.log` entries. Changes are recorded in `CHANGELOG.md`.

## Credits

- PEAK: Aggro Crab and Landfall.
- Minecraft and original game resources: Mojang Studios / Microsoft.
- Sound support: loaforc — loaforcsSoundAPI. Item registration: PEAKLib.
- This is an unofficial community mod with no affiliation to the game developers. The source-code license does not change ownership of third-party assets.
