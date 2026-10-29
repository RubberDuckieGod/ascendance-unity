"""Helper tests only: synthetic verdicts never count as Unity execution."""
import copy
import importlib.util
import json
import os
import shutil
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('unity_verdict', ROOT / 'tools/verify_unity_validation.py')
verdict = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verdict)


def fixture():
    result = dict(passed=True, reason='Synthetic helper fixture', editorVersion='6000.6.5f1', buildVersion='0.10.2h', errorCount=0, errors=[], gates=[])
    for name, count in verdict.EXPECTED.items():
        if name == 'snapshot':
            terminal = f'[ASCENDANCE TEST 0.10.2 SNAPSHOT] PASS — {count} checks.'
        else:
            prefix, label = verdict.TERMINALS[name]
            terminal = f'{prefix} PASS — {label} completed: {count} passed, 0 failed.'
        result['gates'].append(dict(name=name, passed=True, expectedChecks=count, observedChecks=count, terminalMessage=terminal))
    return result


class VerdictTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / 'verdict.json'

    def verify(self, result):
        self.path.write_text(json.dumps(result), encoding='utf-8')
        return verdict.verify(self.path)

    def test_complete_synthetic_helper_fixture(self):
        self.assertEqual(self.verify(fixture())['reason'], 'Synthetic helper fixture')

    def test_each_required_gate_is_mandatory(self):
        for index in range(6):
            data = fixture(); del data['gates'][index]
            with self.subTest(index=index), self.assertRaises(ValueError): self.verify(data)

    def test_duplicate_unknown_and_wrong_counts_are_rejected(self):
        for change in ('duplicate', 'unknown', 'count', 'boolean_count', 'expected_count'):
            data = fixture()
            if change == 'duplicate': data['gates'][1] = copy.deepcopy(data['gates'][0])
            elif change == 'unknown': data['gates'][0]['name'] = 'unknown'
            elif change == 'count': data['gates'][0]['observedChecks'] = 1
            elif change == 'boolean_count': data['gates'][0]['observedChecks'] = True
            else: data['gates'][0]['expectedChecks'] = 0
            with self.subTest(change=change), self.assertRaises(ValueError): self.verify(data)

    def test_error_and_failure_cannot_be_hidden_by_pass_flag(self):
        for field, value in [('passed', False), ('errorCount', 1), ('errorCount', False), ('errors', ['Exception']), ('editorVersion', '6000.0.1f1'), ('buildVersion', 'wrong'), ('buildVersion', '0.10.2c'), ('buildVersion', '0.10.2g')]:
            data = fixture(); data[field] = value
            with self.subTest(field=field, value=value), self.assertRaises(ValueError): self.verify(data)

    def test_terminal_must_name_correct_gate_count_and_success(self):
        for terminal in ['', 'PASS', '[ASCENDANCE][0.10.2h TEST] FAIL', fixture()['gates'][1]['terminalMessage']]:
            data = fixture(); data['gates'][0]['terminalMessage'] = terminal
            with self.subTest(terminal=terminal), self.assertRaises(ValueError): self.verify(data)

    def test_nonobjects_and_duplicate_json_are_rejected(self):
        for text in ['[]', 'null', '{"passed":false,"passed":true}', '{"gates":{}}']:
            self.path.write_text(text)
            with self.subTest(text=text), self.assertRaises(ValueError): verdict.verify(self.path)

    def test_cli_missing_or_malformed_results_fail(self):
        for content in (None, '{'):
            if content is not None: self.path.write_text(content)
            process = subprocess.run(['python3', str(ROOT / 'tools/verify_unity_validation.py'), str(self.path)], capture_output=True)
            self.assertNotEqual(process.returncode, 0)


