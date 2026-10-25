using BagofChaos.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Ammo
{
	public abstract class ChaosBulletAmmo : ModItem
	{
		protected abstract int ProjectileType { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 99;
		}

		public override void SetDefaults() {
			Item.damage = 7;
			Item.DamageType = DamageClass.Ranged;
			Item.width = 8;
			Item.height = 8;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.knockBack = 2f;
			Item.value = Item.buyPrice(copper: 20);
			Item.rare = ItemRarityID.Blue;
			Item.shoot = ProjectileType;
			Item.shootSpeed = 16f;
			Item.ammo = AmmoID.Bullet;
		}
	}

	public class GooglyEyeAmmo : ChaosBulletAmmo
	{
		protected override int ProjectileType => ModContent.ProjectileType<GooglyBullet>();

		public override void AddRecipes() {
			CreateRecipe(50).AddIngredient(ItemID.MusketBall, 50).AddIngredient(ItemID.Lens).AddTile(TileID.WorkBenches).Register();
		}
	}

	public class YoureLongAmmo : ChaosBulletAmmo
	{
		protected override int ProjectileType => ModContent.ProjectileType<YoureLongBullet>();

		public override void AddRecipes() {
			CreateRecipe(50).AddIngredient(ItemID.MusketBall, 50).AddIngredient(ItemID.Rope, 10).AddTile(TileID.WorkBenches).Register();
		}
	}

	public class BuddySystemBullet : ChaosBulletAmmo
	{
		protected override int ProjectileType => ModContent.ProjectileType<BuddyBullet>();

		public override void AddRecipes() {
			CreateRecipe(50).AddIngredient(ItemID.MusketBall, 50).AddIngredient(ItemID.LifeCrystal).AddTile(TileID.Anvils).Register();
		}
	}

	// Post-Moon Lord Clentaminator ammo that converts blocks into random other blocks.
	public class ChaosSolution : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 99;
			ItemID.Sets.SortingPriorityTerraforming[Type] = 110;
		}

		public override void SetDefaults() {
			Item.DefaultToSolution(ModContent.ProjectileType<ChaosSolutionSpray>());
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(silver: 50);
		}

		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) {
			itemGroup = ContentSamples.CreativeHelper.ItemGroup.Solutions;
		}

		public override void AddRecipes() {
			CreateRecipe(25)
				.AddIngredient(ItemID.GreenSolution, 25)
				.AddIngredient(ItemID.LunarBar)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
