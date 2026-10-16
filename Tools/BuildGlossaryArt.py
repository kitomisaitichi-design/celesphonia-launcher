"""Pixel-exact English help/glossary picture replacements.

Creator game graphics are READ ONLY. Localized PNGs retain original dimensions,
fit their actual in-game draw scale, and are supplied as a reversible overlay.
"""
from PIL import Image, ImageDraw, ImageFont
import argparse
import hashlib
import json
from pathlib import Path

HOME = Path(__file__).resolve().parents[1]
NAMES = ("Glossary_1.png", "Glossary_2.png", "Glossary_4.png",
         "hair_win_UI.png", "HypnosisWords.png")
SCALE = 2
FONT = Path("C:/Windows/Fonts/seguisb.ttf")
FONT_REGULAR = Path("C:/Windows/Fonts/segoeui.ttf")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def font(size, regular=False):
    assert (FONT_REGULAR if regular else FONT).is_file()
    return ImageFont.truetype(str(FONT_REGULAR if regular else FONT), size * SCALE)


def size_of(draw, text, f):
    bb = draw.textbbox((0, 0), text, font=f)
    return bb[2] - bb[0], bb[3] - bb[1]


class Art:
    def __init__(self, path):
        original = Image.open(path)
        assert original.format == "PNG"
        self.original_size = original.size
        self.img = original.convert("RGBA").resize(
            tuple(v * SCALE for v in original.size), Image.Resampling.LANCZOS)
        self.draw = ImageDraw.Draw(self.img, "RGBA")
        self.regions = []

    def panel(self, rect, color=(2, 12, 43, 236), radius=8,
              outline=None, width=2):
        box = tuple(int(v * SCALE) for v in rect)
        self.draw.rounded_rectangle(
            box, radius * SCALE, fill=color,
            outline=outline, width=width * SCALE)
        self.regions.append(rect)

    def label(self, xy, value, points=26, color="#f4f8ff", width=None,
              lines=1, regular=False, spacing=5, center=False):
        f = font(points, regular=regular)
        x, y = (v * SCALE for v in xy)
        chunks = []
        if width is not None:
            maxw = width * SCALE
            for hard in value.split("\n"):
                line = ""
                for word in hard.split(" "):
                    candidate = (line + " " + word).strip()
                    if size_of(self.draw, candidate, f)[0] <= maxw:
                        line = candidate
                    else:
                        if line:
                            chunks.append(line)
                        line = word
                        if size_of(self.draw, word, f)[0] > maxw:
                            raise ValueError("Word too wide: " + word)
                if line:
                    chunks.append(line)
        else:
            chunks = value.split("\n")
        if len(chunks) > lines:
            raise ValueError("Too many text lines at " + str(xy) + ": " + repr(chunks))
        line_h = (points + spacing) * SCALE
        for i, chunk in enumerate(chunks):
            measured = size_of(self.draw, chunk, f)[0]
            if width is not None and measured > width * SCALE:
                raise ValueError("Text exceeds panel: " + repr(chunk))
            bx = x + ((width * SCALE - measured) // 2 if center and width else 0)
            self.draw.text((bx, y + i * line_h), chunk, font=f,
                           fill=color, stroke_width=0)
        return len(chunks) * (points + spacing)

    def save(self, target):
        result = self.img.resize(self.original_size, Image.Resampling.LANCZOS).convert("RGB")
        result.save(target, "PNG", optimize=True)
        assert Image.open(target).size == self.original_size
        return {"size": list(self.original_size), "sha256": digest(target),
                "editedRegions": self.regions}


def glossary1(src, dst):
    art = Art(src)
    # The label above the sprites is the only embedded Chinese in this image.
    art.panel((165, 100, 229, 133), (255, 255, 255, 255), radius=0, width=0)
    art.label((171, 104), "Touch", 16, color="#d90000",
              width=53, lines=1, center=True)
    return art.save(dst)


def glossary2(src, dst):
    art = Art(src)
    # Native caller <SGピクチャ拡大率:0.6>; all labels are sized for that
    # reduced viewport, not for a full-width zoomed image.
    art.panel((29, 9, 793, 158), (3, 8, 38, 244),
              radius=6, outline=(86, 131, 246, 220), width=1)
    art.label((48, 19), "BATTLE LOG  /  GRAB ATTACK", 26, "#a5d6ff", width=680)
    art.label((48, 51), "Azure Star is pinned and struggling to breathe.", 26, width=720)
    art.label((48, 84), "Azure Star: Mmph... ngh...!", 26, "#d4e8ff", width=720)
    art.label((48, 116), "PLEASURE +1  /  Finisher damage increases at low enemy HP.",
              24, "#ffaed9", width=720)
    # The source image has one additional Chinese speech tag between the
    # battle log and status frame; remove it without masking the HP numbers.
    art.panel((53, 159, 170, 185), (5, 14, 63, 246), radius=0, width=0)

    # Keep the original status gauges, values, icons and decorative frame.
    rows = [(195, "Azure Star"), (365, "Sensitivity"),
            (408, "Arousal"), (455, "Outfit HP"),
            (504, "Miasma")]
    for y, label in rows:
        art.panel((25, y - 1, 208 if y == 195 else 211, y + 29),
                  (9, 43, 117, 250), radius=0, width=0)
        art.label((28, y), label, 23 if y != 195 else 26,
                  width=182, lines=1)
    # The native CJK status label has a tall glow and otherwise peeks above
    # our translated Miasma caption; cover only that label, not its gauge.
    art.panel((25, 490, 211, 502), (9, 43, 117, 250),
              radius=0, width=0)

    # The original scene and figures are retained behind these compact
    # translated instructions. Avoid painting over the character's face.
    art.panel((517, 346, 892, 455), (5, 17, 40, 239),
              radius=8, outline=(146, 195, 255, 210), width=1)
    art.label((539, 356), "GAGGED: Magic is blocked", 25,
              "#ffedaf", width=333)
    art.label((539, 390), "Hold Z: spend MP", 26, width=330)
    art.label((539, 420), "to break free.", 26, width=325)
    art.panel((374, 533, 814, 629), (4, 17, 38, 245),
              radius=7, outline=(137, 197, 255, 170), width=1)
    art.label((391, 544), "Escape difficulty depends on", 25, width=408)
    art.label((391, 573), "enemy ATK and penetration count.", 25, width=408)

    # Preserve the original button panel's location and overall blue/yellow
    # hierarchy so that the screenshot remains useful as an in-game guide.
    art.panel((62, 644, 675, 797), (13, 39, 112, 250), radius=4,
              outline=(154, 191, 251, 245), width=2)
    art.label((77, 654), "CAUGHT BY THE ENEMY!", 29,
              "#ffe578", width=560)
    art.label((77, 694), "Hold Z: cast magic (blocked if gagged).",
              24, width=574)
    art.label((77, 729), "Hold X: resist    |    Q: other commands",
              24, width=574)
    art.panel((247, 763, 537, 787), (70, 58, 40, 255), radius=8,
              outline=(218, 178, 111, 255), width=3)
    art.panel((263, 769, 516, 779), (11, 21, 40, 255), radius=5)
    art.panel((190, 811, 904, 910), (9, 25, 69, 252),
              radius=8, outline=(113, 177, 255, 205), width=1)
    art.label((211, 820), "Hold X to spend SP and struggle free.",
              25, width=672)
    art.label((211, 850), "Green bar: 2x effect. No escape during",
              24, width=672)
    art.label((211, 880), "an H attack or climax.", 24, width=672)
    return art.save(dst)


def glossary4(src, dst):
    art = Art(src)
    # Fill the full source montage before rendering four replacement samples.
    # Otherwise 1px antialiased bands of the original Chinese remain at the
    # joins between screenshot rows.
    art.draw.rectangle((0, 0, 481 * SCALE, 400 * SCALE),
                       fill=(8, 27, 73, 255))
    rows = [
        ("01", "School Swimsuit (Navy)", "Outfit  |  Change at a wardrobe"),
        ("02", "Standard Swimsuit", "Outfit  |  Cannot remove freely"),
        ("03", "Enchanting Nipple Rings",
         "Engraved spatial runes\nMDF -5  |  Always worn"),
        ("04", "Alluring Collar (Locked)",
         "Owner: Defector Elites x2\nWeakens stats  |  Key required"),
    ]
    for i, (number, title, description) in enumerate(rows):
        y = i * 100
        art.panel((0, y, 480, y + 100), (10, 37, 101, 255),
                  radius=0, outline=(69, 138, 248, 242), width=2)
        art.panel((9, y + 17, 65, y + 76), (24, 57, 129, 255),
                  radius=7, outline=(255, 224, 98, 255), width=2)
        art.label((17, y + 31), number, 24, "#ffe768",
                  width=44, center=True)
        art.label((78, y + 15), title, 24, "#f9f5e8",
                  width=387)
        art.label((78, y + (49 if "\n" in description else 58)),
                  description, 15 if "\n" in description else 17,
                  "#b9e5ff", width=390,
                  lines=2 if "\n" in description else 1, regular=True)
    return art.save(dst)


def hair_color_help(src, dst):
    original=Image.open(src)
    assert original.size==(1280,720) and original.mode=="RGBA"
    out=original.copy()
    painter=ImageDraw.Draw(out,"RGBA")
    # Change Color modal: the native bottom footer is CJK baked into the
    # texture. Replace the instruction only, leaving the UI panel and alpha
    # channel completely intact. Fits within the 442px footer at native scale.
    painter.rounded_rectangle((128,402,580,441),radius=4,
                              fill=(249,235,215,255))
    message="Arrows: Adjust  |  Shift: Blend  |  Back: Reset  |  OK: Save"
    selected=ImageFont.truetype(str(FONT_REGULAR),15)
    while painter.textbbox((0,0),message,font=selected)[2]>430:
        selected=ImageFont.truetype(str(FONT_REGULAR),selected.size-1)
        assert selected.size>=11
    painter.text((134,414),message,font=selected,fill=(70,35,44,255))
    out.save(dst,"PNG",optimize=True)
    assert Image.open(dst).mode=="RGBA"
    return {"size":list(original.size),"sha256":digest(dst),
            "editedRegions":[(128,402,580,441)]}


def hypnosis(src, dst):
    """Keep the native 960×384 MV animation atlas, including empty cells."""
    original = Image.open(src).convert("RGBA")
    assert original.size == (960, 384), "Hypnosis atlas frame geometry changed"
    # The source sheet contains only lettering in cells 105–107. Keep native
    # frame centers, empty cells and transparency; recolor the English text
    # rather than replacing the battle animation or its animation IDs.
    atlas = Image.new("RGBA", original.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(atlas, "RGBA")
    entries = [
        (0, "SUBMIT", "#ff007b", "#7d003e"),
        (192, "SUBMIT", "#f7a2f2", "#8f0569"),
        (384, "MASTURBATE", "#ff61e4", "#9d158f"),
    ]
    for x, word, color, stroke in entries:
        size = 34 if len(word) < 8 else 24
        f = ImageFont.truetype(str(FONT), size)
        while draw.textbbox((0, 0), word, font=f)[2] > 180:
            size -= 1
            assert size >= 17, "Atlas English lettering cannot fit"
            f = ImageFont.truetype(str(FONT), size)
        bb = draw.textbbox((0, 0), word, font=f, stroke_width=2)
        text_w = bb[2] - bb[0]
        x0 = x + (192 - text_w) // 2
        draw.text((x0, 261), word, font=f, fill=color,
                  stroke_width=2, stroke_fill=stroke)
    atlas.save(dst, "PNG", optimize=True)
    assert Image.open(dst).size == original.size
    return {"size": list(original.size), "sha256": digest(dst),
            "editedRegions": [(0, 258, 576, 306)]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--creator-pictures", type=Path, required=True)
    args = parser.parse_args()
    source = args.creator_pictures.resolve()
    assert source.name == "pictures"
    target = HOME / "English" / "ImageLocalization"
    target.mkdir(parents=True, exist_ok=True)
    result = {"format": 1, "versionIds": [
        "three-in-one-3-3-5-v1-051",
        "three-in-one-3-3-5-v1-052"],
        "images": {}}
    builders = {"Glossary_1.png": glossary1,
                "Glossary_2.png": glossary2,
                "Glossary_4.png": glossary4,
                "hair_win_UI.png": hair_color_help,
                "HypnosisWords.png": hypnosis}
    for name in NAMES:
        is_animation = name == "HypnosisWords.png"
        path = source.parent / "animations" / "催眠用语.png" if is_animation else source / name
        assert path.is_file()
        dest = target / name
        meta = builders[name](path, dest)
        result["images"][name] = {
            "originalSHA256": digest(path),
            "localizedSHA256": meta["sha256"],
            "width": meta["size"][0], "height": meta["size"][1],
            "kind": "animation" if is_animation else "picture",
            "nativePath": "img/animations/催眠用语.png" if is_animation else "img/pictures/" + name,
            "nativeName": "催眠用语" if is_animation else name[:-4],
            "localizedName": name[:-4],
            "renderOnly": True, "nativePictureUnchanged": True,
        }
    (target / "manifest.json").write_text(
        json.dumps(result, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
