"""Pinned test descriptors reject tampering and environment-command injection."""
import contextlib
import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("reviewed_combat_inputs", ROOT / "tools/load_reviewed_combat_inputs.py")
INPUTS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(INPUTS)


class ReviewedCombatInputsTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="asc-review-combat-inputs-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.path = self.root / "reviewed.json"
        self.document = {"release_tag": "unity-combat-reviewed", "source_revision": "a" * 40,
                         "server_archive_sha256": "b" * 64, "server_manifest_sha256": "c" * 64,
                         "client_archive_sha256": "d" * 64, "client_manifest_sha256": "e" * 64}

    def write(self, value=None, raw=None):
        self.path.write_text(raw if raw is not None else json.dumps(self.document if value is None else value))
        return hashlib.sha256(self.path.read_bytes()).hexdigest()

    def test_exact_reviewed_descriptor_maps_only_known_environment_identifiers(self):
        result = INPUTS.load_reviewed_inputs(self.path, self.write())
        self.assertEqual(set(result), {name for name, _ in INPUTS.FIELDS.values()})
        self.assertEqual(result["RELEASE_TAG"], self.document["release_tag"])

    def test_unset_pin_or_mismatched_contents_are_refused(self):
        checksum = self.write()
        for pin in ["", "0" * 64, checksum + "\n", True]:
            with self.subTest(pin=pin), self.assertRaises(ValueError): INPUTS.load_reviewed_inputs(self.path, pin)

    def test_unknown_or_missing_fields_cannot_become_environment_settings(self):
        for document in [{**self.document, "DIGITALOCEAN_ACCESS_TOKEN": "unrecognized"},
                         {key: value for key, value in self.document.items() if key != "source_revision"}]:
            checksum = self.write(document)
            with self.assertRaisesRegex(ValueError, "exactly"):
                INPUTS.load_reviewed_inputs(self.path, checksum)

    def test_duplicate_fields_and_non_object_documents_are_refused(self):
        for raw in ['{"release_tag":"one","release_tag":"two"}', '[]', 'null']:
            checksum = self.write(raw=raw)
            with self.subTest(raw=raw), self.assertRaises(ValueError): INPUTS.load_reviewed_inputs(self.path, checksum)

    def test_multiline_and_shell_syntax_identifiers_are_refused(self):
        for value in ["safe\nNEW_VARIABLE=injected", "$(whoami)", "`whoami`", "release/tag", "safe\r\ninjected", "x" * 65, 123]:
            document = copy.deepcopy(self.document); document["release_tag"] = value
            checksum = self.write(document)
            with self.subTest(value=value), self.assertRaisesRegex(ValueError, "identifier"):
                INPUTS.load_reviewed_inputs(self.path, checksum)

    def test_symlinks_and_oversized_descriptors_are_refused(self):
        checksum = self.write()
        link = self.root / "link.json"
        if hasattr(os, "symlink"):
            link.symlink_to(self.path)
            with self.assertRaisesRegex(ValueError, "regular"):
                INPUTS.load_reviewed_inputs(link, checksum)
        checksum = self.write(raw=" " * 16385)
        with self.assertRaisesRegex(ValueError, "small"):
            INPUTS.load_reviewed_inputs(self.path, checksum)

    def test_cli_appends_only_validated_environment_lines(self):
        checksum = self.write()
        output = self.root / "github-env"
        output.write_text("KEEP=existing\n")
        with mock.patch.dict(os.environ, {"GITHUB_ENV": str(output)}), contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(INPUTS.main(["--input", str(self.path), "--sha256", checksum]), 0)
        expected = "KEEP=existing\n" + "".join(name + "=" + value + "\n" for name, value in INPUTS.load_reviewed_inputs(self.path, checksum).items())
        self.assertEqual(output.read_text(), expected)

    def test_refused_descriptor_cannot_partially_write_environment(self):
        self.write()
        output = self.root / "github-env"; output.write_text("KEEP=existing\n")
        with mock.patch.dict(os.environ, {"GITHUB_ENV": str(output)}), contextlib.redirect_stderr(io.StringIO()):
            with self.assertRaises(SystemExit) as result:
                INPUTS.main(["--input", str(self.path), "--sha256", "0" * 64])
        self.assertEqual(result.exception.code, 1)
        self.assertEqual(output.read_text(), "KEEP=existing\n")


if __name__ == "__main__":
    unittest.main()
