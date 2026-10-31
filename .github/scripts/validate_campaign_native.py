#!/usr/bin/env python3
"""Extract reviewed public builds and collect only public native test results."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import zipfile

ROOT = Path(__file__).resolve().parents[2]
SOURCE = '597f44215ec2624ff0f215db80c6c487f05a84d7'
PUBLIC_RESULTS = {'results.json', 'campaign-full-sixteen-roster.png',
                  'campaign-two-ready-nondefault-heroes.png', 'campaign-fresh-two-nondefault-worlds.png'}


def require(ok, message):
    if not ok: raise SystemExit(message)


def sha(data): return hashlib.sha256(data).hexdigest()


def unique(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate JSON field is refused')
        result[key] = value
    return result


def load(data): return json.loads(data, object_pairs_hook=unique)


def public_module():
    spec = importlib.util.spec_from_file_location('packaging', ROOT/'tools/package_multiplayer_release.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def inventory():
    path = ROOT/'validation/full-roster-597f442/inventory.json'
    data = path.read_bytes()
    trusted = os.environ.get('REVIEWED_VALIDATION_INVENTORY_SHA256', '')
    require(re.fullmatch('[0-9a-f]{64}', trusted) and sha(data) == trusted, 'Validation input inventory is not frozen')
    document = load(data)
    require(document.get('format') == 1 and document.get('sourceRevision') == SOURCE
            and document.get('expectedChecks') == 47, 'Validation runtime identity or expected checks differ')
    require(set(document.get('files', {})) == {'linux-server.zip', 'linux-development-client.zip',
                                               'linux-server-manifest.json', 'linux-development-manifest.json'},
            'Validation payload inventory differs')
    public = public_module()
    for name, proof in document['files'].items():
        path = ROOT/'validation/full-roster-597f442'/name
        require(path.is_file() and not path.is_symlink(), 'Validation payload must be a regular file')
        content = path.read_bytes()
        require(len(content) == proof.get('bytes') and len(content) < 95*1024*1024
                and sha(content) == proof.get('sha256'), 'Validation payload checksum or size differs')
        public.check_public_file(name, content)
    return document, public


def prepare(destination):
    document, public = inventory()
    require(not destination.exists(), 'Private native input destination must be absent')
    destination.mkdir(mode=0o700)
    for key, prefix, executable, proof_name in [
        ('server', 'LinuxServer', 'AscendanceServer.x86_64', 'AscendanceServer.provenance.json'),
        ('development', 'LinuxClient', 'AscendanceClient.x86_64', 'AscendanceClient.provenance.json'),
    ]:
        archive_name = 'linux-server.zip' if key == 'server' else 'linux-development-client.zip'
        manifest_name = 'linux-server-manifest.json' if key == 'server' else 'linux-development-manifest.json'
        base = ROOT/'validation/full-roster-597f442'
        manifest = load((base/manifest_name).read_bytes())
        require(manifest.get('format') == 1 and manifest.get('files'), 'Complete validation manifest missing')
        contents, folded = {}, set()
        with zipfile.ZipFile(base/archive_name) as archive:
            entries = archive.infolist()
            require(0 < len(entries) <= 5000 and sum(e.file_size for e in entries) <= 512*1024*1024,
                    'Validation archive expanded size or file count exceeds the reviewed boundary')
            for entry in entries:
                name, pure = entry.filename, PurePosixPath(entry.filename)
                mode = (entry.external_attr >> 16) & 0o170000
                require(not entry.is_dir() and mode in (0, stat.S_IFREG) and not entry.flag_bits & 1
                        and name.startswith(prefix+'/') and pure.as_posix() == name and '..' not in pure.parts
                        and not pure.is_absolute() and '\\' not in name and ':' not in name
                        and name not in contents and name.casefold() not in folded,
                        'Validation archive contains an unsafe, duplicate or nonregular entry')
                data = archive.read(entry); public.check_public_file(name, data)
                contents[name] = data; folded.add(name.casefold())
        expected = {prefix+'/'+name: checksum for name,checksum in manifest['files'].items()}
        require(set(contents) == set(expected) and all(sha(contents[n]) == checksum for n,checksum in expected.items()),
                'Validation archive differs from complete per-file manifest')
        proof = load(contents.get(prefix+'/'+proof_name, b'{}'))
        require(proof.get('schemaVersion') == 1 and proof.get('outcome') == 'success' and proof.get('exitCode') == 0
                and proof.get('build', {}).get('target') == 'StandaloneLinux64'
                and proof.get('source') == {'vcs':'git','revision':SOURCE,'dirty':False},
                'Validation archive is not the exact reviewed clean runtime')
        header = contents.get(prefix+'/'+executable, b'')[:20]
        require(len(header) == 20 and header[:6] == b'\x7fELF\x02\x01' and header[18:20] == b'\x3e\x00'
                and prefix+'/UnityPlayer.so' in contents, 'Complete native x86-64 Unity player is missing')
        for name,data in contents.items():
            path = destination/name
            path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
            path.write_bytes(data); path.chmod(0o700 if name.endswith('/'+executable) else 0o600)
    print(json.dumps({'status':'VERIFIED_EXTRACTED_PRIVATE_INPUTS','sourceRevision':SOURCE,
                      'expectedChecks':document['expectedChecks']}))


def collect(source, destination):
    _, public = inventory()
    require(not destination.exists(), 'Public output directory must be absent')
    destination.mkdir(mode=0o700)
    for name in sorted(PUBLIC_RESULTS):
        path = source/name
        if not path.exists(): continue
        require(path.is_file() and not path.is_symlink(), 'Public report must be a regular file')
        data = path.read_bytes(); public.check_public_file(name, data)
        if name.endswith('.json'): load(data)
        else: require(data.startswith(b'\x89PNG\r\n\x1a\n'), 'Public screenshot is not PNG')
        (destination/name).write_bytes(data)
    print(json.dumps({'collectedPublicFiles': sorted(p.name for p in destination.iterdir()), 'privateFilesCollected':False}))


def verify_result(source):
    document, _ = inventory()
    result = load((source/'results.json').read_bytes())
    require(result.get('kind') == 'actual-native-full-roster-online-campaign'
            and result.get('transport') == 'pinned-TLS-and-room-key' and result.get('failed') == 0
            and result.get('passed') == document['expectedChecks'] and len(result.get('checks', [])) == document['expectedChecks']
            and all(c.get('passed') is True and c.get('actual') == c.get('expected') for c in result['checks']),
            'Actual graphical campaign acceptance did not complete all 47 semantic checks')
    require({p.name for p in source.iterdir()} == PUBLIC_RESULTS, 'Successful graphical run must include all three screenshots')
    print(json.dumps({'status':'PASS','passed':47,'failed':0,'sourceRevision':SOURCE,'deployed':False}))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--prepare',type=Path)
    group.add_argument('--collect',type=Path)
    group.add_argument('--verify-result',type=Path)
    parser.add_argument('--public-output',type=Path)
    args=parser.parse_args()
    if args.prepare: prepare(args.prepare)
    elif args.collect:
        require(args.public_output is not None, 'Collection needs a public output directory')
        collect(args.collect,args.public_output)
    else: verify_result(args.verify_result)
