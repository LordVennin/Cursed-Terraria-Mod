using BagofChaos.Common.Players;
using BagofChaos.Content.Items.Tools;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalNPCs
{
	// Where the NPC Directory pieces are sold. All repeatable, so losing one never blocks the Directory.
	public class NPCToolShops : GlobalNPC
	{
		public static readonly Condition Reforged = new(Language.GetText("Mods.BagofChaos.Conditions.Reforged"), () => Main.LocalPlayer.GetModPlayer<ChaosPlayer>().HasReforged);
		public static readonly Condition HadHaircut = new(Language.GetText("Mods.BagofChaos.Conditions.HadHaircut"), () => Main.LocalPlayer.GetModPlayer<ChaosPlayer>().HasUsedHairWindow);

		private static Item Priced(int type, int gold) => new(type) { shopCustomPrice = Item.buyPrice(gold: gold) };

		public override void ModifyShop(NPCShop shop) {
			if (shop.NpcType == NPCID.Merchant)
				shop.Add(Priced(ModContent.ItemType<ShopkeepersPortableShopOfKeeping>(), 10), Condition.InBeach);
			else if (shop.NpcType == NPCID.GoblinTinkerer)
				shop.Add(Priced(ModContent.ItemType<GoblinYourMoney>(), 50), Reforged);
			else if (shop.NpcType == NPCID.Stylist)
				shop.Add(Priced(ModContent.ItemType<SnippingTool>(), 20), HadHaircut);
		}
	}
}
