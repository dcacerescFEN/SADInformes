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
| `Viñeta Informes` | `PERMISSION_REPORT_ACCESS` | Muestra el destino Informes en la navbar. |
| `ver Postgrado` | `PERMISSION_REPORT_ACCESS` | Habilita la sección Postgrado; ya no es un permiso independiente. |
| `consulta alumnos` | — | En el SAD expulsaba al módulo de alumnos. No se migra como permiso de informes. |
| `solo informes` | `PERMISSION_REPORT_ACCESS` | Solo ocultaba botones del encabezado; el nuevo shell no los tiene. |
| `informes acreditación` | `PERMISSION_REPORT_ACCESS` | Era un requisito adicional en varias páginas de Postgrado. |
| `cuadro de honor` | `PERMISSION_REPORT_ACCESS` | Requisito adicional de rankings; hoy lo cubre el permiso del informe. |
| `encuesta egresados` | `PERMISSION_REPORT_ACCESS` | Acceso a estadísticas de salida cuando se migren. |
| `reporte proximas licenciaturas` | `PERMISSION_REPORT_ACCESS` | Solo controlaba la visibilidad del enlace. |
| `INFORMES - MANTENEDORES POSTGRADO` | `PERMISSION_REPORT_ACCESS` | El acceso real lo define cada sistema externo. |
| `INFORMES - MANTENEDORES POSTGRADO CONSTANCIAS` | — | Permiso de una aplicación externa; sigue en su sistema. |
| `INFORMES - MANTENEDORES POSTGRADO D Y C` | — | Permiso de una aplicación externa; sigue en su sistema. |
| `INFORMES - MANTENEDORES POSTGRADO NCE POSTGRADO` | — | Permiso de una aplicación externa; sigue en su sistema. |
| `INFORMES - POSTGRADO I NCE POSTGRADO` | — | Permiso de una aplicación externa; sigue en su sistema. |

## Informes migrados

| Permiso SAD legado | Equivalente en SADInformes | Estado |
| --- | --- | --- |
| `ver Postgrado` + `informes acreditación` (Encuesta docente por curso) | `PERMISSION_REPORT_VIEW_POSTGRADUATE_TEACHER_SURVEY` | Implementado |
| `ver Postgrado` + `informes acreditación` (Alumnos sin contestar encuesta docente) | `PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `informes acreditación` (Cursos vs alumnos) | `PERMISSION_REPORT_VIEW_POSTGRADUATE_COURSES_BY_STUDENT` | Implementado; requiere asignación en Keycloak |
| `ver Postgrado` + `cuadro de honor` (Inscripción masiva: consulta) | `PERMISSION_REPORT_VIEW_POSTGRADUATE_MASS_ENROLLMENT` | Implementado |
| Inscripción masiva: botón Inscribir (antes sin permiso separado) | `PERMISSION_REPORT_MANAGE_POSTGRADUATE_MASS_ENROLLMENT` | Implementado; exige también el permiso de consulta |
| `ver Postgrado` + `cuadro de honor` (Rankings) | `PERMISSION_REPORT_VIEW_POSTGRADUATE_RANKING` (propuesto) | Pendiente |
| `Admin firma Postgrado` | `PERMISSION_REPORT_MANAGE_POSTGRADUATE_SIGNATURE` (propuesto) | Pendiente; además requiere `UPLOAD` para imágenes |
| `INFORMES - POSTGRADO I NCE POSTGRADO` | Integración externa | Pendiente de definir contrato |

Los nombres propuestos **no existen todavía** en Keycloak: se definen al migrar
cada informe, con su prueba de autorización.

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
   `PERMISSION_REPORT_ACCESS` y los permisos `PERMISSION_REPORT_*` que existan.
2. Asignar a cada usuario o grupo solo los permisos de los informes que necesita.
3. Verificar con un token real que `resource_access.SADInformes.roles` los incluye;
   sin eso la API responde 403 aunque la navbar muestre el destino.
