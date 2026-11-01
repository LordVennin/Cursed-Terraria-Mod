using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Pets
{
	// A spinning spider on top of a spinning ball. It doesn't make sense. Shimmer the Balls of Spiders to get it.
	public class SpiderOnABallProjectile : ModProjectile
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.WallCreeper;

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = 1;
			Main.projPet[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.ZephyrFish);
			Projectile.width = 40;
			Projectile.height = 40;
			AIType = ProjectileID.ZephyrFish;
		}

		public override bool PreAI() {
			Main.player[Projectile.owner].zephyrfish = false;
			return true;
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (!player.dead && player.HasBuff(ModContent.BuffType<SpiderOnABallBuff>()))
				Projectile.timeLeft = 2;
			Projectile.rotation += 0.15f;
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D spider = TextureAssets.Npc[NPCID.WallCreeper].Value;
			Texture2D ball = ModContent.Request<Texture2D>("BagofChaos/Content/Projectiles/PongBall").Value;
			Vector2 center = Projectile.Center - Main.screenPosition;
			// The ball spins one way...
			Main.EntitySpriteDraw(ball, center + new Vector2(0f, 12f), null, lightColor.MultiplyRGB(Color.Orange), -Projectile.rotation * 2f, ball.Size() * 0.5f, 1.6f, SpriteEffects.None, 0);
			// ...the spider spins the other way, on top.
			Rectangle frame = spider.Frame(1, Main.npcFrameCount[NPCID.WallCreeper], 0, 0);
			Main.EntitySpriteDraw(spider, center - new Vector2(0f, 10f), frame, lightColor, Projectile.rotation, frame.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
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
