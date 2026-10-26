# SADInformes — Keycloak (etapa 2)
Integración de autenticación basada en SADAlumnos, sin navbar ni sidebar.
Riesgo: item 2 (autenticación); item 5 (dependencias).

## Specs
S1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
S2. "etapa 2 ya hice git init"
S3. "el mismo servidor y real el cliendId es SADInformes"

## Tasks
T0 | S2 | inline | en progreso | commit: pendiente | Registrar base existente de etapa 1.
T1 | S1-S3 | inline + verificación independiente | pendiente | commit: pendiente | Configuración pública y validación JWT de la API.
T2 | S1-S3 | inline + verificación independiente | pendiente | commit: pendiente | Sesión Angular, guard, interceptor y cierre de sesión.
T3 | S1-S3 | verificación independiente + revisión nativa | pendiente | commit: pendiente | Comprobar integración, actualizar documentación y cerrar.

## Log
L1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
L2. "etapa 2 ya hice git init"
L3. "el mismo servidor y real el cliendId es SADInformes"
L4. Servidor público de referencia: https://sso-dev.fen.uchile.cl; realm: FenUChile. No copiar otras configuraciones.
L5. Diseño: conservar /api/health anónimo; exponer solo url/realm/clientId; /api/auth/me requiere JWT válido. Sin permisos funcionales, suplantación ni clientes administrativos.
L6. Git estaba inicializado en master, sin commits; se crea una rama de funcionalidad y un checkpoint de la etapa 1 como base de comparación. No push ni publicación.
