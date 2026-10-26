import { spawn } from 'node:child_process';
import { connect } from 'node:net';
import { fileURLToPath } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';
import { once } from 'node:events';
import assert from 'node:assert/strict';
import test from 'node:test';

const api = 'http://localhost:5086';
const frontend = 'http://127.0.0.1:4206';

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
  await requireFreePort(5086);
  await requireFreePort(4206);
  const child = spawn('dotnet', ['run', '--no-build', '--launch-profile', 'http'], {
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
      const response = await fetch(api, {
        redirect: 'manual', signal: AbortSignal.timeout(2000),
      });
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
  const page = await fetch(frontend, { signal: AbortSignal.timeout(5000) });
  assert.equal(page.status, 200);
  const html = await page.text();
  assert.match(html, /<title>SADInformes<\/title>/);
  assert.match(html, /<app-root><\/app-root>/);
  const script = html.match(/<script src="([^"]*main\.js[^"]*)"/);
  assert.ok(script, 'Falta el script de inicio Angular');
  const javascript = await fetch(new URL(script[1], frontend), { signal: AbortSignal.timeout(5000) });
  assert.equal(javascript.status, 200);
  assert.match(javascript.headers.get('content-type'), /javascript/);
  const health = await fetch(frontend + '/api/health', { signal: AbortSignal.timeout(5000) });
  assert.equal(health.status, 200);
  assert.match(health.headers.get('content-type'), /application\/json/);
  assert.deepEqual(await health.json(), { status: 'ok', application: 'SADInformes' });
});
