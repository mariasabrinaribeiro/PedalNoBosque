"""
Baia do Avesso — Procedural Hand-Painted Coastal Textures (512x512 seamless)
+ 36 Custom Illustrated Cozy Cardapio Icons (128x128 RGBA)
Runs inside Blender 5.3 Python (zero external dependencies — pure math + zlib PNG writer!).
"""

import math
import os
import struct
import zlib

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
ORLA_DIR = os.path.dirname(BASE_DIR)
TEX_DIR = os.path.join(ORLA_DIR, "Textures")
ICON_DIR = os.path.join(ORLA_DIR, "Resources", "CardapioIcons")
os.makedirs(TEX_DIR, exist_ok=True)
os.makedirs(ICON_DIR, exist_ok=True)


def clamp01(v):
    return 0.0 if v < 0.0 else (1.0 if v > 1.0 else v)


def lerp(a, b, t):
    return a + (b - a) * t


def hex_rgb(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0)


def write_png_rgba(path, w, h, pixels):
    """pixels: flat bytearray of length w*h*4 (top-to-bottom rows, RGBA 0..255)."""
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    raw = bytearray()
    stride = w * 4
    for y in range(h):
        raw.append(0)
        raw.extend(pixels[y * stride:(y + 1) * stride])

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(bytes(raw), 6)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def hash2(ix, iy, seed=0):
    n = (ix * 374761393 + iy * 668265263 + seed * 1442695041) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFFFF) / 16777215.0


def tile_noise(u, v, freq, seed=0):
    x = u * freq
    y = v * freq
    ix = int(math.floor(x))
    iy = int(math.floor(y))
    fx = x - ix
    fy = y - iy
    ux = fx * fx * (3.0 - 2.0 * fx)
    uy = fy * fy * (3.0 - 2.0 * fy)
    i0 = ix % freq
    i1 = (ix + 1) % freq
    j0 = iy % freq
    j1 = (iy + 1) % freq
    a = hash2(i0, j0, seed)
    b = hash2(i1, j0, seed)
    c = hash2(i0, j1, seed)
    d = hash2(i1, j1, seed)
    return lerp(lerp(a, b, ux), lerp(c, d, ux), uy)


def fbm_tile(u, v, base_freq=8, seed=0):
    val = 0.0
    amp = 0.5
    f = base_freq
    for o in range(3):
        val += tile_noise(u, v, f, seed + o * 17) * amp
        f *= 2
        amp *= 0.5
    return val / 0.875


