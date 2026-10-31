#!/usr/bin/env python3
"""Synthetic native uploaded-g -> candidate-h -> g compatibility acceptance.

All saves are generated in newly created temporary XDG directories. The uploaded
legacy assembly runs with a verified local Unity runtime and selected base assets: this is a
mixed-provenance fixture, not certification of original host assets. No supplied
campaign/save directory or remote hostname is accepted.
"""
from pathlib import Path
import argparse, copy, hashlib, importlib.util, json, os, shutil, socket, sys, tempfile, time, uuid
REPO = Path(__file__).resolve().parents[1]
G_SHA = 'f25f2849e76eb3335f8c06dd7d5f339f6a9655b3a94ee0f2aabc40262a0baedd'

def load(name, path):
    s = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(s)
    s.loader.exec_module(m)
    return m
native = load('g_h_native_support', REPO / 'tools/native_server_acceptance.py')
release = load('g_h_release_support', REPO / 'Deploy/DigitalOcean/release.py')
UNITY_PLAYER_SHA256 = '8d5a32f46f562cb89cbd1f72a4c6cf9ce1fa792dd84edd175c1c784caf43f74d'
KNOWN_MANAGED_RUNTIME_SHA256 = {'netstandard': 'bf0b7eac9010b75413e4ec1a07a3453bc6a68eeb8e916fbed7c1f2777841f7c7',
 'UnityEngine.CoreModule': 'a42b0321162e90c106bbfe16b318f8050976db8062dc91406714c6aa77813d51',
 'UnityEngine.PhysicsModule': '0081c519c40f75e5b43edbd3682ce4e4f887728be033f7a03537b2daab90bb03',
 'UnityEngine.AIModule': '3fdca0ff2b89a7aba91f1a2389adbf97f781d2201069e025315223da77ba16a4',
 'UnityEngine.TextRenderingModule': 'fa6800f02259b1aaa7a6ab908fd284daaa51d425443acb1de4734f4e8cee7c93',
 'UnityEngine.AnimationModule': '91457683ba620b472a83c71e47e49ed93e583acf596fcd9a1a53dcd454b2b54b',
 'UnityEngine.UIModule': 'c14e64ff5e6293c654f533f226f86df2308c31c9576ea3a9e97a9b0dc68a97ab',
 'UnityEngine.UI': 'a30c47fa6b354ae50db6549458017dfed99664e881e8857e0740cbb6f00c68dd',
 'UnityEngine.InputLegacyModule': 'c29191e3675d012276de5305d74681b1c1618cb4c08311c6b8a06e27d04b4b12',
 'UnityEngine.JSONSerializeModule': '4e69c1269cc8f7c857a7b7b279aeacb62c74d1f1fc349c870837b989f5aae10f',
 'UnityEngine.AudioModule': '83b6eb39b73087281b33b78a4122d682ed0b4dfecf439578f0ebc89571d88644'}
EXPECTED_INITIALIZERS = {('Ascendance', 'GameBootstrap', 'Install'), ('Ascendance', 'MainMenuUI', 'InstallSceneListener'), ('Ascendance', 'ShellCameraBootstrap', 'Install'), ('Ascendance.Online', 'AscendanceDedicatedServerBootstrap0100', 'Install'), ('Ascendance.Online', 'AscendanceRemoteClientBootstrap0102', 'Parse')}

def require(condition, message):
    if not condition:
        raise ValueError(message)

def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate runtime metadata field')
        result[key] = value
    return result

def validate_initializer_contract(base):
    path = base / 'AscendanceServer_Data/RuntimeInitializeOnLoads.json'
    require(path.is_file() and (not path.is_symlink()) and (path.stat().st_size <= 65536), 'Runtime initialization metadata is not a supported regular file')
    data = json.loads(path.read_text(), object_pairs_hook=unique_object)
    entries = data.get('root') if isinstance(data, dict) else None
    require(isinstance(entries, list) and len(entries) <= 128, 'Unsupported runtime initialization metadata')
    actual = []
    for entry in entries:
        require(isinstance(entry, dict), 'Invalid runtime initializer')
        if entry.get('assemblyName') != 'Assembly-CSharp':
            continue
        require(type(entry.get('loadTypes')) is int and entry['loadTypes'] == 1 and (entry.get('isUnityClass') is False), 'Unsupported game runtime initialization phase')
        values = tuple((entry.get(key) for key in ('nameSpace', 'className', 'methodName')))
        require(all((isinstance(value, str) for value in values)), 'Invalid initializer identity')
        actual.append(values)
    require(len(actual) == len(EXPECTED_INITIALIZERS) and set(actual) == EXPECTED_INITIALIZERS, 'The supplied runtime does not match the reviewed g initialization contract')

