using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Mounts
{
	public abstract class ChaosMountItem : ModItem
	{
		protected abstract int MountType { get; }
		protected abstract int BuffType { get; }

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 22;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
			Item.UseSound = SoundID.Item79;
			Item.noMelee = true;
			Item.mountType = MountType;
		}
	}

	// "Harpy" Wings: a pre-hardmode wing accessory. A harpy grabs your shoulders and flaps like her life depends on it,
	// which is enough to glide and nothing more. No fall damage. Running, jumping and boots all still work normally.
	[AutoloadEquip(EquipType.Wings)]
	public class HarpyWings : ModItem
	{
		public override void SetStaticDefaults() {
			// Almost no flight time: it exists so vanilla's glide (hold jump while falling) kicks in.
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(2, 3f, 1f);
		}

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 22;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}

		public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend) {
			// She tries. She really does.
			ascentWhenFalling = 0.2f;
			ascentWhenRising = 0.02f;
			maxCanAscendMultiplier = 0.1f;
			maxAscentMultiplier = 0.2f;
			constantAscend = 0.02f;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			player.noFallDmg = true;
			var chaos = player.GetModPlayer<Common.Players.ChaosPlayer>();
			chaos.HarpyWings = true;
			chaos.HarpyWingsVisible |= !hideVisual;
		}

		public override void UpdateVanity(Player player) => player.GetModPlayer<Common.Players.ChaosPlayer>().HarpyWingsVisible = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Feather, 15).AddIngredient(ItemID.Rope, 20).AddIngredient(ItemID.Cloud, 10).AddTile(TileID.Anvils).Register();
		}
	}

	// Goes in the minecart equipment slot like any vanilla minecart: hop on a track and you're riding it.
	public class RatCart : ModItem
	{
		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.mountType = ModContent.MountType<Content.Mounts.RatCartMount>();
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Minecart).AddIngredient(ItemID.Leather, 5).AddIngredient(ItemID.RottenChunk, 5).AddTile(TileID.Anvils).Register();
		}
	}

	public class Sharkboard : ChaosMountItem
	{
		protected override int MountType => ModContent.MountType<Content.Mounts.SharkboardMount>();
		protected override int BuffType => ModContent.BuffType<Buffs.SharkboardBuff>();
	}
}
