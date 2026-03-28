from PIL import Image, ImageDraw
import json
from pathlib import Path

# Target path
iconset = Path(__file__).resolve().parent.parent / "ios/App/App/Assets.xcassets/AppIcon.appiconset"
iconset.mkdir(parents=True, exist_ok=True)

# Clean old icons to avoid unassigned files in Xcode
for png in iconset.glob("*.png"):
    png.unlink()

# Base design
base_size = 1024
bg = Image.new("RGBA", (base_size, base_size), (12, 32, 68, 255))  # deep navy

draw = ImageDraw.Draw(bg)

# Add subtle inner circle
circle_radius = int(base_size * 0.36)
center = base_size // 2
bbox = [center - circle_radius, center - circle_radius, center + circle_radius, center + circle_radius]
draw.ellipse(bbox, fill=(28, 183, 142, 255))  # teal

# Draw white cross
cross_w = int(base_size * 0.12)
cross_h = int(base_size * 0.42)
# vertical
vx0 = center - cross_w // 2
vy0 = center - cross_h // 2
vx1 = center + cross_w // 2
vy1 = center + cross_h // 2
draw.rounded_rectangle([vx0, vy0, vx1, vy1], radius=cross_w // 2, fill=(255, 255, 255, 255))
# horizontal
hx0 = center - cross_h // 2
hy0 = center - cross_w // 2
hx1 = center + cross_h // 2
hy1 = center + cross_w // 2
draw.rounded_rectangle([hx0, hy0, hx1, hy1], radius=cross_w // 2, fill=(255, 255, 255, 255))

# Draw small heart bottom-right
heart_size = int(base_size * 0.18)
heart_center = (int(base_size * 0.66), int(base_size * 0.66))
# heart using two circles and triangle
r = heart_size // 3
x, y = heart_center
left = (x - r, y - r)
right = (x + r, y - r)

draw.ellipse([left[0] - r, left[1] - r, left[0] + r, left[1] + r], fill=(255, 255, 255, 255))
draw.ellipse([right[0] - r, right[1] - r, right[0] + r, right[1] + r], fill=(255, 255, 255, 255))

tip = (x, y + r)
poly = [(left[0] - r, left[1]), (right[0] + r, right[1]), tip]
draw.polygon(poly, fill=(255, 255, 255, 255))

# Save base
(base_path := iconset / "AppIcon-1024.png")
bg.save(base_path)

# iOS sizes
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
            img = bg.resize((px, px), Image.LANCZOS)
            img.save(iconset / filename)
        images.append(
            {
                "idiom": idiom,
                "size": f"{size}x{size}",
                "scale": f"{scale}x",
                "filename": filename,
            }
        )

contents = {"images": images, "info": {"version": 1, "author": "xcode"}}
(iconset / "Contents.json").write_text(json.dumps(contents, indent=2))

print(f"Generated {len(images)} icons in {iconset}")
