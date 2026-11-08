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

## Update 3 (the big one)

### Patches
* Alchemical Dehydrator: hold right click to mass-process. Slippery Frog uses the vanilla frog sprite; Frog Rain slipperiness only on blocks, no slowdown, double run speed on blocks.
* Glue lasts 5 seconds, glued things get a white outline, coming unglued removes the debuff, and glued enemies also get Life Link (8 s, 25% damage reduction).
* Delirium is now the **Delirium Moon**: 1-in-3 new moon nights, every enemy is a silhouette, one swaps places with you every 10-15 seconds.
* Corporate Zombie Invasion uses the vanilla (goblin) progress bar and needs 50 kills; the corporate zombies wear vanilla zombie sprites.
* Sky in a Bottle makes its cloud when a jump peaks instead of on a double jump. Fart Reverb uses the vanilla fart sound and barely moves you.
* Weedy Beer lasts 10 minutes and stacks up to five tiers. Glue Slime is a white vanilla slime.
* Weather beacons are tinted Crystal Balls, toggle with right click (last switched-on wins), and cost glass, a Life Crystal, clouds and a weather block.

### New items
Chaotic Dehydrator, Bottled Prehardmode / Hardmode (+ Biome'd), the Rubber Banding Bandit armor set (four class helmets, suit, shoes, bouncy set bonus),
Tanning Mirror, Woops! All Arrows!, Arrow of Replication, Ball of Spiders / Balls of Spiders (wings) / Spider on a Ball (pet) via shimmer,
the three Triforce pieces and the Complete Triforce, Succ., Doot Doot, Stalemate, Loot Scanner, Snail Flail / Hot Snail Flail, Ball of Confetti / Balls
of Confetti / Ball of Glue, five remote NPC tools and the NPC Directory, "Harpy" Wings, Rat Tacks, Axe Me a Question / Lots of Questions, Rat Cart,
Fart Reverb.mp4, Touch Grass, Slippery Frog Shoes, Nail and Hammer, Stacks of Fish, Sharkboard, Chained Chainsaw, Hollow Block, Blue Core Block,
Crushed Glass / Crushed Dirt potions, N-Terraria, Pull the Lever Kronk!, Gentlemen Stack., Chaotic Quiver / Ammo Box / Ammunition Roulette, the four
Chaotic weapon boxes, Chaotic Dash Box, Chaotic Banker, Bag of Chaos, Unstable Concoction, Team Tether, Slotter Pillar, Slotting Pillar, Rat Package Co.,
Model Demon Altar, Gravity Flail, the three Compressed crystals, BBQ Skewer.

### New NPCs and bosses
Gremlot, Rat Merchant, Chaotic Beholder, Shlue (town pet), Doppleganger, four hopping buckets, Dual Flintlock Pistols, Biome Piles, ",.,", Glue Digger,
Glue Bat, More Zombies!, Spinning Worm, Screaming Flinx; bosses: Bonemass of Skeletron (1-in-4 Skeletron replacement, or Bone Pile at a demon altar),
Slot-bar Caterpillar (Slot Machine Token), Chaotic Slime (Chaotic Gel). Expert treasure bags where it makes sense.

### Notes
* Wherever a vanilla sprite fit, the mod borrows it (`Texture` overrides). Everything else is still placeholder art from `tools/gen_assets.py`.
* ",.," really does crash the game when killed, as requested. It can only be killed between 3:21 and 3:24 AM on a full moon.
* The Screaming Flinx uses a synthesized scream; a certain cartoon cat's scream is copyrighted.

## Update 4 (tester feedback)

### Load errors and crashes fixed
* Missing `_Head_Boss` / `_Highlight` textures (Slot-bar Caterpillar, Chaotic Slime, Zombie CEO, both dehydrators, weather beacons).
* All three mounts would have crashed the client right after that (no `_Back` texture); "Harpy" Wings is now a wing accessory instead.
* Compressed Life Crystal / Mana Crystal / Life Fruit froze the game (an endless loop). They now set the vanilla progression counters
  (400 life, 200 mana, 500 life with golden hearts), never lower anything, and after that are free potions forever.

### Reworked
* **Pong Power** paddles are horizontal; **Green Pong Power** has two on top and two on the bottom. **Balls of Confetti** fly straight through enemies.
* **Glue Bat** glues you to itself, Cave Bat damage. **Spider on a Ball** is a ground pet on the Clown's Happy Bomb, spinning much faster.
  **Balls of Spiders** leave a fading trail of little spiders.
* **Succ.** gives one full-strength shove per click toward you (stacks with momentum, no cap, no teleport).
* **Rubber Banding Bandit**: 5 tile bounces at -10% damage each, snap-homing after an enemy hit (a wall hit switches it off).
  Warpaint set bonus: press the "projectile magnet" key (default V) to pull in every projectile within 6 tiles for 5 s and fire them back
  as your own (45 s cooldown).
* **Tanning Mirror** turns minions into pinballs inside the cone, with a bumper floor above your head.
* **Triforces** really convert damage classes now (weapon, ammo and projectiles), show the class in the item name, and only the
  pieces cost 10%. The Complete Triforce cycles Melee / Ranged / Magic / Summoner and lights the matching segment.
* **Snail Flails** work in multiplayer, can't hit snails, and Hot Snail Flail is now Magma Snail Flail.
* **NPC tools** work with the NPC dead or absent (an invisible stand-in, never saved). Shopkeeper's (Merchant, at the beach, 10 gold),
  Goblin' Your Money (after a reforge, 50 gold), Snipping Tool (after a haircut, 20 gold), Angler's Pocketguide (every 5th quest).
  NPC Directory = all five at a Tinkerer's Workshop and lists everyone who ever moved in.
* **Rat Tacks**: vanilla mice as stationary traps, 3 bites or 30 s each, 20 max (oldest leave first), mode switch updates every rat.
* **Axe Me a Question / Lots of Questions** fell any tree they touch. **Rat Cart** goes in the minecart slot. **Sharkboard** swims.
* **Fart Reverb.mp4**: 50% over 14 s, then 100% at exactly 15 s; hurts other players too. **Touch Grass** stops while airborne.
* **Nagel und Hammer** (hammer + rapid nails, 15-stack combo) and its upgrade **Geschmolzener Nagel und Hammer** (molten boomerang hammer).
* **Stacks of Fish** is a tower of goldfish that shoot goldfish. **Hollow Block** places in mid-air, 1 Forbidden Fragment = 500.
* **Crushed Dirt** really lights dirt like a torch; **Crushed Glass** hides anything made from glass or sand (cached from recipes). Both 10 min.
* **Chaotic Boxes**: hold to use, the weapon in hand changes every half second. **Slotter Pillar / Slotting Pillar** no longer need the inventory open.
* **Rat Package Co.** row: nine light-brown slots beside the trash can (under the chest when one is open). A tenth-slot item is given back.
* **Slot-bar Caterpillar** copies your hotbar as a picture (no more item stealing), half defense, and uses one of your weapons every 15 s.
* **Shlue** is two vanilla glue slimes with googly eyes on top, and has a lot more to say. **Spinning Worm / Screaming Flinx** actually fall now.

### New
**Wob** (a bow, backwards), **Slam Dunker** (basketball hoop sentry; right click near a ball to dunk it for a homing swarm),
**Ocean in a Fishbowl** (air is water: swim everywhere, drown without gills).

## Config
Client side: toggle/intensity for the 3D effect, toggle/radius for TM Flash darkness, Zombie Salesman icon takeover, and whether the
Fart Reverb launches players.

## Multiplayer notes
Everything has at least a best-effort sync (custom packets for NPC moves/kills/spawns, cosmetic flags, clouds, beacons, fart
shockwaves; world data for events and weather). Block Rod edits are made by the client and synced as tile squares. Expect weirdness; it is in the name.
