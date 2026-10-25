using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Fly out, latch onto an NPC, drag it to the player. Weight, gravity and knockback resistance are not consulted.
	public class GparrlingHookProjectile : ModProjectile
	{
		private int HookedNPC { get => (int)Projectile.ai[0] - 1; set => Projectile.ai[0] = value + 1; }
		private bool Retracting => Projectile.ai[1] == 1f;

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 240;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			Projectile.rotation = (owner.Center - Projectile.Center).ToRotation() - MathHelper.PiOver2;

			if (!Retracting) {
				if (Vector2.Distance(owner.Center, Projectile.Center) > 700f)
					StartRetract();
				return;
			}

			Projectile.tileCollide = false;
			Vector2 toOwner = owner.Center - Projectile.Center;
			float dist = toOwner.Length();
			if (dist < 40f) {
				Projectile.Kill();
				return;
			}
			Projectile.velocity = toOwner / dist * 18f;

			int index = HookedNPC;
			if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active) {
				NPC npc = Main.npc[index];
				npc.Center = Projectile.Center;
				npc.velocity = Projectile.velocity;
				if (Main.netMode == NetmodeID.Server)
					npc.netUpdate = true;
			}
		}

		private void StartRetract() {
			Projectile.ai[1] = 1f;
			Projectile.netUpdate = true;
			Projectile.timeLeft = 240;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Retracting)
				return;
			HookedNPC = target.whoAmI;
			StartRetract();
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			StartRetract();
			return false;
		}

		public override bool? CanHitNPC(NPC target) => Retracting ? false : null;

		public override bool PreDraw(ref Color lightColor) {
			// Chain from the player to the hook
			Player owner = Main.player[Projectile.owner];
			Texture2D chain = TextureAssets.Chain.Value;
			Vector2 from = owner.MountedCenter;
			Vector2 to = Projectile.Center;
			Vector2 dir = to - from;
			float length = dir.Length();
			if (length < 1f)
				return true;
			dir /= length;
			float rotation = dir.ToRotation() + MathHelper.PiOver2;
			for (float d = 0f; d < length; d += chain.Height) {
				Vector2 pos = from + dir * d;
				Color color = Lighting.GetColor((int)(pos.X / 16f), (int)(pos.Y / 16f));
				Main.EntitySpriteDraw(chain, pos - Main.screenPosition, null, color, rotation, chain.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			}
			return true;
		}
	}
}
