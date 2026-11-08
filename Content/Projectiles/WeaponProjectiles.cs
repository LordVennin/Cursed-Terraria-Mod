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
	// ---- Nagel und Hammer: thrown nails embed in enemies and add Nails stacks ----
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
			target.GetGlobalNPC<ChaosGlobalNPC>().AddNails(1, Projectile.velocity);
			target.AddBuff(ModContent.BuffType<Nails>(), ChaosGlobalNPC.NailDuration); // the visible timer; refreshed per nail
		}
	}

	// Geschmolzener nails: slower, bigger, red-hot. Burn and still build Nails stacks.
	public class MoltenNail : ThrownNail
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Projectile.scale = 1.4f;
			Projectile.width = 11;
			Projectile.height = 11;
		}

		public override void AI() {
			base.AI();
			Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.05f);
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch).noGravity = true;
		}

		public override Color? GetAlpha(Color lightColor) => new Color(255, 110, 60, 220);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			base.OnHitNPC(target, hit, damageDone);
			target.AddBuff(BuffID.OnFire3, 60 * 4);
		}
	}

	// The oversized Molten Hamaxe boomerang. Pulls all the nails out of whatever it hits: one explosion, fixed damage.
	public class MoltenHammerProjectile : ModProjectile
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.MoltenHamaxe;

		public override void SetDefaults() {
			Projectile.width = 50;
			Projectile.height = 50;
			Projectile.scale = 2f;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.aiStyle = ProjAIStyleID.Boomerang;
			AIType = ProjectileID.WoodenBoomerang;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override void PostAI() {
			Lighting.AddLight(Projectile.Center, 0.8f, 0.35f, 0.1f);
			if (Main.rand.NextBool(2))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, default, 1.4f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.OnFire3, 60 * 5);
			ChaosGlobalNPC global = target.GetGlobalNPC<ChaosGlobalNPC>();
			if (global.NailStacks > 0 && Projectile.owner == Main.myPlayer) {
				global.ConsumeNails(global.NailStacks);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<NailExplosion>(),
					Items.Weapons.MoltenNailAndHammer.ExplosionDamage, 6f, Projectile.owner);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Main.instance.LoadItem(ItemID.MoltenHamaxe);
			Texture2D texture = TextureAssets.Item[ItemID.MoltenHamaxe].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.5f), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	public class NailExplosion : InstantExplosion
	{
		protected override int Size => 120;
		protected override int DustType => DustID.Torch;
		public override string Texture => "BagofChaos/Content/Projectiles/CanExplosion";

		public override void SetDefaults() {
			base.SetDefaults();
			Projectile.DamageType = DamageClass.Melee;
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

		// Snails are immune to snail flails. Both kinds, all snails. No snail recursion.
		public override bool? CanHitNPC(NPC target) => ChaosGlobalNPC.IsSnail(target) ? false : null;

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			// Mark the target so a killing blow replaces its loot with snails. The server decides loot, so tell it too
			// (this packet goes out before the hit itself, so it arrives first).
			target.GetGlobalNPC<ChaosGlobalNPC>().MarkForSnails(Hot ? 2 : 1);
			BagofChaos.SendSnailMark(target, Hot ? 2 : 1);
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
			// Own out-and-back flight: vanilla boomerang AI turns around on every enemy hit, these fly straight through.
			Projectile.aiStyle = -1;
			AIType = 0;
			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 1; // ignores i-frames
			Projectile.ArmorPenetration = 10000; // ignores defense
		}

		private bool Returning { get => Projectile.ai[0] == 1f; set => Projectile.ai[0] = value ? 1f : 0f; }

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			Projectile.rotation += 0.4f * (Projectile.velocity.X < 0f ? -1f : 1f);
			Lighting.AddLight(Projectile.Center, Main.DiscoColor.ToVector3() * 0.4f);
			if (!Returning) {
				if (++Projectile.ai[1] >= 35f) {
					Returning = true;
					Projectile.netUpdate = true;
				}
				return;
			}
			Projectile.tileCollide = false;
			Vector2 toOwner = owner.Center - Projectile.Center;
			if (toOwner.Length() < 24f || owner.dead || !owner.active) {
				if (Projectile.owner == Main.myPlayer)
					Projectile.Kill();
				return;
			}
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, toOwner.SafeNormalize(Vector2.Zero) * 16f, 0.12f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			// Walls still send it home, like a normal light disc.
			Projectile.velocity = -oldVelocity;
			Returning = true;
			Projectile.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
			return false;
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
			Main.instance.LoadItem(Hardmode ? ItemID.AcornAxe : ItemID.CopperAxe);
			Texture2D texture = Hardmode ? TextureAssets.Item[ItemID.AcornAxe].Value : TextureAssets.Item[ItemID.CopperAxe].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			return false;
		}

		public override void AI() {
			Projectile.rotation += 0.4f * Projectile.direction;
			if (Projectile.owner != Main.myPlayer)
				return;
			// Any tree the axe touches comes down in one hit: find the bottom of the trunk and cut it there.
			// (No axe power involved; this is the projectile doing it.)
			Rectangle box = Projectile.Hitbox;
			for (int x = box.Left / 16; x <= box.Right / 16; x++) {
				for (int y = box.Top / 16; y <= box.Bottom / 16; y++) {
					if (!WorldGen.InWorld(x, y, 5))
						continue;
					Tile tile = Main.tile[x, y];
					if (!tile.HasTile || !TileID.Sets.IsATreeTrunk[tile.TileType])
						continue;
					int baseY = y;
					while (baseY + 1 < Main.maxTilesY - 5 && Main.tile[x, baseY + 1].HasTile && TileID.Sets.IsATreeTrunk[Main.tile[x, baseY + 1].TileType])
						baseY++;
					WorldGen.KillTile(x, baseY);
					if (Main.netMode == NetmodeID.MultiplayerClient)
						NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: x, number3: baseY);
					SoundEngine.PlaySound(SoundID.Item37, Projectile.Center);
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Main.rand.NextFloat() < (Hardmode ? 0.5f : 0.2f))
				target.AddBuff(BuffID.Confused, 180);
		}
	}

	// ---- Rat Tacks: a single rat trap. Sits still on the floor; 3 successful bites or 30 seconds, whichever comes first. ----
	// ai[0] = successful hits so far. Damage type and damage follow the owner's current Rat Tacks mode every frame.
	public class RatTack : ModProjectile
	{
		public const int MaxHits = 3;
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Mouse;

		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 60 * 30;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			// Mode switches apply to rats already on the ground, without touching their timers or hit counts.
			Projectile.DamageType = Items.Weapons.RatTacks.ModeClass(owner);
			Projectile.damage = (int)owner.GetTotalDamage(Projectile.DamageType).ApplyTo(Items.Weapons.RatTacks.ModeDamage(owner));
			Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + 0.3f, 12f);
			if (Projectile.velocity.Y == 0f)
				Projectile.velocity.X = 0f; // landed: it's a trap now
			else
				Projectile.spriteDirection = Projectile.velocity.X < 0 ? -1 : 1;
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (oldVelocity.Y > 0f) {
				Projectile.velocity = Vector2.Zero;
			}
			else if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = 0f;
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Squeak") { Volume = 0.5f, PitchVariance = 0.4f }, Projectile.Center);
			if (++Projectile.ai[0] >= MaxHits)
				Projectile.Kill();
			else
				Projectile.netUpdate = true;
		}

		public override bool PreDraw(ref Color lightColor) {
			Main.instance.LoadNPC(NPCID.Mouse);
			Texture2D texture = TextureAssets.Npc[NPCID.Mouse].Value;
			Rectangle frame = texture.Frame(1, Main.npcFrameCount[NPCID.Mouse], 0, 0);
			SpriteEffects effects = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 position = Projectile.Bottom - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY + 2f);
			Main.EntitySpriteDraw(texture, position, frame, lightColor, 0f, new Vector2(frame.Width / 2f, frame.Height), 1f, effects, 0);
			return false;
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

	// ---- Stacks of Fish: a tower of goldfish on your head. Each fish shoots goldfish on its own range and cooldown. ----
	// ai[0] = this fish's attack cooldown. The tower never breaks: fish stay put and fire separate projectiles.
	public class FishStackMinion : ModProjectile
	{
		public const float AttackRange = 520f;
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Goldfish;

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = 1;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.CultistIsResistantTo[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 24;
			Projectile.height = 16;
			Projectile.tileCollide = false;
			Projectile.friendly = false; // it shoots; it doesn't flop at things
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
		}

		public override bool? CanCutTiles() => false;
		public override bool MinionContactDamage() => false;

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active) {
				owner.ClearBuff(ModContent.BuffType<FishStackBuff>());
				return;
			}
			if (owner.HasBuff<FishStackBuff>())
				Projectile.timeLeft = 2;

			// Our floor in the tower: one fish above the other, starting on top of the player's head.
			int index = 0;
			foreach (Projectile other in Main.ActiveProjectiles) {
				if (other.type == Type && other.owner == Projectile.owner && other.whoAmI < Projectile.whoAmI)
					index++;
			}
			Vector2 seat = owner.Top + new Vector2(owner.direction * -2f, -8f - index * 14f + owner.gfxOffY);
			Projectile.Center = seat;
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation = (float)System.Math.Sin((Main.GameUpdateCount + index * 13) * 0.08f) * 0.12f; // a little wobble

			// Targeting: a whip-marked enemy in range wins, otherwise the closest valid enemy in this fish's own range.
			NPC target = null;
			if (owner.HasMinionAttackTargetNPC) {
				NPC marked = Main.npc[owner.MinionAttackTargetNPC];
				if (marked.CanBeChasedBy(Projectile) && Vector2.Distance(marked.Center, Projectile.Center) <= AttackRange)
					target = marked;
			}
			if (target == null) {
				float best = AttackRange;
				foreach (NPC npc in Main.ActiveNPCs) {
					if (!npc.CanBeChasedBy(Projectile))
						continue;
					float d = Vector2.Distance(npc.Center, Projectile.Center);
					if (d < best && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) { best = d; target = npc; }
				}
			}
			Projectile.spriteDirection = target != null ? (target.Center.X > Projectile.Center.X ? 1 : -1) : owner.direction;

			if (Projectile.ai[0] > 0f)
				Projectile.ai[0]--;
			if (target != null && Projectile.ai[0] <= 0f && Projectile.owner == Main.myPlayer) {
				Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 11f;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<GoldfishShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
				Projectile.ai[0] = 50 + Main.rand.Next(20); // each fish keeps its own rhythm
				Projectile.netUpdate = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Main.instance.LoadNPC(NPCID.Goldfish);
			Texture2D texture = TextureAssets.Npc[NPCID.Goldfish].Value;
			int frames = Main.npcFrameCount[NPCID.Goldfish];
			Rectangle frame = texture.Frame(1, frames, 0, (int)(Main.GameUpdateCount / 8 % frames));
			SpriteEffects effects = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() * 0.5f, 1f, effects, 0);
			return false;
		}
	}

	// A goldfish, fired from the tower. Flops through the air and splats.
	public class GoldfishShot : ModProjectile
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Goldfish;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionShot[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 120;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.12f;
			Projectile.rotation += 0.3f * (Projectile.velocity.X < 0f ? -1f : 1f);
		}

		public override void OnKill(int timeLeft) {
			for (int i = 0; i < 5; i++)
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water);
			SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = 0.3f, Pitch = 0.5f }, Projectile.Center);
		}

		public override bool PreDraw(ref Color lightColor) {
			Main.instance.LoadNPC(NPCID.Goldfish);
			Texture2D texture = TextureAssets.Npc[NPCID.Goldfish].Value;
			Rectangle frame = texture.Frame(1, Main.npcFrameCount[NPCID.Goldfish], 0, 0);
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() * 0.5f, 0.8f, SpriteEffects.None, 0);
			return false;
		}
	}

	// ---- Mega fart explosion (Fart Reverb.mp4): hits everything, blows up blocks ----
	// ai[0] = block radius in tiles. Every client sizes it and checks its own player, so other players get hit too.
	public class MegaFartExplosion : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/RocketPackExplosion";

		public override void SetDefaults() {
			Projectile.width = 100;
			Projectile.height = 100;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 4;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 60;
		}

		private float Radius => Math.Max(100, (int)Projectile.ai[0] * 32) / 2f;

		public override void AI() {
			if (Projectile.localAI[0] != 0f)
				return;
			Projectile.localAI[0] = 1f;
			int radiusTiles = (int)Projectile.ai[0];
			int size = (int)(Radius * 2f);
			Projectile.Resize(size, size);

			// Everyone takes it. Including you. Including the Guide. Each client handles its own player.
			Player local = Main.LocalPlayer;
			if (Main.netMode != NetmodeID.Server && local.active && !local.dead && Vector2.Distance(local.Center, Projectile.Center) < Radius)
				local.Hurt(PlayerDeathReason.ByCustomReason(Terraria.Localization.NetworkText.FromKey("Mods.BagofChaos.DeathMessages.FartMp4", local.name)), Projectile.damage, 0);

			if (Projectile.owner != Main.myPlayer)
				return;
			foreach (NPC npc in Main.ActiveNPCs) {
				if ((npc.townNPC || npc.friendly) && !npc.dontTakeDamage && Vector2.Distance(npc.Center, Projectile.Center) < Radius)
					npc.SimpleStrikeNPC(Projectile.damage, 0, noPlayerInteraction: true);
			}
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

		// Enemies are hit by normal (circular-ish) collision; players and friendly NPCs are handled above.
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.Distance(closest, Projectile.Center) < Radius;
		}

		public override bool CanHitPlayer(Player target) => false;
		public override bool CanHitPvp(Player target) => false;
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

	// ---- Slotting Pillar: a random backpack slot launches itself the way you're aiming, then flies back into its slot ----
	// ai[1] = slot index + 1 (never the hotbar). It only *shows* the item; nothing is taken out of your inventory.
	public class SlotProjectile : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/PongBall";
		private int Slot => (int)Projectile.ai[1] - 1;
		private bool Returning { get => Projectile.ai[0] == 1f; set => Projectile.ai[0] = value ? 1f : 0f; }

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 600;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void OnSpawn(IEntitySource source) {
			if (Projectile.owner != Main.myPlayer)
				return;
			Projectile.ai[1] = Main.rand.Next(10, 50) + 1;
			Projectile.Center = SlotMinion.SlotWorldPosition(Slot);
			Projectile.netUpdate = true;
		}

		public override void AI() {
			Projectile.rotation += 0.15f;
			if (Projectile.owner != Main.myPlayer)
				return; // the slot's position only makes sense on its owner's screen
			if (Returning) {
				Vector2 home = SlotMinion.SlotWorldPosition(Slot);
				Vector2 to = home - Projectile.Center;
				Projectile.friendly = false;
				Projectile.tileCollide = false;
				if (to.Length() < 24f) {
					Projectile.Kill();
					return;
				}
				Projectile.velocity = to.SafeNormalize(Vector2.Zero) * Math.Max(18f, Projectile.velocity.Length());
			}
			else if (!Common.ChaosUtils.OnScreen(Projectile) && Projectile.timeLeft < 590) {
				Returning = true;
				Projectile.netUpdate = true;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			Returning = true;
			Projectile.netUpdate = true;
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Returning = true;
			Projectile.netUpdate = true;
		}

		public override bool PreDraw(ref Color lightColor) {
			SlotMinion.DrawSlot(Projectile, Slot, Projectile.rotation);
			return false;
		}
	}

	// ---- Slotter Pillar: inventory slots as minions. They live where their slot is on screen, inventory open or not. ----
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

		/// <summary>The world position under inventory slot (col,row) on the local screen. Vanilla: 20 + col*56, 20 + row*56 at 0.85 scale.</summary>
		public static Vector2 SlotWorldPosition(int slot) {
			int col = slot % 10, row = slot / 10;
			Vector2 ui = new Vector2(20f + col * 56f * 0.85f + 24f, 20f + row * 56f * 0.85f + 24f);
			Vector2 screen = ui * Main.UIScale;
			Vector2 half = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
			return (screen - half) / Main.GameViewMatrix.Zoom + half + Main.screenPosition;
		}

		/// <summary>Draws an inventory slot background with the item currently in that slot. A picture, not the item.</summary>
		public static void DrawSlot(Projectile projectile, int slot, float rotation) {
			Texture2D back = TextureAssets.InventoryBack.Value;
			Main.EntitySpriteDraw(back, projectile.Center - Main.screenPosition, null, Color.White, rotation, back.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
			Player owner = Main.player[projectile.owner];
			if (slot < 0 || slot >= owner.inventory.Length || owner.inventory[slot].IsAir)
				return;
			Main.instance.LoadItem(owner.inventory[slot].type);
			Texture2D icon = TextureAssets.Item[owner.inventory[slot].type].Value;
			Rectangle frame = Main.itemAnimations[owner.inventory[slot].type] != null ? Main.itemAnimations[owner.inventory[slot].type].GetFrame(icon) : icon.Frame();
			float scale = 0.6f * System.Math.Min(1f, 40f / System.Math.Max(frame.Width, frame.Height));
			Main.EntitySpriteDraw(icon, projectile.Center - Main.screenPosition, frame, Color.White, rotation, frame.Size() * 0.5f, scale, SpriteEffects.None, 0);
		}

		private Vector2 HomeWorldPosition(Player owner) => SlotWorldPosition(Slot);

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active) {
				owner.ClearBuff(ModContent.BuffType<SlotterBuff>());
				return;
			}
			if (owner.HasBuff<SlotterBuff>())
				Projectile.timeLeft = 2;
			if (Projectile.owner != Main.myPlayer)
				return; // home is a spot on the owner's screen; everyone else just follows the sync
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
				if (Main.rand.NextBool(120)) {
					Projectile.ai[1] = 60f;
					Projectile.netUpdate = true;
				}
			}
			if (Main.GameUpdateCount % 20 == 0)
				Projectile.netUpdate = true;
		}

		public override bool PreDraw(ref Color lightColor) {
			DrawSlot(Projectile, Slot, 0f);
			return false;
		}
	}
}