# =========================================================================
# 1. SEAMLESS HAND-PAINTED COASTAL TEXTURES (256x256 cozy painterly tiles!)
# =========================================================================
def generate_textures():
    sz = 256

    def make_tex(name, fn):
        buf = bytearray(sz * sz * 4)
        idx = 0
        for y in range(sz):
            v = y / sz
            for x in range(sz):
                u = x / sz
                r, g, b = fn(u, v)
                buf[idx] = int(clamp01(r) * 255)
                buf[idx + 1] = int(clamp01(g) * 255)
                buf[idx + 2] = int(clamp01(b) * 255)
                buf[idx + 3] = 255
                idx += 4
        out = os.path.join(TEX_DIR, name)
        write_png_rgba(out, sz, sz, buf)
        print("[Texture]", out)

    # 1. Cream Coastal Walkway Paver Tiles
    def walkway(u, v):
        rows, cols = 4, 4
        gv = v * rows
        row_i = int(math.floor(gv))
        gu = (u * cols) + (0.5 if row_i % 2 == 1 else 0.0)
        col_i = int(math.floor(gu)) % cols
        fu = gu - math.floor(gu)
        fv = gv - math.floor(gv)
        joint = 0.045
        is_joint = (fu < joint or fu > 1.0 - joint or fv < joint or fv > 1.0 - joint)
        tile_var = (hash2(col_i, row_i, 11) - 0.5) * 0.06
        grain = (fbm_tile(u, v, 12, 3) - 0.5) * 0.05
        if is_joint:
            return (0.83 + grain, 0.78 + grain, 0.70 + grain)
        return (0.96 + tile_var + grain, 0.94 + tile_var + grain, 0.90 + tile_var + grain)

    # 2. Golden Beach Sand with Gentle Wind Ripples & Micro-Pebbles
    def sand(u, v):
        ripple = math.sin(v * math.pi * 12.0 + math.sin(u * math.pi * 4.0) * 0.8) * 0.028
        grain = (fbm_tile(u, v, 16, 21) - 0.5) * 0.07
        speck = 0.045 if tile_noise(u, v, 64, 99) > 0.91 else 0.0
        b = 0.96 + ripple + grain + speck
        return (b, b * 0.97, b * 0.92)

    # 3. Soft Painterly Coastal Grass with Subtle Blade Clusters
    def grass(u, v):
        soft = (fbm_tile(u, v, 8, 41) - 0.5) * 0.11
        tuft = 0.06 if tile_noise(u, v, 24, 53) > 0.76 else 0.0
        clover = 0.04 if tile_noise(u, v, 32, 77) > 0.88 else 0.0
        return (0.90 + soft + clover, 0.97 + soft + tuft, 0.91 + soft)

    # 4. Warm Light Coastal Wood Planks (for Pier, Stalls, Benches, Tables)
    def wood(u, v):
        planks = 6
        pv = v * planks
        pi = int(math.floor(pv)) % planks
        fv = pv - math.floor(pv)
        seam = fv < 0.05 or fv > 0.95
        grain = math.sin((v * 48.0 + fbm_tile(u, v, 8, 61) * 3.5)) * 0.035
        plank_var = (hash2(pi, 0, 71) - 0.5) * 0.08
        if seam:
            return (0.72, 0.65, 0.58)
        return (0.97 + plank_var + grain, 0.94 + plank_var + grain, 0.89 + plank_var + grain)

    # 5. Cozy Cobblestone Plaza (for Feira)
    def cobble(u, v):
        rows, cols = 6, 6
        gv = v * rows
        ri = int(math.floor(gv))
        gu = u * cols + (0.5 if ri % 2 == 1 else 0.0)
        ci = int(math.floor(gu)) % cols
        fu = (gu - math.floor(gu)) - 0.5
        fv = (gv - math.floor(gv)) - 0.5
        d = math.sqrt(fu * fu * 1.15 + fv * fv * 1.15)
        var = (hash2(ci, ri, 83) - 0.5) * 0.10
        grain = (fbm_tile(u, v, 14, 87) - 0.5) * 0.04
        if d > 0.44:
            return (0.80 + grain, 0.76 + grain, 0.70 + grain)
        return (0.97 + var + grain, 0.94 + var + grain, 0.90 + var + grain)

    # 6. Soft Woven Coastal Awning Fabric
    def fabric(u, v):
        weave = (math.sin(u * math.pi * 64.0) * math.sin(v * math.pi * 64.0)) * 0.025
        soft = (fbm_tile(u, v, 8, 105) - 0.5) * 0.04
        return (0.98 + weave + soft, 0.98 + weave + soft, 0.98 + weave + soft)

    make_tex("Tex_WalkwayTiles.png", walkway)
    make_tex("Tex_BeachSand.png", sand)
    make_tex("Tex_CoastalGrass.png", grass)
    make_tex("Tex_WoodPlanks.png", wood)
    make_tex("Tex_CobbleFeira.png", cobble)
    make_tex("Tex_StripedAwning.png", fabric)


