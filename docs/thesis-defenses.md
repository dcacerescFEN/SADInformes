# Defensas de tesis Postgrado

`/postgrado/defensas-tesis` reemplaza `ReporteProfesoresNuevo.aspx`. Consulta los
dos formatos de comisiones de defensa y exporta cada uno a Excel; no modifica
datos ni usa SSRS.

## Uso

1. Elegir **Año Defensa**, **Tipo Alumno**, **Modalidad** y **Año Ingreso**. Todos
   incluyen la opción `Todos` (`Todas` para modalidad), que el SP interpreta como
   filtro deshabilitado.
2. Al cambiar el tipo de alumno se recargan sus modalidades; siempre se vuelve a
   agregar `Todos`, preservando el comportamiento legado.
3. Pulsar **Buscar**. No se consulta al cargar la pantalla.
4. Revisar los dos resultados en pestañas separadas:
   - **Coordinación**: 20 columnas.
   - **Tesorería**: 17 columnas.
5. Exportar cada formato con su botón en el encabezado, junto al título; cada botón se habilita cuando su resultado tiene filas.

Cada grilla pagina de a 10 filas e incluye el componente Search de SADAlumnos.
La búsqueda filtra las columnas visibles de esa grilla y vuelve a su primera
página. La paginación Bootstrap muestra dos números antes y dos después de la
página actual, con `...` cuando hay saltos, además de Anterior/Siguiente.

Los dos SP se llaman con los mismos cuatro valores y devuelven formas distintas;
no se consolidan. Si ambos resultados están vacíos se muestra **Sin Datos**.

## API

| Ruta base: `/api/reports/postgraduate/thesis-defenses` | Resultado |
| --- | --- |
| `GET /filters?studentType=MBA` | `{ years, studentTypes, modalities }` |
| `GET /?defenseYear=Todos&studentType=MBA&modality=Todos&entryYear=Todos` | `{ coordination, treasury, coordinationTotal, treasuryTotal }` |
| `GET /download/coordination` con los cuatro filtros | XLSX, hoja `InformeComision`, 20 columnas |
| `GET /download/treasury` con los cuatro filtros | XLSX, hoja `InformeTesorería`, 17 columnas |
| Permiso de las cuatro rutas | `REPORT_VIEW_POSTGRADUATE_THESIS_DEFENSES`; administradores existentes conservados |
| Errores | 400 filtros inválidos o exportación vacía; 401 sin sesión; 403 sin permiso; 503 indisponible, sin datos SQL privados |

Las respuestas autenticadas usan `Cache-Control: no-store`; los archivos llevan
`X-Content-Type-Options: nosniff`. Los nombres incluyen los cuatro filtros.

## Contrato

Los wrappers tipados llaman a `SpRec_InformePostgradoProfesoresGCI` y
`SpRec_InformePostgradoProfesoresGCI2` con `AnioDefensa`, `TipoAlumno`,
`Modalidad` y `AnioIngreso` varchar(100) nullable, retorno opcional y
cancelación. Los catálogos usan `SpRecTipoAlumnosPostgrado` y
`SpRecModalidadesPostgrado`. Application conserva el orden de cada SP y no
recalcula notas, estados ni fechas.

Los XLSX reutilizan `TextWorkbook` y conservan los encabezados originales. La
fecha de examen de Tesorería se formatea de forma determinista; no se altera el
tipo de fecha del SP.

## Defectos legados documentados

- Las excepciones de búsqueda se silenciaban en el code-behind.
- Las llamadas construían SQL por interpolación; la migración usa parámetros tipados.
- `Todos` y la cadena vacía deshabilitan cada filtro en ambos SP.
- Cambiar el tipo de alumno limpiaba las modalidades y dejaba sólo `Todas`; la
  migración recarga el catálogo correspondiente y conserva el `Todos` explícito.

## Verificación y límites

Pruebas Application cubren cascada, los cuatro argumentos idénticos en ambos SP,
las dos formas de 20/17 campos, `Todos`, rechazo antes de llamar a los reportes y
vacío. HTTP cubre permiso, no-store, reporte, ambas descargas, 400/503 y ausencia
de adjunto vacío. Angular cubre filtros, cascada, dos grillas y dos descargas.

No sustituyen ejecución contra SQL Server institucional, apertura real en Excel o
descarga desde navegador. No se crean permisos en Keycloak, se reinician procesos
ni se publican cambios.
