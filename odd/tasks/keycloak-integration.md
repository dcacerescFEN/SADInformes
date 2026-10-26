# SADInformes — Keycloak (etapa 2)
Integración de autenticación basada en SADAlumnos, sin navbar ni sidebar.
Riesgo: item 2 (autenticación); item 5 (dependencias).

## Specs
S1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
S2. "etapa 2 ya hice git init"
S3. "el mismo servidor y real el cliendId es SADInformes"

S4. "agrega el  https y ocupa los mismo puertos que ocupan sadalumnos y sadadministracion el 7070 y el 4200"

## Tasks
T0 | S2 | inline | completado | commit: ce7785c | Registrar base existente de etapa 1.
T1 | S1-S3 | inline + verificación independiente | completado | commit: 46d928a | Configuración pública y validación JWT de la API.
T2 | S1-S3 | inline + verificación independiente | completado | commit: b0d41f4 | Sesión Angular, guard, interceptor y cierre de sesión.
T3 | S1-S3 | verificación independiente + revisión nativa | bloqueado: decisión humana sobre prueba real y revisión no disponible | commit: b0d41f4 | Comprobar integración, actualizar documentación y cerrar.

T4 | S4 | inline | completado | commit: pendiente | Fijar pruebas de HTTPS y puertos.
T5 | S4 | inline | completado | commit: pendiente | Configurar certificados locales, HTTPS 7070/4200 y proxy.
T6 | S4 | verificación independiente + revisión nativa | en progreso | commit: pendiente | Verificar arranque TLS, regresiones y documentación.

## Log
L1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
L2. "etapa 2 ya hice git init"
L3. "el mismo servidor y real el cliendId es SADInformes"
L4. Servidor público de referencia: https://sso-dev.fen.uchile.cl; realm: FenUChile. No copiar otras configuraciones.
L5. Diseño: conservar /api/health anónimo; exponer solo url/realm/clientId; /api/auth/me requiere JWT válido. Sin permisos funcionales, suplantación ni clientes administrativos.
L6. Git estaba inicializado en master, sin commits; se crea una rama de funcionalidad y un checkpoint de la etapa 1 como base de comparación. No push ni publicación.
L7. API: RED HTTP observado (404 en configuración); primer intento .NET no compiló por dependencia aún ausente. GREEN: 11 pruebas .NET y 7 Node; RSA efímero, audiencia/issuer/firma/vigencia y sesión anónima comprobados.
L8. Verificador independiente T1: aceptado S1-S3 sin bloqueadores. Revisión nativa no disponible: package-local-binary-missing; no se reintentará esta unidad.
L9. Angular: RED (9 fallos de comportamiento) seguido de GREEN de suite completa y build. Adaptador keycloak-js 26.2.4, como SADAlumnos; sesión solo en memoria, redirecciones del mismo origen y renovación fallida bloquea la solicitud.
L10. La consulta OIDC real fue bloqueada al resolver a dirección interna; no se eludió el bloqueo. Login real requiere conectividad institucional y cliente configurado; no se modificó Keycloak remotamente.
L11. T2 aceptado por verificador independiente S1-S3, sin bloqueadores estructurales. Login real no verificado; configuración administrativa del cliente no comprobada ni modificada.
L12. Build de solución y Angular aprobados; 11 pruebas .NET, suite Angular completa, 7 Node y 1 smoke integrado aprobados. Smoke comprueba arranque conjunto, JS y health/config/me401 a través del proxy. No hubo navegador real ni usuarios Keycloak reales.
L13. Ambas unidades se evaluaron por separado para revisión nativa; no disponible por package-local-binary-missing. No se creó autoridad ni consentimiento, no se instalaron herramientas globales y no se hizo push.
L14. Para cerrar T3, el usuario debe decidir si acepta la entrega con pruebas locales y verificación independiente o si espera la validación real desde la red institucional. Configuración necesaria del cliente documentada en README.md.
L15. "agrega el  https y ocupa los mismo puertos que ocupan sadalumnos y sadadministracion el 7070 y el 4200"
L16. Se conserva la modificación humana de SADInformes.slnx (Build/Deploy de WebUI); no se incluirá en el commit de HTTPS. Certificados exportados solo a WebUI/.certs, ignorados por Git; no se modifica confianza global automáticamente.
L17. RED observado: la prueba de configuración exige https://localhost:7070 y https://localhost:4200 y falla contra los puertos anteriores.
L18. HTTPS principal: localhost:7070 y localhost:4200. Se preserva el perfil http y su listener 5086, añadiendo 7070 para mantener funcional el proxy. Certificado solo en .certs ignorado; NODE_EXTRA_CA_CERTS solo en el proceso Angular y proxy secure:true. La confianza global se deja como comando manual.
L19. GREEN: build solución y Angular, 11 pruebas .NET, suite Angular, 7 Node y smoke HTTPS aprobados. El smoke validó certificado/hostname, redirección 7070→4200 y health/config/me401 por el proxy. Verificador independiente aceptó S4 sin bloqueadores.
