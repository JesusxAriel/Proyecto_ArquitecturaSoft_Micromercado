# Estructura del proyecto PuntoMarket

Este documento describe la estructura versionada y relevante del proyecto de gestión
para el micromercado. La solución es una aplicación monolítica modular construida con
ASP.NET Core Razor Pages sobre .NET 10, C# y MySQL. La estructura está organizada en
capas lógicas para separar presentación, casos de uso, dominio y detalles técnicos.

## Árbol completo del repositorio

El árbol siguiente incluye todas las carpetas y archivos del proyecto fuente. Se
excluyen intencionalmente artefactos generados o dependencias: `.git/`, `bin/`,
`obj/`, `node_modules/`, `dist/`, `vendor/`, `packages/` y
`wwwroot/lib/` (bibliotecas frontend copiadas desde dependencias).

```tree
Proyecto_Arquitectura_Micromercado - copia/
├── .gitignore
├── PROJECT_STRUCTURE_ACTUAL.md
├── README.md
├── Proyecto_Arquitectura_Micromercado.slnx
├── bdMicroMercadoArqui.sql
├── doc/
│   └── .gitkeep
└── Proyecto_Arquitectura_Micromercado/
    ├── Application/
    │   ├── Common/
    │   │   ├── IRepositorioBase.cs
    │   │   └── IServicioCRUD.cs
    │   ├── Categories/
    │   │   ├── CategoryService.cs
    │   │   ├── ICategoryRepository.cs
    │   │   └── ICategoryService.cs
    │   ├── Products/
    │   │   ├── IProductRepository.cs
    │   │   ├── IProductService.cs
    │   │   ├── IConCatalogo.cs
    │   │   ├── IConHistorialPrecios.cs
    │   │   ├── IConListado.cs
    │   │   ├── IConRegistroHistorial.cs
    │   │   └── ProductService.cs
    │   └── Suppliers/
    │       ├── IConBusqueda.cs
    │       ├── IConListado.cs
    │       ├── ISupplierRepository.cs
    │       ├── ISupplierService.cs
    │       └── SupplierService.cs
    ├── Domain/
    │   ├── Categories/
    │   │   ├── Category.cs
    │   │   └── CategoryDtos.cs
    │   ├── Products/
    │   │   ├── Product.cs
    │   │   ├── ProductDtos.cs
    │   │   └── ProductPriceHistory.cs
    │   └── Suppliers/
    │       ├── Supplier.cs
    │       └── SupplierDtos.cs
    ├── Infrastructure/
    │   ├── Persistence/
    │   │   ├── MySqlCategoryRepository.cs
    │   │   ├── MySqlPriceHistoryRepository.cs
    │   │   ├── MySqlProductRepository.cs
    │   │   └── MySqlSupplierRepository.cs
    │   ├── Database/
    │   │   └── DatabaseConnection.cs
    │   ├── Factories/
    │   │   ├── CreatorCategoryRepository.cs
    │   │   ├── CreatorPriceHistoryRepository.cs
    │   │   ├── CreatorProductRepository.cs
    │   │   ├── CreatorRepositorio.cs
    │   │   └── CreatorSupplierRepository.cs
    │   └── Web/
    │       └── DecimalModelBinder.cs
    ├── Pages/
    │   ├── Categories/
    │   │   ├── _CategoryForm.cshtml
    │   │   ├── CategoryFormModel.cs
    │   │   ├── Create.cshtml
    │   │   ├── Create.cshtml.cs
    │   │   ├── Delete.cshtml
    │   │   ├── Delete.cshtml.cs
    │   │   ├── Edit.cshtml
    │   │   ├── Edit.cshtml.cs
    │   │   ├── Index.cshtml
    │   │   └── Index.cshtml.cs
    │   ├── Products/
    │   │   ├── _ProductForm.cshtml
    │   │   ├── Create.cshtml
    │   │   ├── Create.cshtml.cs
    │   │   ├── Delete.cshtml
    │   │   ├── Delete.cshtml.cs
    │   │   ├── Edit.cshtml
    │   │   ├── Edit.cshtml.cs
    │   │   ├── History.cshtml
    │   │   ├── History.cshtml.cs
    │   │   ├── Index.cshtml
    │   │   ├── Index.cshtml.cs
    │   │   └── ProductFormModel.cs
    │   ├── Shared/
    │   │   ├── _Layout.cshtml
    │   │   ├── _Layout.cshtml.css
    │   │   └── _ValidationScriptsPartial.cshtml
    │   ├── Suppliers/
    │   │   ├── _SupplierForm.cshtml
    │   │   ├── _SupplierForm.cshtml.cs
    │   │   ├── Create.cshtml
    │   │   ├── Create.cshtml.cs
    │   │   ├── Delete.cshtml
    │   │   ├── Delete.cshtml.cs
    │   │   ├── Edit.cshtml
    │   │   ├── Edit.cshtml.cs
    │   │   ├── Index.cshtml
    │   │   ├── Index.cshtml.cs
    │   │   └── SupplierFormModel.cs
    │   ├── _ViewImports.cshtml
    │   ├── _ViewStart.cshtml
    │   ├── Error.cshtml
    │   ├── Error.cshtml.cs
    │   ├── Index.cshtml
    │   ├── Index.cshtml.cs
    │   ├── Login.cshtml
    │   ├── Login.cshtml.cs
    │   ├── Privacy.cshtml
    │   └── Privacy.cshtml.cs
    ├── Properties/
    │   └── launchSettings.json
    ├── Validations/
    │   └── Validation.cs
    ├── wwwroot/
    │   ├── css/
    │   │   └── site.css
    │   ├── images/
    │   │   └── logo.jpg
    │   ├── js/
    │   │   └── site.js
    │   └── favicon.ico
    ├── appsettings.Development.json
    ├── appsettings.json
    ├── Program.cs
    ├── Proyecto_Arquitectura_Micromercado.csproj
    └── Proyecto_Arquitectura_Micromercado.csproj.user
```

