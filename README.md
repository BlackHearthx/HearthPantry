# Hearth Pantry

Never miss a meal at the homestead.

**By BlackHearthx.**

Your bag already holds the food. Hearth Pantry just keeps an eye on the three
meals in your belly the way a good pantry should: when a buff starts to fade,
it reaches for another of the same dish; when a seat at the table is empty, it
picks something sensible from what you are carrying; and when a fight turns
ugly, it drinks the mead you packed for that moment.

You still cook. You still loot. You just stop dying because you forgot to chew.

## What you get

The pantry re-eats a meal once the timer drops near half (default is 45%, right
as the icon is flashing and the health/stamina from that food has already started
to slip). If you are walking around with fewer than three foods active, it fills
the gap with the best match from your inventory. A short line on screen tells
you when it ate, when a dish is running low, and when the stack in your bag is
almost gone.

Auto-eating pauses while vomiting (for example after eating Bukeperries), then
resumes when the effect ends. The pantry never automatically selects food that
applies a vomiting effect.

At the workbench and on a boat the countdown holds still so crafting and sailing
do not burn through dinner. Healing and stamina from the meal keep working;
only the timer pauses.

Health meads and resist meads drink themselves when you need them in a fight —
low health after an enemy hit, poison or fire closing in, frost biting or the
mountain freezing you. Shift+T pauses the whole auto-eat habit if you want the
table quiet for a while.

## Your first session

1. Pack the meals you actually want to run, plus a spare or two of each.
2. Eat as usual until you have one, two, or three foods going.
3. Play. When a dish flashes, the pantry should finish the thought for you.
4. Bring meads if you are headed into a fight; leave health mead for combat so
   a clumsy fall does not drink the bottle.
5. Sit at a workbench or hop on a ship and watch the timers hold.

## Controls at a glance

| Key | What it does |
| --- | --- |
| Shift+T | Pause or resume auto-eating (and filling empty slots) |

Everything else is in the config file and can be tuned or turned off.

## Compat and notes

Needs BepInEx and Jötunn. Soft support for DrummerCraig's TimeControl: if that
mod is lengthening the day, food lasts with it. Keep Food On Death is available
but off by default.

Do not run this beside Hunger Pangs or Glutton — they all touch the same food
loop and will fight each other.

## Como usar (PT-BR)

A despensa cuida das três comidas ativas: recome a mesma quando o timer chega
perto da metade, preenche slot vazio com o que tiver de melhor na bolsa, avisa
quando o estoque está baixo, pausa o countdown na bancada e no barco, e bebe
meads sozinha na hora da luta. Shift+T liga e desliga o auto-eat. Continua
cozinhando e carregando comida — só para de esquecer de comer.

Ao usar a fruta que provoca vômito, o auto-eat espera o efeito terminar antes
de voltar a comer. Itens que provocam vômito não são escolhidos automaticamente.

## Language

Follows the game language. English, both Portuguese variants, German, French,
Spanish, Russian, Polish, Dutch, Italian, Swedish, Turkish, Ukrainian, Chinese
(simplified and traditional), Japanese and Korean ship with the mod. To tweak a
line, edit `Translations/<Language>/hearthpantry.json` next to the DLL.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)

## Config

Written on first run to `BepInEx/config/com.blackhearthx.hearthpantry.cfg`.
Every habit has its own switch: auto-eat, slot fill, notifications, workbench
and boat pause, meads, and the scoring weights used when the pantry chooses a
new dish.

Food scoring compares health, stamina, eitr, duration and regeneration on
comparable scales. Set Eitr Weight to zero if you do not use magic. TimeControl
slows the food countdown without slowing healing; renewal percentages still
follow the meal's remaining duration. Existing extended timers from older
versions return to the new behavior when that meal is eaten again.
