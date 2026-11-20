# Alumnos que cumplen requisitos y no han abierto expediente

Migración de `SAD/Informes/AplicacionReglamento/ExpedientesNoAbiertos.aspx`.

## Permisos

- Entrada a la sección: `informes aplicación reglamento` legacy →
  `PERMISSION_REPORT_VIEW_REGULATION`.
- La API exige además `PERMISSION_REPORT_ACCESS_REPORT_MODULE`.
- El legacy no tenía una validación propia en el code-behind de esta página.

## Datos

- Endpoint: `GET /api/reports/regulation/unopened-files/report`.
- SP: `spRec_ExpedientesNoAbiertos`, sin parámetros.
- Solo lectura; sin filtros ni exportación legacy.

## Tabla

Nombre, Foto, Título/Grado, Rut, Carrera, Situación Académica, Promoción,
Teléfono(s), Correo Fen y Correo Personal.

El nombre enlaza a la ficha nueva de SADAlumnos:

```text
legacy: alumnos/default.aspx?modo=34&codigo=<Codigo>
nuevo:  students/detail/<Codigo>/academic-status/file-opening
```

`modo=34` corresponde a **Apertura expediente**. La foto usa `Cod_Persona`
(mostrado como Rut) y cae a `perfilNN.jpg` cuando no hay código.

## Aviso

Se mantiene el aviso legacy: el SAD no valida deudas a nivel de universidad.

## Ruta

```text
/regulation/unopened-files
```
