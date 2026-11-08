using BagofChaos.Common.Players;
using BagofChaos.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Weapons
{
	// Wob: a bow, backwards. Fires arrows out of the back of the bow, i.e. directly away from where you're aiming.
	public class Wob : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.WoodenBow;

		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.WoodenBow);
			Item.damage = 11;
			Item.rare = ItemRarityID.White;
			Item.value = Item.sellPrice(silver: 30);
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
			velocity = -velocity;
			position = player.MountedCenter + velocity.SafeNormalize(Vector2.Zero) * 12f; // out the back
		}

		public override Vector2? HoldoutOffset() => new Vector2(2f, 0f);

		// It even looks backwards in the inventory.
		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			Texture2D texture = Terraria.GameContent.TextureAssets.Item[Type].Value;
			spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale, SpriteEffects.FlipHorizontally, 0f);
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.WoodenBow).AddRecipeGroup(RecipeGroupID.Wood, 5).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Slam Dunker: left click puts down a basketball hoop sentry that shoots basketballs at enemies.
	// Right click next to one of its basketballs to slam-dunk it into your nearest hoop, which answers with a swarm of
	// homing basketballs.
	public class SlamDunker : ModItem
	{
		public const float SlamReach = 6f * 16f;

		public override void SetStaticDefaults() {
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults() {
			Item.damage = 38;
			Item.knockBack = 4f;
			Item.mana = 10;
			Item.width = 30;
			Item.height = 30;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.sentry = true;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 8);
			Item.UseSound = SoundID.Item44;
			Item.shoot = ModContent.ProjectileType<DunkHoop>();
		}

		public override bool AltFunctionUse(Player player) => true;

		public static Projectile NearestBall(Player player) {
			Projectile best = null;
			float bestDistance = SlamReach;
			foreach (Projectile p in Main.ActiveProjectiles) {
				if (p.owner != player.whoAmI || p.type != ModContent.ProjectileType<Basketball>())
					continue;
				float d = Vector2.Distance(p.Center, player.Center);
				if (d < bestDistance) { bestDistance = d; best = p; }
			}
			return best;
		}

		public static Projectile NearestHoop(Player player) {
			Projectile best = null;
			float bestDistance = float.MaxValue;
			foreach (Projectile p in Main.ActiveProjectiles) {
				if (p.owner != player.whoAmI || p.type != ModContent.ProjectileType<DunkHoop>())
					continue;
				float d = Vector2.Distance(p.Center, player.Center);
				if (d < bestDistance) { bestDistance = d; best = p; }
			}
			return best;
		}

		public override bool CanUseItem(Player player) {
			if (player.altFunctionUse == 2) {
				Item.useStyle = ItemUseStyleID.Swing;
				Item.UseSound = SoundID.Item1;
				return NearestBall(player) != null && NearestHoop(player) != null;
			}
			Item.UseSound = SoundID.Item44;
			return true;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			if (player.altFunctionUse == 2) {
				Slam(player, source, damage, knockback);
				return false;
			}
			Vector2 at = Main.MouseWorld;
			player.LimitPointToPlayerReachableArea(ref at);
			Projectile.NewProjectile(source, at, Vector2.Zero, type, damage, knockback, player.whoAmI);
			player.UpdateMaxTurrets();
			return false;
		}

		private static void Slam(Player player, IEntitySource source, int damage, float knockback) {
			Projectile ball = NearestBall(player);
			Projectile hoop = NearestHoop(player);
			if (ball == null || hoop == null)
				return;
			// A line of dust from the ball to the rim: the dunk.
			for (float t = 0f; t <= 1f; t += 0.05f) {
				Dust dust = Dust.NewDustPerfect(Vector2.Lerp(ball.Center, hoop.Center, t), DustID.OrangeTorch, Vector2.Zero, 0, default, 1.4f);
				dust.noGravity = true;
			}
			ball.Kill();
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.3f }, hoop.Center);
			for (int i = 0; i < 8; i++) {
				Vector2 velocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 9f;
				Projectile.NewProjectile(source, hoop.Center, velocity, ModContent.ProjectileType<HomingBasketball>(), (int)(damage * 1.25f), knockback, player.whoAmI);
			}
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.BeachBall).AddIngredient(ItemID.SoulofLight, 8).AddRecipeGroup("BagofChaos:CobaltBar", 10).AddIngredient(ItemID.Silk, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}
}

namespace BagofChaos.Content.Items.Accessories
{
	// Ocean in a Fishbowl: as far as you're concerned, air is water. You swim, you sink slowly, water mounts and gear work
	// everywhere, and you drown in the open air unless you can breathe water (Gills, Neptune's Shell, ...).
	public class OceanInAFishbowl : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.FishBowl;

		public override void SetDefaults() {
			Item.DefaultToAccessory(26, 26);
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 3);
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			player.GetModPlayer<ChaosPlayer>().OceanFishbowl = true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.FishBowl).AddIngredient(ItemID.WaterBucket).AddIngredient(ItemID.Coral, 5).AddTile(TileID.Sinks).Register();
		}
	}
}