# =========================================================================
# 2. MINI VECTOR CANVAS FOR 36 CRISP ILLUSTRATED COZY CARDAPIO ICONS (128x128)
# =========================================================================
class Canvas:
    def __init__(self, sz=128):
        self.sz = sz
        self.buf = [0.0] * (sz * sz * 4)

    def blend(self, x, y, r, g, b, a):
        if x < 0 or y < 0 or x >= self.sz or y >= self.sz or a <= 0.001:
            return
        i = (y * self.sz + x) * 4
        oa = self.buf[i + 3]
        na = a + oa * (1.0 - a)
        if na > 0.0001:
            self.buf[i] = (r * a + self.buf[i] * oa * (1.0 - a)) / na
            self.buf[i + 1] = (g * a + self.buf[i + 1] * oa * (1.0 - a)) / na
            self.buf[i + 2] = (b * a + self.buf[i + 2] * oa * (1.0 - a)) / na
            self.buf[i + 3] = na

    def circle(self, cx, cy, rad, col_hex, outline_hex=None, outline_w=3.0):
        r, g, b = hex_rgb(col_hex)
        or_, og_, ob_ = hex_rgb(outline_hex) if outline_hex else (0, 0, 0)
        x0 = max(0, int(cx - rad - outline_w - 2))
        x1 = min(self.sz - 1, int(cx + rad + outline_w + 2))
        y0 = max(0, int(cy - rad - outline_w - 2))
        y1 = min(self.sz - 1, int(cy + rad + outline_w + 2))
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
                if outline_hex:
                    oa = clamp01(rad + outline_w - d)
                    if oa > 0:
                        self.blend(x, y, or_, og_, ob_, oa)
                fa = clamp01(rad - d)
                if fa > 0:
                    self.blend(x, y, r, g, b, fa)

    def ellipse(self, cx, cy, rx, ry, col_hex, outline_hex=None, outline_w=2.8):
        r, g, b = hex_rgb(col_hex)
        or_, og_, ob_ = hex_rgb(outline_hex) if outline_hex else (0, 0, 0)
        x0 = max(0, int(cx - rx - outline_w - 2))
        x1 = min(self.sz - 1, int(cx + rx + outline_w + 2))
        y0 = max(0, int(cy - ry - outline_w - 2))
        y1 = min(self.sz - 1, int(cy + ry + outline_w + 2))
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                dx = (x + 0.5 - cx) / max(1.0, rx)
                dy = (y + 0.5 - cy) / max(1.0, ry)
                d = math.hypot(dx, dy)
                px_scale = min(rx, ry)
                if outline_hex:
                    oa = clamp01((1.0 + outline_w / px_scale - d) * px_scale)
                    if oa > 0:
                        self.blend(x, y, or_, og_, ob_, oa)
                fa = clamp01((1.0 - d) * px_scale)
                if fa > 0:
                    self.blend(x, y, r, g, b, fa)

    def rrect(self, x0, y0, x1, y1, rad, col_hex, outline_hex=None, outline_w=2.8):
        r, g, b = hex_rgb(col_hex)
        or_, og_, ob_ = hex_rgb(outline_hex) if outline_hex else (0, 0, 0)
        cx, cy = (x0 + x1) * 0.5, (y0 + y1) * 0.5
        hx, hy = (x1 - x0) * 0.5 - rad, (y1 - y0) * 0.5 - rad
        bx0 = max(0, int(x0 - outline_w - 2))
        bx1 = min(self.sz - 1, int(x1 + outline_w + 2))
        by0 = max(0, int(y0 - outline_w - 2))
        by1 = min(self.sz - 1, int(y1 + outline_w + 2))
        for y in range(by0, by1 + 1):
            for x in range(bx0, bx1 + 1):
                qx = abs(x + 0.5 - cx) - hx
                qy = abs(y + 0.5 - cy) - hy
                d = math.hypot(max(qx, 0.0), max(qy, 0.0)) + min(max(qx, qy), 0.0) - rad
                if outline_hex:
                    oa = clamp01(outline_w - d)
                    if oa > 0:
                        self.blend(x, y, or_, og_, ob_, oa)
                fa = clamp01(-d)
                if fa > 0:
                    self.blend(x, y, r, g, b, fa)

    def seg(self, x0, y0, x1, y1, thickness, col_hex):
        r, g, b = hex_rgb(col_hex)
        rad = thickness * 0.5
        vx, vy = x1 - x0, y1 - y0
        l2 = max(0.001, vx * vx + vy * vy)
        bx0 = max(0, int(min(x0, x1) - rad - 2))
        bx1 = min(self.sz - 1, int(max(x0, x1) + rad + 2))
        by0 = max(0, int(min(y0, y1) - rad - 2))
        by1 = min(self.sz - 1, int(max(y0, y1) + rad + 2))
        for y in range(by0, by1 + 1):
            for x in range(bx0, bx1 + 1):
                t = clamp01(((x + 0.5 - x0) * vx + (y + 0.5 - y0) * vy) / l2)
                d = math.hypot(x + 0.5 - (x0 + vx * t), y + 0.5 - (y0 + vy * t))
                a = clamp01(rad - d)
                if a > 0:
                    self.blend(x, y, r, g, b, a)

    def tri(self, p0, p1, p2, col_hex):
        r, g, b = hex_rgb(col_hex)
        xs = [p0[0], p1[0], p2[0]]
        ys = [p0[1], p1[1], p2[1]]
        bx0, bx1 = max(0, int(min(xs) - 2)), min(self.sz - 1, int(max(xs) + 2))
        by0, by1 = max(0, int(min(ys) - 2)), min(self.sz - 1, int(max(ys) + 2))

        def edge(a, b_, px, py):
            return (px - a[0]) * (b_[1] - a[1]) - (py - a[1]) * (b_[0] - a[0])

        area = edge(p0, p1, p2[0], p2[1])
        if abs(area) < 0.001:
            return
        sgn = 1.0 if area > 0 else -1.0
        for y in range(by0, by1 + 1):
            for x in range(bx0, bx1 + 1):
                px, py = x + 0.5, y + 0.5
                e0 = edge(p0, p1, px, py) * sgn
                e1 = edge(p1, p2, px, py) * sgn
                e2 = edge(p2, p0, px, py) * sgn
                if e0 >= -1.0 and e1 >= -1.0 and e2 >= -1.0:
                    self.blend(x, y, r, g, b, 1.0)

    def save(self, filename):
        out = bytearray(self.sz * self.sz * 4)
        for i, v in enumerate(self.buf):
            out[i] = int(clamp01(v) * 255)
        path = os.path.join(ICON_DIR, filename)
        write_png_rgba(path, self.sz, self.sz, out)


