using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Accessories
{
	// Somehow you had the exact glasses for this bizarre situation.
	public class ThreeDGlasses : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).ThreeDGlasses = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Sunglasses).AddIngredient(ItemID.Ruby).AddIngredient(ItemID.Sapphire).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Your explosions heal instead of hurting or destroying anything. Keeps the boom.
	public class ExplosiveHealingKit : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.LightRed;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).ExplosiveHealingKit = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.BandofRegeneration).AddIngredient(ItemID.Bomb, 10).AddIngredient(ItemID.HealingPotion, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// TM Flash: +40% damage underground, but you can only see about 20 tiles. Also, no homing. Flash was never that good.
	public class TMFlash : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Orange;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TMFlash = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.FlareGun).AddIngredient(ItemID.Torch, 20).AddIngredient(ItemID.GoldBar, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// While equipped, all text on screen has hitboxes that hurt enemies. Damage numbers hurt. Item pickup text hurts. Words hurt.
	public class TextBoxingGloves : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Pink;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TextBoxingGloves = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.FeralClaws).AddIngredient(ItemID.Book, 30).AddTile(TileID.Anvils).Register();
		}
	}
}
