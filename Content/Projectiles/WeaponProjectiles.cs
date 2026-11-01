using System;
using System.Collections.Generic;
using BagofChaos.Common.GlobalNPCs;
using BagofChaos.Common.GlobalProjectiles;
using BagofChaos.Common.Players;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// ---- Nail and Hammer ----
	public class ThrownNail : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.NailFriendly;

		public override void SetDefaults() {
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.1f;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(ModContent.BuffType<Nails>(), 60 * 15);
		}
	}

	// ---- Snail Flail (vanilla flail AI, snail-based loot replacement on kill) ----
	public class SnailFlailProjectile : ModProjectile
	{
		private bool Hot => Projectile.ai[1] == 1f;
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallOHurt;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 3;
		}

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.BallOHurt);
			Projectile.width = 22;
			Projectile.height = 22;
			AIType = ProjectileID.BallOHurt;
		}

		public override bool PreDraw(ref Color lightColor) {
			// Vanilla draws the chain only for its own flail types; draw it ourselves.
			Player owner = Main.player[Projectile.owner];
			Texture2D chain = TextureAssets.Chain.Value;
			Vector2 from = owner.MountedCenter;
			Vector2 to = Projectile.Center;
			Vector2 dir = to - from;
			float length = dir.Length();
			if (length > 1f) {
				dir /= length;
				float rotation = dir.ToRotation() + MathHelper.PiOver2;
				for (float d = 0f; d < length; d += chain.Height) {
					Vector2 pos = from + dir * d;
					Main.EntitySpriteDraw(chain, pos - Main.screenPosition, null, Lighting.GetColor(pos.ToTileCoordinates()), rotation, chain.Size() * 0.5f, 1f, SpriteEffects.None, 0);
				}
			}
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Hot ? lightColor.MultiplyRGB(Color.OrangeRed) : lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			// Mark the target so a killing blow replaces its loot with snails.
			target.GetGlobalNPC<ChaosGlobalNPC>().SnailMark = Hot ? 2 : 1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Hot)
				target.AddBuff(BuffID.OnFire, 180);
		}
	}

	// ---- Balls of Confetti ----
	public class ConfettiDisc : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.LightDisc;

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.LightDisc);
			AIType = ProjectileID.LightDisc;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 1; // ignores i-frames
			Projectile.ArmorPenetration = 10000; // ignores defense
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.SetMaxDamage(1);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			for (int i = 0; i < 4; i++)
				Dust.NewDust(target.position, target.width, target.height, Main.rand.Next(139, 143));
		}

		public override Color? GetAlpha(Color lightColor) => Main.DiscoColor;
	}

	// ---- Throwable axes (boomerang AI) ----
	public class ThrownAxe : ModProjectile
	{
		private bool Hardmode => Projectile.ai[1] == 1f;
		public override string Texture => "Terraria/Images/Item_" + ItemID.CopperAxe;

		public override void SetDefaults() {
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.aiStyle = ProjAIStyleID.Boomerang;
			AIType = ProjectileID.WoodenBoomerang;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D texture = Hardmode ? TextureAssets.Item[ItemID.AcornAxe].Value : TextureAssets.Item[ItemID.CopperAxe].Value;
			Main.instance.LoadItem(Hardmode ? ItemID.AcornAxe : ItemID.CopperAxe);
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			return false;
		}

		public override void AI() {
			Projectile.rotation += 0.4f * Projectile.direction;
			if (Projectile.owner != Main.myPlayer)
				return;
			// Chop trees we pass through.
			Rectangle box = Projectile.Hitbox;
			for (int x = box.Left / 16; x <= box.Right / 16; x++) {
				for (int y = box.Top / 16; y <= box.Bottom / 16; y++) {
					if (!WorldGen.InWorld(x, y, 5))
						continue;
					Tile tile = Main.tile[x, y];
					if (!tile.HasTile || !TileID.Sets.IsATreeTrunk[tile.TileType])
						continue;
					if (Hardmode || Main.rand.NextBool(8)) {
						WorldGen.KillTile(x, y);
						if (Main.netMode == NetmodeID.MultiplayerClient)
							NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: x, number3: y);
						SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
					}
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Main.rand.NextFloat() < (Hardmode ? 0.5f : 0.2f))
				target.AddBuff(BuffID.Confused, 180);
		}
	}

	// ---- Rat Tacks: lie on the ground for 40 seconds, bite whatever steps on them ----
	public class RatTack : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/RatBullet";

		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 60 * 40;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void OnSpawn(IEntitySource source) {
			Projectile.DamageType = Main.player[Projectile.owner].GetModPlayer<ChaosPlayer>().RatTacksSummon ? DamageClass.Summon : DamageClass.Melee;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.3f;
			Projectile.velocity.X *= 0.98f;
			Projectile.spriteDirection = Projectile.velocity.X < 0 ? -1 : 1;
			if (Projectile.velocity.Y == 0f && Main.rand.NextBool(90))
				Projectile.velocity.X = Main.rand.NextFloat(-1.5f, 1.5f); // fidget
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (oldVelocity.Y > 1f)
				Projectile.velocity.Y = 0f;
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Squeak") { Volume = 0.5f, PitchVariance = 0.4f }, Projectile.Center);
		}
	}

	// ---- Chained Chainsaw yoyo ----
	public class ChainsawYoyo : ModProjectile
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.CobaltChainsaw;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = 6f;
			ProjectileID.Sets.YoyosMaximumRange[Type] = 280f;
			ProjectileID.Sets.YoyosTopSpeed[Type] = 13f;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.aiStyle = ProjAIStyleID.Yoyo;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.DamageType = DamageClass.MeleeNoSpeed;
			Projectile.scale = 0.8f;
		}

		public override void PostAI() {
			Projectile.rotation += 0.6f;
			if (Main.rand.NextBool(3))
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cobalt);
		}

		public override bool PreDraw(ref Color lightColor) {
			// Vanilla draws a string for yoyos; we draw a chain instead and skip the string.
			Player owner = Main.player[Projectile.owner];
			Texture2D chain = TextureAssets.Chain.Value;
			Vector2 from = owner.MountedCenter;
			Vector2 dir = Projectile.Center - from;
			float length = dir.Length();
			if (length > 1f) {
				dir /= length;
				float rotation = dir.ToRotation() + MathHelper.PiOver2;
				for (float d = 0f; d < length; d += chain.Height)
					Main.EntitySpriteDraw(chain, from + dir * d - Main.screenPosition, null, Lighting.GetColor((from + dir * d).ToTileCoordinates()), rotation, chain.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			}
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}

		public override bool PreDrawExtras() => false; // no yoyo string
	}

	// ---- Gravity Flail ----
	public class GravityFlailProjectile : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.TheDaoofPow;

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.TheDaoofPow);
			AIType = ProjectileID.TheDaoofPow;
		}

		public override bool PreDrawExtras() {
			Player owner = Main.player[Projectile.owner];
			Texture2D chain = TextureAssets.Chain.Value;
			Vector2 from = owner.MountedCenter;
			Vector2 dir = Projectile.Center - from;
			float length = dir.Length();
			if (length > 1f) {
				dir /= length;
				float rotation = dir.ToRotation() + MathHelper.PiOver2;
				for (float d = 0f; d < length; d += chain.Height)
					Main.EntitySpriteDraw(chain, from + dir * d - Main.screenPosition, null, Lighting.GetColor((from + dir * d).ToTileCoordinates()), rotation, chain.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ChaosGlobalNPC g = target.GetGlobalNPC<ChaosGlobalNPC>();
			g.SlamStartY = target.position.Y;
			g.SlamOwner = Projectile.owner;
			g.Slamming = true;
			target.velocity = new Vector2(0f, 30f);
			target.netUpdate = true;
		}
	}

	// ---- BBQ Skewer ----
	public class SkewerProjectile : ModProjectile
	{
		private readonly List<int> skewered = new();
		private int cookTimer;

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		private float Length => 80f + skewered.Count * 24f + Main.player[Projectile.owner].GetModPlayer<ChaosPlayer>().SkewerBonusLength;

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (!owner.channel || owner.dead || owner.HeldItem.type != ModContent.ItemType<Items.Weapons.BBQSkewer>()) {
				Projectile.Kill();
				return;
			}
			Projectile.timeLeft = 2;
			owner.heldProj = Projectile.whoAmI;
			owner.itemTime = owner.itemAnimation = 2;
			if (Projectile.owner == Main.myPlayer) {
				Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX);
				if (aim != Projectile.velocity) {
					Projectile.velocity = aim;
					Projectile.netUpdate = true;
				}
			}
			owner.ChangeDir(Projectile.velocity.X < 0 ? -1 : 1);
			owner.itemRotation = (Projectile.velocity * owner.direction).ToRotation();
			Projectile.Center = owner.MountedCenter + Projectile.velocity * Length;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

			// Enemies on the skewer stay on the skewer and cook.
			cookTimer++;
			for (int i = skewered.Count - 1; i >= 0; i--) {
				NPC npc = Main.npc[skewered[i]];
				if (!npc.active || npc.life <= 0) {
					skewered.RemoveAt(i);
					continue;
				}
				npc.Center = owner.MountedCenter + Projectile.velocity * (60f + i * 24f);
				npc.velocity = Vector2.Zero;
				if (cookTimer % 30 == 0 && Projectile.owner == Main.myPlayer)
					npc.SimpleStrikeNPC(Projectile.damage / 2, owner.direction, false, 0f, DamageClass.Melee, true);
				if (Main.rand.NextBool(6))
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.Torch);
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			Player owner = Main.player[Projectile.owner];
			float unused = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), owner.MountedCenter, owner.MountedCenter + Projectile.velocity * Length, 12f, ref unused);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (target.boss || skewered.Contains(target.whoAmI) || skewered.Count >= 8)
				return;
			skewered.Add(target.whoAmI);
			SoundEngine.PlaySound(SoundID.Item1, target.Center);
		}

		public override void OnKill(int timeLeft) {
			foreach (int index in skewered) {
				if (Main.npc[index].active)
					Main.npc[index].velocity = Projectile.velocity * 6f; // flung off the skewer
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Player owner = Main.player[Projectile.owner];
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 from = owner.MountedCenter;
			Vector2 dir = Projectile.velocity;
			float rotation = dir.ToRotation();
			// Stretch the skewer sprite along its length.
			float length = Length;
			Main.EntitySpriteDraw(texture, from - Main.screenPosition, null, lightColor, rotation, new Vector2(0f, texture.Height / 2f), new Vector2(length / texture.Width, 1f), SpriteEffects.None, 0);
			return false;
		}
	}

	// ---- Stacks of Fish minion ----
	public class FishStackMinion : ModProjectile
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Goldfish;

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = 1;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.CultistIsResistantTo[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 24;
			Projectile.height = 16;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 1f / 3f;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool? CanCutTiles() => false;
		public override bool MinionContactDamage() => true;

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active) {
				owner.ClearBuff(ModContent.BuffType<FishStackBuff>());
				return;
			}
			if (owner.HasBuff<FishStackBuff>())
				Projectile.timeLeft = 2;

			// Find our place in the stack.
			int index = 0;
			foreach (Projectile other in Main.ActiveProjectiles) {
				if (other.type == Type && other.owner == Projectile.owner && other.whoAmI < Projectile.whoAmI)
					index++;
			}
			Vector2 idle = owner.Top + new Vector2(0f, -10f - index * 14f);
			Projectile.spriteDirection = owner.direction;

			// Attack nearby enemies by flopping at them, then go back to the stack.
			NPC target = null;
			float best = 400f;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy())
					continue;
				float d = Vector2.Distance(npc.Center, owner.Center);
				if (d < best) { best = d; target = npc; }
			}
			if (target != null && Projectile.ai[0] > 0f) {
				Projectile.velocity = (Projectile.velocity * 9f + (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 10f) / 10f;
				Projectile.friendly = true;
				if (++Projectile.ai[1] > 60) { Projectile.ai[1] = 0; Projectile.ai[0] = 0f; }
			}
			else {
				Projectile.Center = Vector2.Lerp(Projectile.Center, idle, 0.4f);
				Projectile.velocity = Vector2.Zero;
				Projectile.friendly = false;
				if (target != null && Main.rand.NextBool(90 + index * 20))
					Projectile.ai[0] = 1f;
			}
			Projectile.rotation = Projectile.velocity.X * 0.1f;
		}
	}

	// ---- Mega fart explosion (Fart Reverb.mp4): hits everything, blows up blocks ----
	public class MegaFartExplosion : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/RocketPackExplosion";

		public override void SetDefaults() {
			Projectile.width = 100;
			Projectile.height = 100;
			Projectile.friendly = true;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 4;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 60;
		}

		public override void OnSpawn(IEntitySource source) {
			int radiusTiles = (int)Projectile.ai[0];
			int size = Math.Max(100, radiusTiles * 32);
			Projectile.Resize(size, size);
			if (Projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[Projectile.owner];
			// Everyone takes it. Including you. Including the Guide.
			foreach (Player player in Main.ActivePlayers) {
				if (!player.dead && Vector2.Distance(player.Center, Projectile.Center) < size / 2f) {
					if (player.whoAmI == Main.myPlayer)
						player.Hurt(PlayerDeathReason.ByCustomReason(Terraria.Localization.NetworkText.FromKey("Mods.BagofChaos.DeathMessages.FartMp4", player.name)), Projectile.damage, 0);
				}
			}
			foreach (NPC npc in Main.ActiveNPCs) {
				if ((npc.townNPC || npc.friendly) && Vector2.Distance(npc.Center, Projectile.Center) < size / 2f)
					npc.SimpleStrikeNPC(Projectile.damage, 0, noPlayerInteraction: true);
			}
			// Blow up blocks.
			if (radiusTiles > 0) {
				Point center = Projectile.Center.ToTileCoordinates();
				for (int x = center.X - radiusTiles; x <= center.X + radiusTiles; x++) {
					for (int y = center.Y - radiusTiles; y <= center.Y + radiusTiles; y++) {
						if (!WorldGen.InWorld(x, y, 10) || new Vector2(x - center.X, y - center.Y).Length() > radiusTiles)
							continue;
						Tile tile = Main.tile[x, y];
						if (tile.HasTile && WorldGen.CanKillTile(x, y) && !TileID.Sets.BasicChest[tile.TileType] && tile.TileType != TileID.LihzahrdBrick)
							WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: true);
					}
				}
				if (Main.netMode == NetmodeID.MultiplayerClient) {
					for (int y = center.Y - radiusTiles; y <= center.Y + radiusTiles; y += 30)
						NetMessage.SendTileSquare(-1, center.X - radiusTiles, y, radiusTiles * 2 + 1, Math.Min(30, center.Y + radiusTiles - y + 1));
				}
			}
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 20f, 8f, 40, 3000f, "FartMp4"));
		}

		public override bool CanHitPlayer(Player target) => false; // handled manually so the owner is included
	}

	// ---- Woops! All Arrows! ----
	public class WoopsArrow : ModProjectile
	{
		public bool Child { get => Projectile.ai[0] == 1f; set => Projectile.ai[0] = value ? 1f : 0f; }
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			AIType = ProjectileID.WoodenArrowFriendly;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 1;
			Projectile.ArmorPenetration = 10000;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.SetMaxDamage(1);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Child || Projectile.owner != Main.myPlayer)
				return;
			int count = 2 * CountUniqueBossesKilled();
			for (int i = 0; i < count; i++) {
				int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f, 12f), Type, 1, 0f, Projectile.owner, 1f);
				if (index >= 0 && index < Main.maxProjectiles)
					Main.projectile[index].tileCollide = true;
			}
		}

		public static int CountUniqueBossesKilled() {
			int n = 0;
			bool[] flags = { NPC.downedSlimeKing, NPC.downedBoss1, NPC.downedBoss2, NPC.downedQueenBee, NPC.downedBoss3, NPC.downedDeerclops, Main.hardMode,
				NPC.downedQueenSlime, NPC.downedMechBoss1, NPC.downedMechBoss2, NPC.downedMechBoss3, NPC.downedPlantBoss, NPC.downedGolemBoss, NPC.downedFishron,
				NPC.downedEmpressOfLight, NPC.downedAncientCultist, NPC.downedMoonlord };
			foreach (bool f in flags)
				if (f) n++;
			return n;
		}
	}

	// ---- Arrow of Replication ----
	public class ReplicationArrow : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.HolyArrow;

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.WoodenArrowFriendly);
			AIType = ProjectileID.WoodenArrowFriendly;
		}

		public override void OnSpawn(IEntitySource source) {
			if (Projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[Projectile.owner];
			foreach (Projectile other in Main.ActiveProjectiles) {
				if (other.owner != Projectile.owner || !other.arrow || other.whoAmI == Projectile.whoAmI)
					continue;
				var global = other.GetGlobalProjectile<ChaosGlobalProjectile>();
				if (global.Replica)
					continue; // duplicates can't duplicate
				// Each duplicate costs one arrow from your inventory.
				if (!ConsumeAnyArrow(owner))
					break;
				int index = Projectile.NewProjectile(other.GetSource_FromThis(), other.Center, other.velocity.RotatedByRandom(0.15f), other.type, other.damage, other.knockBack, other.owner);
				if (index >= 0 && index < Main.maxProjectiles)
					Main.projectile[index].GetGlobalProjectile<ChaosGlobalProjectile>().Replica = true;
			}
		}

		private static bool ConsumeAnyArrow(Player player) {
			foreach (Item item in player.inventory) {
				if (!item.IsAir && item.ammo == AmmoID.Arrow && item.consumable && item.stack > 0) {
					item.stack--;
					if (item.stack <= 0)
						item.TurnToAir();
					return true;
				}
			}
			return false;
		}
	}

	// ---- Slotting Pillar: a flying inventory slot that boomerangs back ----
	public class SlotProjectile : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/PongBall";
		private int slot = -1;

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = true;
		}

		public override void AI() {
			if (slot < 0)
				slot = Main.rand.Next(10, 50); // never a hotbar slot
			Projectile.rotation += 0.1f;
			if (Projectile.ai[0] == 1f) {
				// returning
				Player owner = Main.player[Projectile.owner];
				Vector2 to = owner.Center - Projectile.Center;
				if (to.Length() < 30f) { Projectile.Kill(); return; }
				Projectile.velocity = to.SafeNormalize(Vector2.Zero) * 16f;
				Projectile.tileCollide = false;
			}
			else if (!Common.ChaosUtils.OnScreen(Projectile)) {
				Projectile.ai[0] = 1f;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) { Projectile.ai[0] = 1f; return false; }
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { Projectile.ai[0] = 1f; Projectile.penetrate = -1; }

		public override bool PreDraw(ref Color lightColor) {
			Texture2D back = TextureAssets.InventoryBack.Value;
			Main.EntitySpriteDraw(back, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, back.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
			Player owner = Main.player[Projectile.owner];
			if (slot >= 0 && slot < owner.inventory.Length && !owner.inventory[slot].IsAir) {
				Main.instance.LoadItem(owner.inventory[slot].type);
				Texture2D icon = TextureAssets.Item[owner.inventory[slot].type].Value;
				Main.EntitySpriteDraw(icon, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, icon.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
			}
			return false;
		}
	}

	// ---- Slotter Pillar: inventory slots as minions (only while the inventory is open) ----
	public class SlotMinion : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/PongBall";
		public int Slot => (int)Projectile.ai[0];

		public override void SetStaticDefaults() {
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.CultistIsResistantTo[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 0.5f;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool MinionContactDamage() => true;

		private Vector2 HomeWorldPosition(Player owner) {
			// The on-screen position of inventory slot (col,row) converted into world space. Vanilla: 20 + col*56, 20 + row*56 at UI scale, scaled by 0.85.
			int col = Slot % 10, row = Slot / 10;
			Vector2 ui = new Vector2(20f + col * 56f * 0.85f + 24f, 20f + row * 56f * 0.85f + 24f);
			Vector2 screen = ui * Main.UIScale;
			Vector2 half = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
			return (screen - half) / Main.GameViewMatrix.Zoom + half + Main.screenPosition;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active) {
				owner.ClearBuff(ModContent.BuffType<SlotterBuff>());
				return;
			}
			if (owner.HasBuff<SlotterBuff>())
				Projectile.timeLeft = 2;
			// Closing the inventory recalls everything.
			if (Projectile.owner == Main.myPlayer && !Main.playerInventory) {
				Projectile.Kill();
				return;
			}
			Vector2 home = HomeWorldPosition(owner);
			if (Projectile.ai[1] > 0f) {
				Projectile.ai[1]--;
				NPC target = null;
				float best = 600f;
				foreach (NPC npc in Main.ActiveNPCs) {
					if (!npc.CanBeChasedBy()) continue;
					float d = Vector2.Distance(npc.Center, home);
					if (d < best) { best = d; target = npc; }
				}
				if (target != null)
					Projectile.velocity = (Projectile.velocity * 6f + (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 18f) / 7f;
				Projectile.friendly = true;
			}
			else {
				Projectile.Center = Vector2.Lerp(Projectile.Center, home, 0.3f);
				Projectile.velocity = Vector2.Zero;
				Projectile.friendly = false;
				if (Main.rand.NextBool(120))
					Projectile.ai[1] = 60f;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D back = TextureAssets.InventoryBack.Value;
			Main.EntitySpriteDraw(back, Projectile.Center - Main.screenPosition, null, Color.White, 0f, back.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
			Player owner = Main.player[Projectile.owner];
			if (Slot >= 0 && Slot < owner.inventory.Length && !owner.inventory[Slot].IsAir) {
				Main.instance.LoadItem(owner.inventory[Slot].type);
				Texture2D icon = TextureAssets.Item[owner.inventory[Slot].type].Value;
				Main.EntitySpriteDraw(icon, Projectile.Center - Main.screenPosition, null, Color.White, 0f, icon.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
