# Equivalencia de permisos: SAD legado → SADInformes

SADInformes reemplaza los servicios de la tabla `Permisos` del SAD por permisos y
roles de Keycloak del cliente físico `SADInformes` (o `SADInformes-iis`). Este
documento es la referencia para asignar accesos y para migrar usuarios; **no crea
roles ni asignaciones**.

## Reglas de lectura

- El permiso SAD es solo el punto de partida: el comportamiento del SAD no era
  uniforme (algunas páginas no validaban nada, otras redirigían y otras solo
  ocultaban botones).
- `PERMISSION_REPORT_*` se valida en el backend. Un permiso en el token que no
  corresponda al cliente físico configurado se ignora.
- `ROLE_REPORT_ADMIN` y `ROLE_REPORT_SUPERADMIN` otorgan todo el módulo, como en
  las demás aplicaciones. No habilitan suplantación.
- Los permisos heredados que redirigían a otros sistemas (`consulta alumnos`,
  `solo informes`) no se reproducen: el acceso se decide dentro de SADInformes.

## Permisos de entrada y navegación

| Permiso SAD legado | Equivalente en SADInformes | Observación |
| --- | --- | --- |
| `Viñeta Informes` | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` | Muestra el destino Informes en la navbar. |
| `ver Postgrado` | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Permiso único para todos los informes de Postgrado; reemplaza los permisos específicos por informe. |
| `consulta alumnos` | — | En el SAD expulsaba al módulo de alumnos. No se migra como permiso de informes. |
| `solo informes` | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` | Solo ocultaba botones del encabezado; el nuevo shell no los tiene. |
| `informes acreditación` | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` | Era un requisito adicional en varias páginas de Postgrado. |
| `cuadro de honor` | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` | Requisito adicional de rankings; hoy lo cubre el permiso del informe. |
| `ver Postgrado` + `encuesta egresados` (Estadísticas encuesta salida) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `reporte proximas licenciaturas` | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` | Solo controlaba la visibilidad del enlace. |
| `INFORMES - MANTENEDORES POSTGRADO` | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` | El acceso real lo define cada sistema externo. |
| `INFORMES - MANTENEDORES POSTGRADO CONSTANCIAS` | — | Permiso de una aplicación externa; sigue en su sistema. |
| `INFORMES - MANTENEDORES POSTGRADO D Y C` | — | Permiso de una aplicación externa; sigue en su sistema. |
| `INFORMES - MANTENEDORES POSTGRADO NCE POSTGRADO` | — | Gestión no migrada; sigue en Maestro. |
| `INFORMES - POSTGRADO I NCE POSTGRADO` | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; el listado y el informe requieren este permiso |

## Informes migrados

Todos los informes de Postgrado usan el mismo permiso `PERMISSION_REPORT_VIEW_POSTGRADUATE`.
El permiso anterior `PERMISSION_REPORT_ACCESS_REPORT_MODULE` solo habilita la entrada al módulo.

| Permiso SAD legado | Equivalente en SADInformes | Estado |
| --- | --- | --- |
| `ver Postgrado` + `informes acreditación` (Encuesta docente por curso) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado |
| `ver Postgrado` + `informes acreditación` (Alumnos sin contestar encuesta docente) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `informes acreditación` (Cursos vs alumnos) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| Sin validación propia en el code-behind legado (Cursos Profesores Postgrado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Inscripción masiva: consulta) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado |
| Inscripción masiva: botón Inscribir (antes sin permiso separado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; exige también el permiso de consulta |
| `ver Postgrado` + `cuadro de honor` (Inscripción encuesta salida: consulta) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| Inscripción encuesta salida: botón Inscribir (antes sin permiso separado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; exige también el permiso de consulta |
| `ver Postgrado` + `cuadro de honor` (Ranking notas ponderado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Alumnos matriculados MBA) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Profesores confirmados por cátedra) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Reporte áreas y cátedras) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Reporte Vista SAD Sigma) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Defensas de tesis) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Ayudantías Postgrado Sigma) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` (Estadísticas encuesta entrada) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` (Evaluación docente/curso) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` (Datos personales Postgrado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Registro de tesis Postgrado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` (Estadísticas ISS Postgrado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` (Estadísticas Encuesta Alumni) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `informes acreditación` (Avances AFE) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` (Próximas Licenciaturas Pregrado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` (Actividades Cursos Postgrado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Pago Derecho Básico: consulta) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; requiere asignación en Keycloak |
| Pago Derecho Básico: Guardar (antes sin permiso separado) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; exige también el permiso de consulta |
| `ver Postgrado` + `cuadro de honor` (Rankings) | `PERMISSION_REPORT_VIEW_POSTGRADUATE` (propuesto) | Pendiente |
| `INFORMES - POSTGRADO I NCE POSTGRADO` | `PERMISSION_REPORT_VIEW_POSTGRADUATE` | Implementado; reemplaza el iframe/cookie de Maestro |
| `INFORMES - MANTENEDORES POSTGRADO NCE POSTGRADO` | — | Gestión no migrada; `Bloquear/Activar` sigue en Maestro. |

Los nombres propuestos **no existen todavía** en Keycloak: se definen al migrar
cada informe, con su prueba de autorización.

Programación Docente Postgrado X Programa y Modalidad agrega el permiso
`PERMISSION_REPORT_VIEW_POSTGRADUATE`. Su página legada no
incluye una validación específica en el code-behind; la migración protege tanto
el catálogo como el listado con este permiso o los roles administrativos REPORT.
No se crean roles ni asignaciones de Keycloak automáticamente.

Cursos Profesores Postgrado agrega `PERMISSION_REPORT_VIEW_POSTGRADUATE`.
Además del permiso, el alcance de datos depende del usuario SAD configurado en
AppConfig (`PostgraduateUser`, con `mvenegas` como valor nulo predeterminado):
la API solo devuelve los departamentos visibles para ese usuario y rechaza el
resto antes de ejecutar el informe. El usuario no se elige desde el cliente.

## Diferencias de comportamiento a tener presentes

1. El SAD consultaba la tabla `Permisos` en cada sesión y aceptaba `*` como acceso
   total. SADInformes solo confía en el token del cliente físico.
2. En el SAD, ocultar un enlace no protegía la operación. Aquí toda consulta de
   datos exige un permiso de backend; ocultar un elemento de la interfaz es solo
   presentación.
3. `consulta alumnos` expulsaba a otro módulo y `solo informes` cambiaba el
   encabezado: son diferencias de navegación del sistema legado, no permisos de
   informes, y no se replican.
4. La suplantación de usuarios del SAD no se incorporó a SADInformes.

## Cómo asignar acceso

1. Crear en Keycloak, dentro del cliente `SADInformes`, el rol
   `PERMISSION_REPORT_ACCESS_REPORT_MODULE` y los permisos `PERMISSION_REPORT_*` que existan.
2. Asignar a cada usuario o grupo solo los permisos de los informes que necesita.
3. Verificar con un token real que `resource_access.SADInformes.roles` los incluye;
   sin eso la API responde 403 aunque la navbar muestre el destino.
