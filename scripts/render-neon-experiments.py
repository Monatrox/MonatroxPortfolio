#!/usr/bin/env python3
"""Create experimental derivatives without changing the original Neon artwork or photographs. Pillow only."""
import hashlib
from pathlib import Path

from PIL import Image, ImageOps

root = Path(__file__).resolve().parents[1] / 'Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Images'
baseline = root / 'neon-glow.webp'
before = hashlib.sha256(baseline.read_bytes()).hexdigest()
assert before.startswith('1b606b362edf'), 'Review the experiment inputs if the original Neon artwork changes.'

# Match the existing 72px CSS grid, including the two lines' overlapping alpha.
grid = Image.new('RGBA', (72, 72))
for coordinate in range(72):
    grid.putpixel((coordinate, 0), (168, 212, 56, 5))
    grid.putpixel((0, coordinate), (168, 212, 56, 5))
grid.putpixel((0, 0), (168, 212, 56, 10))
grid.save(root / 'neon-grid.png', optimize=True)
with Image.open(root / 'neon-grid.png') as saved:
    assert saved.size == (72, 72) and saved.getpixel((0, 0))[3] == 10
    assert saved.getpixel((1, 0))[3] == saved.getpixel((0, 1))[3] == 5
    assert saved.getpixel((1, 1))[3] == 0

for name, image in [
    ('neon-lowres.webp', Image.open(baseline).resize((800, 640), Image.Resampling.LANCZOS)),
    ('neon-mirror.webp', ImageOps.mirror(Image.open(baseline)).resize((1000, 800), Image.Resampling.LANCZOS)),
]:
    image.save(root / name, format='WEBP', lossless=True, method=6)
    assert Image.open(root / name).size == image.size

for source, name, size in [
    ('MX Abstract Logo Cleaned.png', 'neon-hero.webp', 512),
    ('monatrox-about.jpg', 'neon-portrait.webp', 960),
]:
    original = root / source
    checksum = hashlib.sha256(original.read_bytes()).digest()
    image = Image.open(original)
    image.thumbnail((size, size), Image.Resampling.LANCZOS)
    image.save(root / name, format='WEBP', quality=88, method=6)
    assert max(Image.open(root / name).size) <= size
    assert hashlib.sha256(original.read_bytes()).digest() == checksum

assert hashlib.sha256(baseline.read_bytes()).hexdigest() == before
for name in ['neon-grid.png', 'neon-lowres.webp', 'neon-mirror.webp', 'neon-hero.webp', 'neon-portrait.webp']:
    path = root / name
    print(name, Image.open(path).size, path.stat().st_size, hashlib.sha256(path.read_bytes()).hexdigest()[:12])
