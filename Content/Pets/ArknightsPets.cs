using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Pets
{
	// Floating chibi placeholders for two Arknights characters. Replace the PNGs in Content/Pets with real chibis whenever.
	// Monst3r is a light pet (red glow), Kal'tsit is a vanity pet. Both float around like a Zephyr Fish.

	public abstract class FloatingChibiProjectile : ModProjectile
	{
		protected abstract int BuffType { get; }

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = 4;
			Main.projPet[Type] = true;
			ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Type], 8)
				.WithOffset(-10, -20f)
				.WithSpriteDirection(-1)
				.WithCode(DelegateMethods.CharacterPreview.Float);
		}

		public override void SetDefaults() {
			Projectile.CloneDefaults(ProjectileID.ZephyrFish);
			Projectile.width = 32;
			Projectile.height = 36;
			AIType = ProjectileID.ZephyrFish;
		}

		public override bool PreAI() {
			Main.player[Projectile.owner].zephyrfish = false; // relic from AIType
			return true;
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (!player.dead && player.HasBuff(BuffType))
				Projectile.timeLeft = 2;
			Projectile.frameCounter++;
			if (Projectile.frameCounter >= 8) {
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			Projectile.rotation = Projectile.velocity.X * 0.03f;
		}
	}

	// ---- Monst3r (light pet) ----
	public class Monst3rProjectile : FloatingChibiProjectile
	{
		protected override int BuffType => ModContent.BuffType<Monst3rBuff>();

		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			ProjectileID.Sets.LightPet[Type] = true;
		}

		public override void AI() {
			base.AI();
			Lighting.AddLight(Projectile.Center, 1.0f, 0.25f, 0.3f);
		}
	}

	public class Monst3rBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.lightPet[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			bool unused = false;
			player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<Monst3rProjectile>());
		}
	}

	public class Monst3rItem : ModItem
	{
		public override void SetDefaults() {
			Item.damage = 0;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.shoot = ModContent.ProjectileType<Monst3rProjectile>();
			Item.width = 20;
			Item.height = 24;
			Item.UseSound = SoundID.Item2;
			Item.useAnimation = 20;
			Item.useTime = 20;
			Item.rare = ItemRarityID.Yellow;
			Item.noMelee = true;
			Item.value = Item.sellPrice(gold: 5);
			Item.buffType = ModContent.BuffType<Monst3rBuff>();
		}

		public override void UseStyle(Player player, Rectangle heldItemFrame) {
			if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
				player.AddBuff(Item.buffType, 3600);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Wire, 20).AddIngredient(ItemID.Ruby).AddIngredient(ItemID.SoulofNight, 5).AddTile(TileID.Anvils).Register();
		}
	}

	// ---- Kal'tsit (vanity pet) ----
	public class KaltsitProjectile : FloatingChibiProjectile
	{
		protected override int BuffType => ModContent.BuffType<KaltsitBuff>();
	}

	public class KaltsitBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.vanityPet[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			bool unused = false;
			player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<KaltsitProjectile>());
		}
	}

	public class KaltsitItem : ModItem
	{
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.ZephyrFish);
			Item.width = 20;
			Item.height = 24;
			Item.shoot = ModContent.ProjectileType<KaltsitProjectile>();
			Item.buffType = ModContent.BuffType<KaltsitBuff>();
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer)
				player.AddBuff(Item.buffType, 3600);
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Emerald).AddIngredient(ItemID.HealingPotion, 5).AddIngredient(ItemID.Silk, 10).AddTile(TileID.Anvils).Register();
		}
	}
}