## Organización arquitectónica

### `Domain/`: reglas y modelos del negocio

Contiene las entidades y objetos de transferencia relacionados con categorías,
productos y proveedores. Esta capa representa conceptos del micromercado y sus
validaciones, sin conocer Razor Pages, MySQL ni el contenedor de servicios.

- `Categories/` agrupa `Category` y sus atributos de validación para nombre, código,
  ubicación de pasillo y descripción.
- `Products/` agrupa `Product`, `ProductListItem`, `LookupOption`,
  `ProductPriceHistory` y atributos para textos, precio de venta y precio de costo.
- `Suppliers/` agrupa `Supplier`, `SupplierListItem`, sus DTOs y validaciones de
  correo, teléfono, nombre y la regla de proveedor autogestionado.

Esta separación favorece **responsabilidad única (SRP)**: las entidades expresan el
modelo y las reglas intrínsecas, mientras que la persistencia y la presentación se
mantienen fuera del dominio.

### `Application/`: casos de uso y contratos

Implementa la lógica de aplicación para productos, categorías y proveedores.
Cada módulo contiene interfaces de servicios y repositorios, más la implementación
del servicio correspondiente.

- `IProductService`, `ICategoryService` e `ISupplierService` definen las operaciones
  que necesita la presentación.
- `IProductRepository`, `ICategoryRepository` e `ISupplierRepository` definen las
  operaciones de persistencia que necesita cada servicio.
- `ProductService`, `CategoryService` y `SupplierService` coordinan validaciones,
  normalización, reglas de negocio, eliminación lógica, historial de precios y
  acceso a repositorios.

Las interfaces aplican **Inversión de Dependencias (DIP)** y **Segregación de
Interfaces (ISP)**: los PageModels dependen de servicios abstractos y los servicios
dependen de repositorios abstractos, no de clases MySQL concretas. También favorece
**Sustitución de Liskov (LSP)**, porque una implementación alternativa de un
repositorio puede reemplazar a la implementación MySQL sin cambiar el contrato.

`Application/Common/IRepositorioBase.cs` define
`IRepositorioBase<TEntidad, TListItem, TId>` para obtener por identificador, crear,
listar, actualizar y desactivar lógicamente de forma asíncrona. `TListItem`
conserva la diferencia entre las entidades editables y las proyecciones de listado
sin convertir una proyección incompleta en una entidad.

`Application/Common/IServicioCRUD.cs` define
`IServicioCRUD<TEntidad, TId, TCrearDto, TActualizarDto>` para las operaciones
asíncronas comunes de caso de uso. En esta iteración los DTOs de creación y
actualización son los tipos que ya reciben los servicios, por lo que no se altera
ningún consumidor existente.

Las interfaces segregadas son:

- `Products/IConListado.cs`: listado como `ProductListItem`.
- `Products/IConCatalogo.cs`: catálogos de categorías y proveedores.
- `Products/IConHistorialPrecios.cs`: consulta del historial de precios.
- `Products/IConRegistroHistorial.cs`: registro de historial, exclusivo del
  repositorio porque el servicio actual no expone ese método.
