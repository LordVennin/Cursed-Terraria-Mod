using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Paddles follow your cursor at the edges of the screen. Balls bounce between them and grow every bounce.
	// Five paddle bounces per ball, then it pops. Miss the ball and it's gone. Scales with summon damage.
	//
	// Paddle ai[0]: 0 top, 1 bottom, 2 left, 3 right. ai[1]: 1 = green (longer).
	public class PongPaddle : ModProjectile
	{
		public const float EdgeOffset = 50f + 32f; // 2 blocks closer to the player than the original 50px
		public int Side => (int)Projectile.ai[0];
		public bool Green => Projectile.ai[1] == 1f;
		public bool Vertical => Side >= 2;

		public override void SetStaticDefaults() {
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = false;
		}

		public override void SetDefaults() {
			Projectile.width = 12;
			Projectile.height = 180;
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

		public override string Texture => "BagofChaos/Content/Projectiles/PongPaddle";

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active) {
				owner.ClearBuff(ModContent.BuffType<PongBuff>());
				return;
			}
			if (owner.HasBuff<PongBuff>())
				Projectile.timeLeft = 2;

			int length = Green ? 220 : 180;
			if (Vertical)
				Projectile.Resize(length, 12);
			else
				Projectile.Resize(12, length);

			float halfW = Main.screenWidth / 2f / Main.GameViewMatrix.Zoom.X;
			float halfH = Main.screenHeight / 2f / Main.GameViewMatrix.Zoom.Y;
			Vector2 mouse = Projectile.owner == Main.myPlayer ? Main.MouseWorld : owner.Center + new Vector2(owner.direction * 200f, 0f);
			Vector2 target = Side switch {
				0 => new Vector2(mouse.X, owner.Center.Y - halfH + EdgeOffset),
				1 => new Vector2(mouse.X, owner.Center.Y + halfH - EdgeOffset),
				2 => new Vector2(owner.Center.X - halfW + EdgeOffset, mouse.Y),
				_ => new Vector2(owner.Center.X + halfW - EdgeOffset, mouse.Y),
			};
			Projectile.rotation = Vertical ? 0f : MathHelper.PiOver2;
			Projectile.Center = Vector2.Lerp(Projectile.Center, target, 0.35f);
			Projectile.velocity = Vector2.Zero;
			if (Projectile.owner == Main.myPlayer && Main.GameUpdateCount % 10 == 0)
				Projectile.netUpdate = true;
		}

		public override bool PreDraw(ref Color lightColor) {
			var texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
			Color color = Green ? Color.LimeGreen : Color.White;
			float scaleY = Projectile.height / (float)texture.Height;
			if (Vertical)
				scaleY = Projectile.width / (float)texture.Height;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Vertical ? MathHelper.PiOver2 : 0f, texture.Size() * 0.5f, new Vector2(1f, scaleY), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
			return false;
		}
	}

	// Ball ai[0]: bounces so far. ai[1]: 1 = green (side paddles exist, double speed).
	public class PongBall : ModProjectile
	{
		public const int MaxBounces = 5;
		public int Bounces { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
		public bool Green => Projectile.ai[1] == 1f;

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
				int side = (int)paddle.ai[0];
				bool reflected = false;
				if (side == 0 && Projectile.velocity.Y < 0f || side == 1 && Projectile.velocity.Y > 0f) {
					Projectile.velocity.Y *= -1f;
					Projectile.velocity.X += (Projectile.Center.X - paddle.Center.X) * 0.1f;
					reflected = true;
				}
				else if (side == 2 && Projectile.velocity.X < 0f || side == 3 && Projectile.velocity.X > 0f) {
					Projectile.velocity.X *= -1f;
					Projectile.velocity.Y += (Projectile.Center.Y - paddle.Center.Y) * 0.1f;
					reflected = true;
				}
				if (!reflected)
					continue;
				Projectile.velocity *= 1.06f;
				Bounces++;
				Projectile.damage += Projectile.originalDamage / 4;
				Projectile.netUpdate = true;
				SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Pong") { Volume = 0.8f, Pitch = -0.2f + Bounces * 0.05f }, Projectile.Center);
				if (Bounces >= MaxBounces) {
					Pop();
					return;
				}
			}

			// Off the owner's screen = you missed. Game over for the ball. (Side edges only bounce when there are no side paddles.)
			float halfW = Main.screenWidth / 2f / Main.GameViewMatrix.Zoom.X;
			float halfH = Main.screenHeight / 2f / Main.GameViewMatrix.Zoom.Y;
			if (Projectile.Center.Y < owner.Center.Y - halfH - 40f || Projectile.Center.Y > owner.Center.Y + halfH + 40f) {
				Miss(owner);
				return;
			}
			bool offLeft = Projectile.Center.X < owner.Center.X - halfW, offRight = Projectile.Center.X > owner.Center.X + halfW;
			if (Green) {
				if (Projectile.Center.X < owner.Center.X - halfW - 40f || Projectile.Center.X > owner.Center.X + halfW + 40f) {
					Miss(owner);
					return;
				}
			}
			else if (offLeft && Projectile.velocity.X < 0f || offRight && Projectile.velocity.X > 0f) {
				Projectile.velocity.X *= -1f;
			}

			Lighting.AddLight(Projectile.Center, (Green ? new Vector3(0.2f, 0.5f, 0.2f) : Vector3.One * 0.4f) * Projectile.scale);
		}

		private void Miss(Player owner) {
			if (Main.netMode != NetmodeID.Server)
				CombatText.NewText(owner.Hitbox, Color.Red, Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.Projectiles.PongBall.Miss"), true);
			Projectile.Kill();
		}

		private void Pop() {
			for (int i = 0; i < 20; i++) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, Green ? DustID.GreenTorch : DustID.WhiteTorch, 0f, 0f, 0, default, 1.5f);
				dust.noGravity = true;
			}
			SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
			Projectile.Kill();
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			// Bounce off enemies back toward the paddles; it's a pong ball, that's what it does.
			if (System.Math.Abs(Projectile.velocity.X) > System.Math.Abs(Projectile.velocity.Y))
				Projectile.velocity.X *= -1f;
			else
				Projectile.velocity.Y *= -1f;
			Projectile.netUpdate = true;
		}

		public override bool PreDraw(ref Color lightColor) {
			var texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Green ? Color.LimeGreen : Color.White, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
			return false;
		}
	}
}
