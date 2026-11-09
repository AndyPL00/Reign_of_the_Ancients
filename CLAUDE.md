# Reign of the Ancients: project notes for Claude

## Environment
- tModLoader (latest stable, Terraria 1.4.4 API). No `DisplayName.SetDefault`; display names live in `Localization/en-US_Mods.Reign_of_the_Ancients.hjson`.
- Root namespace `Reign_of_the_Ancients`. Folder casing must match namespaces exactly (`NPCs/Elves/`, `Projectiles/Elves/`).
- Build with `dotnet build` from this folder and fix any compile errors before handing back. Andy tests in-game himself.

## Conventions
- NPC sprites: 32×48 per frame, 4 frames (32×192 sheet). Projectiles: 16×16 per frame.
- Sprite art faces RIGHT, so visuals are flipped: keep `NPC.direction` as the real direction and set `NPC.spriteDirection = -NPC.direction`. (The Apprentice uses an older inverted-direction style; it only affects visuals.)
- Each texture PNG sits next to its `.cs` file with the same name.
- Multiplayer-safe patterns: spawn projectiles and roll random choices only when `Main.netMode != NetmodeID.MultiplayerClient`; sync with `NPC.ai[]` + `NPC.netUpdate`. Custom NPC hitboxes check `Main.LocalPlayer` on clients.
- Tunable numbers go in `const` fields at the top of each class.
- Known gotchas: `using Terraria.DataStructures;` is needed for `PlayerDeathReason.ByNPC(...)` (capital NPC).
- Lore is Andy's: don't invent or change lore unless asked.

## Status (Reign of the Elves, Pre-Hardmode, after the Evil boss)
- Done: Elven Apprentice, Elf Squire, Elf High-Guard.
- Written, needs in-game testing: Elven Archmage (spread / telegraphed beam / safe teleport), ElvenArcaneBolt.
- Written, needs in-game testing: Elven Greatsword (held heavy swing + right-click magic slash, `MeleeMagicDamageClass`). Sprites are placeholders.
- All `SpawnChance` return 0f until the Mystical Library biome exists.

## Next up
1. Test and tune the Archmage.
2. Test and tune the Elven Greatsword; replace placeholder sprites.
3. Mystical Library: pre-built structure + spawn conditions.
4. Volodar boss (mage-archer) and his signature bow. Aim for a flashy, Calamity-style feel.

## Workflow
- Small, focused commits with clear messages.