- `Suppliers/IConListado.cs`: listado como `SupplierListItem`.
- `Suppliers/IConBusqueda.cs`: comprobación de nombre de empresa duplicado.

`Category` conserva sus contratos sincrónicos para `Pages/Categories`, mientras
`CategoryService` y `MySqlCategoryRepository` implementan además los métodos
asíncronos del contrato genérico mediante `Task.FromResult`, reutilizando la lógica
sincrónica existente.

### `Infrastructure/`: adaptadores técnicos

- `Database/DatabaseConnection.cs` implementa un Singleton manual con
  **Double-Check Locking**. `Program.cs` lo inicializa una vez al arrancar mediante
  `GetInstance(builder.Configuration.GetConnectionString("MySqlConnection")!)`.
  Los repositorios obtienen conexiones nuevas para cada operación con
  `DatabaseConnection.Instance.CreateConnection()`, manteniendo centralizada la
  cadena de conexión sin registrar el Singleton en el contenedor de dependencias.
- `Factories/` implementa el patrón **Factory Method** mediante
  `CreatorRepositorio<T>` y los cuatro creadores concretos:
  `CreatorCategoryRepository`, `CreatorProductRepository`,
  `CreatorSupplierRepository` y `CreatorPriceHistoryRepository`. Los creadores ya
  no reciben `IConfiguration` y construyen sus repositorios sin parámetros.
- `Persistence/` contiene `MySqlCategoryRepository`,
  `MySqlProductRepository`, `MySqlSupplierRepository` y
  `MySqlPriceHistoryRepository`. Estas clases implementan los contratos de
  `Application` y ejecutan SQL parametrizado mediante `MySql.Data`/ADO.NET.
  Incluyen consultas de listado, búsqueda, creación, actualización, eliminación
  lógica, catálogos de categorías/proveedores e historial de precios. Las
  operaciones de creación conservan el identificador autogenerado de MySQL en la
  entidad y también lo retornan, preparando el contrato genérico para encadenar
  operaciones posteriores.
- `Web/DecimalModelBinder.cs` adapta la entrada HTTP a `decimal`, permitiendo
  interpretar formatos decimales con coma o punto según la cultura configurada.

La infraestructura queda en el borde de la aplicación: puede cambiar MySQL o el
model binding sin modificar las entidades ni los casos de uso. Esto materializa
**Abierto/Cerrado (OCP)** y DIP.

### `Pages/`: presentación Razor Pages

Es la capa de entrada web. Cada página combina una vista `.cshtml` con su
PageModel `.cshtml.cs`. Los PageModels reciben solicitudes, ejecutan model binding
y validación de `ModelState`, invocan interfaces de `Application` y devuelven una
página o redirección. No contienen SQL.

- `Categories/` ofrece listado y formularios CRUD de categorías. `Index` también
  soporta el flujo de edición; `_CategoryForm.cshtml` reutiliza el formulario y
  `CategoryFormModel.cs` centraliza dependencias y datos comunes de creación/edición.
- `Products/` ofrece listado, creación, edición, eliminación lógica e historial de
  precios. `_ProductForm.cshtml` es el formulario compartido y
  `ProductFormModel.cs` centraliza catálogos de categorías/proveedores y la
  dependencia de `IProductService`.
- `Suppliers/` ofrece CRUD de proveedores. `Index.cshtml` y su PageModel concentran
  los modales/handlers de crear, editar y eliminar; `SupplierFormModel.cs` y
  `_SupplierForm.cshtml` reutilizan la preparación y representación del formulario.
- `Shared/` contiene `_Layout.cshtml`, la navegación, recursos globales y el
  `@RenderBody`; `_Layout.cshtml.css` contiene sus estilos; y
  `_ValidationScriptsPartial.cshtml` carga validación cliente.
- `_ViewImports.cshtml` importa namespaces y Tag Helpers; `_ViewStart.cshtml`
  establece `_Layout` como layout predeterminado.
- `Index`, `Login`, `Privacy` y `Error` son páginas transversales de inicio,
  acceso visual, privacidad y manejo de errores. `Index.cshtml.cs` carga el
  resumen de productos; `Login` representa actualmente la pantalla de acceso.

La capa de presentación mantiene **SRP** al delegar la lógica de negocio a
servicios. La inyección por constructor de los PageModels evita instanciar
repositorios o servicios directamente y mantiene bajo acoplamiento.

### `Validations/`

