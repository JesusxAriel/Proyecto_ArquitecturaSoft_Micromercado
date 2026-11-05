> HISTORICO: describe el monolito o una etapa retirada. Guia vigente: [README](../../README.md). No ejecutar sus comandos como arranque actual.

# Preparación del refactor de PuntoMarket — Ventas

**Fecha:** 2026-11-04  
**Alcance:** auditoría estática del repositorio, solución, proyectos, dependencias y pruebas.  
**Regla aplicada:** no se modificó código fuente ni configuración, no se cambió de rama, no se ejecutaron Docker Compose, migraciones SQL, `fetch`, `pull`, `push`, `merge`, `reset` ni `checkout`.

## 1. Resumen ejecutivo

El repositorio está en la rama local `main`, alineada según el indicador local con `origin/main`, pero el árbol de trabajo **no está limpio**:

- `Proyecto_Arquitectura_Micromercado/Program.cs` tiene una modificación local no preparada.
- `AUDITORIA_MONOLITO_VENTAS.md` está sin seguimiento.
- No hay cambios preparados para commit.

La solución real contiene dos proyectos .NET 10 dentro de un único archivo `.slnx`:

1. Aplicación web monolítica ASP.NET Core Razor Pages.
2. Proyecto de pruebas xUnit que referencia directamente la aplicación.

Domain, Application, Infrastructure, Pages y pruebas de Ventas no están separados en proyectos independientes. El módulo de Ventas comparte el mismo `.csproj` con autenticación, usuarios, productos administrativos, categorías, proveedores, comprobantes y demás funcionalidades del monolito.

Validación realizada:

- `dotnet build .\Proyecto_Arquitectura_Micromercado.slnx --no-restore`: **correcto**, 0 errores y 2 advertencias.
- `dotnet test .\Proyecto_Arquitectura_Micromercado.Tests\Proyecto_Arquitectura_Micromercado.Tests.csproj --no-restore --no-build --logger "console;verbosity=normal"`: **correcto**, 531 pruebas, 530 correctas y 1 omitida.
- Las pruebas que requieren una base MySQL externa no se ejecutaron porque no estaba definida `MICROMERCADO_PRUEBAS_MYSQL`.
- No se ejecutó Docker Compose ni se conectó a una base compartida.

La extracción de Ventas debe comenzar desde una rama de trabajo propia y preservar el segundo entregable. Los puntos de coordinación más sensibles son `Program.cs`, `.slnx`, `docker-compose.yml`, `Dockerfile`, `bdMicroMercadoArqui.sql` y los contratos compartidos de autenticación/cliente.

## 2. Estado inicial de Git

### Comandos ejecutados

Se ejecutaron exactamente estos comandos de lectura:

```text
git status --short --branch
git branch -a
git remote -v
git log -5 --oneline
git diff --stat
git diff --cached --stat
```

### Resultado inicial

```text
## main...origin/main
 M Proyecto_Arquitectura_Micromercado/Program.cs
?? AUDITORIA_MONOLITO_VENTAS.md
```

Interpretación:

- **Rama actual:** `main`.
- **Relación local reportada:** `main...origin/main`; este indicador no demuestra que las referencias remotas estén actualizadas porque no se ejecutó `git fetch`.
- **Archivo modificado:** `Proyecto_Arquitectura_Micromercado/Program.cs`.
- **Archivo nuevo sin seguimiento:** `AUDITORIA_MONOLITO_VENTAS.md`.
- **Cambios preparados:** ninguno; `git diff --cached --stat` no produjo salida.
- **Repositorio limpio:** no.
- **Cambios que podrían perderse durante un refactor:** sí. La modificación de `Program.cs` y el informe no seguimiento deben conservarse explícitamente antes de operaciones posteriores.

El diff estadístico mostró:

```text
 Proyecto_Arquitectura_Micromercado/Program.cs | 1 +
 1 file changed, 1 insertion(+)
```

La modificación consiste en un comentario local antes del registro de Data Protection. No fue alterada durante esta auditoría.

