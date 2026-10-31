"""Pure PowerShell launcher/archive fixtures; never launch a game or SSH."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import zipfile


ROOT = Path(__file__).resolve().parents[1]
PWSH = shutil.which("pwsh")
if not PWSH:
    candidate = Path("/workspace/.unity-tools/powershell/pwsh")
    PWSH = str(candidate) if candidate.exists() else None


@unittest.skipUnless(PWSH, "PowerShell is required for launcher fixtures")
class MultiplayerLauncher(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="asc-private-invite-fixture-")
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.profile = self.folder / "fixture-invite.json"
        self.invite = {"format": 1, "endpoint": "159.65.100.206:7777", "room": "ASC-PRIMARY",
                       "certificateSha256": "a" * 64, "accessKey": "b" * 64}
        self.profile.write_text(json.dumps(self.invite))

    def ps(self, body):
        def quote(value): return "'" + str(value).replace("'", "''") + "'"
        script = self.folder / "fixture.ps1"
        script.write_text("$ErrorActionPreference='Stop'\n"
                          + ". " + quote(ROOT / "tools/Play-AscendanceMultiplayer.ps1") + "\n"
                          + ". " + quote(ROOT / "tools/windows-deploy-multiplayer.ps1") + " -ReleaseDescriptorUrl 'https://github.com/RubberDuckieGod/ascendance-unity/releases/download/fixture/release.json' -ReleaseDescriptorSha256 ('a'*64)\n"
                          + "$inviteFile=" + quote(self.profile) + "\n" + body + "\n")
        environment = {**os.environ, "XDG_CONFIG_HOME": str(self.folder / "config"),
                       "XDG_CACHE_HOME": str(self.folder / "cache"), "XDG_DATA_HOME": str(self.folder / "data")}
        return subprocess.run([PWSH, "-NoProfile", "-File", str(script)], env=environment,
                              capture_output=True, text=True, timeout=15)

    def test_valid_invite_and_spaced_name_generate_tls_arguments_without_secret(self):
        output = self.ps("$invite=Read-AscendanceInvite $inviteFile; Get-AscendancePlayerArguments $invite 'Owner Player' | ConvertTo-Json")
        self.assertEqual(output.returncode, 0, output.stderr)
        arguments = json.loads(output.stdout)
        self.assertIn("-ascendance-tls", arguments)
        self.assertIn('"Owner Player"', arguments)
        self.assertIn("a" * 64, arguments)
        self.assertNotIn(self.invite["accessKey"], output.stdout + output.stderr)

    def test_missing_pin_or_short_access_key_are_not_accepted(self):
        for field, value in [("certificateSha256", ""), ("accessKey", "short")]:
            with self.subTest(field=field):
                invite = dict(self.invite)
                invite[field] = value
                self.profile.write_text(json.dumps(invite))
                output = self.ps("Read-AscendanceInvite $inviteFile | Out-Null")
                self.assertNotEqual(output.returncode, 0)
                self.assertNotIn(self.invite["accessKey"], output.stderr)

    def test_malformed_private_json_does_not_echo_its_secret(self):
        self.profile.write_text('{"accessKey":"do-not-print-private-fixture",malformed}')
        output = self.ps("Read-AscendanceInvite $inviteFile | Out-Null")
        self.assertNotEqual(output.returncode, 0)
        self.assertIn("fresh invite", output.stderr)
        self.assertNotIn("do-not-print-private-fixture", output.stdout + output.stderr)

    def test_player_name_metacharacters_are_refused(self):
        output = self.ps("$invite=Read-AscendanceInvite $inviteFile; Get-AscendancePlayerArguments $invite 'Owner\" -other-flag' | Out-Null")
        self.assertNotEqual(output.returncode, 0)
        self.assertNotIn(self.invite["accessKey"], output.stdout + output.stderr)

    def test_only_expected_https_download_repository_and_pinned_source_are_accepted(self):
        for url in ["http://github.com/RubberDuckieGod/ascendance-unity/releases/download/x/y",
                    "https://example.com/a", "https://raw.githubusercontent.com/RubberDuckieGod/ascendance-unity/main/a.py"]:
            with self.subTest(url=url):
                output = self.ps("Assert-AscendanceDownloadUrl '" + url + "'")
                self.assertNotEqual(output.returncode, 0)
        output = self.ps("Assert-AscendanceDownloadUrl 'https://raw.githubusercontent.com/RubberDuckieGod/ascendance-unity/" + "a" * 40 + "/Deploy/script.py'; 'validated'")
        self.assertEqual(output.returncode, 0, output.stderr)

    def test_client_zip_traversal_is_rejected_before_extraction(self):
        archive = self.folder / "unsafe.zip"
        with zipfile.ZipFile(archive, "w") as target:
            target.writestr("Windows/../../escaped", "bad")
        destination = self.folder / "client"
        output = self.ps("Expand-AscendanceClientZip '" + str(archive) + "' '" + str(destination) + "'")
        self.assertNotEqual(output.returncode, 0)
        self.assertFalse(destination.exists())
        self.assertFalse((self.folder / "escaped").exists())

    def test_safe_client_zip_extracts_whole_payload_without_executing_it(self):
        archive = self.folder / "safe.zip"
        with zipfile.ZipFile(archive, "w") as target:
            target.writestr("Windows/Ascendance.exe", b"synthetic-not-executable")
            target.writestr("Windows/Ascendance_Data/fixture", b"required-data")
        destination = self.folder / "client"
        output = self.ps("Expand-AscendanceClientZip '" + str(archive) + "' '" + str(destination) + "'")
        self.assertEqual(output.returncode, 0, output.stderr)
        self.assertEqual((destination / "Windows/Ascendance.exe").read_bytes(), b"synthetic-not-executable")
        self.assertEqual((destination / "Windows/Ascendance_Data/fixture").read_bytes(), b"required-data")

    def test_existing_private_file_cannot_be_truncated_by_precreation(self):
        before = self.profile.read_bytes()
        output = self.ps("New-AscendanceOwnerFile $inviteFile")
        self.assertNotEqual(output.returncode, 0)
        self.assertEqual(self.profile.read_bytes(), before)
        self.assertNotIn(self.invite["accessKey"], output.stdout + output.stderr)

    @unittest.skipUnless(os.name == "posix", "Windows junctions are tested by the nonadmin Windows CI fixture")
    def test_reparse_file_is_refused_before_windows_acl_access(self):
        link = self.folder / "linked-invite.json"
        link.symlink_to(self.profile)
        before = self.profile.read_bytes()
        output = self.ps("Protect-AscendanceOwnerPath '" + str(link) + "'")
        self.assertNotEqual(output.returncode, 0)
        self.assertIn("reparse point", output.stderr)
        self.assertEqual(self.profile.read_bytes(), before)

    @unittest.skipUnless(os.name == "posix", "Windows junctions are tested by the nonadmin Windows CI fixture")
    def test_reparse_ancestor_is_refused_before_windows_acl_access(self):
        directory = self.folder / "linked-parent"
        directory.symlink_to(self.folder, target_is_directory=True)
        before = self.profile.read_bytes()
        output = self.ps("Protect-AscendanceOwnerPath '" + str(directory / self.profile.name) + "'")
        self.assertNotEqual(output.returncode, 0)
        self.assertIn("reparse point", output.stderr)
        self.assertEqual(self.profile.read_bytes(), before)


if __name__ == "__main__":
    unittest.main()