`Validation.cs` proporciona utilidades reutilizables para campos obligatorios,
longitud máxima, correo electrónico y teléfono. Complementa las anotaciones de
datos definidas junto a las entidades/DTOs sin mezclar validación con SQL o
renderizado.

### `wwwroot/`

Recursos estáticos servidos directamente por ASP.NET Core:

- `css/site.css`: estilos globales de la interfaz.
- `images/logo.jpg`: identidad visual mostrada por el layout.
- `js/site.js`: comportamiento JavaScript global y eventos de interfaz.
- `favicon.ico`: icono del sitio.
- `lib/` no se documenta en el árbol porque contiene bibliotecas frontend de
  terceros/dependencias; además, sus subcarpetas `dist/` son artefactos de esas
  dependencias.

## Archivos de configuración y arranque

- `Program.cs`: punto de composición de la aplicación. Registra Razor Pages,
  configura `DecimalModelBinder`, registra los cuatro creadores y los servicios
  con ciclo de vida `Scoped`, inicializa el Singleton `DatabaseConnection` con la
  cadena `MySqlConnection`, configura la cultura `es-BO`, HTTPS, manejo de errores,
  routing y el mapeo de páginas.
- `appsettings.json`: configuración base, incluida la cadena
  `ConnectionStrings:MySqlConnection`, logging y hosts permitidos. La contraseña
  debe mantenerse fuera del control de versiones en un entorno real.
- `appsettings.Development.json`: errores detallados y logging de desarrollo.
- `Properties/launchSettings.json`: perfiles locales HTTP/HTTPS, puertos de
  desarrollo y variable `ASPNETCORE_ENVIRONMENT`.
- `Proyecto_Arquitectura_Micromercado.csproj`: define el SDK web, `net10.0`,
  nullable reference types, implicit usings y la dependencia `MySql.Data`.
- `Proyecto_Arquitectura_Micromercado.csproj.user`: preferencias locales de
  Visual Studio/Razor, como el perfil de depuración seleccionado.
- `Proyecto_Arquitectura_Micromercado.slnx`: archivo de solución que agrupa el
  proyecto.

## Base de datos y documentación raíz

- `bdMicroMercadoArqui.sql`: script de creación y datos de prueba de MySQL.
  Define `CATEGORIAS`, `PROVEEDOR`, `PRODUCTO`, `LOTE` y `HISTORIAL_PRECIO`,
  relaciones y restricciones, eliminación lógica mediante `estaActivo` y la vista
  `vw_productos_con_stock` para calcular inventario a partir de lotes.
- `README.md`: descripción funcional, tecnologías, arquitectura en N capas,
  flujo de operaciones y guía de funcionalidades.
- `doc/.gitkeep`: conserva la carpeta destinada a documentación adicional.
- `.gitignore`: evita versionar archivos locales, temporales y artefactos
  generados.
- `PROJECT_STRUCTURE_ACTUAL.md`: este inventario arquitectónico y de archivos.

## Flujo de dependencias e inyección

La composición se realiza en `Program.cs` con el contenedor integrado de ASP.NET
Core:

```csharp
IProductRepository  -> MySqlProductRepository
IProductService     -> ProductService
ICategoryRepository -> MySqlCategoryRepository
ICategoryService    -> CategoryService
ISupplierRepository -> MySqlSupplierRepository
ISupplierService    -> SupplierService

CreatorCategoryRepository      -> MySqlCategoryRepository
CreatorProductRepository       -> MySqlProductRepository
CreatorSupplierRepository      -> MySqlSupplierRepository
CreatorPriceHistoryRepository  -> MySqlPriceHistoryRepository

DatabaseConnection.GetInstance(...) -> DatabaseConnection.Instance
DatabaseConnection.Instance         -> MySqlConnection
```

El flujo normal de una operación es:

1. Una vista Razor envía un formulario o handler.
2. El PageModel recibe y valida los datos.
3. El PageModel invoca un `I*Service`.
4. El servicio aplica reglas del caso de uso y utiliza un `I*Repository`.
5. El repositorio MySQL solicita una conexión a
   `DatabaseConnection.Instance.CreateConnection()` y ejecuta la consulta
   parametrizada.
6. El resultado retorna al servicio y luego al PageModel para mostrarlo o redirigir.

Así, la UI conoce contratos de aplicación, la aplicación conoce contratos de
persistencia y solo `Infrastructure` conoce MySQL. Esta dirección de dependencias
preserva SOLID, facilita pruebas con dobles de las interfaces y permite cambiar
detalles técnicos sin reescribir el dominio.
