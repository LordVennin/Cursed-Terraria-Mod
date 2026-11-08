using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Pets
{
	// A spinning spider on top of a spinning ball. It doesn't make sense. Shimmer the Balls of Spiders to get it.
	// Ground pet: it walks (rolls) after you with gravity like the Puppy, and flies over gaps the same way.
	// The ball is the Clown's Happy Bomb; spider and ball spin independently and absurdly fast.
	public class SpiderOnABallProjectile : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.HappyBomb;

		private ref float SpiderSpin => ref Projectile.localAI[0];
		private ref float BallSpin => ref Projectile.localAI[1];

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = 1;
			Main.projPet[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.Puppy);
			Projectile.width = 28;
			Projectile.height = 28;
			AIType = ProjectileID.Puppy;
		}

		public override bool PreAI() {
			Main.player[Projectile.owner].puppy = false;
			return true;
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (!player.dead && player.HasBuff(ModContent.BuffType<SpiderOnABallBuff>()))
				Projectile.timeLeft = 2;
			// +400% spin, and the two halves never agree on speed or direction.
			BallSpin += 0.9f + 0.3f * (float)System.Math.Sin(Main.GameUpdateCount * 0.07f) + Projectile.velocity.X * 0.05f;
			SpiderSpin -= 0.75f + 0.25f * (float)System.Math.Cos(Main.GameUpdateCount * 0.11f);
		}

		public override bool PreDraw(ref Color lightColor) {
			Main.instance.LoadProjectile(ProjectileID.HappyBomb);
			Main.instance.LoadNPC(NPCID.WallCreeper);
			Texture2D ball = TextureAssets.Projectile[ProjectileID.HappyBomb].Value;
			Texture2D spider = TextureAssets.Npc[NPCID.WallCreeper].Value;
			Vector2 bottom = Projectile.Bottom - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
			float ballScale = 1.3f;
			Rectangle ballFrame = ball.Frame(1, System.Math.Max(1, Main.projFrames[ProjectileID.HappyBomb]), 0, 0);
			float ballRadius = ballFrame.Height * ballScale * 0.5f;
			Vector2 ballCenter = bottom - new Vector2(0f, ballRadius);
			Main.EntitySpriteDraw(ball, ballCenter, ballFrame, lightColor, BallSpin, ballFrame.Size() * 0.5f, ballScale, SpriteEffects.None, 0);
			Rectangle frame = spider.Frame(1, Main.npcFrameCount[NPCID.WallCreeper], 0, 0);
			Vector2 spiderCenter = ballCenter - new Vector2(0f, ballRadius + frame.Height * 0.25f);
			Main.EntitySpriteDraw(spider, spiderCenter, frame, lightColor, SpiderSpin, frame.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
			return false;
		}
	}

	public class SpiderOnABallBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.vanityPet[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			bool unused = false;
			player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<SpiderOnABallProjectile>());
		}
	}

	public class SpiderOnABallItem : ModItem
	{
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.ZephyrFish);
			Item.width = 20;
			Item.height = 24;
			Item.shoot = ModContent.ProjectileType<SpiderOnABallProjectile>();
			Item.buffType = ModContent.BuffType<SpiderOnABallBuff>();
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer)
				player.AddBuff(Item.buffType, 3600);
			return true;
		}
	}
}
