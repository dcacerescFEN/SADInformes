# SADInformes

Base independiente del sistema de informes, basada en SADAlumnos sin copiar
sus funcionalidades ni configuración sensible. Etapa 4: sidebar y shell responsive.

## Alcance

- Backend .NET 10: Domain ← Application ← Infrastructure; WebApi referencia
  Application e Infrastructure y enlaza el proyecto Angular `WebUI.esproj`.
- Angular 22 standalone, rutas lazy y detección OnPush.
- Página inicial y diagnóstico `GET /api/health`.
- Autenticación JWT con Keycloak y navbar basada en SADAlumnos.
- AppConfig SQL de solo lectura y sidebar adaptada de SADAdministracion.
- Sin base funcional ni catálogo nuevo de informes.
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
La navbar ofrece cierre de sesión, selector de tema y navegación entre sistemas.
La sidebar se incorpora en la etapa 4, sin cambiar el inicio de sesión.

Las pruebas automatizadas usan adaptadores simulados; el login real requiere
un usuario del realm y que el administrador haya registrado estas URLs.

## Menú central de AppConfig

`GET /api/config/site` requiere un Bearer de SADInformes y devuelve únicamente
`{ menu: { ... } }`. Lee registros activos de aplicación **SADInformes** y
ambiente **DEV**; también exige aplicación y ambiente activos.

Configurar `ConnectionStrings__AppConfig` en el entorno del proceso WebApi, fuera
del repositorio. Usar una cuenta con permiso de lectura sobre
`Applications`, `Environments` y `Configurations`. No registrar conexiones
ni contraseñas en commits o capturas. Los selectores públicos están en
`ConfigureSite:ApplicationName` y `ConfigureSite:EnvironmentName`.

Claves centrales admitidas, en pares URL/clientId:

| Destino | URL | Cliente físico |
| --- | --- | --- |
| Alumnos | StudentUrl | StudentClientId |
| Profesores | TeacherUrl | TeacherClientId |
| Cursos | CoursesMenuUrl | CoursesMenuClientId |
| Exámenes | ExamsMenuUrl | ExamsMenuClientId |
| Actividades | ActivitiesMenuUrl | ActivitiesMenuClientId |
| Ayudantes | AssistantsMenuUrl | AssistantsMenuClientId |
| Procesos | ProcessesMenuUrl | ProcessesMenuClientId |
| Informes | ReportsMenuUrl | ReportsMenuClientId |
| Administración | AdministrationMenuUrl | AdministrationMenuClientId |

`ConfigurationInitialiser` carga AppConfig una vez durante el arranque mediante
`await app.InitialiseConfigurationAsync(...)`. Prepara todos los valores antes
de publicarlos en `SiteSettings`, singleton aislado por aplicación, no estático
global al proceso. `SiteMenuProvider` devuelve esa configuración ya inicializada.
Los cambios de AppConfig se aplican al reiniciar el proceso.

Las claves ausentes o nulas usan los mismos defaults públicos de SADAlumnos:

| Destino | URL predeterminada | ClientId predeterminado |
| --- | --- | --- |
| Alumnos | https://sadalumnos-dev.fen.uchile.cl | SADAlumnos |
| Profesores | https://sadprofesores-dev.fen.uchile.cl | SADProfesores |
| Cursos | https://sadcursos-dev.fen.uchile.cl | SADCursos |
| Exámenes | https://sadexamenes-dev.fen.uchile.cl | SADExamenes |
| Actividades | https://sadactividades-dev.fen.uchile.cl | SADActividades |
| Ayudantes | https://sadayudantes-dev.fen.uchile.cl | SADAyudantes |
| Procesos | https://sadprocesos-dev.fen.uchile.cl | SADProcesos |
| Informes | https://sadinformes-dev.fen.uchile.cl | SADInformes |
| Administración | https://sadadministracion-dev.fen.uchile.cl | SADAdministracion |

`StatusSite` usa `1` como default operativo, sin añadirlo al contrato público
del menú. Los valores de AppConfig tienen prioridad; las URLs se recortan y una
URL explícitamente vacía rechaza la inicialización. Un clientId explícitamente
vacío se conserva para ocultar el destino, no se reemplaza por el default.

No se crean tablas, migraciones ni registros. El modelo EF conserva nombres y
relaciones de AppConfig en SADAlumnos. Las claves se interpretan sin distinguir
mayúsculas; duplicados activos rechazan la inicialización completa. No se
publican valores parciales ni otras claves, incluidas credenciales.

Sin conexión o ante errores de AppConfig, no se usan defaults para ocultar el
error: el menú devuelve 503 con un mensaje genérico, sin detalles SQL. Health y
Keycloak siguen disponibles después del intento de inicialización. Las pruebas
del lector usan una base EF InMemory aislada y el endpoint HTTP real, no el SQL
institucional.

### Acceso a los destinos de la navbar

