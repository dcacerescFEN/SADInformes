# SADInformes — Navbar AppConfig (etapa 3)
Barra superior basada en SADAlumnos; la sidebar queda fuera.
Risk: item 2 (filtro de acceso), item 5 (dependencias/configuración).

## Specs
S1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
S2. "Inicia Etapa 3"
S3. "appconfig"
S4. "SADInformes /DEV"

## Tasks
T1 | S1-S4 | inline | completado | commit: 096a0ef | Leer menú propio de AppConfig mediante endpoint autenticado.
T2 | S1-S4 | inline | completado | commit: 25226f0 (núcleo); UI por registrar | Incorporar navbar, acceso por cliente, tema y sesión.
T3 | S1-S4 | verificación independiente + revisión nativa | en progreso | commit: pendiente | Verificar API, navbar responsive, regresiones y documentación.

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
L11. T1 aceptado por verificador independiente sin bloqueadores. Revisión nativa de 096a0ef no disponible (package-local-binary-missing). La prueba SQL real queda a cargo del entorno con conexión autorizada.
L12. T2 (núcleo de navegación): RED con 23 fallos; GREEN 47 pruebas de sesión/acceso y carga HTTP. IDs exactos y -iis separados, roles nuevos, URLs seguras/deshabilitadas y fallo AppConfig sin enlaces obsoletos.
L13. Revisión nativa de 25226f0 no disponible por binario local ausente. UI: RED observado en 8 pruebas de navbar/tema; implementación integrada con Bootstrap/ng-bootstrap de mismas versiones que SADAlumnos, logo original y temas propios. GREEN y build en curso.
L14. No hay herramientas de navegador disponibles en la sesión (búsqueda de capacidades sin resultados). Se ejecutan pruebas DOM de interacción y smoke HTTPS de assets/API; no se afirmará inspección visual real.
L15. UI GREEN: suite Angular completa y build aprobados tras RED de 8 pruebas. Advertencia de bundle inicial: 642,59 kB frente al umbral de aviso 500 kB, bajo el límite de error 1 MB; no se cambió el presupuesto para ocultarla.
