# Reign of the Ancients: project notes for Claude

## Environment
- tModLoader (latest stable, Terraria 1.4.4 API). No `DisplayName.SetDefault`; display names live in `Localization/en-US_Mods.Reign_of_the_Ancients.hjson`.
- Root namespace `Reign_of_the_Ancients`. Folder casing must match namespaces exactly (`NPCs/Elves/`, `Projectiles/Elves/`).
- Build with `dotnet build` from this folder and fix any compile errors before handing back. Andy tests in-game himself.

## Working with Andy
- Always ask me questions when something is unclear, as many as it takes for you to get a clear picture of what we are doing/want to achieve.
- For anything bigger than a small fix, propose a short plan and wait for my OK before writing code.
- Lore is Andy's: don't invent or change lore unless asked. Use clearly marked placeholder names until I choose real ones.
- Small, focused commits with clear messages.

## Conventions
- NPC sprites: 32×48 per frame, 4 frames (32×192 sheet). Projectiles: 16×16 per frame.
- Sprite art faces RIGHT, so visuals are flipped: keep `NPC.direction` as the real direction and set `NPC.spriteDirection = -NPC.direction`. (The Apprentice uses an older inverted-direction style; it only affects visuals.)
- Each texture PNG sits next to its `.cs` file with the same name. Placeholder sprites are fine until Andy draws them.
- Armor: `[AutoloadEquip(EquipType.Head/Body/Legs)]`; textures `Name_Head.png`, `Name_Body.png`, `Name_Legs.png` next to the item `.cs` + item icon `Name.png`. Set bonus via `IsArmorSet` + `UpdateArmorSet`, with the set bonus text in the hjson.
- Damage classes: `MeleeMagicDamageClass` (in `DamageClasses/`) inherits Generic, Melee AND Magic bonuses. Elven Greatsword: swing = Melee, right-click magic slash = MeleeMagic. Volodar's Grimoire = Magic. Volodar's Bow = Ranged.
- Multiplayer-safe patterns: spawn projectiles and roll random choices only when `Main.netMode != NetmodeID.MultiplayerClient`; sync with `NPC.ai[]` + `NPC.netUpdate`. Custom NPC hitboxes check `Main.LocalPlayer` on clients.
- Tunable numbers go in `const` fields at the top of each class.
- Known gotchas: `using Terraria.DataStructures;` is needed for `PlayerDeathReason.ByNPC(...)` (capital NPC).
- Downed flags live in `Common/DownedBossSystem.cs`; use them to gate post-boss content.

## Roadmap (one Reign = enemies + biome/structure + boss + loot)
| Stage | Reign | Boss |
|---|---|---|
| Pre-Hardmode, after Evil boss | Elves | Volodar (written, testing) |
| Pre-Hardmode, after Skeletron | Dwarves | Dwarven Titan |
| Pre-Hardmode | Skrougers | Boulder Drake |
| Early/Mid Hardmode | Murks | Irwhyal |
| Late Hardmode | Giants | Obdgar, the Mountainsplitter |
| Post-Moon Lord | Old Gods, Great Ones | TBD |
- Balance each Reign's enemies, weapons and armor against vanilla gear from the same stage.

## Status: Reign of the Elves
### Enemies
- Done: Elven Apprentice, Elf Squire, Elf High-Guard.
- Written, needs in-game testing: Elven Archmage (spread / telegraphed beam / safe teleport), ElvenArcaneBolt.
- Apprentice and Archmage collide with blocks and only attack with line of sight; their projectiles and the beam stop at walls.

### Mystical Library (written, needs in-game testing)
- World gen in `WorldGeneration/`, debug command `/buildlibrary`. Floors detected by wall type (`MysticalLibrary.FloorOfWall`). Blocks need 130 pick power, walls unbreakable until Hardmode. Vanilla textures as placeholders except the carpet. Furniture placed by vanilla ItemID so styles are always right. Loot chests: 2 Marble Chests in floor-1 reading nooks, 2 Obsidian Chests in floor-2 alcoves (never the vault), placed last where there is free floor (`LootChests`), filled by `WorldGeneration/LibraryLoot` (only in newly generated libraries).
- Floor 1: white/gold cathedral with red carpet, arches, stained glass + rose window, bell towers (Apprentice, Squire).
- Floor 2 (grand staircase): forbidden archive, obsidian/purple, sealed glowing vault (Archmage, High-Guard).
- Floor 3: Volodar throne room, midnight granite + silver, pillars with small balconies, blue banners, stepped dais + throne (no spawns).
- Exterior v3: porches with lean-to roofs, two-stage towers with belfries, hollow beamed attic, string courses, weathered stone, raised marble podium with steps, lamp posts and plants. Domes over the rose-window bay, the vault and the throne. Underground hulls are uneven with rounded corners. A final pass slopes all exposed brick edges (`SmoothEdges`).

