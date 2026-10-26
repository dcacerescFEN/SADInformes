# SADInformes

Base independiente del sistema de informes. Etapa 1: estructura de SADAlumnos,
sin copiar sus funcionalidades ni configuración sensible.

## Alcance

- Backend .NET 10: Domain ← Application ← Infrastructure; WebApi referencia
  Application e Infrastructure y enlaza el proyecto Angular `WebUI.esproj`.
- Angular 22 standalone, rutas lazy y detección OnPush.
- Página inicial y diagnóstico `GET /api/health`.
- Sin base de datos, autenticación, navbar, sidebar ni informes. Las capas interiores
  permanecen vacías hasta que exista una funcionalidad que las necesite.
- No se modifican SADAlumnos ni SADAdministracion.

Esta etapa no está preparada para exposición pública ni uso con datos reales.
Keycloak y los permisos se incorporarán antes de implementar informes.

## Requisitos

SDK .NET 10, Node.js compatible con Angular 22
(`^22.22.3 || ^24.15.0 || >=26.0.0`) y pnpm 12.6.0.
`global.json` permite cualquier feature band instalada del SDK 10.0.

## Ejecución local

Instalar las dependencias frontend una vez, desde la raíz de SADInformes:

```sh
cd src/Web/WebUI
pnpm install --frozen-lockfile
cd ../../..
dotnet build SADInformes.slnx
dotnet run --project src/Web/WebApi --launch-profile http
```

Abrir http://localhost:5086. El perfil `http` activa ASP.NET Core SPA proxy:
WebApi inicia `pnpm start` en WebUI y, cuando Angular está listo, redirige
al frontend en http://127.0.0.1:4206. No es necesario iniciar ambos proyectos
por separado. El servidor Angular reenvía `/api` a la API; no requiere CORS
ni certificados locales.

En Visual Studio, abrir `SADInformes.slnx`: WebUI aparece como proyecto JavaScript
junto a WebApi. Establecer WebApi como proyecto de inicio y utilizar el perfil
`http`. La referencia a WebUI tiene `ReferenceOutputAssembly=false`, porque
no es un ensamblado .NET.

Opcionalmente se puede ejecutar `pnpm start` manualmente en WebUI; el SPA proxy
reutiliza ese servidor si ya está activo.
Se utilizan puertos distintos de los de SADAlumnos y SADAdministracion.

`GET http://localhost:5086/api/health` devuelve:

```json
{"status":"ok","application":"SADInformes"}
```

## Verificación

Desde la raíz de SADInformes:

```sh
dotnet build SADInformes.slnx
node --test tests/*.test.mjs
cd src/Web/WebUI
pnpm build
pnpm test
```

Las pruebas Node comprueban estructura, dependencias y arranque HTTP real de la API.
Las pruebas Angular comprueban contenedor, página inicial y redirección de rutas.

Para verificar el arranque integrado después de compilar, sin servidores previos
en los puertos 5086 y 4206:

```sh
node --test tests/dev-smoke.mjs
```

Esta prueba inicia WebApi con el perfil `http`, espera la redirección a Angular,
comprueba la página y su JavaScript, consulta `/api/health` a través del frontend
y cierra el árbol de procesos que inició. Si los puertos están ocupados, falla
sin detener los procesos existentes.

Los scripts nativos de las dependencias de compilación se autorizan explícitamente
mediante `pnpm-workspace.yaml`, siguiendo la base de SADAlumnos. No se habilitan
scripts de cualquier paquete de forma global.

La solución incluye backend y frontend; WebApi referencia `WebUI.esproj`.
Como en SADAlumnos, la compilación Angular se ejecuta explícitamente con
`pnpm build` (`ShouldRunBuildScript=false`); restaurar o compilar la solución
no instala paquetes npm ni modifica instalaciones globales.

La salida Angular queda en `src/Web/WebUI/dist/webui/browser`.
El enlace y arranque conjunto están configurados para desarrollo.
El backend todavía no sirve esa salida en producción: publicación y despliegue
quedan fuera de esta etapa.

## Siguientes etapas

1. Integración Keycloak basada en SADAlumnos, con cliente y permisos propios.
2. Navbar de SADAlumnos.
3. Sidebar de SADAdministracion adaptada a informes.
4. Catálogo de informes y fuentes de datos aprobados.

No copiar connection strings, secretos, entidades, procedimientos ni servicios
funcionales de los sistemas de referencia.
