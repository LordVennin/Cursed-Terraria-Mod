using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Tool Gun bolt: duplicates any NPC it hits. Any. Yes, that one too.
	public class ToolGunBolt : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 120;
			Projectile.aiStyle = -1;
			Projectile.extraUpdates = 2;
			Projectile.tileCollide = true;
			Projectile.DamageType = DamageClass.Generic;
		}

		public override void AI() {
			Projectile.rotation += 0.4f;
			Lighting.AddLight(Projectile.Center, 0.2f, 0.4f, 0.9f);
			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Vector2.Zero, 0, default, 0.6f);
				dust.noGravity = true;
			}
		}

		// Town NPCs and critters are friendly, so a normal friendly projectile never "hits" them. We check manually.
		public override bool? CanHitNPC(NPC target) => target.active && !target.dontTakeDamage ? true : null;

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.FinalDamage *= 0f;
			modifiers.Knockback *= 0f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Duplicate(target);
		}

		private void Duplicate(NPC target) {
			if (Projectile.owner != Main.myPlayer)
				return;
			SoundEngine.PlaySound(SoundID.Item92, target.Center);
			BagofChaos.SendSpawnNPC(target.type, target.Center + new Vector2(target.direction * -(target.width + 8), 0f), target.life);
			for (int i = 0; i < 15; i++) {
				Dust dust = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Electric, 0f, 0f, 0, default, 1.2f);
				dust.noGravity = true;
			}
		}
	}
}
