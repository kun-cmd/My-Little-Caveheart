from pathlib import Path
from PIL import Image, ImageDraw

OUT_DIR = Path("Assets/Resources/Sprites/Caveheart")
FRAME = 64
STATES = ["sleeping", "startled", "resisting", "settled", "sitting"]
FRAMES = 4

SKIN = (210, 142, 84, 255)
SKIN_DARK = (178, 105, 68, 255)
HAIR = (58, 38, 28, 255)
BLANKET = (88, 125, 174, 255)
BLANKET_DARK = (62, 92, 142, 255)
EYE = (36, 24, 20, 255)
TEAR = (92, 170, 255, 230)
BLUSH = (255, 122, 108, 110)
SHADOW = (35, 28, 26, 80)


def rect(draw, xy, fill):
    draw.rectangle([int(v) for v in xy], fill=fill)


def ellipse(draw, xy, fill):
    draw.ellipse([int(v) for v in xy], fill=fill)


def line(draw, xy, fill, width=2):
    draw.line([int(v) for p in xy for v in p], fill=fill, width=width)


def draw_face(draw, expr, x, y, scale=1.0):
    if expr == "sleepy":
        line(draw, [(x - 7 * scale, y), (x - 2 * scale, y - 1 * scale)], EYE, 2)
        line(draw, [(x + 5 * scale, y), (x + 10 * scale, y - 1 * scale)], EYE, 2)
        line(draw, [(x - 2 * scale, y + 8 * scale), (x + 6 * scale, y + 8 * scale)], EYE, 2)
    elif expr == "scared":
        ellipse(draw, (x - 9 * scale, y - 4 * scale, x - 3 * scale, y + 3 * scale), EYE)
        ellipse(draw, (x + 5 * scale, y - 4 * scale, x + 11 * scale, y + 3 * scale), EYE)
        line(draw, [(x - 11 * scale, y - 7 * scale), (x - 3 * scale, y - 10 * scale)], EYE, 2)
        line(draw, [(x + 4 * scale, y - 10 * scale), (x + 12 * scale, y - 7 * scale)], EYE, 2)
        ellipse(draw, (x - 3 * scale, y + 8 * scale, x + 6 * scale, y + 14 * scale), EYE)
    elif expr == "safe":
        line(draw, [(x - 9 * scale, y), (x - 3 * scale, y + 2 * scale)], EYE, 2)
        line(draw, [(x + 5 * scale, y + 2 * scale), (x + 11 * scale, y)], EYE, 2)
        line(draw, [(x - 3 * scale, y + 8 * scale), (x + 5 * scale, y + 10 * scale)], EYE, 2)
        ellipse(draw, (x - 12 * scale, y + 4 * scale, x + 14 * scale, y + 11 * scale), BLUSH)


def draw_hair(draw, cx, cy, w=24, h=14):
    ellipse(draw, (cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2), HAIR)
    rect(draw, (cx - w // 2, cy, cx + w // 2, cy + h // 2), HAIR)


def sleeping(frame):
    img = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    bob = [0, 1, 0, -1][frame]
    ellipse(d, (17, 42, 48, 52), SHADOW)
    ellipse(d, (17, 26 + bob, 49, 47 + bob), SKIN)
    draw_hair(d, 29, 26 + bob, 28, 14)
    ellipse(d, (16, 35 + bob, 53, 55 + bob), BLANKET)
    rect(d, (17, 44 + bob, 53, 55 + bob), BLANKET_DARK)
    draw_face(d, "sleepy", 32, 35 + bob, 0.8)
    return img


def startled(frame):
    img = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    shake = [-1, 1, -1, 1][frame]
    ellipse(d, (19, 47, 47, 56), SHADOW)
    ellipse(d, (21 + shake, 20, 45 + shake, 43), SKIN)
    draw_hair(d, 32 + shake, 19, 25, 13)
    rect(d, (23 + shake, 40, 43 + shake, 55), BLANKET)
    line(d, [(24 + shake, 36), (13 + shake, 25)], SKIN_DARK, 4)
    line(d, [(42 + shake, 36), (53 + shake, 25)], SKIN_DARK, 4)
    draw_face(d, "scared", 33 + shake, 30, 0.8)
    return img


def resisting(frame):
    img = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    shake = [-2, 1, -1, 2][frame]
    ellipse(d, (17, 48, 49, 57), SHADOW)
    ellipse(d, (21 + shake, 20, 45 + shake, 43), SKIN)
    draw_hair(d, 31 + shake, 18, 28, 15)
    rect(d, (20 + shake, 42, 48 + shake, 56), BLANKET)
    line(d, [(23 + shake, 36), (10 + shake, 39)], SKIN_DARK, 4)
    line(d, [(43 + shake, 36), (56 + shake, 39)], SKIN_DARK, 4)
    draw_face(d, "scared", 33 + shake, 31, 0.82)
    ellipse(d, (24 + shake, 36 + frame % 2, 27 + shake, 43 + frame % 2), TEAR)
    ellipse(d, (41 + shake, 36, 44 + shake, 45), TEAR)
    return img


def settled(frame):
    img = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    bob = [0, 0, 1, 0][frame]
    ellipse(d, (18, 47, 48, 56), SHADOW)
    ellipse(d, (21, 22 + bob, 45, 45 + bob), SKIN)
    draw_hair(d, 32, 21 + bob, 25, 13)
    rect(d, (21, 43 + bob, 47, 55 + bob), BLANKET)
    line(d, [(23, 38 + bob), (18, 45 + bob)], SKIN_DARK, 3)
    line(d, [(43, 38 + bob), (48, 45 + bob)], SKIN_DARK, 3)
    draw_face(d, "safe", 33, 32 + bob, 0.82)
    return img


def sitting(frame):
    img = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    bob = [0, -1, 0, 1][frame]
    ellipse(d, (18, 52, 48, 59), SHADOW)
    rect(d, (23, 34 + bob, 43, 55 + bob), SKIN)
    ellipse(d, (20, 14 + bob, 46, 39 + bob), SKIN)
    draw_hair(d, 32, 14 + bob, 28, 14)
    rect(d, (19, 50, 49, 58), BLANKET)
    line(d, [(23, 39 + bob), (13, 46 + bob)], SKIN_DARK, 3)
    line(d, [(43, 39 + bob), (53, 46 + bob)], SKIN_DARK, 3)
    draw_face(d, "safe", 33, 25 + bob, 0.9)
    return img


DRAWERS = {
    "sleeping": sleeping,
    "startled": startled,
    "resisting": resisting,
    "settled": settled,
    "sitting": sitting,
}


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for state in STATES:
        strip = Image.new("RGBA", (FRAME * FRAMES, FRAME), (0, 0, 0, 0))
        for i in range(FRAMES):
            frame = DRAWERS[state](i)
            frame.save(OUT_DIR / f"{state}_{i}.png")
            strip.alpha_composite(frame, (FRAME * i, 0))
        strip.save(OUT_DIR / f"{state}_strip.png")

    preview = Image.new("RGBA", (FRAME * FRAMES, FRAME * len(STATES)), (0, 0, 0, 0))
    for row, state in enumerate(STATES):
        strip = Image.open(OUT_DIR / f"{state}_strip.png")
        preview.alpha_composite(strip, (0, FRAME * row))
    preview.save(OUT_DIR / "caveheart_animation_preview.png")


if __name__ == "__main__":
    main()
