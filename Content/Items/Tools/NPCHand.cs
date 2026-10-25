using BagofChaos.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Tools
{
	// Click and drag any NPC. No damage. Rearrange your town. Or the Eye of Cthulhu.
	public class NPCHand : ModItem
	{
		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 28;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 10;
			Item.useAnimation = 10;
			Item.channel = true;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(silver: 50);
		}

		public override bool CanUseItem(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return true;
			ChaosPlayer modPlayer = player.GetModPlayer<ChaosPlayer>();
			if (modPlayer.NPCHandTarget >= 0)
				return true; // already dragging
			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.Hitbox.Contains(Main.MouseWorld.ToPoint())) {
					modPlayer.NPCHandTarget = npc.whoAmI;
					modPlayer.NPCHandOffset = npc.Center - Main.MouseWorld;
					return true;
				}
			}
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Leather, 5).AddIngredient(ItemID.Silk, 5).AddTile(TileID.Loom).Register();
		}
	}
}
