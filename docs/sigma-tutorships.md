# Ayudantías Postgrado Sigma

`/postgrado/ayudantias-sigma` reemplaza `ReporteAyudantiaSigma.aspx`. Consulta la
vista SAD de ayudantías y enriquece cada fila con el estado de remuneración en
Sigma; no modifica datos.

## Uso

1. Elegir un año desde `spRec_Años(NO, Todos)`.
2. Pulsar **Mostrar**. No se consulta al cargar la pantalla.
3. Revisar las 25 columnas: 20 de la vista SAD y 5 de Sigma.
4. La tabla pagina de a 10 filas e incluye el componente Search de SADAlumnos;
   la búsqueda filtra las 25 columnas visibles y vuelve a la primera página. La
   paginación Bootstrap muestra dos números antes y dos después de la página
   actual, con `...` cuando hay saltos, además de Anterior/Siguiente.
5. Pulsar **Descargar Excel** para exportar el resultado completo del año mostrado.

Columnas Sigma:

- `Existe sigma`: `SI` o `NO`.
- `Fecha sigma`: fecha de creación en Sigma, o vacía.
- `Paso`: mensaje derivado de `Math.Floor(estado_id)`.
- `Flag`: `🚩` cuando `bandera_id` es verdadero; `⛔` cuando es falso.
- `id remu`: identificador de remuneración.

Si Sigma no está configurado o falla, se conservan las filas SAD y las cinco
columnas Sigma quedan en `NO`/vacías.

## API

| Ruta base: `/api/reports/postgraduate/ayudantias-sigma` | Resultado |
| --- | --- |
| `GET /filters` | `{ success, message, data: { years } }` |
| `GET /?year=2024` | `{ success, message, data: { rows, total } }` |
| `GET /download?year=2024` | XLSX, hoja `ReporteAyudantiaSigma`, 25 columnas |
| Permiso de las tres rutas | `REPORT_VIEW_POSTGRADUATE_SIGMA_TUTORSHIPS`; administradores existentes conservados |
| Errores | 400 año inválido o exportación vacía; 401 sin sesión; 403 sin permiso; 503 indisponible, sin datos SQL privados |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff` y el nombre `ReporteAyudantiaSigma-{year}.xlsx`.

## Persistencia

La vista `dbo.POSTGRADO_Ayudantes_Sigma_SinHistorial` se mapea en
`Domain/Entities/Sad/Tables/dbo` con solo las 20 columnas consumidas. La vista no
declara clave; el contexto usa una clave técnica compuesta por cátedra, sección,
período, persona y centro de costo para permitir el tracking de EF. No se modifica
la vista.

Sigma se consulta con `ISigmaRemuneracionesContext`; se agregaron `EstadoId` y
`BanderaId` a `Remuneracion`, y se conserva el join por `prestacion.cod_imp`. Todos
los `COD` se consultan en un solo lote, reemplazando el N+1 legado.

## Verificación y límites

Pruebas Application cubren filtros, mapeo de 25 columnas, `PASO 2`, flag, fallback
sin Sigma, año inválido y vacío. HTTP cubre permiso, no-store, estado Sigma,
XLSX de 25 columnas y año inválido. Angular cubre filtro anual, Mostrar, 25
columnas, cancelación, descarga, Sin Datos y error sanitizado.

No sustituyen una conexión real a Sigma o SQL Server, una apertura real en Excel o
una descarga desde navegador. No se crean permisos en Keycloak ni se publican
cambios.
