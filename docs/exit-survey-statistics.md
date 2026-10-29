# Estadísticas encuesta de salida — P1

La ruta `/postgrado/estadisticas-encuesta-salida` ya abre el shell migrado de
`EncuestaSalidaGraduados/Default.aspx`: filtros, muestra y navegación por
secciones. Las secciones de resultados y los gráficos se implementan en fases
posteriores; por ahora muestran un placeholder explícito.

## P1 entregado

- Filtros Período ingreso, Tipo Alumno y Modalidad. La lista de tipos conserva
  los 13 valores legados; período y modalidad salen de sus SP canónicos.
- Muestra desde `spRec_EncuestaGraduados_Estadisticas`: Tipo, Modalidad, Muestra,
  Incompletas y Contestadas, con totales de footer calculados por el servidor.
- Cambios de tipo recargan modalidades; cambios de filtros cancelan la consulta
  anterior y no permiten que una respuesta vieja reemplace la nueva.
- Tabs de las 14 secciones con placeholder; no se inventan datos ni se ejecutan
  consultas de secciones todavía no migradas.
- Permiso de solo lectura `REPORT_VIEW_POSTGRADUATE_EXIT_SURVEY_STATISTICS`.
  Las respuestas autenticadas llevan `Cache-Control: no-store`.

## API P1

| Ruta base: `/api/reports/postgraduate/exit-survey-statistics` | Resultado |
| --- | --- |
| `GET /filters?studentType=MBA` | Períodos, tipos y modalidades dependientes |
| `GET /sample?period=2024A&studentType=Todos&modality=Todos` | Filas y totales de muestra |
| Errores | 400 filtros inválidos, 401 sin sesión, 403 sin permiso, 503 saneado |

Los wrappers usan parámetros tipados: período varchar(5), tipo varchar(30),
modalidad varchar(10). No se copia el SQL dinámico de las funciones legadas ni se
modifican SP, funciones, vistas o tablas.

## Decisiones de alcance

- Gráficos: CSS/SVG nativo, sin Highcharts ni otra dependencia.
- Navegación: una página con tabs; no se agregan subrutas.
- Exportación: solo pantalla; no PNG, print, CSV ni Excel en esta migración.
- Defectos heredados: se corregirá el mapeo visible sin cambiar SQL. Los defectos
  de Q9 y DetalleProfesor se documentarán en P4.
- Q1-Q7 Likert, Q8/Q11, docentes/detalle, Resumen y gráficos quedan para P2-P6.

## Verificación P1

14 pruebas backend cubren filtros, muestra, totales, nulos, validación, permisos,
no-store y errores saneados. 5 pruebas de componente y 12 de navegación cubren
carga automática, cambio dependiente de modalidad, tabs, muestra vacía,
cancelación de respuestas obsoletas y errores. Suites completas observadas: 293 pruebas .NET, 38 Node, 188/193 Angular y
build de producción correcto. Las cinco fallas Angular son las ya conocidas de
tema/favoritos por `localStorage` ausente bajo Node 26, ajenas a esta pantalla.
No se probó contra SQL Server, Keycloak ni un navegador real. No hay commits ni
cambios de base de datos.
