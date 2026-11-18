"""Prepare pinned upstream binaries and declarative profiles; never execute upstream scripts."""
import hashlib
import json
from pathlib import Path
import re
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from experimental_profiles import generate

ROOT = Path(__file__).resolve().parents[1]
VERSION = "1.10.4"
ARCHIVE_SHA256 = "291af673bd0238506f9548e0e94ca0abf244000f4c9a06960544fb01c5f97b34"
URL = f"https://github.com/Flowseal/zapret-discord-youtube/releases/download/{VERSION}/zapret-discord-youtube-{VERSION}.zip"


def main():
    dist = ROOT / "dist"
    dist.mkdir(exist_ok=True)
    archive = dist / "upstream.zip"
    if not archive.exists():
        urllib.request.urlretrieve(URL, archive)
    if hashlib.sha256(archive.read_bytes()).hexdigest() != ARCHIVE_SHA256:
        raise ValueError("Archive differs from the pinned build input")
    engine = dist / "engine"
    profiles = ET.Element("profiles")
    with zipfile.ZipFile(archive) as z:
        for item in z.infolist():
            parts = Path(item.filename).parts[1:]
            if not parts or item.is_dir():
                continue
            rel = Path(*parts)
            if ".." in rel.parts or rel.is_absolute():
                raise ValueError("Unsafe archive path")
            data = z.read(item)
            if rel.parts[0] in ("bin", "lists") or rel.name.lower().startswith("license"):
                target = engine / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                if not target.exists() or target.read_bytes() != data:
                    target.write_bytes(data)
            if rel.name.startswith("general") and rel.suffix == ".bat":
                text = data.decode("utf-8-sig")
                # Extract only winws arguments, without executing any batch code.
                match = re.search(r'"%BIN%winws.exe"\s+(.*)', text, re.S)
                if not match:
                    raise ValueError(f"Missing engine command: {rel}")
                args = match[1].replace("^", " ")
                args = " ".join(args.split())
                for name in ("GameFilter", "GameFilterTCP", "GameFilterUDP"):
                    args = args.replace(f"%{name}%", "12")
                args = args.replace("%BIN%", "{BIN}").replace("%LISTS%", "{LISTS}")
                if "%" in args or not args.startswith("--wf-"):
                    raise ValueError(f"Unsupported profile: {rel}")
                ET.SubElement(profiles, "profile", name=rel.stem).text = args
    lists = engine / "lists"
    for name, content in {
        "list-general-user.txt": "# User domains\ndomain.example.abc\n",
        "list-exclude-user.txt": "domain.example.abc\n",
        "ipset-exclude-user.txt": "203.0.113.113/32\n",
    }.items():
        path = lists / name
        if not path.exists():
            path.write_text(content, encoding="utf-8")
    ET.ElementTree(profiles).write(dist / "profiles.xml", encoding="utf-8", xml_declaration=True)
    experimental = generate()
    ET.ElementTree(experimental).write(dist / "experimental-profiles.xml", encoding="utf-8", xml_declaration=True)
    (engine / "LICENSE.txt").write_bytes((ROOT / "THIRD_PARTY_LICENSE.txt").read_bytes())
    # Verify every referenced bundled asset before delivering the application.
    for profile in list(profiles) + list(experimental):
        for folder, filename in re.findall(r'\{(BIN|LISTS)\}([^"\s]+)', profile.text):
            if not (engine / folder.lower() / filename).is_file():
                raise ValueError(f"Missing profile asset: {filename}")
    manifest = {"version": VERSION, "url": URL,
                "archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
                "files": {str(p.relative_to(dist)): hashlib.sha256(p.read_bytes()).hexdigest()
                          for p in engine.rglob("*") if p.is_file()}}
    (dist / "upstream-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Prepared {len(profiles)} upstream + {len(experimental)} experimental profiles; assets checked. No engine execution.")


if __name__ == "__main__":
    main()
