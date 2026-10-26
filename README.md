# SADInformes

Base independiente del sistema de informes, basada en SADAlumnos sin copiar
sus funcionalidades ni configuración sensible. Etapa 2: integración Keycloak.

## Alcance

- Backend .NET 10: Domain ← Application ← Infrastructure; WebApi referencia
  Application e Infrastructure y enlaza el proyecto Angular `WebUI.esproj`.
- Angular 22 standalone, rutas lazy y detección OnPush.
- Página inicial y diagnóstico `GET /api/health`.
- Autenticación JWT de la API con Keycloak. Sin base de datos, navbar, sidebar ni informes. Las capas interiores
  permanecen vacías hasta que exista una funcionalidad que las necesite.
- No se modifican SADAlumnos ni SADAdministracion.

Esta etapa no está preparada para exposición pública ni uso con datos reales.
Los permisos funcionales se definirán antes de implementar informes.

## Keycloak

Servidor: `https://sso-dev.fen.uchile.cl`; realm: `FenUChile`;
cliente público: `SADInformes` (sensible a mayúsculas).

La API valida firma, issuer, audiencia y vigencia del token; no acepta audiencia
`SADAlumnos`. No se copia administración de usuarios, suplantación ni permisos
`STUDENT_*`.

- `GET /api/health`: público, conserva el diagnóstico.
- `GET /api/config/keycloak`: público, solo `url`, `realm` y `clientId`.
- `GET /api/auth/me`: exige Bearer válido y devuelve `username`;
  sin token o con token inválido responde 401, sin identidad.
- Los endpoints de negocio futuros deben declarar su autorización explícitamente.

Solo se guardan valores públicos en `appsettings.json`. Se pueden sobrescribir
mediante `Keycloak__Url`, `Keycloak__Realm` y `Keycloak__ClientId`;
no se necesita ni debe añadirse un client secret a la SPA.

## Requisitos

SDK .NET 10, Node.js compatible con Angular 22
(`^22.22.3 || ^24.15.0 || >=26.0.0`) y pnpm 12.6.0.
`global.json` permite cualquier feature band instalada del SDK 10.0.

## Ejecución local

Instalar las dependencias frontend una vez, desde la raíz de SADInformes:

```sh
cd src/Web/WebUI
pnpm install --frozen-lockfile
cd ../../..
dotnet build SADInformes.slnx
dotnet run --project src/Web/WebApi --launch-profile http
```

Abrir http://localhost:5086. El perfil `http` activa ASP.NET Core SPA proxy:
WebApi inicia `pnpm start` en WebUI y, cuando Angular está listo, redirige
al frontend en http://127.0.0.1:4206. No es necesario iniciar ambos proyectos
por separado. El servidor Angular reenvía `/api` a la API; no requiere CORS
ni certificados locales.

En Visual Studio, abrir `SADInformes.slnx`: WebUI aparece como proyecto JavaScript
junto a WebApi. Establecer WebApi como proyecto de inicio y utilizar el perfil
`http`. La referencia a WebUI tiene `ReferenceOutputAssembly=false`, porque
no es un ensamblado .NET.

Opcionalmente se puede ejecutar `pnpm start` manualmente en WebUI; el SPA proxy
reutiliza ese servidor si ya está activo.
Se utilizan puertos distintos de los de SADAlumnos y SADAdministracion.

`GET http://localhost:5086/api/health` devuelve:

```json
{"status":"ok","application":"SADInformes"}
```

## Verificación

Desde la raíz de SADInformes:

```sh
dotnet build SADInformes.slnx
dotnet test tests/WebApi.Tests/WebApi.Tests.csproj
node --test tests/*.test.mjs
cd src/Web/WebUI
pnpm build
pnpm test
```

Las pruebas .NET usan claves RSA efímeras y metadatos OIDC locales al proceso:
validan tokens por HTTP sin usuarios ni acceso al servidor Keycloak real.
Las pruebas Node comprueban estructura, dependencias y arranque HTTP real de la API.
Las pruebas Angular comprueban contenedor, página inicial y redirección de rutas.

Para verificar el arranque integrado después de compilar, sin servidores previos
en los puertos 5086 y 4206:

```sh
node --test tests/dev-smoke.mjs
```

Esta prueba inicia WebApi con el perfil `http`, espera la redirección a Angular,
comprueba la página y su JavaScript, consulta `/api/health` a través del frontend
y cierra el árbol de procesos que inició. Si los puertos están ocupados, falla
sin detener los procesos existentes.

Los scripts nativos de las dependencias de compilación se autorizan explícitamente
mediante `pnpm-workspace.yaml`, siguiendo la base de SADAlumnos. No se habilitan
scripts de cualquier paquete de forma global.

La solución incluye backend y frontend; WebApi referencia `WebUI.esproj`.
Como en SADAlumnos, la compilación Angular se ejecuta explícitamente con
`pnpm build` (`ShouldRunBuildScript=false`); restaurar o compilar la solución
no instala paquetes npm ni modifica instalaciones globales.

La salida Angular queda en `src/Web/WebUI/dist/webui/browser`.
El enlace y arranque conjunto están configurados para desarrollo.
El backend todavía no sirve esa salida en producción: publicación y despliegue
quedan fuera de esta etapa.

## Siguientes etapas

1. Integración Keycloak basada en SADAlumnos, con cliente y permisos propios.
2. Navbar de SADAlumnos.
3. Sidebar de SADAdministracion adaptada a informes.
4. Catálogo de informes y fuentes de datos aprobados.

No copiar connection strings, secretos, entidades, procedimientos ni servicios
funcionales de los sistemas de referencia.