@unittest.skipUnless(os.name == 'posix', 'Bash wrapper checks require POSIX')
class BashWrapperTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='asc wrapper ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.editor = self.root / 'fake-editor'
        self.output = self.root / 'output'

    def run_wrapper(self, body, *extra):
        self.editor.write_text('#!/usr/bin/env python3\n' + body)
        self.editor.chmod(0o700)
        return subprocess.run(['bash', str(ROOT / 'tools/run_unity_validation.sh'), '--editor', str(self.editor), '--artifacts', str(self.output), *extra], capture_output=True, text=True, timeout=10)

    def test_successful_process_without_runtime_verdict_fails(self):
        # A process exit 0 must never stand in for gameplay assertions.
        result = self.run_wrapper("import sys\nif '-version' in sys.argv: print('6000.6.5f1')\n")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('result rejected', result.stderr)

    def test_editor_version_mismatch_prevents_runtime_execution(self):
        result = self.run_wrapper("print('6000.0.1f1')\n")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('version check failed', result.stderr)
        self.assertFalse((self.output / 'game-data').exists())

    def test_runtime_exit_failure_prevents_builds(self):
        result = self.run_wrapper("import sys\nif '-version' in sys.argv: print('6000.6.5f1')\nelse: sys.exit(7)\n", '--build-linux-server')
        self.assertEqual(result.returncode, 7)
        self.assertFalse((self.output / 'LinuxServer').exists())

    def test_existing_artifact_directory_refused(self):
        self.output.mkdir()
        result = self.run_wrapper("raise RuntimeError('must not run')\n")
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.output / 'editor-version.txt').exists())

    def test_invalid_deadline_and_missing_editor_fail_early(self):
        result = self.run_wrapper("raise RuntimeError('must not run')\n", '--timeout', '0')
        self.assertEqual(result.returncode, 2)
        self.assertFalse(self.output.exists())
        result = subprocess.run(['bash', str(ROOT / 'tools/run_unity_validation.sh'), '--editor', '/nonexistent/unity'], capture_output=True)
        self.assertEqual(result.returncode, 2)


@unittest.skipUnless(os.environ.get('POWERSHELL_PATH') or shutil.which('pwsh'), 'PowerShell is not installed')
class PowerShellWrapperTests(BashWrapperTests):
    def run_wrapper(self, body, *extra):
        self.editor.write_text('#!/usr/bin/env python3\n' + body)
        self.editor.chmod(0o700)
        # Windows handles process invocation natively; on POSIX this checks the same
        # PowerShell control flow with an executable helper, not a Unity substitute.
        translated = []
        names = {'--build-linux-server': '-BuildLinuxServer', '--timeout': '-TimeoutSeconds'}
        for arg in extra: translated.append(names.get(arg, arg))
        return subprocess.run([os.environ.get('POWERSHELL_PATH') or shutil.which('pwsh'), '-NoLogo', '-NoProfile', '-File',
            str(ROOT / 'tools/Run-UnityValidation.ps1'), '-Editor', str(self.editor), '-Artifacts', str(self.output), *translated],
            capture_output=True, text=True, timeout=15)

    def test_successful_process_without_runtime_verdict_fails(self):
        result = self.run_wrapper("import sys\nif '-version' in sys.argv: print('6000.6.5f1')\n")
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.output / 'LinuxServer').exists())
        self.assertIn('validation-results.json', result.stderr)

    def test_editor_version_mismatch_prevents_runtime_execution(self):
        result = self.run_wrapper("print('6000.0.1f1')\n")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('not Unity 6000.6.5f1', result.stderr)
        self.assertFalse((self.output / 'game-data').exists())

    def test_runtime_exit_failure_prevents_builds(self):
        result = self.run_wrapper("import sys\nif '-version' in sys.argv: print('6000.6.5f1')\nelse: sys.exit(7)\n", '--build-linux-server')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('exit 7', result.stderr)
        self.assertFalse((self.output / 'LinuxServer').exists())

    def test_invalid_deadline_and_missing_editor_fail_early(self):
        result = self.run_wrapper("raise RuntimeError('must not run')\n", '--timeout', '0')
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(self.output.exists())


if __name__ == '__main__':
    unittest.main()
