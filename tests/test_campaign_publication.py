"""Synthetic publication fixtures; no game, GitHub upload or deployment runs."""
import copy
import importlib.util
import json
import os
from pathlib import Path
import shutil
import unittest
from unittest.mock import patch
import zipfile

from tests import test_multiplayer_packaging as packaging_tests

package = packaging_tests.package

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('campaign_publish', ROOT/'.github/scripts/publish_campaign_draft.py')
publish = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publish)


class CampaignPublication(unittest.TestCase):
    def setUp(self):
        build = packaging_tests.MultiplayerPackaging('test_complete_packages_checksums_provenance_and_launcher_layout')
        build.setUp()
        self.addCleanup(build.doCleanups)
        self.root = build.root/'repository'
        self.root.mkdir()
        self.revision = build.sha
        self.tooling = 'b'*40
        self.tag = 'unity-0.10.2h-online-campaigns-' + self.revision[:7]
        self.assets = self.root/'distribution'/self.tag
        self.assets.parent.mkdir()
        build.args.output = self.assets
        build.args.release = self.tag
        package.package(build.args)
        for name in ['tools/package_multiplayer_release.py', *publish.HELPERS.values()]:
            target = self.root/name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT/name, target)
        def entry(name):
            data = (self.assets/name).read_bytes()
            return {'url':'https://github.com/'+publish.REPOSITORY+'/releases/download/'+self.tag+'/'+name,
                    'sha256':publish.digest(data), 'sizeBytes':len(data)}
        descriptor = {'format':1,'release':self.tag,'sourceRevision':self.revision,'deploymentRevision':self.tooling,
                      'buildVersion':'0.10.2h','endpoint':'159.65.100.206:7777',
                      'server':{'archive':entry('ascendance-linux-server-'+self.tag+'.zip'), 'manifest':entry('linux-server-manifest.json')},
                      'client':{'archive':entry('ascendance-windows-'+self.tag+'.zip')},
                      'helpers':{name:{'url':'https://raw.githubusercontent.com/'+publish.REPOSITORY+'/'+self.tooling+'/'+relative,
                                       'sha256':publish.digest((self.root/relative).read_bytes())} for name,relative in publish.HELPERS.items()}}
        (self.assets/'release.json').write_bytes(package.canonical(descriptor))
        check = {'name':'synthetic fixture only','expected':1,'actual':1,'passed':True}
        native = {'kind':'actual-native-full-roster-online-campaign','checks':[check],'passed':1,'failed':0,'transport':'pinned-TLS-and-room-key'}
        migration = {**native,'originalManagedAssemblySha256':publish.OLD_H_DLL,'sourceBuildVersion':'0.10.2h',
                     'candidateSourceRevision':self.revision,'gameplayCommandsSubmittedToCopiedRoom':0}
        summary = {'format':'ascendance-full-roster-campaign-acceptance-1','runtimeSourceRevision':self.revision,
                   'runtimeBuildVersion':'0.10.2h','serverDeploymentRequired':True,'privateDataPublished':False,
                   'builds':{'windowsShipping':'SUCCEEDED','linuxDevelopment':'SUCCEEDED','linuxServer':'SUCCEEDED',
                             'cleanSource':True,'windowsDevelopmentDriversPresent':False},
                   'pythonHelperFixtures':{'passed':1,'failed':0,'skipped':0},
                   'screenshotsVisuallyReviewed':True,'framesAdvancedBeforeCapture':15}
        editor = {'passed':True,'errorCount':0,'errors':[],'buildVersion':'0.10.2h','editorVersion':'6000.6.5f1',
                  'gates':[{'name':'fixture','passed':True,'expectedChecks':1,'observedChecks':1}]}
        reports = {'acceptance-summary.json':package.canonical(summary),'editor-gates.json':package.canonical(editor),
                   'native-full-campaign-tls.json':package.canonical(native),'native-h-campaign-restore.json':package.canonical(migration),
                   'windows-build-provenance.json':(build.windows/'Ascendance.provenance.json').read_bytes(),
                   'linux-development-build-provenance.json':package.canonical({'schemaVersion':1,'outcome':'success','exitCode':0,
                       'source':{'vcs':'git','revision':self.revision,'dirty':False},'build':{'target':'StandaloneLinux64'}})}
        with zipfile.ZipFile(self.assets/publish.REPORTS,'x') as archive:
            for name,data in reports.items(): package.add_zip(archive,name,data)
        self.inventory = {'format':1,'releaseId':123,'release':self.tag,'sourceRevision':self.revision,'deploymentRevision':self.tooling,
                          'clientOnly':False,'launcherSha256':publish.digest((ROOT/'tools/Play-AscendanceMultiplayer.ps1').read_bytes()),
                          'acceptance':{'editorGates':{'fixture':1},'helperChecks':1,'graphicalChecks':1,'migrationChecks':1,
                                        'plainServerChecks':1,'tlsServerChecks':1},
                          'reports':{name:{'sha256':publish.digest(data),'bytes':len(data)} for name,data in reports.items()}}
        self.write_inventory()
        self.root_patch = patch.object(publish,'ROOT',self.root)
        self.root_patch.start(); self.addCleanup(self.root_patch.stop)
        self.git_patch = patch.object(publish.subprocess,'check_output',side_effect=self.git_blob)
        self.git_mock = self.git_patch.start(); self.addCleanup(self.git_patch.stop)
        self.env = patch.dict(os.environ,{'REVIEWED_INVENTORY_SHA256':self.inventory_sha})
        self.env.start(); self.addCleanup(self.env.stop)

    def git_blob(self,command,**kwargs):
        self.assertEqual(command[:2],['git','show'])
        revision,relative = command[2].split(':',1)
        self.assertEqual(revision,self.tooling)
        return (self.root/relative).read_bytes()

    def write_inventory(self):
        sums = {p.name:publish.digest(p.read_bytes()) for p in self.assets.iterdir() if p.name != 'SHA256SUMS'}
        (self.assets/'SHA256SUMS').write_text(''.join(sha+'  '+name+'\n' for name,sha in sorted(sums.items())))
        self.inventory['assets'] = {p.name:{'bytes':p.stat().st_size,'sha256':publish.digest(p.read_bytes())} for p in self.assets.iterdir()}
        data = package.canonical(self.inventory)
        (self.root/'distribution/reviewed-campaign-upload.json').write_bytes(data)
        self.inventory_sha = publish.digest(data)

    def test_complete_full_release_is_verified_without_network_or_mutation(self):
        before = {p.name:p.read_bytes() for p in self.assets.iterdir()}
        inventory,assets = publish.verify()
        self.assertEqual(inventory,self.inventory)
        self.assertEqual(assets,self.assets)
        self.assertEqual(before,{p.name:p.read_bytes() for p in self.assets.iterdir()})
        self.assertEqual(self.git_mock.call_count,len(publish.HELPERS))

    def test_missing_freeze_or_changed_inventory_refused(self):
        for checksum in ['', '0'*64]:
            with self.subTest(checksum=checksum),patch.dict(os.environ,{'REVIEWED_INVENTORY_SHA256':checksum}):
                with self.assertRaises(SystemExit):publish.verify()
        self.git_mock.assert_not_called()

    def test_tampered_asset_or_extra_file_refused_before_helpers(self):
        (self.assets/'CONNECTION_README.txt').write_text('changed')
        with self.assertRaises(SystemExit):publish.verify()
        self.git_mock.assert_not_called()
        (self.assets/'unexpected.json').write_text('{}')
        with self.assertRaises(SystemExit):publish.verify()

    def test_wrong_descriptor_or_helper_hash_cannot_be_frozen_as_reviewed(self):
        path=self.assets/'release.json'; original=json.loads(path.read_bytes())
        for key,value in [('sourceRevision','c'*40),('endpoint','other.example:7777')]:
            changed=copy.deepcopy(original);changed[key]=value;path.write_bytes(package.canonical(changed))
            self.write_inventory()
            with patch.dict(os.environ,{'REVIEWED_INVENTORY_SHA256':self.inventory_sha}),self.assertRaises(SystemExit):publish.verify()
        changed=copy.deepcopy(original);changed['helpers']['release.py']['sha256']='0'*64;path.write_bytes(package.canonical(changed))
        self.write_inventory()
        with patch.dict(os.environ,{'REVIEWED_INVENTORY_SHA256':self.inventory_sha}),self.assertRaises(SystemExit):publish.verify()

    def test_execution_success_without_actual_expected_effects_is_refused(self):
        for report in [
            {'passed':1,'failed':0,'checks':[{'passed':True,'expected':20,'actual':0}]},
            {'passed':1,'failed':0,'checks':[]},
            {'passed':1,'failed':0,'checks':[{'passed':False,'expected':20,'actual':20}]},
        ]:
            with self.subTest(report=report),self.assertRaises(SystemExit):publish.checks_passed(report,1,'fixture')

    def test_zip_case_collision_and_private_owner_token_refused(self):
        for label,files in [('collision',{'Folder/A':b'a','folder/a':b'b'}),
                            ('private',{'settings.json':b'{"CampaignOwnerToken0102":"private-fixture"}'})]:
            path=self.root/(label+'.zip')
            with zipfile.ZipFile(path,'x') as archive:
                for name,data in files.items(): package.add_zip(archive,name,data)
            with self.subTest(label=label),self.assertRaises((SystemExit,package.Refusal)):
                publish.zip_files(path,package)

    def test_upload_refuses_published_or_wrong_target_without_uploading(self):
        target={'id':123,'tag_name':self.tag,'target_commitish':self.revision,'draft':True,'prerelease':True,'assets':[]}
        for field,value in [('draft',False),('tag_name','other'),('target_commitish','c'*40)]:
            changed={**target,field:value}
            with patch.object(publish.subprocess,'check_output',return_value=json.dumps(changed)),patch.object(publish.subprocess,'run') as run:
                with self.subTest(field=field),self.assertRaises(SystemExit):publish.upload(self.inventory,self.assets)
                run.assert_not_called()

    def test_upload_refuses_existing_mismatched_bytes_without_clobber(self):
        name=next(iter(self.inventory['assets']))
        target={'id':123,'tag_name':self.tag,'target_commitish':self.revision,'draft':True,'prerelease':True,
                'assets':[{'name':name,'size':self.inventory['assets'][name]['bytes'],'digest':'sha256:'+'0'*64}]}
        with patch.object(publish.subprocess,'check_output',return_value=json.dumps(target)),patch.object(publish.subprocess,'run') as run:
            with self.assertRaises(SystemExit):publish.upload(self.inventory,self.assets)
            run.assert_not_called()

    def test_upload_only_missing_assets_keeps_exact_draft_and_checks_tag(self):
        assets=[{'name':name,'size':proof['bytes'],'digest':'sha256:'+proof['sha256']} for name,proof in self.inventory['assets'].items()]
        target={'id':123,'tag_name':self.tag,'target_commitish':self.revision,'draft':True,'prerelease':True,'assets':assets[:-1]}
        ref={'ref':'refs/tags/'+self.tag,'object':{'type':'commit','sha':self.revision}}
        complete={**target,'assets':assets}
        with patch.object(publish.subprocess,'check_output',side_effect=[json.dumps(target),json.dumps(ref),json.dumps(complete)]),patch.object(publish.subprocess,'run') as run:
            publish.upload(self.inventory,self.assets)
            run.assert_called_once_with(['gh','release','upload',self.tag,'--repo',publish.REPOSITORY,str(self.assets/assets[-1]['name'])],check=True)
        ref['object']['sha']='c'*40
        with patch.object(publish.subprocess,'check_output',side_effect=[json.dumps(target),json.dumps(ref)]),patch.object(publish.subprocess,'run') as run:
            with self.assertRaises(SystemExit):publish.upload(self.inventory,self.assets)
            run.assert_not_called()


if __name__=='__main__':unittest.main()