OUT = "#5A4235"


def draw_fries(cheddar=False):
    c = Canvas()
    for i, (fx, fh) in enumerate([(34, 24), (46, 18), (58, 22), (70, 17), (82, 25), (90, 30)]):
        c.rrect(fx - 5, fh, fx + 5, 76, 3, "#FFD859" if i % 2 == 0 else "#F5BE3F", OUT, 2.2)
    if cheddar:
        c.ellipse(64, 48, 28, 12, "#FF9B26", OUT, 2.0)
        c.circle(52, 54, 7, "#FF9B26")
        c.circle(74, 56, 8, "#FF9B26")
    c.rrect(28, 56, 100, 108, 12, "#E85D6F", OUT, 3.0)
    c.rrect(36, 66, 92, 98, 8, "#F27988")
    c.circle(64, 82, 10, "#FFF4D9")
    return c


def draw_pastel():
    c = Canvas()
    c.rrect(20, 38, 108, 92, 12, "#F2BE66", OUT, 3.2)
    c.rrect(28, 46, 100, 84, 8, "#F8D182")
    for x in range(28, 102, 10):
        c.seg(x, 40, x, 46, 2.5, "#C98C36")
        c.seg(x, 84, x, 90, 2.5, "#C98C36")
    c.ellipse(64, 65, 24, 11, "#FCE2A4")
    return c


