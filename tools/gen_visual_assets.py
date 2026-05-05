#!/usr/bin/env python3
"""Generate pixel PNGs for LoreLegacyMonsters (world, combat, element silhouettes). Standard library only."""
from __future__ import annotations

import os
import struct
import zlib
from typing import Callable

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), ".."))
OUT_WORLD = os.path.join(ROOT, "Assets", "Resources", "Sprites", "World")
OUT_COMBAT = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Combat")
OUT_ELEM = os.path.join(ROOT, "Assets", "Resources", "Sprites", "Elements")

# Cozy palette
C = {
    "ink": (43, 30, 24),
    "cream": (250, 232, 184),
    "grass": (126, 173, 87),
    "grass_dark": (95, 141, 78),
    "forest": (47, 103, 67),
    "moss": (95, 141, 78),
    "mud": (89, 74, 58),
    "marsh": (89, 142, 114),
    "marsh_dark": (45, 112, 95),
    "water": (77, 166, 154),
    "stone": (146, 145, 131),
    "ruin": (139, 132, 112),
    "sand": (183, 164, 120),
    "sky": (121, 184, 216),
    "sky_glow": (214, 238, 208),
    "road": (183, 134, 86),
    "shard": (182, 217, 255),
    "spire": (180, 168, 200),
    "danger": (217, 94, 69),
    "fire": (231, 106, 69),
}


def write_png(path: str, w: int, h: int, pixels_rgba: bytes) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    assert len(pixels_rgba) == w * h * 4
    raw = b"".join(
        b"\x00" + pixels_rgba[y * w * 4 : (y + 1) * w * 4] for y in range(h)
    )
    comp = zlib.compress(raw, 9)

    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", comp) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def solid(w, h, rgba: tuple[int, int, int, int]) -> bytes:
    r, g, b, a = rgba
    return bytes([r, g, b, a]) * (w * h)


def noise_tile(w: int, h: int, base: tuple, var: int, dots: list[tuple]) -> bytes:
    """Simple tileable grass/dirt: base fill + sparse brighter pixels."""
    buf = bytearray(w * h * 4)
    br, bg, bb = base[:3]
    for y in range(h):
        for x in range(w):
            i = (y * w + x) * 4
            buf[i : i + 4] = bytes([br, bg, bb, 255])
    for x, y, c in dots:
        if 0 <= x < w and 0 <= y < h:
            i = (y * w + x) * 4
            buf[i : i + 3] = bytes(c)
    return bytes(buf)


def blit(buf: bytearray, bw: int, bh: int, sx: int, sy: int, data: bytes, dw: int, dh: int, replace: bool = False):
    for y in range(dh):
        for x in range(dw):
            si = ((sy + y) * bw + (sx + x)) * 4
            if si < 0 or si + 3 >= len(buf):
                continue
            di = (y * dw + x) * 4
            r, g, b, a = data[di], data[di + 1], data[di + 2], data[di + 3]
            if a == 0:
                continue
            if replace or a == 255:
                buf[si : si + 4] = bytes([r, g, b, 255])
            else:
                # alpha blend
                ar = buf[si]
                ag = buf[si + 1]
                ab = buf[si + 2]
                ta = a / 255.0
                buf[si] = int(r * ta + ar * (1 - ta))
                buf[si + 1] = int(g * ta + ag * (1 - ta))
                buf[si + 2] = int(b * ta + ab * (1 - ta))
                buf[si + 3] = 255


def ellipse_rgba(w: int, h: int, cx: float, cy: float, rx: float, ry: float, rgba: tuple[int, int, int, int]) -> bytes:
    buf = bytearray(w * h * 4)
    r, g, b, a = rgba
    for y in range(h):
        for x in range(w):
            dx = (x - cx) / max(rx, 0.01)
            dy = (y - cy) / max(ry, 0.01)
            if dx * dx + dy * dy <= 1.0:
                i = (y * w + x) * 4
                buf[i : i + 4] = bytes([r, g, b, a])
    return bytes(buf)


