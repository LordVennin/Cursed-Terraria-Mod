using Terraria.ModLoader;

namespace BagofChaos.Common.Systems
{
	public class ChaosKeybinds : ModSystem
	{
		public static ModKeybind WarpaintMagnet { get; private set; }

		public override void Load() {
			WarpaintMagnet = KeybindLoader.RegisterKeybind(Mod, "WarpaintMagnet", "V");
		}

		public override void Unload() {
			WarpaintMagnet = null;
		}
	}
}
