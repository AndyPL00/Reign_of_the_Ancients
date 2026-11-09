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
- Always ask me questions when something is unclear,as many as it takes for you to get a clear picture of what we are doing/want to achieve.

## Status (Reign of the Elves, Pre-Hardmode, after the Evil boss)
- Done: Elven Apprentice, Elf Squire, Elf High-Guard.
- Written, needs in-game testing: Elven Archmage (spread / telegraphed beam / safe teleport), ElvenArcaneBolt.
- Done: Elven Greatsword (held heavy swing + right-click magic slash, `MeleeMagicDamageClass`). Sprites are placeholders.
- Written, needs in-game testing (v2, beautified): Mystical Library (world gen in `WorldGeneration/`, debug `/buildlibrary`). Floor 1 white/gold cathedral with red carpet, arches, stained glass + rose window, bell towers (Apprentice, Squire). Grand staircase to floor 2 forbidden archive: obsidian/purple, sealed glowing vault (Archmage, High-Guard). Staircase to floor 3 Volodar throne room: midnight granite + silver, pillars with small balconies, blue banners, stepped dais + throne (no spawns). Floors detected by wall type (`MysticalLibrary.FloorOfWall`). Blocks need 130 pick power, walls unbreakable until Hardmode. Vanilla textures as placeholders except the carpet. Furniture placed by vanilla ItemID so styles are always right. No loot yet.
- Library exterior v3: porches with lean-to roofs, two-stage towers with belfries, hollow beamed attic, string courses, weathered stone, raised marble podium with steps, lamp posts and plants. Domes over the rose-window bay, the vault and the throne. Underground hulls are uneven with rounded corners. A final pass slopes all exposed brick edges (`SmoothEdges`).
- Written, needs in-game testing: Volodar boss step 1 (`NPCs/Bosses/Volodar.cs`, projectiles in `Projectiles/Bosses/`). 5000 HP, 2 phases (switch at 50%). P1: volleys, arrow rain, telegraphed piercing shot, homing arrows, rune arrows. P2 adds split arrows, explosive orbs, sweeping beam, spectral clones, elf guards from the entrance. Summoned with the Sealed Tome (12 Elven Essence + Book + 3 Fallen Stars at a demon altar), only in the throne room; he rises from the throne. Players in the throne room are locked in during the fight (`VolodarArenaPlayer`). Elven Essence drops from all library elves (`ElvenEssenceDrops`). Placeholder names and sprites. Music: Boss 1, then Boss 3 in phase 2.
- Apprentice and Archmage collide with blocks and only attack with line of sight; their projectiles and the beam stop at walls.

## Next up
1. Test and tune the Archmage.
2. Test and tune the Mystical Library layout; later: custom sprites, loot chests.
3. Replace Elven Greatsword placeholder sprites.
4. Volodar step 2: Volodar's Bow (fires arrows, every few shots a flashy magic arrow), treasure bag, Expert accessory.
5. Volodar step 3: trophy, mask, relic, Boss Checklist, bestiary, spawn/defeat chat lines (Andy writes the text).

## Workflow
- Small, focused commits with clear messages.
