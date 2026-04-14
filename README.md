# The Case of the Missing Glasses

A [SMAPI](https://smapi.io/) mod for Stardew Valley that adds lore and a meaningful choice around the **Winter Mystery** quest — buy the Magnifying Glass from Krobus before he loses it, or find it the old-fashioned way.

---

## Overview

In vanilla Stardew Valley, the Magnifying Glass appears out of nowhere during the Winter Mystery quest. This mod adds a backstory: **Krobus has owned the Magnifying Glass all along**, and will sell it to you from his shop until Fall 28, Year 1 — the day it gets stolen. If you miss your chance, the Winter Mystery plays out as normal.

---

## Features

- 🔍 **Buy the Magnifying Glass from Krobus's shop** for 5,000g — available any time before Fall 28, Year 1.
- 🚫 **Suppresses the Winter Mystery quest** if you already own the Magnifying Glass, so the event doesn't trigger redundantly.
- 💬 **New Krobus dialogue** — Krobus reacts to losing his glasses and, if you've found them via the quest, acknowledges that you have them.
- 🛍️ **Wallet item integration** — purchasing from the shop grants the Magnifying Glass as a proper wallet item, exactly like obtaining it through the quest.

---

## How It Works

| Situation | Result |
|---|---|
| You buy from Krobus before Fall 28, Year 1 | Magnifying Glass granted as wallet item; Winter Mystery quest suppressed |
| You don't buy it in time | Winter Mystery plays out as normal in Winter |
| You met Krobus before Fall 28 but didn't buy it | Krobus has new dialogue lamenting his lost glasses |
| You complete the Winter Mystery after meeting Krobus | Krobus has new dialogue acknowledging you found them |

---

## Installation

1. Install [SMAPI](https://smapi.io/) (minimum version 3.0.0).
2. Download this mod and unzip it into your `Stardew Valley/Mods` folder.
3. Launch the game through SMAPI.

---

## Compatibility

- **Stardew Valley**: 1.6+
- **SMAPI**: 3.0.0+
- **Multiplayer**: Not tested — intended for single-player use.
- **Content Patcher**: Not required; asset edits are handled directly via SMAPI.

This mod patches `Data/Events/BusStop`, `Data/Objects`, `Characters/Dialogue/Krobus`, and `Data/Shops` (ShadowShop). It may conflict with other mods that heavily edit the same assets.

---

## Known Issues / Limitations

- The hold-up animation triggers when the item lands in inventory rather than at the moment of purchase (unlike the Stardrop from Krobus's shop). This means edge cases like a full inventory are possible. A fix is planned.

---

## License

This mod is provided as-is for personal use. Feel free to fork or build on it — just credit the original author.

---

*Mod ID: `Raghu.KrobusMagnifyingGlass`*