### Volodar (written, needs in-game testing)
- `NPCs/Bosses/Volodar.cs`, projectiles in `Projectiles/Bosses/`. 5000 HP, 2 phases (switch at 50%).
- P1: volleys, arrow rain, telegraphed piercing shot, homing arrows, rune arrows. P2 adds split arrows, explosive orbs, sweeping beam, spectral clones, elf guards from the entrance.
- Summoned with the Sealed Tome (12 Elven Essence + Book + 3 Fallen Stars at a demon altar), only in the throne room; he rises from the throne. Players in the throne room are locked in during the fight (`VolodarArenaPlayer`). Elven Essence drops from all library elves (`ElvenEssenceDrops`).
- Music: Boss 1, then Boss 3 in phase 2. Placeholder names and sprites.
- Drops: Volodar's Bow (30 dmg, every 3rd shot a splitting arcane arrow), Volodar's Grimoire (34 dmg magic, exploding orb + shards), Spectral Sigil (Expert accessory: spectral archer when firing ranged), treasure bag (also 80-100 Elven Ore, ~half an armor set). Bow or grimoire 50/50, with a 1/10 chance of both (`Volodar.WeaponDrop`, shared by Normal mode and the bag).
- Collectibles: trophy (1/10), mask (1/7 normal + bag), Master relic, Boss Checklist entry (`Common/ModIntegrations.cs`, progression 3.5), bestiary (Volodar + library biome on elves). Chat lines + bestiary text are placeholders in the hjson for Andy to write.

### Weapons
- Done: Elven Greatsword (held heavy swing + right-click magic slash). Sword + magic slash sprites from SpriteCook; effects are pale blue/ice.

### Accessories (written, needs in-game testing; names placeholder, sprites placeholder)
- Enemy drops (`Common/ElvenAccessoryDrops`): Apprentice's Spellbook (Apprentice 1/25: +20 mana, +4% magic dmg), Squire's Bracer (Squire 1/25: +6% melee speed, +1 def, +50% melee knockback on enemies targeting you), Guardian's Crest (High-Guard 1/20: after 6s unhit, next hit -30%, buff icon while ready).
- Library chest loot (`LibraryLoot`): each chest has a 33% chance of its floor's signature item (floor 1 Archivist's Lantern: light + treasure within 10 tiles sparkles; floor 2 Elven Quiver: +10% ranged dmg, every 3rd ranged shot a homing spectral arrow at 45%, own counter, stacks with Arcane Volley), 15% for one of the three elf-drop accessories, plus Elven Essence 3-6, 2-3 potions, bombs, arrows, gold.
- Logic in `Common/ElvenAccessoryPlayer` (Bracer, Crest, Lantern) and `Common/ElvenRangerPlayer` (Quiver).

### Art (SpriteCook)
- Style: see memory "elf-art-direction". Done: Volodar (menacing redesign), Elf High-Guard, Elven Greatsword, magic slash. NPC sheets are one pose x4 frames with a 1px breathing dip; real animations later (Andy may subscribe).
- Still placeholder: Archmage, Squire, Apprentice, Volodar head icon/mask/trophy/relic, Volodar's weapons + sigil/tome/bag, ore tier, armor, accessories, buffs. High-Guard should be redone in the darker Volodar style.

