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
| Credit Cannon | Rocket launcher (150 base damage). 100x damage, but only while the actual end credits are rolling. Dropped by the Zombie CEO. |
| Glue Gun | Applies the Glue debuff. Glued things stick to anything they touch. Fire burns it off. |
| Chain Can | Enemies hit spawn Tin Can NPCs next to them; shoot the cans to blow them up. |
| Pong Power | Summon weapon: two big paddles follow your cursor at the top/bottom of the screen. Balls (max 5) grow every bounce, bounce off enemies back to the paddles, and pop after 5 paddle bounces. Miss and it's gone. |
| Green Pong Power | Upgrade: four longer paddles on every screen edge, double ball speed, 10 balls. |
| Rattling Gun | Fires high speed rats that die on impact and poison. |
| Pocket Rats | Magic barrage of rats that burst into poison clouds. |
| Venom Snake | Gun whose bullets all apply Venom. |
| Gparrling Hook | Pulls the hooked NPC to you at high speed over a short range, ignoring gravity, weight, knockback resistance and common sense. |
| Tool Gun / Tool Minigun | Duplicates any NPC the bolt touches, friendly or not (manual collision, no damage). The minigun has a 2 degree spread. |
| NPC Lawnmower | Kills non-hostile NPCs (town folk, critters) on contact. Enemies are unaffected. |

### Ammo
Googly Eye Ammo (googly eyes on hit, with physics), You're Long? (4x wide sprites), You're Tall? (4x high sprites), Buddy System Bullet (Life Link: linked NPCs on
screen share every hit, copied not split), Chaos Solution (post-Moon Lord Clentaminator ammo that converts blocks to random blocks).

### Tools
| Item | What it does |
|---|---|
| Block Rod | Click-drag a rectangle (up to 24x24) to lift it. Left click to place it precisely, right click to put it back. |
| NPC Hand | Click and drag any NPC. |

### Accessories
| Item | What it does |
|---|---|
| Instant Up/Down, Instant Left/Right | Pressing a direction instantly sets velocity on that axis to your normal maximum. |
| Unreasonable Up/Down, Unreasonable Left/Right | Same, but an absurd speed (3x the original absurd speed) that ignores limits, with sub-stepped collision so you don't tunnel. |
| Instant Omni Movement | Both tier 2 items combined at 4x the original absurd speed. Momentum persists after releasing keys. Opposite key reverses. No brakes. |
| Sky in a Bottle | Every double jump becomes a temporary real cloud platform under your feet (shared, one-way, fixed lifetime). |
| Rocket Pack | Dash becomes a GIGA rocket transformation (double speed and distance, swept collision) that explodes on impact. |
| Driller Pack (Base, Hellstone, Chlorophyte, Luminite) | Dash becomes a drill that mines through terrain. |
| Liquid Snake | Full speed through liquids. |
| 3D Glasses | Immune to the 3D debuff. |
| Explosive Healing Kit | Your explosions heal players and friendly NPCs, deal zero damage and break no blocks. |
| TM Flash | Underground/Cavern only: +40% damage, vision limited to 10 tiles (fading to black by 12), your homing projectiles stop homing. |
| Text Boxing Gloves | Every piece of on-screen text (damage numbers, pickup text, the held item label, the chat box, mouse-over text) has a hitbox that damages enemies. |

### Armor
Muncher Mask / Shirt / Shoes: walking into a block at head / chest / foot level eats it using your best pickaxe's power. Set bonus
heals 1 life per block eaten.

### Consumables
Drinkable Air (refills breath any time, 10 s cooldown), Drinkable Lava (kills you), Solid Snake (10 s of phasing, fall damage still applies),
3D Movie Ticket, Weedy Beer (everything gets an upside-down reflection under your feet), Delirium Pills, Pink Slip (night only), Moon Shoes.

### Placeables
| Item | What it does |
|---|---|
| Alchemical Dehydrator | Works like an Extractinator: hold a critter, fish, plant or dye plant and right click it for a random potion ingredient. Gold critters, dye plants and quest fish give 6-31. |
| Rain / Storm / Blizzard / Sunshine / Sandstorm in a Bottle | Global weather override. Last placed wins; breaking the active one returns control to vanilla. |
| Bouncing Jelly Storm in a Bottle | Rain visuals and music. NPCs deal no contact damage; players, NPCs and enemies all bounce off each other, hard. |
| Slippery Frog Rain in a Bottle | Rain visuals and music. Everything is ice, four times as slippery, and Slippery Frogs spawn everywhere. |

### Debuffs
Glue, 3D (red/cyan anaglyph screen effect, toggleable in the config; applied by every zombie), Life Link, Cardboard Box, Winded, Delirious, Weedy.

### Events
* **Delirium** (Delirium Pills): enemies spawn as silhouettes near spawn and swap places with players, at most once every 15 seconds. Progress bar; 100 kills end it.
* **Corporate Zombie Invasion** (Pink Slip, night only, ends at dawn): Zombie Salesmen (your item icons become walking zombies), Zombie
  Interns (sit around doing nothing), and the Zombie CEO boss (tanky, loud, +100% damage per zombie on screen). Progress bar; 100 kills end it.
* **Night of Moon Shoes** (Moon Shoes, or 1-in-12 random hardmode nights): all players and NPCs have half gravity until dawn.

### NPCs
Tin Can, Slippery Frog (critter; slides sideways, teleports up/down a block when blocked), Glue Slime (neutral; no contact damage, glues
everything it touches), Zombie Salesman, Zombie Intern, Zombie CEO.

### Pets
Monst3r (light pet) and Kal'tsit (vanity pet): floating chibi placeholders of the Arknights characters.

## Config
Client side: toggle/intensity for the 3D effect, toggle/radius for TM Flash darkness, Zombie Salesman icon takeover, and whether the
Fart Reverb launches players.

## Multiplayer notes
Everything has at least a best-effort sync (custom packets for NPC moves/kills/spawns, cosmetic flags, clouds, beacons, fart
shockwaves; world data for events and weather). Block Rod edits are made by the client and synced as tile squares. Expect weirdness; it is in the name.
