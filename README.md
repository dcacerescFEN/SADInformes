# SADInformes

Base independiente del sistema de informes, basada en SADAlumnos sin copiar
sus funcionalidades ni configuración sensible. Etapa 5: navegación visual de Postgrado.

## Alcance

- Backend .NET 10: Domain ← Application ← Infrastructure; WebApi referencia
  Application e Infrastructure y enlaza el proyecto Angular `WebUI.esproj`.
- Angular 22 standalone, rutas lazy y detección OnPush.
- Página inicial y diagnóstico `GET /api/health`.
- Autenticación JWT con Keycloak y navbar basada en SADAlumnos.
- AppConfig SQL de solo lectura y sidebar adaptada de SADAdministracion.
- Catálogo visual de Postgrado, sin informes funcionales ni acceso a datos académicos.
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

`GET /api/config/site` requiere un Bearer de SADInformes y la política
`REPORT_ACCESS` (permiso de entrada o rol administrativo REPORT) y devuelve únicamente
`{ success: true, message: null, data: { ... } }`, con los 18 campos de
`SiteConfigDTO` de NewSAD dentro de `data`. Lee registros activos de aplicación **SADInformes** y
ambiente **DEV**; también exige aplicación y ambiente activos.

Configurar `ConnectionStrings__AppConfig` en el entorno del proceso WebApi, fuera
del repositorio. Usar una cuenta con permiso de lectura sobre
`Applications`, `Environments` y `Configurations`. No registrar conexiones
ni contraseñas en commits o capturas. Los selectores públicos están en
`ConfigureSite:ApplicationName` y `ConfigureSite:EnvironmentName`.

Claves centrales admitidas, en pares URL/clientId:

| Destino | URL | Cliente físico |
| --- | --- | --- |
| Alumnos | StudentsMenuUrl | StudentsMenuClientId |
| Profesores | TeachersMenuUrl | TeachersMenuClientId |
| Cursos | CoursesMenuUrl | CoursesMenuClientId |
| Exámenes | ExamsMenuUrl | ExamsMenuClientId |
| Actividades | ActivitiesMenuUrl | ActivitiesMenuClientId |
| Ayudantes | AssistantsMenuUrl | AssistantsMenuClientId |
| Procesos | ProcessesMenuUrl | ProcessesMenuClientId |
| Informes | ReportsMenuUrl | ReportsMenuClientId |
| Administración | AdministrationMenuUrl | AdministrationMenuClientId |

Como transición, se aceptan las claves antiguas `StudentUrl`, `TeacherUrl`,
`StudentClientId` y `TeacherClientId` solo si falta su clave canónica. Si existen
ambas, gana la canónica, incluso si su clientId está vacío. No se modifican filas
SQL automáticamente. Angular consume únicamente el contrato canónico de `data`.

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
de carga de configuración usan una base EF InMemory aislada y el endpoint HTTP
real, no el SQL institucional.

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

### Config y Mediator

`Config/site` despacha `GetSiteConfig` mediante el mediador propio de NewSAD:
`IRequestDispatcher` / `IRequestHandler`, sin MediatR. `Application/ConfigureServices.cs`
registra automáticamente los handlers de esa capa mediante Scrutor 7.0.0.
`AddWebApiServices` reúne autenticación y documentación de API.

`/api/config/site` aplica `REPORT_ACCESS` y devuelve `ApiResponse<SiteConfigDTO>`: success, message y data,
sin el anterior objeto raíz menu. Se adaptó el consumidor Angular en conjunto.
Los fallos de AppConfig siguen respondiendo 503 sin detalles privados.
`/api/config/keycloak` sigue público, lee IConfiguration directamente como NewSAD
y devuelve únicamente url, realm y clientId; ya no necesita consulta ni handler.

`ConfigurationInitialiser` recibe directamente el `AppConfigContext` scoped,
registrado con `AddDbContext`, siguiendo el patrón de NewSAD. Se eliminaron
`AppConfigConfigurationReader` y su interfaz; la consulta y sus validaciones están
en el inicializador. Las pruebas conservan filtros, rechazo de duplicados,
publicación única y errores sanitizados sustituyendo el DbContext por uno aislado.
La API no requiere una base de informes funcional para estas fichas.

