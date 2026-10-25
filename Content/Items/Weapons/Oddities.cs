using BagofChaos.Common;
using BagofChaos.Common.GlobalNPCs;
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
	// Fart Reverb.mp3 - does 1 damage, but the knockback is measured in screens. Affects NPCs and players, including you.
	public class FartReverb : ModItem
	{
		public override void SetDefaults() {
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 60;
			Item.useAnimation = 60;
			Item.useTurn = true;
			Item.noMelee = true;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 1);
			Item.UseSound = null; // the shockwave plays its own sound, once, for everyone
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer && player.itemAnimation == player.itemAnimationMax)
				BagofChaos.SendFart(player.Center, player.whoAmI);
			return true;
		}

		public override void AddRecipes() {
			// Post-Skeletron (decided by die roll). Ingredient that already farts: the Whoopie Cushion.
			CreateRecipe().AddIngredient(ItemID.WhoopieCushion).AddIngredient(ItemID.Bone, 25).AddIngredient(ItemID.Lens).AddTile(TileID.Anvils).Register();
		}
	}

	// Cuts the enemy's sprite in half and swaps the halves. Purely visual. Lasts until death. One cut per enemy.
	public class SpriteSlasher : ModItem
	{
		public override void SetDefaults() {
			Item.width = 34;
			Item.height = 34;
			Item.damage = 22;
			Item.DamageType = DamageClass.Melee;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 4f;
			Item.autoReuse = true;
			Item.useTurn = true;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(gold: 1);
			Item.UseSound = SoundID.Item1;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
			ChaosGlobalNPC g = target.GetGlobalNPC<ChaosGlobalNPC>();
			if (g.Slashed)
				return;
			g.Slashed = true;
			BagofChaos.SendNPCVisual(target);
			SoundEngine.PlaySound(SoundID.Item71, target.Center);
		}

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 8).AddIngredient(ItemID.Lens, 2).AddTile(TileID.Anvils).Register();
		}
	}

	// Two pong paddles follow your cursor on the top and bottom of the screen. Click to serve a ball that grows with every bounce.
	public class PongPower : ModItem
	{
		protected virtual bool Green => false;
		protected virtual int MaxBalls => 5;
		protected virtual float ServeSpeed => 7f;
		protected virtual int PaddleCount => 2;

		public override void SetStaticDefaults() {
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults() {
			Item.damage = 25;
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 30;
			Item.height = 30;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.value = Item.buyPrice(gold: 5);
			Item.rare = ItemRarityID.Pink;
			Item.UseSound = SoundID.Item44;
			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<PongBuff>();
			Item.shoot = ModContent.ProjectileType<PongBall>();
		}

		public override bool CanUseItem(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<PongBall>()] < MaxBalls;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			player.AddBuff(Item.buffType, 2);
			int paddleType = ModContent.ProjectileType<PongPaddle>();
			if (player.ownedProjectileCounts[paddleType] != PaddleCount) {
				// Swapping between the two versions: clear the old set and summon a fresh one.
				foreach (Projectile paddle in Main.ActiveProjectiles) {
					if (paddle.type == paddleType && paddle.owner == player.whoAmI)
						paddle.Kill();
				}
				for (int side = 0; side < PaddleCount; side++)
					Projectile.NewProjectile(source, player.Center, Vector2.Zero, paddleType, damage, knockback, player.whoAmI, side, Green ? 1f : 0f);
			}
			Vector2 spawn = Main.MouseWorld;
			player.LimitPointToPlayerReachableArea(ref spawn);
			Vector2 serve = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextBool() ? ServeSpeed : -ServeSpeed);
			int ball = Projectile.NewProjectile(source, spawn, serve, type, damage, knockback, player.whoAmI, 0f, Green ? 1f : 0f);
			if (ball >= 0 && ball < Main.maxProjectiles)
				Main.projectile[ball].originalDamage = damage;
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Wire, 30).AddIngredient(ItemID.Glass, 10).AddIngredient(ItemID.SoulofLight, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// Four longer paddles, twice the ball speed, ten balls. Green, because upgrades are green.
	public class GreenPongPower : PongPower
	{
		protected override bool Green => true;
		protected override int MaxBalls => 10;
		protected override float ServeSpeed => 14f;
		protected override int PaddleCount => 4;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.damage = 45;
			Item.rare = ItemRarityID.Lime;
			Item.value = Item.buyPrice(gold: 15);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<PongPower>().AddIngredient(ItemID.ChlorophyteBar, 10).AddIngredient(ItemID.Wire, 30).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// A constant barrage of rats that burst into poison clouds. Magic, technically.
	public class PocketRats : ModItem
	{
		public override void SetDefaults() {
			Item.damage = 16;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 4;
			Item.width = 28;
			Item.height = 28;
			Item.useTime = 6;
			Item.useAnimation = 6;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.knockBack = 2f;
			Item.autoReuse = true;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 3);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<PocketRat>();
			Item.shootSpeed = 9f;
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
			velocity = velocity.RotatedByRandom(MathHelper.ToRadians(20)) * Main.rand.NextFloat(0.8f, 1.2f);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.RottenChunk, 20).AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.Stinger, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// Gparrling Hook: pulls NPCs to you. Ignores gravity, weight and knockback resistance. Yes, bosses too.
	public class GparrlingHook : ModItem
	{
		public override void SetDefaults() {
			Item.damage = 10;
			Item.DamageType = DamageClass.Melee;
			Item.width = 30;
			Item.height = 30;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.knockBack = 0f;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 2);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<GparrlingHookProjectile>();
			Item.shootSpeed = 36f;
		}

		public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.GrapplingHook).AddIngredient(ItemID.Chain, 10).AddIngredient(ItemID.Cog, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// Keeps your town nice and tidy. Kills non-hostile NPCs on contact. Hostile enemies are unaffected; it's not a weapon, it's a mower.
	public class NPCLawnmower : ModItem
	{
		public override void SetDefaults() {
			Item.width = 40;
			Item.height = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(gold: 5);
			Item.UseSound = new SoundStyle($"{BagofChaos.AssetPath}Sounds/Mower") { Volume = 0.9f, PitchVariance = 0.1f };
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return true;
			Rectangle mowArea = player.Hitbox;
			mowArea.Inflate(28, 10);
			mowArea.Offset(player.direction * 24, 0);
			foreach (NPC npc in Main.ActiveNPCs) {
				if (ChaosUtils.IsMowable(npc) && npc.Hitbox.Intersects(mowArea)) {
					for (int i = 0; i < 15; i++)
						Dust.NewDust(npc.position, npc.width, npc.height, DustID.Grass, Main.rand.NextFloat(-3f, 3f), -3f);
					BagofChaos.SendKillNPC(npc);
				}
			}
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 20).AddIngredient(ItemID.Cog, 10).AddIngredient(ItemID.Sunflower).AddTile(TileID.Anvils).Register();
		}
	}
}
