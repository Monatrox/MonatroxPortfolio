#!/usr/bin/env python3
"""Authoring-only: bake a smooth, dithered glow. Requires installed NumPy/Pillow, not runtime packages."""

import hashlib
from pathlib import Path

import numpy as np
from PIL import Image

WIDTH, HEIGHT = 1600, 1280
BASE = np.array([24, 26, 24], dtype=np.float32)


def linear(rgb):
    value = np.asarray(rgb, dtype=np.float32) / 255
    return np.where(value <= 0.04045, value / 12.92, ((value + 0.055) / 1.055) ** 2.4)


y, x = np.mgrid[0:HEIGHT, 0:WIDTH].astype(np.float32)
u, v = (x + 0.5) / WIDTH, (y + 0.5) / HEIGHT
light = np.zeros((HEIGHT, WIDTH, 3), dtype=np.float32)


def emit(rgb, strength):
    light[:] += strength[:, :, None] * linear(rgb)


# Gaussian light falloff is baked in linear light, not blurred or recomputed by the browser.
emit([168, 212, 56], 0.045 * np.exp(-0.5 * (((u - 0.20) / 0.24) ** 2 + ((v - 0.25) / 0.28) ** 2)))
emit([138, 199, 240], 0.018 * np.exp(-0.5 * (((u - 0.82) / 0.22) ** 2 + ((v - 0.18) / 0.25) ** 2)))
for distance, colour, halo, core in [
    ((v - (0.86 - 0.67 * u)) / np.sqrt(1 + 0.67**2), [168, 212, 56], 0.018, 0.20),
    ((v - (0.62 * u - 0.23)) / np.sqrt(1 + 0.62**2), [138, 199, 240], 0.009, 0.07),
]:
    emit(colour, halo * np.exp(-0.5 * (distance / 0.025) ** 2))
    emit(colour, core * np.exp(-0.5 * (distance / 0.0010) ** 2))
    emit([238, 241, 233], core * 0.08 * np.exp(-0.5 * (distance / 0.00045) ** 2))

# End on the exact page colour so the finite, page-anchored image has no bottom seam.
t = np.clip((v - 0.70) / 0.28, 0, 1)
fade = 1 - t * t * (3 - 2 * t)
scene = np.clip(linear(BASE) + light * fade[:, :, None], 0, 1)
srgb = np.where(scene <= 0.0031308, scene * 12.92, 1.055 * scene ** (1 / 2.4) - 0.055)

# Shared-channel, sub-code-value noise breaks quantisation contours without coloured speckles.
noise = (np.random.default_rng(2026).random((HEIGHT, WIDTH, 1), dtype=np.float32) - 0.5) * fade[:, :, None]
pixels = np.clip(np.floor(srgb * 255 + 0.5 + noise), 0, 255).astype(np.uint8)
assert pixels.shape == (HEIGHT, WIDTH, 3)
assert np.isfinite(srgb).all()
assert np.all(pixels[-16:] == BASE.astype(np.uint8))
assert pixels[:, :, 1].max() > BASE[1] + 40
assert pixels[:, :, 1].max() > pixels[:, :, 2].max()
assert np.any(pixels != np.floor(srgb * 255 + 0.5).astype(np.uint8))
assert abs((pixels.astype(np.float32) - srgb * 255).mean()) < 0.05

output = Path(__file__).resolve().parents[1] / 'Hosts/MonatroxPortfolio.Blazor/wwwroot/Content/Images/neon-glow.webp'
Image.fromarray(pixels).save(output, format='WEBP', lossless=True, quality=100, method=6)
assert np.array_equal(np.asarray(Image.open(output)), pixels), 'The checked-in texture must preserve its dither losslessly.'
version = hashlib.sha256(output.read_bytes()).hexdigest()[:12]
print(f'{output}: {output.stat().st_size:,} bytes, {WIDTH}x{HEIGHT}')
print(f'CSS URL: Content/Images/neon-glow.webp?v={version}')
