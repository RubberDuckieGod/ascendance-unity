"""Client-only handoff contracts; no server, SCP, game or Windows ACL execution here."""
import copy
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
SOURCE = "55d25c03a3279a8396d5b8954e73180e08f7a0c5"
RELEASE = "unity-0.10.2h-multiplayer-55d25c0"


@unittest.skipUnless(PWSH, "PowerShell is required for client handoff fixtures")
class ClientOnlyHandoff(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="asc-client-only-fixture-")
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)
        self.invite = {"format": 1, "endpoint": "159.65.100.206:7777", "room": "ASC-PRIMARY",
                       "certificateSha256": "a" * 64, "accessKey": "b" * 64}
        self.profile = self.folder / "private-fixture.json"
        self.profile.write_text(json.dumps(self.invite))
        self.descriptor = {"format": 1, "release": RELEASE, "sourceRevision": SOURCE,
                           "buildVersion": "0.10.2h", "endpoint": "159.65.100.206:7777",
                           "client": {"archive": {
                               "url": "https://github.com/RubberDuckieGod/ascendance-unity/releases/download/" + RELEASE + "/ascendance-windows-" + RELEASE + ".zip",
                               "sha256": "c" * 64}}}
        self.descriptor_file = self.folder / "release.json"
        self.descriptor_file.write_text(json.dumps(self.descriptor))
        self.provenance = {"schemaVersion": 1, "outcome": "success", "exitCode": 0,
                           "build": {"target": "StandaloneWindows64"},
                           "source": {"vcs": "git", "revision": SOURCE, "dirty": False}}
        self.provenance_file = self.folder / "build.provenance.json"
        self.provenance_file.write_text(json.dumps(self.provenance))
        self.game = self.folder / "Game with spaces"
        self.game.mkdir()
        self.staging = self.folder / "Private staging"
        self.staging.mkdir()
        self.launcher = self.game / "Play-AscendanceMultiplayer.ps1"
        self.launcher.write_bytes(b"original archived launcher")
        self.game_executable = self.game / "Ascendance.exe"
        self.game_executable.write_bytes(b"unchanged synthetic executable")
        self.private_invite = self.game / "Ascendance.invite.json"
        self.private_invite.write_bytes(self.profile.read_bytes())

    def ps(self, body):
        def quote(value): return "'" + str(value).replace("'", "''") + "'"
        fixture = self.folder / "fixture.ps1"
        fixture.write_text("$ErrorActionPreference='Stop'\n"
                           "function global:Invoke-WebRequest { throw 'No network permitted in this fixture' }\n"
                           "function global:scp { throw 'No SCP permitted in this fixture' }\n"
                           "function global:ssh { throw 'No SSH permitted in this fixture' }\n"
                           + ". " + quote(ROOT / "tools/windows-install-client.ps1")
                           + " -ReleaseDescriptorUrl 'https://github.com/RubberDuckieGod/ascendance-unity/releases/download/fixture/release.json' -ReleaseDescriptorSha256 ('f'*64)\n"
                           + "$profilePath=" + quote(self.profile) + "\n"
                           + "$descriptorPath=" + quote(self.descriptor_file) + "\n"
                           + "$provenancePath=" + quote(self.provenance_file) + "\n" + body + "\n")
        environment = {**os.environ, "XDG_CONFIG_HOME": str(self.folder / "config"),
                       "XDG_DATA_HOME": str(self.folder / "data"), "XDG_CACHE_HOME": str(self.folder / "cache")}
        return subprocess.run([PWSH, "-NoLogo", "-NoProfile", "-NonInteractive", "-File", str(fixture)],
                              env=environment, text=True, capture_output=True, timeout=15)

    def test_acl_and_safe_download_helpers_are_exactly_the_validated_implementation(self):
        original = (ROOT / "tools/windows-deploy-multiplayer.ps1").read_text()
        client = (ROOT / "tools/windows-install-client.ps1").read_text()
        old_helpers = original[original.index("function Assert-AscendanceDownloadUrl {"):
                               original.index("if ($MyInvocation.InvocationName -eq '.') { return }")]
        new_helpers = client[client.index("function Assert-AscendanceDownloadUrl {"):
                            client.index("function Assert-AscendanceClientRelease {")]
        self.assertEqual(old_helpers.strip(), new_helpers.strip())

    def test_client_helpers_import_without_download_ssh_or_scp(self):
        result = self.ps("'imported'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "imported")

    def test_reviewed_launcher_overlay_verifies_pin_and_changes_only_launcher(self):
        def quote(value): return "'" + str(value).replace("'", "''") + "'"
        before_executable = self.game_executable.read_bytes()
        before_invite = self.private_invite.read_bytes()
        script = ROOT / "tools/Play-AscendanceMultiplayer.ps1"
        result = self.ps(
            "function global:Invoke-WebRequest { param($Uri,$OutFile,[switch]$UseBasicParsing); "
            "if($Uri -cne 'https://raw.githubusercontent.com/RubberDuckieGod/ascendance-unity/8c143b7e6a7b5ec3c151b7692dbc7ed57cf48860/tools/Play-AscendanceMultiplayer.ps1'){throw 'Unexpected URL'}; "
            "Copy-Item -LiteralPath " + quote(script) + " -Destination $OutFile }; "
            "Install-AscendanceReviewedLauncher " + quote(self.game) + " " + quote(self.staging) + "; 'installed'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "installed")
        self.assertEqual(self.launcher.read_bytes(), script.read_bytes())
        self.assertEqual(self.game_executable.read_bytes(), before_executable)
        self.assertEqual(self.private_invite.read_bytes(), before_invite)
        self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)

    def test_modified_launcher_download_is_refused_before_replacement(self):
        def quote(value): return "'" + str(value).replace("'", "''") + "'"
        before = self.launcher.read_bytes()
        result = self.ps(
            "function global:Invoke-WebRequest { param($Uri,$OutFile,[switch]$UseBasicParsing); "
            "[IO.File]::WriteAllText($OutFile,'tampered public launcher') }; "
            "Install-AscendanceReviewedLauncher " + quote(self.game) + " " + quote(self.staging))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("reviewed release checksum", result.stderr)
        self.assertEqual(self.launcher.read_bytes(), before)
        self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)

    def test_existing_staged_launcher_is_refused_without_replacement_or_network(self):
        def quote(value): return "'" + str(value).replace("'", "''") + "'"
        staged = self.staging / "Play-AscendanceMultiplayer.reviewed.ps1"
        staged.write_bytes(b"must not overwrite")
        before = self.launcher.read_bytes()
        result = self.ps("Install-AscendanceReviewedLauncher " + quote(self.game) + " " + quote(self.staging))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("download path already exists", result.stderr)
        self.assertEqual(staged.read_bytes(), b"must not overwrite")
        self.assertEqual(self.launcher.read_bytes(), before)

    @unittest.skipUnless(os.name == "posix", "POSIX symlink fixture")
    def test_launcher_symlink_is_refused_before_download(self):
        def quote(value): return "'" + str(value).replace("'", "''") + "'"
        self.launcher.unlink()
        self.launcher.symlink_to(self.game_executable)
        before = self.game_executable.read_bytes()
        result = self.ps("Install-AscendanceReviewedLauncher " + quote(self.game) + " " + quote(self.staging))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("reparse point", result.stderr)
        self.assertEqual(self.game_executable.read_bytes(), before)

    def test_matching_h_release_and_clean_windows_provenance_are_accepted(self):
        result = self.ps("$d=Get-Content $descriptorPath -Raw | ConvertFrom-Json; "
                         f"Assert-AscendanceClientRelease $d '{RELEASE}' '{SOURCE}'; "
                         f"Assert-AscendanceClientProvenance $provenancePath '{SOURCE}'; 'verified'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "verified")

    def test_mismatched_release_source_version_endpoint_or_archive_is_refused(self):
        for key, value in [("release", "other-release"), ("sourceRevision", "0" * 40),
                           ("buildVersion", "0.10.2c"), ("endpoint", "other:7777"), ("format", True)]:
            data = copy.deepcopy(self.descriptor)
            data[key] = value
            self.descriptor_file.write_text(json.dumps(data))
            result = self.ps("$d=Get-Content $descriptorPath -Raw | ConvertFrom-Json; "
                             f"Assert-AscendanceClientRelease $d '{RELEASE}' '{SOURCE}'")
            with self.subTest(key=key): self.assertNotEqual(result.returncode, 0)
        data = copy.deepcopy(self.descriptor)
        data["client"]["archive"]["url"] = "https://example.com/game.zip"
        self.descriptor_file.write_text(json.dumps(data))
        result = self.ps("$d=Get-Content $descriptorPath -Raw | ConvertFrom-Json; "
                         f"Assert-AscendanceClientRelease $d '{RELEASE}' '{SOURCE}'")
        self.assertNotEqual(result.returncode, 0)

    def test_dirty_wrong_source_or_non_windows_build_is_refused(self):
        for section, key, value in [("source", "dirty", True), ("source", "dirty", "false"),
                                    ("source", "revision", "1" * 40), ("build", "target", "StandaloneLinux64")]:
            proof = copy.deepcopy(self.provenance)
            proof[section][key] = value
            self.provenance_file.write_text(json.dumps(proof))
            result = self.ps(f"Assert-AscendanceClientProvenance $provenancePath '{SOURCE}'")
            with self.subTest(section=section, key=key): self.assertNotEqual(result.returncode, 0)

    def test_valid_profile_is_not_written_to_success_output(self):
        result = self.ps("Read-AscendanceClientProfile $profilePath '159.65.100.206:7777' 'ASC-PRIMARY' ('a'*64) | Out-Null; 'private profile verified'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "private profile verified")
        self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)

    def test_profile_type_room_case_endpoint_key_or_pin_mismatch_is_private_refusal(self):
        for field, value in [("format", "1"), ("format", True), ("room", "asc-primary"),
                             ("endpoint", "other:7777"), ("certificateSha256", "c" * 64),
                             ("accessKey", ["b" * 64]), ("accessKey", "short")]:
            profile = dict(self.invite)
            profile[field] = value
            self.profile.write_text(json.dumps(profile))
            result = self.ps("Read-AscendanceClientProfile $profilePath '159.65.100.206:7777' 'ASC-PRIMARY' ('a'*64) | Out-Null")
            with self.subTest(field=field, value=value):
                self.assertNotEqual(result.returncode, 0)
                self.assertNotIn(self.invite["accessKey"], result.stdout + result.stderr)

    def test_malformed_private_profile_does_not_echo_its_contents(self):
        self.profile.write_text('{"accessKey":"private-fixture-secret-do-not-print",malformed}')
        result = self.ps("Read-AscendanceClientProfile $profilePath '159.65.100.206:7777' 'ASC-PRIMARY' ('a'*64) | Out-Null")
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn("private-fixture-secret-do-not-print", result.stdout + result.stderr)

    def test_private_storage_refuses_local_root_desktop_onedrive_and_prefix_siblings(self):
        def quote(value): return "'" + str(value).replace("'", "''") + "'"
        local = self.folder / "LocalAppData"
        desktop = local / "Desktop"
        onedrive = local / "OneDrive"
        excluded = "@(" + quote(desktop) + "," + quote(onedrive) + ")"
        for path in [local, desktop / "game", onedrive / "game", self.folder / "LocalAppData-Other" / "game"]:
            result = self.ps("Assert-AscendanceClientStorage " + quote(path) + " " + quote(local) + " " + excluded)
            with self.subTest(path=path): self.assertNotEqual(result.returncode, 0)
            self.assertFalse(path.exists())
        allowed = local / "Ascendance" / "fresh-game"
        result = self.ps("Assert-AscendanceClientStorage " + quote(allowed) + " " + quote(local) + " " + excluded + "; 'private location verified'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "private location verified")
        self.assertFalse(allowed.exists())

    def test_client_acl_driver_reuses_single_validated_supervisor_source_substitution(self):
        original = (ROOT / "tests/windows/Test-DeploymentAcl.ps1").read_text()
        needle = "Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\\..\\tools\\windows-deploy-multiplayer.ps1') -Destination $WrapperPath"
        self.assertEqual(original.count(needle), 1)
        result = self.ps("$tokens=$null;$errors=$null; "
                         "[Management.Automation.Language.Parser]::ParseFile('"
                         + str(ROOT / "tests/windows/Test-ClientOnlyAcl.ps1")
                         + "',[ref]$tokens,[ref]$errors) | Out-Null; if($errors.Count){throw 'Parser errors'}; 'parsed'")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "parsed")

    def test_actual_client_main_has_no_remote_deployment_or_game_launch_command(self):
        result = self.ps("$tokens=$null;$errors=$null; "
                         "$ast=[Management.Automation.Language.Parser]::ParseFile('"
                         + str(ROOT / "tools/windows-install-client.ps1")
                         + "',[ref]$tokens,[ref]$errors); if($errors.Count){throw 'Parser errors'}; "
                         "$ast.FindAll({param($n) $n -is [Management.Automation.Language.CommandAst]},$true) "
                         "| ForEach-Object {$_.GetCommandName()} | ConvertTo-Json")
        self.assertEqual(result.returncode, 0, result.stderr)
        names = json.loads(result.stdout)
        self.assertIn("scp", names)
        self.assertNotIn("ssh", names)
        self.assertNotIn("Start-Process", names)
        self.assertNotIn("Set-Acl", names)


if __name__ == "__main__":
    unittest.main()
