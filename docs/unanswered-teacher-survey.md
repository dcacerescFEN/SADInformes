# Alumnos sin contestar encuesta docente — API

API de solo lectura mediante Queries de Application y el contrato tipado ISadContextProcedures.

## Semántica heredada

Se reutiliza `dbo.spRec_Alumnos_Sin_Evaluacion_Docente`, sin modificar SQL ni tablas.
El SP devuelve inscripciones CURSADA en cursos no eliminados del período exacto,
tipo de alumno y modalidad. Exige ausencia de registros tanto en
`Evaluacion_Docente_curso` como en `Evaluacion_Docente_Docentes` para ese
alumno/curso/sección/período: si existe cualquiera de las dos evaluaciones,
el alumno no aparece. No se amplía la consulta a quienes completaron solo una.

El texto Decreto del ASPX legado no tenía un control ni un parámetro SQL; no se
incorpora como filtro. Los enlaces antiguos a alumnos/cursos de otra aplicación
se presentan como texto, siguiendo los informes actuales.

## API y permisos

- `GET /api/reports/postgraduate/unanswered-teacher-survey/filters?tipoAlumno=MBA`.
- `GET /api/reports/postgraduate/unanswered-teacher-survey?periodo=2024A&tipoAlumno=MBA&modalidad=PT`.

El listado devuelve `{ success, message, data: { rows, total } }`. Sin tipo
seleccionado, el catálogo devuelve modalidades vacías. Se reutilizan
`spRec_PeriodosAcademicosProgramacion` (POSTGRADO, ConTodos=NO),
`spRec_TiposAlumno` (IncluirTodos=NO) y `spRec_TiposAlumnosModalidades`.

Ambas rutas requieren
`PERMISSION_REPORT_VIEW_POSTGRADUATE_UNANSWERED_TEACHER_SURVEY` en el cliente
físico configurado. Los roles REPORT ADMIN/SUPERADMIN existentes también permiten
consultar. El permiso de entrada o el de otro informe no bastan. La aplicación
no crea roles ni asignaciones en Keycloak.

Sin sesión devuelve 401; sin permiso, 403. Los filtros vacíos, Todos, comodines
o valores que exceden el contrato varchar (20/30/30) se rechazan con 400 antes de
consultar datos. Los fallos de almacenamiento devuelven 503 sanitizado.