def draw_acai():
    c = Canvas()
    c.ellipse(64, 76, 42, 28, "#F5E9D6", OUT, 3.0)
    c.ellipse(64, 58, 38, 20, "#6E2E68", OUT, 2.8)
    for bx, by in [(46, 54), (60, 50), (74, 55)]:
        c.circle(bx, by, 7, "#FFE27A", OUT, 1.8)
    c.circle(82, 48, 8, "#F25C78", OUT, 1.8)
    c.circle(52, 62, 6, "#E3B878")
    c.circle(68, 63, 5, "#E3B878")
    return c


def draw_grilled_fish():
    c = Canvas()
    c.ellipse(64, 66, 48, 30, "#FAF2E4", OUT, 2.6)
    c.tri((90, 66), (112, 48), (112, 84), "#D99252")
    c.ellipse(60, 66, 36, 18, "#E8A664", OUT, 2.8)
    for gx in (44, 58, 72):
        c.seg(gx, 54, gx - 5, 78, 3.2, "#8E5329")
    c.circle(36, 63, 3.5, OUT)
    c.circle(78, 82, 8, "#FFE055", OUT, 2.0)
    return c


def draw_burger():
    c = Canvas()
    c.rrect(24, 82, 104, 102, 10, "#E5A55A", OUT, 2.8)
    c.rrect(22, 68, 106, 84, 7, "#7A452D", OUT, 2.5)
    c.rrect(24, 60, 104, 70, 4, "#FFD24C")
    c.rrect(22, 54, 106, 62, 5, "#68BA68")
    c.ellipse(64, 52, 41, 24, "#EDA95E", OUT, 3.0)
    for sx, sy in [(46, 42), (62, 38), (78, 43), (54, 48), (72, 49)]:
        c.circle(sx, sy, 2.5, "#FFF7DC")
    return c


def draw_water():
    c = Canvas()
    c.rrect(44, 34, 84, 108, 12, "#A6E2F5", OUT, 2.8)
    c.rrect(52, 18, 76, 36, 5, "#5EBCE8", OUT, 2.5)
    c.rrect(46, 58, 82, 82, 4, "#FFFFFF")
    c.circle(64, 70, 8, "#4BB4E6")
    c.seg(52, 42, 52, 96, 4, "#DFF6FF")
    return c


def draw_coconut():
    c = Canvas()
    c.seg(68, 42, 86, 16, 6, "#F26392")
    c.circle(64, 72, 36, "#5BB563", OUT, 3.0)
    c.ellipse(64, 46, 22, 10, "#FAF4E6", OUT, 2.4)
    c.ellipse(64, 46, 13, 5, "#9BE2EE")
    c.circle(48, 78, 8, "#7CC97E")
    return c


def draw_soda():
    c = Canvas()
    c.rrect(40, 30, 88, 106, 10, "#4CB872", OUT, 2.8)
    c.rrect(44, 24, 84, 34, 4, "#DCE5EE", OUT, 2.2)
    c.rrect(42, 54, 86, 82, 4, "#FAF2D6")
    c.circle(64, 68, 10, "#E85468")
    c.circle(64, 68, 4, "#5A4235")
    return c


def draw_juice(col_hex, garnish_hex):
    c = Canvas()
    c.seg(74, 42, 92, 18, 5, "#FAF2E4")
    c.rrect(38, 40, 90, 106, 12, col_hex, OUT, 2.8)
    c.rrect(44, 48, 84, 58, 4, "#FFFFFF")
    c.circle(86, 42, 13, garnish_hex, OUT, 2.2)
    c.circle(86, 42, 8, "#FFF8D8")
    return c