def gen_grass_tile() -> bytes:
    dots = []
    for i in range(48):
        dots.append(((i * 7 + 3) % 32, (i * 11 + 5) % 32, C["grass_dark"]))
    return noise_tile(32, 32, C["grass"], 0, dots)


def gen_ground_marsh() -> bytes:
    dots = []
    for i in range(40):
        dots.append(((i * 5) % 32, (i * 13) % 32, C["mud"]))
    return noise_tile(32, 32, C["marsh"], 0, dots)


def gen_ground_stone() -> bytes:
    dots = []
    for i in range(56):
        dots.append(((i * 3 + 1) % 32, (i * 17) % 32, C["stone"]))
    base = tuple((C["stone"][i] - 18) % 256 for i in range(3))
    return noise_tile(32, 32, base, 0, dots)


def gen_ground_ruins() -> bytes:
    dots = []
    for i in range(50):
        dots.append(((i * 9) % 32, (i * 7) % 32, C["stone"]))
    return noise_tile(32, 32, C["ruin"], 0, dots)


def gen_ground_delta() -> bytes:
    dots = []
    for i in range(35):
        dots.append(((i * 11) % 32, (i * 5) % 32, C["marsh_dark"]))
    return noise_tile(32, 32, C["sand"], 0, dots)


def gen_parallax(w: int, h: int, build: Callable[[bytearray], None]) -> bytes:
    buf = bytearray(solid(w, h, (0, 0, 0, 0)))
    build(buf)
    return bytes(buf)


def parallax_generic(buf: bytearray, color_main: tuple, peaks: list[int]):
    bw, bh = 160, 56
    for y in range(bh):
        for x in range(bw):
            top = peaks[x % len(peaks)]
            a = 230 if y >= bh - top else 0
            i = (y * bw + x) * 4
            if a:
                buf[i : i + 4] = (*color_main, 255)
            else:
                buf[i : i + 4] = (0, 0, 0, 0)


def gen_battle_bg(w: int, h: int, sky: tuple, mid: tuple, ground: tuple, accent_fn: Callable[[bytearray], None] | None):
    buf = bytearray(w * h * 4)
    horizon = int(h * 0.42)
    for y in range(h):
        for x in range(w):
            i = (y * w + x) * 4
            if y < horizon:
                t = y / max(horizon, 1)
                r = int(sky[0] * (1 - t) + mid[0] * t)
                g_ = int(sky[1] * (1 - t) + mid[1] * t)
                b = int(sky[2] * (1 - t) + mid[2] * t)
                buf[i : i + 4] = (r, g_, b, 255)
            else:
                t = (y - horizon) / max(h - horizon, 1)
                r = int(mid[0] * (1 - t) + ground[0] * t)
                g_ = int(mid[1] * (1 - t) + ground[1] * t)
                b = int(mid[2] * (1 - t) + ground[2] * t)
                buf[i : i + 4] = (r, g_, b, 255)
    if accent_fn:
        accent_fn(buf)
    return bytes(buf)


def add_ground_stripes(buf: bytearray, w: int, h: int, y0: int, color: tuple):
    for x in range(0, w, 6):
        for yy in range(y0, min(h, y0 + 5)):
            for dx in range(3):
                if x + dx < w:
                    i = (yy * w + x + dx) * 4
                    buf[i : i + 4] = (*color, 255)


