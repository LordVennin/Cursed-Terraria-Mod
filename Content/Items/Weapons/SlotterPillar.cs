using BagofChaos.Content.Buffs;
using BagofChaos.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Weapons
{
	// Slotter Pillar: your inventory slots become minions while the inventory is open.
	public class SlotterPillar : ModItem
	{
		public override void SetStaticDefaults() {
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
			ItemID.Sets.StaffMinionSlotsRequired[Type] = 0.5f;
		}

		public override void SetDefaults() {
			Item.damage = 35;
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 30;
			Item.height = 30;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.value = Item.buyPrice(gold: 8);
			Item.rare = ItemRarityID.LightRed;
			Item.UseSound = SoundID.Item44;
			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<SlotterBuff>();
			Item.shoot = ModContent.ProjectileType<SlotMinion>();
		}

		public override bool CanUseItem(Player player) => Main.playerInventory;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			player.AddBuff(Item.buffType, 2);
			// Pick an inventory slot (not the hotbar) that isn't already a minion.
			int slot = Main.rand.Next(10, 50);
			for (int tries = 0; tries < 40; tries++) {
				bool taken = false;
				foreach (Projectile p in Main.ActiveProjectiles)
					if (p.type == type && p.owner == player.whoAmI && (int)p.ai[0] == slot) { taken = true; break; }
				if (!taken) break;
				slot = Main.rand.Next(10, 50);
			}
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, slot);
			return false;
		}
	}
}
