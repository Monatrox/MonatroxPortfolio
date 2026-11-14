#!/usr/bin/env python3
"""Authoring-only Pillow script for the checked-in 1200x630 social preview."""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps

root = Path(__file__).resolve().parents[1]
images = root / 'Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Images'
source = images / 'MX Abstract Logo Cleaned_Letters_DarkTheme.png'
checksum = hashlib.sha256(source.read_bytes()).hexdigest()
canvas = Image.new('RGBA', (1200, 630), '#181a18')
draw = ImageDraw.Draw(canvas)
draw.rectangle((72, 72, 78, 558), fill='#b0dc4a')
with Image.open(source) as image:
    logo = ImageOps.contain(image.convert('RGBA'), (190, 190), Image.Resampling.LANCZOS)
canvas.alpha_composite(logo, (1128 - logo.width, 86))
for text, xy, size, color, bold in [
    ('Zac Taylor', (106, 145), 72, '#eef1e9', True),
    ('Monatrox', (110, 245), 32, '#b0dc4a', False),
    ('Backend engineering.', (110, 378), 36, '#eef1e9', False),
    ('Technical leadership.', (110, 430), 36, '#eef1e9', False),
    ('monatrox.com', (110, 540), 24, '#bbc4b1', False),
]:
    font = ImageFont.truetype('DejaVuSans-Bold.ttf' if bold else 'DejaVuSans.ttf', size)
    assert draw.textbbox(xy, text, font=font)[2] < 920
    draw.text(xy, text, font=font, fill=color)
target = images / 'portfolio-share.png'
canvas.convert('RGB').save(target, optimize=True)
with Image.open(target) as saved:
    assert saved.size == (1200, 630) and saved.format == 'PNG'
assert hashlib.sha256(source.read_bytes()).hexdigest() == checksum
print(f'{target.relative_to(root)}: 1200x630, {target.stat().st_size} bytes')
