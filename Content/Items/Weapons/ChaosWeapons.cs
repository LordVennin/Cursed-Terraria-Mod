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
	// Nagel und Hammer (pre-hardmode, post-Skeletron): left click swings a big Iron Hammer, right click throws nails fast.
	// Nails embed in enemies as a combo marker (up to 15). Each stack adds a flat amount to the hammer's damage against that
	// enemy, and every hammer hit uses up one stack.
	// ---------------------------------------------------------------------------------------------------------------
	public class NailAndHammer : ModItem
	{
		public const int HammerDamage = 24;
		public const int BonusPerStack = 4;
		protected virtual int NailType => ModContent.ProjectileType<ThrownNail>();
		protected virtual int NailUseTime => 8;
		protected virtual float NailSpeed => 13f;
		public override string Texture => "Terraria/Images/Item_" + ItemID.IronHammer;

		public override void SetDefaults() {
			Item.width = 32;
			Item.height = 32;
			Item.damage = HammerDamage;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 6f;
			Item.scale = 1.5f;
			Item.autoReuse = true;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 2);
			SetHammerMode();
		}

		protected virtual void SetHammerMode() {
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.noMelee = false;
			Item.noUseGraphic = false;
			Item.shoot = ProjectileID.None;
			Item.shootSpeed = 0f;
			Item.UseSound = SoundID.Item1;
		}

		private void SetNailMode() {
			Item.useStyle = ItemUseStyleID.Swing; // with no use graphic this is the throwing animation
			Item.useTime = NailUseTime;
			Item.useAnimation = NailUseTime;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = NailType;
			Item.shootSpeed = NailSpeed;
			Item.UseSound = SoundID.Item1;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			if (player.altFunctionUse == 2)
				SetNailMode();
			else
				SetHammerMode();
			return true;
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
			damage = (int)(damage * NailDamageFraction);
			knockback = 1f;
		}

		protected virtual float NailDamageFraction => 0.4f;

		// Hammer hits: bonus first, then one nail comes out. Melee swings hit each enemy once, so one stack per hit.
		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) {
			int stacks = target.GetGlobalNPC<ChaosGlobalNPC>().NailStacks;
			if (stacks > 0)
				modifiers.FlatBonusDamage += stacks * BonusPerStack;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
			target.GetGlobalNPC<ChaosGlobalNPC>().ConsumeNails(1);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Spike, 10).AddIngredient(ItemID.Bone, 20).AddRecipeGroup(RecipeGroupID.IronBar, 8).AddTile(TileID.Anvils).Register();
		}
	}

	// Geschmolzener Nagel und Hammer (early hardmode): left click throws a huge spinning Molten Hamaxe that comes back.
	// Hitting a nailed enemy pulls every nail out at once in a single fixed-damage explosion. Right click throws slower,
	// bigger, red-hot nails that burn.
	public class MoltenNailAndHammer : NailAndHammer
	{
		public const int ExplosionDamage = 90;
		protected override int NailType => ModContent.ProjectileType<MoltenNail>();
		protected override int NailUseTime => 14;
		protected override float NailSpeed => 12f;
		protected override float NailDamageFraction => 0.5f;
		public override string Texture => "Terraria/Images/Item_" + ItemID.MoltenHamaxe;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.damage = 46;
			Item.scale = 1f;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 6);
		}

		protected override void SetHammerMode() {
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<MoltenHammerProjectile>();
			Item.shootSpeed = 13f;
			Item.UseSound = SoundID.Item1;
		}

		public override bool CanUseItem(Player player) {
			bool ok = base.CanUseItem(player);
			// Only one giant hammer in the air at a time.
			return ok && (player.altFunctionUse == 2 || player.ownedProjectileCounts[ModContent.ProjectileType<MoltenHammerProjectile>()] == 0);
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
			if (player.altFunctionUse == 2)
				base.ModifyShootStats(player, ref position, ref velocity, ref type, ref damage, ref knockback);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<NailAndHammer>().AddIngredient(ItemID.HellstoneBar, 15).AddIngredient(ItemID.SoulofNight, 5).AddTile(TileID.MythrilAnvil).Register();
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
	// Rat Tacks: throw 2-3 rats that sit on the ground as traps and bite anything that steps on them.
	// Each rat lasts 3 successful bites or 30 seconds. 20 rats max; new throws evict the oldest rats first.
	// Right click toggles melee/summon, which instantly applies to every rat already on the ground too.
	// ---------------------------------------------------------------------------------------------------------------
	public class RatTacks : ModItem, Common.GlobalItems.IManagesOwnDamageClass
	{
		public const int MaxRats = 20;
		public const int MeleeDamage = 14;
		public const int SummonDamage = 17;

		public override string Texture => "Terraria/Images/Item_" + ItemID.Mouse;

		public static DamageClass ModeClass(Player player) => player.GetModPlayer<ChaosPlayer>().RatTacksSummon ? DamageClass.Summon : DamageClass.Melee;
		public static int ModeDamage(Player player) => player.GetModPlayer<ChaosPlayer>().RatTacksSummon ? SummonDamage : MeleeDamage;

		// Keep the tooltip (damage number and class) in step with the selected mode.
		public override void UpdateInventory(Player player) {
			Item.DamageType = ModeClass(player);
			Item.damage = ModeDamage(player);
		}

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
					UpdateInventory(player);
				}
				return false;
			}
			UpdateInventory(player);
			return true;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			int count = Main.rand.Next(2, 4);
			// Over the cap: the oldest rats (least time left) leave first, just enough to fit this throw.
			var rats = new List<Projectile>();
			foreach (Projectile p in Main.ActiveProjectiles) {
				if (p.type == type && p.owner == player.whoAmI)
					rats.Add(p);
			}
			rats.Sort((a, b) => a.timeLeft.CompareTo(b.timeLeft));
			for (int i = 0; i < rats.Count + count - MaxRats && i < rats.Count; i++)
				rats[i].Kill();
			for (int i = 0; i < count; i++)
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

		// One fish per use, stacked on the tower. Vanilla minion slot rules decide how tall it can get.
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			player.AddBuff(Item.buffType, 2);
			int index = Projectile.NewProjectile(source, player.Top, Vector2.Zero, type, damage, knockback, player.whoAmI);
			if (index >= 0 && index < Main.maxProjectiles)
				Main.projectile[index].originalDamage = Item.damage;
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
			Item.useTime = 18;
			Item.useAnimation = 18;
			Item.autoReuse = false; // one click, one pull
			Item.noMelee = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 3);
			Item.UseSound = SoundID.Item24;
		}

		// Each click is one full-strength shove toward where you are right now. No teleports, no homing, no speed cap:
		// the shove adds to whatever velocity the target already has, so spam-clicking gets silly fast.
		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return true;
			Rectangle screen = Common.ChaosUtils.ScreenWorldRect;
			foreach (NPC npc in Main.ActiveNPCs) {
				if ((npc.townNPC || npc.friendly) && screen.Intersects(npc.Hitbox))
					BagofChaos.SendPushNPC(npc, Common.ChaosUtils.SuccImpulse(npc.Center, player.Center));
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
		// Charge curve: 0% -> 50% over the first 14 seconds, holds at 50%, then jumps to 100% at exactly 15 seconds.
		public const int HalfChargeTime = 60 * 14;
		public const int MaxCharge = 60 * 15;

		public static float ChargePercent(int frames) => frames >= MaxCharge ? 1f : 0.5f * Math.Min(1f, frames / (float)HalfChargeTime);

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
				float percent = ChargePercent(chaos.FartCharge);
				if (chaos.FartCharge % 60 == 0 && chaos.FartCharge < MaxCharge && Main.netMode != NetmodeID.Server) {
					SoundEngine.PlaySound(SoundID.Item16 with { Pitch = -0.5f + percent, Volume = 0.4f }, player.Center);
					if (player.whoAmI == Main.myPlayer)
						CombatText.NewText(player.Hitbox, new Color(160, 200, 80), (int)(percent * 100f) + "%", false, true);
				}
				// Maximum charge: the real (vanilla) fart sound.
				if (chaos.FartCharge == MaxCharge && Main.netMode != NetmodeID.Server) {
					SoundEngine.PlaySound(SoundID.Item16 with { Volume = 1.5f }, player.Center);
					if (player.whoAmI == Main.myPlayer)
						CombatText.NewText(player.Hitbox, Color.OrangeRed, "100%", true);
				}
				if (Main.rand.NextBool(3)) {
					Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Smoke, 0f, -1f, 100, new Color(120, 180, 60), 1f + percent * 2f);
					dust.noGravity = true;
				}
			}
			else if (chaos.FartCharge > 0) {
				Detonate(player, chaos.FartCharge);
				chaos.FartCharge = 0;
			}
		}

		private static void Detonate(Player player, int charge) {
			float power = ChargePercent(charge);
			int damage = (int)(800 * power);
			// Blocks scale harder than damage: half charge is a 12 tile crater, full charge is 50.
			int radiusTiles = (int)(50 * power * power);
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
