using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// A short-lived friendly hitbox with a flash. Used by exploding tin cans and the Rocket Pack.
	public abstract class InstantExplosion : ModProjectile
	{
		protected abstract int Size { get; }
		protected abstract int DustType { get; }

		public override void SetDefaults() {
			Projectile.width = Size;
			Projectile.height = Size;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 6;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.DamageType = DamageClass.Generic;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) {
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			for (int i = 0; i < Size / 2; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustType, 0f, 0f, 100, default, 2f);
				dust.noGravity = true;
				dust.velocity *= 3f;
			}
			for (int i = 0; i < Size / 4; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
				dust.velocity *= 2f;
			}
		}

		public override void AI() {
			Projectile.alpha = 255 - Projectile.timeLeft * 40;
		}
	}

	public class CanExplosion : InstantExplosion
	{
		protected override int Size => 110;
		protected override int DustType => DustID.Torch;

		public override void SetDefaults() {
			base.SetDefaults();
			Projectile.DamageType = DamageClass.Ranged;
		}
	}

	public class RocketPackExplosion : InstantExplosion
	{
		protected override int Size => 170;
		protected override int DustType => DustID.Torch;

		public override void SetDefaults() {
			base.SetDefaults();
			Projectile.DamageType = DamageClass.Melee;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) {
			base.OnSpawn(source);
			SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
			if (Main.netMode != NetmodeID.Server && Projectile.owner == Main.myPlayer)
				Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 10f, 6f, 20, 1000f, "RocketPack"));
		}
	}
}
