import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { once } from 'node:events';
import assert from 'node:assert/strict';
import test from 'node:test';

test('La API mantiene health y publica configuración, sin permitir una sesión anónima', { timeout: 15000 }, async (t) => {
  const dll = fileURLToPath(new URL('../src/Web/WebApi/bin/Debug/net10.0/WebApi.dll', import.meta.url));
  const child = spawn('dotnet', [dll, '--urls', 'http://127.0.0.1:0'], {
    cwd: fileURLToPath(new URL('../src/Web/WebApi/', import.meta.url)),
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let output = '';
  let errors = '';
  child.stderr.on('data', (data) => { errors += data; });
  t.after(async () => {
    if (child.exitCode === null && child.signalCode === null) {
      const exited = once(child, 'exit');
      child.kill();
      await exited;
    }
  });
  const url = await new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('La API no arrancó: ' + output + errors)), 10000);
    const fail = (error) => { clearTimeout(timer); reject(error); };
    child.once('error', fail);
    child.once('exit', (code) => fail(new Error('La API terminó antes de arrancar: ' + code + output + errors)));
    child.stdout.on('data', (data) => {
      output += data;
      const match = output.match(/Now listening on: (http:\/\/127\.0\.0\.1:\d+)/);
      if (match) {
        clearTimeout(timer);
        resolve(match[1]);
      }
    });
  });
  const health = await fetch(url + '/api/health');
  assert.equal(health.status, 200);
  assert.match(health.headers.get('content-type'), /application\/json/);
  assert.deepEqual(await health.json(), { status: 'ok', application: 'SADInformes' });
  for (const path of ['/api/Students', '/api/auth', '/api/reports']) {
    assert.equal((await fetch(url + path)).status, 404, path);
  }
  const config = await fetch(url + '/api/config/keycloak');
  assert.equal(config.status, 200);
  assert.deepEqual(await config.json(), {
    url: 'https://sso-dev.fen.uchile.cl', realm: 'FenUChile', clientId: 'SADInformes',
  });
  const session = await fetch(url + '/api/auth/me');
  assert.equal(session.status, 401);
  assert.equal(await session.text(), '');
  const menu = await fetch(url + '/api/config/site');
  assert.equal(menu.status, 401);
  assert.equal(await menu.text(), '');
  assert.equal(errors, '');
});