## 3. Ramas y commits

### Ramas locales observadas

```text
Ariel
agents/crud-product-entity-csharp-implementation
agents/fix-supplier-form-validation-errors
agents/generate-project-structure-and-diff-report
agents/implement-product-price-history-audit
agents/revert-all-changes-to-original-state
agents/singleton-database-connection-csharp
agents/update-project-structure-file
agents/update-project-structure-md
agents/workspace-crea-un-archivo-llamado-project-struct
feat/docker-redis-nginx
feature/CorreccionesProducto-p1-p2-p5-p6
main
oriana
refactor/general
refactortablas
```

### Referencias remotas observadas

Se observaron `origin/HEAD -> origin/main`, `origin/main` y ramas remotas relacionadas con Docker, Usuarios, Reportes, autenticación, comprobantes, historial, refactor hexagonal y correcciones. La lista completa fue obtenida con `git branch -a`.

**Importante:** las ramas remotas conocidas no se consideran actualizadas; no se ejecutó `git fetch`.

### Últimos cinco commits observados

```text
1a8019a Merge pull request #53 from JesusxAriel/feat/workbench-docker
c7780df fix: usar el puerto 3307 por defecto para no chocar con un MySQL local
1d33938 docs: documentar la conexión de MySQL Workbench con la base de Docker
d1ab792 feat: publicar el puerto de MySQL solo en el equipo local para Workbench
4ac4d2e Merge pull request #52 from JesusxAriel/fix/zona-horaria-bolivia
```

## 4. Estructura real de la solución

### Árbol resumido

```text
Proyecto_Arquitectura_Micromercado - copia/
├── Proyecto_Arquitectura_Micromercado.slnx
├── Proyecto_Arquitectura_Micromercado/
│   ├── Proyecto_Arquitectura_Micromercado.csproj
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   ├── Pages/
│   ├── wwwroot/
│   ├── Program.cs
│   └── appsettings*.json
├── Proyecto_Arquitectura_Micromercado.Tests/
│   ├── Proyecto_Arquitectura_Micromercado.Tests.csproj
│   ├── *Tests.cs
│   └── js/ventas-reglas.test.cjs
├── Dockerfile
├── docker-compose.yml
├── nginx/nginx.conf
├── bdMicroMercadoArqui.sql
├── .gitignore
└── .env.example
```

### Archivos de solución y configuración encontrados

| Archivo | Estado / contenido |
|---|---|
| `Proyecto_Arquitectura_Micromercado.slnx` | Solución XML moderna con los dos `.csproj`. |
| `Proyecto_Arquitectura_Micromercado/Proyecto_Arquitectura_Micromercado.csproj` | Proyecto web monolítico. |
| `Proyecto_Arquitectura_Micromercado.Tests/Proyecto_Arquitectura_Micromercado.Tests.csproj` | Proyecto de pruebas xUnit. |
| `Dockerfile` | Publica sólo la aplicación web principal, no el proyecto de pruebas. |
| `docker-compose.yml` | MySQL, Redis, tres réplicas de la app y Nginx. |
| `.gitignore` | Ignora `bin`, `obj`, `.vs`, `appsettings.json`, `.env` y archivos de usuario. |
| `Directory.Build.props` | **NO EXISTE** en el repositorio inspeccionado. |
| Archivos de configuración de pruebas | No se encontró `.runsettings`; el proyecto usa configuración del `.csproj` y atributos xUnit. |
| `Properties/launchSettings.json` | Configuración local de lanzamiento de la aplicación. |

### Proyecto web

**Ruta:** `Proyecto_Arquitectura_Micromercado/Proyecto_Arquitectura_Micromercado.csproj`  
**Nombre:** `Proyecto_Arquitectura_Micromercado`  
**Tipo:** `Microsoft.NET.Sdk.Web` / ASP.NET Core Razor Pages  
**Target Framework:** `net10.0`  
**Nullable:** habilitado  
**Implicit usings:** habilitado  
**Referencias a otros proyectos:** ninguna  

