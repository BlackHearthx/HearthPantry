# Changelog

## 1.0.4

You choose every meal yourself. The pantry only re-eats the same food while it
is still active and nearing the configured renewal threshold. Empty slots stay
empty, including after vomiting or death. Automatic slot filling and food
scoring have been removed; old Fill Empty Slots settings no longer apply.
The workbench/boat pause also fixes the private SetMaxEitr access that caused
repeated MethodAccessException errors in version 1.0.3.

## 1.0.3

The pantry now leaves your supplies alone during death, teleporting, cutscenes
and vomiting. Dodged hits no longer trigger healing or frost meads, and friendly
creatures no longer make it drink resistance meads. Vulnerability effects are
never mistaken for protection, and frost-hit history stays bounded and resets
between characters.

Workbench and boat pauses preserve the actual meal timers and food bonuses.
TimeControl slows the countdown while keeping healing and renewal percentages
consistent. Food choices use balanced scales and include a configurable eitr
weight. The pantry tries another food when its first choice cannot be eaten,
checks each nearby threat's current equipment, and keeps an English fallback
even when only part of the translations folder is available.

## 1.0.2

Auto-eating pauses while vomiting and resumes when the effect ends. Puke-inducing
items are excluded from automatic food selection. Empty-slot filling is bounded
and stops if consuming an item does not add a food slot. Food timer pauses no
longer interfere with vomiting. Auto-eating also pauses while dead or teleporting.
Low-stock notices for newly filled slots no longer repeat every tick.

## 1.0.1

HUD messages follow the game language across the Valheim language set.

## 1.0.0

First release. Auto-refill of the same meal near the flash, fill empty food
slots from inventory, workbench and boat timer pause, low-stock notices, fight
meads, and a hotkey to quiet the pantry.
