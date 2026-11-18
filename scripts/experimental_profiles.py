"""Three candidates based on upstream ALT; verify effectiveness on the local connection."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET


def generate():
    source = Path(__file__).resolve().parents[1] / "dist" / "profiles.xml"
    base = next(p.text for p in ET.parse(source).getroot()
                if p.attrib["name"] == "general (ALT)")
    profiles = ET.Element("profiles")
    for name, method, position, repeats in (
        ("CUSTOM 1 - FakeSplit TS", "fake,fakedsplit", "1", 4),
        ("CUSTOM 2 - MultiSplit TS", "fake,multisplit", "1,midsld", 8),
        ("CUSTOM 3 - MultiDisorder TS", "fake,multidisorder", "1,midsld", 10),
    ):
        args = base.replace("--dpi-desync=fake,fakedsplit",
                            f"--dpi-desync={method} --dpi-desync-split-pos={position}")
        if method != "fake,fakedsplit":
            args = args.replace(" --dpi-desync-fakedsplit-pattern=0x00", "")
        args = re.sub(r"--dpi-desync-repeats=\d+", f"--dpi-desync-repeats={repeats}", args)
        ET.SubElement(profiles, "profile", name=name, origin="zapretus-experimental").text = args
    return profiles
