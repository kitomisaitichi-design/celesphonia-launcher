"""Regression checks for CH-only authoring-utility catalog exclusions.

Read-only for the game/source manifest. No archive or game files are modified.
Run: python Tests/TestNativeReferenceCatalogPolicy.py
"""
import importlib.util
import pathlib
import tempfile
import unittest
import json


SCRIPT = pathlib.Path(__file__).resolve().with_name("BuildNativeReferenceCatalog.py")
spec = importlib.util.spec_from_file_location("native_reference_catalog_policy", SCRIPT)
catalog = importlib.util.module_from_spec(spec)
spec.loader.exec_module(catalog)


class CHReferencePolicyTests(unittest.TestCase):
    def test_ch_exclusion_is_exactly_scoped(self):
        exclusions = (
            "TrsData.bin",
            "TrsData_Bing_842023, 102632 PMP.bin",
            "RMDEC.exe",
            "www/RPG-MV-MZ-Decrypter-1.3.3-win.exe",
            "www/Game.rpgproject",
            "www/img/pictures/cloth/desktop.ini",
            "www/img/pictures/under/desktop.ini",
            "www/data/stand_base_actor_01.xlsx",
            "locales/en-US.pak.info",
            "www/js/.vs/js/v15/.suo",
            "www/js/.vs/slnx.sqlite",
            "www/img/tilesets/World_A1.txt",
            "www/asset_credits.txt",
        )
        for path in exclusions:
            with self.subTest(path=path):
                self.assertIsNotNone(catalog.exclusion_reason("ch-current", path, set()))
                self.assertIsNone(catalog.exclusion_reason("gorpg-1-23", path, set()))
                self.assertIsNone(catalog.exclusion_reason("maple-1-7-2", path, set()))

    def test_preserve_all_native_runtime_extensions(self):
        required = (
            "Game.exe", "nw.exe", "nw.dll", "node.dll",
            "natives_blob.bin", "snapshot_blob.bin", "icudtl.dat",
            "resources.pak", "package.json", "www/index.html",
            "locales/en-US.pak", "locales/nested/en-US.pak.info",
            "www/data/System.json", "www/js/plugins.js",
            "www/js/rpg_managers.js", "www/js/plugins/YEP_CoreEngine.js",
            "www/img/characters/Actor1.png", "www/audio/bgm/Theme1.rpgmvo",
            "www/js/plugins/OtherPlugin.exe", "www/data/essential.txt.json",
        )
        for path in required:
            with self.subTest(path=path):
                self.assertIsNone(catalog.exclusion_reason("ch-current", path, set()))
        self.assertIsNone(catalog.exclusion_reason("ch-current", "www/js/plugins/ClipLoger.exe", None))
        self.assertIsNone(catalog.exclusion_reason("ch-current", "www/js/plugins/ClipLoger.exe", {"clipboard_llule"}))
        self.assertIsNotNone(catalog.exclusion_reason("ch-current", "www/js/plugins/ClipLoger.exe", set()))
        self.assertIsNone(catalog.exclusion_reason("ch-current", "www/js/plugins/Clipboard_llule.js", {"clipboard_llule"}))

    def test_plugin_parse_enabled_status(self):
        with tempfile.TemporaryDirectory(prefix="ch-native-policy-") as temp:
            path = pathlib.Path(temp) / "js" / "plugins.js"
            path.parent.mkdir()
            path.write_text('var $plugins = [{"name":"Clipboard_llule","status":false},'
                            '{"name":"YEP_CoreEngine","status":true}];', encoding="utf-8")
            self.assertEqual(catalog.enabled_plugins(path.parent.parent), {"yep_coreengine"})
            path.write_text('var $plugins = [{"name":"Clipboard_llule","status":true}];', encoding="utf-8")
            self.assertEqual(catalog.enabled_plugins(path.parent.parent), {"clipboard_llule"})
            path.write_text('broken JavaScript', encoding="utf-8")
            self.assertIsNone(catalog.enabled_plugins(path.parent.parent))

    def test_three_required_ch_hashes_and_fail_closed(self):
        verified = {"www/"+key: {"Hash": digest} for key,digest in catalog.CH_FINGERPRINTS.items()}
        catalog.require_fingerprints(verified, "ch-current")
        self.assertEqual(len(catalog.CH_FINGERPRINTS), 3)
        for key in catalog.CH_FINGERPRINTS:
            damaged = dict(verified)
            damaged.pop("www/"+key)
            with self.assertRaises(SystemExit):
                catalog.require_fingerprints(damaged, "ch-current")
            damaged = dict(verified)
            damaged["www/"+key] = {"Hash": "0"*64}
            with self.assertRaises(SystemExit):
                catalog.require_fingerprints(damaged, "ch-current")
        catalog.require_fingerprints({}, "maple-1-7-2")

    def test_current_ch_manifest_or_regenerated_manifest(self):
        manifest = SCRIPT.parent.parent / "NativeReferenceFiles-ch-current.json"
        if not manifest.is_file():
            self.skipTest("CH manifest unavailable on this machine")
        inventory = json.loads(manifest.read_text(encoding="utf-8"))
        self.assertEqual(inventory["VersionId"], "ch-current")
        indexed = {entry["Path"]: entry for entry in inventory["Files"]}
        catalog.require_fingerprints(indexed, "ch-current")
        excludes = [path for path in indexed if catalog.exclusion_reason("ch-current", path, set())]
        self.assertIn((len(indexed),len(excludes)), {(13654,118),(13592,56),(13539,3),(13536,0)})


if __name__ == "__main__":
    unittest.main(verbosity=2)
