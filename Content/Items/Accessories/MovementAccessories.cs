using System.Collections.Generic;
using BagofChaos.Common.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Accessories
{
	public abstract class ChaosAccessory : ModItem
	{
		protected virtual int Rarity => ItemRarityID.Green;
		protected virtual int Value => Item.buyPrice(gold: 2);

		public override void SetDefaults() {
			Item.DefaultToAccessory(24, 24);
			Item.rare = Rarity;
			Item.value = Value;
		}

		protected ChaosPlayer Chaos(Player player) => player.GetModPlayer<ChaosPlayer>();
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Instant movement, three tiers
	// ----------------------------------------------------------------------------------------------------------------
	public class InstantUpDown : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).InstantUpDown = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.ClimbingClaws).AddRecipeGroup(RecipeGroupID.IronBar, 10).AddTile(TileID.Anvils).Register();
		}
	}

	public class InstantLeftRight : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).InstantLeftRight = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Aglet).AddRecipeGroup(RecipeGroupID.IronBar, 10).AddTile(TileID.Anvils).Register();
		}
	}

	public class UnreasonableUpDown : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Pink;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).UnreasonableUpDown = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<InstantUpDown>().AddIngredient(ItemID.SoulofFlight, 15).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public class UnreasonableLeftRight : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Pink;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).UnreasonableLeftRight = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<InstantLeftRight>().AddIngredient(ItemID.SoulofMight, 15).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public class InstantOmniMovement : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Yellow;
		protected override int Value => Item.buyPrice(gold: 20);

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).OmniMovement = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<UnreasonableUpDown>().AddIngredient<UnreasonableLeftRight>().AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Sky in a Bottle: every double jump becomes a cloud platform under your feet.
	// ----------------------------------------------------------------------------------------------------------------
	public class SkyInABottle : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) {
			Chaos(player).SkyInABottle = true;
			player.GetJumpState<SkyJump>().Enable(); // guarantees at least one cloud even with no other bottle
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.CloudinaBottle).AddIngredient(ItemID.Cloud, 10).AddIngredient(ItemID.Bottle).AddTile(TileID.Anvils).Register();
		}
	}

	public class SkyJump : ExtraJump
	{
		public override Position GetDefaultPosition() => new After(CloudInABottle);

		public override float GetDurationMultiplier(Player player) => 0.01f; // ChaosPlayer sets it to 0 anyway
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Zig / Zag / Zigzag
	// ----------------------------------------------------------------------------------------------------------------
	public class BottleOfZig : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => player.GetJumpState<ZigJump>().Enable();

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.CloudinaBottle).AddIngredient(ItemID.Feather, 5).AddTile(TileID.Anvils).Register();
		}
	}

	public class BottleOfZag : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => player.GetJumpState<ZagJump>().Enable();

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.CloudinaBottle).AddIngredient(ItemID.FallenStar, 5).AddTile(TileID.Anvils).Register();
		}
	}

	public class BottleOfZigzag : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.LightRed;

		public override void UpdateAccessory(Player player, bool hideVisual) => player.GetJumpState<ZigzagJump>().Enable();

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<BottleOfZig>().AddIngredient<BottleOfZag>().AddTile(TileID.Anvils).Register();
		}
	}

	public abstract class DashJump : ExtraJump
	{
		protected abstract float VerticalSign(Player player);

		public override Position GetDefaultPosition() => new After(BlizzardInABottle);

		public override IEnumerable<Position> GetModdedConstraints() {
			yield return new Before(ModContent.GetInstance<SkyJump>());
		}

		public override float GetDurationMultiplier(Player player) => 0.1f;

		public override void OnStarted(Player player, ref bool playSound) {
			float dir = player.direction;
			if (player.controlLeft) dir = -1f;
			if (player.controlRight) dir = 1f;
			player.velocity = new Vector2(dir * 15f, VerticalSign(player) * 9f * player.gravDir);
			player.fallStart = (int)(player.position.Y / 16f);
			for (int i = 0; i < 10; i++) {
				Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Cloud, 0f, 0f, 100, Color.White, 1.5f);
				dust.noGravity = true;
				dust.velocity = -player.velocity * 0.1f;
			}
		}
	}

	public class ZigJump : DashJump
	{
		protected override float VerticalSign(Player player) => -1f; // forward-up
	}

	public class ZagJump : DashJump
	{
		protected override float VerticalSign(Player player) => 1f; // forward-down
	}

	public class ZigzagJump : DashJump
	{
		protected override float VerticalSign(Player player) {
			ChaosPlayer chaos = player.GetModPlayer<ChaosPlayer>();
			chaos.ZigNext = !chaos.ZigNext;
			return chaos.ZigNext ? 1f : -1f;
		}

		public override float GetDurationMultiplier(Player player) => 0.1f;

		public override void OnRefreshed(Player player) {
			// Two charges per landing: one zig, one zag.
			player.GetJumpState<ZigzagJump>().Available = true;
		}
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Rocket Pack / Driller Pack: replace the dash
	// ----------------------------------------------------------------------------------------------------------------
	public class RocketPack : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.LightRed;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).RocketPack = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.RocketBoots).AddIngredient(ItemID.RocketI, 25).AddIngredient(ItemID.Cog, 5).AddTile(TileID.Anvils).Register();
		}
	}

	public class DrillerPack : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).DrillerTier = System.Math.Max(Chaos(player).DrillerTier, 1);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.ShoeSpikes).AddRecipeGroup(RecipeGroupID.IronBar, 20).AddTile(TileID.Anvils).Register();
		}
	}

	public class DrillerPackHellstone : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Orange;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).DrillerTier = System.Math.Max(Chaos(player).DrillerTier, 2);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<DrillerPack>().AddIngredient(ItemID.HellstoneBar, 15).AddTile(TileID.Anvils).Register();
		}
	}

	public class DrillerPackChlorophyte : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Lime;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).DrillerTier = System.Math.Max(Chaos(player).DrillerTier, 3);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<DrillerPackHellstone>().AddIngredient(ItemID.ChlorophyteBar, 15).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public class DrillerPackLuminite : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Red;
		protected override int Value => Item.buyPrice(gold: 30);

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).DrillerTier = 4;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<DrillerPackChlorophyte>().AddIngredient(ItemID.LunarBar, 10).AddTile(TileID.LunarCraftingStation).Register();
		}
	}

	// ----------------------------------------------------------------------------------------------------------------
	// Liquid Snake: full speed through liquids.
	// ----------------------------------------------------------------------------------------------------------------
	public class LiquidSnake : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).LiquidSnake = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Flipper).AddIngredient(ItemID.EyePatch).AddIngredient(ItemID.Silk, 10).AddTile(TileID.Anvils).Register();
		}
	}
}
