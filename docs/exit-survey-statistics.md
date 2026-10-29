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

## P2 entregado

Q1-Q7 están disponibles en las tabs Malla Curricular, Cuerpo Académico,
Funcionamiento, Infraestructura y recursos, Servicios, Vinculación Medio y
Networking. Una única consulta selecciona la pregunta canónica por sección.

Cada fila muestra Descripción, conteo y porcentaje de las cinco alternativas
(Muy en desacuerdo a Muy de acuerdo), promedio con un decimal y total. El footer
suma cada alternativa. Los porcentajes usan `alternativa / total` con un decimal;
si el total es cero se muestra 0,0. El promedio se presenta con formato es-CL.
Una respuesta vacía muestra **No existen datos disponibles.** Sin datos no se
inventan filas ni totales.

## API P2

| Ruta base: `/api/reports/postgraduate/exit-survey-statistics` | Resultado |
| --- | --- |
| `GET /likert?section=malla&period=2024A&studentType=Todos&modality=Todos` | Matriz de la sección Q1 |
| Secciones Q1-Q7 | `malla`, `cuerpo-academico`, `funcionamiento`, `infraestructura`, `servicios`, `vinculacion-medio`, `networking` |
| Error | Sección inválida: 400; fallo de almacenamiento: 503 saneado |

Los wrappers Q1-Q7 son métodos tipados separados y comparten el helper interno
que fija el nombre de procedimiento desde una lista cerrada. No se concatena SQL
de usuario. La consulta valida sección, período, tipo y modalidad antes de invocar
el procedimiento.

## P3 entregado

Q8 **Cátedras x Área** usa cinco alternativas: Muy Malo, Malo, Regular, Bueno y
Muy Bueno. Q11 **Conocimientos y habilidades** usa siete: Malo, Deficiente,
Insuficiente, Suficiente, Bueno, Muy Bueno y Sobresaliente. Ambas matrices
muestran conteo y porcentaje por alternativa, promedio con un decimal y total;
sus tablas comparten un componente configurable para no duplicar renderizado.

| Ruta base: `/api/reports/postgraduate/exit-survey-statistics` | Resultado |
| --- | --- |
| `GET /catedras-area?period=2024A&studentType=Todos&modality=Todos` | Filas Q8 y totales de cinco alternativas |
| `GET /skills?period=2024A&studentType=Todos&modality=Todos` | Filas Q11 y totales de siete alternativas |
| Error | Filtros inválidos 400; almacenamiento 503 saneado |

Los tipos de resultado conservan `Cod_Catedra` para Q8 y `Pregunta` para Q11.
Los wrappers son métodos tipados separados con parámetros período varchar(5),
tipo varchar(30) y modalidad varchar(10). No se modifica el SQL de los SP.

## P4 entregado

Q9 muestra tres rankings docentes con el mapeo visible corregido respecto del
legado: **I** conocimientos teóricos, **II** experiencia práctico-profesional y
**III** capacidad formativo-pedagógica. El ASPX original mostraba III bajo el
título de práctico y II bajo el de formativo; esta migración no cambia los SP,
solo presenta cada resultado con su título correcto.

Q10 lista profesores insatisfechos y permite abrir sus motivos en un panel
inline. El detalle conserva los cuatro filtros originales: período, tipo de
alumno, modalidad y código de profesor.

| Ruta base: `/api/reports/postgraduate/exit-survey-statistics` | Resultado |
| --- | --- |
| `GET /teachers?period=2024A&studentType=Todos&modality=Todos` | Arreglos teórico, práctico y formativo |
| `GET /insatisfaction?...` | Profesor, cantidad y datos para el drilldown |
| `GET /insatisfaction/detail?period=...&studentType=...&modality=...&professor=P9` | Motivos del profesor |
| Error | Filtros/profesor inválidos 400; almacenamiento 503 saneado |

**Defectos SQL heredados documentados, sin modificar:** `DetallesProf` usa
`@Tipo_Alumno` como modalidad en uno de sus UNION y repite `Q13_M2` en el
tercer bloque. La UI no puede corregir datos que el procedimiento devuelve mal;
se muestran los motivos recibidos y se deja constancia para una futura corrección
de base de datos. Los rankings Q9 mantienen las frecuencias del SP.

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
no-store y errores saneados. 8 pruebas de componente, 12 de navegación y pruebas backend (incluyendo ADO.NET
para Q1/Q7/Q8/Q11/Q9/Q10/DetalleProfesor) cubren carga automática, cambio
dependiente de modalidad, tabs, muestra vacía, matrices, porcentajes, promedio,
ranking docente corregido, drilldown de insatisfacción, cancelación de respuestas
obsoletas y errores. Suites completas observadas: 293 pruebas .NET, 38 Node, 188/193 Angular y
build de producción correcto. Las cinco fallas Angular son las ya conocidas de
tema/favoritos por `localStorage` ausente bajo Node 26, ajenas a esta pantalla.
No se probó contra SQL Server, Keycloak ni un navegador real. No hay commits ni
cambios de base de datos.
