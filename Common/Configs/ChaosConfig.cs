using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BagofChaos.Common.Configs
{
	// Client-side options, mostly accessibility toggles for the screen effects.
	public class ChaosConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header("ScreenEffects")]
		[DefaultValue(true)]
		public bool Enable3DEffect { get; set; }

		[Range(1, 24)]
		[DefaultValue(8)]
		[Slider]
		public int ThreeDOffsetPixels { get; set; }

		[DefaultValue(true)]
		public bool EnableTMFlashDarkness { get; set; }

		[Range(10, 60)]
		[DefaultValue(20)]
		[Slider]
		public int TMFlashVisionTiles { get; set; }

		[Header("Chaos")]
		[DefaultValue(true)]
		public bool ZombieSalesmanReplacesIcons { get; set; }

		[DefaultValue(true)]
		public bool FartAffectsPlayers { get; set; }
	}
}
