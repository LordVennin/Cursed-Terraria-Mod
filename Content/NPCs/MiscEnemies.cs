using System;
using BagofChaos.Common;
using BagofChaos.Common.Players;
using BagofChaos.Common.Systems;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace BagofChaos.Content.NPCs
{
	// ---------------------------------------------------------------------------------------------------------------
	// Dual Flintlock Pistols: two pistols flying above you in the daytime desert, taking turns shooting every 5 seconds.
	// ---------------------------------------------------------------------------------------------------------------
	public class DualFlintlockPistols : ModNPC
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.FlintlockPistol;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 1;
		}

		public override void SetDefaults() {
			NPC.width = 40;
			NPC.height = 24;
			NPC.damage = 12;
			NPC.defense = 8;
			NPC.lifeMax = 70;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.value = 90f;
			NPC.knockBackResist = 0.4f;
			NPC.aiStyle = -1;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			return spawnInfo.Player.ZoneDesert && Main.dayTime && NPC.downedBoss1 && spawnInfo.Player.ZoneOverworldHeight ? 0.06f : 0f;
		}

		public override void AI() {
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			Vector2 goal = target.Center + new Vector2(0f, -180f);
			Vector2 toGoal = goal - NPC.Center;
			if (toGoal.Length() > 20f)
				NPC.velocity = (NPC.velocity * 20f + toGoal.SafeNormalize(Vector2.Zero) * 4f) / 21f;
			else
				NPC.velocity *= 0.95f;
			NPC.direction = NPC.spriteDirection = target.Center.X < NPC.Center.X ? -1 : 1;
			NPC.rotation = NPC.velocity.X * 0.03f;

			NPC.ai[0]++;
			if (NPC.ai[0] >= 300 && Main.netMode != NetmodeID.MultiplayerClient && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)) {
				NPC.ai[0] = 0;
				NPC.ai[1] = 1 - NPC.ai[1]; // alternate pistols
				Vector2 muzzle = NPC.Center + new Vector2(NPC.ai[1] == 0 ? -12f : 12f, 0f);
				Vector2 velocity = (target.Center - muzzle).SafeNormalize(Vector2.UnitY) * 9f;
				Projectile.NewProjectile(NPC.GetSource_FromAI(), muzzle, velocity, ProjectileID.BulletDeadeye, 14, 2f, Main.myPlayer);
				SoundEngine.PlaySound(SoundID.Item41, NPC.Center);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D texture = TextureAssets.Npc[Type].Value;
			SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			Vector2 center = NPC.Center - screenPos;
			float bob = (float)Math.Sin(Main.GameUpdateCount * 0.08f) * 3f;
			spriteBatch.Draw(texture, center + new Vector2(-12f, bob), null, NPC.GetAlpha(drawColor), NPC.rotation, texture.Size() * 0.5f, 1f, effects, 0f);
			spriteBatch.Draw(texture, center + new Vector2(12f, -bob), null, NPC.GetAlpha(drawColor), NPC.rotation, texture.Size() * 0.5f, 1f, effects, 0f);
			return false;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ItemID.MusketBall, 1, 20, 50));
			npcLoot.Add(ItemDropRule.Common(ItemID.IronOre, 1, 3, 8));
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert, new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.DualFlintlockPistols")]);
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// <Biome> Pile: a heap of the blocks that make the biome you're in. Doesn't move. Drops fishing loot for that biome.
	// ---------------------------------------------------------------------------------------------------------------
	public class BiomePile : ModNPC
	{
		private static readonly (string name, int tile, int[] loot)[] Biomes = {
			("Forest", TileID.Grass, new int[] { ItemID.Bass, ItemID.Trout, ItemID.Salmon, ItemID.WoodenCrate }),
			("Desert", TileID.Sand, new int[] { ItemID.Flounder, ItemID.RockLobster, ItemID.OasisCrate }),
			("Snow", TileID.SnowBlock, new int[] { ItemID.FrostMinnow, ItemID.AtlanticCod, ItemID.FrozenCrate }),
			("Jungle", TileID.JungleGrass, new int[] { ItemID.DoubleCod, ItemID.NeonTetra, ItemID.VariegatedLardfish, ItemID.JungleFishingCrate }),
			("Corruption", TileID.CorruptGrass, new int[] { ItemID.Ebonkoi, ItemID.CorruptFishingCrate }),
			("Crimson", TileID.CrimsonGrass, new int[] { ItemID.Hemopiranha, ItemID.CrimsonFishingCrate }),
			("Hallow", TileID.HallowedGrass, new int[] { ItemID.PrincessFish, ItemID.Prismite, ItemID.HallowedFishingCrate }),
			("Ocean", TileID.Sand, new int[] { ItemID.RedSnapper, ItemID.Tuna, ItemID.Shrimp, ItemID.OceanCrate }),
			("Cavern", TileID.Stone, new int[] { ItemID.ArmoredCavefish, ItemID.SpecularFish, ItemID.GoldenCarp, ItemID.IronCrate }),
			("Underworld", TileID.Ash, new int[] { ItemID.Obsidifish, ItemID.FlarefinKoi, ItemID.LavaCrate }),
		};

		public int Biome { get => (int)NPC.ai[0]; set => NPC.ai[0] = value; }

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 1;
		}

		public override void SetDefaults() {
			NPC.width = 48;
			NPC.height = 32;
			NPC.damage = 0;
			NPC.defense = 4;
			NPC.lifeMax = 80;
			NPC.HitSound = SoundID.Dig;
			NPC.DeathSound = SoundID.Dig;
			NPC.value = 0f;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1;
			NPC.friendly = false;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

		public override float SpawnChance(NPCSpawnInfo spawnInfo) => NPC.CountNPCS(Type) < 2 ? 0.02f : 0f;

		public override void OnSpawn(IEntitySource source) {
			NPC.TargetClosest(false);
			Player p = Main.player[NPC.target];
			Biome = p.ZoneUnderworldHeight ? 9 : p.ZoneBeach ? 7 : p.ZoneHallow ? 6 : p.ZoneCrimson ? 5 : p.ZoneCorrupt ? 4 : p.ZoneJungle ? 3 : p.ZoneSnow ? 2 : p.ZoneDesert ? 1 : (p.ZoneRockLayerHeight || p.ZoneDirtLayerHeight) ? 8 : 0;
			NPC.GivenName = Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.NPCs.BiomePile.Named", Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.NPCs.BiomePile.Biome" + Biome));
			NPC.netUpdate = true;
		}

		public override void AI() {
			NPC.velocity.X = 0f;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			int tileType = Biomes[Math.Clamp(Biome, 0, Biomes.Length - 1)].tile;
			Main.instance.LoadTiles(tileType);
			Texture2D texture = TextureAssets.Tile[tileType].Value;
			var rand = new Random(NPC.whoAmI * 7 + 3);
			int count = 5 + rand.Next(6);
			for (int i = 0; i < count; i++) {
				float x = (float)(rand.NextDouble() - 0.5) * NPC.width;
				float y = -(float)rand.NextDouble() * (NPC.height - 16f) * (1f - Math.Abs(x) / NPC.width);
				Vector2 pos = NPC.Bottom - screenPos + new Vector2(x - 8f, y - 16f);
				Rectangle frame = new Rectangle(18 * rand.Next(3), 18 * rand.Next(3), 16, 16);
				spriteBatch.Draw(texture, pos, frame, drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			}
			return false;
		}

		public override void OnKill() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int[] loot = Biomes[Math.Clamp(Biome, 0, Biomes.Length - 1)].loot;
			int count = Main.rand.Next(1, 3);
			for (int i = 0; i < count; i++)
				Item.NewItem(NPC.GetSource_Death(), NPC.Hitbox, loot[Main.rand.Next(loot.Length)]);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.BiomePile"));
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// ",.,": three black pixels. Only mortal between 3:21 and 3:24 AM on a full moon. Killing it crashes the game.
	// ---------------------------------------------------------------------------------------------------------------
	public class CommaDotComma : ModNPC
	{
		public override string Texture => "BagofChaos/Assets/Textures/Pixel";

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 1;
			NPCID.Sets.CountsAsCritter[Type] = true;
			NPCID.Sets.TownCritter[Type] = true;
		}

		public override void SetDefaults() {
			NPC.width = 6;
			NPC.height = 4;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 5;
			NPC.friendly = true;
			NPC.aiStyle = -1;
			NPC.knockBackResist = 0f;
			NPC.dontTakeDamage = true;
			NPC.npcSlots = 0.1f;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) => spawnInfo.Player.ZoneOverworldHeight && !NPC.AnyNPCs(Type) ? 0.02f : 0f;

		public static bool IsVulnerable() => !Main.dayTime && Main.moonPhase == 0 && Main.time >= 28260.0 && Main.time <= 28440.0;

		public override void AI() {
			NPC.dontTakeDamage = !IsVulnerable();
			NPC.velocity.X = 0f;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Vector2 pos = NPC.Center - screenPos;
			for (int i = -1; i <= 1; i++)
				spriteBatch.Draw(pixel, new Rectangle((int)pos.X + i * 2 - 1, (int)pos.Y, 1, 1), Color.Black);
			return false;
		}

		public override void OnKill() {
			// You were warned. The item file says it crashes the game. The item file wins.
			throw new Exception(",.,");
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.CommaDotComma"));
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Glue Bat: a cave bat, slightly faster, white, sticky.
	// ---------------------------------------------------------------------------------------------------------------
	public class GlueBat : ModNPC
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.CaveBat;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.CaveBat];
		}

		public override void SetDefaults() {
			NPC.CloneDefaults(NPCID.CaveBat);
			NPC.damage = ContentSamples.NpcsByNetId[NPCID.CaveBat].damage; // exactly a Cave Bat's bite
			NPC.lifeMax = 25;
			AIType = NPCID.CaveBat;
			AnimationType = NPCID.CaveBat;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) => SpawnCondition.Cavern.Chance * 0.08f;

		public override void PostAI() {
			// A little faster than a Cave Bat, but capped so it never runs away with itself.
			if (NPC.velocity.Length() < 7f)
				NPC.velocity *= 1.03f;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
			target.AddBuff(ModContent.BuffType<GlueDebuff>(), 60 * 5);
			// Contact glues you to the bat itself: enjoy the flight.
			target.GetModPlayer<ChaosPlayer>().GlueToNPC(NPC);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D texture = WhitenedTextures.Get(Texture);
			SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 center = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);
			spriteBatch.Draw(texture, center, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, effects, 0f);
			return false;
		}

		public override void OnKill() => ChaosEvents.RegisterGlueKill();

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns, new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.GlueBat")]);
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// More Zombies!: 600 HP, spawns a zombie every time it takes damage. Blood moons only, one at a time.
	// ---------------------------------------------------------------------------------------------------------------
	public class MoreZombies : ModNPC
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Zombie;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Zombie];
		}

		public override void SetDefaults() {
			NPC.width = 24;
			NPC.height = 48;
			NPC.damage = 30;
			NPC.defense = 12;
			NPC.lifeMax = 600;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath2;
			NPC.value = 500f;
			NPC.knockBackResist = 0.2f;
			NPC.aiStyle = NPCAIStyleID.Fighter;
			NPC.scale = 1.4f;
			NPC.color = new Color(255, 120, 120);
			AIType = NPCID.Zombie;
			AnimationType = NPCID.Zombie;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) => Main.bloodMoon && NPC.downedBoss1 && !NPC.AnyNPCs(Type) && spawnInfo.Player.ZoneOverworldHeight ? 0.05f : 0f;

		public override void HitEffect(NPC.HitInfo hit) {
			if (NPC.life > 0 && Main.netMode != NetmodeID.MultiplayerClient && NPC.CountNPCS(NPCID.Zombie) < 30)
				ChaosUtils.SpawnNPCDirect(NPCID.Zombie, NPC.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), -10f));
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ItemID.ZombieArm, 4));
			npcLoot.Add(ItemDropRule.Common(ItemID.Shackle, 5));
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.BloodMoon, new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.MoreZombies")]);
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Sky-fallers: Spinning Worms during rain in the forest, Screaming Flinxes during blizzards in the snow.
	// ---------------------------------------------------------------------------------------------------------------
	// Things that fall out of the weather: spawn above the top of the screen, fall into view, and die the moment they
	// touch a tile (no landing and walking off). Loot drops once, on the server.
	public abstract class SkyFaller : ModNPC
	{
		protected abstract bool WeatherOk(Player player);
		private ref float FallTime => ref NPC.ai[1];
		private bool Dead { get => NPC.ai[2] == 1f; set => NPC.ai[2] = value ? 1f : 0f; }

		public override void SetDefaults() {
			NPC.aiStyle = -1;
			NPC.knockBackResist = 0f;
			NPC.noTileCollide = false;
			NPC.noGravity = true; // gravity is done below, so the fall is always the same
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) => Main.raining && WeatherOk(spawnInfo.Player) && spawnInfo.Player.ZoneOverworldHeight ? 0.12f : 0f;

		public override int SpawnNPC(int tileX, int tileY) {
			// Fall out of the sky instead of walking in from the side: above the top edge of any normal screen.
			Player player = Main.player[Player.FindClosest(new Vector2(tileX * 16f, tileY * 16f), 16, 16)];
			for (int tries = 0; tries < 10; tries++) {
				float x = player.Center.X + Main.rand.NextFloat(-600f, 600f);
				float y = player.Center.Y - 800f;
				if (y > 16f * 5f && !Collision.SolidCollision(new Vector2(x - 16f, y - 16f), 32, 32))
					return NPC.NewNPC(new EntitySource_SpawnNPC(), (int)x, (int)y, Type);
			}
			return Main.maxNPCs; // nowhere open to fall from: no spawn this time
		}

		/// <summary>Call first thing in AI. Returns false once the NPC has hit something and is dying.</summary>
		protected bool UpdateFall() {
			if (Dead)
				return false;
			FallTime++;
			NPC.velocity.Y = System.Math.Min(NPC.velocity.Y + 0.3f, 12f);
			NPC.velocity.X *= 0.99f;
			bool touching = FallTime > 2 && (NPC.collideY || NPC.collideX || NPC.velocity.Y == 0f
				|| Collision.SolidCollision(NPC.position + new Vector2(0f, NPC.velocity.Y + 2f), NPC.width, NPC.height));
			if (touching)
				Splat();
			return !Dead;
		}

		protected void Splat() {
			if (Dead || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			Dead = true;
			NPC.StrikeInstantKill();
		}
	}

	public class SpinningWorm : SkyFaller
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Worm;
		protected override bool WeatherOk(Player player) => player.ZoneForest;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Worm];
		}

		public override void SetDefaults() {
			base.SetDefaults();
			NPC.width = 14;
			NPC.height = 14;
			NPC.damage = 20;
			NPC.lifeMax = 10;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			AnimationType = NPCID.Worm;
		}

		public override void AI() {
			NPC.rotation += 0.5f;
			UpdateFall();
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.Add(ItemDropRule.Common(ItemID.Worm));

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.Rain, new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.SpinningWorm")]);
		}
	}

	public class ScreamingFlinx : SkyFaller
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.SnowFlinx;
		protected override bool WeatherOk(Player player) => player.ZoneSnow;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.SnowFlinx];
		}

		public override void SetDefaults() {
			base.SetDefaults();
			NPC.width = 26;
			NPC.height = 24;
			NPC.damage = 15;
			NPC.lifeMax = 60;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			AnimationType = NPCID.SnowFlinx;
		}

		public override void AI() {
			NPC.rotation = NPC.velocity.Y * 0.05f;
			if (!UpdateFall())
				return;
			if (++NPC.ai[0] % 40 == 0) {
				SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Scream") { Volume = 1f, PitchVariance = 0.3f }, NPC.Center);
				for (int i = 0; i < 12; i++) {
					Dust dust = Dust.NewDustPerfect(NPC.Center, DustID.Snow, Main.rand.NextVector2Circular(6f, 6f), 100, default, 1.5f);
					dust.noGravity = true;
				}
				if (Main.netMode != NetmodeID.Server) {
					Player local = Main.LocalPlayer;
					if (!local.dead && Vector2.Distance(local.Center, NPC.Center) < 220f) {
						local.velocity = (local.Center - NPC.Center).SafeNormalize(-Vector2.UnitY) * 22f; // massive knockback
						local.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), 8, 0);
					}
				}
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) => npcLoot.Add(ItemDropRule.Common(ItemID.FlinxFur));

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Events.Blizzard, new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.ScreamingFlinx")]);
		}
	}
}
