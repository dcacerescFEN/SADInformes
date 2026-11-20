# Ranking de Licenciatura y Egreso

Migración de `SAD/Informes/AplicacionReglamento/Ranking.aspx`.

## Permisos

- Entrada a la sección: `informes aplicación reglamento` legacy →
  `PERMISSION_REPORT_VIEW_REGULATION`.
- La API exige además `PERMISSION_REPORT_ACCESS_REPORT_MODULE`.

## Datos

- Filtros: `GET /api/reports/regulation/ranking/filters`.
  Los períodos vienen de `spRec_PeriodosAcademicos '00000', 0, 'NO', '%'`.
  Es importante enviar `@Tipo = '%'`: con `NULL` el SP no devuelve períodos.
- Informe: `GET /api/reports/regulation/ranking/report`.
- SP: `spRec_RankingEgresoXGrupo`.
- Parámetros: `Grupo`, `Periodo`, `RankingPorPeriodo`, `OrdenarPor`, `Orden`.
- Solo lectura; sin exportación legacy.

## Filtros

Grupo/título:

```text
ICMA, ICME, IICG, CA, LICADM, LICECO, LICINF
```

Tipo de selección:

```text
PROMOCION, INGRESO
```

Orden:

```text
RANKING, NOMBRE
```

Dirección:

```text
ASC, DESC
```

## Tabla

Código, Nombre, Decreto, Promedio, Promoción, Semestre de Ingreso y Lugar
(`Ranking de Total`).

El código enlaza a la ficha nueva de SADAlumnos:

```text
legacy: alumnos/default.aspx?codigo=<Cod_Alumno>&modo=7
nuevo:  students/detail/<Cod_Alumno>/grade-reports/progress
```

`modo=7` corresponde a **Avance**.

## Ruta

```text
/regulation/ranking
```
