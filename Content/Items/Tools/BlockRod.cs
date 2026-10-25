using BagofChaos.Common.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Tools
{
	// Dirt Rod, but for whole chunks. Click-drag to select, left click to place precisely, right click to put it back where it came from.
	public class BlockRod : ModItem
	{
		public const int MaxSelection = 24; // tiles per side

		public override void SetDefaults() {
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.noMelee = true;
			Item.autoReuse = false;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 5);
		}

		public override bool CanUseItem(Player player) => false; // all the work happens in HoldItem

		public static Rectangle SelectionRect(Point a, Point b) {
			int x0 = System.Math.Min(a.X, b.X), x1 = System.Math.Max(a.X, b.X);
			int y0 = System.Math.Min(a.Y, b.Y), y1 = System.Math.Max(a.Y, b.Y);
			int w = System.Math.Min(x1 - x0 + 1, MaxSelection);
			int h = System.Math.Min(y1 - y0 + 1, MaxSelection);
			if (b.X < a.X) x0 = x1 - w + 1;
			if (b.Y < a.Y) y0 = y1 - h + 1;
			return new Rectangle(x0, y0, w, h);
		}

		public static Point PasteAnchor(BlockRodClipboard clip) {
			Point mouse = Main.MouseWorld.ToTileCoordinates();
			return new Point(mouse.X - clip.Width / 2, mouse.Y - clip.Height / 2);
		}

		public override void HoldItem(Player player) {
			if (player.whoAmI != Main.myPlayer || Main.gameMenu)
				return;
			ChaosPlayer modPlayer = player.GetModPlayer<ChaosPlayer>();
			if (player.mouseInterface) {
				return;
			}
			Point mouseTile = Main.MouseWorld.ToTileCoordinates();

			if (modPlayer.Clipboard == null) {
				// Selection phase: drag out a rectangle.
				if (!modPlayer.Selecting && PlayerInput.Triggers.JustPressed.MouseLeft) {
					modPlayer.Selecting = true;
					modPlayer.SelectionStart = mouseTile;
					modPlayer.SelectionEnd = mouseTile;
				}
				else if (modPlayer.Selecting) {
					modPlayer.SelectionEnd = mouseTile;
					if (!Main.mouseLeft) {
						modPlayer.Selecting = false;
						Rectangle sel = SelectionRect(modPlayer.SelectionStart, modPlayer.SelectionEnd);
						BlockRodClipboard clip = BlockRodClipboard.Cut(new Point(sel.X, sel.Y), sel.Width, sel.Height);
						clip.Origin = new Point(sel.X, sel.Y);
						if (HasAnyBlocks(clip)) {
							modPlayer.Clipboard = clip;
							SoundEngine.PlaySound(SoundID.Dig, Main.MouseWorld);
						}
					}
				}
				if (PlayerInput.Triggers.JustPressed.MouseRight)
					modPlayer.Selecting = false;
				return;
			}

			// Holding a chunk.
			BlockRodClipboard held = modPlayer.Clipboard;
			Point anchor = PasteAnchor(held);
			if (PlayerInput.Triggers.JustPressed.MouseLeft) {
				if (held.CanPlace(anchor)) {
					held.Paste(anchor);
					modPlayer.Clipboard = null;
					SoundEngine.PlaySound(SoundID.Dig, Main.MouseWorld);
				}
				else {
					SoundEngine.PlaySound(SoundID.Unlock, Main.MouseWorld);
				}
			}
			else if (PlayerInput.Triggers.JustPressed.MouseRight) {
				// Cancel: put the chunk back where it came from (or as close as it fits).
				Point origin = held.Origin;
				int guard = 0;
				while (!held.CanPlace(origin) && guard++ < 32)
					origin.Y--;
				if (guard < 32) {
					held.Paste(origin);
					modPlayer.Clipboard = null;
					SoundEngine.PlaySound(SoundID.Dig, Main.MouseWorld);
				}
			}
		}

		private static bool HasAnyBlocks(BlockRodClipboard clip) {
			for (int x = 0; x < clip.Width; x++)
				for (int y = 0; y < clip.Height; y++)
					if (clip.Tiles[x, y].HasTile)
						return true;
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.DirtRod).AddRecipeGroup(RecipeGroupID.IronBar, 5).AddIngredient(ItemID.Wire, 10).AddTile(TileID.Anvils).Register();
		}
	}
}
