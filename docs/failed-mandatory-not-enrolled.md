# Obligatorias reprobadas no inscritas

Migración de `SAD/Informes/Inscripcion/ObligatoriasReprobadasNoInscritas.aspx` a
la ruta `/academic-enrollment/failed-mandatory-not-enrolled`.

## Comportamiento

- Carga los períodos activos de toma de ramos.
- Al cambiar el período vuelve a consultar el informe.
- Conserva las cinco columnas legadas: **Código**, **Nombre**, **Tipo**,
  **Cátedra** y **Nombre Cátedra**.
- Mantiene visibles las condiciones de inclusión que documentaba la pantalla:
  - El alumno tiene al menos un ramo inscrito en el semestre en cuestión.
  - La cátedra reprobada no está inscrita en el semestre.
  - No existe solicitud de diferir para la cátedra y semestre dados.
  - La cátedra se dicta en el semestre considerado.
- Usa `app-table`, búsqueda, paginación, skeleton y estado vacío.
- No agrega exportación porque la pantalla legada no la tenía.

## Backend

| Elemento | Valor |
|---|---|
| Filtros de período | `GET /api/reports/academic-enrollment/failed-mandatory-not-enrolled/filters` |
| Informe | `GET .../report?period=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Origen | `spRec_ObligatoriasReprobadasNoInscritasSinSolicitud` |
| Períodos | `spRec_PeriodosActivosTomaRamos` |

El procedimiento consulta las bases `AdmdocIC1989` y `AdmdocCA1989` para
descartar solicitudes de diferir aceptadas o condicionales. No se modificó el
procedimiento ni el esquema.

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles. No se verificó contra SQL
Server ni en navegador real.
