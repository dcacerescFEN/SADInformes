# Expedientes abiertos en un período dado

Migración de `SAD/Informes/AplicacionReglamento/ExpedientesXPeriodo.aspx`.

## Permisos

- Entrada a la sección: `informes aplicación reglamento` legacy →
  `PERMISSION_REPORT_VIEW_REGULATION`.
- La API exige además `PERMISSION_REPORT_ACCESS_REPORT_MODULE`.
- El legacy no tenía validación propia en el code-behind de esta página.

## Datos

- Endpoint: `GET /api/reports/regulation/opened-files-by-period/report?from=yyyy-MM-dd&to=yyyy-MM-dd`.
- SP: `spRec_ExpedientesAbiertosXPeriodo @Desde, @Hasta`.
- Solo lectura; sin exportación legacy.
- `from` y `to` son obligatorios, deben ser fechas ISO válidas y `from <= to`.
- El backend valida `yyyy-MM-dd` y envía al SP el formato compacto `yyyyMMdd`, que SQL Server interpreta sin ambigüedad al comparar con `datetime`.

## Tabla

Nombre, Foto, Título/Grado, Rut, Carrera, Situación Académica, Promoción,
Fecha Apertura, Teléfono(s), Correo Fen y Correo Personal.

El nombre enlaza a la ficha nueva de SADAlumnos:

```text
legacy: alumnos/default.aspx?modo=34&codigo=<Codigo>
nuevo:  students/detail/<Codigo>/academic-status/file-opening
```

`modo=34` corresponde a **Apertura expediente**.

## Ruta

```text
/regulation/opened-files-by-period
```
