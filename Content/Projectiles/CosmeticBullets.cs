using BagofChaos.Common.GlobalNPCs;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	public abstract class SimpleBullet : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = ProjAIStyleID.Arrow;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 600;
			Projectile.alpha = 255;
			Projectile.ignoreWater = true;
			Projectile.extraUpdates = 1;
			AIType = ProjectileID.Bullet;
		}

		public override void OnKill(int timeLeft) {
			Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
		}
	}

	// Puts googly eyes on whatever it hits. Permanently. They never stop looking at you.
	public class GooglyBullet : SimpleBullet
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ChaosGlobalNPC g = target.GetGlobalNPC<ChaosGlobalNPC>();
			if (!g.Googly) {
				g.Googly = true;
				BagofChaos.SendNPCVisual(target);
			}
		}
	}

	// "You're Long?" - stretches the target's sprite to 4x width. The hitbox does not care.
	public class YoureLongBullet : SimpleBullet
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ChaosGlobalNPC g = target.GetGlobalNPC<ChaosGlobalNPC>();
			if (!g.Stretched) {
				g.Stretched = true;
				BagofChaos.SendNPCVisual(target);
			}
		}
	}

	// "You're Tall?" - stretches the target's sprite to 4x height. Mind your head.
	public class YoureTallBullet : SimpleBullet
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ChaosGlobalNPC g = target.GetGlobalNPC<ChaosGlobalNPC>();
			if (!g.Tall) {
				g.Tall = true;
				BagofChaos.SendNPCVisual(target);
			}
		}
	}

	// Buddy System: applies Life Link. Everyone linked on screen shares every hit.
	public class BuddyBullet : SimpleBullet
	{
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			BagofChaos.SendApplyBuffToNPC(target, ModContent.BuffType<LifeLinkDebuff>(), 60 * 30);
		}
	}
}
