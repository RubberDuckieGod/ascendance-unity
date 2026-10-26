#!/usr/bin/env python3
"""Exercise installer HELLO compatibility on an isolated preserved native server.

No remote host, existing save, JOIN, command, account or production service is
accepted. The case-variant check follows the server's OrdinalIgnoreCase routing.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import tempfile
import uuid

ROOT = Path(__file__).resolve().parents[1]


def load(name, relative):
    spec = importlib.util.spec_from_file_location(name, ROOT / relative)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def run(build, manifest, output):
    native = load('native_legacy_wire_support', 'tools/native_server_acceptance.py')
    upgrade = load('native_legacy_installer_support', 'Deploy/DigitalOcean/upgrade-existing.py')
    release = load('native_legacy_manifest_support', 'Deploy/DigitalOcean/release.py')
    release.verify_artifact(build, release.read_json(manifest))
    report = {'scope': 'Preserved compiled native server on loopback with synthetic temporary XDG data; HELLO only',
              'checks': [], 'noProductionConnection': True}
    client = server = None
    with tempfile.TemporaryDirectory(prefix='asc-native-legacy-compatibility-') as directory:
        room = 'LEGACY-COMPAT-' + uuid.uuid4().hex.upper()
        try:
            server = native.NativeServer(build, Path(directory), room)
            client, first = server.start()
            for label, requested_room, expected_accept in [('exact room', room, True), ('case variant of same room', room.lower(), True), ('different room', room+'-WRONG', False)]:
                response = client.exchange('HELLO', RoomId=requested_room)
                snapshot = response.get('Snapshot') or {}
                metadata = {'accepted': response.get('Accepted'), 'code': response.get('Code'),
                            'buildVersion': response.get('ServerBuildVersion'), 'protocolVersion': response.get('ProtocolVersion'),
                            'exactRoomMatches': snapshot.get('RoomId') == requested_room,
                            'caseInsensitiveRoomMatches': str(snapshot.get('RoomId') or '').casefold() == requested_room.casefold()}
                protocol_passed = metadata['accepted'] is expected_accept and metadata['code'] == ('HELLO' if expected_accept else 'ROOM_MISMATCH')
                report['checks'].append({'name': 'Native minimal HELLO: '+label,
                                         'expected': {'accepted': expected_accept, 'code': 'HELLO' if expected_accept else 'ROOM_MISMATCH'},
                                         'actual': metadata, 'passed': protocol_passed})
                helper_accepted = False
                refusal = None
                try:
                    upgrade.legacy_hello(server.port, requested_room)
                    helper_accepted = True
                except upgrade.Refusal as error:
                    refusal = str(error)
                report['checks'].append({'name': 'Exact installer legacy_hello: '+label,
                                         'expected': {'accepted': expected_accept},
                                         'actual': {'accepted': helper_accepted, 'refusal': refusal},
                                         'passed': helper_accepted is expected_accept})
            report['status'] = 'PASS' if all(c['passed'] for c in report['checks']) else 'FAIL'
        except Exception as error:
            report['status'] = 'FAIL'
            report['error'] = type(error).__name__ + ': ' + str(error)
        finally:
            if client:
                client.close()
            if server:
                server.stop()
    report['passed'] = sum(c['passed'] for c in report['checks'])
    report['failed'] = sum(not c['passed'] for c in report['checks'])
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2)+'\n')
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build-dir', type=Path, required=True)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = run(args.build_dir.resolve(), args.manifest.resolve(), args.output.resolve())
    print(json.dumps({k: result.get(k) for k in ['status', 'passed', 'failed', 'error']}))
    return 0 if result.get('status') == 'PASS' else 1


if __name__ == '__main__':
    raise SystemExit(main())
