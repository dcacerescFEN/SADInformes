import { existsSync, readFileSync } from 'node:fs';
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
    'src/Web/WebUI/WebUI.esproj', 'tests/WebApi.Tests/WebApi.Tests.csproj',
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
        ['Microsoft.AspNetCore.SpaProxy', 'Microsoft.AspNetCore.Authentication.JwtBearer',
          'Microsoft.AspNetCore.OpenApi', 'Scalar.AspNetCore'],
      );
    } else if (layer === 'Application') {
      assert.match(project, /PackageReference Include="Scrutor" Version="7.0.0"/);
    } else if (layer === 'Infrastructure') {
      assert.match(project, /PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.12"/);
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
  assert.match(api, /<SpaProxyServerUrl>https:\/\/localhost:4200<\/SpaProxyServerUrl>/);
  const profile = json('src/Web/WebApi/Properties/launchSettings.json').profiles.https;
  assert.equal(profile.environmentVariables.ASPNETCORE_HOSTINGSTARTUPASSEMBLIES,
    'Microsoft.AspNetCore.SpaProxy');
  assert.ok(profile.applicationUrl.split(';').includes('https://localhost:7070'));
  const proxy = json('src/Web/WebUI/src/proxy.conf.json');
  assert.equal(proxy['/api'].target, 'https://localhost:7070');
  const scripts = json('src/Web/WebUI/package.json').scripts;
  assert.equal(scripts.prestart, 'node aspnetcore-https.js');
  assert.equal(scripts.start, 'node start-dev.js');
  const launcher = read('src/Web/WebUI/start-dev.js');
  assert.match(launcher, /'--ssl'/);
  assert.match(launcher, /'--port', '4200'/);
  assert.match(launcher, /'--host', 'localhost'/);
  const ignore = read('.gitignore');
  assert.match(ignore, /\*\*\/\.certs\//);
});

test('Los perfiles de desarrollo abren Scalar como los proyectos de referencia', () => {
  const profiles = json('src/Web/WebApi/Properties/launchSettings.json').profiles;
  for (const name of ['https', 'http']) {
    assert.equal(profiles[name].launchBrowser, true, name);
    assert.equal(profiles[name].launchUrl, 'scalar', name);
    assert.equal(profiles[name].environmentVariables.ASPNETCORE_ENVIRONMENT, 'Development');
  }
});

test('AppConfig se carga directamente sin lector ni interfaz redundantes', () => {
  assert.equal(existsSync(resolve(root, 'src/Infrastructure/Configuration/AppConfigConfigurationReader.cs')), false);
  assert.equal(existsSync(resolve(root, 'src/Application/Configuration/IAppConfigurationReader.cs')), false);
  const initialiser = read('src/Infrastructure/Configuration/ConfigurationInitialiser.cs');
  assert.match(initialiser, /AppConfigContext configContext/);
  assert.match(initialiser, /configContext\.Configurations\.AsNoTracking\(\)/);
  assert.match(read('src/Infrastructure/ConfigureServices.cs'), /AddDbContext<AppConfigContext>/);
});

test('WebApi descubre IEndpoint e inicializa configuración antes de servir', () => {
  const program = read('src/Web/WebApi/Program.cs');
  assert.match(program, /\.AddEndpoints\(/);
  assert.match(program, /\.MapEndpoints\(/);
  assert.match(program, /await app\.InitialiseConfigurationAsync\(/);
  assert.doesNotMatch(program, /app\.MapGet\(|MapSiteConfiguration/);
  assert.match(read('src/Web/WebApi/Extension/IEndpoint.cs'), /interface IEndpoint/);
});

test('Application registra handlers por scan y Config conserva el patrón de NewSAD', () => {
  const registration = read('src/Application/ConfigureServices.cs');
  assert.match(registration, /AddApplicationServices/);
  assert.match(registration, /services\.Scan\(/);
  assert.doesNotMatch(read('src/Web/WebApi/ConfigureServices.cs'), /AddApplicationServices/);
  const site = read('src/Web/WebApi/Endpoints/Config/ConfigEndpoints.cs');
  assert.match(site, /IRequestDispatcher sender/);
  assert.match(site, /sender\.Send\(/);
  const keycloak = read('src/Web/WebApi/Endpoints/Config/PublicKeycloakConfigEndpoints.cs');
  assert.match(keycloak, /IConfiguration configuration/);
  assert.doesNotMatch(keycloak, /IRequestDispatcher/);
});

test('Publicar WebApi compila Angular y copia sus assets a wwwroot', () => {
  const project = read('src/Web/WebApi/WebApi.csproj');
  assert.match(project, /Target Name="PublishAngular"/);
  assert.match(project, /pnpm install --frozen-lockfile/);
  assert.match(project, /pnpm build/);
  assert.match(project, /dist\/webui\/browser/);
  assert.match(project, /ResolvedFileToPublish/);
  assert.match(project, /wwwroot/);
});

test('El SAD se consume con SadContext y procedimientos al estilo de las referencias', () => {
  const infrastructure = read('src/Infrastructure/ConfigureServices.cs');
  assert.match(infrastructure, /AddDbContext<SadContext>/);
  assert.ok(infrastructure.includes('GetConnectionString("Sad")'));
  assert.ok(infrastructure.includes('AddScoped<ISadContextProcedures, SadContextProcedures>()'));
  const context = read('src/Infrastructure/Data/SadContext.cs');
  assert.match(context, /DbContextOptions<SadContext>/);
  assert.match(context, /ISadContext/);
  const procedures = read('src/Infrastructure/Data/SadContextProcedures.cs');
  for (const sp of ['spRec_Encuesta_DocenteXCurso', 'spRec_PeriodosAcademicosProgramacion',
    'spRec_TiposAlumno', 'spRec_TiposAlumnosModalidades']) {
    assert.ok(procedures.includes('[dbo].[' + sp + ']'), sp);
  }
  assert.match(procedures, /SqlQueryAsync<SpRecEncuestaDocenteXCursoResult>/);
  assert.equal(existsSync(resolve(root, 'src/Infrastructure/Reports/Postgraduate/SqlTeacherSurveyReader.cs')), false);
});

test('Las pantallas de informes usan el page-header de las referencias', () => {
  for (const page of ['postgraduate/teacher-survey/teacher-survey.ts', 'postgraduate/postgraduate-placeholder.ts']) {
    const source = read('src/Web/WebUI/src/app/' + page);
    assert.ok(source.includes('container-fluid px-0 py-4'), page);
    assert.ok(source.includes('class=\"page-header\"'), page);
    assert.ok(source.includes('class=\"page-title\"'), page);
    assert.ok(source.includes('page-subtitle'), page);
  }
  const styles = read('src/Web/WebUI/src/styles.scss');
  assert.match(styles, /^.page-header,/m);
  assert.match(styles, /--card-radius/);
});

test('La navbar conserva breakpoints y espaciado de SADAlumnos', () => {
  const styles = read('src/Web/WebUI/src/app/core/layout/header.scss');
  assert.match(styles, /max-width:\s*991\.98px/);
  assert.match(styles, /max-width:\s*575\.98px/);
  assert.doesNotMatch(styles, /1199\.98px/);
  assert.match(styles, /padding:\s*0\.375rem 0\.75rem/);
});

test('La sidebar conserva anchos de referencia y el shell evita superposición del header', () => {
  const sidebar = read('src/Web/WebUI/src/app/core/layout/reports-sidebar.scss');
  assert.match(sidebar, /width:\s*280px/);
  assert.match(sidebar, /width:\s*76px/);
  assert.match(sidebar, /max-width:\s*991\.98px/);
  assert.match(sidebar, /prefers-reduced-motion/);
  const shell = read('src/Web/WebUI/src/app/app.scss');
  assert.match(shell, /height:\s*100dvh/);
  assert.match(shell, /\.layout-shell\s*\{[^}]*overflow-y:\s*auto/s);
  assert.match(shell, /min-height:\s*0/);
  assert.match(shell, /overflow-y:\s*auto/);
});

test('El frontend usa Angular 22 sin dependencias funcionales de alumnos', () => {
  const packageJson = json('src/Web/WebUI/package.json');
  assert.equal(packageJson.name, 'sadinformes');
  assert.match(packageJson.dependencies['@angular/core'], /^22\./);
  assert.deepEqual(Object.keys(packageJson.dependencies).sort(), [
    '@angular/common', '@angular/compiler', '@angular/core', '@angular/forms', '@angular/localize',
    '@angular/platform-browser', '@angular/router', '@ng-bootstrap/ng-bootstrap', '@popperjs/core',
    'bootstrap', 'bootstrap-icons', 'keycloak-js', 'rxjs', 'tslib',
  ]);
  assert.equal(packageJson.scripts.build, 'ng build');
});

test('La configuración pública conserva servidor, realm y cliente propio por ambiente', () => {
  const settings = json('src/Web/WebApi/appsettings.json');
  assert.deepEqual(settings.ConfigureSite, { ApplicationName: 'SADInformes', EnvironmentName: 'DEV' });
  assert.equal(settings.Keycloak.Url, 'https://sso-dev.fen.uchile.cl');
  assert.equal(settings.Keycloak.Realm, 'FenUChile');
  assert.ok(['SADInformes', 'SADInformes-iis'].includes(settings.Keycloak.ClientId));
});
