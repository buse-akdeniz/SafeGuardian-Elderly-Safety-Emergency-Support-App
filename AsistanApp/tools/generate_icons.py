"""Generate production-quality SafeGuardian app icons for iOS and Android."""

from __future__ import annotations

import json
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
IOS_ICONSET = ROOT / "ios/App/App/Assets.xcassets/AppIcon.appiconset"
ANDROID_RES = ROOT / "android/app/src/main/res"
WEB_LOGO_DIR = ROOT / "wwwroot/elderly-ui/assets"

BASE = 1024


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def lerp_color(c1: tuple[int, int, int], c2: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return (
        int(lerp(c1[0], c2[0], t)),
        int(lerp(c1[1], c2[1], t)),
        int(lerp(c1[2], c2[2], t)),
    )


def vertical_gradient(size: int, top: tuple[int, int, int], bottom: tuple[int, int, int]) -> Image.Image:
    img = Image.new("RGBA", (size, size))
    px = img.load()
    for y in range(size):
        t = y / max(size - 1, 1)
        color = lerp_color(top, bottom, t) + (255,)
        for x in range(size):
            px[x, y] = color
    return img


def radial_glow(size: int, center: tuple[int, int], radius: int, color: tuple[int, int, int, int]) -> Image.Image:
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(glow)
    steps = 28
    for i in range(steps, 0, -1):
        r = int(radius * (i / steps))
        alpha = int(color[3] * (i / steps) ** 1.6)
        draw.ellipse(
            [center[0] - r, center[1] - r, center[0] + r, center[1] + r],
            fill=(color[0], color[1], color[2], alpha),
        )
    return glow.filter(ImageFilter.GaussianBlur(radius=8))


def shield_points(cx: int, cy: int, w: int, h: int) -> list[tuple[int, int]]:
    top_y = cy - h // 2
    bottom_y = cy + h // 2
    left_x = cx - w // 2
    right_x = cx + w // 2
    shoulder_y = top_y + int(h * 0.18)
    return [
        (left_x, shoulder_y),
        (left_x, cy + int(h * 0.05)),
        (cx, bottom_y),
        (right_x, cy + int(h * 0.05)),
        (right_x, shoulder_y),
        (cx, top_y),
    ]


def draw_shield_gradient(draw: ImageDraw.ImageDraw, points: list[tuple[int, int]], top: tuple[int, int, int], bottom: tuple[int, int, int]) -> None:
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    min_y, max_y = min(ys), max(ys)
    mask = Image.new("L", (BASE, BASE), 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    layer = Image.new("RGBA", (BASE, BASE), (0, 0, 0, 0))
    px = layer.load()
    mpx = mask.load()
    for y in range(min_y, max_y + 1):
        t = (y - min_y) / max(max_y - min_y, 1)
        color = lerp_color(top, bottom, t)
        for x in range(min(xs), max(xs) + 1):
            if mpx[x, y]:
                px[x, y] = color + (255,)
    return layer, mask


def rounded_cross(draw: ImageDraw.ImageDraw, cx: int, cy: int, arm: int, thickness: int, fill: tuple[int, int, int, int]) -> None:
    radius = thickness // 2
    draw.rounded_rectangle(
        [cx - thickness // 2, cy - arm, cx + thickness // 2, cy + arm],
        radius=radius,
        fill=fill,
    )
    draw.rounded_rectangle(
        [cx - arm, cy - thickness // 2, cx + arm, cy + thickness // 2],
        radius=radius,
        fill=fill,
    )


def draw_heart(draw: ImageDraw.ImageDraw, cx: int, cy: int, size: int, fill: tuple[int, int, int, int]) -> None:
    r = size // 3
    draw.ellipse([cx - size // 2 - r // 2, cy - r, cx - size // 2 + r, cy + r], fill=fill)
    draw.ellipse([cx + size // 2 - r, cy - r, cx + size // 2 + r // 2, cy + r], fill=fill)
    draw.polygon(
        [
            (cx - size // 2 - r // 2, cy),
            (cx + size // 2 + r // 2, cy),
            (cx, cy + size // 2 + r),
        ],
        fill=fill,
    )


def render_icon(size: int) -> Image.Image:
    scale = size / BASE
    canvas = vertical_gradient(size, (8, 22, 48), (14, 36, 72))

    glow = radial_glow(size, (size // 2, int(size * 0.52)), int(size * 0.42), (37, 158, 255, 70))
    canvas = Image.alpha_composite(canvas, glow)

    overlay = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)

    cx, cy = size // 2, int(size * 0.52)
    shield_w = int(size * 0.52)
    shield_h = int(size * 0.58)
    points = shield_points(cx, cy, shield_w, shield_h)

    # Shield body with vertical gradient via temporary full-size layer
    full = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).polygon(points, fill=255)
    shield_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    spx = shield_layer.load()
    mpx = mask.load()
    ys = [p[1] for p in points]
    min_y, max_y = min(ys), max(ys)
    for y in range(min_y, max_y + 1):
        t = (y - min_y) / max(max_y - min_y, 1)
        color = lerp_color((20, 184, 166), (37, 99, 235), t) + (255,)
        for x in range(size):
            if mpx[x, y]:
                spx[x, y] = color
    overlay = Image.alpha_composite(overlay, shield_layer)

    draw = ImageDraw.Draw(overlay)
    inset = int(4 * scale)
    inner = shield_points(cx, cy, shield_w - inset * 2, shield_h - inset * 2)
    draw.polygon(inner, fill=(255, 255, 255, 28))

    cross_arm = int(size * 0.14)
    cross_th = int(size * 0.085)
    rounded_cross(draw, cx, cy - int(size * 0.02), cross_arm, cross_th, (255, 255, 255, 255))

    heart_size = int(size * 0.14)
    draw_heart(draw, cx + int(size * 0.11), cy + int(size * 0.13), heart_size, (255, 255, 255, 235))

    ring_r = int(size * 0.34)
    draw.ellipse(
        [cx - ring_r, cy - ring_r, cx + ring_r, cy + ring_r],
        outline=(255, 255, 255, 36),
        width=max(1, int(2 * scale)),
    )

    canvas = Image.alpha_composite(canvas, overlay)
    return canvas.convert("RGBA")


def save_png(path: Path, image: Image.Image) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=True)


def generate_ios_icons() -> None:
    IOS_ICONSET.mkdir(parents=True, exist_ok=True)
    for png in IOS_ICONSET.glob("*.png"):
        png.unlink()

    master = render_icon(BASE)
    save_png(IOS_ICONSET / "AppIcon-1024.png", master)

    sizes = [
        (20, [2, 3], "iphone"),
        (29, [2, 3], "iphone"),
        (40, [2, 3], "iphone"),
        (60, [2, 3], "iphone"),
        (20, [1, 2], "ipad"),
        (29, [1, 2], "ipad"),
        (40, [1, 2], "ipad"),
        (76, [1, 2], "ipad"),
        (83.5, [2], "ipad"),
        (1024, [1], "ios-marketing"),
    ]

    images = []
    for size, scales, idiom in sizes:
        for scale in scales:
            px = int(round(size * scale))
            if size == 1024 and scale == 1:
                filename = "AppIcon-1024.png"
            else:
                filename = f"AppIcon-{size}x{size}@{scale}x.png".replace(".0", "")
                save_png(IOS_ICONSET / filename, master.resize((px, px), Image.Resampling.LANCZOS))
            images.append(
                {
                    "idiom": idiom,
                    "size": f"{size}x{size}",
                    "scale": f"{scale}x",
                    "filename": filename,
                }
            )

    (IOS_ICONSET / "Contents.json").write_text(json.dumps({"images": images, "info": {"version": 1, "author": "xcode"}}, indent=2))
    print(f"iOS: {len(images)} icons -> {IOS_ICONSET}")


def generate_android_icons() -> None:
    densities = {
        "mipmap-mdpi": 48,
        "mipmap-hdpi": 72,
        "mipmap-xhdpi": 96,
        "mipmap-xxhdpi": 144,
        "mipmap-xxxhdpi": 192,
    }
    master = render_icon(BASE)
    for folder, px in densities.items():
        target = ANDROID_RES / folder
        target.mkdir(parents=True, exist_ok=True)
        icon = master.resize((px, px), Image.Resampling.LANCZOS)
        save_png(target / "ic_launcher.png", icon)
        save_png(target / "ic_launcher_round.png", icon)
        save_png(target / "ic_launcher_foreground.png", icon)
    print(f"Android: launcher icons in {ANDROID_RES}")


def generate_web_assets() -> None:
    WEB_LOGO_DIR.mkdir(parents=True, exist_ok=True)
    master = render_icon(BASE)
    for name, size in [("logo-512.png", 512), ("logo-192.png", 192), ("logo-96.png", 96), ("favicon-32.png", 32)]:
        save_png(WEB_LOGO_DIR / name, master.resize((size, size), Image.Resampling.LANCZOS))
    svg = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" role="img" aria-label="SafeGuardian">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#081630"/>
      <stop offset="100%" stop-color="#0e2448"/>
    </linearGradient>
    <linearGradient id="shield" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="#14b8a6"/>
      <stop offset="100%" stop-color="#2563eb"/>
    </linearGradient>
  </defs>
  <rect width="64" height="64" rx="14" fill="url(#bg)"/>
  <path d="M32 12 L18 18 V30 C18 40 24 47 32 52 C40 47 46 40 46 30 V18 Z" fill="url(#shield)"/>
  <rect x="29" y="24" width="6" height="18" rx="3" fill="#fff"/>
  <rect x="23" y="30" width="18" height="6" rx="3" fill="#fff"/>
  <path d="M38 36 C38 34 39 33 40 33 C41 33 42 34 42 36 C42 38 40 40 40 40 C40 40 38 38 38 36 Z" fill="#fff" opacity="0.9"/>
</svg>
"""
    (WEB_LOGO_DIR / "logo.svg").write_text(svg, encoding="utf-8")
    print(f"Web: assets -> {WEB_LOGO_DIR}")


if __name__ == "__main__":
    generate_ios_icons()
    generate_android_icons()
    generate_web_assets()
    print("Done.")
