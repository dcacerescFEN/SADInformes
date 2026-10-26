'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');

const folder = path.join(__dirname, '.certs');
const certificate = path.join(folder, 'localhost.pem');
const key = path.join(folder, 'localhost.key');

if (!fs.existsSync(certificate) || !fs.existsSync(key)) {
  fs.mkdirSync(folder, { recursive: true, mode: 0o700 });
  const result = spawnSync('dotnet', [
    'dev-certs', 'https', '--export-path', certificate, '--format', 'Pem', '--no-password',
  ], { stdio: 'inherit' });
  if (result.error || result.status !== 0) {
    console.error('No se pudo exportar el certificado de desarrollo HTTPS.');
    process.exit(result.status || 1);
  }
}
