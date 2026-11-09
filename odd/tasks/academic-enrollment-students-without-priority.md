# Inscripción académica: alumnos sin prioridad

Migrar `SAD/Informes/Inscripcion/AlumnosSinPrioridad.aspx` a SADInformes.

## Specs

- **S1:** La ruta `/academic-enrollment/students-without-priority` deja de ser una ficha pendiente y muestra el informe funcional.
- **S2:** Conservar el título legado y las columnas `Código`, `Nombre`, `Situación` y `Tipo Alumno`; los datos provienen de `spRec_AlumnosSinPrioridad`.
- **S3:** No agregar filtros ni exportación porque la pantalla legada no los tiene. La carga es automática, con opción de actualizar y estado vacío.
- **S4:** Mantener el enlace en la sidebar de Inscripción académica y el mismo diseño de `page-header`, `form-section`, `app-table` y búsqueda compartida.

## Tasks

- T1 | S1-S2 | backend: resultado del SP, wrapper, query y endpoint | completado
- T2 | S1, S3-S4 | frontend: componente funcional, catálogo y ruta | completado
- T3 | S1-S4 | pruebas backend/frontend y documentación | completado

## Log

L1. Solicitud del usuario: «migra ahora Informes/Inscripcion/AlumnosSinPrioridad.aspx».
L2. Legacy: carga automática sin filtros ni exportación; SP `spRec_AlumnosSinPrioridad`; columnas Código/Nombre/Situación/Tipo Alumno. Implementado en `/academic-enrollment/students-without-priority`.
L3. Verificación: backend `StudentsWithoutPriorityTests` 2/2 y suite WebApi 537/537; Angular enfocado 45/45; suite completa 340/345 con los 5 fallos preexistentes de `localStorage`; Node 38/38; build Angular correcto. Sin verificación SQL/navegador real.
