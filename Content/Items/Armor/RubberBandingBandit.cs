using BagofChaos.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Armor
{
	// The Rubber Banding Bandit set. Four helmets (one per class), one suit, one pair of shoes. Everything bounces.
	public abstract class BanditHelmet : ModItem
	{
		public static LocalizedText SetBonusText { get; private set; }
		public static LocalizedText WarpaintBonusText { get; private set; }

		public override void SetStaticDefaults() {
			SetBonusText ??= Language.GetText("Mods.BagofChaos.Items.RubberBandingBanditSuit.SetBonus");
			WarpaintBonusText ??= Language.GetText("Mods.BagofChaos.Items.RubberBandingBanditWarpaint.SetBonus");
		}

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 20;
			Item.defense = 8;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 3);
		}

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<RubberBandingBanditSuit>() && legs.type == ModContent.ItemType<RubberBandingBanditShoes>();
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = SetBonusText.Value;
			// The set bonus text is only ever shown to the local player. Keybinds don't exist on a dedicated server
			// (reading them there throws), so never touch them outside the local client.
			if (this is RubberBandingBanditWarpaint && !Main.dedServ && player.whoAmI == Main.myPlayer) {
				string key = Language.GetTextValue("Mods.BagofChaos.Keybinds.Unbound");
				try {
					var keys = Common.Systems.ChaosKeybinds.WarpaintMagnet?.GetAssignedKeys();
					if (keys != null && keys.Count > 0)
						key = keys[0];
				}
				catch (System.Collections.Generic.KeyNotFoundException) {
				}
				player.setBonus += "\n" + WarpaintBonusText.Format(key);
			}
			player.GetModPlayer<ChaosPlayer>().BanditSetBonus = true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.HallowedBar, 8).AddIngredient(ItemID.PinkGel, 10).AddIngredient(ItemID.Silk, 5).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class RubberBandingBanditMask : BanditHelmet
	{
		public override void UpdateEquip(Player player) {
			player.GetDamage(DamageClass.Ranged) += 0.15f;
			player.GetAttackSpeed(DamageClass.Ranged) += 0.10f;
			player.GetModPlayer<ChaosPlayer>().BanditMask = true;
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class RubberBandingBanditBandana : BanditHelmet
	{
		public override void UpdateEquip(Player player) {
			player.GetDamage(DamageClass.Magic) += 0.15f;
			player.manaCost -= 0.12f;
			player.GetModPlayer<ChaosPlayer>().BanditBandana = true;
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class RubberBandingBanditGoggles : BanditHelmet
	{
		public override void UpdateEquip(Player player) {
			player.GetDamage(DamageClass.Summon) += 0.15f;
			player.maxMinions += 2;
			player.GetModPlayer<ChaosPlayer>().BanditGoggles = true;
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class RubberBandingBanditWarpaint : BanditHelmet
	{
		public override void UpdateEquip(Player player) {
			player.GetDamage(DamageClass.Melee) += 0.15f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.10f;
			player.GetModPlayer<ChaosPlayer>().BanditWarpaint = true;
		}
	}

	[AutoloadEquip(EquipType.Body)]
	public class RubberBandingBanditSuit : ModItem
	{
		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 20;
			Item.defense = 14;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 4);
		}

		public override void UpdateEquip(Player player) {
			player.GetDamage(DamageClass.Generic) += 0.08f;
			player.GetModPlayer<ChaosPlayer>().BanditSuit = true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.HallowedBar, 14).AddIngredient(ItemID.PinkGel, 20).AddIngredient(ItemID.Silk, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	[AutoloadEquip(EquipType.Legs)]
	public class RubberBandingBanditShoes : ModItem
	{
		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 18;
			Item.defense = 10;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 3);
		}

		public override void UpdateEquip(Player player) {
			player.GetDamage(DamageClass.Generic) += 0.06f;
			player.moveSpeed += 0.15f;
			player.GetModPlayer<ChaosPlayer>().BanditShoes = true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.HallowedBar, 10).AddIngredient(ItemID.PinkGel, 15).AddIngredient(ItemID.Silk, 5).AddTile(TileID.MythrilAnvil).Register();
		}
	}
}
