"""Offline tests for our generated strategy matrix."""
from pathlib import Path
import re
import sys
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
from experimental_profiles import generate


class ProfilesTest(unittest.TestCase):
    def test_custom_matrix_is_distinct_and_scoped(self):
        rows = list(generate())
        self.assertEqual(len(rows), 3)
        self.assertEqual(len({row.text for row in rows}), 3)
        upstream = {row.text for row in ET.parse(ROOT / "dist/profiles.xml").getroot()}
        for row in rows:
            self.assertNotIn(row.text, upstream)
            self.assertEqual(row.attrib["origin"], "zapretus-experimental")
            self.assertTrue(row.attrib["name"].startswith("CUSTOM "))
            self.assertIn('--hostlist="{LISTS}list-general.txt"', row.text)
            self.assertIn('--hostlist="{LISTS}list-google.txt"', row.text)
            self.assertIn("--hostlist-exclude=", row.text)
            self.assertNotIn("1024-65535", row.text)
            for section in row.text.split(" --new "):
                if "--dpi-desync-any-protocol=1" in section:
                    self.assertRegex(section, r"--filter-(tcp|udp)=12(?:\s|$)")
                    self.assertIn('--ipset="{LISTS}ipset-all.txt"', section)
            for folder, filename in re.findall(r'\{(BIN|LISTS)\}([^"\s]+)', row.text):
                self.assertTrue((ROOT / "dist/engine" / folder.lower() / filename).is_file(), filename)


if __name__ == "__main__":
    unittest.main()
