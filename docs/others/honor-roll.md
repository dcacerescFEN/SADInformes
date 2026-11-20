# Cuadro de Honor

Migración de `SAD/Informes/AplicacionReglamento/CuadroHonor.aspx`.

## Permisos

- Sección: `informes otros` → `PERMISSION_REPORT_VIEW_OTHER`.
- Informe: `cuadro de honor` → `PERMISSION_REPORT_VIEW_HONOR_ROLL`.
- La API exige además `PERMISSION_REPORT_ACCESS_REPORT_MODULE`.

## Datos

- Filtros: `GET /api/reports/others/honor-roll/filters`.
  Carreras `IC`, `IICG`, `CA`; años desde `spRec_Años 'NO', 'Todos'`.
- Informe: `GET /api/reports/others/honor-roll/report`.
- SP: `spRec_AlumnosCuadroDeHonor` con `Carrera`, `AñoIngreso` y `Año`.
- Solo lectura; sin exportación legacy.

## Tabla

Posición, Foto, Nombre, Rut, Código Alumno, Semestre de Ingreso, Situación,
Promedio y Percentil.

El código enlaza a la ficha nueva de SADAlumnos:

```text
legacy: alumnos/default.aspx?modo=4&codigo=<Código>
nuevo:  students/detail/<Código>/grade-reports/grades
```

`modo=4` corresponde a **Notas**. La foto usa `Rut` (`Cod_Persona`) con fallback
`perfilNN.jpg`.

## Ruta

```text
/others/honor-roll
```
