# Programación Docente Postgrado — contratos SQL

Wrappers tipados de MappingDB en ISadContextProcedures/SadContextProcedures, sin cambios de SQL ni acceso a una base real.

- spRec_ProgramacionDocenteXProgramaModalidad: varchar 20/20/30/30/7, retorno y cancelación.
- spRec_PeriodosAcademicosXTipo: tipo, escuela y límite, varchar(20).
- spRec_TiposAlumnoXEscuela: escuela varchar(20), IncluirTodos varchar(2).
- spRec_PromocionesPostgradoTodos: resultados Promocion y Descripcion.

Se conserva el valor completo de Descripcion y el parámetro SQL de Promocion con tamaño 7; no se sustituye por el código ni se recorta manualmente. Las pruebas verifican valor y metadatos antes de la ejecución, sin afirmar equivalencia contra SQL Server real.

Los resultados conservan Ud nullable y las columnas de profesores y clases. Los cuatro wrappers usan SqlQueryAsync, OutputParameter y CancellationToken como el modelo canónico.
