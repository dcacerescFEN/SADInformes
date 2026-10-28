# Comparación estructural de los backends SAD

SADInformes conserva las cuatro capas y el mediador propio de los sistemas de
referencia. Tras la homogeneización autorizada comparte con NewSAD el registro de
handlers por scan, el contrato `ApiResponse<SiteConfigDTO>`, la lectura directa de
la configuración pública y el patrón de política de módulo. Además sirve Angular
en producción desde WebApi. No se copiaron funcionalidades académicas,
suplantación, administración de usuarios ni configuración sensible.

## Alcance y evidencia

Se inventariaron todos los archivos C# de Domain, Application, Infrastructure y
WebApi y se contrastaron proyectos, referencias, paquetes, composición de servicios,
arranque, endpoints, configuración, autenticación y pruebas. Se excluyeron WebUI,
bin y obj. Es una comparación de estructura y contratos comunes, no una auditoría
funcional de cada operación de negocio de los otros sistemas. Las referencias se
leyeron, no se modificaron ni se ejecutaron contra sus bases.

Rutas de evidencia en esta sección: relativas a la carpeta de cada proyecto.

| Capa: archivos C# | SADInformes | NewSAD | SADAlumnos | SADAdministracion |
| --- | ---: | ---: | ---: | ---: |
| Domain | 3 | 9 | 222 | 13 |
| Application | 7 | 11 | 388 | 55 |
| Infrastructure | 5 | 5 | 38 | 10 |
| WebApi | 10 | 13 | 62 | 14 |

Los cuatro apuntan a .NET 10 y mantienen Domain ← Application ← Infrastructure,
con WebApi referenciando Application/Infrastructure y el proyecto WebUI sin
tratarlo como un ensamblado .NET.

## Matriz de diferencias

| Área | SADInformes | NewSAD | SADAlumnos | SADAdministracion |
| --- | --- | --- | --- | --- |
| Arranque IDE | `launchBrowser:true`, `launchUrl:scalar` después de esta corrección | Mismo destino Scalar | Mismo destino Scalar | Mismo destino Scalar |
| Scalar | 2.18.2, solo Development | 2.18.2, solo Development | 2.17.10, solo Development | 2.18.0, solo Development |
| ASP.NET JWT/OpenAPI | 10.0.12 | 10.0.13 | 10.0.12 | 10.0.13 |
| API | Minimal APIs y descubrimiento `IEndpoint` | Igual | Igual, con endpoints de alumnos | Igual, con endpoints administrativos |
| Mediador | Contrato propio, scan Scrutor en Application | Mismo contrato, scan Scrutor | Mismo contrato, scan y servicios académicos | Mismo contrato, scan y servicios administrativos |
| Registro Application | `Application/ConfigureServices.cs`, Scrutor 7.0.0 | `Application/ConfigureServices.cs` | `Application/ConfigureServices.cs` | `Application/ConfigureServices.cs` |
| Config público Keycloak | Lectura directa de `IConfiguration` | Lectura directa de `IConfiguration` | Lectura directa de `IConfiguration` | Lectura directa de `IConfiguration` |
| Configuración SQL | DbContext scoped directo; sin lector intermedio | DbContext directo | DbContext directo y configuración académica adicional | DbContext directo |
| Estado del menú | `SiteSettings` singleton por aplicación, publicación única | `ConfigSite` estático | `SiteSettings` estático | `ConfigSite` estático |
| Respuesta de site | `ApiResponse<SiteConfigDTO>` | `ApiResponse<SiteConfigDTO>` | `ApiResponse<SiteConfigDTO>` con menú y datos académicos | `ApiResponse<SiteConfigDTO>` |
| Autorización Config/site | `REPORT_ACCESS` del cliente físico exacto | Política SAD_Access | Política SADStudentsEnter | Política ADMINISTRATION_ACCESS |
| Permisos funcionales | Solo entrada REPORT; por informe pendiente | Proveedor de políticas y permisos SAD | Permisos STUDENT | Permisos ADMINISTRATION |
| Suplantación | No incorporada | Infraestructura de suplantación | Infraestructura de suplantación | Infraestructura de suplantación |
| DbContexts | AppConfig | AppConfig | AppConfig, Sad, Justificaciones | AppConfig, Sad |
| Publicación SPA | Static files, fallback del cliente y publish integrado | Static files y fallback | Static files y fallback | Static files y fallback |
| Pruebas | HTTP WebApi net10 y Node de arranque/estructura | Proyecto Application net10 | Proyecto Application net10 | Proyectos Application y Keycloak net10 |

Evidencia principal:

- Los cuatro: `src/Web/WebApi/Properties/launchSettings.json`, `Program.cs` y `WebApi.csproj`.
- Referencias: `src/Application/ConfigureServices.cs`, `src/Infrastructure/ConfigureServices.cs`,
  `src/Infrastructure/Configuration/ConfigurationInitialiser.cs`.
- SADInformes: `src/Web/WebApi/ConfigureServices.cs`, `src/Web/WebApi/OpenApi/ApiDocumentation.cs`,
  `src/Infrastructure/Configuration/SiteSettings.cs` y `src/Web/WebApi/Authentication/KeycloakAuthentication.cs`.