def ensure_new_output(path):
    require(not path.exists() and (not path.is_symlink()), 'Output directory must be new')
    require(path.parent.is_dir(), 'Output parent directory must already exist')
    path.mkdir(mode=0o700)
    path.chmod(0o700)

def prepare_legacy_runtime(legacy_assembly, candidate, manifest, output, legacy_base=None, legacy_manifest=None):
    """Verify all code inputs, then construct an explicitly mixed-provenance private fixture."""
    require(sys.platform.startswith('linux'), 'This native server gate requires Linux')
    require(legacy_assembly.is_file() and (not legacy_assembly.is_symlink()) and (0 < legacy_assembly.stat().st_size <= 8 * 1024 * 1024), 'Legacy assembly must be a regular local file under 8 MiB')
    require(digest(legacy_assembly) == G_SHA, 'Legacy assembly SHA-256 differs from the inspected 0.10.2g server')
    require((legacy_base is None) == (legacy_manifest is None), 'Legacy base build and manifest must be supplied together')
    release.verify_artifact(candidate, release.read_json(manifest))
    base = legacy_base or candidate
    base_manifest = legacy_manifest or manifest
    if legacy_base is not None:
        release.verify_artifact(base, release.read_json(base_manifest))
    require(digest(base / 'UnityPlayer.so') == UNITY_PLAYER_SHA256, 'Legacy fixture requires the reviewed Unity runtime binary')
    for name, checksum in KNOWN_MANAGED_RUNTIME_SHA256.items():
        require(digest(base / 'AscendanceServer_Data/Managed' / (name + '.dll')) == checksum, 'Legacy fixture managed runtime differs from the reviewed dependency set')
    validate_initializer_contract(base)
    ensure_new_output(output)
    private = output / 'private'
    private.mkdir(mode=0o700)
    build = private / 'legacy-runtime'
    shutil.copytree(base, build)
    target = build / 'AscendanceServer_Data/Managed/Assembly-CSharp.dll'
    shutil.copyfile(legacy_assembly, target)
    require(digest(target) == G_SHA, 'Private legacy assembly copy checksum mismatch')
    fixture_manifest = release.artifact_manifest(build)
    provenance = {'fixtureOnly': True, 'notAnOriginalGDistribution': True, 'legacyAssemblySha256': G_SHA, 'baseManifestSha256': digest(base_manifest), 'unityPlayerSha256': UNITY_PLAYER_SHA256, 'managedDependencyHashesVerified': True, 'originalGameInitializerContractVerified': True, 'hostResourceAssetsCompared': False, 'baseUsesCandidateAssets': legacy_base is None}
    for name, value in [('legacy-fixture-manifest.json', fixture_manifest), ('fixture-provenance.json', provenance)]:
        path = output / name
        path.write_text(json.dumps(value, indent=2) + '\n')
        path.chmod(0o600)
    return (build, provenance)

class GClient(native.WireClient):

    def envelope(self, command_type, snapshot=None, **fields):
        snapshot = snapshot or self.snapshot()
        return {'CommandId': uuid.uuid4().hex, 'RoomId': self.room, 'PlayerId': self.player['PlayerId'], 'ExpectedRevision': snapshot['Revision'], 'Type': int(command_type), 'ActorHeroId': self.player['HeroId'], **fields}

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def json_read(path):
    return json.loads(path.read_text())

def canonical_campaign(value):
    """Ignore timestamps/version and enemy enumeration order only; retain ordered decks."""
    value = copy.deepcopy(value)
    value.pop('Version', None)
    value.pop('SavedAtUtc', None)
    enemies = value.get('Enemies')
    if not isinstance(enemies, list) or any((not isinstance(e, dict) or not isinstance(e.get('Id'), str) or (not e['Id']) for e in enemies)):
        raise AssertionError('Campaign enemy collection is malformed')
    if len({e['Id'].casefold() for e in enemies}) != len(enemies):
        raise AssertionError('Enemy IDs are not unique')
    value['Enemies'] = sorted(enemies, key=lambda e: e['Id'])
    return value

