# Estadísticas encuesta de salida

La ruta `/postgrado/estadisticas-encuesta-salida` implementa el informe completo de
`EncuestaSalidaGraduados/Default.aspx`: filtros, muestra, las 14 secciones de
resultados, Q17, Resumen y ocho gráficos CSS. La navegación usa dos niveles del patrón app-tabs de SADAlumnos. No usa SSRS ni modifica datos.

## Navegación por etapas

El primer nivel reproduce los grupos de Informes y queda fuera del contenedor de
secciones:

- **I. Evaluación Programa**
- **II. Evaluación Cursos**
- **III. Docentes**
- **IV. Conocimientos y Habilidades Adquiridas**
- **Gráficos**

Dentro de cada etapa se muestran sus tabs secundarios con app-tabs--secondary:
Programa incluye Malla, Cuerpo Académico, Funcionamiento, Infraestructura,
Servicios, Vinculación Medio, Networking y Resumen; Cursos incluye Cátedras x Área;
Docentes incluye Ranking e Insatisfacción; Conocimientos incluye Habilidades y
Evaluación general. Gráficos contiene el selector de los ocho gráficos. Cambiar de
etapa carga su primer tab y cancela las consultas de la etapa anterior.

## P1 entregado

- Filtros Período ingreso, Tipo Alumno y Modalidad. La lista de tipos conserva
  los 13 valores legados; período y modalidad salen de sus SP canónicos.
- Muestra desde `spRec_EncuestaGraduados_Estadisticas`: Tipo, Modalidad, Muestra,
  Incompletas y Contestadas, con totales de footer calculados por el servidor.
- Cambios de tipo recargan modalidades; cambios de filtros cancelan la consulta
  anterior y no permiten que una respuesta vieja reemplace la nueva.
- Tabs de las 14 secciones sobre un shell único; las fases P2-P6 incorporaron
  el contenido de cada sección.
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

Q10 lista profesores insatisfechos con paginación de 10 filas y el componente
Search de SADAlumnos. La búsqueda filtra por profesor, código o cantidad y vuelve
a la primera página. La acción es un botón de icono con tooltip y abre los motivos
en un modal; el detalle conserva los cuatro filtros originales: período, tipo de
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

## P5 entregado

La tab **Resumen** compone Q1-Q7 y muestra una fila por dimensión con:

- porcentaje de cada alternativa sobre la suma de footers de esa dimensión,
  formateado con dos decimales es-CL y `%`;
- **Evaluación Dimensión** = `(1·a1 + 2·a2 + 3·a3 + 4·a4 + 5·a5) / total`,
  con dos decimales.

La fórmula reproduce el cálculo del ASCX legado. Si el total de una dimensión es
cero, la fila queda sin porcentajes ni dimensión; si todas están en cero se
muestra **No existen datos disponibles.** No se inventan promedios.

| Ruta base: `/api/reports/postgraduate/exit-survey-statistics` | Resultado |
| --- | --- |
| `GET /summary?period=2024A&studentType=Todos&modality=Todos` | Siete dimensiones con conteos, total y dimensión ponderada |
| Error | Filtros inválidos 400; almacenamiento 503 saneado |

El endpoint ejecuta las siete consultas Q1-Q7 por el mismo camino tipado ya usado
en las matrices; no duplica SQL ni recalcula el promedio de cada pregunta.

## P6 entregado

Q17 **Evaluación general** comparte la matriz Likert de cinco alternativas y se
expone por la sección `evaluacion-general`.

La tab **Gráficos** ofrece ocho opciones: Malla Curricular, Cuerpo Académico,
Funcionamiento, Infraestructura y recursos, Servicios, Vinculación Medio,
Networking y Evaluación general. Cada gráfico usa los datos de la sección Likert
correspondiente y se dibuja con CSS nativo:

- barras apiladas por alternativa con los colores del legado;
- leyenda Muy en desacuerdo → Muy de acuerdo;
- porcentaje por segmento y dimensión promedio a la derecha;
- selector de gráfico, estados de carga/error y “No existen datos disponibles”.

No se agregan Chart.js, Highcharts ni otra dependencia. No hay exportación de
imágenes porque la decisión de alcance fue solo pantalla.

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

## Verificación final

Pruebas verificadas durante P1-P7:

- 50 pruebas backend focalizadas del módulo; 329 en la suite .NET completa.
- 23 pruebas de UI focalizadas del shell/navegación y 197/202 en la suite Angular.
- 38 pruebas Node y build de producción correcto.
- Verificación independiente final sin bloqueos: Q17 excluido de Resumen,
  mapeo Q9 I/II/III correcto, fórmula de Resumen y permisos/no-store observados.

Las cinco fallas Angular son las previas de tema/favoritos por `localStorage`
ausente bajo Node 26, ajenas a esta pantalla. No se probó contra SQL Server,
Keycloak, fotos, SADAlumnos ni un navegador real. Los defectos internos de
`DetallesProf` se documentaron sin modificar SP, vistas o tablas. No hay commits
ni cambios de base de datos.
