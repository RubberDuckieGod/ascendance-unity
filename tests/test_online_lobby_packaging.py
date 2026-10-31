"""Synthetic Windows files only; no Unity, network, server or deployment."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock
import zipfile


ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("online_lobby_packaging", ROOT / "tools/package_online_lobby_client.py")
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)


class OnlineLobbyPackaging(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="asc-lobby-package-test-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.build = self.root / "Windows"
        self.build.mkdir()
        (self.build / "Ascendance.exe").write_bytes(b"MZ synthetic game; never executed")
        (self.build / "UnityPlayer.dll").write_bytes(b"MZ synthetic library; never executed")
        (self.build / "Ascendance_Data/Managed").mkdir(parents=True)
        (self.build / "Ascendance_Data/globalgamemanagers").write_bytes(b"authored fixture data")
        (self.build / "Ascendance_Data/Managed/Assembly-CSharp.dll").write_bytes(b"synthetic managed fixture")
        self.source = "a" * 40
        self.proof = {"schemaVersion": 1, "outcome": "success", "exitCode": 0,
                      "build": {"target": "StandaloneWindows64"},
                      "source": {"vcs": "git", "revision": self.source, "dirty": False}}
        self.proof_path = self.build / "Ascendance.provenance.json"
        self.proof_path.write_text(json.dumps(self.proof))
        self.args = SimpleNamespace(windows_build=self.build, source_revision=self.source,
                                    release="unity-0.10.2h-lobby-fixture", output=self.root / "public")

    def refuse(self):
        with self.assertRaises(Exception):
            package.package(self.args)
        self.assertFalse(self.args.output.exists())

    def test_whole_archive_exact_payload_hashes_and_client_only_installer_descriptor(self):
        before = {str(p.relative_to(self.build)): p.read_bytes() for p in self.build.rglob("*") if p.is_file()}
        result = package.package(self.args)
        descriptor = json.loads((self.args.output / "release.json").read_text())
        self.assertTrue(result["clientOnly"])
        self.assertEqual(descriptor["format"], 1)
        self.assertEqual(descriptor["release"], self.args.release)
        self.assertEqual(descriptor["sourceRevision"], self.source)
        self.assertEqual(descriptor["buildVersion"], "0.10.2h")
        self.assertEqual(descriptor["endpoint"], "159.65.100.206:7777")
        self.assertEqual(descriptor["compatibleServer"]["protocolVersion"], 1)
        self.assertFalse(descriptor["compatibleServer"]["deploymentRequired"])
        self.assertNotIn("server", descriptor)
        self.assertNotIn("helpers", descriptor)
        archive_name = "ascendance-windows-" + self.args.release + ".zip"
        self.assertEqual(descriptor["client"]["archive"]["url"],
                         "https://github.com/RubberDuckieGod/ascendance-unity/releases/download/" + self.args.release + "/" + archive_name)
        archive_path = self.args.output / archive_name
        self.assertEqual(descriptor["client"]["archive"]["sha256"], hashlib.sha256(archive_path.read_bytes()).hexdigest())
        self.assertEqual(descriptor["client"]["archive"]["sizeBytes"], archive_path.stat().st_size)
        manifest = json.loads((self.args.output / "windows-client-manifest.json").read_text())["files"]
        with zipfile.ZipFile(archive_path) as archive:
            self.assertEqual(set(archive.namelist()), {"Windows/" + name for name in manifest})
            for name, digest in manifest.items():
                self.assertEqual(hashlib.sha256(archive.read("Windows/" + name)).hexdigest(), digest)
            for name in ("Play-AscendanceMultiplayer.ps1", "Play-AscendanceMultiplayer.cmd"):
                self.assertEqual(archive.read("Windows/" + name), (ROOT / "tools" / name).read_bytes())
            self.assertIn(b"JOIN ONLINE CAMPAIGN", archive.read("Windows/CONNECTION_README.txt"))
            self.assertFalse(any("invite" in name.lower() or "linuxserver" in name.lower() for name in archive.namelist()))
        for name, content in before.items():
            self.assertEqual((self.build / name).read_bytes(), content)
        for line in (self.args.output / "SHA256SUMS").read_text().splitlines():
            expected, name = line.split("  ", 1)
            self.assertEqual(hashlib.sha256((self.args.output / name).read_bytes()).hexdigest(), expected)

    def test_dirty_wrong_source_wrong_platform_and_non_git_provenance_refused(self):
        variants = [("source", "dirty", True), ("source", "revision", "b" * 40),
                    ("source", "vcs", "archive"), ("build", "target", "StandaloneLinux64")]
        for section, key, value in variants:
            with self.subTest(section=section, key=key):
                document = copy.deepcopy(self.proof)
                document[section][key] = value
                self.proof_path.write_text(json.dumps(document))
                self.refuse()
        for schema in (None, 2, True):
            with self.subTest(schema=schema):
                document = copy.deepcopy(self.proof)
                document["schemaVersion"] = schema
                self.proof_path.write_text(json.dumps(document))
                self.refuse()

    def test_private_invite_nested_credentials_and_key_material_refused(self):
        for name, content in [("Ascendance.invite.json", b"{}"),
                              ("nested/config.json", b'{"nested":{"reconnectToken":"fixture-secret"}}'),
                              ("private.pem", b"fixture private binary"),
                              ("notes.txt", b"-----BEGIN PRIVATE KEY-----\nfixture")]:
            with self.subTest(name=name):
                path = self.build / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(content)
                self.refuse()
                path.unlink()

    def test_missing_executable_library_or_all_data_refused(self):
        for name in ("Ascendance.exe", "UnityPlayer.dll"):
            with self.subTest(name=name):
                path = self.build / name
                content = path.read_bytes()
                path.unlink()
                self.refuse()
                path.write_bytes(content)
        for path in (self.build / "Ascendance_Data").rglob("*"):
            if path.is_file():
                path.unlink()
        self.refuse()

    def test_case_colliding_paths_symlinks_and_non_pe_files_refused(self):
        alias = self.build / "ASCENDANCE.EXE"
        alias.write_bytes(b"MZ case-collision fixture")
        self.refuse()
        alias.unlink()
        alias.symlink_to(self.build / "Ascendance.exe")
        self.refuse()
        alias.unlink()
        (self.build / "UnityPlayer.dll").write_bytes(b"not Windows PE")
        self.refuse()

    def test_existing_output_and_overlapping_destinations_refused_without_changes(self):
        self.args.output.mkdir()
        marker = self.args.output / "preserve.txt"
        marker.write_text("existing public output")
        with self.assertRaises(Exception):
            package.package(self.args)
        self.assertEqual(marker.read_text(), "existing public output")
        self.args.output = self.build / "nested-output"
        self.refuse()

    def test_payload_added_during_packaging_refused_before_publication(self):
        original = package.support.add_zip
        changed = False
        def add_and_mutate(*args, **kwargs):
            nonlocal changed
            original(*args, **kwargs)
            if not changed:
                (self.build / "concurrent-build-output").write_bytes(b"changed during archive creation")
                changed = True
        with mock.patch.object(package.support, "add_zip", side_effect=add_and_mutate):
            self.refuse()

    def test_same_inputs_produce_identical_archive_and_metadata(self):
        package.package(self.args)
        first = {path.name: path.read_bytes() for path in self.args.output.iterdir()}
        self.args.output = self.root / "other-public"
        package.package(self.args)
        self.assertEqual(first, {path.name: path.read_bytes() for path in self.args.output.iterdir()})


if __name__ == "__main__":
    unittest.main()
