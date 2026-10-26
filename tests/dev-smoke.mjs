import { spawn, execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { request } from 'node:https';
import { connect } from 'node:net';
import { fileURLToPath } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';
import { once } from 'node:events';
import assert from 'node:assert/strict';
import test from 'node:test';

const api = 'https://localhost:7070';
const frontend = 'https://localhost:4200';
const certificatePath = fileURLToPath(new URL('../src/Web/WebUI/.certs/localhost.pem', import.meta.url));

function fetchLocal(url) {
  return new Promise((resolve, reject) => {
    const req = request(url, { ca: readFileSync(certificatePath), timeout: 5000 }, (response) => {
      let body = '';
      response.setEncoding('utf8');
      response.on('data', (chunk) => { body += chunk; });
      response.on('error', reject);
      response.on('end', () => resolve({
        status: response.statusCode,
        headers: { get: (name) => response.headers[name.toLowerCase()] },
        text: async () => body,
        json: async () => JSON.parse(body),
      }));
    });
    req.on('timeout', () => req.destroy(new Error('Tiempo de espera HTTPS agotado')));
    req.on('error', reject);
    req.end();
  });
}

async function requireFreePort(port) {
  await new Promise((resolve, reject) => {
    const socket = connect({ host: '127.0.0.1', port });
    socket.once('connect', () => {
      socket.destroy();
      reject(new Error('Puerto ocupado; no se modificará el proceso existente: ' + port));
    });
    socket.once('error', (error) => {
      if (error.code === 'ECONNREFUSED') resolve();
      else reject(error);
    });
  });
}

test('WebApi inicia Angular y el frontend puede consultar la API', { timeout: 120000 }, async (t) => {
  await requireFreePort(7070);
  await requireFreePort(4200);
  await requireFreePort(5086);
  execFileSync(process.execPath, [
    fileURLToPath(new URL('../src/Web/WebUI/aspnetcore-https.js', import.meta.url)),
  ], { stdio: 'inherit' });
  const child = spawn('dotnet', ['run', '--no-build', '--launch-profile', 'https'], {
    cwd: fileURLToPath(new URL('../src/Web/WebApi/', import.meta.url)),
    detached: process.platform !== 'win32',
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let output = '';
  child.stdout.on('data', (data) => { output += data; });
  child.stderr.on('data', (data) => { output += data; });
  t.after(async () => {
    if (child.exitCode === null && child.signalCode === null) {
      if (process.platform === 'win32') {
        const killer = spawn('taskkill.exe', ['/PID', String(child.pid), '/T', '/F']);
        const [code] = await once(killer, 'exit');
        assert.equal(code, 0, 'No se pudo cerrar el árbol de procesos de la prueba');
      } else {
        process.kill(-child.pid, 'SIGTERM');
        await once(child, 'exit');
      }
    }
  });

  const deadline = Date.now() + 90000;
  let ready = false;
  while (Date.now() < deadline) {
    assert.equal(child.exitCode, null, output);
    assert.equal(child.signalCode, null, output);
    try {
      // La primera petición hace que SPA proxy inicie el servidor Angular.
      const response = await fetchLocal(api);
      if (response.status === 302) {
        assert.equal(response.headers.get('location')?.replace(/\/$/, ''), frontend);
        ready = true;
        break;
      }
      assert.equal(response.status, 200, output);
      await response.text();
    } catch (error) {
      if (error.name === 'AssertionError') throw error;
    }
    await delay(300);
  }
  assert.ok(ready, 'SPA proxy no quedó listo: ' + output);
  const page = await fetchLocal(frontend);
  assert.equal(page.status, 200);
  const html = await page.text();
  assert.match(html, /<title>SADInformes<\/title>/);
  assert.match(html, /<app-root><\/app-root>/);
  const script = html.match(/<script src="([^"]*main\.js[^"]*)"/);
  assert.ok(script, 'Falta el script de inicio Angular');
  const javascript = await fetchLocal(new URL(script[1], frontend));
  assert.equal(javascript.status, 200);
  assert.match(javascript.headers.get('content-type'), /javascript/);
  const health = await fetchLocal(frontend + '/api/health');
  assert.equal(health.status, 200);
  assert.match(health.headers.get('content-type'), /application\/json/);
  assert.deepEqual(await health.json(), { status: 'ok', application: 'SADInformes' });
  const configuration = await fetchLocal(frontend + '/api/config/keycloak');
  assert.equal(configuration.status, 200);
  assert.deepEqual(await configuration.json(), {
    url: 'https://sso-dev.fen.uchile.cl', realm: 'FenUChile', clientId: 'SADInformes',
  });
  const logo = await fetchLocal(frontend + '/logo-fen-white-letters.svg');
  assert.equal(logo.status, 200);
  assert.match(logo.headers.get('content-type'), /image\/svg\+xml/);
  assert.match(await logo.text(), /<svg/);
  const menu = await fetchLocal(frontend + '/api/config/site');
  assert.equal(menu.status, 401);
  assert.equal(await menu.text(), '');
  const anonymousSession = await fetchLocal(frontend + '/api/auth/me');
  assert.equal(anonymousSession.status, 401);
  assert.equal(await anonymousSession.text(), '');
});