def draw_cone(scoop_hex, leaf=False, triple=False):
    c = Canvas()
    c.tri((44, 66), (84, 66), (64, 114), "#E3A75B")
    c.seg(44, 66, 64, 114, 3, OUT)
    c.seg(84, 66, 64, 114, 3, OUT)
    if triple:
        c.circle(50, 56, 18, "#F27B96", OUT, 2.4)
        c.circle(78, 56, 18, "#FFE075", OUT, 2.4)
        c.circle(64, 40, 19, "#8CE0C8", OUT, 2.6)
    else:
        c.circle(64, 50, 24, scoop_hex, OUT, 2.8)
        c.circle(56, 42, 7, "#FFFFFF")
    if leaf:
        c.ellipse(80, 32, 11, 6, "#4BB864", OUT, 2.0)
    return c


def draw_brigadeiro():
    c = Canvas()
    c.rrect(36, 72, 92, 104, 8, "#FAF2E4", OUT, 2.6)
    c.circle(64, 56, 26, "#6B3F2B", OUT, 2.8)
    for sx, sy in [(52, 46), (66, 42), (76, 52), (58, 60), (70, 62), (48, 58)]:
        c.circle(sx, sy, 3.2, "#3D2217")
    return c


def draw_cocada():
    c = Canvas()
    c.rrect(24, 56, 104, 96, 14, "#F5E2BC", OUT, 2.8)
    c.rrect(32, 46, 96, 82, 14, "#FFF6E2", OUT, 2.6)
    for sx, sy in [(48, 62), (66, 58), (82, 64), (58, 72), (74, 74)]:
        c.seg(sx - 5, sy, sx + 5, sy + 2, 2.8, "#D4AE74")
    return c


def draw_cupcake():
    c = Canvas()
    c.rrect(36, 66, 92, 106, 10, "#F5A8C0", OUT, 2.8)
    c.ellipse(64, 56, 32, 22, "#FAF2E4", OUT, 2.6)
    c.ellipse(64, 42, 22, 14, "#F26392", OUT, 2.4)
    c.circle(64, 28, 9, "#E04462", OUT, 2.2)
    return c


def draw_sushi_combo():
    c = Canvas()
    c.rrect(16, 42, 112, 94, 12, "#8B5E3C", OUT, 2.8)
    c.rrect(28, 52, 62, 82, 10, "#FFFFFF", OUT, 2.2)
    c.rrect(26, 48, 64, 66, 10, "#F78759", OUT, 2.2)
    c.circle(88, 68, 17, "#2E3540", OUT, 2.4)
    c.circle(88, 68, 12, "#FFFFFF")
    c.circle(88, 68, 6, "#F78759")
    return c


def draw_hotroll():
    c = Canvas()
    for cx, cy in [(46, 68), (80, 62), (64, 82)]:
        c.circle(cx, cy, 20, "#D99548", OUT, 2.6)
        c.circle(cx, cy, 13, "#FFFFFF")
        c.circle(cx, cy, 7, "#F78759")
    return c


def draw_temaki():
    c = Canvas()
    c.tri((32, 34), (96, 34), (64, 112), "#2F4238")
    c.ellipse(64, 38, 30, 14, "#FFFFFF", OUT, 2.4)
    c.circle(54, 36, 9, "#F78759")
    c.circle(72, 36, 8, "#6BC26C")
    return c


def draw_ceviche():
    c = Canvas()
    c.ellipse(64, 74, 44, 26, "#88C9E8", OUT, 2.8)
    c.ellipse(64, 62, 36, 18, "#FAF4EA", OUT, 2.2)
    for fx, fy, ch in [(48, 60, "#F7A688"), (64, 58, "#FFE27A"), (78, 62, "#C9669E"), (60, 68, "#68BA68")]:
        c.circle(fx, fy, 7, ch)
    return c


def draw_sunomono():
    c = Canvas()
    c.ellipse(64, 76, 42, 24, "#F5ECD8", OUT, 2.8)
    for cx, cy in [(48, 64), (66, 58), (80, 66), (58, 72)]:
        c.circle(cx, cy, 11, "#55AE62", OUT, 2.0)
        c.circle(cx, cy, 7, "#A8E6A4")
    return c


