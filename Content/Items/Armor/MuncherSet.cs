using BagofChaos.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Armor
{
	// Walk into a block at the matching body level and it's gone. Power is your best pickaxe's.
	[AutoloadEquip(EquipType.Head)]
	public class MuncherMask : ModItem
	{
		public static LocalizedText SetBonusText { get; private set; }

		public override void SetStaticDefaults() {
			SetBonusText = this.GetLocalization("SetBonus");
		}

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 20;
			Item.defense = 3;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(gold: 1);
		}

		public override void UpdateEquip(Player player) => player.GetModPlayer<ChaosPlayer>().MuncherHead = true;

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<MuncherShirt>() && legs.type == ModContent.ItemType<MuncherShoes>();
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = SetBonusText.Value;
			player.GetModPlayer<ChaosPlayer>().MuncherSetBonus = true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 10).AddIngredient(ItemID.Goldfish).AddTile(TileID.Anvils).Register();
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class MuncherShirt : ModItem
	{
		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 20;
			Item.defense = 4;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(gold: 1);
		}

		public override void UpdateEquip(Player player) => player.GetModPlayer<ChaosPlayer>().MuncherBody = true;

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 20).AddIngredient(ItemID.Bunny).AddTile(TileID.Anvils).Register();
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class MuncherShoes : ModItem
	{
		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 18;
			Item.defense = 3;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(gold: 1);
		}

		public override void UpdateEquip(Player player) => player.GetModPlayer<ChaosPlayer>().MuncherLegs = true;

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 15).AddIngredient(ItemID.Frog).AddTile(TileID.Anvils).Register();
		}
	}
}
