# Cursos vs. alumnos

Ruta: `/postgrado/cursos-por-alumnos`. Usa el formato de Encuesta docente por curso:
cabecera, tres selectores, Mostrar, tabla ordenable, total y Exportar CSV.

## Consulta

- Semestre ingreso: filtra `Alumnos.Sem_Ingreso`, no el período del curso.
- Tipo alumno y modalidad: selección concreta obligatoria; sin Todos ni comodines.
- Se muestran Curso, Cátedra, Código Alumno, Nombre Alumno y Semestre Ingreso.
- El SP incluye inscripciones CURSADA en cursos no eliminados; puede devolver cursos
  de distintos períodos para alumnos del mismo semestre de ingreso.
- CSV: `CursosXAlumnos.csv`, separador punto y coma y BOM UTF-8.

## API y acceso

`GET /api/reports/postgraduate/courses-by-students/filters?tipoAlumno=MBA`
devuelve períodos, tipos sin Todos y modalidades del tipo seleccionado.
Sin tipo, las modalidades permanecen vacías.

`GET /api/reports/postgraduate/courses-by-students?periodo=2024A&tipoAlumno=MBA&modalidad=PT`
devuelve `{ success, message, data: { rows, total } }`. Los filtros incompletos o
comodines responden 400 antes de consultar datos; los errores de almacenamiento
responden 503 sin detalles SQL. Sin sesión responde 401; sin permiso responde 403.

Ambas rutas requieren `PERMISSION_REPORT_VIEW_POSTGRADUATE_COURSES_BY_STUDENT`
en el cliente físico configurado; también se admiten los roles administrativos
REPORT existentes. El permiso de entrada o el de Encuesta docente no bastan.
No se crean roles ni se asignan accesos en Keycloak automáticamente.

## Dependencias

Usa `ConnectionStrings:Sad` y el procedimiento existente
`dbo.spRec_AlumnosXPeriodoXTipo_AlumnoXModalidad`. Los catálogos usan
`spRec_PeriodosAcademicosProgramacion` (POSTGRADO, ConTodos=NO),
`spRec_TiposAlumno` (IncluirTodos=NO) y `spRec_TiposAlumnosModalidades`.

No se modifican SQL, tablas ni registros. Las pruebas automatizadas usan dobles
de lectura: la ejecución contra SAD y la asignación real de permisos requieren
verificación en el entorno autorizado.
