using BagofChaos.Content.NPCs.Bosses;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Consumables
{
	public abstract class ChaosBossSummon : ModItem
	{
		protected abstract int BossType { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 20;
			Item.rare = ItemRarityID.Blue;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
			Item.UseSound = SoundID.Roar;
		}

		public override bool CanUseItem(Player player) => !NPC.AnyNPCs(BossType);

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer)
				BagofChaos.SendSpawnBoss(player.whoAmI, BossType);
			return true;
		}
	}

	// Chaotic Gel: summons the Chaotic Slime.
	public class ChaoticGel : ChaosBossSummon
	{
		protected override int BossType => ModContent.NPCType<ChaoticSlime>();
		public override bool CanUseItem(Player player) => base.CanUseItem(player) && Main.hardMode;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Gel, 50).AddIngredient(ItemID.PinkGel, 10).AddIngredient(ItemID.SoulofLight, 5).AddTile(TileID.DemonAltar).Register();
	}

	// Slot Machine Token: summons the Slot-bar Caterpillar.
	public class SlotMachineToken : ChaosBossSummon
	{
		protected override int BossType => ModContent.NPCType<SlotBarCaterpillar>();
		public override bool CanUseItem(Player player) => base.CanUseItem(player) && Main.hardMode;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.GoldCoin, 5).AddIngredient(ItemID.SoulofNight, 10).AddIngredient(ItemID.Wire, 20).AddTile(TileID.DemonAltar).Register();
	}

	// Bone Pile: summons the Bonemass directly (it can also replace Skeletron on its own).
	public class BonePile : ChaosBossSummon
	{
		protected override int BossType => ModContent.NPCType<BonemassOfSkeletron>();
		public override bool CanUseItem(Player player) => base.CanUseItem(player) && !Main.dayTime;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bone, 50).AddIngredient(ItemID.Lens, 3).AddTile(TileID.DemonAltar).Register();
	}

	// ---- Treasure bags ----
	public abstract class ChaosBossBag : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.BossBag[Type] = true;
		}

		public override void SetDefaults() {
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.width = 24;
			Item.height = 24;
			Item.rare = ItemRarityID.Purple;
			Item.expert = true;
		}

		public override bool CanRightClick() => true;
	}

	public class CaterpillarBag : ChaosBossBag
	{
		public override void ModifyItemLoot(ItemLoot itemLoot) {
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Weapons.SlotterPillar>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Ammo.SlottingPillarAmmo>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RatPackageCo>()));
			itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 20, 40));
		}
	}

	public class ChaoticSlimeBag : ChaosBossBag
	{
		public override void ModifyItemLoot(ItemLoot itemLoot) {
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Tools.ChaoticDashBox>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Tools.ChaoticBanker>()));
			itemLoot.Add(ItemDropRule.Common(ItemID.Gel, 1, 100, 200));
			itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 10, 25));
		}
	}
}
