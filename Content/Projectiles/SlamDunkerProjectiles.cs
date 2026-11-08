using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// The Slam Dunker's hoop: a stationary sentry that lobs basketballs at the nearest enemy.
	public class DunkHoop : ModProjectile
	{
		public const float Range = 640f;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = false;
			Projectile.sentry = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.timeLeft = Projectile.SentryLifeTime;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override bool? CanDamage() => false;

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			Projectile.velocity = Vector2.Zero;
			if (Projectile.owner != Main.myPlayer || ++Projectile.ai[0] < 50f)
				return;
			NPC target = null;
			if (owner.HasMinionAttackTargetNPC && Vector2.Distance(Main.npc[owner.MinionAttackTargetNPC].Center, Projectile.Center) < Range)
				target = Main.npc[owner.MinionAttackTargetNPC];
			float best = Range;
			if (target == null) {
				foreach (NPC npc in Main.ActiveNPCs) {
					if (!npc.CanBeChasedBy(Projectile))
						continue;
					float d = Vector2.Distance(npc.Center, Projectile.Center);
					if (d < best) { best = d; target = npc; }
				}
			}
			if (target == null)
				return;
			Projectile.ai[0] = 0f;
			// A lob: aim a little above the target so gravity brings it down on them.
			Vector2 delta = target.Center - Projectile.Center;
			float time = Math.Clamp(delta.Length() / 10f, 10f, 45f);
			Vector2 velocity = new Vector2(delta.X / time, delta.Y / time - 0.5f * Basketball.Gravity * time);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + new Vector2(0f, 6f), velocity, ModContent.ProjectileType<Basketball>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
			SoundEngine.PlaySound(SoundID.Item5 with { Pitch = 0.4f }, Projectile.Center);
		}
	}

	// A basketball. Bounces around for a few seconds after a shot, so you can go and slam it.
	public class Basketball : ModProjectile
	{
		public const float Gravity = 0.25f;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.SentryShot[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 60 * 6;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void AI() {
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Gravity, 14f);
			Projectile.rotation += Projectile.velocity.X * 0.05f;
			if (Projectile.timeLeft < 30)
				Projectile.alpha = (int)(255 * (1f - Projectile.timeLeft / 30f));
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (Projectile.velocity.X != oldVelocity.X)
				Projectile.velocity.X = -oldVelocity.X * 0.8f;
			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = -oldVelocity.Y * 0.75f;
				Projectile.velocity.X *= 0.95f;
				if (Math.Abs(oldVelocity.Y) > 2f)
					SoundEngine.PlaySound(SoundID.Item56 with { Volume = 0.4f, Pitch = 0.2f }, Projectile.Center);
			}
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Projectile.velocity = new Vector2(-Projectile.velocity.X * 0.6f, -6f); // bonk
		}
	}

	// The swarm after a slam dunk: homes in on enemies.
	public class HomingBasketball : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/Basketball";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.SentryShot[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 60 * 4;
			Projectile.tileCollide = false;
		}

		public override void AI() {
			Projectile.rotation += 0.3f;
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.OrangeTorch).noGravity = true;
			if (Projectile.timeLeft > 225)
				return; // burst out of the hoop first
			NPC target = null;
			float best = 900f;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.CanBeChasedBy(Projectile))
					continue;
				float d = Vector2.Distance(npc.Center, Projectile.Center);
				if (d < best) { best = d; target = npc; }
			}
			if (target != null)
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 14f, 0.12f);
		}
	}
}
