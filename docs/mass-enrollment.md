# Inscripción masiva de Postgrado

`/postgrado/inscripcion-masiva` permite consultar alumnos y registrar su inscripción
administrativa real. A diferencia de los informes anteriores, esta pantalla escribe
datos académicos y auditorías.

## Uso

1. Elegir tipo alumno, modalidad, cohorte, período y curso/sección.
2. Presionar **Actualizar** y seleccionar alumnos no inscritos, individualmente
   o con **Inscribir** en la cabecera de la tabla.
3. Presionar **Inscribir** y confirmar
   `Desea inscribir a los alumnos seleccionados?`.
4. La pantalla informa la cantidad confirmada y actualiza el listado.
   Si falla la operación, actualizar antes de reintentar; no hay reintentos automáticos.

Los filtros no ofrecen Todos. Cambiar filtros descarta el listado y la selección
anteriores. La tabla conserva foto, nombre, código, Rut, ingreso, situación, motivo,
estado de inscripción y decreto. Las fotos usan el servicio institucional existente;
si fallan, se usa su imagen de perfil predeterminada.

## Acceso y auditoría

| Operación | Permiso en el cliente físico configurado |
| --- | --- |
| Catálogos y listado | `PERMISSION_REPORT_VIEW_POSTGRADUATE_MASS_ENROLLMENT` |
| Inscripción | El anterior más `PERMISSION_REPORT_MANAGE_POSTGRADUATE_MASS_ENROLLMENT` |

Se conservan los roles globales REPORT ADMIN/SUPERADMIN. El permiso de entrada al
módulo o el de otro informe no permiten consultar o inscribir por sí solos.
La asignación de roles en Keycloak es externa: la aplicación no la modifica.

El catálogo de tipos usa el usuario autenticado, no el usuario fijo del legado.
La identidad debe existir y ser compatible con `Usuarios` del SAD para que el
catálogo aplique su alcance; no se omite esa restricción mediante un usuario vacío.
El SP acepta usuario de hasta 20 caracteres: se rechazan identidades más largas
en lugar de truncarlas. Usuario e IP de auditoría provienen del servidor, nunca
del JSON enviado por el cliente. Se registra `SistemaProcesa='SADInformes'`.

## API

Base: `/api/reports/postgraduate/mass-enrollment`.

- `GET /filters`: parámetros opcionales `studentType`, `modality`, `cohort`,
  `period`, `course`. Devuelve `studentTypes, modalities, cohorts, periods, courses`.
- `GET /`: exige los cinco filtros concretos. Devuelve `{ rows, total }`.
- `POST /`: recibe la selección mostrada y los códigos elegidos:

```json
{
  "selection": {
    "studentType": "MBA",
    "modality": "PT",
    "cohort": "2024",
    "period": "2024A",
    "course": "MBA00101/1/0"
  },
  "studentCodes": ["A1", "A2"],
  "confirmed": true
}
```

Las respuestas exitosas usan `{ success, message, data }`. El POST devuelve
`data.enrolled`. Filtros inválidos, IDs duplicados o falta de confirmación dan 400;
selección ajena al listado o ya inscrita, 409. Sin sesión, 401; sin permiso, 403.
Errores de almacenamiento dan 503 sin detalles SQL. Un error al confirmar puede
tener resultado incierto: actualizar el listado antes de cualquier reintento.

## Persistencia y reglas heredadas

Se reutilizan `spRec_TiposAlumno`, `spRec_TiposAlumnosModalidades`, `spRec_Años`,
`spRec_PeriodosAcademicosFiltrados`, `spRec_CursosxPeriodo`,
`spRec_Alumnos_insc_masiva_catedra_disp` y
`spCre_MovInscAdministrativo_Postgrado`. Conexión: `ConnectionStrings:Sad`.
No se alteran procedimientos ni tablas.

Antes de escribir se revalidan catálogos, curso y todos los alumnos seleccionados.
El lote usa una transacción exterior Serializable y `XACT_ABORT ON`; contiene las
transacciones anidadas y auditorías del SP. Antes del commit se comprueban tanto
la inscripción CURSADA de la sección elegida como el movimiento aceptado. Un fallo
o un no-op del SP aborta el lote, en lugar de anunciar éxito parcial.

La consulta heredada no recibe período: su estado Inscrito considera registros
anteriores de la cátedra. Extrae los primeros ocho caracteres del valor del curso;
se conserva esa regla. La modalidad no filtra directamente el SP de cursos y
el tipo participa en su validación de vacantes: no se modifica ese SQL.
Filas repetidas del mismo alumno se agrupan; si alguna indica SI, no es seleccionable.

## Verificación antes de habilitar uso real

Las pruebas HTTP usan un gateway aislado con rollback simulado; las pruebas de
mapeo usan procedimientos dobles. Angular verifica el DOM y la confirmación sin
enviar inscripciones a SAD. Esto no prueba el motor SQL Server ni las auditorías
reales de los procedimientos institucionales.

En una copia descartable y autorizada de SAD, verificar inscripción y auditorías,
rollback completo cuando falla el segundo alumno, no-op por plan incompatible,
dos solicitudes concurrentes y repetición del mismo lote. Validar además en un
navegador con usuario y permisos reales. No ejecutar estos ensayos en producción.