def draw_sushi_boat():
    c = Canvas()
    c.ellipse(64, 78, 50, 22, "#B98050", OUT, 3.0)
    c.rrect(24, 62, 104, 78, 8, "#D8A26E", OUT, 2.2)
    for sx, col in [(40, "#F78759"), (64, "#F26392"), (88, "#FFE075")]:
        c.rrect(sx - 10, 48, sx + 10, 68, 6, col, OUT, 2.2)
    return c


def draw_shirt(col_hex):
    c = Canvas()
    c.rrect(22, 36, 106, 62, 10, col_hex, OUT, 2.8)
    c.rrect(38, 34, 90, 104, 10, col_hex, OUT, 2.8)
    c.ellipse(64, 36, 14, 9, "#FAF2E4", OUT, 2.2)
    c.circle(64, 66, 10, "#FFF5DC")
    return c


def draw_hat():
    c = Canvas()
    c.ellipse(64, 68, 48, 18, "#ECCE86", OUT, 2.8)
    c.ellipse(64, 52, 28, 20, "#F2D892", OUT, 2.8)
    c.rrect(36, 56, 92, 66, 4, "#F26392")
    # Cute sunglasses below brim
    c.circle(48, 92, 12, "#354050", OUT, 2.4)
    c.circle(80, 92, 12, "#354050", OUT, 2.4)
    c.seg(58, 90, 70, 90, 4, "#354050")
    return c


def draw_bandana():
    c = Canvas()
    c.tri((20, 42), (108, 42), (64, 102), "#F26392")
    c.rrect(20, 34, 108, 50, 8, "#E04C7C", OUT, 2.8)
    c.circle(64, 66, 10, "#FFF5DC")
    c.circle(54, 56, 4, "#FFF5DC")
    c.circle(74, 56, 4, "#FFF5DC")
    return c


def draw_skate():
    c = Canvas()
    c.rrect(18, 54, 110, 78, 12, "#F26392", OUT, 2.8)
    c.rrect(32, 60, 96, 72, 6, "#4CC6B8")
    c.circle(36, 86, 10, "#FFE075", OUT, 2.4)
    c.circle(92, 86, 10, "#FFE075", OUT, 2.4)
    return c


def draw_scooter():
    c = Canvas()
    c.seg(84, 24, 92, 88, 7, "#4CC6B8")
    c.seg(70, 24, 98, 24, 7, "#F26392")
    c.rrect(26, 78, 94, 90, 6, "#4CC6B8", OUT, 2.6)
    c.circle(34, 94, 11, "#475160", OUT, 2.4)
    c.circle(92, 94, 11, "#475160", OUT, 2.4)
    return c


def draw_necklace():
    c = Canvas()
    for i in range(9):
        a = math.radians(20 + i * 17.5)
        cx = 64 + math.cos(a) * 34
        cy = 44 + math.sin(a) * 34
        c.circle(cx, cy, 7, "#FFF3E0" if i % 2 == 0 else "#88D6E8", OUT, 2.0)
    c.circle(64, 86, 14, "#F6A8C0", OUT, 2.6)
    return c


def draw_bracelet():
    c = Canvas()
    for i in range(10):
        a = (i / 10.0) * math.pi * 2.0
        cx = 64 + math.cos(a) * 30
        cy = 64 + math.sin(a) * 30
        col = ["#F26392", "#4CC6B8", "#FFE075", "#FAF2E4"][i % 4]
        c.circle(cx, cy, 9, col, OUT, 2.2)
    return c


def draw_painting():
    c = Canvas()
    c.rrect(22, 26, 106, 100, 8, "#C6905A", OUT, 3.0)
    c.rrect(30, 34, 98, 92, 4, "#92D6F0")
    c.rrect(30, 68, 98, 92, 2, "#4CC6B8")
    c.rrect(30, 80, 98, 92, 2, "#EBD09A")
    c.circle(82, 48, 10, "#FFE075")
    return c


