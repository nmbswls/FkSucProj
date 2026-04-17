# -*- coding: utf-8 -*-
"""生成占位大地图 PNG（深色底 + 纯色轮廓线），输出到 Resources/WorldMap。需 Pillow： pip install pillow"""
from __future__ import annotations

import os
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:
    print("Please: pip install pillow")
    sys.exit(1)

# Editor -> WorldMap -> UI -> BigMap -> Scripts -> Assets
ROOT = os.path.normpath(
    os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", "..", "Resources", "WorldMap")
)
W = H = 512


def make_map(name: str, contour_rgb: tuple[int, int, int]) -> None:
    os.makedirs(ROOT, exist_ok=True)
    bg = (18, 20, 26)
    im = Image.new("RGB", (W, H), bg)
    dr = ImageDraw.Draw(im)
    margin = 28
    thick = 2
    dr.rectangle([0, 0, W - 1, H - 1], outline=contour_rgb, width=thick)
    dr.rectangle(
        [margin, margin, W - 1 - margin, H - 1 - margin],
        outline=contour_rgb,
        width=thick,
    )
    # 内部“走廊”折线，模拟简单分区
    dr.line([(margin * 2, H // 3), (W // 2, H // 3)], fill=contour_rgb, width=1)
    dr.line([(W // 2, H // 3), (W // 2, (2 * H) // 3)], fill=contour_rgb, width=1)
    dr.line([(W // 2, (2 * H) // 3), (W - margin * 2, (2 * H) // 3)], fill=contour_rgb, width=1)
    dr.line([(W // 3, margin * 2), (W // 3, H // 2)], fill=contour_rgb, width=1)
    dr.line([(W // 3, H // 2), ((2 * W) // 3, H // 2)], fill=contour_rgb, width=1)
    out = os.path.join(ROOT, f"{name}.png")
    im.save(out, "PNG")
    print("wrote", out)


def main() -> None:
    make_map("fake_map_base_01", (100, 255, 120))
    make_map("fake_map_village_01", (255, 160, 80))
    make_map("fake_map_game_init", (100, 200, 255))
    make_map("fake_map_fallback", (190, 190, 210))


if __name__ == "__main__":
    main()
