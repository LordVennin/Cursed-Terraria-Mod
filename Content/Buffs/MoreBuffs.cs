using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Buffs
{
	// Glass and sand blocks are invisible to you.
	public class CrushedGlass : ModBuff
	{
	}

	// Dirt glows like a torch.
	public class CrushedDirt : ModBuff
	{
	}

	// Eye on Me!: your camera belongs to the Eye of Cthulhu now.
	public class EyeOnMe : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	// Nails: does nothing on its own. The hammer disagrees.
	public class Nails : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}

	// Tanned: tagged by the Tanning Mirror's cone. Minions hit harder.
	public class Tanned : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}

	// Unstable: the random 50% damage / attack speed roll while potion sick with the Unstable Concoction.
	public class UnstablePower : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}
	}

	// Stacks of Fish minion buff.
	public class FishStackBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.FishStackMinion>()] > 0)
				player.buffTime[buffIndex] = 18000;
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	// Slotter Pillar minion buff.
	public class SlotterBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.SlotMinion>()] > 0)
				player.buffTime[buffIndex] = 18000;
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}

	// Harpy wings mount buff.
	public class HarpyMountBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			player.mount.SetMount(ModContent.MountType<Mounts.HarpyWingsMount>(), player);
			player.buffTime[buffIndex] = 10;
		}
	}

	public class RatCartBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			player.mount.SetMount(ModContent.MountType<Mounts.RatCartMount>(), player);
			player.buffTime[buffIndex] = 10;
		}
	}

	public class SharkboardBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			player.mount.SetMount(ModContent.MountType<Mounts.SharkboardMount>(), player);
			player.buffTime[buffIndex] = 10;
			player.gills = true;
		}
	}
}
