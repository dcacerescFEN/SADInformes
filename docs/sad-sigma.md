# Reporte Vista SAD Sigma

`/postgrado/vista-sad-sigma` reemplaza `ReporteVistaSadSigma.aspx`. Consulta el
reporte SAD y completa el estado de pago en Sigma por año; no modifica datos.

## Uso

1. Elegir un año desde `spRec_Años(NO, Todos)`.
2. Pulsar **Mostrar**. No se consulta al cargar la pantalla.
3. Revisar las 37 columnas del SP y, al final, `Creado_En_Sigma`.
4. Pulsar **Descargar Excel** para exportar el resultado mostrado.

`Existe_En_Sigma` muestra `SI` cuando el `COD` aparece en Sigma o `NO` cuando no
aparece. `Creado_En_Sigma` muestra la fecha de creación encontrada. Si Sigma no
está configurado, no responde o la consulta falla, se conserva el fallback legado:
`NO` y fecha vacía; el reporte SAD sigue disponible.

La tabla pagina de a 10 filas e incluye el componente Search de SADAlumnos; la
búsqueda filtra las 38 columnas visibles y vuelve a la primera página. La
paginación Bootstrap muestra dos números antes y dos después de la página actual,
con `...` cuando hay saltos, además de Anterior/Siguiente. La exportación mantiene
el resultado completo del año mostrado.

## Configuración externa

La consulta usa el contexto EF **`SigmaRemuneracionesContext`** (solo `remuneracion` y `prestacion`) y una connection string **opcional**:

```text
ConnectionStrings__SigmaRemuneraciones=<connection-string>
```

No se guardan credenciales en el repositorio. Sin esa variable, el contexto
devuelve un resultado vacío sin abrir conexiones y el reporte usa `NO`/vacío. La
consulta es LINQ de solo lectura, hace join entre `prestacion` y `remuneracion`, y
se agrupa por los `COD` del resultado para evitar el N+1 del legado.

## API

| Ruta base: `/api/reports/postgraduate/sad-sigma` | Resultado |
| --- | --- |
| `GET /filters` | `{ success, message, data: { years } }` |
| `GET /?year=2024` | `{ success, message, data: { rows, total } }` |
| `GET /download?year=2024` | XLSX con 38 columnas |
| Permiso de las tres rutas | `REPORT_VIEW_POSTGRADUATE_SAD_SIGMA`; administradores existentes conservados |
| Errores | 400 año inválido o exportación vacía; 401 sin sesión; 403 sin permiso; 503 indisponible, sin datos SQL privados |

Las respuestas autenticadas usan `Cache-Control: no-store`; el archivo lleva
`X-Content-Type-Options: nosniff`.

## Contrato y exportación

`SadContextProcedures.SpRec_CatedrasAreasXPagosProfesoresAsync` llama a
`dbo.spRec_CatedrasAreasXPagosProfesores` con `Año` varchar(10) nullable,
retorno opcional y cancelación mediante `SqlQueryAsync`. No se modifica el
procedimiento ni el esquema.

Application conserva el orden del SP. El XLSX mantiene las 37 columnas SQL en su
orden y agrega `Creado_En_Sigma` al final, como la grilla legado. Las fechas y
decimales se formatean de manera invariante para que el texto sea determinista;
no se recalculan escenarios, alumnos ni promedios. Reutiliza `TextWorkbook`:
celdas `inlineStr`, sin fórmulas ni dependencias Excel adicionales.

El proveedor `MySql.EntityFrameworkCore` y el contexto se usan únicamente en
Infrastructure. La connection string se lee de configuración y nunca se incluye
en logs ni en la respuesta.

## Verificación y límites

Pruebas Application cubren catálogo de años, orden, 38 campos, enriquecimiento
Sigma y fallback `NO`/vacío sin llamar a SQL o Sigma cuando el año es inválido.
Pruebas HTTP cubren permiso, no-store, 400/503, XLSX de 38 columnas y ausencia de
adjunto vacío. La prueba del contexto cubre ausencia de connection string sin red.
Angular cubre filtro anual, Mostrar, 38 columnas, cancelación y descarga.

No sustituyen una conexión real a Sigma, SQL Server institucional, apertura en
Excel ni descarga desde navegador. No se crean permisos en Keycloak, se reinician
procesos ni se publican cambios.
