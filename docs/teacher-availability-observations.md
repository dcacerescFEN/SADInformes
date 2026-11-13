# Disponibilidad Observaciones

Migración de `SAD/Informes/Inscripcion/DispObservaciones.aspx` a
`/academic-enrollment/teacher-availability-observations`.

## Fase implementada

Solo lectura.

- Períodos desde `spRec_PeriodosAcademicos` con límite `20082`, modo `0`,
  `ConTodos='NO'` y tipo `SEMESTRAL`.
- Observaciones desde `spRec_FE_DisponibilidadObservaciones`.
- Columnas: Cod Profesor, Nombre y Observaciones.
- Búsqueda, skeleton y estado vacío compartidos.

## Backend

```text
GET /api/reports/academic-enrollment/teacher-availability/observations?period=...
```

Requiere `REPORT_VIEW_AVAILABILITY` y responde `Cache-Control: no-store`.
Errores de validación `400`; fallos SQL/configuración `503` sanitizados.

No se modificaron procedimientos ni esquema. No se verificó contra SQL Server ni
en navegador real.