La política REPORT sigue el patrón `PermissionPolicyProvider` de NewSAD, con
alcance al cliente físico exacto configurado en `Keycloak:ClientId`. Acepta
`PERMISSION_REPORT_ACCESS`, `ROLE_REPORT_ADMIN` o `ROLE_REPORT_SUPERADMIN` dentro
de su `resource_access`. No mezcla `SADInformes` con `SADInformes-iis`, no toma
roles de otros clientes ni acepta nombres parciales. Sin token devuelve 401;
con token válido sin acceso devuelve 403, sin configuración. Health y configuración
pública de Keycloak siguen anónimos. No se crean roles ni asignaciones en Keycloak
automáticamente; se requieren los existentes del cliente correspondiente.

La comparación completa con NewSAD, SADAlumnos y SADAdministracion está en
[docs/backend-comparison.md](docs/backend-comparison.md), incluyendo diferencias
que se mantienen intencionalmente y faltantes previos a una migración funcional.

### Scalar y OpenAPI en desarrollo

Con ambiente `Development`, abrir `https://localhost:7070/scalar`
(también admite `/scalar/v1`); el documento está en
`/openapi/v1.json`. Fuera de Development ambas rutas responden 404. Scalar usa
Authorization Code con PKCE S256 y el cliente público de SADInformes; no requiere
client secret. Las operaciones protegidas están identificadas en OpenAPI; health
y configuración pública no se anuncian como protegidas.

Para probar OAuth desde Scalar, el administrador debe habilitar la URL de retorno
correspondiente al origen real en Keycloak. No se cambia el cliente del servidor
automáticamente. Este cambio no incorpora suplantación, permisos funcionales,
repositorios académicos, tareas periódicas ni estado global estático de NewSAD.

## Sidebar de informes

Se adapta el componente de SADAdministracion: panel de 280 px expandido y 76 px
colapsado, con perfil del usuario y temas de la aplicación. Conserva **Inicio**
e incorpora las 14 secciones de Informes del SAD como dropdowns con flecha,
submenú y estado activo. En escritorio colapsado, cada icono abre un flyout
mediante clic o teclado, sin quedar recortado por el scroll. En móvil se mantiene
el panel con backdrop. No copia períodos académicos, gestión de usuarios ni
permisos administrativos.

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
scroll independiente de contenido/sidebar. La comprobación visual y del overflow
en ventanas más bajas que el menú expandido sigue pendiente; no se considera
cerrada esta integración hasta resolverla. La sidebar respeta
`prefers-reduced-motion`. Los controles de sidebar del Header son opcionales
para conservar sus usos anteriores.

### Postgrado: etapa visual

Las secciones ajenas a Postgrado muestran **Pendiente de migración** al
expandirse; todavía no tienen pantallas funcionales ni rutas nuevas.
Postgrado muestra 31 funcionalidades: las 30 principales de `SAD/Informes/Postgrado`,
incluidas Estadísticas ISS y Encuesta Salida. Cada opción abre una ruta autenticada
`/postgrado/<opción>` con título, descripción, referencia al archivo legado y
estado **Pendiente de migración**. Un catálogo compartido define menú y rutas.
El detalle de profesor pertenece a Encuesta Salida, no tiene una opción separada.
Programación Docente Postgrado X Programa y Modalidad se incorpora desde
`SAD/Informes/Inscripcion/ProgramacionXProgramaModalidad.aspx`; las 30 rutas
anteriores se conservan.

Las fichas no consultan APIs ni bases de datos, no abren aplicaciones externas y
no permiten inscribir, registrar pagos, administrar firmas ni exportar informes.
No se agregan permisos por informe: mostrar una ficha no autoriza la operación
futura. REPORT_ACCESS controla la configuración de entrada al módulo, no sustituye
los permisos de consulta o modificación que requiera cada informe. La migración de cada funcionalidad y sus permisos se decidirá después.
Las rutas desconocidas siguen redirigiendo a Inicio.

La comprobación visual en navegador de esta nueva navegación sigue pendiente;
las pruebas automatizadas verifican fichas, sesión, rutas y navegación móvil.

