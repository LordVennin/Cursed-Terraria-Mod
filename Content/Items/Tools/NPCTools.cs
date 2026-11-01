using BagofChaos.Common.Players;
using BagofChaos.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Tools
{
	// Right-click tools that open a town NPC's interface from anywhere (as long as that NPC lives in your world).
	public abstract class RemoteNPCTool : ModItem
	{
		protected abstract int NPCType { get; }
		protected abstract RemoteMenu Menu { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 2);
		}

		public override bool CanRightClick() => true;

		public override bool ConsumeItem(Player player) => false;

		public override void RightClick(Player player) {
			RemoteMenuSystem.Toggle(Menu, NPCType);
		}
	}

	public class GuidesGuidebookOfGuiding : RemoteNPCTool
	{
		protected override int NPCType => NPCID.Guide;
		protected override RemoteMenu Menu => RemoteMenu.Chat;
	}

	public class ShopkeepersPortableShopOfKeeping : RemoteNPCTool
	{
		protected override int NPCType => NPCID.Merchant;
		protected override RemoteMenu Menu => RemoteMenu.SellOnlyShop;
	}

	public class GoblinYourMoney : RemoteNPCTool
	{
		protected override int NPCType => NPCID.GoblinTinkerer;
		protected override RemoteMenu Menu => RemoteMenu.Reforge;
	}

	public class SnippingTool : RemoteNPCTool
	{
		protected override int NPCType => NPCID.Stylist;
		protected override RemoteMenu Menu => RemoteMenu.Hair;
	}

	public class AnglersPocketguide : RemoteNPCTool
	{
		protected override int NPCType => NPCID.Angler;
		protected override RemoteMenu Menu => RemoteMenu.AnglerQuest;
	}

	public class NPCDirectory : RemoteNPCTool
	{
		protected override int NPCType => -1;
		protected override RemoteMenu Menu => RemoteMenu.Directory;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Book).AddIngredient(ItemID.Wire, 5).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Gentlemen, Stack.: everything in your inventory stacks to at least 99 while you carry it.
	public class GentlemenStack : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 10);
		}

		public override void UpdateInventory(Player player) {
			foreach (Item item in player.inventory) {
				if (!item.IsAir && item.maxStack < 99 && item.type != Type)
					item.maxStack = 99;
			}
		}
	}

	// Chaotic Quiver / Ammo Box / Roulette: random equipped ammo every shot, +1 base damage per distinct ammo type.
	public abstract class ChaoticAmmoTool : ModItem
	{
		public abstract bool Arrows { get; }
		public abstract bool Bullets { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 3);
		}

		public override void UpdateInventory(Player player) {
			ChaosPlayer chaos = player.GetModPlayer<ChaosPlayer>();
			if (Arrows) chaos.ChaoticArrows = true;
			if (Bullets) chaos.ChaoticBullets = true;
		}
	}

	public class ChaoticQuiver : ChaoticAmmoTool
	{
		public override bool Arrows => true;
		public override bool Bullets => false;
	}

	public class ChaoticAmmoBox : ChaoticAmmoTool
	{
		public override bool Arrows => false;
		public override bool Bullets => true;
	}

	public class ChaoticAmmunitionRoulette : ChaoticAmmoTool
	{
		public override bool Arrows => true;
		public override bool Bullets => true;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.LightRed;
		}

		public override void UpdateInventory(Player player) {
			base.UpdateInventory(player);
			player.GetModPlayer<ChaosPlayer>().AmmoRoulette = true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<ChaoticQuiver>().AddIngredient<ChaoticAmmoBox>().AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Chaotic Dash Box: no dash cooldown, dashes chain twice, but they go in a random direction.
	public class ChaoticDashBox : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 8);
		}

		public override void UpdateInventory(Player player) {
			player.GetModPlayer<ChaosPlayer>().ChaoticDash = true;
		}
	}

	// Chaotic Banker: passive tools in your piggy bank and safe still work.
	public class ChaoticBanker : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 8);
		}

		public override void UpdateInventory(Player player) {
			player.GetModPlayer<ChaosPlayer>().ChaoticBanker = true;
		}
	}
}
