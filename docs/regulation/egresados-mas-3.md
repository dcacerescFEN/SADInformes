# Alumnos egresados más de 3 años

Migración de `SAD/Informes/AplicacionReglamento/EgresadosMas3.aspx`.

## Permisos

- Entrada a la sección: `informes aplicación reglamento` legacy →
  `PERMISSION_REPORT_VIEW_REGULATION`.
- La API exige además `PERMISSION_REPORT_ACCESS_REPORT_MODULE`.
- `consulta alumnos` y `solo informes` eran comportamiento de navegación del
  SAD; no se migran como permisos del informe.

## Datos

- Endpoint: `GET /api/reports/regulation/graduates-over-3-years/report`.
- SP: `spRec_AlumnosEgresadosMasTresAños`.
- Parámetro: período académico activo (`PeriodosAcademicos`, tipo `SEMESTRAL`).
  Reemplaza `SemestreDefecto`.
- Sin filtros legacy y sin exportación.
- Solo lectura.

## Tabla

Código, Nombre, Carrera (`Tipo_Alumno`), Decreto, Situación, Egresado de,
Semestre de Ingreso y Promoción.

El código enlaza a la ficha nueva de SADAlumnos:

```text
legacy: alumnos/default.aspx?codigo=<Cod_Alumno>&modo=4
nuevo:  students/detail/<Cod_Alumno>/grade-reports/grades
```

`modo=4` corresponde a **Notas**.

## Ruta

```text
/regulation/graduates-over-3-years
```
