#!/usr/bin/env python3
"""Generates every placeholder texture and sound for Bag of Chaos.

Run from the repo root:  python3 tools/gen_assets.py
Requires Pillow (pip install pillow). Pure programmer art - replace any PNG with real pixel art whenever you like,
the code only cares about the file names and (for tiles / animated sprites) the dimensions.
"""
import math
import os
import random
import struct
import wave

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
random.seed(1337)

# ---------------------------------------------------------------------------------------------------------------------
# Palette
# ---------------------------------------------------------------------------------------------------------------------
BLACK = (20, 18, 24, 255)
WHITE = (245, 245, 245, 255)
GREY = (140, 140, 150, 255)
DARKGREY = (80, 80, 90, 255)
LIGHTGREY = (200, 200, 210, 255)
RED = (220, 50, 50, 255)
DARKRED = (140, 20, 30, 255)
ORANGE = (240, 140, 40, 255)
YELLOW = (250, 220, 60, 255)
GREEN = (70, 190, 80, 255)
DARKGREEN = (30, 110, 50, 255)
LIME = (160, 240, 90, 255)
BLUE = (60, 110, 230, 255)
LIGHTBLUE = (130, 190, 250, 255)
DARKBLUE = (30, 50, 140, 255)
PURPLE = (150, 70, 200, 255)
PINK = (240, 120, 200, 255)
BROWN = (130, 85, 45, 255)
DARKBROWN = (80, 50, 25, 255)
TAN = (210, 180, 130, 255)
SKIN = (235, 190, 150, 255)
ZOMBIE = (140, 180, 110, 255)
ZOMBIEDARK = (90, 120, 70, 255)
GOLD = (240, 200, 60, 255)
SILVER = (190, 195, 205, 255)
CYAN = (90, 230, 230, 255)
TRANSPARENT = (0, 0, 0, 0)


