"""Safety checks use synthetic ELF headers and fake local service/health programs.

These tests never run Unity, systemd, SSH, or a network health check.
"""
import json
import os
from pathlib import Path
import pwd
import subprocess
import sys
import tempfile
import unittest


HELPER = Path(__file__).resolve().parents[1] / "Deploy/DigitalOcean/release.py"


class ReleaseSafetyTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="ascendance-release-")
        self.addCleanup(self.temporary.cleanup)
        self.base = Path(self.temporary.name)
        self.root = self.base / "install"
        self.units = self.base / "units"
        self.units.mkdir()
        self.unit = self.units / "ascendance.service"
        self.fake = self.base / "fake-systemctl"
        self.fake.write_text("#!/usr/bin/env python3\n" + f"""
import os
from pathlib import Path
import sys
base = Path({str(self.base)!r})
args = sys.argv[1:]
with (base / 'service-calls').open('a') as log:
    log.write(' '.join(args) + '\\n')
if args[0] == 'show':
    if '--property=LoadState' in args:
        print('loaded' if (base / 'unmanaged-service').exists() else 'not-found')
    elif '--property=DropInPaths' in args:
        print('/unmanaged/override.conf' if (base / 'service-override').exists() else '')
    else:
        print(base / 'units/ascendance.service')
elif args[0] == 'restart':
    current = base / 'install/current'
    release = Path(os.readlink(current)).parts[1] if current.is_symlink() else ''
    failure = base / 'fail-restart'
    if failure.exists() and failure.read_text() in ('all', release):
        sys.exit(1)
""")
        self.fake.chmod(0o755)
        self.health = self.base / "health"
        self.health.write_text("#!/usr/bin/env python3\n" + f"""
from pathlib import Path
import os
import sys
import time
base = Path({str(self.base)!r})
release = os.environ['ASCENDANCE_RELEASE']
with (base / 'health-calls').open('a') as log:
    log.write(release + '\\n')
if (base / 'health-timeout').exists():
    time.sleep(1)
failure = base / 'unhealthy'
sys.exit(1 if failure.exists() and failure.read_text() in ('all', release) else 0)
""")
        self.health.chmod(0o755)

    def run_helper(self, action, *options, ok=True):
        command = [sys.executable, str(HELPER), action, *map(str, options)]
        if action != "manifest":
            command += ["--root", str(self.root), "--service-command", str(self.fake)]
        result = subprocess.run(command, capture_output=True, text=True, env=dict(os.environ, PYTHONDONTWRITEBYTECODE="1"), timeout=10, umask=0o077)
        self.assertEqual(result.returncode == 0, ok, result.stdout + result.stderr)
        return result

    def initialize(self, ok=True, **overrides):
        options = {"config-dir": self.base / "config", "data-home": self.base / "data", "log-dir": self.base / "logs",
                   "unit-dir": self.units, "user": pwd.getpwuid(os.getuid()).pw_name, "room": "ASC-TEST", **overrides}
        return self.run_helper("init", *[part for key, value in options.items() for part in ("--" + key, value)], ok=ok)

    def build(self, name="artifact"):
        build = self.base / name
        build.mkdir()
        elf = b"\x7fELF\x02\x01" + bytes(12) + b"\x3e\x00" + b"test fixture, never executed"
        (build / "AscendanceServer.x86_64").write_bytes(elf)
        (build / "UnityPlayer.so").write_bytes(elf)
        (build / "AscendanceServer_Data").mkdir()
        (build / "AscendanceServer_Data/globalgamemanagers").write_bytes(b"synthetic test data")
        return build

    def manifest(self, build, name="manifest.json"):
        manifest = self.base / name
        self.run_helper("manifest", "--build-dir", build, "--output", manifest)
        return manifest

    def stage(self, release, build=None, manifest=None, ok=True):
        if build is None:
            build = self.build("artifact-" + release)
        if manifest is None:
            manifest = self.manifest(build, "manifest-" + release + ".json")
        return self.run_helper("stage", "--release", release, "--build-dir", build, "--manifest", manifest, ok=ok)

    def activate(self, release, ok=True, *options):
        return self.run_helper("activate", "--release", release, "--health-command", self.health, *options, ok=ok)

    def test_fresh_layout_requires_absent_target_and_service(self):
        self.root.mkdir()
        self.initialize(ok=False)
        self.root.rmdir()
        self.unit.write_text("unmanaged\n")
        self.initialize(ok=False)
        self.unit.unlink()
        (self.base / "unmanaged-service").touch()
        self.initialize(ok=False)
        self.assertFalse(self.root.exists())

    def test_external_existing_data_overlap_and_symlink_are_refused(self):
        existing = self.base / "existing"
        existing.mkdir()
        (existing / "campaign.json").write_text("keep")
        self.initialize(ok=False, **{"data-home": existing})
        self.assertEqual((existing / "campaign.json").read_text(), "keep")
        self.initialize(ok=False, **{"data-home": self.base / "config"})
        link = self.base / "link"
        link.symlink_to(existing, target_is_directory=True)
        self.initialize(ok=False, **{"data-home": link / "new"})
        self.assertFalse(self.root.exists())

    def test_incomplete_artifact_and_symlink_files_or_ancestors_are_refused(self):
        build = self.build()
        (build / "UnityPlayer.so").unlink()
        self.run_helper("manifest", "--build-dir", build, "--output", self.base / "bad.json", ok=False)
        (build / "UnityPlayer.so").symlink_to(build / "AscendanceServer.x86_64")
        self.run_helper("manifest", "--build-dir", build, "--output", self.base / "bad.json", ok=False)
        alias = self.base / "alias"
        alias.symlink_to(build, target_is_directory=True)
        self.run_helper("manifest", "--build-dir", alias, "--output", self.base / "bad.json", ok=False)

    def test_manifest_traversal_absolute_paths_and_tampering_are_refused(self):
        self.initialize()
        build = self.build()
        manifest = self.manifest(build)
        original = json.loads(manifest.read_text())
        for name in ("../escape", "/tmp/escape", "AscendanceServer_Data/../../escape", "AscendanceServer_Data\\escape"):
            invalid = json.loads(json.dumps(original))
            invalid["files"][name] = "0" * 64
            manifest.write_text(json.dumps(invalid))
            self.stage("invalid", build, manifest, ok=False)
        manifest.write_text(json.dumps(original))
        (build / "AscendanceServer_Data/globalgamemanagers").write_bytes(b"changed")
        self.stage("tampered", build, manifest, ok=False)
        self.assertEqual(list((self.root / "releases").iterdir()), [])

    def test_successful_activation_retains_data_and_all_releases(self):
        self.initialize()
        campaign = self.base / "data/campaign.json"
        campaign.write_text("keep")
        self.stage("one")
        self.activate("one")
        self.stage("two")
        self.activate("two")
        self.assertEqual(os.readlink(self.root / "current"), "releases/two/build")
        self.assertEqual(campaign.read_text(), "keep")
        self.assertEqual(sorted(p.name for p in (self.root / "releases").iterdir()), ["one", "two"])
        for path in (self.root, self.root / "releases", self.root / "releases/two", self.root / "releases/two/build", self.root / "releases/two/build/AscendanceServer_Data"):
            self.assertEqual(path.stat().st_mode & 0o777, 0o755)
        self.assertEqual(self.unit.stat().st_mode & 0o777, 0o644)
        self.assertEqual((self.root / "run-ascendance.sh").stat().st_mode & 0o777, 0o755)
        self.assertNotIn("enable", (self.base / "service-calls").read_text())

    def test_failed_health_restores_previous_release_and_restarts_it(self):
        self.initialize()
        self.stage("one")
        self.activate("one")
        self.stage("two")
        (self.base / "unhealthy").write_text("two")
        result = self.activate("two", ok=False)
        self.assertIn("recovery completed", result.stderr)
        self.assertEqual(os.readlink(self.root / "current"), "releases/one/build")
        self.assertEqual((self.base / "health-calls").read_text().splitlines(), ["one", "two", "one"])

    def test_failed_initial_restart_restores_absence_and_stops_service(self):
        self.initialize()
        self.stage("one")
        (self.base / "fail-restart").write_text("one")
        self.activate("one", ok=False)
        self.assertFalse((self.root / "current").is_symlink())
        self.assertTrue((self.base / "service-calls").read_text().endswith("stop ascendance.service\n"))
        self.assertFalse((self.base / "health-calls").exists())

    def test_failed_restart_upgrade_and_failed_recovery_report_both(self):
        self.initialize()
        self.stage("one")
        self.activate("one")
        self.stage("two")
        (self.base / "fail-restart").write_text("all")
        result = self.activate("two", ok=False)
        self.assertIn("Activation failed", result.stderr)
        self.assertIn("recovery failed", result.stderr)
        self.assertEqual(os.readlink(self.root / "current"), "releases/one/build")

    def test_health_timeout_rolls_back_initial_activation(self):
        self.initialize()
        self.stage("one")
        (self.base / "health-timeout").touch()
        self.activate("one", False, "--health-timeout", "0.05")
        self.assertFalse((self.root / "current").exists())

    def test_mutated_configuration_or_staged_release_refuses_switch(self):
        self.initialize()
        self.stage("one")
        env = self.base / "config/server.env"
        original = env.read_bytes()
        env.write_bytes(original.replace(b"ASC-TEST", b"OTHER"))
        self.activate("one", ok=False)
        env.write_bytes(original)
        (self.root / "releases/one/build/UnityPlayer.so").write_bytes(b"changed")
        self.activate("one", ok=False)
        self.assertFalse((self.root / "current").exists())
        self.assertNotIn("restart", (self.base / "service-calls").read_text())

    def test_current_external_symlink_and_missing_old_release_are_refused(self):
        self.initialize()
        self.stage("one")
        current = self.root / "current"
        current.symlink_to(self.base)
        self.activate("one", ok=False)
        current.unlink()
        current.symlink_to("releases/missing/build")
        self.activate("one", ok=False)
        self.assertNotIn("restart", (self.base / "service-calls").read_text())

    def test_duplicate_release_and_symlinked_staged_artifact_are_refused(self):
        self.initialize()
        build = self.build()
        manifest = self.manifest(build)
        self.stage("one", build, manifest)
        self.stage("one", build, manifest, ok=False)
        binary = self.root / "releases/one/build/AscendanceServer.x86_64"
        binary.unlink()
        binary.symlink_to(build / binary.name)
        self.activate("one", ok=False)
        self.assertNotIn("restart", (self.base / "service-calls").read_text())

    def test_special_artifact_files_and_duplicate_manifest_keys_are_refused(self):
        build = self.build()
        fifo = build / "pipe"
        os.mkfifo(fifo)
        self.run_helper("manifest", "--build-dir", build, "--output", self.base / "manifest.json", ok=False)
        fifo.unlink()
        self.initialize()
        manifest = self.base / "duplicate.json"
        manifest.write_text('{"format":1,"format":1,"files":{}}')
        self.stage("one", build, manifest, ok=False)

    def test_managed_nonregular_files_and_malformed_metadata_are_refused(self):
        self.initialize()
        metadata = self.root / "managed.json"
        original_metadata = metadata.read_bytes()
        metadata.write_text("[]")
        self.run_helper("status", ok=False)
        metadata.write_bytes(original_metadata)
        for path in (self.base / "config/server.env", self.root / "run-ascendance.sh", self.unit):
            original = path.read_bytes()
            path.unlink()
            os.mkfifo(path)
            self.run_helper("status", ok=False)
            path.unlink()
            path.write_bytes(original)

    def test_changed_account_identity_and_service_overrides_refuse_activation(self):
        self.initialize()
        self.stage("one")
        metadata = self.root / "managed.json"
        original = json.loads(metadata.read_text())
        altered = dict(original, uid=original["uid"] + 1)
        metadata.write_text(json.dumps(altered))
        self.activate("one", ok=False)
        metadata.write_text(json.dumps(original))
        (self.base / "service-override").touch()
        self.activate("one", ok=False)
        self.assertFalse((self.root / "current").exists())
        self.assertNotIn("restart", (self.base / "service-calls").read_text())


if __name__ == "__main__":
    unittest.main()
