using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Mounts
{
	// "Harpy" Wings: a harpy sits behind your head and lets you glide. Uses the vanilla harpy sheet.
	public class HarpyWingsMount : ModMount
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Harpy;

		public override void SetStaticDefaults() {
			MountData.jumpHeight = 8;
			MountData.acceleration = 0.12f;
			MountData.jumpSpeed = 5f;
			MountData.blockExtraJumps = false;
			MountData.constantJump = true;
			MountData.heightBoost = 0;
			MountData.fallDamage = 0f;
			MountData.runSpeed = 5f;
			MountData.dashSpeed = 5f;
			MountData.flightTimeMax = 0;
			MountData.fatigueMax = 0;
			MountData.buff = ModContent.BuffType<Buffs.HarpyMountBuff>();
			MountData.spawnDust = DustID.Cloud;

			int frames = Main.npcFrameCount[NPCID.Harpy];
			MountData.totalFrames = frames;
			MountData.playerYOffsets = Enumerable.Repeat(0, frames).ToArray();
			MountData.xOffset = 0;
			MountData.yOffset = -26; // behind the head
			MountData.playerHeadOffset = 0;
			MountData.bodyFrame = 3;
			MountData.standingFrameCount = frames;
			MountData.standingFrameDelay = 8;
			MountData.standingFrameStart = 0;
			MountData.runningFrameCount = frames;
			MountData.runningFrameDelay = 6;
			MountData.runningFrameStart = 0;
			MountData.flyingFrameCount = frames;
			MountData.flyingFrameDelay = 4;
			MountData.flyingFrameStart = 0;
			MountData.inAirFrameCount = frames;
			MountData.inAirFrameDelay = 4;
			MountData.inAirFrameStart = 0;
			MountData.idleFrameCount = frames;
			MountData.idleFrameDelay = 10;
			MountData.idleFrameStart = 0;
			MountData.idleFrameLoop = true;
			MountData.swimFrameCount = frames;
			MountData.swimFrameDelay = 8;
			MountData.swimFrameStart = 0;
			if (!Main.dedServ) {
				MountData.textureWidth = MountData.backTexture.Width();
				MountData.textureHeight = MountData.backTexture.Height();
			}
		}

		public override void UpdateEffects(Player player) {
			// Glide: holding jump in the air slows your fall like a pair of wings would.
			if (player.controlJump && player.velocity.Y > 0f && player.jump == 0)
				player.velocity.Y = System.Math.Min(player.velocity.Y, 1.2f);
			if (Main.rand.NextBool(10))
				Dust.NewDust(player.position, player.width, player.height, DustID.Cloud, 0f, 1f);
		}
	}

	// Rat Cart: a minecart that squeaks and is faster underground.
	public class RatCartMount : ModMount
	{
		public override void SetStaticDefaults() {
			MountData.Minecart = true;
			MountData.spawnDust = DustID.Smoke;
			MountData.buff = ModContent.BuffType<Buffs.RatCartBuff>();
			MountData.MinecartUpgradeRunSpeed = 14f;
			MountData.MinecartUpgradeDashSpeed = 14f;
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
			if (Main.rand.NextBool(240))
				SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Squeak") { Volume = 0.7f, PitchVariance = 0.3f }, player.Center);
			// Slower on the surface; "faster underground" is the same thing from the other side.
			if (!player.ZoneDirtLayerHeight && !player.ZoneRockLayerHeight && !player.ZoneUnderworldHeight && System.Math.Abs(player.velocity.X) > 8f)
				player.velocity.X *= 0.97f;
		}
	}

	// Sharkboard: ride a shark. Breathe underwater. Go fast in water.
	public class SharkboardMount : ModMount
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.Shark;

		public override void SetStaticDefaults() {
			MountData.jumpHeight = 6;
			MountData.acceleration = 0.2f;
			MountData.jumpSpeed = 4f;
			MountData.blockExtraJumps = false;
			MountData.constantJump = false;
			MountData.heightBoost = 14;
			MountData.fallDamage = 1f;
			MountData.runSpeed = 4f;
			MountData.dashSpeed = 4f;
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
			if (player.wet) {
				player.ignoreWater = true;
				player.accFlipper = true;
				player.maxRunSpeed *= 2.5f;
				player.accRunSpeed *= 2.5f;
			}
		}
	}
}