def draw_dreamcatcher():
    c = Canvas()
    c.circle(64, 52, 28, "#F5ECD8", OUT, 3.2)
    c.circle(64, 52, 20, "#98DBD1")
    c.circle(64, 52, 7, "#F26392")
    for fx, col in [(46, "#F6A8C0"), (64, "#FFE075"), (82, "#88C2E8")]:
        c.seg(fx, 80, fx, 106, 4, OUT)
        c.ellipse(fx, 102, 7, 12, col, OUT, 2.0)
    return c


def draw_candle():
    c = Canvas()
    c.ellipse(64, 30, 8, 14, "#FFD859", OUT, 2.0)
    c.ellipse(64, 34, 4, 7, "#FFF8D8")
    c.rrect(40, 48, 88, 106, 10, "#F6A8C0", OUT, 2.8)
    c.rrect(46, 66, 82, 88, 4, "#FAF2E4")
    return c


def draw_gift():
    c = Canvas()
    c.rrect(26, 46, 102, 104, 10, "#F26392", OUT, 2.8)
    c.rrect(22, 38, 106, 56, 8, "#F686AA", OUT, 2.6)
    c.rrect(58, 38, 70, 104, 2, "#FFE075")
    c.ellipse(50, 30, 13, 9, "#FFE075", OUT, 2.2)
    c.ellipse(78, 30, 13, 9, "#FFE075", OUT, 2.2)
    return c


def generate_icons():
    icons = {
        # Comidas
        "icon_fries.png": draw_fries(False),
        "icon_fries_cheddar.png": draw_fries(True),
        "icon_pastel.png": draw_pastel(),
        "icon_acai.png": draw_acai(),
        "icon_fish.png": draw_grilled_fish(),
        "icon_burger.png": draw_burger(),
        # Bebidas
        "icon_water.png": draw_water(),
        "icon_coconut.png": draw_coconut(),
        "icon_guarana.png": draw_soda(),
        "icon_juice_lemon.png": draw_juice("#98E078", "#E6F06A"),
        "icon_juice_berry.png": draw_juice("#F27592", "#FF4C72"),
        "icon_lemonade.png": draw_juice("#D6F2C2", "#78C962"),
        # Sorvetes & Doces
        "icon_ice_lemon.png": draw_cone("#FFE678", leaf=True),
        "icon_ice_berry.png": draw_cone("#F684A4", leaf=True),
        "icon_ice_trio.png": draw_cone("#FFFFFF", triple=True),
        "icon_brigadeiro.png": draw_brigadeiro(),
        "icon_cocada.png": draw_cocada(),
        "icon_cupcake.png": draw_cupcake(),
        # Sushi & Mar
        "icon_sushi_combo.png": draw_sushi_combo(),
        "icon_hotroll.png": draw_hotroll(),
        "icon_temaki.png": draw_temaki(),
        "icon_ceviche.png": draw_ceviche(),
        "icon_sunomono.png": draw_sunomono(),
        "icon_sushi_boat.png": draw_sushi_boat(),
        # Moda & Esportes
        "icon_shirt_pink.png": draw_shirt("#F26392"),
        "icon_shirt_mint.png": draw_shirt("#4CC6B8"),
        "icon_hat.png": draw_hat(),
        "icon_bandana.png": draw_bandana(),
        "icon_skate.png": draw_skate(),
        "icon_scooter.png": draw_scooter(),
        # Artesanatos
        "icon_necklace.png": draw_necklace(),
        "icon_bracelet.png": draw_bracelet(),
        "icon_painting.png": draw_painting(),
        "icon_dreamcatcher.png": draw_dreamcatcher(),
        "icon_candle.png": draw_candle(),
        "icon_gift.png": draw_gift(),
    }
    for fname, canvas in icons.items():
        canvas.save(fname)
    print(f"[Icons] Generated {len(icons)} custom Cardapio icons in {ICON_DIR}")


if __name__ == "__main__":
    generate_textures()
    generate_icons()
    print("[Textures & 36 Icons Complete]")
