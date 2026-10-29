"""On-host bootstrap fixtures: no network, service commands, or game execution."""
import contextlib
import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock
import urllib.request


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("install_release", ROOT / "Deploy/DigitalOcean/install-release.py")
INSTALLER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(INSTALLER)


class Response(io.BytesIO):
    pass


class MemoryDownloads:
    def __init__(self, routes):
        self.routes, self.requests = routes, []

    def open(self, request, timeout):
        self.requests.append(request.full_url)
        return Response(self.routes[request.full_url])


class OnHostInstaller(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="asc-onhost-bootstrap-fixture-")
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)
        self.payload = b"synthetic public release file; never executed"
        checksum = hashlib.sha256(self.payload).hexdigest()
        self.data = {
            "format": 1, "release": INSTALLER.RELEASE, "sourceRevision": INSTALLER.SOURCE_REVISION,
            "deploymentRevision": INSTALLER.DEPLOYMENT_REVISION,
            "endpoint": "159.65.100.206:7777",
            "helpers": {name: {"url": INSTALLER.SOURCE_BASE + relative, "sha256": checksum}
                        for name, relative in INSTALLER.HELPER_PATHS.items()},
            "server": {
                "archive": {"url": INSTALLER.RELEASE_BASE + "ascendance-linux-server-" + INSTALLER.RELEASE + ".zip", "sha256": checksum},
                "manifest": {"url": INSTALLER.RELEASE_BASE + "linux-server-manifest.json", "sha256": checksum}},
        }
        self.descriptor_path = self.folder / "descriptor.json"
        self.descriptor_path.write_text(json.dumps(self.data))

    def test_streamed_file_requires_exact_digest_and_keeps_verified_bytes(self):
        target = self.folder / "verified.py"
        downloads = MemoryDownloads({INSTALLER.DESCRIPTOR_URL: self.payload})
        INSTALLER.download(INSTALLER.DESCRIPTOR_URL, hashlib.sha256(self.payload).hexdigest(), target,
                           len(self.payload), opener=downloads)
        self.assertEqual(target.read_bytes(), self.payload)
        self.assertFalse(target.with_name("verified.py.partial").exists())

    def test_checksum_or_size_failure_never_leaves_executable_download(self):
        for label, checksum, limit in [("checksum", "0" * 64, 1000),
                                        ("size", hashlib.sha256(self.payload).hexdigest(), len(self.payload) - 1)]:
            target = self.folder / (label + ".py")
            with self.subTest(label=label), self.assertRaises(INSTALLER.Refusal):
                INSTALLER.download(INSTALLER.DESCRIPTOR_URL, checksum, target, limit,
                                   opener=MemoryDownloads({INSTALLER.DESCRIPTOR_URL: self.payload}))
            self.assertFalse(target.exists())
            self.assertFalse(target.with_name(target.name + ".partial").exists())

    def test_existing_download_is_never_overwritten(self):
        target = self.folder / "existing.py"
        target.write_bytes(b"keep original bytes")
        downloads = MemoryDownloads({INSTALLER.DESCRIPTOR_URL: self.payload})
        with self.assertRaisesRegex(INSTALLER.Refusal, "already exists"):
            INSTALLER.download(INSTALLER.DESCRIPTOR_URL, "a" * 64, target, 1000, opener=downloads)
        self.assertEqual(target.read_bytes(), b"keep original bytes")
        self.assertFalse(downloads.requests)

    def test_redirects_reject_http_credentials_unexpected_hosts_and_nonstandard_ports(self):
        handler = INSTALLER.GithubHttpsRedirects()
        request = urllib.request.Request(INSTALLER.DESCRIPTOR_URL)
        for url in ["http://github.com/file", "https://example.com/file", "https://github.com:444/file",
                    "https://user:password@github.com/file", "https://github.com/file\nnext"]:
            with self.subTest(url=url), self.assertRaises(INSTALLER.Refusal):
                handler.redirect_request(request, None, 302, "Found", {}, url)
        target = handler.redirect_request(request, None, 302, "Found", {},
                                          "https://release-assets.githubusercontent.com/public/file?signature=public-download-token")
        self.assertTrue(target.full_url.startswith("https://release-assets.githubusercontent.com/"))

    def test_descriptor_refuses_different_source_repository_helper_set_or_room(self):
        for change in [lambda data: data.update(sourceRevision="0" * 40),
                       lambda data: data.update(endpoint="other-server:7777"),
                       lambda data: data["helpers"]["release.py"].update(url="https://example.com/release.py"),
                       lambda data: data["helpers"].update({"unexpected.py": {}})]:
            data = copy.deepcopy(self.data)
            change(data)
            self.descriptor_path.write_text(json.dumps(data))
            with self.assertRaises(INSTALLER.Refusal):
                INSTALLER.descriptor(self.descriptor_path)

    def test_duplicate_descriptor_keys_are_rejected(self):
        self.descriptor_path.write_text('{"format":1,"format":1}')
        with self.assertRaisesRegex(INSTALLER.Refusal, "Duplicate"):
            INSTALLER.descriptor(self.descriptor_path)

    @unittest.skipUnless(os.name == "posix", "The on-host installer requires Linux root")
    def test_nonroot_refused_before_tempfiles_download_or_service_work(self):
        with mock.patch.object(INSTALLER.os, "geteuid", return_value=1000), mock.patch.object(INSTALLER, "download") as download, mock.patch.object(INSTALLER.tempfile, "mkdtemp") as temporary, mock.patch.object(INSTALLER.subprocess, "run") as child:
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(INSTALLER.main([]), 1)
            download.assert_not_called()
            temporary.assert_not_called()
            child.assert_not_called()

    def diagnosis(self):
        return {"format": "ascendance-readonly-hello-diagnostic-1", "readOnly": True,
                "hello": {"responseReceived": True, "accepted": True, "code": "HELLO",
                          "protocolVersion": 1, "serverBuildVersion": "0.10.2c",
                          "requestIdMatches": True, "snapshotRoomMatchesExpected": True}}

    def exercise(self, apply, bad_archive=False, diagnosis=None, upgrade_code=0):
        descriptor_bytes = json.dumps(self.data).encode()
        routes = {INSTALLER.DESCRIPTOR_URL: descriptor_bytes}
        for entry in self.data["helpers"].values(): routes[entry["url"]] = self.payload
        for entry in self.data["server"].values(): routes[entry["url"]] = self.payload
        if bad_archive: routes[self.data["server"]["archive"]["url"]] = b"tampered server build"
        downloads = MemoryDownloads(routes)
        bootstrap = self.folder / "private-bootstrap"
        bootstrap.mkdir()
        output, errors = io.StringIO(), io.StringIO()
        def run(command, **kwargs):
            if Path(command[1]).name == "diagnose-existing.py":
                return SimpleNamespace(returncode=0, stdout=json.dumps(diagnosis or self.diagnosis()), stderr="PRIVATE-RAW-STDERR")
            return SimpleNamespace(returncode=upgrade_code, stdout=json.dumps({"status": "REFUSED" if upgrade_code else "STAGED_SECURE"}), stderr="PRIVATE-RAW-STDERR")
        with mock.patch.object(INSTALLER.sys, "platform", "linux"), mock.patch.object(INSTALLER.os, "geteuid", return_value=0, create=True), mock.patch.object(INSTALLER.os, "umask"), mock.patch.object(INSTALLER.tempfile, "mkdtemp", return_value=str(bootstrap)), mock.patch.object(INSTALLER, "DESCRIPTOR_SHA256", hashlib.sha256(descriptor_bytes).hexdigest()), mock.patch.object(INSTALLER.urllib.request, "build_opener", return_value=downloads), mock.patch.object(INSTALLER.subprocess, "run", side_effect=run) as child, contextlib.redirect_stdout(output), contextlib.redirect_stderr(errors):
            code = INSTALLER.main(["--apply"] if apply else [])
        return code, child, downloads, bootstrap, output.getvalue(), errors.getvalue()

    @unittest.skipUnless(os.name == "posix", "The on-host bootstrap requires Linux filesystem paths")
    def test_staging_downloads_only_verified_server_inputs_before_delegation(self):
        code, child, downloads, bootstrap, output, errors = self.exercise(False)
        self.assertEqual(code, 0)
        self.assertEqual(len(downloads.requests), 8)
        command = child.call_args.args[0]
        self.assertEqual(Path(command[1]), bootstrap / "upgrade-existing.py")
        self.assertNotIn("--apply", command)
        self.assertEqual(child.call_args.kwargs["stdin"], INSTALLER.subprocess.DEVNULL)
        self.assertFalse(child.call_args.kwargs.get("shell", False))
        self.assertEqual(json.loads(output)["status"], "STAGED_SECURE")
        self.assertIn("staging only", errors)
        self.assertEqual(bootstrap.stat().st_mode & 0o777, 0o700)
        self.assertFalse(any("windows" in url.lower() or "invite" in url.lower() for url in downloads.requests))

    @unittest.skipUnless(os.name == "posix", "The on-host bootstrap requires Linux filesystem paths")
    def test_activation_is_forwarded_only_when_explicitly_requested(self):
        code, child, _, _, _, errors = self.exercise(True)
        self.assertEqual(code, 0)
        self.assertIn("--apply", child.call_args.args[0])
        self.assertEqual(Path(child.call_args_list[0].args[0][1]).name, "diagnose-existing.py")
        self.assertIn("isolated canary, backup and recovery", errors)

    @unittest.skipUnless(os.name == "posix", "The on-host bootstrap requires Linux filesystem paths")
    def test_tampered_server_never_invokes_any_service_or_upgrader(self):
        code, child, _, _, output, _ = self.exercise(True, bad_archive=True)
        self.assertEqual(code, 1)
        self.assertEqual(child.call_count, 1)  # Read-only probe only; no upgrader.
        self.assertEqual(Path(child.call_args.args[0][1]).name, "diagnose-existing.py")
        self.assertEqual(json.loads(output)["status"], "REFUSED")
        self.assertIn("checksum mismatch", json.loads(output)["message"])

    def test_case_drift_reports_diagnostic_without_game_download_canary_or_upgrade(self):
        diagnosis = self.diagnosis()
        diagnosis["hello"].update(snapshotRoomMatchesExpected=False, snapshotRoomMatchesAsciiCaseInsensitive=True)
        code, child, downloads, _, output, errors = self.exercise(True, diagnosis=diagnosis)
        self.assertEqual(code, 1)
        result = json.loads(output)
        self.assertEqual(result["failedIdentityChecks"], ["snapshotRoomMatchesExpected"])
        self.assertFalse(result["productionServiceChanged"])
        self.assertEqual(result["diagnostic"], diagnosis)
        self.assertEqual(child.call_count, 1)
        self.assertFalse(any(url.endswith(".zip") for url in downloads.requests))
        self.assertNotIn("PRIVATE-RAW-STDERR", output + errors)

    def test_unknown_build_protocol_and_correlation_are_never_waived(self):
        for key, value in [("serverBuildVersion", "0.8.62b"), ("protocolVersion", True),
                           ("requestIdMatches", False), ("code", "OTHER"), ("accepted", False)]:
            report = self.diagnosis()
            report["hello"][key] = value
            with self.subTest(key=key):
                self.assertIn(key, INSTALLER.identity_blockers(report))

    def test_observed_live_0102g_is_not_silently_downgraded_to_candidate_c(self):
        report = json.loads((ROOT / "tests/fixtures/observed-0102g-hello-metadata.json").read_text())
        self.assertEqual(report["hello"]["serverBuildVersion"], "0.10.2g")
        self.assertTrue(report["hello"]["snapshotRoomMatchesExpected"])
        self.assertIsNone(report["hello"]["transient"]["HeroMovementCount"])
        code, child, downloads, _, output, _ = self.exercise(True, diagnosis=report)
        result = json.loads(output)
        self.assertEqual(code, 1)
        self.assertEqual(result["failedIdentityChecks"], ["serverBuildVersion"])
        self.assertFalse(result["productionServiceChanged"])
        self.assertEqual(result["diagnostic"], report)
        self.assertEqual(child.call_count, 1)
        self.assertEqual(Path(child.call_args.args[0][1]).name, "diagnose-existing.py")
        self.assertFalse(any(url.endswith(".zip") for url in downloads.requests))

    def test_unavailable_plaintext_probe_defers_to_authenticated_upgrader(self):
        report = {"readOnly": True, "format": "ascendance-readonly-hello-diagnostic-1",
                  "hello": {"responseReceived": False, "error": "TimeoutError"}}
        code, child, _, _, output, _ = self.exercise(True, diagnosis=report)
        self.assertEqual(code, 0)
        self.assertEqual(child.call_count, 2)
        self.assertNotIn("diagnostic", json.loads(output))

    def test_later_upgrade_refusal_includes_redacted_probe_automatically(self):
        code, child, _, _, output, errors = self.exercise(True, upgrade_code=1)
        self.assertEqual(code, 1)
        self.assertEqual(json.loads(output)["diagnostic"], self.diagnosis())
        self.assertEqual(child.call_count, 2)
        self.assertNotIn("PRIVATE-RAW-STDERR", output + errors)

    def test_invalid_diagnostic_output_is_not_echoed(self):
        with mock.patch.object(INSTALLER.subprocess, "run", return_value=SimpleNamespace(stdout="PRIVATE malformed raw response")):
            result = INSTALLER.inspect_current(self.folder)
        self.assertFalse(result["diagnosticAvailable"])
        self.assertNotIn("PRIVATE", json.dumps(result))


if __name__ == "__main__":
    unittest.main()
