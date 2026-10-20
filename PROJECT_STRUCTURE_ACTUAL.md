# Estructura actual del proyecto PuntoMarket

Este documento describe la estructura versionada y relevante del sistema de
gestion para el micromercado. La solucion es una aplicacion monolitica modular
con ASP.NET Core Razor Pages sobre .NET 10, C# y MySQL. Las capas logicas viven
dentro de `Proyecto_Arquitectura_Micromercado/`; los dobles en memoria y las
pruebas se encuentran en un proyecto separado.

El inventario se actualizo a partir del contenido actual del repositorio. Se
omiten `.git/`, `bin/`, `obj/`, dependencias de `wwwroot/lib/` y otros
artefactos generados.

## Arbol del repositorio

```text
Proyecto_Arquitectura_Micromercado - copia/
├── .gitignore
├── README.md
├── PROJECT_STRUCTURE_ACTUAL.md
├── EXPLICACION_ESTRUCTURA_ACTUAL.md
├── INFORME_ANALISIS_ATQUERRA.md
├── historial_actualizado.txt
├── historial_commits.txt
├── bdMicroMercadoArqui.sql
├── Proyecto_Arquitectura_Micromercado.slnx
├── doc/
│   └── .gitkeep
├── Proyecto_Arquitectura_Micromercado/
│   ├── Application/
│   │   ├── Categories/
│   │   │   ├── CategoryCodeGenerator.cs
│   │   │   ├── CategoryCodePreview.cs
│   │   │   ├── CategoryService.cs
│   │   │   ├── ICategoryRepository.cs
│   │   │   ├── ICategoryService.cs
│   │   │   ├── IConListado.cs
│   │   │   ├── IConListadoPaginado.cs
│   │   │   └── IConVerificacionDeNombre.cs
│   │   ├── Common/
│   │   │   ├── ErrorDePersistenciaException.cs
│   │   │   ├── IDetectorDeInterbloqueo.cs
│   │   │   ├── IRepositorioBase.cs
│   │   │   ├── IServicioCRUD.cs
│   │   │   ├── IUnidadDeTrabajo.cs
│   │   │   └── PagedQuery.cs
│   │   ├── Comprobantes/
│   │   ├── Customers/
│   │   ├── Facades/
│   │   ├── Products/
│   │   ├── Sales/
│   │   ├── Security/
│   │   ├── Stock/
│   │   └── Suppliers/
│   ├── Domain/
│   │   ├── Categories/
│   │   ├── Common/
│   │   ├── Products/
│   │   ├── Sales/
│   │   ├── Stock/
│   │   └── Suppliers/
│   ├── Infrastructure/
│   │   ├── Background/
│   │   ├── Comprobantes/
│   │   ├── Database/
│   │   ├── Factories/
│   │   ├── Persistence/
│   │   │   ├── EnMemoria/
│   │   │   └── Errores/
│   │   ├── Security/
│   │   └── Web/
│   ├── Pages/
│   │   ├── Categories/Index.cshtml[.cs]
│   │   ├── Comprobante/Index.cshtml[.cs]
│   │   ├── Customers/Create.cshtml[.cs]
│   │   ├── Products/
│   │   │   ├── Catalog.cshtml[.cs]
│   │   │   ├── History.cshtml[.cs]
│   │   │   ├── Index.cshtml[.cs]
│   │   │   └── _PriceHistoryDetail.cshtml
│   │   ├── Sales/
│   │   │   ├── Historial.cshtml[.cs]
│   │   │   ├── Index.cshtml[.cs]
│   │   │   └── SaleInputModel.cs
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml[.css]
│   │   │   ├── _Pager.cshtml
│   │   │   ├── _SearchBox.cshtml
│   │   │   ├── ComprobanteUrl.cs
│   │   │   ├── EjecutorDePeticiones.cs
│   │   │   ├── PrincipalDeSesion.cs
│   │   │   ├── SearchBoxModel.cs
│   │   │   ├── UniqueCheckResult.cs
│   │   │   └── VariablesDeSesion.cs
│   │   ├── Suppliers/Index.cshtml[.cs]
│   │   ├── AccesoDenegado.cshtml[.cs]
│   │   ├── Error.cshtml[.cs]
│   │   ├── Index.cshtml[.cs]
│   │   ├── Login.cshtml[.cs]
│   │   ├── Logout.cshtml[.cs]
│   │   ├── Privacy.cshtml[.cs]
│   │   ├── _ViewImports.cshtml
│   │   └── _ViewStart.cshtml
│   ├── Properties/launchSettings.json
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   ├── images/logo.jpg
│   │   ├── js/
│   │   │   ├── historial.js
│   │   │   ├── site.js
│   │   │   ├── ventas-reglas.js
│   │   │   └── ventas.js
│   │   └── favicon.ico
│   ├── appsettings.example.json
│   ├── appsettings.json
│   ├── Program.cs
│   └── Proyecto_Arquitectura_Micromercado.csproj
└── Proyecto_Arquitectura_Micromercado.Tests/
    ├── pruebas unitarias de dominio, aplicacion e infraestructura (*.cs)
    ├── pruebas de reglas JavaScript (js/ventas-reglas.test.cjs)
    └── Proyecto_Arquitectura_Micromercado.Tests.csproj
```