class Sprite:
    def __init__(self, w, h, bg=TRANSPARENT):
        self.im = Image.new("RGBA", (w, h), bg)
        self.d = ImageDraw.Draw(self.im)
        self.w, self.h = w, h

    # primitives -------------------------------------------------------------------------------------------------------
    def rect(self, x0, y0, x1, y1, fill, outline=BLACK):
        self.d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline)

    def ellipse(self, x0, y0, x1, y1, fill, outline=BLACK):
        self.d.ellipse([x0, y0, x1, y1], fill=fill, outline=outline)

    def line(self, x0, y0, x1, y1, fill, width=1):
        self.d.line([x0, y0, x1, y1], fill=fill, width=width)

    def px(self, x, y, fill):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.im.putpixel((x, y), fill)

    def poly(self, pts, fill, outline=BLACK):
        self.d.polygon(pts, fill=fill, outline=outline)

    def text_ish(self, x, y, n, color):
        """Draws n little dashes that look like text."""
        for i in range(n):
            self.line(x + i * 3, y, x + i * 3 + 1, y, color)

    def save(self, rel):
        path = os.path.join(ROOT, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        self.im.save(path)
        print("wrote", rel)


# ---------------------------------------------------------------------------------------------------------------------
# Generic shapes reused by many items
# ---------------------------------------------------------------------------------------------------------------------
def bottle(color, w=20, h=28, label=None):
    s = Sprite(w, h)
    s.rect(w // 2 - 3, 0, w // 2 + 2, 4, TAN)                  # cork
    s.rect(w // 2 - 4, 4, w // 2 + 3, 9, LIGHTGREY)             # neck
    s.ellipse(1, 8, w - 2, h - 1, color)                        # body
    s.ellipse(4, 12, 7, 17, (255, 255, 255, 140), outline=None)  # shine
    if label:
        s.rect(5, h // 2, w - 6, h // 2 + 5, label)
    return s


def gun(body, accent, w=44, h=22, barrel_len=18):
    s = Sprite(w, h)
    s.rect(4, 8, w - 1, 13, body)                               # barrel/body
    s.rect(w - barrel_len, 6, w - 1, 9, accent)                 # top rail
    s.rect(6, 13, 14, h - 1, DARKGREY)                          # grip
    s.rect(14, 13, 20, 18, DARKGREY)                            # trigger guard
    s.rect(2, 4, 12, 12, body)                                  # stock/receiver
    return s


def accessory_ring(color, w=24, h=24):
    s = Sprite(w, h)
    s.ellipse(2, 2, w - 3, h - 3, color)
    s.ellipse(8, 8, w - 9, h - 9, TRANSPARENT, outline=BLACK)
    return s


def sword(blade, hilt, w=34, h=34):
    s = Sprite(w, h)
    s.poly([(w - 4, 2), (w - 1, 5), (10, h - 6), (6, h - 10)], blade)
    s.line(8, h - 12, 14, h - 6, GOLD, 3)
    s.line(1, h - 2, 8, h - 9, hilt, 3)
    return s


def pack(color, w=28, h=30):
    s = Sprite(w, h)
    s.rect(3, 4, w - 4, h - 2, color)
    s.rect(7, 0, w - 8, 6, DARKGREY)
    s.rect(6, 10, w - 7, 14, DARKGREY)
    s.rect(6, 18, w - 7, 22, DARKGREY)
    return s


def tile_block(w, h, color, dark, bolts=True):
    s = Sprite(w, h)
    s.rect(0, 0, w - 1, h - 1, color)
    if bolts:
        for (x, y) in [(2, 2), (w - 4, 2), (2, h - 4), (w - 4, h - 4)]:
            s.rect(x, y, x + 1, y + 1, dark, outline=None)
    return s


# ---------------------------------------------------------------------------------------------------------------------
# Items
# ---------------------------------------------------------------------------------------------------------------------
def gen_items():
    I = "Content/Items/"

    # ---- Ammo ----
    s = Sprite(14, 14)
    s.ellipse(0, 0, 13, 13, WHITE)
    s.ellipse(5, 4, 10, 9, BLACK, outline=None)
    s.save(I + "Ammo/GooglyEyeAmmo.png")

    s = Sprite(10, 18)
    s.rect(2, 0, 7, 17, GOLD)
    s.rect(2, 12, 7, 17, DARKGREY)
    s.line(4, 3, 5, 10, RED, 1)
    s.save(I + "Ammo/YoureLongAmmo.png")

    s = Sprite(10, 18)
    s.rect(2, 0, 7, 17, GOLD)
    s.rect(2, 12, 7, 17, DARKGREY)
    s.line(4, 2, 4, 10, BLUE, 1); s.line(5, 2, 5, 10, BLUE, 1)
    s.save(I + "Ammo/YoureTallAmmo.png")

    s = Sprite(10, 18)
    s.rect(2, 0, 7, 17, PINK)
    s.rect(2, 12, 7, 17, DARKGREY)
    s.ellipse(3, 3, 6, 6, RED, outline=None)
    s.save(I + "Ammo/BuddySystemBullet.png")

    bottle((255, 0, 255, 255), label=GREEN).save(I + "Ammo/ChaosSolution.png")

    # ---- Weapons ----
    s = Sprite(32, 32)                                        # Fart Reverb.mp3: a cassette tape
    s.rect(1, 6, 30, 26, DARKGREY)
    s.rect(4, 9, 27, 16, LIGHTGREY)
    s.ellipse(7, 17, 13, 23, WHITE)
    s.ellipse(18, 17, 24, 23, WHITE)
    s.rect(6, 10, 25, 11, BROWN, outline=None)
    s.save(I + "Weapons/FartReverb.png")

    sword(SILVER, DARKRED).save(I + "Weapons/SpriteSlasher.png")

    s = Sprite(52, 22)                                        # Credit Cannon
    s.rect(0, 6, 51, 16, DARKGREY)
    s.rect(30, 3, 51, 19, GOLD)
    s.rect(6, 15, 14, 21, DARKGREY)
    s.text_ish(33, 8, 5, BLACK)
    s.text_ish(33, 12, 5, BLACK)
    s.save(I + "Weapons/CreditCannon.png")

    s = gun(WHITE, ORANGE)
    s.rect(18, 2, 30, 8, YELLOW)                              # glue bottle on top
    s.save(I + "Weapons/GlueGun.png")

    s = gun(SILVER, GREY, w=48)
    for i in range(3):
        s.rect(28 + i * 6, 14, 32 + i * 6, 20, SILVER)        # dangling cans
    s.save(I + "Weapons/ChainCan.png")

    s = Sprite(30, 30)                                        # Pong Power
    s.rect(0, 0, 29, 29, BLACK)
    s.rect(2, 8, 4, 20, WHITE, outline=None)
    s.rect(25, 10, 27, 22, WHITE, outline=None)
    s.rect(14, 14, 16, 16, WHITE, outline=None)
    s.line(14, 1, 14, 28, DARKGREY)
    s.save(I + "Weapons/PongPower.png")
    s = Sprite(30, 30)                                        # Green Pong Power
    s.rect(0, 0, 29, 29, BLACK)
    s.rect(2, 8, 4, 20, LIME, outline=None); s.rect(25, 10, 27, 22, LIME, outline=None)
    s.rect(8, 2, 20, 4, LIME, outline=None); s.rect(10, 25, 22, 27, LIME, outline=None)
    s.rect(14, 14, 16, 16, LIME, outline=None)
    s.save(I + "Weapons/GreenPongPower.png")

    s = Sprite(28, 28)                                        # Text Boxing Gloves
    s.ellipse(2, 6, 20, 24, RED)
    s.rect(14, 18, 26, 26, RED)
    s.text_ish(6, 12, 4, WHITE)
    s.save(I + "Accessories/TextBoxingGloves.png")

    s = gun(BROWN, DARKBROWN)
    s.ellipse(20, 1, 30, 8, GREY)                             # rat on the barrel
    s.save(I + "Weapons/RattlingGun.png")

    s = Sprite(28, 28)                                        # Pocket Rats: jeans pocket with rats
    s.rect(2, 4, 25, 26, DARKBLUE)
    s.line(2, 8, 25, 8, LIGHTBLUE)
    s.ellipse(5, 10, 13, 17, GREY)
    s.ellipse(13, 12, 22, 19, GREY)
    s.px(7, 12, BLACK); s.px(15, 14, BLACK)
    s.save(I + "Weapons/PocketRats.png")

    s = gun(DARKGREEN, GREEN)
    s.ellipse(22, 0, 34, 7, PURPLE)
    s.save(I + "Weapons/VenomSnake.png")

    s = Sprite(30, 30)                                        # Gparrling Hook
    s.line(4, 26, 20, 10, DARKGREY, 3)
    s.poly([(20, 2), (28, 10), (20, 18), (12, 10)], SILVER)
    s.ellipse(0, 22, 8, 30, BROWN)
    s.save(I + "Weapons/GparrlingHook.png")

    s = Sprite(40, 30)                                        # NPC Lawnmower
    s.rect(4, 10, 32, 22, RED)
    s.rect(28, 2, 39, 12, DARKGREY)
    s.ellipse(4, 20, 14, 29, BLACK, outline=DARKGREY)
    s.ellipse(22, 20, 32, 29, BLACK, outline=DARKGREY)
    s.save(I + "Weapons/NPCLawnmower.png")

    s = gun(BLUE, LIGHTBLUE)
    s.rect(30, 2, 38, 8, WHITE)
    s.save(I + "Weapons/ToolGun.png")
    s = gun(BLUE, LIGHTBLUE, w=52)
    for i in range(3):
        s.rect(6, 6 + i * 3, 50, 7 + i * 3, DARKBLUE, outline=None)
    s.save(I + "Weapons/ToolMinigun.png")

    # ---- Tools ----
    s = Sprite(32, 32)                                        # Block Rod
    s.line(2, 29, 22, 9, BROWN, 3)
    s.rect(20, 2, 29, 11, TAN)
    s.rect(22, 4, 27, 9, BROWN, outline=None)
    s.save(I + "Tools/BlockRod.png")

    s = Sprite(26, 28)                                        # NPC Hand
    s.rect(6, 12, 20, 27, SKIN)
    for i in range(4):
        s.rect(6 + i * 4, 2 + (0 if i in (1, 2) else 3), 8 + i * 4, 13, SKIN)
    s.rect(1, 14, 6, 22, SKIN)
    s.save(I + "Tools/NPCHand.png")

    # ---- Accessories ----
    def arrow_badge(color, dirs):
        s = Sprite(26, 26)
        s.ellipse(0, 0, 25, 25, color)
        c = 12
        for (dx, dy) in dirs:
            s.line(c, c, c + dx * 8, c + dy * 8, WHITE, 3)
        return s

    arrow_badge(BLUE, [(0, -1), (0, 1)]).save(I + "Accessories/InstantUpDown.png")
    arrow_badge(BLUE, [(-1, 0), (1, 0)]).save(I + "Accessories/InstantLeftRight.png")
    arrow_badge(RED, [(0, -1), (0, 1)]).save(I + "Accessories/UnreasonableUpDown.png")
    arrow_badge(RED, [(-1, 0), (1, 0)]).save(I + "Accessories/UnreasonableLeftRight.png")
    arrow_badge(PURPLE, [(0, -1), (0, 1), (-1, 0), (1, 0)]).save(I + "Accessories/InstantOmniMovement.png")

    s = bottle(LIGHTBLUE)
    s.ellipse(5, 14, 14, 20, WHITE, outline=None)
    s.save(I + "Accessories/SkyInABottle.png")

    s = Sprite(30, 14)                                        # 3D glasses
    s.rect(0, 2, 13, 12, RED)
    s.rect(16, 2, 29, 12, BLUE)
    s.rect(13, 5, 16, 7, BLACK)
    s.save(I + "Accessories/ThreeDGlasses.png")

    s = Sprite(28, 24)                                        # Explosive Healing Kit
    s.rect(1, 4, 26, 22, WHITE)
    s.rect(11, 8, 16, 19, RED, outline=None)
    s.rect(7, 11, 20, 16, RED, outline=None)
    s.rect(9, 0, 18, 5, DARKGREY)
    s.save(I + "Accessories/ExplosiveHealingKit.png")

    s = Sprite(24, 24)                                        # TM Flash: a disc
    s.ellipse(0, 0, 23, 23, YELLOW)
    s.ellipse(8, 8, 15, 15, WHITE)
    s.save(I + "Accessories/TMFlash.png")

    s = Sprite(30, 20)                                        # Liquid Snake: flipper + snake
    s.poly([(0, 6), (20, 2), (29, 10), (20, 18), (0, 14)], DARKBLUE)
    s.line(4, 10, 24, 10, GREEN, 2)
    s.save(I + "Accessories/LiquidSnake.png")

    s = pack(RED)
    s.poly([(6, 24), (14, 30), (22, 24)], ORANGE)
    s.save(I + "Accessories/RocketPack.png")

    for name, col in [("DrillerPack", GREY), ("DrillerPackHellstone", ORANGE), ("DrillerPackChlorophyte", LIME), ("DrillerPackLuminite", CYAN)]:
        s = pack(col)
        s.poly([(10, 2), (18, 2), (14, 12)], SILVER)
        s.save(I + "Accessories/" + name + ".png")

    # ---- Armor ----
    s = Sprite(22, 20)
    s.ellipse(1, 0, 20, 19, TAN)
    s.rect(4, 11, 17, 16, DARKRED)
    for i in range(4):
        s.rect(5 + i * 3, 11, 6 + i * 3, 13, WHITE, outline=None)
    s.save(I + "Armor/MuncherMask.png")
    s = Sprite(26, 20)
    s.rect(1, 2, 24, 19, TAN)
    s.rect(6, 6, 19, 14, DARKRED)
    for i in range(4):
        s.rect(7 + i * 3, 6, 8 + i * 3, 8, WHITE, outline=None)
    s.save(I + "Armor/MuncherShirt.png")
    s = Sprite(26, 18)
    s.rect(1, 6, 11, 17, TAN); s.rect(14, 6, 24, 17, TAN)
    s.rect(3, 12, 9, 15, DARKRED); s.rect(16, 12, 22, 15, DARKRED)
    s.save(I + "Armor/MuncherShoes.png")
    gen_armor_sheets()

    # ---- Consumables ----
    bottle((200, 230, 255, 90)).save(I + "Consumables/DrinkableAir.png")
    bottle(ORANGE, label=RED).save(I + "Consumables/DrinkableLava.png")
    s = Sprite(24, 24)                                        # Solid Snake: cardboard box
    s.rect(1, 6, 22, 22, BROWN)
    s.line(1, 6, 12, 1, BROWN, 2); s.line(12, 1, 22, 6, BROWN, 2)
    s.line(11, 7, 11, 21, DARKBROWN)
    s.save(I + "Consumables/SolidSnake.png")
    s = Sprite(28, 16)                                        # 3D movie ticket
    s.rect(0, 0, 27, 15, YELLOW)
    s.rect(2, 2, 9, 13, RED, outline=None); s.rect(18, 2, 25, 13, BLUE, outline=None)
    s.save(I + "Consumables/ThreeDMovieTicket.png")
    s = Sprite(18, 30)                                        # Weedy Beer: a brown bottle with a leaf
    s.rect(6, 0, 11, 8, BROWN); s.rect(2, 8, 15, 29, DARKBROWN)
    s.ellipse(5, 14, 12, 22, GREEN)
    s.save(I + "Consumables/WeedyBeer.png")

    s = Sprite(20, 22)                                        # Delirium Pills
    s.rect(3, 2, 16, 21, ORANGE); s.rect(3, 2, 16, 6, WHITE)
    s.save(I + "Consumables/DeliriumPills.png")
    s = Sprite(26, 20)                                        # Pink Slip
    s.rect(0, 0, 25, 19, PINK)
    s.text_ish(3, 5, 6, DARKRED); s.text_ish(3, 9, 5, DARKRED); s.text_ish(3, 13, 6, DARKRED)
    s.save(I + "Consumables/PinkSlip.png")
    s = Sprite(26, 18)                                        # Moon Shoes
    s.rect(1, 6, 11, 17, WHITE); s.rect(14, 6, 24, 17, WHITE)
    s.rect(1, 14, 11, 17, GREY); s.rect(14, 14, 24, 17, GREY)
    s.ellipse(3, 1, 9, 7, YELLOW); s.ellipse(16, 1, 22, 7, YELLOW)
    s.save(I + "Consumables/MoonShoes.png")

    # ---- Placeables ----
    s = Sprite(32, 26)                                        # Alchemical Dehydrator
    s.rect(0, 6, 31, 25, GREY)
    s.rect(4, 10, 27, 21, DARKGREY)
    s.ellipse(8, 0, 23, 10, PURPLE)
    s.save(I + "Placeable/AlchemicalDehydrator.png")
    for name, col in [("RainInABottle", BLUE), ("StormInABottle", DARKBLUE), ("BlizzardInABottle", WHITE),
                      ("SunshineInABottle", YELLOW), ("SandstormInABottle", TAN), ("JellyStormInABottle", PINK),
                      ("FrogRainInABottle", GREEN)]:
        bottle(col).save(I + "Placeable/" + name + ".png")
    s = Sprite(20, 14)                                        # Slippery Frog item
    s.ellipse(0, 2, 19, 13, GREEN)
    s.ellipse(3, 0, 8, 5, WHITE); s.ellipse(11, 0, 16, 5, WHITE)
    s.px(5, 2, BLACK); s.px(13, 2, BLACK)
    s.save(I + "Consumables/SlipperyFrogItem.png")


def gen_armor_sheets():
    """Equip texture sheets. Head/Legs: 40x1120 (20 frames of 56px). Body: 360x224 (9 columns x 4 rows of 40x56)."""
    A = "Content/Items/Armor/"
    head = Sprite(40, 1120)
    for f in range(20):
        y = f * 56
        head.ellipse(9, y + 2, 30, y + 22, TAN)
        head.rect(12, y + 14, 27, y + 19, DARKRED)
        for i in range(5):
            head.rect(13 + i * 3, y + 14, 14 + i * 3, y + 16, WHITE, outline=None)
    head.save(A + "MuncherMask_Head.png")

    body = Sprite(360, 224)
    for row in range(4):
        for col in range(9):
            x, y = col * 40, row * 56
            body.rect(x + 10, y + 20, x + 29, y + 40, TAN)
            body.rect(x + 13, y + 25, x + 26, y + 32, DARKRED)
            for i in range(4):
                body.rect(x + 14 + i * 3, y + 25, x + 15 + i * 3, y + 27, WHITE, outline=None)
    body.save(A + "MuncherShirt_Body.png")

    legs = Sprite(40, 1120)
    for f in range(20):
        y = f * 56
        legs.rect(12, y + 40, 18, y + 54, TAN)
        legs.rect(21, y + 40, 27, y + 54, TAN)
        legs.rect(13, y + 48, 17, y + 51, DARKRED)
        legs.rect(22, y + 48, 26, y + 51, DARKRED)
    legs.save(A + "MuncherShoes_Legs.png")


# ---------------------------------------------------------------------------------------------------------------------
# Projectiles
# ---------------------------------------------------------------------------------------------------------------------
def gen_projectiles():
    P = "Content/Projectiles/"

    s = Sprite(12, 12); s.ellipse(0, 0, 11, 11, WHITE); s.ellipse(4, 3, 8, 7, BLACK, outline=None); s.save(P + "GooglyBullet.png")
    s = Sprite(8, 16); s.rect(2, 0, 5, 15, GOLD); s.save(P + "YoureLongBullet.png")
    s = Sprite(8, 16); s.rect(2, 0, 5, 15, GOLD); s.line(3, 2, 3, 12, BLUE); s.save(P + "YoureTallBullet.png")
    s = Sprite(8, 16); s.rect(2, 0, 5, 15, PINK); s.save(P + "BuddyBullet.png")
    s = Sprite(16, 16); s.ellipse(0, 0, 15, 15, (255, 0, 255, 200)); s.save(P + "ChaosSolutionSpray.png")
    s = Sprite(14, 14); s.ellipse(0, 0, 13, 13, (250, 250, 230, 220)); s.save(P + "GlueGlob.png")

    s = Sprite(60, 60)                                        # Can explosion
    s.ellipse(0, 0, 59, 59, (255, 180, 60, 180), outline=ORANGE)
    s.ellipse(15, 15, 44, 44, (255, 240, 200, 220), outline=None)
    s.save(P + "CanExplosion.png")

    s = Sprite(12, 180); s.rect(0, 0, 11, 179, WHITE); s.save(P + "PongPaddle.png")
    s = Sprite(12, 220); s.rect(0, 0, 11, 219, LIME); s.save(P + "GreenPongPaddle.png")
    s = Sprite(16, 16); s.rect(0, 0, 15, 15, LIME); s.save(P + "GreenPongBall.png")
    s = Sprite(16, 16); s.rect(0, 0, 15, 15, WHITE); s.save(P + "PongBall.png")

    rat = Sprite(22, 12)
    rat.ellipse(2, 2, 17, 11, GREY)
    rat.ellipse(14, 1, 21, 8, GREY)
    rat.line(0, 7, 3, 5, PINK, 1)
    rat.px(18, 3, BLACK); rat.ellipse(16, 0, 18, 2, PINK, outline=None)
    rat.save(P + "RatBullet.png")
    rat.save(P + "PocketRat.png")

    s = Sprite(18, 18); s.poly([(9, 0), (17, 9), (9, 17), (1, 9)], SILVER); s.save(P + "GparrlingHookProjectile.png")
    s = Sprite(10, 10); s.ellipse(0, 0, 9, 9, LIGHTBLUE); s.save(P + "ToolGunBolt.png")

    s = Sprite(90, 90)                                        # Rocket pack explosion
    s.ellipse(0, 0, 89, 89, (255, 120, 40, 170), outline=RED)
    s.ellipse(20, 20, 69, 69, (255, 230, 120, 220), outline=None)
    s.save(P + "RocketPackExplosion.png")

    s = Sprite(40, 24)                                        # Rocket body drawn over the player while dashing
    s.poly([(0, 6), (28, 4), (39, 12), (28, 20), (0, 18)], RED)
    s.rect(4, 9, 14, 15, LIGHTBLUE)
    s.poly([(0, 6), (6, 0), (8, 8)], DARKRED); s.poly([(0, 18), (6, 24), (8, 16)], DARKRED)
    s.save(P + "RocketPackRocket.png")

    s = Sprite(36, 24)                                        # Drill body
    s.poly([(0, 4), (24, 2), (35, 12), (24, 22), (0, 20)], GREY)
    for i in range(4):
        s.line(24 + i * 3, 6 + i, 24 + i * 3, 18 - i, DARKGREY)
    s.save(P + "DrillerPackDrill.png")



def chibi(path, hair, cloth, frames=4, ears=False, glow=None):
    """Floating chibi placeholder: big head, tiny body. frames stacked vertically, 32x36 each."""
    w, h = 32, 36
    s = Sprite(w, h * frames)
    for f in range(frames):
        y = f * h + (f % 2)
        s.rect(11, y + 20, 20, y + 32, cloth)                    # body
        s.rect(9, y + 32, 13, y + 35, cloth); s.rect(18, y + 32, 22, y + 35, cloth)
        s.ellipse(5, y + 2, 26, y + 22, SKIN)                     # head
        s.ellipse(5, y + 0, 26, y + 12, hair)                     # hair
        s.rect(4, y + 8, 8, y + 18, hair); s.rect(23, y + 8, 27, y + 18, hair)
        if ears:
            s.poly([(6, y + 4), (9, y - 2 + 4), (12, y + 4)], hair); s.poly([(19, y + 4), (22, y - 2 + 4), (25, y + 4)], hair)
        s.px(12, y + 14, BLACK); s.px(19, y + 14, BLACK)
        s.px(12, y + 13, CYAN if glow is None else glow); s.px(19, y + 13, CYAN if glow is None else glow)
    s.save(path)


def gen_pets():
    chibi("Content/Pets/Monst3rProjectile.png", (230, 230, 240, 255), BLACK, glow=(220, 60, 60, 255))
    chibi("Content/Pets/KaltsitProjectile.png", (235, 235, 230, 255), WHITE, ears=True, glow=(90, 200, 140, 255))
    s = Sprite(20, 24)                                        # Monst3r item: a red-eyed data chip
    s.rect(2, 2, 17, 21, BLACK); s.ellipse(6, 8, 13, 15, RED)
    s.save("Content/Pets/Monst3rItem.png")
    s = Sprite(20, 24)                                        # Kal'tsit item: a glowing green medical crystal
    s.poly([(10, 1), (18, 12), (10, 23), (2, 12)], (120, 230, 160, 255))
    s.save("Content/Pets/KaltsitItem.png")


# ---------------------------------------------------------------------------------------------------------------------
# NPCs
# ---------------------------------------------------------------------------------------------------------------------
def gen_npcs():
    N = "Content/NPCs/"

    # Tin Can: 2 frames, 18x26 each
    s = Sprite(18, 52)
    for f in range(2):
        y = f * 26
        s.rect(2, y + 2 + f, 15, y + 23, SILVER)
        s.ellipse(2, y + f, 15, y + 5 + f, LIGHTGREY)
        s.rect(4, y + 10, 13, y + 16, RED)
    s.save(N + "TinCan.png")

    # Glue Slime: 2 frames of 32x24, a translucent white blob
    s = Sprite(32, 48)
    for f in range(2):
        y = f * 24
        squash = f * 3
        s.ellipse(1, y + 4 + squash, 30, y + 23, (250, 250, 235, 220), outline=(200, 200, 180, 255))
        s.ellipse(9, y + 8 + squash, 13, y + 13 + squash, BLACK, outline=None); s.ellipse(19, y + 8 + squash, 23, y + 13 + squash, BLACK, outline=None)
        s.ellipse(5, y + 7 + squash, 9, y + 10 + squash, (255, 255, 255, 160), outline=None)
    s.save(N + "GlueSlime.png")

    # Slippery Frog: 4 frames of 22x16
    s = Sprite(22, 64)
    for f in range(4):
        y = f * 16
        s.ellipse(1, y + 4, 20, y + 15, GREEN)
        s.ellipse(4, y + 1, 9, y + 7, WHITE); s.ellipse(12, y + 1, 17, y + 7, WHITE)
        s.px(6 + (f % 2), y + 3, BLACK); s.px(14 + (f % 2), y + 3, BLACK)
        s.line(2, y + 14, 6, y + 15 - (f % 2), DARKGREEN, 2)
    s.save(N + "SlipperyFrog.png")

    def zombie_sheet(path, frames, w, h, outfit, tie=None, hat=None, briefcase=False, sitting=False, scale=1):
        s = Sprite(w, h * frames)
        for f in range(frames):
            y = f * h
            bob = (f % 2) * scale
            cx = w // 2
            hw = 7 * scale
            # legs
            if sitting:
                s.rect(cx - 9 * scale, y + h - 10 * scale, cx - 2 * scale, y + h - 2, DARKGREY)
                s.rect(cx + 2 * scale, y + h - 10 * scale, cx + 9 * scale, y + h - 2, DARKGREY)
            else:
                lo = (f % 4) * scale
                s.rect(cx - 6 * scale, y + h - 16 * scale, cx - 1 * scale, y + h - 2 - lo, DARKGREY)
                s.rect(cx + 1 * scale, y + h - 16 * scale - lo, cx + 6 * scale, y + h - 2, DARKGREY)
            # torso
            s.rect(cx - 8 * scale, y + 14 * scale + bob, cx + 8 * scale, y + h - 15 * scale, outfit)
            if tie:
                s.rect(cx - 1 * scale, y + 15 * scale + bob, cx + 1 * scale, y + h - 20 * scale, tie)
            # arms outstretched like a zombie
            s.rect(cx + 8 * scale, y + 16 * scale + bob, cx + 15 * scale, y + 19 * scale + bob, ZOMBIE)
            # head
            s.ellipse(cx - hw, y + 2 * scale + bob, cx + hw, y + 15 * scale + bob, ZOMBIE)
            s.px(cx - 3 * scale, y + 7 * scale + bob, BLACK); s.px(cx + 3 * scale, y + 7 * scale + bob, BLACK)
            s.line(cx - 3 * scale, y + 11 * scale + bob, cx + 3 * scale, y + 11 * scale + bob, DARKRED)
            if hat:
                s.rect(cx - hw - 2, y + 1 + bob, cx + hw + 2, y + 3 * scale + bob, hat)
                s.rect(cx - hw + 2, y - 4 * scale + bob + 4, cx + hw - 2, y + 2 * scale + bob, hat)
            if briefcase:
                s.rect(cx - 16 * scale, y + h - 24 * scale, cx - 9 * scale, y + h - 17 * scale, DARKBROWN)
        s.save(path)

    zombie_sheet(N + "ZombieSalesman.png", 4, 36, 48, LIGHTGREY, tie=RED, briefcase=True)
    zombie_sheet(N + "ZombieIntern.png", 2, 36, 48, WHITE, tie=BLUE, sitting=True)
    zombie_sheet(N + "ZombieCEO.png", 4, 72, 96, BLACK, tie=GOLD, hat=DARKGREY, briefcase=True, scale=2)

    # Boss head icon for the map (22x22-ish)
    s = Sprite(24, 24)
    s.ellipse(2, 4, 21, 23, ZOMBIE)
    s.rect(0, 2, 23, 6, DARKGREY); s.rect(5, 0, 18, 4, DARKGREY)
    s.px(8, 12, BLACK); s.px(15, 12, BLACK)
    s.save(N + "ZombieCEO_Head_Boss.png")


# ---------------------------------------------------------------------------------------------------------------------
# Buffs (32x32)
# ---------------------------------------------------------------------------------------------------------------------
def gen_buffs():
    B = "Content/Buffs/"

    def buff(name, draw, folder=B):
        s = Sprite(32, 32)
        s.rect(0, 0, 31, 31, DARKGREY, outline=BLACK)
        draw(s)
        s.save(folder + name + ".png")

    # Pet buffs live next to their pet classes in Content/Pets (tModLoader looks for textures beside the class's namespace path).
    buff("Monst3rBuff", lambda s: (s.ellipse(8, 8, 23, 23, (230, 230, 240, 255)), s.ellipse(13, 13, 18, 18, RED)), "Content/Pets/")
    buff("KaltsitBuff", lambda s: (s.ellipse(8, 8, 23, 23, (235, 235, 230, 255)), s.ellipse(13, 13, 18, 18, (90, 200, 140, 255))), "Content/Pets/")

    buff("GlueDebuff", lambda s: (s.ellipse(6, 6, 25, 25, WHITE), s.ellipse(12, 14, 20, 28, (255, 255, 200, 255))))
    buff("ThreeDDebuff", lambda s: (s.rect(3, 10, 14, 22, RED), s.rect(17, 10, 28, 22, BLUE)))
    buff("LifeLinkDebuff", lambda s: (s.ellipse(4, 8, 15, 19, RED), s.ellipse(16, 12, 27, 23, RED), s.line(10, 14, 22, 18, PINK, 2)))
    buff("CardboardBox", lambda s: (s.rect(4, 10, 27, 28, BROWN), s.line(4, 10, 16, 4, BROWN, 2), s.line(16, 4, 27, 10, BROWN, 2)))
    buff("Winded", lambda s: (s.ellipse(6, 6, 25, 25, LIGHTBLUE), s.text_ish(10, 15, 4, WHITE)))
    buff("PongBuff", lambda s: (s.rect(4, 8, 7, 24, WHITE), s.rect(24, 8, 27, 24, WHITE), s.rect(14, 14, 17, 17, WHITE)))
    buff("Delirious", lambda s: (s.ellipse(6, 6, 25, 25, BLACK), s.ellipse(11, 11, 20, 20, PURPLE)))
    buff("CorporateSynergy", lambda s: (s.rect(6, 6, 25, 25, GOLD), s.text_ish(9, 12, 5, BLACK), s.text_ish(9, 18, 5, BLACK)))
    buff("Weedy", lambda s: (s.rect(10, 6, 21, 26, DARKBROWN), s.ellipse(12, 12, 19, 20, GREEN)))
    buff("Rocketing", lambda s: (s.poly([(6, 24), (16, 4), (26, 24)], RED), s.poly([(12, 24), (16, 30), (20, 24)], ORANGE)))


# ---------------------------------------------------------------------------------------------------------------------
# Tiles
# ---------------------------------------------------------------------------------------------------------------------
def gen_tiles():
    T = "Content/Tiles/"

    # Cloud platform: 27 frames of 18x18 (16px tile + 2px padding)
    s = Sprite(486, 18)
    for f in range(27):
        x = f * 18
        s.ellipse(x, 2, x + 15, 13, WHITE, outline=LIGHTGREY)
        s.ellipse(x + 4, 0, x + 11, 8, WHITE, outline=None)
    s.save(T + "CloudPlatform.png")

    # Alchemical Dehydrator: 3x2 tiles (54x36 with 18px cells)
    s = Sprite(54, 36)
    s.rect(0, 10, 53, 35, GREY)
    s.rect(6, 16, 47, 31, DARKGREY)
    s.ellipse(14, 0, 39, 18, PURPLE)
    s.rect(20, 20, 33, 27, LIME)
    s.save(T + "AlchemicalDehydratorTile.png")

    # Weather beacon: 7 styles of 2x3 (each 36x54), laid out horizontally -> 252x54
    cols = [BLUE, DARKBLUE, WHITE, YELLOW, TAN, PINK, GREEN]
    s = Sprite(36 * len(cols), 54)
    for i, c in enumerate(cols):
        x = i * 36
        s.rect(x + 8, 20, x + 27, 53, DARKGREY)                # pedestal
        s.rect(x + 13, 4, x + 22, 9, TAN)                      # cork
        s.rect(x + 12, 8, x + 23, 14, LIGHTGREY)
        s.ellipse(x + 4, 12, x + 31, 40, c)                     # bottle
    s.save(T + "WeatherBeaconTile.png")


# ---------------------------------------------------------------------------------------------------------------------
# Misc textures (UI overlays, zombie icon)
# ---------------------------------------------------------------------------------------------------------------------
def gen_misc():
    M = "Assets/Textures/"
    # radial darkness mask for TM Flash: transparent centre, opaque edge (512x512)
    size = 512
    im = Image.new("RGBA", (size, size), (0, 0, 0, 255))
    px = im.load()
    c = size / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c) / c
            a = 0 if d < 0.55 else min(255, int((d - 0.55) / 0.11 * 255))
            px[x, y] = (0, 0, 0, a)
    path = os.path.join(ROOT, M + "DarknessMask.png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path); print("wrote", M + "DarknessMask.png")

    s = Sprite(1, 1); s.px(0, 0, WHITE); s.save(M + "Pixel.png")

    # Googly eye parts drawn over NPCs hit by Googly Eye Ammo
    s = Sprite(14, 14); s.ellipse(0, 0, 13, 13, WHITE); s.save(M + "GooglyEye.png")
    s = Sprite(6, 6); s.ellipse(0, 0, 5, 5, BLACK, outline=None); s.save(M + "GooglyPupil.png")

    # Walking zombie icon for the inventory takeover (4 frames of 24x24)
    s = Sprite(24, 96)
    for f in range(4):
        y = f * 24
        lo = (f % 2) * 2
        s.rect(6, y + 14, 10, y + 23 - lo, DARKGREY); s.rect(13, y + 14 - lo, 17, y + 23, DARKGREY)
        s.rect(4, y + 8, 19, y + 16, LIGHTGREY)
        s.rect(19, y + 9, 23, y + 11, ZOMBIE)
        s.ellipse(6, y + 0, 17, y + 10, ZOMBIE)
        s.px(9, y + 4, BLACK); s.px(14, y + 4, BLACK)
    s.save(M + "ZombieIcon.png")


# ---------------------------------------------------------------------------------------------------------------------
# Sounds (22 kHz mono 16-bit WAV, synthesised)
# ---------------------------------------------------------------------------------------------------------------------
def write_wav(rel, samples, rate=22050):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 32000)) for v in samples))
    print("wrote", rel)


