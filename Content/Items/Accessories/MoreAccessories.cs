using System;
using BagofChaos.Common.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Accessories
{
	// ---------------------------------------------------------------------------------------------------------------
	// Triforce pieces: convert damage types. Hardmode, pre-mech.
	// Conversion is real: the weapon (and its ammo and projectiles) becomes the new class, so the new class's bonuses
	// apply and the old class's don't. See ChaosPlayer.Round4 / TriforceGlobalItem.
	// ---------------------------------------------------------------------------------------------------------------
	public abstract class TriforcePiece : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.LightRed;
		protected override int Value => Item.buyPrice(gold: 5);
	}

	public class TriforceOfWisdom : TriforcePiece
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TriforceSources.Add(DamageConversion.Magic);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SoulofLight, 10).AddIngredient(ItemID.Sapphire, 3).AddIngredient(ItemID.CrystalShard, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public class TriforceOfPower : TriforcePiece
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TriforceSources.Add(DamageConversion.Melee);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SoulofNight, 10).AddIngredient(ItemID.Ruby, 3).AddIngredient(ItemID.CrystalShard, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public class TriforceOfCourage : TriforcePiece
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TriforceSources.Add(DamageConversion.Ranged);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SoulofFlight, 10).AddIngredient(ItemID.Emerald, 3).AddIngredient(ItemID.CrystalShard, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Every vanilla damage class becomes the one you pick (right click to cycle Melee, Ranged, Magic, Summoner). No penalty.
	// Only the matching segment of the sprite lights up: red top (melee), blue lower left (magic), green lower right (ranged).
	// Summoner turns the whole thing gold.
	public class CompleteTriforceOfHyrule : TriforcePiece
	{
		protected override int Rarity => ItemRarityID.Yellow;

		public override void UpdateAccessory(Player player, bool hideVisual) {
			ChaosPlayer chaos = Chaos(player);
			chaos.CompleteTriforce = true;
		}

		private static Texture2D ModeTexture() {
			DamageConversion mode = Main.gameMenu ? DamageConversion.Melee : Main.LocalPlayer.GetModPlayer<ChaosPlayer>().HyruleMode;
			return ModContent.Request<Texture2D>("BagofChaos/Content/Items/Accessories/CompleteTriforce_" + mode).Value;
		}

		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			Texture2D texture = ModeTexture();
			spriteBatch.Draw(texture, position, texture.Bounds, drawColor, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			return false;
		}

		public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
			Texture2D texture = ModeTexture();
			Vector2 center = Item.Center - Main.screenPosition;
			spriteBatch.Draw(texture, center, null, lightColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			return false;
		}

		public override bool CanRightClick() => true;

		public override bool ConsumeItem(Player player) => false;

		public override void RightClick(Player player) {
			ChaosPlayer chaos = Chaos(player);
			chaos.HyruleMode = chaos.HyruleMode switch {
				DamageConversion.Melee => DamageConversion.Ranged,
				DamageConversion.Ranged => DamageConversion.Magic,
				DamageConversion.Magic => DamageConversion.Summon,
				_ => DamageConversion.Melee,
			};
			Color color = chaos.HyruleMode switch { DamageConversion.Magic => Color.DeepSkyBlue, DamageConversion.Melee => Color.OrangeRed, DamageConversion.Ranged => Color.LimeGreen, _ => Color.Gold };
			Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.Items.CompleteTriforceOfHyrule.Mode" + chaos.HyruleMode), color);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<TriforceOfWisdom>().AddIngredient<TriforceOfPower>().AddIngredient<TriforceOfCourage>().AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public enum DamageConversion
	{
		None,
		Magic,
		Melee,
		Ranged,
		Summon,
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Stalemate: nobody hurts anybody. Debuffs, traps and the environment still count.
	// ---------------------------------------------------------------------------------------------------------------
	public class Stalemate : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Pink;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).Stalemate = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.HallowedBar, 10).AddIngredient(ItemID.SoulofSight, 5).AddIngredient(ItemID.SoulofMight, 5).AddIngredient(ItemID.SoulofFright, 5).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Loot Scanner: enemies tagged by whips drop double loot.
	public class LootScanner : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.LightRed;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).LootScanner = true;

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup("BagofChaos:TitaniumBar", 10).AddIngredient(ItemID.Wire, 20).AddIngredient(ItemID.Lens, 2).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Touch Grass: +5 life regen while standing on grass. Found in surface chests.
	public class TouchGrass : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TouchGrass = true;
	}

	// Slippery Frog Shoes: your Y position never changes. Walk on air. Never go up or down again.
	public class SlipperyFrogShoes : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).FrogShoes = true;
	}

	// N-Terraria: inverts every colour on screen. Cursed. Cosmetic.
	public class NTerraria : ChaosAccessory
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.vanity = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).NTerraria = true;

		public override void UpdateVanity(Player player) => Chaos(player).NTerraria = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.NegativeDye, 2).AddIngredient(ItemID.Glass, 5).AddTile(TileID.DyeVat).Register();
		}
	}

	// Unstable Concoction: potions don't heal; while sick you can't die and get random power; when it wears off you explode. And die.
	public class UnstableConcoction : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Pink;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).UnstableConcoction = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.GreaterHealingPotion, 5).AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Dynamite, 3).AddIngredient(ItemID.SoulofNight, 5).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Team Tether: a grappling hook you can't aim. It hooks low-health allies and heals them.
	public class TeamTether : ModItem
	{
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.GrapplingHook);
			Item.shoot = ProjectileID.Hook;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 5);
		}

		public override bool CanUseItem(Player player) => false; // automatic only

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.GrapplingHook).AddIngredient(ItemID.HealingPotion, 10).AddIngredient(ItemID.Chain, 10).AddTile(TileID.Anvils).Register();
		}
	}

	// Balls of Spiders: wings. Constantly drop spiders while flying; cling to walls.
	[AutoloadEquip(EquipType.Wings)]
	public class BallsOfSpiders : ModItem
	{
		public override void SetStaticDefaults() {
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(100, 6.25f, 1f); // same as Leaf Wings
			ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<Pets.SpiderOnABallItem>();
		}

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 20;
			Item.value = Item.buyPrice(gold: 10);
			Item.rare = ItemRarityID.LightRed;
			Item.accessory = true;
		}

		public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend) {
			ascentWhenFalling = 0.85f;
			ascentWhenRising = 0.15f;
			maxCanAscendMultiplier = 1f;
			maxAscentMultiplier = 2.5f;
			constantAscend = 0.125f;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			ChaosPlayer chaos = player.GetModPlayer<ChaosPlayer>();
			chaos.SpiderWings = true;
			// A fading trail of little spiders while airborne, like vanilla wing particles.
			if (!hideVisual && !Main.dedServ && player.velocity.Y != 0f && !player.mount.Active && Main.rand.NextBool(2)) {
				Vector2 from = player.Center + new Vector2(-player.direction * 10f, Main.rand.NextFloat(-8f, 8f));
				Dust.NewDustPerfect(from, ModContent.DustType<Dusts.SpiderDust>(), new Vector2(-player.velocity.X * 0.15f + Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.2f, 1.2f)));
			}
		}
	}
}
