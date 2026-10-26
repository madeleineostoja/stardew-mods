import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from zipfile import ZipFile

from scripts import mods, release


class PackagingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        root_patch = patch.object(mods, "ROOT", self.root)
        root_patch.start()
        self.addCleanup(root_patch.stop)

    def write(self, path, content):
        file = self.root / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(content, encoding="utf-8")

    def test_content_pack_is_complete_and_excludes_local_files(self):
        package = {
            "path": "patches/example", "folder": "Example",
            "files": ["content.json", "assets", "i18n"],
        }
        self.write("patches/example/manifest.json", json.dumps({
            "Version": "2.2.0", "UniqueID": "Author.Example",
            "ContentPackFor": {"UniqueID": "Pathoschild.ContentPatcher"},
        }))
        content = '{\n// Content Patcher accepts comments\n"Changes": [],\n}\n'
        self.write("patches/example/content.json", content)
        self.write("patches/example/assets/maps/farm.tmx", "map data")
        self.write("patches/example/assets/.DS_Store", "local metadata")
        self.write("patches/example/i18n/default.json", '{}')
        self.write("patches/example/config.json", '{"personal": true}')
        self.write("patches/example/.git/config", "private repository metadata")
        self.write("patches/example/tmp/unrelated.txt", "experiment")
        archive = mods.package_mod("example", package)
        with ZipFile(archive) as output:
            self.assertEqual(set(output.namelist()), {
                "Example/manifest.json", "Example/content.json",
                "Example/assets/maps/farm.tmx", "Example/i18n/default.json",
            })
            self.assertEqual(output.read("Example/content.json").decode(), content)
            self.assertEqual(output.read("Example/assets/maps/farm.tmx"), b"map data")
            self.assertEqual(json.loads(output.read("Example/manifest.json"))["Version"], "2.2.0")

    def test_compiled_package_resolves_version_and_contains_only_its_assembly(self):
        package = {"path": "mods/example", "folder": "Example", "project": "Example.csproj", "files": []}
        self.write("mods/example/manifest.json", json.dumps({
            "Version": "%ProjectVersion%", "UniqueID": "Author.Example", "EntryDll": "Example.dll",
        }))
        self.write("mods/example/Example.csproj", '<Project><PropertyGroup><Version>1.3.4</Version>'
                   '<TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>')
        with self.assertRaisesRegex(ValueError, "missing build output"):
            mods.package_mod("example", package)
        self.write("mods/example/bin/Release/net6.0/Example.dll", "mod assembly")
        self.write("mods/example/bin/Release/net6.0/Stardew Valley.dll", "not redistributable")
        self.write("mods/example/bin/Release/net6.0/OtherMod.dll", "unrelated mod")
        archive = mods.package_mod("example", package)
        self.assertEqual(archive.name, "Example-1.3.4.zip")
        with ZipFile(archive) as output:
            self.assertEqual(set(output.namelist()), {"Example/manifest.json", "Example/Example.dll"})
            self.assertEqual(json.loads(output.read("Example/manifest.json"))["Version"], "1.3.4")
        self.assertEqual(json.loads((self.root / "mods/example/manifest.json").read_text())["Version"],
                         "%ProjectVersion%")
        self.assertNotEqual(mods.metadata("example", package)["tag"], mods.metadata("other", package)["tag"])

    def test_unregistered_mod_cannot_silently_skip_ci(self):
        self.write("packages.json", "{}")
        self.write("mods/new-mod/manifest.json", "{}")
        with self.assertRaisesRegex(ValueError, "does not match mod directories"):
            mods.load_packages()

    def test_compiled_build_runs_tests_only_when_registered(self):
        for tests in (None, "Tests.csproj"):
            with self.subTest(tests=tests):
                package = {"path": "mods/example", "project": "Example.csproj"}
                if tests:
                    package["tests"] = tests
                with patch.object(mods, "validate"), patch.object(mods.subprocess, "run") as run, \
                        patch.object(mods, "package_mod", return_value=self.root / "dist/Example.zip") as package_mod:
                    mods.build("example", package, None)
                self.assertEqual([call.args[0][1] for call in run.call_args_list],
                                 ["build", "test"] if tests else ["build"])
                package_mod.assert_called_once_with("example", package)

    def test_failed_build_does_not_create_a_package(self):
        package = {"path": "mods/example", "project": "Example.csproj", "tests": "Tests.csproj"}
        with patch.object(mods, "validate"), patch.object(mods.subprocess, "run") as run:
            run.side_effect = subprocess.CalledProcessError(1, "dotnet")
            with self.assertRaises(subprocess.CalledProcessError):
                mods.build("example", package, None)
        self.assertFalse((self.root / "dist").exists())


class ReleaseTests(unittest.TestCase):
    def test_missing_release_is_distinguished_from_api_failure(self):
        with patch.object(release.subprocess, "run") as run:
            run.return_value = subprocess.CompletedProcess([], 1, "", "gh: Not Found (HTTP 404)")
            self.assertFalse(release.release_exists("owner/repo", "example/v1.0.0"))
            self.assertIn("example%2Fv1.0.0", run.call_args.args[0][-1])
            run.return_value = subprocess.CompletedProcess([], 1, "", "gh: Forbidden (HTTP 403)")
            with self.assertRaisesRegex(RuntimeError, "Could not check release"):
                release.release_exists("owner/repo", "example/v1.0.0")

    def test_existing_release_is_not_republished(self):
        with patch.object(release.subprocess, "run") as run:
            run.return_value = subprocess.CompletedProcess([], 0, '{"draft": false}', "")
            release.publish("impactful", "owner/repo", "new-commit")
            run.assert_called_once()

    def test_conflicting_tag_blocks_release(self):
        with patch.object(release, "release_exists", return_value=False), \
                patch.object(release.subprocess, "run") as run:
            run.return_value = subprocess.CompletedProcess([], 0, "old-commit\n", "")
            with self.assertRaisesRegex(ValueError, "another commit"):
                release.publish("impactful", "owner/repo", "new-commit")
            run.assert_called_once()


if __name__ == "__main__":
    unittest.main()