### Elven ore tier (written, needs in-game testing; all names and sprites are placeholders)
- Balance target: Molten-tier alternative (Molten is reachable before Skeletron). Spellblade = Molten for melee/magic, Ranger = the ranged pick right before WoF (between Necro and Molten).
- **Elven Ore** (`Tiles/Ores/ElvenOreTile`, vanilla Mythril texture): 65 pick power. Veins spawn through the underground + cavern layers the first time Volodar dies (`Common/ElvenOreSpawner`, called from `Volodar.OnKill`), with a chat message (placeholder in hjson `ElvenOreSpawned`).
- **Elven Bar**: 4 ore at a Hellforge. Not placeable.
- **Arcane Crystal** (class `ArcaneCrystal`): Elven Archmage drops 1-2 at 50%.
- **Tools** (`Items/Tools/`, Anvil): Elven Pickaxe (100 power, 15 bars), Elven Waraxe (140% axe, 12 bars), Elven Hammer (70% hammer, 12 bars).
- **Spellblade set** (`Items/Armor/`, Anvil): Helmet 8 def, +8% melee & magic dmg, +40 mana (10 bars, 3 Essence); Breastplate 9 def, +7% melee & magic crit (20 bars, 5 Essence); Greaves 8 def, +8% melee speed, +6% move (15 bars, 4 Essence). 25 def total. Melee + magic bonuses both stack on `MeleeMagicDamageClass`.
  - Set bonus **Spellweave** (`Common/SpellweavePlayer`): 4 pure-melee hits (12-tick cooldown between counted hits) fill a charge; the next magic attack (incl. Greatsword right-click) costs no mana and gets +25% magic damage. Magic hits (incl. the MeleeMagic slash) give Spellweave Haste: +15% melee speed for 4s. Buffs in `Buffs/`.
- **Elven Ranger set** (Anvil): Hood 6 def, +8% ranged dmg; Tunic 7 def, +8% ranged crit, 15% ammo save; Leggings 6 def, +6% ranged dmg, +10% move. 19 def total. Each piece: 4 Arcane Crystal + bars (10/20/15) + Essence (3/5/4).
  - Set bonus **Arcane Volley** (`Common/ElvenRangerPlayer`): every 6th ranged shot also fires 2 homing `ElvenSpectralArrow`s at 50% of the shot's damage.
- Placeholder sprites generated with System.Drawing; the `_Body` sheets only fill the torso frame (arms left empty) until Andy draws them.
- **Weapons** (`Items/Weapons/`, Anvil, bars + Essence; bow/sceptre also Crystal):
  - Elven Shortsword 24 dmg, fast stabs; every 4th stab that hits fires a piercing light-dash (60%).
  - Elven Broadsword 32 dmg, each swing releases a short crescent (50%, `ElvenCrescent`, reuses the magic slash art).
  - Elven Bow 26 dmg, wooden arrows become `ElvenLightStreak` (fast, pierces 3).
  - Elven Sceptre 26 dmg, 12 mana (`ElvenSceptreBeam`): 0.5s aim line -> BOOM (x1.5 burst hits) -> hold left click up to 1.5s (3 mana / 10 ticks) -> forced stop; 1.5s cooldown (`CooldownTicks`) before the next cast.
  - Elven Whip 26 dmg: Arcane Mark tag (`Buffs/ElvenWhipTag`) +6 tag dmg, minion hits 1/4 burst into 3 summon `ArcaneShard`s (40%).

### Polish
- Banners for all 4 elves (`Tiles/Banners/ElvenBanner` ModBannerTile + `Items/Placeable/Banners/`), 50 kills.
- Master pet: Tiny Sealed Tome (`Items/Pets`, `Projectiles/Pets`, buff), 1/4 in Master like vanilla; listed in Boss Checklist collectibles.
- Library ambience (`Common/LibraryAmbiencePlayer`): pale motes + sapphire sparks in front of library walls. Music: vanilla Space as a placeholder (`MysticalLibraryBiome.Music`).

