using BagofChaos.Common.GlobalProjectiles;
using BagofChaos.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Weapons
{
	// Like the paint sprayer, but glue. Sticks NPCs to NPCs, NPCs to players, players to players. No refunds.
	public class GlueGun : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ModContent.ProjectileType<GlueGlob>(), AmmoID.None, singleShotTime: 12, shotVelocity: 10f, hasAutoReuse: true);
			Item.width = 44;
			Item.height = 22;
			Item.damage = 4;
			Item.knockBack = 0.5f;
			Item.UseSound = SoundID.Item17;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 2);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.PaintSprayer).AddIngredient(ItemID.Gel, 50).AddIngredient(ItemID.Bottle, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// Every enemy hit spawns a tin can next to it. Shoot the cans. They explode.
	public class ChainCan : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ProjectileID.PurificationPowder, AmmoID.Bullet, singleShotTime: 9, shotVelocity: 11f, hasAutoReuse: true);
			Item.width = 48;
			Item.height = 22;
			Item.damage = 12;
			Item.knockBack = 2f;
			Item.UseSound = SoundID.Item11;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 5);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-6f, 0f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			int index = Projectile.NewProjectile(source, position, velocity.RotatedByRandom(MathHelper.ToRadians(6)), type, damage, knockback, player.whoAmI);
			if (index >= 0 && index < Main.maxProjectiles)
				Main.projectile[index].GetGlobalProjectile<ChaosGlobalProjectile>().FromChainCan = true;
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Minishark).AddIngredient(ItemID.TinCan, 25).AddIngredient(ItemID.Chain, 10).AddTile(TileID.Anvils).Register();
		}
	}

	// Kept you waiting, huh? Every bullet applies Venom.
	public class VenomSnake : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ProjectileID.PurificationPowder, AmmoID.Bullet, singleShotTime: 14, shotVelocity: 12f, hasAutoReuse: true);
			Item.width = 44;
			Item.height = 22;
			Item.damage = 34;
			Item.knockBack = 3f;
			Item.UseSound = SoundID.Item41;
			Item.rare = ItemRarityID.LightPurple;
			Item.value = Item.buyPrice(gold: 8);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			int index = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
			if (index >= 0 && index < Main.maxProjectiles)
				Main.projectile[index].GetGlobalProjectile<ChaosGlobalProjectile>().Venomous = true;
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Handgun).AddIngredient(ItemID.VialofVenom, 15).AddIngredient(ItemID.EyePatch).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Shoots rats. High speed rats. They die on impact and poison whatever they hit. Uses bullets as fuel, somehow.
	public class RattlingGun : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ModContent.ProjectileType<RatBullet>(), AmmoID.Bullet, singleShotTime: 7, shotVelocity: 14f, hasAutoReuse: true);
			Item.width = 44;
			Item.height = 22;
			Item.damage = 11;
			Item.knockBack = 1.5f;
			Item.UseSound = SoundID.Item36;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(gold: 3);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
			type = ModContent.ProjectileType<RatBullet>(); // whatever you loaded, it's rats now
			velocity = velocity.RotatedByRandom(MathHelper.ToRadians(10));
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Musket).AddIngredient(ItemID.RottenChunk, 10).AddIngredient(ItemID.Leather, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// Duplicates any NPC it hits. Any NPC. Think about that before using it on a boss.
	public class ToolGun : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ModContent.ProjectileType<ToolGunBolt>(), AmmoID.None, singleShotTime: 30, shotVelocity: 12f, hasAutoReuse: false);
			Item.DamageType = DamageClass.Generic;
			Item.width = 44;
			Item.height = 22;
			Item.damage = 0;
			Item.knockBack = 0f;
			Item.UseSound = SoundID.Item92;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 10);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Wrench).AddIngredient(ItemID.IllegalGunParts).AddIngredient(ItemID.Wire, 50).AddTile(TileID.Anvils).Register();
		}
	}

	public class ToolMinigun : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ModContent.ProjectileType<ToolGunBolt>(), AmmoID.None, singleShotTime: 4, shotVelocity: 14f, hasAutoReuse: true);
			Item.DamageType = DamageClass.Generic;
			Item.width = 52;
			Item.height = 22;
			Item.damage = 0;
			Item.knockBack = 0f;
			Item.UseSound = SoundID.Item92 with { Volume = 0.5f };
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(gold: 40);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-8f, 0f);

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
			velocity = velocity.RotatedByRandom(MathHelper.ToRadians(2));
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<ToolGun>().AddIngredient(ItemID.ChainGun).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// A rocket launcher that deals normal damage... unless the end credits are rolling. Then it's 100x. The game is over; go nuts.
	public class CreditCannon : ModItem
	{
		public override void SetStaticDefaults() {
			AmmoID.Sets.SpecificLauncherAmmoProjectileFallback[Type] = ItemID.RocketLauncher;
		}

		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ProjectileID.RocketI, AmmoID.Rocket, singleShotTime: 30, shotVelocity: 6f, hasAutoReuse: true);
			Item.width = 52;
			Item.height = 22;
			Item.damage = 150;
			Item.knockBack = 4f;
			Item.UseSound = SoundID.Item11;
			Item.rare = ItemRarityID.Purple;
			Item.value = Item.buyPrice(gold: 25);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-8f, 2f);

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			// The actual credits state, not "has Moon Lord been beaten".
			if (CreditsRollEvent.IsEventOngoing)
				damage *= 100f;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.RocketLauncher).AddIngredient(ItemID.LunarBar, 5).AddIngredient(ItemID.GoldCoin, 10).AddTile(TileID.LunarCraftingStation).Register();
		}
	}
}
