"""Deployment control-flow tests. All privileged commands/paths are replaced in a temporary sandbox."""
import json
import os
from pathlib import Path
import shlex
import sqlite3
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
OLD = 'a' * 40
NEW = 'b' * 40


class DeploymentTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.config = self.root / 'config'
        self.state = self.root / 'state'
        self.bin = self.root / 'bin'
        for path in (self.config, self.state / 'data', self.state / 'backups', self.bin):
            path.mkdir(parents=True, exist_ok=True)
        for name in ('compose.yaml', 'api.env'):
            (self.config / name).write_text('# fixture\n')
        (self.config / 'deployment.env').write_text('IMAGE_PREFIX=ghcr.io/test/portfolio\nPUBLIC_HOST=example.com\n')
        self.database = self.state / 'data/portfolio.db'
        with sqlite3.connect(self.database) as db:
            db.execute('CREATE TABLE records (value INTEGER)')
            db.execute('INSERT INTO records VALUES (1)')
        (self.state / 'current').write_text(OLD)
        self.environment = dict(os.environ, TEST_ROOT=str(self.root), TEST_FAILURE='')
        fake = '''#!/usr/bin/python3
import json, os, pathlib, signal, sqlite3, subprocess, sys
root = pathlib.Path(os.environ['TEST_ROOT'])
command = pathlib.Path(sys.argv[0]).name
args = sys.argv[1:]
with (root/'events').open('a') as log: log.write(json.dumps([command, args, os.environ.get('RELEASE_SHA')])+'\\n')
if command == 'stat':
    if args[1] == '%u': print(0)
    else: sys.exit(subprocess.call(['/usr/bin/stat', *args]))
elif command == 'systemctl': sys.exit(0 if os.environ.get('TEST_OLD_ACTIVE') else 3)
elif command == 'docker':
    if args[:2] == ['image', 'inspect']: print(args[2].rsplit(':', 1)[1])
    elif 'run' in args:
        with sqlite3.connect(root/'state/data/portfolio.db') as db:
            db.execute('CREATE TABLE IF NOT EXISTS records (value INTEGER)')
            db.execute('DELETE FROM records')
            db.execute('INSERT INTO records VALUES (2)')
        if os.environ['TEST_FAILURE'] == 'import': sys.exit(5)
        if os.environ['TEST_FAILURE'] == 'hangup': os.kill(os.getppid(), signal.SIGHUP)
elif command == 'curl':
    if os.environ['TEST_FAILURE'] == 'https' and any(x.startswith('https://') for x in args): sys.exit(22)
    print('Blazor-WebAssembly-Component-State: fixture')
'''
        for command in ('stat', 'systemctl', 'docker', 'curl', 'sleep', 'chown'):
            path = self.bin / command
            path.write_text(fake)
            path.chmod(0o755)
        script = (ROOT / 'deploy/deploy.sh').read_text()
        # Production entry remains root-only, fixed-PATH and fixed-location. Only this isolated copy is patched.
        script = script.replace('if [[ $EUID != 0 ]]', 'if false', 1)
        script = script.replace('export PATH=/usr/sbin:/usr/bin:/sbin:/bin',
                                f'export PATH={shlex.quote(str(self.bin))}:/usr/sbin:/usr/bin:/sbin:/bin', 1)
        script = script.replace('/etc/monatrox-portfolio', str(self.config))
        script = script.replace('/var/lib/monatrox-portfolio', str(self.state))
        self.script = self.root / 'deploy.sh'
        self.script.write_text(script)

    def run_deploy(self, sha=NEW):
        return subprocess.run(['bash', str(self.script), sha], env=self.environment,
                              text=True, capture_output=True)

    def value(self):
        with sqlite3.connect(self.database) as db:
            return db.execute('SELECT value FROM records').fetchone()[0]

    def events(self):
        path = self.root / 'events'
        return [json.loads(line) for line in path.read_text().splitlines()] if path.exists() else []

    def test_success_backs_up_database_then_records_release(self):
        result = self.run_deploy()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.value(), 2)
        self.assertEqual((self.state / 'current').read_text().strip(), NEW)
        backup, = (self.state / 'backups').glob('*.db')
        with sqlite3.connect(backup) as db:
            self.assertEqual(db.execute('SELECT value FROM records').fetchone()[0], 1)

    def test_partial_import_failure_restores_data_and_previous_images(self):
        self.environment['TEST_FAILURE'] = 'import'
        result = self.run_deploy()
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.value(), 1)
        self.assertEqual((self.state / 'current').read_text().strip(), OLD)
        self.assertIn('previous images and pre-deployment database restored', result.stderr)
        self.assertTrue(any(command == 'docker' and 'up' in args and release == OLD
                            for command, args, release in self.events()))

    def test_ssh_hangup_during_import_restores_previous_release(self):
        self.environment['TEST_FAILURE'] = 'hangup'
        result = self.run_deploy()
        self.assertEqual(result.returncode, 129)
        self.assertEqual(self.value(), 1)
        self.assertEqual((self.state / 'current').read_text().strip(), OLD)

    def test_https_failure_rolls_back_even_after_containers_start(self):
        self.environment['TEST_FAILURE'] = 'https'
        result = self.run_deploy()
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.value(), 1)
        self.assertEqual((self.state / 'current').read_text().strip(), OLD)

    def test_failed_first_import_leaves_no_partial_database(self):
        self.database.unlink()
        (self.state / 'current').unlink()
        self.environment['TEST_FAILURE'] = 'import'
        result = self.run_deploy()
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(self.database.exists())
        self.assertIn('Administrator must restart old frontend', result.stderr)

    def test_old_frontend_is_never_stopped_implicitly(self):
        self.environment['TEST_OLD_ACTIVE'] = '1'
        result = self.run_deploy()
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(any(command == 'docker' for command, _, _ in self.events()))
        self.assertIn('nothing stopped', result.stderr)

    def test_writable_configuration_is_rejected(self):
        (self.config / 'compose.yaml').chmod(0o666)
        self.assertNotEqual(self.run_deploy().returncode, 0)
        self.assertFalse(any(command == 'docker' for command, _, _ in self.events()))

    def test_invalid_release_rejected_before_privileged_commands(self):
        for value in ('main', '-help', NEW + ';id', NEW + '\n', 'A' * 40):
            with self.subTest(value=value):
                self.assertEqual(self.run_deploy(value).returncode, 2)
        self.assertEqual(self.events(), [])

    def test_ssh_dispatch_accepts_only_exact_deploy_command(self):
        sudo = self.bin / 'sudo'
        sudo.write_text('#!/bin/sh\nprintf "%s\\n" "$@"\n')
        sudo.chmod(0o755)
        environment = dict(os.environ, PATH=f'{self.bin}:/usr/bin:/bin')
        for command in ('', 'deploy main', 'deploy ' + NEW + ';id', 'deploy ' + NEW + '\n',
                        'deploy ' + NEW + ' extra', ' deploy ' + NEW):
            with self.subTest(command=command):
                result = subprocess.run(['bash', str(ROOT / 'deploy/dispatch.sh')],
                                        env=dict(environment, SSH_ORIGINAL_COMMAND=command), capture_output=True)
                self.assertEqual(result.returncode, 2)
        result = subprocess.run(['bash', str(ROOT / 'deploy/dispatch.sh')],
                                env=dict(environment, SSH_ORIGINAL_COMMAND='deploy ' + NEW),
                                text=True, capture_output=True)
        self.assertEqual(result.returncode, 0)
        self.assertEqual(result.stdout.splitlines(), ['-n', '/usr/local/sbin/monatrox-portfolio-deploy', NEW])
