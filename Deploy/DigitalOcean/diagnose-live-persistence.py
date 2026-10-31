#!/usr/bin/env python3
"""Read-only pinned-TLS HELLO versus disk comparison; emit field paths only.

No JOIN, gameplay command, service change, save write, permission change, or
restart is performed. Values, room/player IDs, private keys and raw errors are
never printed. Reuse the owner's already verified installer helper directory.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import subprocess
import sys

sys.dont_write_bytecode = True
SOURCE_H = 'ee6b1db4ed7f78d72dcbccb436d5510d39a18f582b10cb72a059ed3411916ec2'
CERTIFICATE = '7f9987d3848887b028e52e2e8f9077035cedca0e038711212c2ba7c86fb7a6a9'
PINNED_DESCRIPTOR = 'c6ddf1028a738b1648673a38467a12989631c431c58a3a331dba3648a3b5a722'
PINNED_HELPERS = {
    'upgrade-existing.py': '07d2d8422f88ea2f78ef301b3de07e2aa5cf6e883c21bad4ecd228460b5f4d87',
    'inspect-existing.py': '1082801265691b2a39cb440d1ecbe8c87a3af17601f20bab7edd52e0c0de4ac4',
    'save-schema-0102g.json': '204110bafe3216cf87ae830c5a9b37192661512583c7a0e922039f7b2627f93d',
}
CONFIG = Path('/etc/ascendance-secure')
BUNDLE_HELPERS = {'upgrade-existing.py','inspect-existing.py','save-schema-0102g.json',
                  'release.py','diagnose-existing.py','native_server_acceptance.py'}


def require(ok):
    if not ok: raise ValueError('Diagnostic prerequisite requires review')


def regular_bytes(path, limit, owner=None):
    require(path.is_absolute() and not any(p.is_symlink() for p in [path, *path.parents]))
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, 'rb') as stream:
        info = os.fstat(stream.fileno())
        require(stat.S_ISREG(info.st_mode) and info.st_size <= limit
                and (owner is None or info.st_uid == owner and not stat.S_IMODE(info.st_mode) & 0o022))
        data = stream.read(limit + 1)
        require(len(data) <= limit)
    return data


def sha(data): return hashlib.sha256(data).hexdigest()


def verified_helpers(folder):
    folder = folder.absolute()
    require(folder.is_dir() and not folder.is_symlink() and folder.stat().st_uid == os.geteuid()
            and not stat.S_IMODE(folder.stat().st_mode) & 0o022)
    descriptor_bytes=regular_bytes(folder/'release.json',64*1024,os.geteuid())
    require(sha(descriptor_bytes)==PINNED_DESCRIPTOR)
    descriptor=json.loads(descriptor_bytes)
    require(isinstance(descriptor.get('helpers'),dict) and set(descriptor['helpers'])==BUNDLE_HELPERS)
    for name,entry in descriptor['helpers'].items():
        require(isinstance(entry,dict) and re.fullmatch(r'[0-9a-f]{64}',entry.get('sha256','')))
        require(sha(regular_bytes(folder/name,2*1024*1024,os.geteuid()))==entry['sha256'])
    for name, checksum in PINNED_HELPERS.items():
        require(descriptor['helpers'][name]['sha256']==checksum)
        require(sha(regular_bytes(folder/name, 2*1024*1024, os.geteuid())) == checksum)
    def load(name):
        spec = importlib.util.spec_from_file_location('readonly_' + name.replace('-', '_'), folder/(name+'.py'))
        module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
        return module
    return load('upgrade-existing'), load('inspect-existing')


def find_helpers(requested=None):
    if requested is not None:
        upgrade, inspector = verified_helpers(requested)
        return upgrade, inspector
    candidates = sorted(Path('/var/tmp').glob('ascendance-reviewed-install-*'), key=lambda p:p.stat().st_mtime_ns, reverse=True)
    for folder in candidates[:32]:
        try: return verified_helpers(folder)
        except Exception: continue
    raise ValueError('Verified installer helpers were not found')


def field_paths(left, right, prefix='DurableCampaign', limit=100):
    """Only declared-style field names and numeric list indexes may be emitted."""
    result = []
    def visit(a, b, path):
        if len(result) >= limit: return
        if type(a) is not type(b):
            result.append(path); return
        if isinstance(a, dict):
            for key in sorted(set(a) | set(b)):
                require(isinstance(key, str) and re.fullmatch(r'[A-Za-z][A-Za-z0-9_]{0,127}', key))
                child = path+'.'+key
                if key not in a or key not in b: result.append(child)
                else: visit(a[key], b[key], child)
                if len(result) >= limit: return
        elif isinstance(a, list):
            if len(a) != len(b): result.append(path)
            for index, (av, bv) in enumerate(zip(a,b)):
                visit(av,bv,path+'['+str(index)+']')
                if len(result) >= limit: return
        elif a != b: result.append(path)
    visit(left,right,prefix)
    return result


def summarize(room, snapshot, plan, upgrade, schema):
    require(isinstance(snapshot,dict) and isinstance(snapshot.get('Campaign'),dict))
    campaign_type = schema['types'][schema['rootType']]['DurableCampaign']
    upgrade.validate_serialized_value(snapshot['Campaign'], campaign_type, schema)
    expected_path = str(upgrade.room_path(plan))
    player_fields = ('PlayerId','DisplayName','HeroId')
    public_players = snapshot.get('Players')
    require(isinstance(public_players,list) and all(isinstance(p,dict) for p in public_players))
    checks = {
        'roomIdentityMatches': snapshot.get('RoomId') == room['RoomId'],
        'buildIdentityMatches': snapshot.get('BuildVersion') == room['BuildVersion'],
        'revisionMatches': snapshot.get('Revision') == room['Revision'],
        'livePersistencePathMatches': snapshot.get('PersistencePath0102d') == expected_path,
        'roomCreationIdentityMatches': snapshot.get('RoomCreatedAtUtc0102d') == room['CreatedAtUtc'],
        'playerOwnershipMatches': [{k:p.get(k) for k in player_fields} for p in public_players]
                                  == [{k:p.get(k) for k in player_fields} for p in room['Players']],
        'campaignEquivalentUnderReviewedGuard': upgrade.campaigns_equivalent(room['DurableCampaign'], snapshot['Campaign']),
    }
    return {'format':'ascendance-live-persistence-field-paths-1','readOnly':True,
            'status':'MATCH' if all(checks.values()) else 'MISMATCH',
            'gameplayCommandsSubmitted':0,'joinRequestsSubmitted':0,'saveWritesPerformed':0,
            'serviceChangesPerformed':0,'serviceRestarted':False,'valuesEmitted':False,
            'guardChecks':checks,
            'semanticCampaignDifferencePaths':field_paths(upgrade.semantic_campaign(room['DurableCampaign']), upgrade.semantic_campaign(snapshot['Campaign'])),
            'rawCampaignDifferencePaths':field_paths(room['DurableCampaign'], snapshot['Campaign'])}


def read_tls_credentials(plan, upgrade, proc_root=Path('/proc'), config=CONFIG, run=subprocess.run):
    require(config.is_dir() and not config.is_symlink() and config.stat().st_uid == 0
            and not stat.S_IMODE(config.stat().st_mode) & 0o022)
    environment = {}
    allowed = {b'ASCENDANCE_TLS_PFX_PATH', b'ASCENDANCE_TLS_PFX_PASSWORD_FILE', b'ASCENDANCE_ROOM_ACCESS_KEY_FILE'}
    for entry in regular_bytes(proc_root/str(plan['process']['pid'])/'environ',1024*1024).split(b'\0'):
        name,sep,value=entry.partition(b'=')
        if sep and name in allowed: environment[name.decode()]=value.decode('utf-8','strict')
    require(environment.get('ASCENDANCE_TLS_PFX_PATH') == str(config/'server.pfx')
            and environment.get('ASCENDANCE_TLS_PFX_PASSWORD_FILE') == str(config/'pfx-password')
            and environment.get('ASCENDANCE_ROOM_ACCESS_KEY_FILE') == str(config/'room-access-key'))
    # Read only the public certificate and room-authentication key. The PFX and
    # private certificate password are deliberately never opened.
    regular_bytes(config/'server.crt', 64*1024, 0)
    key=regular_bytes(config/'room-access-key',1024,0).decode('ascii').strip()
    require(re.fullmatch(r'[A-Fa-f0-9]{64}',key))
    result=run(['openssl','x509','-in',str(config/'server.crt'),'-outform','DER'],capture_output=True,check=True,timeout=10)
    require(sha(result.stdout)==CERTIFICATE)
    return CERTIFICATE,key


def diagnose(upgrade, inspector, credentials=read_tls_credentials):
    plan=inspector.inspect('ascendance')
    require(plan.get('readOnly') is True and plan['serviceProperties'].get('ActiveState')=='active'
            and plan['serviceProperties'].get('SubState')=='running' and not plan.get('warnings'))
    process=plan['process']
    require(process['port']==7777 and re.fullmatch(r'[A-Za-z0-9_-]{1,128}',process['room']))
    assembly=Path(process['executable']).parent/'AscendanceServer_Data/Managed/Assembly-CSharp.dll'
    require(sha(regular_bytes(assembly,32*1024*1024))==SOURCE_H)
    process['wireBuildVersion']='0.10.2h'
    schema=upgrade.load_save_schema({'helpers':{'save-schema-0102g.json':{'sha256':PINNED_HELPERS['save-schema-0102g.json']}}})
    pin,key=credentials(plan,upgrade)
    before=upgrade.read_private_room(plan,schema)
    snapshot=upgrade.hello(process['port'],process['room'],pin,key,expected_build='0.10.2h')
    after=upgrade.read_private_room(plan,schema)
    report=summarize(after,snapshot,plan,upgrade,schema)
    report['diskStableAcrossReadOnlyProbe']=before==after
    if before != after: report['status']='STATE_CHANGED_DURING_READ_ONLY_PROBE'
    report['reviewedOriginalHSourceVerified']=True
    report['pinnedTlsAuthenticatedHelloVerified']=True
    report['helperSetVerified']=True
    return report


def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--helpers-dir','--bundle',dest='helpers_dir',type=Path)
    args=parser.parse_args(argv)
    try:
        require(sys.platform.startswith('linux') and os.geteuid()==0)
        upgrade,inspector=find_helpers(args.helpers_dir)
        result=diagnose(upgrade,inspector)
        print(json.dumps(result,indent=2)); return 0
    except Exception as error:
        print(json.dumps({'format':'ascendance-live-persistence-field-paths-1','readOnly':True,
                          'status':'DIAGNOSTIC_UNAVAILABLE','errorType':type(error).__name__,
                          'valuesEmitted':False,'gameplayCommandsSubmitted':0,'joinRequestsSubmitted':0,
                          'saveWritesPerformed':0,'serviceChangesPerformed':0,'serviceRestarted':False},indent=2))
        return 1


if __name__=='__main__': sys.exit(main())