def silhouette(shape: str, w: int, h: int, fill: tuple[int, int, int, int]) -> bytes:
    buf = bytearray(solid(w, h, (0, 0, 0, 0)))
    r, g, b, a = fill
    if shape == "neutral":  # round critter + ears
        e = ellipse_rgba(w, h, w * 0.5, h * 0.58, w * 0.35, h * 0.28, (r, g, b, a))
        buf[:] = e
        for ear_x in (int(w * 0.28), int(w * 0.72)):
            for yy in range(int(h * 0.22)):
                for xx in range(ear_x - 6, ear_x + 6):
                    if 0 <= xx < w and 0 <= yy < h:
                        i = (yy * w + int(xx)) * 4
                        buf[i : i + 4] = (r, g, b, a)
    elif shape == "fire":  # flame ears
        e = ellipse_rgba(w, h, w * 0.5, h * 0.55, w * 0.32, h * 0.3, (r, g, b, a))
        buf[:] = e
        for t in range(16):
            for yy in range(h // 2, h // 2 + t):
                xx = int(w * 0.5 - 8 + (t % 3) * 6)
                if 0 <= xx < w and 0 <= yy < h:
                    i = (yy * w + xx) * 4
                    buf[i : i + 4] = (r, g, b, a)
    elif shape == "water":  # teardrop / fish
        e = ellipse_rgba(w, h, w * 0.52, h * 0.56, w * 0.38, h * 0.26, (r, g, b, a))
        buf[:] = e
        for yy in range(int(h * 0.15), int(h * 0.45)):
            for xx in range(int(w * 0.65), int(w * 0.9)):
                i = (yy * w + xx) * 4
                buf[i : i + 4] = (r, g, b, a)
    elif shape == "nature":  # leaf tail
        e = ellipse_rgba(w, h, w * 0.48, h * 0.55, w * 0.33, h * 0.29, (r, g, b, a))
        buf[:] = e
        for yy in range(int(h * 0.35), h - 4):
            for xx in range(int(w * 0.2), int(w * 0.45)):
                i = (yy * w + xx) * 4
                buf[i : i + 4] = (r, g, b, a)
    elif shape == "lightning":  # jagged
        e = ellipse_rgba(w, h, w * 0.5, h * 0.52, w * 0.28, h * 0.32, (r, g, b, a))
        buf[:] = e
        pts = [(32, 8), (38, 24), (28, 28), (40, 44), (30, 56)]
        for i in range(len(pts) - 1):
            x0, y0 = pts[i]
            x1, y1 = pts[i + 1]
            steps = max(abs(x1 - x0), abs(y1 - y0), 1)
            for s in range(steps + 1):
                xx = int(x0 + (x1 - x0) * s / steps)
                yy = int(y0 + (y1 - y0) * s / steps)
                for d in range(-2, 3):
                    for e2 in range(-2, 3):
                        px, py = xx + d, yy + e2
                        if 0 <= px < w and 0 <= py < h:
                            j = (py * w + px) * 4
                            buf[j : j + 4] = (r, g, b, a)
    elif shape == "stone":  # blocky
        for yy in range(int(h * 0.25), h - 6):
            for xx in range(int(w * 0.3), int(w * 0.72)):
                i = (yy * w + xx) * 4
                buf[i : i + 4] = (r, g, b, a)
        for yy in range(int(h * 0.12), int(h * 0.35)):
            for xx in range(int(w * 0.38), int(w * 0.62)):
                i = (yy * w + xx) * 4
                buf[i : i + 4] = (r, g, b, a)
    elif shape == "shadow":  # wisp
        e = ellipse_rgba(w, h, w * 0.5, h * 0.52, w * 0.36, h * 0.22, (r, g, b, int(a * 0.85)))
        buf[:] = e
        e2 = ellipse_rgba(w, h, w * 0.35, h * 0.42, w * 0.12, h * 0.18, (20, 10, 40, int(a * 0.9)))
        blit(buf, w, h, 0, 0, e2, w, h)
    return bytes(buf)


def gen_prop_lamp(w: int, h: int) -> bytes:
    buf = bytearray(solid(w, h, (0, 0, 0, 0)))
    # pole
    for y in range(4, h - 2):
        for x in range(w // 2 - 1, w // 2 + 2):
            i = (y * w + x) * 4
            buf[i : i + 4] = (*C["road"], 255)
    # lamp head
    for yy in range(2, 10):
        for xx in range(w // 2 - 6, w // 2 + 6):
            i = (yy * w + xx) * 4
            buf[i : i + 4] = (*C["cream"], 255)
    glow = ellipse_rgba(w, h, w * 0.5, 6.0, 9.0, 5.5, (255, 230, 150, 120))
    blit(buf, w, h, 0, 0, glow, w, h)
    return bytes(buf)


def gen_prop_rock(w: int, h: int) -> bytes:
    buf = bytearray(solid(w, h, (0, 0, 0, 0)))
    e = ellipse_rgba(w, h, w * 0.48, h * 0.55, w * 0.35, h * 0.35, (*C["stone"], 255))
    buf[:] = e
    return bytes(buf)


def gen_prop_shard(w: int, h: int) -> bytes:
    buf = bytearray(solid(w, h, (0, 0, 0, 0)))
    pts = [(w // 2, 2), (w - 4, h - 6), (6, h - 4)]
    from math import floor

    def tri_area(ax, ay, bx, by, cx, cy):
        return abs((bx - ax) * (cy - ay) - (cx - ax) * (by - ay))

    ax, ay = pts[0]
    bx, by = pts[1]
    cx, cy = pts[2]
    for y in range(h):
        for x in range(w):
            a1 = tri_area(ax, ay, bx, by, x, y)
            a2 = tri_area(bx, by, cx, cy, x, y)
            a3 = tri_area(cx, cy, ax, ay, x, y)
            if abs(a1 + a2 + a3 - tri_area(ax, ay, bx, by, cx, cy)) < 2:
                i = (y * w + x) * 4
                buf[i : i + 4] = (*C["shard"], 255)
    return bytes(buf)


def gen_prop_stump(w: int, h: int) -> bytes:
    buf = bytearray(solid(w, h, (0, 0, 0, 0)))
    for y in range(h // 2, h - 2):
        for x in range(w // 2 - 8, w // 2 + 8):
            i = (y * w + x) * 4
            buf[i : i + 4] = (*C["mud"], 255)
    for y in range(h // 2 - 6, h // 2 + 2):
        for x in range(w // 2 - 9, w // 2 + 9):
            i = (y * w + x) * 4
            buf[i : i + 4] = (*C["road"], 255)
    return bytes(buf)


def main():
    os.makedirs(OUT_WORLD, exist_ok=True)
    os.makedirs(OUT_COMBAT, exist_ok=True)
    os.makedirs(OUT_ELEM, exist_ok=True)

    write_png(os.path.join(OUT_WORLD, "ground_grass_tile.png"), 32, 32, gen_grass_tile())
    write_png(os.path.join(OUT_WORLD, "ground_marsh.png"), 32, 32, gen_ground_marsh())
    write_png(os.path.join(OUT_WORLD, "ground_stone.png"), 32, 32, gen_ground_stone())
    write_png(os.path.join(OUT_WORLD, "ground_ruins.png"), 32, 32, gen_ground_ruins())
    write_png(os.path.join(OUT_WORLD, "ground_delta.png"), 32, 32, gen_ground_delta())
    write_png(os.path.join(OUT_WORLD, "ground_ridge.png"), 32, 32, gen_ground_stone())
    write_png(os.path.join(OUT_WORLD, "ground_spire.png"), 32, 32, gen_ground_stone())

    # Parallax layers (160x56 RGBA)
    for name, fn in [
        ("parallax_town", lambda b: parallax_generic(b, C["cream"], [8, 12, 10, 14, 9, 11, 13, 10, 12])),
        ("parallax_route", lambda b: parallax_generic(b, C["grass_dark"], [6, 8, 7, 9, 8, 7])),
        ("parallax_forest", lambda b: parallax_generic(b, C["forest"], [14, 18, 16, 20, 17, 19])),
        ("parallax_grove", lambda b: parallax_generic(b, C["grass"], [10, 12, 11, 13])),
        ("parallax_marsh", lambda b: parallax_generic(b, C["marsh_dark"], [5, 7, 6, 8, 5])),
        ("parallax_ruins", lambda b: parallax_generic(b, C["ruin"], [9, 11, 10, 12])),
        ("parallax_delta", lambda b: parallax_generic(b, C["water"], [4, 6, 5, 7, 5])),
        ("parallax_ridge", lambda b: parallax_generic(b, C["stone"], [16, 22, 18, 24, 20])),
        ("parallax_spire", lambda b: parallax_generic(b, C["spire"], [20, 26, 30, 28, 32])),
    ]:
        buf = bytearray(160 * 56 * 4)
        fn(buf)
        write_png(os.path.join(OUT_WORLD, f"{name}.png"), 160, 56, bytes(buf))

    write_png(os.path.join(OUT_WORLD, "prop_lamp.png"), 24, 32, gen_prop_lamp(24, 32))
    write_png(os.path.join(OUT_WORLD, "prop_rock.png"), 20, 16, gen_prop_rock(20, 16))
    write_png(os.path.join(OUT_WORLD, "prop_shard.png"), 18, 22, gen_prop_shard(18, 22))
    write_png(os.path.join(OUT_WORLD, "prop_stump.png"), 22, 16, gen_prop_stump(22, 16))
    write_png(os.path.join(OUT_WORLD, "flower_wild.png"), 12, 14, gen_flower())

    # Element silhouettes 64x64
    col = {
        "Neutral": (215, 200, 161, 255),
        "Fire": (231, 106, 69, 255),
        "Water": (90, 173, 214, 255),
        "Nature": (130, 188, 95, 255),
        "Lightning": (242, 202, 85, 255),
        "Stone": (166, 152, 122, 255),
        "Shadow": (126, 98, 168, 255),
    }
    shapes = {
        "Neutral": "neutral",
        "Fire": "fire",
        "Water": "water",
        "Nature": "nature",
        "Lightning": "lightning",
        "Stone": "stone",
        "Shadow": "shadow",
    }
    for name, sh in shapes.items():
        px = silhouette(sh, 64, 64, col[name])
        write_png(os.path.join(OUT_ELEM, f"Silhouette_{name}.png"), 64, 64, px)

    # Battle backgrounds 384x216
    def acc_route(b):
        add_ground_stripes(b, 384, 216, 145, C["road"])

    def acc_forest(b):
        for x in range(0, 384, 20):
            for y in range(90, 140):
                i = (y * 384 + x) * 4
                b[i : i + 4] = (*C["forest"], 255)

    backgrounds = [
        ("bg_town", C["sky"], C["sky_glow"], C["grass"], None),
        ("bg_route", C["sky"], C["grass_dark"], C["road"], acc_route),
        ("bg_forest", C["sky"], C["forest"], C["grass_dark"], acc_forest),
        ("bg_grove", C["sky"], C["grass"], C["grass_dark"], None),
        ("bg_marsh", C["sky"], C["marsh"], C["mud"], None),
        ("bg_ruins", (140, 150, 170), C["ruin"], C["stone"], None),
        ("bg_delta", C["sky"], C["water"], C["sand"], None),
        ("bg_ridge", (150, 160, 185), C["stone"], C["mud"], None),
        ("bg_spire", (100, 120, 180), C["spire"], C["stone"], None),
    ]
    for name, sk, mid, gr, acc in backgrounds:
        px = gen_battle_bg(384, 216, sk, mid, gr, acc)
        write_png(os.path.join(OUT_COMBAT, f"{name}.png"), 384, 216, px)

    print("Wrote pixel assets to Resources/Sprites")


def gen_flower() -> bytes:
    buf = bytearray(12 * 14 * 4)
    for y in range(14):
        for x in range(12):
            i = (y * 12 + x) * 4
            buf[i : i + 4] = (0, 0, 0, 0)
    for y in range(5, 12):
        for x in range(5, 7):
            i = (y * 12 + x) * 4
            buf[i : i + 4] = (*C["forest"], 255)
    for dx, dy in [(-3, 2), (3, 2), (0, -3), (-2, -2), (2, -2)]:
        cx, cy = 6 + dx, 4 + dy
        for yy in range(cy - 1, cy + 2):
            for xx in range(cx - 1, cx + 2):
                if 0 <= xx < 12 and 0 <= yy < 14:
                    i = (yy * 12 + xx) * 4
                    buf[i : i + 4] = (243, 192, 87, 255)
    return bytes(buf)


if __name__ == "__main__":
    main()
