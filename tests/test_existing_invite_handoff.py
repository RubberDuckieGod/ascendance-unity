"""Local invitation-copy flow with synthetic data and mocked ACL/SCP; no network."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]
PWSH = shutil.which("pwsh")
if not PWSH:
    candidate = Path("/workspace/.unity-tools/powershell/pwsh")
    PWSH = str(candidate) if candidate.exists() else None


@unittest.skipUnless(PWSH, "PowerShell is required for invitation handoff fixtures")
class ExistingInviteHandoff(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="asc-existing-invite-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.old = self.root / "Existing game"
        self.new = self.root / "New protected client"
        self.old.mkdir()
        self.new.mkdir()
        self.source = self.old / "Ascendance.invite.json"
        self.destination = self.new / "Ascendance.invite.json"
        self.invite = {"format": 1, "endpoint": "159.65.100.206:7777", "room": "ASC-PRIMARY",
                       "certificateSha256": "a" * 64, "accessKey": "b" * 64}
        self.source.write_text(json.dumps(self.invite))
        self.old_game = self.old / "Ascendance.exe"
        self.old_game.write_bytes(b"preserve existing game fixture")

    @staticmethod
    def quote(value):
        return "'" + str(value).replace("'", "''") + "'"

    def ps(self, body):
        fixture = self.root / "fixture.ps1"
        fixture.write_text("$ErrorActionPreference='Stop'\n"
                           "function global:Invoke-WebRequest { throw 'No network permitted' }\n"
                           "function global:scp { throw 'SCP must not be called for a local invitation' }\n"
                           "function global:ssh { throw 'SSH must not be called' }\n"
                           + ". " + self.quote(ROOT / "tools/windows-install-client.ps1")
                           + " -ReleaseDescriptorUrl 'https://github.com/RubberDuckieGod/ascendance-unity/releases/download/fixture/release.json' -ReleaseDescriptorSha256 ('f'*64)\n"
                           + "function Protect-AscendanceOwnerPath { param([string]$Path); $global:protectCalls++ }\n"
                           + "$global:protectCalls=0\n"
                           + "$source=" + self.quote(self.source) + "\n"
                           + "$destination=" + self.quote(self.destination) + "\n"
                           + body + "\n")
        environment = {**os.environ, "XDG_CONFIG_HOME": str(self.root / "config"),
                       "XDG_CACHE_HOME": str(self.root / "cache"), "XDG_DATA_HOME": str(self.root / "data")}
        return subprocess.run([PWSH, "-NoProfile", "-NonInteractive", "-File", str(fixture)],
                              env=environment, text=True, capture_output=True, timeout=30)

    def install(self, source="$source"):
        return "Install-AscendanceConnectionProfile $destination '159.65.100.206:7777' 'ASC-PRIMARY' ('a'*64) " + source + " ''"

    def test_valid_local_invitation_copies_without_scp_and_preserves_original_game_and_profile(self):
        before = self.source.read_bytes()
        before_mode = self.source.stat().st_mode
        result = self.ps(self.install() + "; if($global:protectCalls -ne 2){throw 'Protected-file creation and final verification were not both called'}; 'copied'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("copied", result.stdout)
        self.assertEqual(self.destination.read_bytes(), before)
        self.assertEqual(self.source.read_bytes(), before)
        self.assertEqual(self.source.stat().st_mode, before_mode)
        self.assertEqual(self.old_game.read_bytes(), b"preserve existing game fixture")
        self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)

    def test_wrong_endpoint_room_certificate_and_malformed_profile_refused_before_destination(self):
        for field, value in [("endpoint", "other:7777"), ("room", "OTHER"),
                             ("certificateSha256", "c" * 64), ("accessKey", "short")]:
            with self.subTest(field=field):
                profile = dict(self.invite)
                profile[field] = value
                self.source.write_text(json.dumps(profile))
                before = self.source.read_bytes()
                result = self.ps(self.install())
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse(self.destination.exists())
                self.assertEqual(self.source.read_bytes(), before)
                self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)
        self.source.write_text('{"accessKey":"do-not-print-fixture-secret",malformed}')
        result = self.ps(self.install())
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(self.destination.exists())
        self.assertNotIn("do-not-print-fixture-secret", result.stdout + result.stderr)

    @unittest.skipUnless(os.name == "posix", "POSIX symlink fixture")
    def test_symlink_source_refused_before_destination_creation(self):
        link = self.old / "linked-profile.json"
        link.symlink_to(self.source)
        before = self.source.read_bytes()
        result = self.ps(self.install(self.quote(link)))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("reparse point", result.stderr)
        self.assertFalse(self.destination.exists())
        self.assertEqual(self.source.read_bytes(), before)

    def test_existing_destination_is_never_overwritten(self):
        self.destination.write_bytes(b"preserve another private invitation")
        before = self.source.read_bytes()
        result = self.ps(self.install())
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.destination.read_bytes(), b"preserve another private invitation")
        self.assertEqual(self.source.read_bytes(), before)
        self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)

    def test_original_scp_branch_still_copies_interactively_with_mocked_command(self):
        before = self.source.read_bytes()
        result = self.ps("function global:scp { $global:LASTEXITCODE=0; Copy-Item -LiteralPath $source -Destination $args[-1] -Force }; "
                         + "Install-AscendanceConnectionProfile $destination '159.65.100.206:7777' 'ASC-PRIMARY' ('a'*64) '' 'fixture-key'; 'remote branch copied'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.destination.read_bytes(), before)
        self.assertIn("remote branch copied", result.stdout)
        self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)

    def test_copy_integrity_change_is_detected_and_original_profile_stays_unchanged(self):
        before = self.source.read_bytes()
        result = self.ps("function Copy-Item { param($LiteralPath,$Destination,[switch]$Force); [IO.File]::WriteAllText($Destination,'changed copied fixture') }; " + self.install())
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("changed while being copied", result.stderr)
        self.assertEqual(self.source.read_bytes(), before)
        self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