### Programación Docente Postgrado X Programa y Modalidad

Ruta: `/postgrado/programacion-docente-programa-modalidad`. Conserva los cinco
filtros y las siete columnas del SAD. Promoción sigue mostrando `Promocion` y
enviando `Descripcion`, sin corregir el binding ni cambiar el parámetro SQL de 7
caracteres. Requiere `PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHING_SCHEDULE`.
Contrato, compatibilidad y pruebas en [`docs/teaching-schedule.md`](docs/teaching-schedule.md).

### Alumnos sin contestar encuesta docente

La ruta `/postgrado/alumnos-sin-contestar-encuesta` ya abre el informe real:
período, tipo y modalidad concretos, tabla con las cinco columnas del SAD y CSV
seguro. Usa el SP existente `spRec_Alumnos_Sin_Evaluacion_Docente`, sin cambios de
base. Requiere `PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY`;
el permiso de entrada no concede acceso a los datos. El contrato, la semántica
exacta de las evaluaciones pendientes y la verificación están en
[`docs/unanswered-teacher-survey.md`](docs/unanswered-teacher-survey.md).

### Primer informe funcional: Encuesta docente por curso

La opción **Encuesta docente por curso** ya no muestra la ficha pendiente: abre la
pantalla real en `/postgrado/encuesta-docente-por-curso`, con los tres filtros del
SAD, tabla de resultados, total y exportación CSV.

| Elemento | Valor |
| --- | --- |
| Endpoints | `GET /api/reports/postgraduate/teacher-survey/filters` y `GET /api/reports/postgraduate/teacher-survey` |
| Permiso | `PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY` en el cliente físico configurado |
| Datos | SP existentes `spRec_Encuesta_DocenteXCurso`, `spRec_PeriodosAcademicosProgramacion`, `spRec_TiposAlumno` y `spRec_TiposAlumnosModalidades` |
| Conexión | `ConnectionStrings:Sad`, definida fuera del repositorio con una cuenta de solo lectura |
| Acceso a datos | `SadContext` + `ISadContextProcedures`, igual que SADAlumnos y SADAdministracion |

`REPORT_ACCESS` habilita la entrada y la configuración del sitio, pero **no** concede
este informe: sin el permiso específico la API responde 403. Los roles de otro cliente,
incluida la variante `-iis` no configurada, no sirven para autorizar.

Los tres filtros son obligatorios. El SP compara tipo de alumno y modalidad con
igualdad, así que la API rechaza `Todos` y comodines con 400 en vez de devolver una
tabla vacía que parezca un resultado válido. Los errores de base se traducen a 503 sin
detalle interno.

Diferencias conocidas con la pantalla legada: la exportación es CSV UTF-8 con separador
`;` y nombre `.csv` (el SAD generaba el mismo formato con extensión `.xls` y Windows-1252),
no se enlaza al curso del SAD legado y no se pagina el resultado del legado, mientras
que la tabla nueva pagina de a 10 filas. Ambas pantallas usan los mismos SP, por lo que
los valores coinciden para los mismos filtros.

La pantalla conserva la glosa de preguntas (A1–A4, B1–B13, B_Nota y comentarios) y la
escala de respuestas 1–7 transcripta del SAD, para que los códigos numéricos de la
tabla sean interpretables sin abrir el sistema antiguo.

El acceso al SAD sigue la forma de las aplicaciones de referencia: `SadContext`
registrado con `AddDbContext` sobre `ConnectionStrings:Sad`, `ISadContext` y
`ISadContextProcedures` con un método por procedimiento (`EXEC` tipado, parámetros
`SqlParameter` y `OutputParameter<int>`). Cada resultado de SP vive en su propio
archivo bajo `Domain/Entities/Sad/Sp`. No hay conexiones propias del informe ni
SQL de negocio duplicado.

Las Queries y Commands de cada módulo en Application consumen directamente
`ISadContextProcedures`; las consultas LINQ usan los getters `DbSet<T>` de
`ISadContext`. No hay lectores de reportes ni un gateway de inscripción en
Infrastructure. `SadContext` conserva los mappings y `BeginTransactionAsync`
genérico; el Command define el aislamiento y confirma la transacción. Los wrappers
siguen MappingDB: `SqlQueryAsync<T>` para resultados y `ExecuteSqlRawAsync` para
operaciones sin filas, conservando parámetros de salida y cancelación.

