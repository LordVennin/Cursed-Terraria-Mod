using Terraria;
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

	public class HarpyWings : ChaosMountItem
	{
		protected override int MountType => ModContent.MountType<Content.Mounts.HarpyWingsMount>();
		protected override int BuffType => ModContent.BuffType<Buffs.HarpyMountBuff>();

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Feather, 15).AddIngredient(ItemID.Rope, 20).AddIngredient(ItemID.Cloud, 10).AddTile(TileID.Anvils).Register();
		}
	}

	public class RatCart : ChaosMountItem
	{
		protected override int MountType => ModContent.MountType<Content.Mounts.RatCartMount>();
		protected override int BuffType => ModContent.BuffType<Buffs.RatCartBuff>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.Green;
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