def changed_paths(a, b, path=''):
    if type(a) != type(b):
        return [path]
    if isinstance(a, dict):
        return sum([changed_paths(a[k], b[k], path + '/' + k) if k in a and k in b else [path + '/' + k] for k in sorted(set(a) | set(b))], [])
    if isinstance(a, list):
        return ([path + '/length'] if len(a) != len(b) else []) + sum([changed_paths(x, y, path + '/' + str(i)) for i, (x, y) in enumerate(zip(a, b))], [])
    return [] if a == b else [path]

def ownership(players):
    result = copy.deepcopy(players)
    for p in result:
        p.pop('Connected', None)
        p.pop('LastSeenUtc', None)
    return result

def run(legacy_assembly, candidate, manifest, output_dir, legacy_base=None, legacy_manifest=None):
    legacy_build, provenance = prepare_legacy_runtime(legacy_assembly, candidate, manifest, output_dir, legacy_base, legacy_manifest)
    output = output_dir / 'results.json'
    report = {
        'scope': 'Actual uploaded g assembly in reconstructed verified-Unity/base-assets fixture, candidate h native build, synthetic temporary saves, loopback clients',
        'legacyAssemblySha256': G_SHA,
        'candidateManifestSha256': digest(manifest),
        'candidateManagedAssemblySha256': digest(candidate / 'AscendanceServer_Data/Managed/Assembly-CSharp.dll'),
        'legacyFixtureProvenance': provenance,
        'noProductionConnection': True,
        'noExistingSaveRead': True,
        'phase': 'all',
        'checks': [],
        'limitations': [
            'Original host resource assets were not collected; legacy g uses the verified selected base assets.',
            'No DigitalOcean/systemd/Windows/GUI/audio acceptance is claimed.'
        ],
        'normalizationOracle': {
            'ignoredCampaignFields': ['Version', 'SavedAtUtc'],
            'orderCanonicalized': 'Enemies only, validated unique Ids; full enemy objects retained',
            'playerComparison': 'Exact private ownership/credential fields; Connected and LastSeenUtc advance on intentional test reconnects',
            'conditionalPresentationRestore': 'Only a blank saved SelectedEnemyId may become the exact first living local enemy in actual post-load array order; verified against actual g restart and unchanged ApplySaveData fallback source',
            'presentationProof': 'Actual uploaded-g restart reproduced the same source-defined blank-selection fallback'
        }
    }

    def check(name, expected, actual):
        ok = actual == expected
        report['checks'].append({'name': name, 'expected': expected, 'actual': actual, 'passed': ok})
        if not ok:
            raise AssertionError(name)

    def campaign_equal(name, expected, actual, allow_empty_selection_restore=False):
        comparison_expected = copy.deepcopy(expected)
        if allow_empty_selection_restore and (not expected.get('SelectedEnemyId')):
            selected = next((hero for hero in actual['Heroes'] if hero['Id'] == actual.get('SelectedHeroId')), None)
            fallback = next((enemy['Id'] for enemy in actual['Enemies'] if enemy['Health'] > 0 and selected is not None and (enemy['LocationId'] == selected['LocationId'])), None)
            check(name + ' legacy-empty-selection fallback is exact first living local enemy', True, fallback == actual.get('SelectedEnemyId'))
            comparison_expected['SelectedEnemyId'] = fallback
        paths = changed_paths(canonical_campaign(comparison_expected), canonical_campaign(actual))
        check(name, [], paths)

    def hello_check(h, s, label, version):
        check(label + ' exact build/HELLO', [version, True, 'HELLO', 1], [h.get('ServerBuildVersion'), h.get('Accepted'), h.get('Code'), h.get('ProtocolVersion')])
        check(label + ' exact isolated save path', True, str(s.save_path) == h['Snapshot'].get('PersistencePath0102d'))
        assert Path(h['Snapshot']['PersistencePath0102d']).is_relative_to(s.config)
        if version == '0.10.2h':
            inodes = set()
            for fd in (Path('/proc') / str(s.process.pid) / 'fd').iterdir():
                try:
                    target = os.readlink(fd)
                    if target.startswith('socket:['):
                        inodes.add(target[8:-1])
                except FileNotFoundError:
                    pass
            addresses = []
            for line in Path('/proc/net/tcp').read_text().splitlines()[1:]:
                parts = line.split()
                address, port = parts[1].split(':')
                if parts[3] == '0A' and int(port, 16) == s.port and (parts[9] in inodes):
                    addresses.append(socket.inet_ntoa(bytes.fromhex(address)[::-1]))
            check(label + ' actual process listener is loopback with explicit bind flag', ['127.0.0.1'], sorted(addresses))

    def make_payload(room, case):
        d = copy.deepcopy(room)
        d['CreatedAtUtc'] = '2025-02-03T04:05:06.0000000Z'
        c = d['DurableCampaign']
        c['PartyGold'] = 111
        c['TurnHeroId'] = ''
        c['SelectedHeroId'] = 'Aldric'
        c['PendingStateSchema0102f'] = 1
        c['HasPendingRewardChoice0102f'] = True
        c['HasPendingEncounterDeckControl0102f'] = True
        c['PendingRewardChoice'] = {'SourceCardId': 'SD082', 'SourceCardName': 'Synthetic g migration reward', 'HeroId': 'Aldric', 'Mode': 'Gold25OrLoot1', 'Prompt': 'Synthetic reward', 'RevealedCardIds': []}
        c['PendingEncounterDeckControl'] = {'SourceCardId': 'SD041', 'SourceCardName': 'Synthetic g migration deck', 'Mode': 'MayBottomTop', 'RevealedCardIds': c['EncounterDeckIds'][:1]}
        if case == 'schema1-false-stale':
            c['HasPendingRewardChoice0102f'] = False
            c['HasPendingEncounterDeckControl0102f'] = False
        elif case == 'schema0-valid':
            c['PendingStateSchema0102f'] = 0
            c['HasPendingRewardChoice0102f'] = False
            c['HasPendingEncounterDeckControl0102f'] = False
        elif case == 'schema1-true-blank':
            c['PendingRewardChoice'] = {}
            c['PendingEncounterDeckControl'] = {}
        return d
    started = time.monotonic()
    try:
        for case in ['schema1-false-stale', 'schema1-true-valid', 'schema0-valid', 'schema1-true-blank']:
            pending = case in ['schema1-true-valid', 'schema0-valid']
            server = client = None
            with tempfile.TemporaryDirectory(prefix='asc-g-h-migration-') as tmp:
                try:
                    root = Path(tmp)
                    room = 'G-H-' + uuid.uuid4().hex.upper()
                    server = native.NativeServer(legacy_build, root, room, environment={'ASCENDANCE_LISTEN_LOOPBACK': '1'}, client_factory=GClient)
                    client, h = server.start()
                    hello_check(h, server, case + ' g fresh', '0.10.2g')
                    client.close()
                    client = None
                    server.stop()
                    raw = make_payload(json_read(server.save_path), case)
                    server.save_path.write_text(json.dumps(raw))
                    client, h = server.start()
                    hello_check(h, server, case + ' g baseline', '0.10.2g')
                    check(case + ' g pending semantics', [pending, pending], [h['Snapshot']['Transient']['HasPendingRewardChoice'], h['Snapshot']['Transient']['HasPendingEncounterDeckControl']])
                    joined = client.join('Aldric', uuid.uuid4().hex)
                    check(case + ' real g session established', True, joined.get('Accepted'))
                    credential = joined['Player']['ReconnectToken']
                    player_id = joined['Player']['PlayerId']
                    client.close()
                    client = None
                    time.sleep(0.2)
                    server.stop()
                    g_expected = json_read(server.save_path)
                    h_input = copy.deepcopy(g_expected)
                    for field in ['PendingStateSchema0102f', 'HasPendingRewardChoice0102f', 'HasPendingEncounterDeckControl0102f', 'PendingRewardChoice', 'PendingEncounterDeckControl']:
                        h_input['DurableCampaign'][field] = raw['DurableCampaign'][field]
                    server.save_path.write_text(json.dumps(h_input))
                    server.build = candidate
                    server.client_factory = native.WireClient
                    client, h = server.start()
                    hello_check(h, server, case + ' h migration', '0.10.2h')
                    check(case + ' h restored origin', 'RESTORED_FROM_SERVER_DISK', h['Snapshot'].get('RoomStateOrigin0102d'))
                    check(case + ' original CreatedAt preserved', True, g_expected['CreatedAtUtc'] == h['Snapshot'].get('RoomCreatedAtUtc0102d'))
                    check(case + ' h pending semantics', [pending, pending], [h['Snapshot']['Transient']['HasPendingRewardChoice'], h['Snapshot']['Transient']['HasPendingEncounterDeckControl']])
                    h_loaded = json_read(server.save_path)
                    campaign_equal(case + ' complete g-to-h campaign preserved', g_expected['DurableCampaign'], h_loaded['DurableCampaign'])
                    check(case + ' private g ownership/credentials preserved', True, ownership(g_expected['Players']) == ownership(h_loaded['Players']))
                    rejoined = client.reconnect('Aldric', credential)
                    check(case + ' original g session reconnects to h', True, rejoined.get('Accepted') is True and rejoined.get('Player', {}).get('PlayerId') == player_id and (rejoined.get('Player', {}).get('HeroId') == 'Aldric'))
                    if pending:
                        r = client.command(native.Command.REWARD_CHOICE, OptionKey='GOLD')
                        check(case + ' h actual +25 Gold', [True, 136, False], [r.get('Accepted'), r['Snapshot']['Campaign']['PartyGold'], r['Snapshot']['Transient']['HasPendingRewardChoice']])
                        r = client.command(native.Command.ENCOUNTER_DECK_CHOICE, OptionKey='KEEP')
                        check(case + ' h actual deck KEEP', [True, False], [r.get('Accepted'), r['Snapshot']['Transient']['HasPendingEncounterDeckControl']])
                    r = client.command(native.Command.START_TURN)
                    check(case + ' h real turn remains usable', [True, 3], [r.get('Accepted'), native.hero(r['Snapshot'], 'Aldric')['Actions']])
                    client.close()
                    client = None
                    time.sleep(0.2)
                    server.stop()
                    h_before_restart = json_read(server.save_path)
                    check(case + ' exact resulting Gold', 136 if pending else 111, h_before_restart['DurableCampaign']['PartyGold'])
                    client, h = server.start()
                    hello_check(h, server, case + ' h process restart', '0.10.2h')
                    campaign_equal(case + ' complete h campaign survives process restart', h_before_restart['DurableCampaign'], h['Snapshot']['Campaign'], allow_empty_selection_restore=True)
                    rejoined = client.reconnect('Aldric', credential)
                    check(case + ' h-restart same Hero credential', True, rejoined.get('Accepted') is True and rejoined.get('Player', {}).get('PlayerId') == player_id)
                    client.close()
                    client = None
                    time.sleep(0.2)
                    server.stop()
                    h_final = json_read(server.save_path)
                    server.build = legacy_build
                    server.client_factory = GClient
                    client, h = server.start()
                    hello_check(h, server, case + ' rollback g', '0.10.2g')
                    check(case + ' rollback preserves actual Gold and choices', [136 if pending else 111, False, False], [h['Snapshot']['Campaign']['PartyGold'], h['Snapshot']['Transient']['HasPendingRewardChoice'], h['Snapshot']['Transient']['HasPendingEncounterDeckControl']])
                    campaign_equal(case + ' complete campaign preserved by g rollback', h_final['DurableCampaign'], h['Snapshot']['Campaign'], allow_empty_selection_restore=True)
                    g_rollback = json_read(server.save_path)
                    check(case + ' g rollback preserves private Hero credentials', True, ownership(h_final['Players']) == ownership(g_rollback['Players']))
                    check(case + ' rollback retains original CreatedAt', True, g_expected['CreatedAtUtc'] == g_rollback['CreatedAtUtc'])
                finally:
                    if client:
                        client.close()
                    if server:
                        server.stop()
                    if sys.exc_info()[0] is not None:
                        private = output.parent / 'private' / ('failure-' + uuid.uuid4().hex[:8])
                        shutil.copytree(root, private)
                        private.chmod(0o700)
                        report['privateFailureDirectory'] = str(private)
        for invalid in ['corrupt-json', 'future-schema2', 'case-alias', 'missing-campaign', 'linked-save', 'directory-save', 'read-only-save']:
            server = client = None
            with tempfile.TemporaryDirectory(prefix='asc-h-save-refusal-') as tmp:
                try:
                    root = Path(tmp)
                    room = 'H-REFUSE-' + uuid.uuid4().hex.upper()
                    server = native.NativeServer(legacy_build, root, room, environment={'ASCENDANCE_LISTEN_LOOPBACK': '1'}, client_factory=GClient)
                    client, h = server.start()
                    hello_check(h, server, invalid + ' seed g', '0.10.2g')
                    client.close()
                    client = None
                    server.stop()
                    d = json_read(server.save_path)
                    if invalid == 'corrupt-json':
                        server.save_path.write_text('{this is not valid JSON')
                    elif invalid == 'future-schema2':
                        d['DurableCampaign']['PendingStateSchema0102f'] = 2
                        server.save_path.write_text(json.dumps(d))
                    elif invalid == 'case-alias':
                        d['RoomId'] = room.lower()
                        server.save_path.write_text(json.dumps(d))
                    elif invalid == 'missing-campaign':
                        d['DurableCampaign'] = {}
                        server.save_path.write_text(json.dumps(d))
                    elif invalid == 'linked-save':
                        target = root / 'synthetic-link-target.json'
                        target.write_text(json.dumps(d))
                        server.save_path.unlink()
                        server.save_path.symlink_to(target)
                    elif invalid == 'directory-save':
                        server.save_path.unlink()
                        server.save_path.mkdir()
                    elif invalid == 'read-only-save':
                        check(invalid + ' native permission oracle is not root', True, os.geteuid() != 0)
                        server.save_path.chmod(0o444)
                        check(invalid + ' valid readable save has no write bits', 0, server.save_path.stat().st_mode & 0o222)
                    before_files = {str(p.relative_to(root)): digest(p) for p in root.rglob('*') if p.is_file() and p.suffix == '.json'}
                    server.build = candidate
                    server.client_factory = native.WireClient
                    accepted = False
                    exit_code = None
                    failure_type = None
                    try:
                        client, h = server.start()
                        accepted = True
                    except (RuntimeError, TimeoutError) as e:
                        failure_type = type(e).__name__
                        exit_code = server.process.poll() if server.process else None
                    check(invalid + ' no replacement room serves HELLO', False, accepted)
                    check(invalid + ' startup explicitly exits with refusal', 4, exit_code)
                    after_files = {str(p.relative_to(root)): digest(p) for p in root.rglob('*') if p.is_file() and p.suffix == '.json'}
                    check(invalid + ' complete synthetic save JSON hashes unchanged', True, before_files == after_files)
                    log = server.logs[-1].read_text(errors='replace')
                    check(invalid + ' expected refusal marker logged', True, 'ROOM_RESTORE_REFUSED' in log)
                finally:
                    if client:
                        client.close()
                    if server:
                        server.stop()
                    if sys.exc_info()[0] is not None:
                        private = output.parent / 'private' / ('failure-' + uuid.uuid4().hex[:8])
                        shutil.copytree(root, private)
                        private.chmod(0o700)
                        report['privateFailureDirectory'] = str(private)
        check('Original inspected legacy assembly remains unchanged', G_SHA, digest(legacy_assembly))
        check('Verified candidate artifact remains unchanged after native tests', True,
              release.artifact_manifest(candidate) == release.read_json(manifest))
        if legacy_base is not None:
            check('Verified optional legacy base artifact remains unchanged', True,
                  release.artifact_manifest(legacy_base) == release.read_json(legacy_manifest))
        report['status'] = 'PASS'
    except Exception as e:
        report['status'] = 'FAIL'
        report['error'] = {'type': type(e).__name__, 'check': str(e) if isinstance(e, AssertionError) else 'Native migration fixture failed; inspect private fixture logs'}
    finally:
        report['passed'] = sum((c['passed'] for c in report['checks']))
        report['failed'] = sum((not c['passed'] for c in report['checks']))
        report['durationSeconds'] = round(time.monotonic() - started, 3)
        if report.get('status') == 'PASS':
            shutil.rmtree(output_dir / 'private')
        output.write_text(json.dumps(report, indent=2) + '\n')
        output.chmod(0o600)
    return report

