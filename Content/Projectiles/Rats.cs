using BagofChaos.Common.GlobalProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Rattling Gun: a high speed rat. Dies on impact, poisons on hit.
	public class RatBullet : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.aiStyle = -1;
			Projectile.extraUpdates = 1;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.04f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.spriteDirection = Projectile.direction;
			if (Projectile.velocity.X < 0f) {
				Projectile.rotation += MathHelper.Pi;
				Projectile.spriteDirection = -1;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Poisoned, 360);
		}

		public override void OnKill(int timeLeft) {
			SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Squeak") { Volume = 0.6f, PitchVariance = 0.4f }, Projectile.Center);
			for (int i = 0; i < 6; i++)
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f);
		}
	}

	// Pocket Rats: thrown rats that burst into a poison cloud on impact with anything.
	public class PocketRat : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.25f;
			Projectile.rotation += 0.3f * Projectile.direction;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Poisoned, 360);
		}

		public override void OnKill(int timeLeft) {
			SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Squeak") { Volume = 0.5f, PitchVariance = 0.4f }, Projectile.Center);
			if (Projectile.owner != Main.myPlayer)
				return;
			int[] clouds = { ProjectileID.ToxicCloud, ProjectileID.ToxicCloud2, ProjectileID.ToxicCloud3 };
			for (int i = 0; i < 3; i++) {
				Vector2 velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
				int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, clouds[i], Projectile.damage / 2, 0f, Projectile.owner);
				if (index >= 0 && index < Main.maxProjectiles) {
					Main.projectile[index].DamageType = DamageClass.Magic;
					Main.projectile[index].GetGlobalProjectile<ChaosGlobalProjectile>().Poisonous = true;
				}
			}
		}
	}
}