La interfaz reutiliza los componentes de las referencias: `page-header`,
`form-floating` con etiqueta flotante sobre `ng-select`
(`@ng-select/ng-select` con su tema `default.theme.css`), botones `btn-search`,
`btn-clear` y `btn-create` (verde, usado para exportar), y la tabla compartida
`app-table` de `src/app/shared/components/table.ts` —la versión de SADAlumnos, con
paginación, orden y estado vacío—.

Con `dotnet run` el frontend de desarrollo está en 4200; la API y Scalar, en 7070.
Una consulta real requiere la conexión de solo lectura y los roles de Keycloak;
ninguna credencial se guarda en el repositorio.

El acceso requiere `PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY`. La
equivalencia con los servicios del SAD legado y los permisos aún propuestos están en
[docs/permissions-equivalence.md](docs/permissions-equivalence.md).

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
`https`. Los perfiles http/https tienen `launchBrowser: true` y `launchUrl: scalar`,
como NewSAD: al iniciar desde el IDE se abre Scalar, no el inicio Angular.
Con `dotnet run`, abrir manualmente https://localhost:7070/scalar si el launcher
no abre navegador. Para entrar a la aplicación, usar https://localhost:7070/ o
https://localhost:4200; `/scalar` no es una ruta del frontend en 4200.
La referencia a WebUI tiene `ReferenceOutputAssembly=false`, porque no es un
ensamblado .NET.

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
Las pruebas Angular comprueban contenedor, página inicial, dropdowns, flyouts,
fichas de Postgrado, panel móvil y redirección de rutas. Las pruebas HTTP .NET
comprueban también el despacho de Config y Scalar/OpenAPI por ambiente.
La restauración de paquetes advierte vulnerabilidades transitivas en
`System.Security.Cryptography.Pkcs 9.0.11`, ya presentes antes de agregar Scalar;
su actualización requiere una unidad de trabajo aparte.

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

### Publicación integrada

`dotnet publish src/Web/WebApi/WebApi.csproj` ejecuta `pnpm install --frozen-lockfile`
y `pnpm build` y copia la salida Angular a `wwwroot` del artefacto publicado.
El resultado se sirve desde el mismo origen que la API; no requiere un segundo
servidor web ni un proxy de producción. Para omitir esa compilación en un entorno
que ya entrega los assets por otro medio, usar `-p:SpaBuildOnPublish=false`.

WebApi sirve archivos estáticos y devuelve `index.html` solo para rutas del cliente.
Rutas bajo `/api`, `/scalar` u `/openapi` y archivos inexistentes conservan 404;
un método no soportado sobre una ruta existente conserva 405. Esto evita que un
error de API se presente como una página Angular exitosa.

La publicación no despliega: no configura IIS, certificados, AppConfig ni Keycloak.
Tampoco crea ni modifica roles. Esos pasos siguen siendo decisiones humanas.

## Cursos vs. alumnos de Postgrado

Informe en `/postgrado/cursos-por-alumnos`, con el formato de Encuesta docente por
curso y sin opción Todos. Consulta por semestre de ingreso, tipo y modalidad.
Contrato API, permiso específico, exportación y dependencias en
[Courses vs. students](docs/courses-by-students.md).

## Inscripción masiva de Postgrado

`/postgrado/inscripcion-masiva` incorpora consulta, selección y escritura real de
inscripciones administrativas. Requiere permisos separados de consulta y gestión.
Uso, API, transacciones y validación necesaria antes de operar con SAD real en
[Inscripción masiva](docs/mass-enrollment.md).

## Siguientes etapas

1. Validar visualmente el catálogo de Postgrado y elegir la primera funcionalidad.
2. Completar sus dependencias y fuentes de datos aprobadas.
3. Incorporar el informe y sus permisos funcionales correspondientes.

No copiar connection strings, secretos, entidades, procedimientos ni servicios
funcionales de los sistemas de referencia.
