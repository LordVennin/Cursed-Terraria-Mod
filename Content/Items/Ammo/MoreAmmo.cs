using BagofChaos.Common.GlobalProjectiles;
using BagofChaos.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Ammo
{
	// Woops! All Arrows!: 1 damage, ignores i-frames and defense, explodes into 2x(unique bosses killed) more arrows.
	public class WoopsAllArrows : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.WoodenArrow;

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 99;
		}

		public override void SetDefaults() {
			Item.damage = 1;
			Item.DamageType = DamageClass.Ranged;
			Item.width = 10;
			Item.height = 28;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.knockBack = 0.5f;
			Item.value = Item.buyPrice(copper: 50);
			Item.rare = ItemRarityID.Green;
			Item.shoot = ModContent.ProjectileType<WoopsArrow>();
			Item.shootSpeed = 4f;
			Item.ammo = AmmoID.Arrow;
		}

		public override void AddRecipes() {
			CreateRecipe(50).AddIngredient(ItemID.WoodenArrow, 50).AddIngredient(ItemID.FallenStar).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Arrow of Replication: firing one duplicates every arrow you have in flight. Duplicates can't duplicate.
	public class ArrowOfReplication : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.HolyArrow;

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 99;
		}

		public override void SetDefaults() {
			Item.damage = 12;
			Item.DamageType = DamageClass.Ranged;
			Item.width = 10;
			Item.height = 28;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.knockBack = 2f;
			Item.value = Item.buyPrice(silver: 2);
			Item.rare = ItemRarityID.LightRed;
			Item.shoot = ModContent.ProjectileType<ReplicationArrow>();
			Item.shootSpeed = 4.5f;
			Item.ammo = AmmoID.Arrow;
		}

		public override void AddRecipes() {
			CreateRecipe(50).AddIngredient(ItemID.WoodenArrow, 50).AddIngredient(ItemID.SoulofLight, 2).AddIngredient(ItemID.CrystalShard, 5).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Slotting Pillar: infinite ammo for bows and guns, but the bullets are your inventory slots. Only while the inventory is open.
	public class SlottingPillarAmmo : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.damage = 10;
			Item.DamageType = DamageClass.Ranged;
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = 1;
			Item.consumable = false;
			Item.knockBack = 3f;
			Item.value = Item.buyPrice(gold: 5);
			Item.rare = ItemRarityID.LightRed;
			Item.shoot = ModContent.ProjectileType<SlotProjectile>();
			Item.shootSpeed = 10f;
			Item.ammo = AmmoID.Bullet; // and arrows, via CanBeChosenAsAmmo
		}

		public override bool? CanBeChosenAsAmmo(Item weapon, Player player) => weapon.useAmmo == AmmoID.Bullet || weapon.useAmmo == AmmoID.Arrow;

		public override bool CanBeConsumedAsAmmo(Item weapon, Player player) => false;
	}
}