def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--legacy-assembly', type=Path, required=True)
    parser.add_argument('--candidate-build', type=Path, required=True)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True, help='New output directory; upload only results.json')
    parser.add_argument('--legacy-base-build', type=Path)
    parser.add_argument('--legacy-base-manifest', type=Path)
    args = parser.parse_args(argv)
    if bool(args.legacy_base_build) != bool(args.legacy_base_manifest):
        parser.error('--legacy-base-build and --legacy-base-manifest are required together')
    try:
        report = run(args.legacy_assembly.absolute(), args.candidate_build.resolve(), args.manifest.resolve(), args.output.absolute(), args.legacy_base_build.resolve() if args.legacy_base_build else None, args.legacy_base_manifest.resolve() if args.legacy_base_manifest else None)
    except Exception as error:
        report = {'status': 'REFUSED', 'phase': 'preparation', 'passed': 0, 'failed': 0, 'error': {'type': type(error).__name__, 'check': str(error) if isinstance(error, ValueError) else 'Native gate preparation failed; no successful gameplay validation is assumed'}}
    print(json.dumps({key: report.get(key) for key in ['status', 'passed', 'failed', 'error']}))
    return 0 if report.get('status') == 'PASS' else 1
if __name__ == '__main__':
    raise SystemExit(main())
