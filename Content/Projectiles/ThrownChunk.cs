using System.Collections.Generic;
using BagofChaos.Common.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Block Rod throw mode: a rigid chunk of terrain with momentum and gravity. It lands, it becomes terrain again.
	// The clipboard only lives on the thrower's client, which is also the only client that places the tiles (and syncs them).
	public class ThrownChunk : ModProjectile
	{
		private static readonly Dictionary<int, BlockRodClipboard> clipboards = new();

		public static void Attach(int projectileIdentity, BlockRodClipboard clipboard) => clipboards[projectileIdentity] = clipboard;

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 60 * 20;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		private BlockRodClipboard Clipboard => clipboards.TryGetValue(Projectile.identity, out var clip) ? clip : null;

		public override void AI() {
			BlockRodClipboard clip = Clipboard;
			if (clip == null) {
				if (Projectile.owner == Main.myPlayer)
					Projectile.Kill();
				return;
			}
			Projectile.width = clip.Width * 16;
			Projectile.height = clip.Height * 16;
			Projectile.velocity.Y += 0.3f;
			if (Projectile.velocity.Y > 14f)
				Projectile.velocity.Y = 14f;

			// Sweep the whole chunk so we stop where it actually touches terrain.
			Vector2 step = Projectile.velocity;
			int steps = System.Math.Max(1, (int)System.Math.Ceiling(step.Length() / 6f));
			Vector2 sub = step / steps;
			bool landed = false;
			for (int i = 0; i < steps; i++) {
				Vector2 next = Projectile.position + sub;
				if (Collision.SolidCollision(next, Projectile.width, Projectile.height)) {
					landed = true;
					break;
				}
				Projectile.position = next;
			}
			if (landed || Projectile.timeLeft < 5)
				Land(clip);
			Projectile.velocity.X *= 0.999f;
		}

		private void Land(BlockRodClipboard clip) {
			if (Projectile.owner == Main.myPlayer) {
				Point anchor = (Projectile.position / 16f).ToPoint();
				// Snap to the grid and wiggle upward until the chunk fits (it won't overwrite existing blocks).
				int guard = 0;
				while (!clip.CanPlace(anchor) && guard++ < 64)
					anchor.Y--;
				if (guard < 64)
					clip.Paste(anchor);
				SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
				for (int i = 0; i < 20; i++)
					Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Dirt);
			}
			clipboards.Remove(Projectile.identity);
			Projectile.Kill();
		}

		public override void OnKill(int timeLeft) {
			clipboards.Remove(Projectile.identity);
		}

		public override bool PreDraw(ref Color lightColor) {
			BlockRodClipboard clip = Clipboard;
			if (clip == null)
				return false;
			for (int x = 0; x < clip.Width; x++) {
				for (int y = 0; y < clip.Height; y++) {
					var snap = clip.Tiles[x, y];
					if (!snap.HasTile)
						continue;
					Main.instance.LoadTiles(snap.TileType);
					Texture2D texture = TextureAssets.Tile[snap.TileType].Value;
					// Plain center frame of the tilesheet; good enough for a flying rock.
					Rectangle frame = new Rectangle(18, 18, 16, 16);
					Vector2 pos = Projectile.position + new Vector2(x * 16f, y * 16f) - Main.screenPosition;
					Main.EntitySpriteDraw(texture, pos, frame, lightColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
				}
			}
			return false;
		}
	}
}