## Design: Reign of the Dwarves (interviewed, not started; all names placeholders)
- **Lore (Andy's words):** "Once the elves' allies, the two factions turned on each other, leading to both of their falls. All that remains are brainless, corrupted grunts and an ancient Dwarven Titan machinery."
- **Theme:** fallen/cursed dwarves; engineering + forge & fire, steampunk attacks, ground slams with shockwaves, hot steam. Palette idea: brass/rust/orange vs the elves' silver/blue.
- **Biome:** deep underground hold, lower caverns toward the Underworld, bigger and more sprawling than the Library (mine tunnels, rails). Exists from world gen; its great gate is sealed (unbreakable) until Skeletron is dead.
- **Grunts:** Sapper (lobs bombs/kegs), Blunderbuss gunner (steampunk shotgun spread), Miner (tunnels through blocks toward you, pickaxe swings), Rusted mech pilot (old mech, shoots bursts at range, charges up close; rarer mini-elite).
- **Dwarven Titan:** self-running ancient war machine, nobody inside (no dialogue: gears, steam, klaxons). Bulky, slow, heavy-hitting, moves on treads (ground-bound, never jumps).
  - Breakable parts: arm cannons (destroying one removes its attacks) and armor plating (falls off, exposes a weak core).
  - Attacks: cannon shells + ground-slam shockwaves, steam vents + flamethrower sweep, tread ram charge + homing steam rockets.
  - Woken by a lever/machine in its chamber (re-usable, no summon item). Arena: foundry hangar with lava channels.
- **Loot:** Titan weapons (to discuss later), grunt accessories, hold chests with signature items. No new ore tier for this Reign.

### Status: Reign of the Dwarves
- **Dwarven Hold (written, needs in-game testing):** `WorldGeneration/DwarvenHoldBuilder` + `DwarvenHoldSystem`, debug `/buildhold` (stand deep in the caverns; builds the gate just in front of you) and `/buildhold open`. Architecture follows Andy's reference screenshots (red brick, glowing molten seams in the hull, stepped ceilings).
  - Built as one carved grid wrapped in a 4-tile lining; zones by wall type (`DwarvenHold.ZoneOfWall`): 1 gatehouse + great hall, 2 forges, 3 mines, 4 Titan foundry.
  - Grey gatehouse with the sealed `HoldGate` -> great hall (V gables, dark pillars, tall framed windows, potted lava plants, lanterns) -> shaft -> forges (bays with ceilings stepping up; centre crucible pouring between two fire-window furnace banks, small furnaces with smokestacks, water tanks, hanging ore buckets, cog panel, steam pipes) -> shaft -> mine rail tunnel (track, timber, side tunnels to mining pockets) -> foundry (lava aqueduct, molten falls, far lava channel, `TitanControlMachine` placeholder that only shows a message).
  - All hold blocks/walls unbreakable until Skeletron (`NPC.downedBoss3`); then 100 pick power. The gate is removed by `DwarvenHoldSystem.PostUpdateWorld` the first time Skeletron is dead (message `HoldGateOpened`, placeholder). Lava moss around the approach cave. Music placeholder: Underground Desert.
- **Grunts (written, needs in-game testing; placeholder names/sprites):** `NPCs/Dwarves/` on `DwarfGruntBase`; spawns only in the hold (`Common/DwarvenHoldSpawns`), none in the foundry.
  - Corrupted Sapper (hall/forges/mines): keeps 7-16 tiles away, lobs arcing `DwarvenBomb`s; below 30% HP lights its keg and runs at you to explode.
  - Corrupted Gunner (hall/forges): aims 0.5s (muzzle smoke), blunderbuss spread of 5 `DwarvenPellet`s, recoil.
  - Corrupted Miner (mines): digs through blocks toward you (never hold/library/dungeon/temple blocks, furniture or hard ores), pickaxe lunge.
  - Rusted Mech Pilot (forges/mines, rare mini-elite, 320 HP): 3-shot bursts at range; up close steam windup -> charge (45 dmg) -> stunned.
- Not yet: banners, drops/accessories, hold chests, Dwarven Titan, Titan weapons.

## Andy to test in-game
- Archmage, Mystical Library layout, Volodar fight, his drops and collectibles.
- Accessories + Library chests (new world or `/buildlibrary`), new sprites (Greatsword grip position in the swing).
- Elven ore tier: ore spawn on first Volodar kill (also in multiplayer), mining/smelting, tools, both armor sets and their set bonuses.
- Ore weapons (esp. sceptre beam feel and whip vs Spinal Tap/Firecracker), banners, Tome pet, Library ambience + music.
- Dwarven Hold (`/buildhold`, gate opening after Skeletron, new world gen placement) and the four dwarf grunts.

## Later
- Mystical Library: custom sprites.
- Elven ore tier: real names, sprites (ore tile, items, armor sheets), maybe placeable bars and ore-tier weapons.
