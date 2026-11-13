# Disponibilidad Horaria

Migración de `SAD/Informes/Inscripcion/DisponibilidadHoraria.aspx` a
`/academic-enrollment/teacher-availability`.

## Fase implementada

Solo lectura. No incluye correo, eliminación, administración del período ni
mantenedor de disponibilidad.

- Período activo desde `spRec_PeriodoDisponibilidad`.
- Lista desde `spRec_FE_ProgramacionDocente`.
- Contadores de pendientes y OK.
- Tabla con Código, Cátedra, Profesor, Necesarias, Dadas, Disp., Estado y
  Observaciones.
- Búsqueda, skeleton y estado vacío compartidos.

## Backend

```text
GET /api/reports/academic-enrollment/teacher-availability/report
```

Requiere `REPORT_VIEW_AVAILABILITY` y responde `Cache-Control: no-store`.
Errores de validación `400`; fallos SQL/configuración `503` sanitizados.

No se modificaron procedimientos ni esquema. No se verificó contra SQL Server ni
en navegador real.
