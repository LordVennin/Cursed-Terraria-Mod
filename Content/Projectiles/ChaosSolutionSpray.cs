using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// Clentaminator spray that turns every block it touches into a random other block. Post-Moon Lord, because of course.
	public class ChaosSolutionSpray : ModProjectile
	{
		private static readonly ushort[] Pool = {
			TileID.Dirt, TileID.Stone, TileID.Sand, TileID.SnowBlock, TileID.IceBlock, TileID.Mud, TileID.Ash, TileID.Obsidian, TileID.ClayBlock,
			TileID.Silt, TileID.Slush, TileID.HardenedSand, TileID.Sandstone, TileID.Marble, TileID.Granite, TileID.Copper, TileID.Iron, TileID.Silver,
			TileID.Gold, TileID.Demonite, TileID.Crimtane, TileID.Hellstone, TileID.Cobalt, TileID.Mythril, TileID.Adamantite, TileID.Chlorophyte,
			TileID.LunarOre, TileID.Diamond, TileID.Ruby, TileID.Emerald, TileID.Sapphire, TileID.Topaz, TileID.Amethyst, TileID.Cloud,
			TileID.RainCloud, TileID.SnowCloud, TileID.HoneyBlock, TileID.CrispyHoneyBlock, TileID.Hive, TileID.Asphalt, TileID.Glass, TileID.PumpkinBlock,
			TileID.SlimeBlock, TileID.PinkSlimeBlock, TileID.FrozenSlimeBlock, TileID.BoneBlock, TileID.FleshBlock, TileID.Cactus, TileID.Spikes,
			TileID.WoodBlock, TileID.LivingWood, TileID.DynastyWood, TileID.Grass, TileID.CorruptGrass, TileID.CrimsonGrass, TileID.HallowedGrass,
			TileID.JungleGrass, TileID.MushroomGrass, TileID.GoldCoinPile, TileID.MeteoriteBrick, TileID.LihzahrdBrick, TileID.ShimmerBlock,
		};

		public ref float Progress => ref Projectile.ai[0];
		public bool ShotFromTerraformer => Projectile.ai[1] == 1f;

		public override void SetDefaults() {
			Projectile.DefaultToSpray();
			Projectile.aiStyle = 0;
		}

		public override bool? CanDamage() => false;

		public override void AI() {
			if (Projectile.timeLeft > 133)
				Projectile.timeLeft = 133;

			if (Projectile.owner == Main.myPlayer) {
				int size = ShotFromTerraformer ? 3 : 2;
				Point center = Projectile.Center.ToTileCoordinates();
				for (int x = center.X - size; x <= center.X + size; x++) {
					for (int y = center.Y - size; y <= center.Y + size; y++) {
						if (!WorldGen.InWorld(x, y, 10))
							continue;
						Tile tile = Main.tile[x, y];
						if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileFrameImportant[tile.TileType])
							continue;
						if (!Main.rand.NextBool(6))
							continue; // sprinkle, don't carpet
						ushort newType = Pool[Main.rand.Next(Pool.Length)];
						if (tile.TileType == newType)
							continue;
						tile.TileType = newType;
						tile.Slope = SlopeType.Solid;
						tile.IsHalfBlock = false;
						WorldGen.SquareTileFrame(x, y);
						if (Main.netMode == NetmodeID.MultiplayerClient)
							NetMessage.SendTileSquare(-1, x, y, 1, 1);
					}
				}
			}

			if (Progress > 7f) {
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, Projectile.velocity.X * 0.4f, Projectile.velocity.Y * 0.4f, 100, Main.DiscoColor);
				dust.noGravity = true;
				dust.scale *= 1.5f;
			}
			Progress++;
			Projectile.rotation += 0.3f * Projectile.direction;
		}
	}
}