Se consulta el cliente físico exacto configurado en `resource_access` del token.
La variante `-iis` solo se normaliza para elegir la política: sus roles no se
mezclan con los del cliente sin sufijo. Se admiten los roles nuevos
`ROLE_<MÓDULO>_ADMIN` y `ROLE_<MÓDULO>_SUPERADMIN`.

| Cliente lógico | Módulo | Permiso de entrada adicional |
| --- | --- | --- |
| NewSAD | SAD | PERMISSION_SAD_ACCESS |
| SADAlumnos | STUDENT | PERMISSION_STUDENT_ACCESS_STUDENT_MODULE |
| SADAdministracion | ADMINISTRATION | PERMISSION_ADMINISTRATION_ACCESS |
| SADProfesores | TEACHER | — |
| SADCursos | COURSE | — |
| SADExamenes | EXAM | — |
| SADActividades | ACTIVITY | — |
| SADAyudantes | ASSISTANT | — |
| SADProcesos | PROCESS | — |
| SADInformes | REPORT | PERMISSION_REPORT_ACCESS |

Un destino sin clientId, desconocido, sin cliente en el token o con roles antiguos
se oculta. Informes aparece antes de Administración y requiere en su cliente
físico `ROLE_REPORT_ADMIN`, `ROLE_REPORT_SUPERADMIN` o `PERMISSION_REPORT_ACCESS`,
con el mismo criterio de los demás destinos. `REPORT_ACCESS` es el nombre lógico
confirmado; no se renombra. La marca lleva al inicio del sistema actual.

En despliegues IIS, configurar `ReportsMenuClientId` con el cliente físico destino
(por ejemplo `SADInformes-iis`); no se mezclan permisos entre variantes.
Estos cambios no crean ni modifican filas de AppConfig automáticamente.

Las URLs deben ser HTTP(S) absolutas, sin credenciales, espacios ni caracteres
de control. Si existe acceso pero falta una URL válida, el enlace queda sin
`href`, deshabilitado y fuera del recorrido de tabulación. El filtro de la navbar
no sustituye la autorización de cada API destino.

### Presentación y sesión

La barra conserva el logo FEN sin texto añadido a la marca, enlaces con iconos,
menú móvil colapsable y breakpoint `lg` (992 px), espaciados y controles de
SADAlumnos. Informes aparece antes de Administración y se marca como sitio
actual. Se conservan correo FEN, cambio de contraseña y cierre de sesión;
el layout puede habilitar los controles de sidebar; no se incluye suplantación.

Los temas Claro/Oscuro/Auto usan la clave local
`sadinformes-theme-preference`, sin modificar la preferencia de SADAlumnos.
Auto sigue los cambios de tema del sistema. Si el navegador bloquea el
almacenamiento, el tema funciona durante la sesión sin persistirlo.

Ante fallo de AppConfig, se muestra `No se pudo cargar la navegación.`;
los controles de usuario y tema siguen disponibles y no se conservan enlaces
obsoletos. Un fallo al cerrar sesión muestra un aviso sin ocultarlo.

### Convención de endpoints

Los endpoints implementan `WebApi.Extension.IEndpoint`.
`builder.Services.AddEndpoints(typeof(Program).Assembly)` descubre los tipos
concretos y `app.MapEndpoints()` registra sus rutas. `Program.cs` no declara
rutas directamente. Health y configuración pública de Keycloak siguen anónimos;
sesión y configuración del sitio siguen requiriendo autenticación.

La infraestructura se registra con `AddInfrastructureServices`; no se copian
inicializadores de datos académicos, tareas periódicas, correos ni credenciales
de SADAlumnos.

## Sidebar de informes

Se adapta el componente de SADAdministracion: panel de 280 px expandido y 76 px
colapsado, con perfil del usuario y temas de la aplicación. Muestra únicamente
**Inicio**, la ruta que ya existe; no copia períodos académicos, gestión de
usuarios ni permisos administrativos.

El botón de la navbar colapsa/expande en escritorio. Antes de 992 px abre un
panel lateral con backdrop, cierre por Escape, enlace, botón o navegación, y
restauración del foco. Al pasar a escritorio o destruir la vista se retira el
panel y su backdrop. No se guarda el estado colapsado ni datos de perfil en
almacenamiento persistente.

La sesión determina el montaje de navbar/sidebar; al perderla se elimina
también cualquier panel abierto. El nombre proviene de la claim pública
`name`, con fallback al username. No se cambian tokens, guards, REPORT_ACCESS
ni autorización backend.

El shell mantiene un único `RouterOutlet`, header con altura automática y
scroll independiente de contenido/sidebar, evitando superposición cuando
crece el menú móvil o un aviso de navegación. La sidebar respeta
`prefers-reduced-motion`. Los controles de sidebar del Header son opcionales
para conservar sus usos anteriores.

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

1. Definir catálogo de informes y fuentes de datos aprobados.
2. Incorporar informes y permisos funcionales correspondientes.

No copiar connection strings, secretos, entidades, procedimientos ni servicios
funcionales de los sistemas de referencia.
