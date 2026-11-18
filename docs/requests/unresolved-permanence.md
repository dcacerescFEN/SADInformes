# Alumnos con Solicitudes de Permanencia No Resueltas

Migración de
`SAD/Informes/Solicitudes/AlumnosConSolicitudesPermanenciaNoResueltas.aspx`.

## Ruta

```text
/requests/unresolved-permanence
```

## Filtros

- Carrera: IC, IICG, CA.
- El período no es un filtro: se resuelve en servidor desde `PeriodosAcademicos`
  activos, igual que SADAlumnos, y se muestra en el subtítulo.

## Backend

```text
GET /api/reports/requests/unresolved-permanence/filters
GET /api/reports/requests/unresolved-permanence/report?career=IC
```

Datos principales:

```text
spRec_AlumnosConSolicitudesPermanenciaNoResueltas(@Tipo_Alumno, @Periodo)
```

Datos por alumno:

```text
spRec_AlumnoSolicitudes(@Cod_Alumno, 'PERMANENCIA RESUELTAS')
spRec_AlumnoSolicitudes(@Cod_Alumno, 'PERMANENCIA PENDIENTES')
spRec_AlumnoReprobadasCore(@Cod_Alumno)
```

El reporte reproduce las columnas legacy: Nombre con foto y situación, Datos
Ingreso, % Crédito, Indicadores Rendimiento, Semestres, Cátedras Core
Reprobadas, UD Inscritas, Solicitudes Permanencia Anteriores/Actuales,
Causales de Eliminación, Observaciones y Restricciones.

La celda Nombre incluye la foto y la situación; no hay enlace a SADAlumnos.

Las solicitudes anteriores muestran check verde o cruz roja según la resolución;
las causales de eliminación muestran un bullet rojo. Los colores usan los tokens
de `variables.scss`.

Las solicitudes anteriores muestran check verde o cruz roja según la resolución;
las causales de eliminación muestran un bullet rojo. Los colores usan los tokens
de .

## Rendimiento

La implementación inicial consulta los datos anidados por alumno. Las definiciones
de `spRec_SolicitudesAnteriores`, `spRec_SolicitudesActuales` y
`spRec_AlumnosReprobadasCore` no están en el repositorio, por lo que se usan los
SP por alumno disponibles. Una fase posterior puede reemplazarlos por consultas
batch si el volumen lo exige.

No se modificaron procedimientos ni esquema. No se verificó contra SQL Server ni
navegador real.
