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

### Configuración del cliente en Keycloak

La configuración del servidor se realiza por el administrador; esta aplicación
no crea ni modifica clientes ni usuarios. Para `SADInformes`:

- Cliente OpenID Connect público, `Client authentication` desactivado.
- Standard flow habilitado y PKCE S256.
- Redirect URIs y post logout redirect URIs:
  `https://localhost:4200/*` en desarrollo.
- Web origins: `https://localhost:4200`, no `*`.
- El access token debe incluir `SADInformes` en `aud`; configurar un audience
  mapper si el realm no lo incluye. Tener `azp=SADInformes` no sustituye `aud`.

La SPA comprueba SSO antes del router, exige sesión al entrar y renueva el token
antes de llamar a `/api/`. Los tokens permanecen en memoria del adaptador, no
en localStorage/sessionStorage. El interceptor no adjunta tokens a terceros.
Si falla la renovación, se limpia la sesión y no se envía la solicitud.
La página inicial ofrece cierre de sesión, sin incorporar navbar ni sidebar.

Las pruebas automatizadas usan adaptadores simulados; el login real requiere
un usuario del realm y que el administrador haya registrado estas URLs.

## Requisitos

SDK .NET 10, Node.js compatible con Angular 22
(`^22.22.3 || ^24.15.0 || >=26.0.0`) y pnpm 12.6.0.
`global.json` permite cualquier feature band instalada del SDK 10.0.

## HTTPS de desarrollo

Si el navegador no confía en el certificado, ejecutar manualmente:

```sh
dotnet dev-certs https --trust
```

Este comando modifica la confianza local y puede requerir confirmación del
sistema operativo. La aplicación no lo ejecuta automáticamente.

`pnpm start` exporta el certificado de desarrollo a
`src/Web/WebUI/.certs/localhost.pem` y su clave a `localhost.key`, antes de
iniciar Angular con HTTPS en localhost:4200. La carpeta está ignorada por Git:
no compartir ni versionar la clave. No se escriben archivos en APPDATA.
Si se renueva el certificado ASP.NET Core, retirar las copias de `.certs` y
volver a iniciar para exportarlo de nuevo.

Angular confía en ese certificado mediante `NODE_EXTRA_CA_CERTS` únicamente
en su proceso hijo, y el proxy mantiene `secure: true`. La prueba TLS también
valida el certificado y hostname, sin desactivar validación global.

## Ejecución local

Instalar las dependencias frontend una vez, desde la raíz de SADInformes:

```sh
cd src/Web/WebUI
pnpm install --frozen-lockfile
cd ../../..
dotnet build SADInformes.slnx
dotnet run --project src/Web/WebApi --launch-profile https
```

Abrir https://localhost:7070. El perfil `https` activa ASP.NET Core SPA proxy:
WebApi inicia `pnpm start` en WebUI y, cuando Angular está listo, redirige
al frontend en https://localhost:4200. No es necesario iniciar ambos proyectos
por separado. El servidor Angular reenvía `/api` a la API; no requiere CORS
y utiliza el certificado de desarrollo ASP.NET Core.

En Visual Studio, abrir `SADInformes.slnx`: WebUI aparece como proyecto JavaScript
junto a WebApi. Establecer WebApi como proyecto de inicio y utilizar el perfil
`https`. La referencia a WebUI tiene `ReferenceOutputAssembly=false`, porque
no es un ensamblado .NET.

Opcionalmente se puede ejecutar `pnpm start` manualmente en WebUI; el SPA proxy
reutiliza ese servidor si ya está activo.
Se comparten los puertos 7070 y 4200 con SADAlumnos y SADAdministracion:
no pueden ejecutarse simultáneamente en esos mismos puertos.

El perfil `http` anterior permanece disponible: conserva el listener
`http://localhost:5086` y levanta también HTTPS en 7070 para no romper el
proxy Angular. Para uso normal se recomienda `--launch-profile https`.

En Keycloak, actualizar las redirect URIs, post logout redirect URIs y Web origins
a las URLs HTTPS indicadas arriba; no se modifican automáticamente en el servidor.

`GET https://localhost:7070/api/health` devuelve:

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
en los puertos 7070 y 4200 (y 5086 para el listener HTTP compatible):

```sh
node --test tests/dev-smoke.mjs
```

Esta prueba inicia WebApi con el perfil `https`, espera la redirección a Angular,
valida TLS con el certificado exportado, comprueba la página y su JavaScript,
consulta `/api/health` a través del frontend
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

1. Navbar de SADAlumnos.
2. Sidebar de SADAdministracion adaptada a informes.
3. Permisos funcionales, catálogo de informes y fuentes de datos aprobados.

No copiar connection strings, secretos, entidades, procedimientos ni servicios
funcionales de los sistemas de referencia.