Paquetes NuGet relevantes:

- `BCrypt.Net-Next` 4.2.0.
- `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.13.
- `MySql.Data` 26.7.0.

### Proyecto de pruebas

**Ruta:** `Proyecto_Arquitectura_Micromercado.Tests/Proyecto_Arquitectura_Micromercado.Tests.csproj`  
**Nombre:** `Proyecto_Arquitectura_Micromercado.Tests`  
**Tipo:** `Microsoft.NET.Sdk`, no empaquetable  
**Target Framework:** `net10.0`  
**Nullable:** habilitado  
**Implicit usings:** habilitado  
**Referencia de proyecto:** `..\Proyecto_Arquitectura_Micromercado\Proyecto_Arquitectura_Micromercado.csproj`  

Paquetes NuGet:

- `coverlet.collector` 6.0.4.
- `Microsoft.NET.Test.Sdk` 17.14.1.
- `xunit` 2.9.3.
- `xunit.runner.visualstudio` 3.1.4.

## 5. Dependencias del módulo de Ventas

### Ubicación actual

El módulo está físicamente organizado por carpetas, pero no por proyectos:

```text
Proyecto_Arquitectura_Micromercado.csproj
├── Domain/Sales/
├── Domain/Stock/
├── Application/Sales/
├── Application/Facades/
├── Application/Products/
├── Application/Customers/
├── Application/Comprobantes/
├── Application/Stock/
├── Infrastructure/Persistence/
├── Infrastructure/Comprobantes/
├── Infrastructure/Factories/
├── Infrastructure/Cache/
├── Infrastructure/Web/
├── Pages/Sales/
├── Pages/Comprobante/
└── wwwroot/js/
```

Las pruebas están en otro `.csproj`, pero referencian todo el monolito completo:

```text
Proyecto_Arquitectura_Micromercado.Tests.csproj
└── ProjectReference -> Proyecto_Arquitectura_Micromercado.csproj
```

### Domain de Ventas

Archivos principales:

- `Domain/Sales/Venta.cs`
- `Domain/Sales/DetalleVenta.cs`
- `Domain/Sales/EstadoVenta.cs`
- `Domain/Stock/AsignadorFefo.cs`
- `Domain/Stock/ConsumoDeLote.cs`
- `Domain/Stock/LoteDisponible.cs`
- `Domain/Stock/ResultadoBajaDeLotes.cs`
- `Domain/Stock/StockInsuficienteException.cs`
- `Domain/Stock/RestauracionExcedeCantidadInicialException.cs`

Dependencias relevantes:

- El dominio de Ventas no referencia directamente `MySql.Data` ni Pages.
- FEFO es una regla pura y está cubierta por pruebas unitarias.
- El stock se integra con Ventas mediante detalles que guardan `idLote` y cantidad.

### Application de Ventas

Archivos principales:

- `Application/Facades/FachadaVenta.cs`
- `Application/Facades/FachadaAnulacion.cs`
- `Application/Facades/FachadaActualizacionStockProducto.cs`
- `Application/Sales/VentaDtos.cs`
- `Application/Sales/VentaConsultaDtos.cs`
- `Application/Sales/IVentaRepository.cs`
- `Application/Sales/IVentaConsultaRepository.cs`
- `Application/Sales/IVentaConsultaService.cs`
- `Application/Sales/VentaConsultaService.cs`
- `Application/Products/ProductService.cs`
- `Application/Products/IProductRepository.cs`
- `Application/Customers/ClienteConsultaService.cs`
- `Application/Customers/ClienteCreacionService.cs`
- `Application/Comprobantes/ComprobanteService.cs`
- `Application/Common/IUnidadDeTrabajo.cs`
- `Application/Security/IUsuarioActual.cs`

Dependencias a considerar:

- `FachadaVenta` coordina repositorio de venta, precio vigente, stock/lotes, cliente activo, usuario actual, reloj, detector de interbloqueos y consulta/cache.
- `FachadaAnulacion` coordina UoW, venta, stock, usuario e invalidación del historial.
- `VentaConsultaService` usa `IDistributedCache`, `ILogger` e `IIdentificadorReplica`; Redis debe eliminarse o aislarse detrás de un puerto porque el tercer entregable no lo contempla.
- `IProductRepository` contiene capacidades administrativas además de las necesarias para vender; debe reducirse al extraer Ventas.
- `IUsuarioActual` actualmente se alimenta desde claims de cookie y deberá adaptarse a JWT.

### Infrastructure de Ventas

Adaptadores principales:

- `Infrastructure/Persistence/MySqlVentaRepository.cs`
- `Infrastructure/Persistence/MySqlVentaConsultaRepository.cs`
- `Infrastructure/Persistence/MySqlLoteRepository.cs`
- `Infrastructure/Persistence/MySqlProductRepository.cs`
- `Infrastructure/Persistence/MySqlClienteConsultaRepository.cs`
- `Infrastructure/Persistence/MySqlClienteCreacionRepository.cs`
- `Infrastructure/Persistence/MySqlComprobanteRepository.cs`
- `Infrastructure/Persistence/MySqlUnidadDeTrabajo.cs`
- `Infrastructure/Database/DatabaseConnection.cs`
- `Infrastructure/Comprobantes/GeneradorComprobanteHtml.cs`
- `Infrastructure/Factories/CreatorVentaRepositoryMySql.cs`
- `Infrastructure/Factories/CreatorLoteRepositoryMySql.cs`
- `Infrastructure/Persistence/Errores/RepositoriosConErroresTraducidos.cs`

Estos archivos dependen de `MySql.Data`, tablas/vistas/triggers de `bdMicroMercadoArqui.sql` y composición de `Program.cs`.

### Razor Pages y frontend de Ventas

- `Pages/Sales/Index.cshtml`
- `Pages/Sales/Index.cshtml.cs`
- `Pages/Sales/Historial.cshtml`
- `Pages/Sales/Historial.cshtml.cs`
- `Pages/Sales/SaleInputModel.cs`
- `Pages/Comprobante/Index.cshtml`
- `Pages/Comprobante/Index.cshtml.cs`
- `wwwroot/js/ventas.js`
- `wwwroot/js/ventas-reglas.js`
- `wwwroot/js/historial.js`
- `wwwroot/css/ventas.css`

Actualmente estas Pages llaman servicios/fachadas por inyección de dependencias en el mismo proceso. No se verificaron controladores API HTTP independientes para Ventas.

### Pruebas de Ventas

Archivos principales:

- `FachadaVentaTests.cs`
- `FachadaAnulacionTests.cs`
- `AsignadorFefoTests.cs`
- `SalesRegistroHandlerTests.cs`
- `SalesAnulacionHandlerTests.cs`
- `SalesBusquedaHandlerTests.cs`
- `SalesClienteHandlerTests.cs`
- `SalesHistorialHandlerTests.cs`
- `VentaConsultaServiceTests.cs`
- `HistorialVentasEnCacheTests.cs`
- `ComprobanteTests.cs`
- `MontoEnLetrasTests.cs`
- `UnidadDeTrabajoTests.cs`
- `InMemoryLoteRepositoryTests.cs`
- `StockActualMySqlTests.cs`
- `ReglasDeVentasJavaScriptTests.cs`
- `js/ventas-reglas.test.cjs`

## 6. Resultado de compilación

### Comando

```text
dotnet build .\Proyecto_Arquitectura_Micromercado.slnx --no-restore
```

### Resultado

**Correcto.**

```text
Compilación correcta.
0 Errores
2 Advertencia(s)
Tiempo transcurrido: 00:00:13.15
```

Advertencias reportadas:

1. `Proyecto_Arquitectura_Micromercado.Tests/ReglasDeVentasJavaScriptTests.cs:33`
   - `CS8602`: posible desreferencia de referencia nula.
2. `Proyecto_Arquitectura_Micromercado.Tests/ProductServiceEnMemoriaTests.cs:246`
   - `xUnit2031`: uso de `Where` antes de `Assert.Single`.

No se corrigieron porque esta tarea es exclusivamente de auditoría.

## 7. Resultado de pruebas

### Comando

```text
dotnet test .\Proyecto_Arquitectura_Micromercado.Tests\Proyecto_Arquitectura_Micromercado.Tests.csproj --no-restore --no-build --logger "console;verbosity=normal"
```

### Resultado

**Correcto.**

```text
Pruebas totales: 531
Correcto: 530
Omitido: 1
Tiempo total: 11,3224 segundos
```

La prueba omitida es:

```text
UsuarioAdministracionMySqlTests.Alta_listado_restricciones_y_roles_funcionan_en_mysql
```

Se omite mediante `MySqlUsuariosFactAttribute` cuando no existe la variable de entorno `MICROMERCADO_PRUEBAS_MYSQL`.

### Pruebas MySQL no ejecutadas

`StockActualMySqlTests` contiene pruebas de integración potencialmente destructivas: registra ventas, anula ventas, ejecuta actualizaciones sobre `LOTE`, crea concurrencia y restaura datos en bloques `finally`. No se ejecutaron contra una base externa porque `MICROMERCADO_PRUEBAS_MYSQL` no estaba configurada y la instrucción exige evitar datos compartidos/reales.

`UsuarioAdministracionMySqlTests` también modifica usuarios y roles en MySQL; quedó omitida automáticamente por faltar la variable de entorno.

Las pruebas ejecutadas fueron principalmente unitarias, de dobles en memoria, de handlers, de arquitectura estática, de JavaScript y de generación de comprobantes. No se inició Redis, MySQL, Docker ni ningún servicio externo.

## 8. Estado final de Git

Después del build y test se ejecutó:

```text
git status --short --branch
```

Resultado:

```text
## main...origin/main
 M Proyecto_Arquitectura_Micromercado/Program.cs