- Los cuatro: `src/Application/Config/Queries/GetSiteConfig/GetSiteConfig.cs` y
  `src/Web/WebApi/Endpoints/Config/ConfigEndpoints.cs`.
- Referencias: `src/Web/WebApi/Services/PermissionPolicyProvider.cs` y `Authentication/`.

## Scalar: causa reproducida y corrección

Antes de esta corrección, SADInformes tenía `launchBrowser:false` y no tenía
`launchUrl`; los otros tres proyectos abren `scalar`. Se observaron dos fallos
RED: el contrato de arranque y la presencia del lector solicitado para eliminación.
La prueba de proceso HTTP real confirmó que `/scalar` (siguiendo su redirección)
y `/openapi/v1.json` ya respondían 200: el fallo reproducido era de configuración de arranque del IDE,
no ausencia del endpoint. Esto no descarta un problema adicional en el entorno
del usuario que requiera observar su navegador.

- Visual Studio/F5 con perfil https: destino `https://localhost:7070/scalar`.
- `dotnet run --project src/Web/WebApi --launch-profile https`: abrir esa URL
  manualmente si el CLI no abre navegador; el flag depende del launcher.
- Scalar pertenece al backend en 7070, no al router Angular en 4200.
- Fuera de Development las rutas de documentación continúan respondiendo 404.
- Se conservaron los puertos propios de SADInformes (7070/5086), no se copiaron
  los puertos HTTP de las otras aplicaciones.

## Configuración: diferencias que no deben borrarse accidentalmente

La consulta movida al inicializador mantiene filtros por aplicación/ambiente y
registros activos, exclusión de claves ajenas al menú, rechazo de duplicados,
publicación única sin valores parciales y errores 503 sin datos privados.
Las pruebas ahora sustituyen el DbContext, no un lector ficticio.

Las claves AppConfig no son idénticas entre referencias: NewSAD y Administración
leen `StudentsMenuUrl`/`TeachersMenuUrl`; Alumnos e Informes usan
`StudentUrl`/`TeacherUrl`. Copiar filas o cambiar nombres sin mapearlos puede hacer
que se usen defaults en lugar de la configuración deseada. Esta revisión no
modifica las filas ni los contratos de configuración existentes.

Asimismo, incorporar `ApiResponse` exigiría adaptar el consumidor Angular; no es
un cambio puramente interno. No se cambió silenciosamente el contrato.

## Faltantes reales y decisiones futuras

1. **Autorización funcional por informe:** `REPORT_ACCESS` habilita la entrada y la
   configuración del sitio, pero cada informe necesita su propio permiso antes de
   exponer datos o mutaciones. No importar permisos STUDENT/SAD/ADMINISTRATION por copia.
2. **Infraestructura funcional:** todavía no hay SadContext, repositorios, SP ni
   exportadores de informes. Incorporar solo los que requiera la funcionalidad elegida.
3. **Estado estático:** Informes mantiene `SiteSettings` inmutable por aplicación en
   lugar del estado estático de las referencias. Homogeneizarlo no aporta beneficio
   funcional y reduciría el aislamiento entre instancias; se documenta como diferencia
   intencional.
4. **Producción SPA:** WebApi sirve Angular publicado y devuelve `index.html` solo
   para rutas del cliente. `/api`, `/scalar`, `/openapi` y archivos inexistentes
   conservan sus códigos. Despliegue, IIS, certificados y roles siguen siendo
   decisiones humanas.
5. **Dependencias:** la restauración ya advertía vulnerabilidades NU1902/NU1903
   de `System.Security.Cryptography.Pkcs 9.0.11` antes de Scalar. Requieren una unidad
   de actualización y pruebas, no copiar todas las versiones de otra aplicación.
6. **Patrones de respuesta y claves:** decidir una normalización común con impacto
   explícito en frontend y AppConfig, si se quiere homogeneizar los cuatro sistemas.

No se incorporaron servicios sin consumidores, credenciales, suplantación,
migraciones, correo, PDF/Word ni administración de Keycloak. La falta de esos
servicios no es un defecto de esta etapa visual. Los títulos, versiones y AGENTS
heredados no se toman como prueba: por ejemplo NewSAD todavía declara
`Title = "SADAlumnos API"` en su generador OpenAPI; Informes conserva su propio título.

## Límites de verificación

Las pruebas HTTP usan estado aislado y no consultan SQL institucional ni SSO real.
La apertura visual y el login OAuth requieren validación de navegador/Keycloak.
Comprobaciones observadas: 30 pruebas .NET, 13 Node de estructura/HTTP, compilación
de solución y una prueba HTTPS real de arranque con SPA proxy. Esta última verifica
`/scalar` → 302 a `/scalar/` en el mismo origen → 200 HTML, y OpenAPI 200. Al añadirla
se corrigieron dos expectativas erróneas de la prueba (200 sin redirect y redirect
a `/scalar/v1`); no fueron fallos del endpoint productivo.
El explorador y la revisión independiente no estuvieron disponibles por falta de
credenciales del proveedor; la revisión nativa también estaba indisponible.
No se presentan esos controles como aprobados.
