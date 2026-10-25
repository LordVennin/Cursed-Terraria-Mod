# Bag of Chaos (Cursed Terraria Mod)

A tModLoader (1.4.4, v2026.08 series) mod that is not meant to be taken seriously. Everything here was built from two loose
text files of ideas; wherever the notes left a blank, the funniest option won.

Internal mod name: `BagofChaos`. Display name: **Bag of Chaos**.

## Building

1. Put this folder in your tModLoader `ModSources` directory (or clone it there).
2. Build from the in-game Mod Sources menu, or run `dotnet tModLoader.dll -server -build <path to this folder>` from the tModLoader install folder.

All sprites and sounds are generated placeholder art. `tools/gen_assets.py` (needs Pillow) regenerates every PNG/WAV; replace any
file with real art whenever you like, only the file names and (for tiles, armor and animated sprites) the dimensions matter.

## What's in the bag

### Weapons
| Item | What it does |
|---|---|
| Fart Reverb.mp3 | 1 damage, knockback measured in screens. Launches NPCs, other players, and you. Post-Skeletron recipe needs a Whoopie Cushion. |
| Sprite Slasher | Sword that visually cuts an enemy's sprite in half and swaps the halves. Permanent, one cut per enemy. |
| Credit Cannon | Rocket launcher. 100x damage, but only while the actual end credits are rolling. Dropped by the Zombie CEO. |
| Glue Gun | Applies the Glue debuff. Glued things stick to anything they touch. Fire burns it off. |
| Chain Can | Enemies hit spawn Tin Can NPCs next to them; shoot the cans to blow them up. |
| Pong Power | Summon weapon: paddles follow your cursor at the top/bottom of the screen, the ball grows every bounce. Miss it and it's gone. |
| Rattling Gun | Fires high speed rats that die on impact and poison. |
| Pocket Rats | Magic barrage of rats that burst into poison clouds. |
| Venom Snake | Gun whose bullets all apply Venom. |
| Gparrling Hook | Pulls the hooked NPC to you, ignoring gravity, weight, knockback resistance and common sense. |
| Tool Gun / Tool Minigun | Duplicates any NPC hit. Any. |
| NPC Lawnmower | Kills non-hostile NPCs (town folk, critters) on contact. Enemies are unaffected. |

### Ammo
Googly Eye Ammo (googly eyes on hit, with physics), You're Long? (4x wide sprites), Buddy System Bullet (Life Link: linked NPCs on
screen share every hit, copied not split), Chaos Solution (post-Moon Lord Clentaminator ammo that converts blocks to random blocks).

### Tools
| Item | What it does |
|---|---|
| Block Rod | Click-drag a rectangle (up to 24x24) to lift it. Left click to place precisely, right click to throw it as a rigid chunk with gravity. |
| NPC Hand | Click and drag any NPC. |

### Accessories
| Item | What it does |
|---|---|
| Instant Up/Down, Instant Left/Right | Pressing a direction instantly sets velocity on that axis to your normal maximum. |
| Unreasonable Up/Down, Unreasonable Left/Right | Same, but an absurd speed that ignores limits (with sub-stepped collision so you don't tunnel). |
| Instant Omni Movement | Both tier 2 items combined. Momentum persists after releasing keys. Opposite key reverses. No brakes. |
| Sky in a Bottle | Every double jump becomes a temporary real cloud platform under your feet (shared, one-way, fixed lifetime). |
| Bottle of Zig / Zag / Zigzag | Double jump dashes you forward-up / forward-down / alternating. |
| Rocket Pack | Dash becomes a GIGA rocket transformation that explodes on impact. |
| Driller Pack (Base, Hellstone, Chlorophyte, Luminite) | Dash becomes a drill that mines through terrain. |
| Liquid Snake | Full speed through liquids. |
| 3D Glasses | Immune to the 3D debuff. |
| Explosive Healing Kit | Your explosions heal players and friendly NPCs, deal zero damage and break no blocks. |
| TM Flash | Underground/Cavern only: +40% damage, vision limited to ~20 tiles, your homing projectiles stop homing. |
| Text Boxing Gloves | Every piece of on-screen text (damage numbers, pickup text) has a hitbox that damages enemies. |

### Armor
Muncher Mask / Shirt / Shoes: walking into a block at head / chest / foot level eats it using your best pickaxe's power. Set bonus
heals 1 life per block eaten.

### Consumables
Drinkable Air (refills breath, 10 s cooldown), Drinkable Lava (kills you), Solid Snake (10 s of phasing, fall damage still applies),
3D Movie Ticket, Dehydrated Mystery Powder (right click for a random potion ingredient), Delirium Pills, Pink Slip, Moon Shoes.

### Placeables
| Item | What it does |
|---|---|
| Alchemical Dehydrator | Crafting station. Critters, fish, plants and dye plants become Dehydrated Mystery Powder. Gold critters, dye plants and quest fish give 6-31 pouches. |
| Null Gravity Beacon | Removes gravity from players, NPCs, items and projectiles in range. Liquids are untouched. |
| Rain / Storm / Blizzard / Sunshine / Sandstorm in a Bottle | Global weather override. Last placed wins; breaking the active one returns control to vanilla. |
| Bouncing Jelly Storm in a Bottle | NPCs deal no contact damage and bounce off each other. |
| Slippery Frog Rain in a Bottle | Everything is ice, twice as slippery, and Slippery Frogs spawn everywhere. |

### Debuffs
Glue, 3D (red/cyan anaglyph screen effect, toggleable in the config), Life Link, Cardboard Box, Winded, Delirious.

### Events
* **Delirium** (Delirium Pills): enemies spawn as silhouettes near spawn and swap places with players, at most once every 15 seconds.
* **Corporate Zombie Invasion** (Pink Slip): Zombie Salesmen (your item icons become walking zombies, hits apply 3D), Zombie
  Interns (sit around doing nothing), and the Zombie CEO boss (tanky, loud, +100% damage per zombie on screen).
* **Night of Moon Shoes** (Moon Shoes, or 1-in-12 random hardmode nights): all NPCs have half gravity until dawn.

### NPCs
Tin Can, Slippery Frog (critter; slides sideways, teleports up/down a block when blocked), Zombie Salesman, Zombie Intern, Zombie CEO.

## Config
Client side: toggle/intensity for the 3D effect, toggle/radius for TM Flash darkness, Zombie Salesman icon takeover, and whether the
Fart Reverb launches players.

## Multiplayer notes
Everything has at least a best-effort sync (custom packets for NPC moves/kills/spawns, cosmetic flags, clouds, beacons, fart
shockwaves; world data for events and weather). Chunks thrown by the Block Rod are placed by the thrower's client and synced as tile
squares. Expect weirdness; it is in the name.
