using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Glue Gun ammo. Sticks to NPCs and players alike. No friends in glue.
	public class GlueGlob : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 180;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.15f;
			Projectile.rotation += 0.2f;
			if (Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0f, 0f, 100, new Color(255, 250, 220), 0.9f);
				dust.noGravity = true;
			}
			// Players get glued too, including the shooter's teammates. And the shooter.
			// Each client checks its own player (buffs on other players don't sync from here); only the owner kills the glob.
			Player local = Main.LocalPlayer;
			if (Main.netMode != NetmodeID.Server && local.active && !local.dead && local.Hitbox.Intersects(Projectile.Hitbox)) {
				bool shooterGracePeriod = local.whoAmI == Projectile.owner && Projectile.timeLeft > 170;
				if (!shooterGracePeriod) {
					local.AddBuff(ModContent.BuffType<GlueDebuff>(), 60 * 20);
					if (Projectile.owner == Main.myPlayer)
						Projectile.Kill();
					return;
				}
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			BagofChaos.SendApplyBuffToNPC(target, ModContent.BuffType<GlueDebuff>(), 60 * 20);
		}

		public override void OnKill(int timeLeft) {
			for (int i = 0; i < 8; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0f, 0f, 100, new Color(255, 250, 220), 1.2f);
				dust.velocity *= 0.5f;
			}
		}
	}
}
