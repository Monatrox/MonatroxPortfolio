#!/usr/bin/env python3
"""Authoring-only Pillow script. Rebuild the two checked-in cover atlases from the curated catalogue."""
import hashlib
import json
import math
from pathlib import Path
import re

from PIL import Image, ImageOps

root = Path(__file__).resolve().parents[1]
images = root / 'Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Images/Influences'
catalogue = (root / 'Hosts/MonatroxPortfolio.Blazor.Client/Components/Profile/Influences.razor').read_text()
columns, width, height, padding = 4, 320, 480, 4
manifest = {}
for collection in ('Books', 'Games'):
    # The catalogue deliberately uses literal (title, cover-path) tuples, not executable configuration.
    block = re.search(rf'\b{collection}\s*=\s*\[(.*?)\];', catalogue, re.S)
    assert block, f'Missing literal {collection} catalogue.'
    covers = re.findall(r'\("(?:[^"\\]|\\.)*",\s*"([^"]+)"\)', block[1])
    assert covers and len(covers) == block[1].count('),') + 1
    rows = math.ceil(len(covers) / columns)
    atlas = Image.new('RGBA', (columns * width, rows * height))
    inputs = []
    for index, cover in enumerate(covers):
        source = (images / f'{cover}.webp').resolve()
        assert source.is_relative_to(images.resolve()) and source.is_file()
        checksum = hashlib.sha256(source.read_bytes()).hexdigest()
        with Image.open(source) as original:
            image = ImageOps.contain(original.convert('RGBA'), (width - 2 * padding, height - 2 * padding), Image.Resampling.LANCZOS)
        x, y = index % columns * width, index // columns * height
        atlas.alpha_composite(image, (x + (width - image.width) // 2, y + (height - image.height) // 2))
        assert hashlib.sha256(source.read_bytes()).hexdigest() == checksum
        inputs.append({'source': cover, 'sha256': checksum, 'x': x, 'y': y})
    target = images / f'{collection.lower()}-atlas.webp'
    atlas.save(target, format='WEBP', quality=90, method=6)
    with Image.open(target) as saved:
        assert saved.size == atlas.size
        decoded = saved.convert('RGBA')
        assert decoded.getpixel((0, 0))[3] == 0
        assert all(decoded.getpixel((entry['x'], entry['y']))[3] == 0 for entry in inputs)
    version = hashlib.sha256(target.read_bytes()).hexdigest()[:12]
    manifest[collection] = {'columns': columns, 'rows': rows, 'cell': [width, height], 'version': version, 'inputs': inputs}
    print(f'{collection}: {len(covers)} covers, {atlas.size}, {target.stat().st_size} bytes; {target.name}?v={version}')
output = root / 'artifacts/influences-lab/atlas-manifest.json'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(manifest, indent=2))
for collection, layout in manifest.items():
    expected = f"{collection.lower()}-atlas.webp?v={layout['version']}"
    assert expected in catalogue, f'Update the {collection} component URL to {expected}, then rerun.'
