using BagofChaos.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Buffs
{
	// Glue: anything afflicted sticks to anything it touches. The debuff wears off; the attachments do not. (See ChaosGlobalNPC / ChaosPlayer.)
	public class GlueDebuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = true; // lets it land on NPCs that are "immune to all debuffs"
		}
	}

	// 3D: red/cyan anaglyph over the whole screen. Purely visual, purely annoying.
	public class ThreeDDebuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	// Life Link: every NPC on screen with this shares damage taken. Not split. Copied.
	public class LifeLinkDebuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}

	// Cardboard Box (Solid Snake): phase through blocks. 10 seconds. Fall damage still applies.
	public class CardboardBox : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			player.noItems = false;
			player.noFallDmg = false;
		}
	}

	// Winded: Drinkable Air cooldown. You may drown while waiting. Working as intended.
	public class Winded : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	// Delirious: shown while the Delirium event is happening near you.
	public class Delirious : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}
	}

	// Pong Power minion buff: keeps the paddles alive.
	public class PongBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.PongPaddle>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