def gen_sounds():
    S = "Assets/Sounds/"
    rate = 22050

    # Fart: low frequency sputtering with pitch drop and noise bursts
    n = int(rate * 1.1)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / rate
        env = math.exp(-t * 2.2) * (0.65 + 0.35 * math.sin(t * 47) * math.sin(t * 13))
        freq = 95 - t * 45 + 10 * math.sin(t * 31)
        phase += 2 * math.pi * freq / rate
        sput = 1.0 if math.sin(t * 120) > -0.3 else 0.25
        v = (math.copysign(1, math.sin(phase)) * 0.6 + math.sin(phase * 2) * 0.3 + random.uniform(-0.25, 0.25)) * env * sput
        out.append(v)
    write_wav(S + "Fart.wav", out)

    # Lawnmower: a buzzy sawtooth with engine wobble
    n = int(rate * 0.9)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / rate
        env = min(1, t * 20) * (1 if t < 0.75 else max(0, 1 - (t - 0.75) * 6.6))
        freq = 70 + 8 * math.sin(t * 9)
        phase += freq / rate
        saw = 2 * (phase - math.floor(phase)) - 1
        out.append((saw * 0.5 + random.uniform(-0.2, 0.2)) * env * 0.8)
    write_wav(S + "Mower.wav", out)

    # Pong blip
    n = int(rate * 0.08)
    write_wav(S + "Pong.wav", [math.copysign(0.5, math.sin(2 * math.pi * 440 * i / rate)) * (1 - i / n) for i in range(n)])

    # CEO shout: descending harsh tone
    n = int(rate * 0.6)
    out = []
    phase = 0.0
    for i in range(n):
        t = i / rate
        env = min(1, t * 40) * max(0, 1 - t / 0.6)
        freq = 220 - t * 120 + 25 * math.sin(t * 60)
        phase += 2 * math.pi * freq / rate
        out.append((math.sin(phase) * 0.4 + math.sin(phase * 3) * 0.3 + random.uniform(-0.15, 0.15)) * env)
    write_wav(S + "CEOShout.wav", out)

    # Rat squeak
    n = int(rate * 0.12)
    write_wav(S + "Squeak.wav", [math.sin(2 * math.pi * (2400 + 800 * math.sin(i / rate * 90)) * i / rate) * 0.35 * (1 - i / n) for i in range(n)])


def gen_icons():
    """Workshop/menu icons: icon.png must be 80x80, icon_small.png 30x30."""
    s = Sprite(80, 80)
    s.rect(10, 20, 69, 76, BROWN)
    s.rect(24, 8, 55, 24, DARKBROWN)
    s.ellipse(20, 36, 34, 50, RED); s.ellipse(44, 36, 58, 50, BLUE)
    s.ellipse(26, 30, 52, 60, (255, 255, 255, 0), outline=YELLOW)
    s.text_ish(26, 66, 9, YELLOW)
    s.save("icon.png")
    s = Sprite(30, 30)
    s.rect(3, 8, 26, 28, BROWN)
    s.rect(9, 3, 20, 9, DARKBROWN)
    s.ellipse(7, 14, 13, 20, RED); s.ellipse(16, 14, 22, 20, BLUE)
    s.save("icon_small.png")


if __name__ == "__main__":
    gen_items()
    gen_projectiles()
    gen_npcs()
    gen_pets()
    gen_buffs()
    gen_tiles()
    gen_misc()
    gen_sounds()
    gen_icons()
    print("done")
