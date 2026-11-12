# Programación Docente

Migración de `SAD/Informes/Inscripcion/ProgramacionDocenteCompacta.aspx` a la
ruta `/academic-enrollment/enrollment-teaching-schedule`.

## Comportamiento

Filtros visibles:

- **Escuela**: `Pregrado y Postgrado Full Time` (`PREGRADO`, por defecto) o
  `Postgrado Executive y Full Time` (`POSTGRADO`).
- **Período**: se obtiene de `spRec_PeriodosAcademicosProgramacion` según la
  escuela, con límite `20040`.
- **Área**: se obtiene de `spRec_AreasCatedras` e incluye la opción `Todo` (`%`).

La coordinación no se muestra, igual que el control oculto legado: se deriva en
el backend como `EPOST-TODO` para Postgrado y `%` para Pregrado.

Al cambiar la escuela se recargan los períodos y el informe; al cambiar período
o área se vuelve a consultar.

## Columnas

Conserva las 11 columnas legadas:

**Curso**, **Cátedra**, **Profesor(es)**, **Clases**, **Ayudantías**, **Sala**,
**Sala Ayudantías**, **Vacantes**, **Inscritos**, **Área**, **Rut Profesor**.

El resultado incluye `RutProfesor`, columna que el modelo scaffolded de
MappingDB no tenía. El procedimiento no entrega `UD/CR`, por lo que esa columna
del modelo legado se omite.

## Backend

| Elemento | Valor |
|---|---|
| Filtros | `GET /api/reports/academic-enrollment/enrollment-teaching-schedule/filters?school=...` |
| Informe | `GET .../report?school=...&period=...&area=...` |
| Exportación | `GET .../export?school=...&period=...&area=...` |
| Permiso | `PERMISSION_REPORT_ACCESS_REPORT_MODULE` |
| Informe | `spRec_ProgramacionDocenteResumida` |
| Períodos | `spRec_PeriodosAcademicosProgramacion` |
| Áreas | `spRec_AreasCatedras` |

La exportación usa el `TextWorkbook` compartido (XLSX, `inlineStr`, sin
fórmulas) y responde `Cache-Control: no-store` y
`X-Content-Type-Options: nosniff`.

Los errores de validación responden `400`; los fallos de SQL, timeout o
configuración responden `503` sin exponer detalles. No se modificaron
procedimientos ni esquema. No se verificó contra SQL Server ni en navegador real.
