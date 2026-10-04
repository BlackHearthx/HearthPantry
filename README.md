# Hearth Pantry

Your meals, your choice. **By BlackHearthx.**

Eat the food you want by hand. Hearth Pantry watches those active meals and
re-eats the same dish from your bag when its timer drops near half (45% by
default). It never chooses a new food or fills an empty slot. If a meal expires
completely, you choose what to eat next.

Bukeperries leave you free to change your meals: the pantry pauses during
vomiting, and the emptied slots stay empty afterward. Eat your new choices
manually and the pantry will maintain those meals.

A short message tells you when it re-eats a dish, when a meal is fading, and
when your supply is running low. At a nearby workbench or on a boat, food
timers pause while healing keeps working.

Health and resistance meads can still drink themselves when needed. Healing
normally requires low health after actual enemy damage; resistance reacts to
hostile fire/poison threats, freezing or repeated frost damage. Death,
teleporting, cutscenes and vomiting pause automatic consumption.

## Controls

| Key | What it does |
| --- | --- |
| Shift+T | Pause or resume automatic renewal of active meals |

Choose your meals manually, carry spare portions of those dishes, and the
pantry takes care of renewal. An empty belly stays empty until you eat.

## Compatibility

Needs BepInEx and Jotunn. With DrummerCraig's TimeControl, food decay follows
its day multiplier without slowing healing. Renewal percentages remain
consistent. Extended food timers from older versions adopt the new behavior
when that meal is eaten again.

Keep Food On Death is available but off by default. Do not run this alongside
Hunger Pangs or Glutton, which also change food consumption and timers.

## Como usar (PT-BR)

Você escolhe e come cada comida manualmente. A despensa só recome a mesma
comida enquanto ela ainda está ativa, quando o tempo restante chega perto
 de 45%. Ela não escolhe comida nova e não preenche espaços vazios.

Depois de vomitar, os espaços ficam vazios até você escolher e comer de novo.
Se uma comida acabar completamente, você também escolhe a próxima. Carregue
porções extras das comidas escolhidas para o mod renovar essas refeições.
Shift+T pausa ou retoma essa renovação.

A pausa dos timers na bancada e no barco, os avisos de estoque e os hidroméis
automáticos continuam disponíveis nas configurações.

## Language

Follows the game language. English, both Portuguese variants, German, French,
Spanish, Russian, Polish, Dutch, Italian, Swedish, Turkish, Ukrainian, Chinese
(simplified and traditional), Japanese and Korean ship with the mod. To change
a message, edit `Translations/<Language>/hearthpantry.json` next to the DLL.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)

## Config

Written on first run to `BepInEx/config/com.blackhearthx.hearthpantry.cfg`.
Configure renewal, notifications, timer pauses, meads and the hotkey there.
Old Fill Empty Slots, Eat Best Foods First and Pantry Scoring settings are
ignored in version 1.0.4 and later.