?? AUDITORIA_MONOLITO_VENTAS.md
```

Comparación con el estado inicial:

- No apareció ningún archivo nuevo inesperado.
- No cambió `Program.cs`.
- No aparecieron cambios preparados.
- Los artefactos generados en `bin` y `obj` no alteraron el estado reportado porque están ignorados por `.gitignore`.
- El archivo nuevo solicitado por la auditoría anterior permanece sin seguimiento.

## 9. Riesgos de trabajo colaborativo

### Hechos comprobados

- Todos los módulos viven en el mismo proyecto web.
- `Program.cs` registra todas las dependencias y será un punto de conflicto.
- `.slnx` sólo conoce los dos proyectos actuales.
- `docker-compose.yml` define una aplicación monolítica replicada, Redis y Nginx, no tres servicios independientes.
- `Dockerfile` publica únicamente la aplicación web actual.
- `bdMicroMercadoArqui.sql` es compartido por todos los componentes y define tablas, triggers y vistas.
- La autenticación actual usa cookies; el objetivo futuro requiere JWT Bearer.

### Riesgos

- Mover archivos directamente en `main` podría mezclar trabajo de Ventas con cambios de Usuarios/Reportes.
- Cambios simultáneos en `Program.cs`, `.slnx`, Docker Compose y SQL pueden producir conflictos de merge o una solución que compile sólo parcialmente.
- Crear nuevos proyectos y retirar tipos del proyecto monolítico puede romper las pruebas existentes por el `ProjectReference` global.
- Cambiar nombres de claims, roles o tablas sin acuerdo puede romper Pages y repositorios.
- El informe `AUDITORIA_MONOLITO_VENTAS.md` y la modificación local de `Program.cs` no deben sobrescribirse accidentalmente.
- Las ramas remotas listadas no deben tratarse como una base actualizada sin `fetch`.

## 10. Estrategia recomendada para la migración

Estas son recomendaciones, no cambios ejecutados:

1. Crear una rama de trabajo propia para Ventas desde un estado explícitamente documentado de `main`, conservando primero los cambios locales existentes.
2. No modificar directamente las ramas de Usuarios o Reportes.
3. Preservar el segundo entregable en su forma funcional hasta que exista una solución nueva compilable.
4. Extraer primero contratos y núcleo de Ventas: entidades, FEFO, DTOs, casos de uso y puertos mínimos.
5. Crear proyectos separados para Domain, Application, Infrastructure y API/host del Servicio 1 sólo cuando los límites estén acordados.
6. Mantener el frontend Razor en un proyecto separado y reemplazar llamadas directas por `HttpClient` hacia APIs.
7. Convertir `IUsuarioActual` en un adaptador basado en JWT Bearer y definir con Usuarios el contrato de claims/roles.
8. Definir con el equipo quién posee clientes, productos y lotes; MySQL compartida no elimina la necesidad de contratos y ownership.
9. Eliminar Redis del diseño nuevo o aislarlo formalmente; el alcance indicado dice que no se usará Redis.
10. Coordinar cambios de `.slnx`, `Program.cs`, `Dockerfile`, `docker-compose.yml` y SQL en commits separados o mediante integración explícita.
11. Mantener pruebas unitarias del núcleo y agregar pruebas HTTP del Servicio 1 antes de retirar las Pages monolíticas.
12. Ejecutar integración MySQL sólo con una base efímera o explícitamente dedicada, nunca contra la base compartida de desarrollo.

### Clasificación inicial de carpetas

**Candidatas al Servicio 1:**

- `Domain/Sales`
- `Domain/Stock` si Ventas será dueño del stock/lotes
- subconjunto de `Application/Sales`
- subconjunto de `Application/Facades`
- puertos mínimos de productos/clientes
- adaptadores MySQL de ventas, lotes y consultas
- comprobantes, si el ownership se confirma

**Candidatas al frontend:**

- `Pages/Sales`
- `Pages/Comprobante`
- `wwwroot/js` y `wwwroot/css` de ventas
- autenticación de navegación y consumo HTTP

**Fuera del Servicio 1 o sujetos a coordinación:**

- administración de Usuarios
- CRUD administrativo de productos, categorías y proveedores
- Redis
- Nginx
- hosted service de baja automática de lotes
- tablas y triggers compartidos

## 11. Decisiones pendientes

1. ¿Quién es dueño funcional de `CLIENTE`, `PRODUCTO` y `LOTE`?
2. ¿Ventas consultará Usuarios y Clientes por HTTP, por puertos sobre la misma base, o mediante una combinación acordada?
3. ¿El Servicio 1 conservará generación HTML de comprobante o devolverá un DTO al frontend?
4. ¿Qué claims exactos emitirá Usuarios en JWT y qué roles podrá usar Ventas?
5. ¿Cómo se reemplazará la invalidación de historial al eliminar Redis?
6. ¿Se crearán proyectos `Domain`, `Application`, `Infrastructure` y `Api` separados para Ventas?
7. ¿Qué parte del SQL compartido puede cambiar cada equipo y cómo se versionarán esos cambios?
8. ¿Cómo se ejecutarán las pruebas MySQL en CI sin tocar datos reales?
9. ¿Se requerirá idempotencia para registrar ventas y anular ventas?
10. ¿Se conservará la baja automática de lotes en Ventas o se moverá a otro componente?
11. ¿Se mantendrá el límite actual de historial de 50 registros y paginación client-side?
12. ¿Cómo se integrará el frontend sin modificar directamente las ramas de Usuarios y Reportes?

## Conclusión

El estado actual permite compilar y ejecutar correctamente la suite no dependiente de servicios externos, pero todavía no existe una separación de proyectos para la arquitectura del tercer entregable. El repositorio debe tratarse como un monolito funcional con cambios locales pendientes, no como una solución ya preparada para extraer Ventas.

La estrategia más segura es trabajar en una rama propia, conservar el segundo entregable, extraer gradualmente contratos y dominio, y coordinar únicamente los archivos compartidos con los equipos de Usuarios y Reportes.
