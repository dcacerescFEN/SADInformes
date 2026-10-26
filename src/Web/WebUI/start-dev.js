'use strict';

const path = require('node:path');
const { spawn } = require('node:child_process');

const certificate = path.join(__dirname, '.certs', 'localhost.pem');
const key = path.join(__dirname, '.certs', 'localhost.key');
const cli = path.join(__dirname, 'node_modules', '@angular', 'cli', 'bin', 'ng.js');

const child = spawn(process.execPath, [
  cli, 'serve', '--ssl', '--ssl-cert', certificate, '--ssl-key', key,
  '--host', 'localhost', '--port', '4200',
], {
  cwd: __dirname,
  stdio: 'inherit',
  env: { ...process.env, NODE_EXTRA_CA_CERTS: certificate },
});
child.on('error', () => {
  console.error('No se pudo iniciar Angular con HTTPS.');
  process.exit(1);
});
child.on('exit', (code) => process.exit(code ?? 1));
