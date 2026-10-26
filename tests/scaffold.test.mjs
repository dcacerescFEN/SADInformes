import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';
import test from 'node:test';

const root = fileURLToPath(new URL('../', import.meta.url));
const read = (path) => readFileSync(resolve(root, path), 'utf8');
const json = (path) => JSON.parse(read(path));

test('La solución contiene las cuatro capas del backend y el proyecto WebUI', () => {
  const projects = [...read('SADInformes.slnx').matchAll(/<Project Path="([^"]+)"/g)]
    .map((match) => match[1]).sort();
  assert.deepEqual(projects, [
    'src/Application/Application.csproj', 'src/Domain/Domain.csproj',
    'src/Infrastructure/Infrastructure.csproj', 'src/Web/WebApi/WebApi.csproj',
    'src/Web/WebUI/WebUI.esproj',
  ]);
});

test('Las capas .NET 10 solo referencian capas interiores, sin paquetes funcionales', () => {
  const layers = {
    Domain: [], Application: ['Domain'],
    Infrastructure: ['Application'], 'Web/WebApi': ['Application', 'Infrastructure'],
  };
  for (const [layer, references] of Object.entries(layers)) {
    const name = layer.split('/').at(-1);
    const project = read(`src/${layer}/${name}.csproj`);
    assert.match(project, /<TargetFramework>net10\.0<\/TargetFramework>/);
    const actual = [...project.matchAll(/<ProjectReference Include="[^"]*\/([^/"]+)\.csproj"/g)]
      .map((match) => match[1]).sort();
    assert.deepEqual(actual, references);
    if (layer === 'Web/WebApi') {
      assert.deepEqual(
        [...project.matchAll(/<PackageReference Include="([^"]+)"/g)].map((match) => match[1]),
        ['Microsoft.AspNetCore.SpaProxy'],
      );
    } else {
      assert.doesNotMatch(project, /<PackageReference/);
    }
  }
});

test('WebApi referencia WebUI sin tratarlo como un ensamblado .NET', () => {
  const api = read('src/Web/WebApi/WebApi.csproj');
  assert.match(api, /<ProjectReference Include="\.\.\/WebUI\/WebUI\.esproj">\s*<ReferenceOutputAssembly>false<\/ReferenceOutputAssembly>\s*<\/ProjectReference>/);
  const ui = read('src/Web/WebUI/WebUI.esproj');
  assert.match(ui, /Microsoft\.VisualStudio\.JavaScript\.Sdk\/0\.5\.127-alpha/);
  assert.match(ui, /<StartupCommand>pnpm start<\/StartupCommand>/);
  assert.match(ui, /<BuildCommand>pnpm build<\/BuildCommand>/);
  assert.match(ui, /<ShouldRunNpmInstall>false<\/ShouldRunNpmInstall>/);
  assert.match(ui, /<ShouldRunBuildScript>false<\/ShouldRunBuildScript>/);
  assert.doesNotMatch(ui, /npm install -g/);
});

test('El perfil de WebApi activa el SPA proxy hacia el frontend configurado', () => {
  const api = read('src/Web/WebApi/WebApi.csproj');
  assert.match(api, /<SpaRoot>\.\.\/WebUI\/<\/SpaRoot>/);
  assert.match(api, /<SpaProxyLaunchCommand>pnpm start<\/SpaProxyLaunchCommand>/);
  assert.match(api, /<SpaProxyServerUrl>http:\/\/127\.0\.0\.1:4206<\/SpaProxyServerUrl>/);
  const profile = json('src/Web/WebApi/Properties/launchSettings.json').profiles.http;
  assert.equal(profile.environmentVariables.ASPNETCORE_HOSTINGSTARTUPASSEMBLIES,
    'Microsoft.AspNetCore.SpaProxy');
  assert.equal(profile.applicationUrl, 'http://localhost:5086');
  const proxy = json('src/Web/WebUI/src/proxy.conf.json');
  assert.equal(proxy['/api'].target, profile.applicationUrl);
  assert.match(json('src/Web/WebUI/package.json').scripts.start, /--port 4206/);
});

test('El frontend usa Angular 22 sin dependencias funcionales de alumnos', () => {
  const packageJson = json('src/Web/WebUI/package.json');
  assert.equal(packageJson.name, 'sadinformes');
  assert.match(packageJson.dependencies['@angular/core'], /^22\./);
  assert.deepEqual(Object.keys(packageJson.dependencies).sort(), [
    '@angular/common', '@angular/compiler', '@angular/core',
    '@angular/platform-browser', '@angular/router', 'rxjs', 'tslib',
  ]);
  assert.equal(packageJson.scripts.build, 'ng build');
});

test('La configuración inicial no requiere base de datos ni autenticación', () => {
  assert.deepEqual(Object.keys(json('src/Web/WebApi/appsettings.json')).sort(),
    ['AllowedHosts', 'Logging']);
});
