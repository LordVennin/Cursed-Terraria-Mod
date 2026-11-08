using BagofChaos.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalItems
{
	// Goblin' Your Money unlocks after the first reforge.
	public class ReforgeTracker : GlobalItem
	{
		public override void PostReforge(Item item) {
			if (!Main.dedServ)
				Main.LocalPlayer.GetModPlayer<ChaosPlayer>().HasReforged = true;
		}
	}
}
