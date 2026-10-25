using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Two paddles follow your cursor's X at the top and bottom of the screen. The ball bounces between them and grows every bounce.
	// Miss the ball and it's gone. Scales with summon damage.
	public class PongPaddle : ModProjectile
	{
		public bool IsBottom => Projectile.ai[0] == 1f;

		public override void SetStaticDefaults() {
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = false;
		}

		public override void SetDefaults() {
			Projectile.width = 12;
			Projectile.height = 60;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = 0f;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool MinionContactDamage() => true;

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active) {
				owner.ClearBuff(ModContent.BuffType<PongBuff>());
				return;
			}
			if (owner.HasBuff<PongBuff>())
				Projectile.timeLeft = 2;

			// The paddles live at the vertical edges of the owner's screen and chase the cursor horizontally.
			float screenTop = owner.Center.Y - Main.screenHeight / 2f / Main.GameViewMatrix.Zoom.Y;
			float screenBottom = owner.Center.Y + Main.screenHeight / 2f / Main.GameViewMatrix.Zoom.Y;
			float targetX = Projectile.owner == Main.myPlayer ? Main.MouseWorld.X : owner.Center.X + owner.direction * 200f;
			Vector2 target = new Vector2(targetX, IsBottom ? screenBottom - 50f : screenTop + 50f);
			Projectile.rotation = MathHelper.PiOver2;
			Projectile.Center = Vector2.Lerp(Projectile.Center, target, 0.35f);
			Projectile.velocity = Vector2.Zero;
			if (Projectile.owner == Main.myPlayer && Main.GameUpdateCount % 10 == 0)
				Projectile.netUpdate = true;
		}
	}

	public class PongBall : ModProjectile
	{
		public int Bounces { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 60 * 60;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
			Projectile.netImportant = true;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead || !owner.HasBuff<PongBuff>()) {
				Projectile.Kill();
				return;
			}

			Projectile.rotation += 0.1f;
			Projectile.scale = 1f + Bounces * 0.3f;
			int size = (int)(16 * Projectile.scale);
			Projectile.Resize(size, size);

			int paddleType = ModContent.ProjectileType<PongPaddle>();
			foreach (Projectile paddle in Main.ActiveProjectiles) {
				if (paddle.type != paddleType || paddle.owner != Projectile.owner)
					continue;
				if (!paddle.Hitbox.Intersects(Projectile.Hitbox))
					continue;
				bool bottom = paddle.ai[0] == 1f;
				if (bottom && Projectile.velocity.Y > 0f || !bottom && Projectile.velocity.Y < 0f) {
					Projectile.velocity.Y *= -1f;
					// Add a bit of english based on where the ball hit the paddle.
					Projectile.velocity.X += (Projectile.Center.X - paddle.Center.X) * 0.1f;
					Projectile.velocity *= 1.06f;
					Bounces++;
					Projectile.damage += Projectile.originalDamage / 4;
					Projectile.netUpdate = true;
					SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Pong") { Volume = 0.8f, Pitch = -0.2f + Bounces * 0.05f }, Projectile.Center);
				}
			}

			// Off the top or bottom of the owner's screen = you missed. Game over for the ball.
			float screenTop = owner.Center.Y - Main.screenHeight / 2f / Main.GameViewMatrix.Zoom.Y;
			float screenBottom = owner.Center.Y + Main.screenHeight / 2f / Main.GameViewMatrix.Zoom.Y;
			if (Projectile.Center.Y < screenTop - 40f || Projectile.Center.Y > screenBottom + 40f) {
				if (Main.netMode != NetmodeID.Server)
					CombatText.NewText(owner.Hitbox, Color.Red, Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.Projectiles.PongBall.Miss"), true);
				Projectile.Kill();
			}
			// Horizontal walls of the screen bounce the ball back.
			float screenLeft = owner.Center.X - Main.screenWidth / 2f / Main.GameViewMatrix.Zoom.X;
			float screenRight = owner.Center.X + Main.screenWidth / 2f / Main.GameViewMatrix.Zoom.X;
			if (Projectile.Center.X < screenLeft && Projectile.velocity.X < 0f || Projectile.Center.X > screenRight && Projectile.velocity.X > 0f)
				Projectile.velocity.X *= -1f;

			Lighting.AddLight(Projectile.Center, Vector3.One * 0.4f * Projectile.scale);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			// Bounce off enemies too; it's a pong ball, that's what it does.
			if (System.Math.Abs(Projectile.velocity.X) > System.Math.Abs(Projectile.velocity.Y))
				Projectile.velocity.X *= -1f;
			else
				Projectile.velocity.Y *= -1f;
		}
	}
}
