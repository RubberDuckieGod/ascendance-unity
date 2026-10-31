"""Synthetic archives only; these tests never execute Unity, upload or deploy."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import stat
import tempfile
from types import SimpleNamespace
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('multiplayer_packaging', ROOT/'tools/package_multiplayer_release.py')
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)


class MultiplayerPackaging(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='asc-public-package-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.linux, self.windows = self.root/'LinuxServer', self.root/'Windows'
        self.linux.mkdir(); self.windows.mkdir()
        elf = b'\x7fELF\x02\x01' + bytes(12) + b'\x3e\x00' + b'never executed fixture'
        for name in ['AscendanceServer.x86_64','UnityPlayer.so']:
            (self.linux/name).write_bytes(elf)
        (self.linux/'AscendanceServer_Data').mkdir()
        (self.linux/'AscendanceServer_Data/globalgamemanagers').write_bytes(b'complete fixture data')
        for name in ['Ascendance.exe','UnityPlayer.dll']:
            (self.windows/name).write_bytes(b'MZ never executed fixture')
        (self.windows/'Ascendance_Data').mkdir()
        (self.windows/'Ascendance_Data/globalgamemanagers').write_bytes(b'complete fixture data')
        self.sha = 'a'*40
        for build, filename, target in [(self.linux,'AscendanceServer.provenance.json','StandaloneLinux64'),(self.windows,'Ascendance.provenance.json','StandaloneWindows64')]:
            (build/filename).write_text(json.dumps({'outcome':'success','exitCode':0,'build':{'target':target},'source':{'revision':self.sha,'dirty':False}}))
        self.manifest = self.root/'manifest.json'
        self.manifest.write_text(json.dumps(package.release_tools().artifact_manifest(self.linux)))
        self.args = SimpleNamespace(linux_build=self.linux, windows_build=self.windows, linux_manifest=self.manifest, source_revision=self.sha, release='test-release', output=self.root/'public')

    def test_complete_packages_checksums_provenance_and_launcher_layout(self):
        result = package.package(self.args)
        self.assertEqual(result['sourceRevision'],self.sha)
        manifest = json.loads(self.manifest.read_text())
        serverzip = self.args.output/'ascendance-linux-server-test-release.zip'
        with zipfile.ZipFile(serverzip) as archive:
            self.assertEqual(set(archive.namelist()),{'LinuxServer/'+name for name in manifest['files']})
            for name, checksum in manifest['files'].items():
                self.assertEqual(hashlib.sha256(archive.read('LinuxServer/'+name)).hexdigest(),checksum)
            mode=archive.getinfo('LinuxServer/AscendanceServer.x86_64').external_attr>>16
            self.assertTrue(mode&stat.S_IXUSR)
        with zipfile.ZipFile(self.args.output/'ascendance-windows-test-release.zip') as archive:
            for name in ['Ascendance.exe','UnityPlayer.dll','Play-AscendanceMultiplayer.ps1','Play-AscendanceMultiplayer.cmd','CONNECTION_README.txt']:
                self.assertIn('Windows/'+name,archive.namelist())
            self.assertFalse(any('invite.json' in name.lower() for name in archive.namelist()))
            instructions = archive.read('Windows/CONNECTION_README.txt').decode()
            self.assertEqual(instructions, (self.args.output/'CONNECTION_README.txt').read_text())
            for step in ['JOIN ONLINE CAMPAIGN', 'NEW ONLINE', 'all 16 Heroes', 'READY', 'START CAMPAIGN', 'SAVED CAMPAIGNS']:
                self.assertIn(step, instructions)
            self.assertIn('Updating\nthe software alone keeps the existing campaign active', instructions)
        proof=json.loads((self.args.output/'RuntimeSourceRevision.json').read_text())
        self.assertEqual(proof['RuntimeSourceRevision'],self.sha)
        self.assertEqual(package.extract_validated_server(serverzip,self.manifest,self.root/'extracted',self.sha),self.root/'extracted')
        self.assertEqual(package.release_tools().artifact_manifest(self.root/'extracted'),manifest)

    def test_dirty_or_wrong_source_provenance_never_writes_release(self):
        path=self.windows/'Ascendance.provenance.json'
        original=json.loads(path.read_text())
        for field,value in [('dirty',True),('revision','b'*40)]:
            with self.subTest(field=field):
                proof=copy.deepcopy(original);proof['source'][field]=value;path.write_text(json.dumps(proof))
                with self.assertRaises(Exception):package.package(self.args)
                self.assertFalse(self.args.output.exists())

    def test_private_filename_nested_credentials_duplicate_json_and_private_key_refused(self):
        cases=[('Ascendance.invite.json',b'{}'),('server.pfx',b'private binary fixture'),('settings.json',b'{"nested":{"accessKey":"private-fixture"}}'),('settings.json',b'{"CampaignOwnerToken0102":"private-fixture"}'),('settings.json',b'{"password":"private-fixture","password":""}'),('notes.txt',b'-----BEGIN PRIVATE KEY-----\nprivate fixture')]
        for name,content in cases:
            with self.subTest(name=name,content=content[:20]):
                path=self.windows/name;path.write_bytes(content)
                with self.assertRaises(Exception):package.package(self.args)
                self.assertFalse(self.args.output.exists());path.unlink()

    def test_tampered_build_symlinks_case_collisions_and_existing_output_refused(self):
        original=(self.linux/'UnityPlayer.so').read_bytes()
        (self.linux/'UnityPlayer.so').write_bytes(b'tampered')
        with self.assertRaises(Exception):package.package(self.args)
        self.assertFalse(self.args.output.exists())
        (self.linux/'UnityPlayer.so').write_bytes(original)
        for relative in ['alias','ASCENDANCE.EXE']:
            path=self.windows/relative
            if relative=='alias':path.symlink_to(self.windows/'Ascendance.exe')
            else:path.write_bytes(b'MZ case collision fixture')
            with self.assertRaises(Exception):package.snapshot(self.windows,package.release_tools()) if relative=='alias' else package.package(self.args)
            path.unlink()
        self.args.output.mkdir()
        with self.assertRaises(Exception):package.package(self.args)

    def test_untrusted_archive_traversal_link_extra_and_duplicate_entries_refused_before_extract(self):
        for label,names in [('traversal',['LinuxServer/../escape']),('extra',['Windows/Ascendance.exe']),('duplicate',['LinuxServer/file','LinuxServer/file']),('link',['LinuxServer/link'])]:
            with self.subTest(label=label):
                archive=self.root/(label+'.zip')
                with zipfile.ZipFile(archive,'w') as target:
                    for name in names:
                        info=zipfile.ZipInfo(name)
                        if label=='link':info.external_attr=(stat.S_IFLNK|0o777)<<16
                        target.writestr(info,b'never executed')
                destination=self.root/('extract-'+label)
                with self.assertRaises(Exception):package.extract_validated_server(archive,self.manifest,destination,self.sha)
                self.assertFalse(destination.exists())


if __name__=='__main__':unittest.main()
