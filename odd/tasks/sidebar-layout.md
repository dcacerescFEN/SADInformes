# SADInformes — Sidebar (etapa 4)
Adaptación de la sidebar de SADAdministracion, sin catálogo de informes nuevo.
Risk: item 2 (perfil y panel ligados a sesión), item 3 (shell y navbar compartidos).

## Specs
S1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
S2. "Continua Etapa 4"

## Tasks
T1 | S1-S2 | inline | completado | commit: d048f27 | Adaptar sidebar, perfil y controles del panel móvil con pruebas.
T2 | S1-S2 | inline | completado | commit: por registrar | Integrar shell y toggles de navbar sin alterar sesión ni enlaces.
T3 | S1-S2 | verificación independiente + revisión nativa | en progreso | commit: pendiente | Verificar layout, regresiones y documentación.

## Log
L1. "necesito que crees un nuevo sistema el SADInformes, utiliza la base de SADAlumnos como la integracion con keycloack, la barra de navegación y utiliza la sidebar que tiene SADAdministracion, genera un plan"
L2. "Continua Etapa 4"
L3. Referencia: SADAdministracion/core/layout/components/administration-sidebar.ts y layout.ts. Anchos280/76px, offcanvas antes992px, cierre por enlace/resize/destroy y perfil autenticado.
L4. Única ruta existente: Inicio. No copiar academic-periods, management, permisos administrativos ni inventar informes. Se conserva navbar de etapa3 y REPORT_ACCESS; el catálogo será una etapa posterior.
L5. Sidebar y drawer solo se montan con sesión; cerrar panel al destruir la vista. Nombre público de Keycloak opcional, con fallback a username; no almacenar datos de perfil ni cambiar tokens, guard o políticas.
L6. Preservar archivos humanos SADInformes.slnx, appsettings.json y appsettings.Development.json, y mantener HTTPS7070/4200. Sin cambios backend, SQL ni dependencias nuevas.
L7. Layout de alto de viewport y scroll en contenido/sidebar evita superposición cuando la navbar crece por menú móvil o aviso. Colores/temas de referencia, sin footer nuevo. Navbar expone toggles opcionales para conservar usos existentes.
L8. La prueba de navegador real no está disponible en esta sesión; se ejecutarán interacciones DOM reales de ng-bootstrap y smoke HTTPS, sin afirmar comprobación visual.
L9. RED observado: 5 fallos de comportamiento (perfil y apertura/presentación del panel), 48 casos previos aprobados. Implementada sidebar real y nombre opcional reactivo; GREEN enfocado en curso.
L10. Primer GREEN detectó dos fallos del fixture, no de la app: ng-bootstrap21 cierra backdrop en mousedown y programa Escape con requestAnimationFrame. Se verificó código instalado y se ajustaron eventos/espera del test, sin cambiar ni simular el servicio de panel real.
L11. GREEN T1: 53 pruebas enfocadas aprobadas con ng-bootstrap real: perfil, Inicio, colapso, foco/restauración, Escape, enlace, botón, backdrop, navegación, resize y destrucción. Integración en shell aún pendiente.
L12. RED T2: 3 fallos de integración en App/Header y prueba de shell inexistente. App/Header/Home integrados manteniendo outlet único y sidebar condicionada por sesión; estructurales Node aprobados. Suite completa/build en curso, sin marcar T2 completada todavía.
L13. GREEN T2: suite Angular completa y build aprobados (bundle681kB, aviso500kB/límite1MB). Se preserva outlet al perder/reabrir sesión, se elimina panel/perfil y controles anteriores funcionan. Ajuste de overflow del shell con RED/GREEN estructural: si la navbar supera el viewport, sigue siendo desplazable en vez de recortar controles; sin cambios de lógica TS.
L14. Compilación final CSS aprobada (bundle681,02kB, advertencia sin alterar presupuesto). T2 listo con fuentes/tests/documentación; falta outside view y smoke. Revisión nativa de unidadT1 no disponible por package-local-binary-missing.
