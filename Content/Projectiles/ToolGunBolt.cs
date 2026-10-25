using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Tool Gun bolt: duplicates any NPC it touches. Any. Yes, that one too.
	// Collision is done by hand so friendly NPCs, critters and bosses all count; the vanilla damage system is bypassed entirely.
	public class ToolGunBolt : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
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

			if (Projectile.owner != Main.myPlayer)
				return;
			Rectangle hitbox = Projectile.Hitbox;
			hitbox.Inflate(4, 4);
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!hitbox.Intersects(npc.Hitbox))
					continue;
				Duplicate(npc);
				Projectile.Kill();
				return;
			}
		}

		private void Duplicate(NPC target) {
			SoundEngine.PlaySound(SoundID.Item92, target.Center);
			BagofChaos.SendSpawnNPC(target.type, target.Center + new Vector2(target.direction * -(target.width + 8), 0f), target.life);
			for (int i = 0; i < 15; i++) {
				Dust dust = Dust.NewDustDirect(target.position, target.width, target.height, DustID.Electric, 0f, 0f, 0, default, 1.2f);
				dust.noGravity = true;
			}
		}
	}
}
