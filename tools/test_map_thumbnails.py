"""Offline checks for map image packaging: python3 -m unittest discover -s tools -p 'test_*.py'."""
import base64
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import build_map_thumbnails as thumbnails


class ThumbnailChecks(unittest.TestCase):
    def test_jsonc_catalog_matches_map_ids(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'config.jsonc'
            path.write_text('''{
                // These values deliberately include comment-like strings.
                "sws2-cs2mapmanager": {"Maps":{"Available":{
                    "local": "local:DE_DUST2",
                    "workshop": {"WorkshopId":"00123", "CKZ":{"NUB":2,"PRO":3}, "VNL":{"NUB":7,"PRO":8}},
                    "name // /* comma,}": "456", /* comment */
                }}}
            }''')
            maps = thumbnails.catalog(thumbnails.read_jsonc(path))
        self.assertEqual(list(maps), ['map:de_dust2', 'workshop:123', 'workshop:456'])
        self.assertEqual(maps['workshop:456'][0], 'name // /* comma,}')
        self.assertEqual(thumbnails.thumbnail_class('WORKSHOP:123'), thumbnails.thumbnail_class('workshop:123'))

    def test_offline_packaging_cache_and_placeholder(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            config = root / 'config.jsonc'
            config.write_text(json.dumps({'sws2-cs2mapmanager': {'Maps': {'Available': {'pictured': '123', 'missing': '456'}}}}))
            png = root / 'real.png'
            png.write_bytes(base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aFksAAAAASUVORK5CYII='))
            images = root / 'images.json'
            images.write_text(json.dumps({'pictured': 'real.png'}))
            command = [sys.executable, str(Path(thumbnails.__file__)), '--config', str(config), '--workshop-dir', str(root / 'addon'), '--offline']
            first = subprocess.run(command + ['--images', str(images)], capture_output=True, text=True, check=True)
            self.assertIn('Packaged 1/2', first.stdout)
            css = root / 'addon/panorama/styles/custom_game/sws2cs2menus/map_thumbnails.css'
            self.assertIn('.' + thumbnails.thumbnail_class('workshop:123'), css.read_text())
            self.assertNotIn('.' + thumbnails.thumbnail_class('workshop:456'), css.read_text())
            self.assertNotIn('https://', css.read_text())
            output = root / ('addon/panorama/images/custom_game/sws2cs2menus/maps/' + thumbnails.thumbnail_class('workshop:123') + '.png')
            self.assertEqual(output.read_bytes(), png.read_bytes())
            # A later offline generation keeps pictures already packaged.
            second = subprocess.run(command, capture_output=True, text=True, check=True)
            self.assertIn('Packaged 1/2', second.stdout)
            # A failed replacement must keep the working cached picture.
            png.write_bytes(b'not an image')
            failed = subprocess.run(command + ['--images', str(images)], capture_output=True, text=True, check=True)
            self.assertIn('Packaged 1/2', failed.stdout)
            self.assertIn('PNG or JPEG', failed.stderr)
            self.assertTrue(output.read_bytes().startswith(b'\x89PNG'))


if __name__ == '__main__':
    unittest.main()
