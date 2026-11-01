using System;
using System.Collections.Generic;
using BagofChaos.Common.GlobalNPCs;
using BagofChaos.Common.GlobalProjectiles;
using BagofChaos.Common.Players;
using BagofChaos.Content.Buffs;
using BagofChaos.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Weapons
{
	// ---------------------------------------------------------------------------------------------------------------
	// Tanning Mirror: a cone of light above you; enemies in it get tagged, minions are trapped in it and bounce around.
	// ---------------------------------------------------------------------------------------------------------------
	public class TanningMirror : ModItem
	{
		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 28;
			Item.damage = 30;
			Item.DamageType = DamageClass.Summon;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.noMelee = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 6);
		}

		public override bool CanUseItem(Player player) => false; // it works by being held

		public override void HoldItem(Player player) {
			player.GetModPlayer<ChaosPlayer>().TanningMirrorHeld = true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.MagicMirror).AddIngredient(ItemID.SoulofLight, 10).AddIngredient(ItemID.Glass, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Nail and Hammer: left click throws nails (apply Nails), right click slams a hammer that hurts nailed enemies extra.
	// ---------------------------------------------------------------------------------------------------------------
	public class NailAndHammer : ModItem
	{
		public override void SetDefaults() {
			Item.width = 30;
			Item.height = 30;
			Item.damage = 20;
			Item.DamageType = DamageClass.Melee;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 16;
			Item.useAnimation = 16;
			Item.knockBack = 3f;
			Item.autoReuse = true;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 2);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<ThrownNail>();
			Item.shootSpeed = 12f;
			Item.noMelee = true;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			if (player.altFunctionUse == 2) {
				// Hammer slam: a slow, heavy melee swing with no projectile.
				Item.noMelee = false;
				Item.useTime = 30;
				Item.useAnimation = 30;
				Item.shoot = ProjectileID.None;
				Item.UseSound = SoundID.Item37;
				Item.knockBack = 8f;
			}
			else {
				Item.noMelee = true;
				Item.useTime = 16;
				Item.useAnimation = 16;
				Item.shoot = ModContent.ProjectileType<ThrownNail>();
				Item.UseSound = SoundID.Item1;
				Item.knockBack = 3f;
			}
			return true;
		}

		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) {
			if (target.HasBuff<Nails>())
				modifiers.SourceDamage *= 3f; // the hammer finds the nails
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Spike, 10).AddIngredient(ItemID.Bone, 20).AddRecipeGroup(RecipeGroupID.IronBar, 8).AddTile(TileID.Anvils).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Snail Flail / Hot Snail Flail: killing blows turn enemies into snails and their loot into snails.
	// ---------------------------------------------------------------------------------------------------------------
	public abstract class SnailFlailBase : ModItem
	{
		protected abstract bool Hot { get; }

		public override void SetDefaults() {
			Item.CloneDefaults(Hot ? ItemID.Sunfury : ItemID.BallOHurt);
			Item.shoot = ModContent.ProjectileType<SnailFlailProjectile>();
			Item.damage = Hot ? 45 : 18;
			Item.rare = Hot ? ItemRarityID.LightRed : ItemRarityID.Blue;
			Item.value = Item.buyPrice(gold: Hot ? 5 : 1);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, Hot ? 1f : 0f);
			return false;
		}
	}

	public class SnailFlail : SnailFlailBase
	{
		protected override bool Hot => false;
		public override string Texture => "Terraria/Images/Item_" + ItemID.BallOHurt;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Chain, 10).AddIngredient(ItemID.Snail, 3).AddRecipeGroup(RecipeGroupID.IronBar, 5).AddTile(TileID.Anvils).Register();
	}

	public class HotSnailFlail : SnailFlailBase
	{
		protected override bool Hot => true;
		public override string Texture => "Terraria/Images/Item_" + ItemID.Sunfury;
		public override void AddRecipes() => CreateRecipe().AddIngredient<SnailFlail>().AddIngredient(ItemID.HellstoneBar, 10).AddIngredient(ItemID.MagmaSnail, 1).AddTile(TileID.MythrilAnvil).Register();
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Balls of Confetti: Light Disc that does 1 damage, pierces infinitely, ignores i-frames and defense. Stacks to 10.
	// ---------------------------------------------------------------------------------------------------------------
	public class BallsOfConfetti : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.LightDisc;

		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.LightDisc);
			Item.damage = 1;
			Item.maxStack = 10;
			Item.shoot = ModContent.ProjectileType<ConfettiDisc>();
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 2);
		}

		public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] < Item.stack;

		public override bool PreDrawInInventory(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			spriteBatch.Draw(Terraria.GameContent.TextureAssets.Item[Type].Value, position, frame, drawColor.MultiplyRGB(Main.DiscoColor), 0f, origin, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
			return false;
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Axe Me a Question / Axe Me Lots of Questions: throwable axes that chop trees and confuse enemies.
	// ---------------------------------------------------------------------------------------------------------------
	public abstract class ThrowableAxe : ModItem
	{
		protected abstract bool Hardmode { get; }

		public override void SetDefaults() {
			Item.width = 30;
			Item.height = 30;
			Item.damage = Hardmode ? 48 : 16;
			Item.DamageType = DamageClass.Melee;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.knockBack = 5f;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.rare = Hardmode ? ItemRarityID.LightRed : ItemRarityID.Blue;
			Item.value = Item.buyPrice(gold: Hardmode ? 4 : 1);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<ThrownAxe>();
			Item.shootSpeed = Hardmode ? 14f : 10f;
			Item.axe = 15; // 75% axe power (axe is stored /5)
		}

		public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] < 1;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, Hardmode ? 1f : 0f);
			return false;
		}
	}

	public class AxeMeAQuestion : ThrowableAxe
	{
		protected override bool Hardmode => false;
		public override string Texture => "Terraria/Images/Item_" + ItemID.CopperAxe;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.CopperAxe).AddIngredient(ItemID.Rope, 10).AddRecipeGroup(RecipeGroupID.Wood, 5).AddTile(TileID.WorkBenches).Register();
	}

	public class AxeMeLotsOfQuestions : ThrowableAxe
	{
		protected override bool Hardmode => true;
		public override string Texture => "Terraria/Images/Item_" + ItemID.AcornAxe;
		public override void AddRecipes() => CreateRecipe().AddIngredient<AxeMeAQuestion>().AddIngredient(ItemID.AcornAxe).AddIngredient(ItemID.SoulofMight, 5).AddTile(TileID.MythrilAnvil).Register();
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Rat Tacks: throw 2-3 rats that lie on the ground and bite anything that steps on them. Right click toggles melee/summon.
	// ---------------------------------------------------------------------------------------------------------------
	public class RatTacks : ModItem
	{
		public override string Texture => "BagofChaos/Content/Projectiles/RatBullet";

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 12;
			Item.damage = 14;
			Item.DamageType = DamageClass.Melee;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 25;
			Item.useAnimation = 25;
			Item.knockBack = 1f;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(silver: 80);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<RatTack>();
			Item.shootSpeed = 7f;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			if (player.altFunctionUse == 2) {
				if (player.ItemTimeIsZero) {
					ChaosPlayer chaos = player.GetModPlayer<ChaosPlayer>();
					chaos.RatTacksSummon = !chaos.RatTacksSummon;
					Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.Items.RatTacks." + (chaos.RatTacksSummon ? "ModeSummon" : "ModeMelee")), Color.Gray);
				}
				return false;
			}
			Item.DamageType = player.GetModPlayer<ChaosPlayer>().RatTacksSummon ? DamageClass.Summon : DamageClass.Melee;
			return player.ownedProjectileCounts[Item.shoot] < 20;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			int count = Main.rand.Next(2, 4);
			for (int i = 0; i < count && player.ownedProjectileCounts[type] + i < 20; i++)
				Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.8f, 1.2f), type, damage, knockback, player.whoAmI);
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.RottenChunk, 10).AddIngredient(ItemID.Spike, 5).AddTile(TileID.WorkBenches).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Doot Doot: a repeater that fires skeletons. The whole skeleton.
	// ---------------------------------------------------------------------------------------------------------------
	public class DootDoot : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.PlatinumBow;

		public override void SetDefaults() {
			Item.DefaultToRangedWeapon(ProjectileID.WoodenArrowFriendly, AmmoID.Arrow, singleShotTime: 25, shotVelocity: 8f, hasAutoReuse: true);
			Item.width = 30;
			Item.height = 50;
			Item.damage = 10;
			Item.knockBack = 2f;
			Item.UseSound = SoundID.Item5;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 2);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			// The arrow becomes a skeleton. Launched. Hostile. You did this.
			BagofChaos.SendSpawnNPC(NPCID.Skeleton, position + velocity * 3f, -1, velocity * 1.5f);
			SoundEngine.PlaySound(SoundID.NPCDeath2, position);
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Bone, 30).AddRecipeGroup(RecipeGroupID.Wood, 10).AddIngredient(ItemID.Cobweb, 10).AddTile(TileID.Anvils).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Chained Chainsaw: a cobalt chainsaw on a chain. Yoyo.
	// ---------------------------------------------------------------------------------------------------------------
	public class ChainedChainsaw : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.CobaltChainsaw;

		public override void SetStaticDefaults() {
			ItemID.Sets.Yoyo[Type] = true;
			ItemID.Sets.GamepadExtraRange[Type] = 15;
			ItemID.Sets.GamepadSmartQuickReach[Type] = true;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 25;
			Item.useAnimation = 25;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item23;
			Item.damage = 40;
			Item.DamageType = DamageClass.MeleeNoSpeed;
			Item.knockBack = 2.5f;
			Item.channel = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 5);
			Item.shoot = ModContent.ProjectileType<ChainsawYoyo>();
			Item.shootSpeed = 16f;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.CobaltChainsaw).AddIngredient(ItemID.Chain, 15).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Gravity Flail: hit enemies get slammed into the ground. Fall damage for them. Uncapped.
	// ---------------------------------------------------------------------------------------------------------------
	public class GravityFlail : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.DaoofPow;

		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.DaoofPow);
			Item.shoot = ModContent.ProjectileType<GravityFlailProjectile>();
			Item.damage = 90;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(gold: 20);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.DaoofPow).AddIngredient(ItemID.LunarBar, 8).AddIngredient(ItemID.Chain, 20).AddTile(TileID.LunarCraftingStation).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// BBQ Skewer: hold to stick out a skewer. Enemies stabbed are stuck on it and cook slowly. It gets longer every time.
	// ---------------------------------------------------------------------------------------------------------------
	public class BBQSkewer : ModItem
	{
		public override void SetDefaults() {
			Item.width = 40;
			Item.height = 40;
			Item.damage = 25;
			Item.DamageType = DamageClass.Melee;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.knockBack = 0f;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.channel = true;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 6);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<SkewerProjectile>();
			Item.shootSpeed = 1f;
		}

		public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] < 1;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.HallowedBar, 10).AddIngredient(ItemID.Chain, 5).AddIngredient(ItemID.AshBlock, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Stacks of Fish: three goldfish on your head per summon slot. They stack.
	// ---------------------------------------------------------------------------------------------------------------
	public class StacksOfFish : ModItem
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.Goldfish;

		public override void SetStaticDefaults() {
			ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
		}

		public override void SetDefaults() {
			Item.damage = 9;
			Item.knockBack = 1f;
			Item.mana = 10;
			Item.width = 20;
			Item.height = 20;
			Item.useTime = 36;
			Item.useAnimation = 36;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.value = Item.buyPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item44;
			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<FishStackBuff>();
			Item.shoot = ModContent.ProjectileType<FishStackMinion>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			player.AddBuff(Item.buffType, 2);
			for (int i = 0; i < 3; i++)
				Projectile.NewProjectile(source, player.Top, Vector2.Zero, type, damage, knockback, player.whoAmI);
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Goldfish, 3).AddIngredient(ItemID.Bowl).AddTile(TileID.WorkBenches).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Succ.: pulls every town NPC and player on screen to you. Through walls.
	// ---------------------------------------------------------------------------------------------------------------
	public class Succ : ModItem
	{
		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 28;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 45;
			Item.useAnimation = 45;
			Item.noMelee = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 3);
			Item.UseSound = SoundID.Item24;
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return true;
			Rectangle screen = Common.ChaosUtils.ScreenWorldRect;
			foreach (NPC npc in Main.ActiveNPCs) {
				if ((npc.townNPC || npc.friendly) && screen.Intersects(npc.Hitbox)) {
					Vector2 target = player.Center - npc.Size * 0.5f + new Vector2(Main.rand.NextFloat(-24f, 24f), 0f);
					npc.position = target;
					npc.velocity = Vector2.Zero;
					npc.netUpdate = true;
					BagofChaos.SendMoveNPC(npc, target);
				}
			}
			BagofChaos.SendSucc(player.whoAmI, player.Center);
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SoulofNight, 10).AddIngredient(ItemID.Cobweb, 20).AddIngredient(ItemID.Bottle).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Fart Reverb.mp4: charge for up to 20 seconds, then everything within up to 50 tiles explodes. Including you.
	// ---------------------------------------------------------------------------------------------------------------
	public class FartReverbMp4 : ModItem
	{
		public const int MaxCharge = 60 * 20;

		public override void SetDefaults() {
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.channel = true;
			Item.noMelee = true;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 10);
		}

		public override bool CanUseItem(Player player) => player.GetModPlayer<ChaosPlayer>().FartCharge == 0;

		public override void HoldItem(Player player) {
			ChaosPlayer chaos = player.GetModPlayer<ChaosPlayer>();
			if (player.channel && player.itemAnimation > 0 || player.channel && chaos.FartCharge > 0) {
				player.itemAnimation = Math.Max(player.itemAnimation, 2);
				chaos.FartCharge = Math.Min(MaxCharge, chaos.FartCharge + 1);
				if (chaos.FartCharge % 60 == 0 && Main.netMode != NetmodeID.Server)
					SoundEngine.PlaySound(SoundID.Item16 with { Pitch = -0.5f + chaos.FartCharge / (float)MaxCharge, Volume = 0.6f }, player.Center);
				if (chaos.FartCharge == MaxCharge && Main.netMode != NetmodeID.Server)
					SoundEngine.PlaySound(SoundID.Item16 with { Volume = 1.5f, Pitch = -0.8f }, player.Center);
				if (Main.rand.NextBool(3)) {
					Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Smoke, 0f, -1f, 100, new Color(120, 180, 60), 1f + chaos.FartCharge / (float)MaxCharge * 2f);
					dust.noGravity = true;
				}
			}
			else if (chaos.FartCharge > 0) {
				Detonate(player, chaos.FartCharge);
				chaos.FartCharge = 0;
			}
		}

		private static void Detonate(Player player, int charge) {
			float power = charge / (float)MaxCharge;
			int damage = (int)(800 * power);
			int radiusTiles = (int)(50 * power);
			if (Main.netMode != NetmodeID.Server) {
				SoundEngine.PlaySound(SoundID.Item16 with { Volume = 1.5f, Pitch = -1f }, player.Center);
				SoundEngine.PlaySound(SoundID.Item62, player.Center);
				for (int i = 0; i < 80 * power + 10; i++) {
					Dust dust = Dust.NewDustPerfect(player.Center, DustID.Smoke, Main.rand.NextVector2Circular(20f, 20f) * power, 100, new Color(120, 180, 60), 3f);
					dust.noGravity = true;
				}
			}
			if (player.whoAmI == Main.myPlayer) {
				Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.Center, Vector2.Zero, ModContent.ProjectileType<MegaFartExplosion>(), damage, 15f, player.whoAmI, radiusTiles);
				if (power >= 1f)
					player.AddBuff(BuffID.Stoned, 60 * 10); // petrified by your own power
			}
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<FartReverb>().AddRecipeGroup("BagofChaos:CobaltBar", 10).AddRecipeGroup("BagofChaos:MythrilBar", 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Bag of Chaos: randomly uses any item in the game. Extremely fast.
	// ---------------------------------------------------------------------------------------------------------------
	public class BagOfChaosItem : ModItem
	{
		private static List<int> usable;

		public override void SetStaticDefaults() {
			usable = new List<int>();
			for (int type = 1; type < ItemLoader.ItemCount; type++) {
				Item item = ContentSamples.ItemsByType.TryGetValue(type, out Item sample) ? sample : new Item(type);
				if (item.shoot > ProjectileID.None && item.damage > 0 && !item.accessory && item.ammo == AmmoID.None)
					usable.Add(type);
			}
		}

		public override void SetDefaults() {
			Item.width = 30;
			Item.height = 30;
			Item.damage = 50;
			Item.DamageType = DamageClass.Generic;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 3;
			Item.useAnimation = 3;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.rare = ItemRarityID.Purple;
			Item.value = Item.buyPrice(gold: 50);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 10f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			if (usable == null || usable.Count == 0)
				return true;
			int itemType = usable[Main.rand.Next(usable.Count)];
			Item sample = ContentSamples.ItemsByType[itemType];
			int proj = sample.shoot;
			// Ammo-using weapons (guns, bows) have a placeholder "shoot"; give them something real to fire.
			if (sample.useAmmo == AmmoID.Bullet) proj = ProjectileID.Bullet;
			else if (sample.useAmmo == AmmoID.Arrow) proj = ProjectileID.WoodenArrowFriendly;
			else if (sample.useAmmo == AmmoID.Rocket) proj = ProjectileID.RocketI;
			else if (sample.useAmmo != AmmoID.None) proj = ProjectileID.Bullet;
			float speed = sample.shootSpeed > 0f ? sample.shootSpeed : 10f;
			Vector2 dir = velocity.SafeNormalize(Vector2.UnitX) * speed;
			Projectile.NewProjectile(source, position, dir, proj, Math.Max(damage, sample.damage), knockback + sample.knockBack, player.whoAmI);
			if (sample.UseSound.HasValue)
				SoundEngine.PlaySound(sample.UseSound.Value, position);
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.LunarBar, 10).AddIngredient(ItemID.FragmentSolar, 5).AddIngredient(ItemID.FragmentNebula, 5).AddIngredient(ItemID.FragmentVortex, 5).AddIngredient(ItemID.FragmentStardust, 5).AddTile(TileID.LunarCraftingStation).Register();
		}
	}
}
