using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Mounts
{
	// Rat Cart: a minecart (it goes in the minecart equipment slot) that squeaks and is faster underground.
	public class RatCartMount : ModMount
	{
		public const float SurfaceSpeed = 13f, UndergroundSpeed = 20f;

		public override void SetStaticDefaults() {
			MountData.Minecart = true;
			MountID.Sets.Cart[Type] = true;
			MountData.runSpeed = SurfaceSpeed;
			MountData.dashSpeed = SurfaceSpeed;
			MountData.acceleration = 0.04f;
			MountData.jumpHeight = 15;
			MountData.jumpSpeed = 5.15f;
			MountData.fallDamage = 1f;
			MountData.spawnDust = DustID.Smoke;
			MountData.buff = ModContent.BuffType<Buffs.RatCartBuff>();
			MountData.MinecartUpgradeRunSpeed = 20f;
			MountData.MinecartUpgradeDashSpeed = 20f;
			MountData.MinecartUpgradeAcceleration = 0.1f;
			MountData.delegations.MinecartDust = DelegateMethods.Minecart.Sparks;
			MountData.delegations.MinecartLandingSound = DelegateMethods.Minecart.LandingSound;
			MountData.delegations.MinecartBumperSound = DelegateMethods.Minecart.BumperSound;
			MountData.totalFrames = 3;
			MountData.playerYOffsets = Enumerable.Repeat(9, 3).ToArray();
			MountData.xOffset = 0;
			MountData.yOffset = 0;
			MountData.playerHeadOffset = 0;
			MountData.bodyFrame = 3;
			MountData.standingFrameCount = 1;
			MountData.standingFrameDelay = 12;
			MountData.standingFrameStart = 0;
			MountData.runningFrameCount = 3;
			MountData.runningFrameDelay = 6;
			MountData.runningFrameStart = 0;
			MountData.flyingFrameCount = 1; MountData.flyingFrameStart = 0; MountData.flyingFrameDelay = 10;
			MountData.inAirFrameCount = 1; MountData.inAirFrameStart = 0; MountData.inAirFrameDelay = 10;
			MountData.idleFrameCount = 1; MountData.idleFrameStart = 0; MountData.idleFrameDelay = 10; MountData.idleFrameLoop = true;
			MountData.swimFrameCount = 1; MountData.swimFrameStart = 0; MountData.swimFrameDelay = 10;
			if (!Main.dedServ) {
				MountData.textureWidth = MountData.backTexture.Width();
				MountData.textureHeight = MountData.backTexture.Height();
			}
		}

		public override void UpdateEffects(Player player) {
			// A squeak every few seconds, on average.
			if (Main.rand.NextBool(240))
				SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Squeak") { Volume = 0.7f, PitchVariance = 0.3f }, player.Center);
			// Native minecart movement, just a higher top speed below the surface.
			bool underground = player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight || player.ZoneUnderworldHeight;
			float speed = underground ? UndergroundSpeed : SurfaceSpeed;
			MountData.runSpeed = speed;
			MountData.dashSpeed = speed;
			MountData.MinecartUpgradeRunSpeed = speed + 6f;
			MountData.MinecartUpgradeDashSpeed = speed + 6f;
		}
	}

	// Sharkboard: ride on top of a vanilla shark. Breathe underwater. Fast in water, miserable on land.
	public class SharkboardMount : ModMount
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Shark;

		// Mount art is normally Texture + "_Back"; the shark sheet itself is the back layer.
		protected override string GetExtraTexture(MountTextureType textureType) => textureType == MountTextureType.Back ? Texture : null;

		public override void SetStaticDefaults() {
			MountData.jumpHeight = 4;
			MountData.acceleration = 0.04f; // sluggish
			MountData.jumpSpeed = 3f;
			MountData.blockExtraJumps = false;
			MountData.constantJump = false;
			MountData.heightBoost = 14;
			MountData.fallDamage = 1f;
			MountData.runSpeed = 2.2f;
			MountData.dashSpeed = 2.2f;
			MountData.flightTimeMax = 0;
			MountData.fatigueMax = 0;
			MountData.buff = ModContent.BuffType<Buffs.SharkboardBuff>();
			MountData.spawnDust = DustID.Water;
			int frames = Main.npcFrameCount[NPCID.Shark];
			MountData.totalFrames = frames;
			MountData.playerYOffsets = Enumerable.Repeat(14, frames).ToArray();
			MountData.xOffset = 0;
			MountData.yOffset = 4;
			MountData.playerHeadOffset = 14;
			MountData.bodyFrame = 3;
			MountData.standingFrameCount = frames; MountData.standingFrameDelay = 10; MountData.standingFrameStart = 0;
			MountData.runningFrameCount = frames; MountData.runningFrameDelay = 6; MountData.runningFrameStart = 0;
			MountData.flyingFrameCount = frames; MountData.flyingFrameDelay = 6; MountData.flyingFrameStart = 0;
			MountData.inAirFrameCount = frames; MountData.inAirFrameDelay = 6; MountData.inAirFrameStart = 0;
			MountData.idleFrameCount = frames; MountData.idleFrameDelay = 10; MountData.idleFrameStart = 0; MountData.idleFrameLoop = true;
			MountData.swimFrameCount = frames; MountData.swimFrameDelay = 4; MountData.swimFrameStart = 0;
			if (!Main.dedServ) {
				MountData.textureWidth = MountData.backTexture.Width();
				MountData.textureHeight = MountData.backTexture.Height();
			}
		}

		public override void UpdateEffects(Player player) {
			player.gills = true;
			if (player.wet && !player.lavaWet) {
				// In water it's a shark: fast, and it swims up and down.
				player.ignoreWater = true;
				player.maxRunSpeed = 9f;
				player.accRunSpeed = 9f;
				player.runAcceleration = 0.4f;
				if (player.controlUp || player.controlJump)
					player.velocity.Y = System.Math.Max(player.velocity.Y - 0.5f, -6f);
				else if (player.controlDown)
					player.velocity.Y = System.Math.Min(player.velocity.Y + 0.5f, 6f);
				else
					player.velocity.Y *= 0.9f;
				player.fallStart = (int)(player.position.Y / 16f);
			}
		}
	}
}
