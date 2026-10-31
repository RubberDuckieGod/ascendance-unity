"""Validation helper tests; synthetic verdicts never demonstrate Unity/art passes."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('location_art_verdict', ROOT / 'tools/verify_unity_validation.py')
verdict = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verdict)

BASE_COUNTS = {
    'master': 54, 'foundation': 24, 'router': 30, 'network': 27,
    'reliability': 22, 'snapshot': 20, 'lobby': 32, 'campaign': 42,
    'reaction': 30, 'combatHud': 11, 'villagers': 67, 'locationArt': 47,
}
TERMINALS = {
    'master': ('[ASCENDANCE][0.10.2h TEST]', 'Master Save/Load + Full Regression'),
    'foundation': ('[ASCENDANCE][0.10.2h SERVER]', 'Dedicated Server Authority Foundation'),
    'router': ('[ASCENDANCE][0.10.2h ROUTER]', 'Client Command Routing'),
    'network': ('[ASCENDANCE][0.10.2h NETWORK]', 'Real Network Transport'),
    'reliability': ('[ASCENDANCE][0.10.2h RELIABILITY]', 'Network Reliability'),
    'lobby': ('[ASCENDANCE][ONLINE LOBBY]', 'Online Hero Lobby'),
    'campaign': ('[ASCENDANCE][ONLINE CAMPAIGN]', 'Online Campaign Setup'),
    'reaction': ('[ASCENDANCE][REACTION AUTHORITY]', 'Reaction Authority'),
    'combatHud': ('[ASCENDANCE][COMBAT HUD]', 'Combat HUD'),
    'villagers': ('[ASCENDANCE][Q001 VILLAGERS]', 'Villager gameplay regression'),
    'locationArt': ('[ASCENDANCE][LOCATION ART]', 'Location Art Framework'),
    'installedArt': ('[ASCENDANCE][LOCATION ART]', 'Installed Location Art Bundle'),
    'allLocationArt': ('[ASCENDANCE][LOCATION ART]', 'All Location Art Bindings'),
}


def gate(name, count):
    if name == 'snapshot':
        terminal = f'[ASCENDANCE TEST 0.10.2 SNAPSHOT] PASS — {count} checks.'
    else:
        prefix, label = TERMINALS[name]
        terminal = f'{prefix} PASS — {label} completed: {count} passed, 0 failed.'
    return dict(name=name, passed=True, expectedChecks=count, observedChecks=count, terminalMessage=terminal)


def fixture(installed=False, all_locations=False, digest=''):
    installed = installed or all_locations
    counts = dict(BASE_COUNTS)
    if installed:
        counts['installedArt'] = 10
    if all_locations:
        counts['allLocationArt'] = 236
    return dict(passed=True, reason='Synthetic validation-helper fixture only',
                editorVersion='6000.6.5f1', buildVersion='0.10.2h', errorCount=0, errors=[],
                requireInstalledArt=installed, requireAllLocationArt=all_locations,
                artManifestSha256=digest if installed else '',
                gates=[gate(name, count) for name, count in counts.items()])


class ArtVerdictFlagTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='art verdict ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.path = self.root / 'verdict.json'
        self.manifest = self.root / 'client art.json'
        self.manifest.write_text('{"helperFixture":true}\n', encoding='utf-8')
        self.digest = hashlib.sha256(self.manifest.read_bytes()).hexdigest()

    def verify(self, result, installed=False, all_locations=False):
        self.path.write_text(json.dumps(result), encoding='utf-8')
        return verdict.verify(self.path, require_installed_art=installed,
                              require_all_location_art=all_locations, art_manifest=self.manifest)

    def test_baseline_always_requires_framework_and_all_core_gates(self):
        self.assertEqual(sum(count for name, count in BASE_COUNTS.items() if name != 'locationArt'), 359)
        self.verify(fixture())
        data = fixture(); data['gates'] = [item for item in data['gates'] if item['name'] != 'locationArt']
        with self.assertRaises(ValueError):
            self.verify(data)

    def test_installed_and_all_modes_accept_only_their_declared_gates(self):
        self.verify(fixture(installed=True, digest=self.digest), installed=True)
        self.verify(fixture(all_locations=True, digest=self.digest), all_locations=True)
        for data, installed, all_locations in [
            (fixture(), True, False),
            (fixture(installed=True, digest=self.digest), False, False),
            (fixture(installed=True, digest=self.digest), False, True),
            (fixture(all_locations=True, digest=self.digest), True, False),
        ]:
            with self.subTest(installed=installed, all_locations=all_locations), self.assertRaises(ValueError):
                self.verify(data, installed, all_locations)

    def test_mode_flags_and_manifest_digest_fail_closed(self):
        changes = [('requireInstalledArt', 'true'), ('requireAllLocationArt', 0),
                   ('requireInstalledArt', False), ('artManifestSha256', ''),
                   ('artManifestSha256', '0' * 64), ('artManifestSha256', True)]
        for field, value in changes:
            data = fixture(installed=True, digest=self.digest); data[field] = value
            with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                self.verify(data, installed=True)
        data = fixture(); data['artManifestSha256'] = self.digest
        with self.assertRaises(ValueError):
            self.verify(data)

    def test_optional_gate_counts_and_terminal_are_checked(self):
        for field, value in [('observedChecks', True), ('expectedChecks', '10'), ('expectedChecks', 10.0),
                             ('observedChecks', 9), ('terminalMessage', 'PASS'),
                             ('terminalMessage', gate('locationArt', 47)['terminalMessage'])]:
            data = fixture(installed=True, digest=self.digest); data['gates'][-1][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                self.verify(data, installed=True)
        data = fixture(all_locations=True, digest=self.digest)
        data['gates'][-1]['observedChecks'] = 235
        with self.assertRaises(ValueError):
            self.verify(data, all_locations=True)

    def test_cli_all_mode_accepts_an_explicit_manifest_path(self):
        self.path.write_text(json.dumps(fixture(all_locations=True, digest=self.digest)), encoding='utf-8')
        env = os.environ.copy(); env.pop('ASCENDANCE_ART_MANIFEST', None)
        result = subprocess.run(['python3', str(ROOT / 'tools/verify_unity_validation.py'), str(self.path),
                                 '--require-all-location-art', '--art-manifest', str(self.manifest)],
                                env=env, capture_output=True, text=True, timeout=10)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('allLocationArt=236', result.stdout)


POWERSHELL = (os.environ.get('POWERSHELL_PATH') or shutil.which('pwsh')
              or ('/workspace/.unity-tools/powershell/pwsh'
                  if Path('/workspace/.unity-tools/powershell/pwsh').is_file() else None))


@unittest.skipUnless(os.name == 'posix' and POWERSHELL, 'Executable helper tests require POSIX and PowerShell')
class PowerShellArtFlagTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='art wrapper ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.manifest = self.root / 'private client art.json'
        self.manifest.write_text('{"helperFixture":true}\n', encoding='utf-8')
        self.digest = hashlib.sha256(self.manifest.read_bytes()).hexdigest()
        self.editor = self.root / 'editor helper'
        self.editor.write_text('''#!/usr/bin/env python3
import json, os, pathlib, sys
with open(os.environ['HELPER_TRACE'], 'a') as trace:
    trace.write(json.dumps({'args': sys.argv[1:], 'manifest': os.environ.get('ASCENDANCE_ART_MANIFEST'), 'expectedHash': os.environ.get('ASCENDANCE_ART_EXPECTED_MANIFEST_SHA256')}) + '\\n')
if '-version' in sys.argv:
    print('6000.6.5f1')
    if os.environ.get('HELPER_CHANGE_MANIFEST'):
        pathlib.Path(os.environ['ASCENDANCE_ART_MANIFEST']).write_text(os.environ['HELPER_CHANGE_MANIFEST'])
else:
    path = pathlib.Path(sys.argv[sys.argv.index('-ascendance-results') + 1])
    path.write_text(pathlib.Path(os.environ['HELPER_VERDICT']).read_text())
''', encoding='utf-8')
        self.editor.chmod(0o700)
        self.run_number = 0

    def run_wrapper(self, data, *switches, manifest='existing', change_manifest=None):
        self.run_number += 1
        case = self.root / f'case-{self.run_number}'
        case.mkdir()
        self.output = case / 'output'
        self.trace = case / 'trace.jsonl'
        source = case / 'synthetic-verdict.json'
        source.write_text(json.dumps(data, ensure_ascii=False), encoding='utf-8')
        env = os.environ.copy()
        env.update(HELPER_TRACE=str(self.trace), HELPER_VERDICT=str(source),
                   XDG_CACHE_HOME=str(self.root / 'cache'), XDG_CONFIG_HOME=str(self.root / 'config'),
                   XDG_DATA_HOME=str(self.root / 'share'), ASCENDANCE_ART_EXPECTED_MANIFEST_SHA256='stale helper parent value')
        env.pop('ASCENDANCE_ART_MANIFEST', None)
        env.pop('HELPER_CHANGE_MANIFEST', None)
        if change_manifest is not None:
            env['HELPER_CHANGE_MANIFEST'] = change_manifest
        if manifest == 'existing':
            env['ASCENDANCE_ART_MANIFEST'] = str(self.manifest)
        elif manifest is not None:
            env['ASCENDANCE_ART_MANIFEST'] = str(manifest)
        return subprocess.run([POWERSHELL, '-NoLogo', '-NoProfile', '-File',
                               str(ROOT / 'tools/Run-UnityValidation.ps1'), '-Editor', str(self.editor),
                               '-Artifacts', str(self.output), *switches],
                              env=env, capture_output=True, text=True, timeout=20)

    def calls(self):
        return [json.loads(line) for line in self.trace.read_text().splitlines()]

    def test_baseline_handoff_has_no_optional_flags(self):
        result = self.run_wrapper(fixture())
        self.assertEqual(result.returncode, 0, result.stderr)
        runtime = self.calls()[1]['args']
        self.assertNotIn('-ascendance-require-installed-art', runtime)
        self.assertNotIn('-ascendance-require-all-location-art', runtime)
        self.assertIsNone(self.calls()[1]['expectedHash'])
        self.assertIn('All 12 Unity runtime gates', result.stdout)

    def test_installed_flag_handoff_and_actual_manifest_hash(self):
        result = self.run_wrapper(fixture(installed=True, digest=self.digest), '-RequireInstalledArt')
        self.assertEqual(result.returncode, 0, result.stderr)
        runtime = self.calls()[1]
        self.assertIn('-ascendance-require-installed-art', runtime['args'])
        self.assertNotIn('-ascendance-require-all-location-art', runtime['args'])
        self.assertEqual(runtime['manifest'], str(self.manifest))
        self.assertEqual(runtime['expectedHash'], self.digest)

    def test_all_flag_also_requires_installed_art(self):
        result = self.run_wrapper(fixture(all_locations=True, digest=self.digest), '-RequireAllLocationArt')
        self.assertEqual(result.returncode, 0, result.stderr)
        runtime = self.calls()[1]['args']
        self.assertIn('-ascendance-require-installed-art', runtime)
        self.assertIn('-ascendance-require-all-location-art', runtime)
        self.assertIn('All 14 Unity runtime gates', result.stdout)

    def test_manifest_hash_is_captured_before_editor_invocation(self):
        changed = '{"helperFixture":"changed by subprocess"}\n'
        changed_digest = hashlib.sha256(changed.encode()).hexdigest()
        result = self.run_wrapper(fixture(installed=True, digest=changed_digest),
                                  '-RequireInstalledArt', change_manifest=changed)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('SHA-256', result.stderr)
        self.assertEqual(self.manifest.read_text(), changed)

    def test_required_manifest_is_checked_before_any_editor_invocation(self):
        for manifest in (None, 'relative-art.json', self.root / 'absent.json', self.root):
            with self.subTest(manifest=manifest):
                result = self.run_wrapper(fixture(), '-RequireInstalledArt', manifest=manifest)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn('ASCENDANCE_ART_MANIFEST', result.stderr)
                self.assertFalse(self.trace.exists())
                self.assertFalse(self.output.exists())

    def test_modes_digest_and_optional_gate_cannot_be_forged(self):
        cases = []
        for field, value in [('requireInstalledArt', 'true'), ('requireAllLocationArt', 0),
                             ('artManifestSha256', '0' * 64), ('artManifestSha256', '')]:
            data = fixture(installed=True, digest=self.digest); data[field] = value
            cases.append(data)
        data = fixture(installed=True, digest=self.digest); data['gates'].pop(); cases.append(data)
        data = fixture(installed=True, digest=self.digest); data['gates'][-1]['observedChecks'] = True; cases.append(data)
        data = fixture(installed=True, digest=self.digest); data['gates'][-1]['terminalMessage'] = 'PASS'; cases.append(data)
        for index, data in enumerate(cases):
            with self.subTest(case=index):
                result = self.run_wrapper(data, '-RequireInstalledArt', '-BuildLinuxServer')
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(len(self.calls()), 2)
                self.assertFalse((self.output / 'LinuxServer').exists())

    def test_baseline_rejects_optional_evidence_and_incomplete_core_verdicts(self):
        cases = [fixture(installed=True, digest=self.digest)]
        data = fixture(); data['artManifestSha256'] = self.digest; cases.append(data)
        data = fixture(); data['gates'].pop(); cases.append(data)
        data = fixture(); data['gates'][0]['name'] = 'MASTER'; cases.append(data)
        data = fixture(); data['gates'][0]['observedChecks'] = '54'; cases.append(data)
        data = fixture(); data['errors'] = ['Exception']; cases.append(data)
        data = fixture(); del data['requireInstalledArt']; cases.append(data)
        for index, data in enumerate(cases):
            with self.subTest(case=index):
                result = self.run_wrapper(data)
                self.assertNotEqual(result.returncode, 0)


if __name__ == '__main__':
    unittest.main()
