"""Make a reproducible pristine GoRPG ZIP from an owned, translated clean-base profile.

The main entry HTML is replaced with its verified original backup, and no
English-loader files, saves, or launcher metadata are placed in the fixture.
"""
import os
import sys
import zipfile
from pathlib import Path


def main():
    source = Path(sys.argv[1]); output = Path(sys.argv[2])
    assert (source / "package.json").is_file()
    original = source / "www" / "celesphonia-english" / "original-entry.bin"
    assert original.is_file(), "Need the original entry backup, not a mutated translated HTML"
    package = __import__("json").loads((source / "package.json").read_text(encoding="utf-8-sig"))
    main_file = Path(package["main"]).as_posix().lstrip("./")
    output.parent.mkdir(parents=True, exist_ok=True)
    count = 0
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_STORED, allowZip64=True) as z:
        for folder, _, names in os.walk(source):
            for name in names:
                p = Path(folder) / name
                rel = p.relative_to(source).as_posix()
                if rel.startswith(("save/", "www/celesphonia-english/")) or rel.startswith(("launcher-", "ManualTransFile.json")):
                    continue
                if rel == main_file:
                    z.writestr("GoRPG-1.23/" + rel, original.read_bytes())
                else:
                    z.write(p, "GoRPG-1.23/" + rel)
                count += 1
    print(f"GO_RPG_ZIP_READY entries={count} size={output.stat().st_size} path={output}")


if __name__ == "__main__":
    main()
