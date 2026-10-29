# Inscripción encuesta de salida

`/postgrado/inscripcion-encuesta-salida` reemplaza
`InscripcionEncuestaSalida.aspx`: lista alumnos por tipo, modalidad y año de
egreso/titulación, y permite inscribirlos en la encuesta de salida. No usa SSRS.

## Uso

1. Elegir tipo de alumno, modalidad y año. El listado se actualiza al cambiar los
   filtros, sin botón Mostrar, como el ASPX original. **Todos** se conserva porque
   el procedimiento de consulta lo soporta.
2. Revisar Tipo Alumno, Modalidad, foto, Rut, Nombre, Código de alumno, Semestre
   de ingreso, fechas de egreso/titulación, Inscrito y estado de la encuesta.
3. Marcar alumnos no inscritos, o usar **Inscribir** del encabezado para todos los
   elegibles.
4. Pulsar **Inscribir**. Se solicita confirmación; luego se guarda el lote.
5. Las filas ya inscritas no se pueden seleccionar. Si están inscritas y no tienen
   porcentaje, el estado muestra **Sin contestar**.

La foto usa `Cod_Persona` como el legado, con fallback `perfilNN.jpg`. El código
abre la ficha moderna de SADAlumnos solo cuando NavigationService tiene un destino
Alumnos autorizado; en caso contrario se muestra como texto. Cambiar filtros
cancela la consulta anterior y descarta selecciones.

## API y permisos

| Ruta base: `/api/reports/postgraduate/exit-survey-enrollment` | Resultado |
| --- | --- |
| `GET /filters?studentType=MBA` | Tipos (incluye Todos), modalidades dependientes del tipo y años |
| `GET /?studentType=Todos&modality=Todos&year=2024` | `{ rows, total, canEnroll }` |
| `POST /` | Inscripción confirmada; cuerpo con filtros, códigos y `confirmed: true` |
| Permiso de lectura | `REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_ENROLLMENT` |
| Permiso de escritura | `REPORT_MANAGE_POSTGRADUATE_EXIT_SURVEY_ENROLLMENT` |
| Respuestas | 400 validación (incluye códigos duplicados), 401 sin sesión, 403 sin permiso, 409 selección obsoleta/no listada, 503 fallo saneado |
| Caché | `Cache-Control: no-store` en catálogos, listado y escritura |

`canEnroll` informa si el usuario puede escribir; la UI deshabilita selección y
botón cuando es falso. El servidor vuelve a exigir el permiso de escritura.

## Escritura segura

El comando recibe solo filtros y códigos de alumno. **No acepta fechas ni
períodos desde el navegador.** En una transacción Serializable:

1. Revalida tipo, modalidad, año y cada código contra el listado vigente.
2. Consulta `Encuesta_Salida_Graduados` para omitir los ya inscritos (idempotencia).
3. Deriva el período del sufijo del código (`...-20241`), igual que el formato
   legado, y toma `Fecha_Egreso` del listado del servidor.
4. Invoca `spCre_EncuestaGraduados_Ins_Alumno` con parámetros tipados.
5. Verifica que todas las filas nuevas existan antes de confirmar.
6. Hace commit solo si todo el lote se completó; cualquier fallo revierte todo.

El resultado devuelve `enrolled` y `skipped`; reintentar un lote ya inscrito no
duplica filas. Una fecha de egreso nula se envía como `NULL`, no como texto vacío
(que SQL convertiría a `1900-01-01`). La comparación de códigos es
case-insensitive para seguir la collation `Modern_Spanish_CI_AS`. Los procedimientos
de lectura y escritura mantienen sus parámetros y nombres canónicos. No se escribe
SQL concatenado.

## Límites heredados

`Encuesta_Salida_Graduados` no tiene una restricción única sobre `Cod_Alumno`
(la clave incluye `Folio` identity). La comprobación dentro de Serializable y la
postcondición reducen el riesgo de duplicados, pero no pueden reemplazar una
restricción de base de datos. No se alteró el esquema.

No se probó contra SQL Server institucional, fotos, SADAlumnos ni un navegador
real. Las pruebas usan EF InMemory y dobles ADO.NET; no se reinician procesos ni
se crean permisos en Keycloak automáticamente.
