# SADInformes — Navbar AppConfig (etapa 3)
Barra superior basada en SADAlumnos; la sidebar queda fuera.
Risk: item 2 (filtro de acceso), item 5 (dependencias/configuración).

## Specs
S1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
S2. "Inicia Etapa 3"
S3. "appconfig"
S4. "SADInformes /DEV"

## Tasks
T1 | S1-S4 | inline | completado | commit: por registrar | Leer menú propio de AppConfig mediante endpoint autenticado.
T2 | S1-S4 | inline | pendiente | commit: pendiente | Incorporar navbar, acceso por cliente, tema y sesión.
T3 | S1-S4 | verificación independiente + revisión nativa | pendiente | commit: pendiente | Verificar API, navbar responsive, regresiones y documentación.

## Log
L1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
L2. "Inicia Etapa 3"
L3. "appconfig"
L4. "SADInformes /DEV"
L5. Configuración explícita ConfigureSite:ApplicationName=SADInformes y EnvironmentName=DEV. ConnectionStrings__AppConfig externa: no se copian secretos. No se modifican datos AppConfig, otros sistemas ni la modificación humana de SADInformes.slnx.
L6. Reutilizar reglas de clientes físicos exactos y roles nuevos de SADAlumnos. Sin suplantación, permisos funcionales de informes inventados ni sidebar. La navbar no sustituye autorización de APIs destino.
L7. Menú sin destinos/client IDs inferidos; solo las 16 claves centrales conocidas. Ausencia de conexión o errores: endpoint 503 sanitizado y aviso en UI, manteniendo sesión/tema.
L8. Conexión AppConfig no definida en el proceso. Se comprobará con estado aislado; prueba real de SQL requiere configuración del operador. HTTPS 7070/4200 y cliente Keycloak SADInformes se conservan.
L9. RED HTTP inicial: /api/config/site devolvía 404 en vez de 401. RED del lector: 7 fallos de comportamiento sobre 19 pruebas al usar el proveedor sin consulta; comprobadas selección/activos, duplicados y respuesta sanitizada. GREEN en curso tras implementar consulta EF de solo lectura.
L10. GREEN T1: 19 pruebas .NET y 7 Node aprobadas. Estado aislado EF InMemory, sin conexión ni escrituras en SQL real.