`[.cs]` representa el PageModel asociado a una vista Razor. Las carpetas que
aparecen sin detallar contienen los archivos del modulo indicado; el listado
completo puede consultarse directamente en el repositorio.

## Capas y responsabilidades

### `Domain/`

Contiene las reglas y modelos del negocio, sin referencias a Razor Pages, SQL o
MySQL:

- `Categories/`: categoria, DTOs y excepciones de duplicidad.
- `Products/`: producto, DTOs, historial, cambio de precio y validaciones.
- `Suppliers/`: proveedor, DTOs y reglas de duplicidad o de productos asociados.
- `Sales/`: venta, detalle y estados de venta.
- `Stock/`: lotes disponibles, consumo FEFO, resultados de baja y excepciones
  de stock insuficiente o restauracion invalida.
- `Common/`: `PagedResult<T>` para resultados paginados.

### `Application/`

Define contratos y coordina casos de uso:

- `Categories/`, `Products/` y `Suppliers/`: servicios, repositorios,
  contratos segregados, validaciones y operaciones paginadas.
- `Customers/`: consulta y creacion de clientes, DTOs y deteccion de
  duplicados de CI/NIT.
- `Sales/`: registro, consulta, anulacion y DTOs de ventas.
- `Facades/`: fachadas para venta, anulacion y actualizacion de stock.
- `Comprobantes/`: servicio, DTOs y contrato del generador de comprobantes.
- `Security/`: autenticacion, roles, claims, usuario actual y credenciales.
- `Stock/`: baja automatica de lotes vencidos y contrato del servicio.
- `Common/`: repositorios y servicios genericos, unidad de trabajo, paginacion,
  deteccion de interbloqueos y errores de persistencia.

Los servicios dependen de interfaces. Las implementaciones MySQL y en memoria
se encuentran en `Infrastructure/`.

### `Infrastructure/`

Implementa los adaptadores tecnicos:

- `Persistence/`: repositorios MySQL para categorias, productos, proveedores,
  clientes, ventas, comprobantes, usuarios y lotes; unidad de trabajo,
  consultas paginadas, reintentos y traduccion de errores.
- `Persistence/EnMemoria/`: repositorios y unidad de trabajo para pruebas.
- `Factories/`: Factory Method para seleccionar repositorios MySQL o en memoria.
- `Database/DatabaseConnection.cs`: acceso centralizado a conexiones MySQL.
- `Comprobantes/`: generador HTML de comprobantes.
- `Security/`: hash BCrypt.
- `Web/`: model binder decimal, campos no enlazables, sesion vencida y
  adaptadores de usuario actual.
- `Background/`: baja automatica de lotes vencidos.

### `Pages/`

Es la capa de presentacion Razor Pages. Los PageModels reciben dependencias por
constructor, validan entradas y delegan la logica a `Application`:

- `Categories/Index`: listado paginado y operaciones CRUD de categorias.
- `Products/Index`, `History` y `Catalog`: gestion, historial de precios y
  catalogo de consulta.
- `Suppliers/Index`: listado y operaciones CRUD de proveedores.
- `Customers/Create`: alta minima de clientes para el flujo de ventas.
- `Sales/Index` y `Sales/Historial`: registro, consulta y anulacion de ventas.
- `Comprobante/Index`: consulta y renderizado de comprobantes.
- `Login`, `Logout` y `AccesoDenegado`: autenticacion y autorizacion.
- `Shared/`: layout, paginador, buscador y utilidades de sesion/comprobantes.

### `wwwroot/`

Contiene recursos estaticos. `ventas.js` y `ventas-reglas.js` gestionan la
interaccion del detalle de venta, busqueda, cantidades y validaciones; `site.js`
e `historial.js` contienen comportamiento general e historial. `lib/` contiene
dependencias frontend de terceros y se omite del arbol detallado.

## Configuracion, base de datos y pruebas

- `Program.cs`: registra Razor Pages, autenticacion, autorizacion, servicios,
  repositorios, factories, model binders, cultura y tareas de fondo.
- `appsettings.json` y `appsettings.example.json`: configuracion y plantilla de
  configuracion; las credenciales reales no deben versionarse.
- `Properties/launchSettings.json`: perfiles locales de ejecucion.
- `Proyecto_Arquitectura_Micromercado.csproj`: proyecto web .NET 10 y
  dependencias de ejecucion, incluida `MySql.Data`.
- `bdMicroMercadoArqui.sql`: esquema y datos iniciales de MySQL, incluyendo
  categorias, proveedores, productos, lotes, ventas, clientes, usuarios,
  comprobantes e historial de precios.
- `Proyecto_Arquitectura_Micromercado.Tests/`: pruebas de dominio, servicios,
  fachadas, handlers Razor Pages, repositorios en memoria, seguridad,
  accesibilidad y reglas JavaScript.

## Flujo de dependencias

```text
Razor Page / PageModel
        |
        v
Servicio o fachada de Application
        |
        v
Contrato de repositorio / unidad de trabajo
        |
        v
Infrastructure (MySQL o implementacion en memoria)
        |
        v
Base de datos MySQL
```

La interfaz web no ejecuta SQL ni instancia repositorios concretos. Los
repositorios utilizan consultas parametrizadas y las pruebas pueden sustituir
las implementaciones MySQL por dobles en memoria. Esta separacion mantiene la
direccion de dependencias hacia los contratos y permite probar los casos de uso
sin requerir una base de datos real.
