"""Read-only diagnostic fixtures; never connect to a game or modify a service."""
import copy
import importlib.util
import json
import os
from pathlib import Path
import shutil
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch

ROOT=Path(__file__).resolve().parents[1]
def load(name):
    spec=importlib.util.spec_from_file_location(name.replace('-','_'),ROOT/'Deploy/DigitalOcean'/(name+'.py'))
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
diagnostic=load('diagnose-live-persistence')
upgrade=load('upgrade-existing')


class LivePersistenceDiagnostic(unittest.TestCase):
    def setUp(self):
        self.schema=json.loads((ROOT/'Deploy/DigitalOcean/save-schema-0102g.json').read_text())
        self.plan={'process':{'pid':42,'uid':999,'gid':988,'port':7777,'room':'private-room'},
                   'persistence':{'expectedDataDirectory':{'path':'/private/persistence'}}}
        campaign={'Version':'0.10.2h','SavedAtUtc':'private-timestamp','QuestId':'Q001','PartyGold':73,
                  'Heroes':[{'Id':'private-hero-a','Health':5},{'Id':'private-hero-b','Health':8}],
                  'Enemies':[{'Id':'private-enemy-a','Health':3},{'Id':'private-enemy-b','Health':4}],
                  'SelectedHeroId':'private-hero-a','SelectedEnemyId':'private-enemy-a'}
        self.room={'RoomId':'private-room','BuildVersion':'0.10.2h','CreatedAtUtc':'private-creation',
                   'Revision':5,'DurableCampaign':campaign,'Players':[{'PlayerId':'private-player','DisplayName':'private-name','HeroId':'private-hero-a','ReconnectToken':'private-token'}]}
        self.snapshot={'RoomId':'private-room','BuildVersion':'0.10.2h','Revision':5,
                       'RoomCreatedAtUtc0102d':'private-creation',
                       'PersistencePath0102d':str(upgrade.room_path(self.plan)),
                       'Campaign':copy.deepcopy(campaign),
                       'Players':[{'PlayerId':'private-player','DisplayName':'private-name','HeroId':'private-hero-a'}]}

    def test_selection_difference_reports_only_field_paths_and_keeps_strict_guard(self):
        self.snapshot['Campaign']['SelectedHeroId']='private-hero-b'
        self.snapshot['Campaign']['SelectedEnemyId']='private-enemy-b'
        report=diagnostic.summarize(self.room,self.snapshot,self.plan,upgrade,self.schema)
        self.assertEqual(report['status'],'MISMATCH')
        self.assertFalse(report['guardChecks']['campaignEquivalentUnderReviewedGuard'])
        self.assertEqual(report['semanticCampaignDifferencePaths'],['DurableCampaign.SelectedEnemyId','DurableCampaign.SelectedHeroId'])
        for private in ['private-hero','private-enemy','private-room','private-player','private-token','private-name','private-creation']:
            self.assertNotIn(private,json.dumps(report))
        self.assertEqual(report['gameplayCommandsSubmitted'],0)

    def test_gameplay_gold_health_and_order_differences_are_visible(self):
        self.snapshot['Campaign']['PartyGold']=1
        self.snapshot['Campaign']['Heroes'][0]['Health']=1
        self.snapshot['Campaign']['Heroes'].reverse()
        report=diagnostic.summarize(self.room,self.snapshot,self.plan,upgrade,self.schema)
        paths=report['semanticCampaignDifferencePaths']
        self.assertIn('DurableCampaign.PartyGold',paths)
        self.assertIn('DurableCampaign.Heroes[0].Health',paths)
        self.assertIn('DurableCampaign.Heroes[0].Id',paths)
        self.assertEqual(report['status'],'MISMATCH')

    def test_known_saved_timestamp_equivalence_is_not_a_gameplay_difference(self):
        self.snapshot['Campaign']['SavedAtUtc']='another-private-time'
        report=diagnostic.summarize(self.room,self.snapshot,self.plan,upgrade,self.schema)
        self.assertEqual(report['status'],'MATCH')
        self.assertEqual(report['semanticCampaignDifferencePaths'],[])
        self.assertEqual(report['rawCampaignDifferencePaths'],['DurableCampaign.SavedAtUtc'])

    def test_identity_mismatch_is_reported_as_booleans_without_values(self):
        self.snapshot['Revision']=6
        self.snapshot['Players'][0]['PlayerId']='another-private-player'
        report=diagnostic.summarize(self.room,self.snapshot,self.plan,upgrade,self.schema)
        self.assertFalse(report['guardChecks']['revisionMatches'])
        self.assertFalse(report['guardChecks']['playerOwnershipMatches'])
        self.assertNotIn('another-private',json.dumps(report))
        self.assertEqual(report['status'],'MISMATCH')

    def test_schema_rejects_unknown_live_fields_and_path_keys_cannot_emit_values(self):
        self.snapshot['Campaign']['UnknownCredential']='private-token'
        with self.assertRaises(Exception):diagnostic.summarize(self.room,self.snapshot,self.plan,upgrade,self.schema)
        with self.assertRaises(ValueError):diagnostic.field_paths({'private/value':1},{})

    def test_pinned_helper_tampering_and_symlinks_refused_before_import(self):
        with tempfile.TemporaryDirectory() as temporary:
            folder=Path(temporary)
            for name in diagnostic.BUNDLE_HELPERS:
                (folder/name).write_text('SOURCE_H_SHA256="synthetic-reviewed-source"\n' if name.endswith('.py') else '{}')
            proofs={name:{'sha256':diagnostic.sha((folder/name).read_bytes())} for name in diagnostic.BUNDLE_HELPERS}
            data=json.dumps({'helpers':proofs}).encode();(folder/'release.json').write_bytes(data)
            pins={name:proofs[name]['sha256'] for name in diagnostic.PINNED_HELPERS}
            with patch.object(diagnostic,'PINNED_DESCRIPTOR',diagnostic.sha(data)),patch.object(diagnostic,'PINNED_HELPERS',pins):
                modules=diagnostic.verified_helpers(folder)
                self.assertEqual(modules[0].SOURCE_H_SHA256,'synthetic-reviewed-source')
                (folder/'upgrade-existing.py').write_text('raise RuntimeError("must never import")')
                with self.assertRaises(ValueError):diagnostic.verified_helpers(folder)
                (folder/'upgrade-existing.py').unlink()
                (folder/'upgrade-existing.py').symlink_to(ROOT/'Deploy/DigitalOcean/upgrade-existing.py')
                with self.assertRaises(ValueError):diagnostic.verified_helpers(folder)
            self.assertFalse((folder/'__pycache__').exists())

    def test_tls_reads_only_public_certificate_and_private_room_key(self):
        class PrivateConfig:
            def is_dir(self):return True
            def is_symlink(self):return False
            def stat(self):return SimpleNamespace(st_uid=0,st_mode=0o40750)
            def __truediv__(self,name):return Path('/private-config')/name
        environment=b'ASCENDANCE_TLS_PFX_PATH=/private-config/server.pfx\0ASCENDANCE_TLS_PFX_PASSWORD_FILE=/private-config/pfx-password\0ASCENDANCE_ROOM_ACCESS_KEY_FILE=/private-config/room-access-key\0UNRELATED_SECRET=must-not-read\0'
        reads=[]
        def read(path,limit,owner=None):
            reads.append(str(path))
            if path.name=='environ':return environment
            if path.name=='server.crt':return b'public-cert-pem'
            if path.name=='room-access-key':return b'a'*64
            raise AssertionError('Unexpected private-file access')
        run=Mock(return_value=SimpleNamespace(stdout=b'public-cert-der'))
        with patch.object(diagnostic,'regular_bytes',side_effect=read),patch.object(diagnostic,'CERTIFICATE',diagnostic.sha(b'public-cert-der')):
            pin,key=diagnostic.read_tls_credentials(self.plan,upgrade,config=PrivateConfig(),run=run)
        self.assertEqual(key,'a'*64)
        self.assertEqual(pin,diagnostic.sha(b'public-cert-der'))
        self.assertEqual(reads,['/proc/42/environ','/private-config/server.crt','/private-config/room-access-key'])
        self.assertNotIn('pfx-password',' '.join(run.call_args.args[0]))

    def test_diagnosis_only_reads_room_and_sends_one_authenticated_hello(self):
        with tempfile.TemporaryDirectory() as temporary:
            code=Path(temporary);(code/'AscendanceServer_Data/Managed').mkdir(parents=True)
            image=code/'AscendanceServer_Data/Managed/Assembly-CSharp.dll';image.write_bytes(b'synthetic-code')
            plan=copy.deepcopy(self.plan);plan['process']['executable']=str(code/'AscendanceServer.x86_64')
            plan.update(readOnly=True,warnings=[],serviceProperties={'ActiveState':'active','SubState':'running'})
            helper=SimpleNamespace(load_save_schema=Mock(return_value=self.schema),read_private_room=Mock(side_effect=[self.room,copy.deepcopy(self.room)]),
                                   hello=Mock(return_value=self.snapshot),room_path=upgrade.room_path,
                                   validate_serialized_value=upgrade.validate_serialized_value,
                                   campaigns_equivalent=upgrade.campaigns_equivalent,semantic_campaign=upgrade.semantic_campaign)
            credentials=Mock(return_value=('pinned-public-certificate','private-key'))
            inspector=SimpleNamespace(inspect=Mock(return_value=plan))
            with patch.object(diagnostic,'SOURCE_H',diagnostic.sha(b'synthetic-code')):
                report=diagnostic.diagnose(helper,inspector,credentials)
            self.assertEqual(report['status'],'MATCH')
            self.assertTrue(report['diskStableAcrossReadOnlyProbe'])
            helper.hello.assert_called_once_with(7777,'private-room','pinned-public-certificate','private-key',expected_build='0.10.2h')
            self.assertEqual(helper.read_private_room.call_count,2)
            self.assertFalse(report['serviceRestarted'])

    def test_main_suppresses_private_exception_details(self):
        with patch.object(diagnostic.os,'geteuid',return_value=0),patch.object(diagnostic,'find_helpers',side_effect=RuntimeError('private-token')),patch('builtins.print') as output:
            self.assertEqual(diagnostic.main([]),1)
            value=json.loads(output.call_args.args[0])
            self.assertEqual(value['errorType'],'RuntimeError')
            self.assertNotIn('private-token',json.dumps(value))
            self.assertEqual(value['status'],'DIAGNOSTIC_UNAVAILABLE')


if __name__=='__main__':unittest.main()
