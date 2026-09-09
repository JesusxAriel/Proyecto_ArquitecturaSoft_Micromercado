# Estructura y arquitectura del módulo de Productos

## 1. Alcance y estado actual

Este documento describe la versión actual del proyecto
`Proyecto_Arquitectura_Micromercado` después de los últimos cambios. La interfaz
del CRUD está ubicada en `Pages/Products/`; no existe una carpeta
`Pages/Productos/` en la versión vigente. El módulo usa Razor Pages, ADO.NET
con `MySql.Data` y no expone controladores REST.

El flujo principal es:

```text
Razor Page (.cshtml)
        |
PageModel (.cshtml.cs)
        |
IProductService -> ProductService
        |
IProductRepository -> MySqlProductRepository
        |
MySQL: PRODUCTO, CATEGORIAS, PROVEEDOR, vw_productos_con_stock
```

La separación física actual es:

```text
Proyecto_Arquitectura_Micromercado/
├── Application/Products/
│   ├── IProductRepository.cs
│   ├── IProductService.cs
│   └── ProductService.cs
├── Domain/Products/
│   ├── Product.cs
│   └── ProductDtos.cs
├── Infrastructure/
│   ├── Persistence/MySqlProductRepository.cs
│   └── Web/DecimalModelBinder.cs
├── Pages/
│   ├── Products/
│   │   ├── Index.cshtml
│   │   ├── Index.cshtml.cs
│   │   ├── Create.cshtml
│   │   ├── Create.cshtml.cs
│   │   ├── Edit.cshtml
│   │   ├── Edit.cshtml.cs
│   │   ├── Delete.cshtml
│   │   ├── Delete.cshtml.cs
│   │   ├── ProductFormModel.cs
│   │   └── _ProductForm.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Privacy.cshtml
│   ├── Privacy.cshtml.cs
│   ├── Error.cshtml
│   ├── Error.cshtml.cs
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── Program.cs
├── appsettings.json
└── Proyecto_Arquitectura_Micromercado.csproj
```

## 2. Responsabilidad de cada archivo

### Raíz y configuración

#### `Program.cs`

Pertenece al namespace global de la aplicación y configura el host ASP.NET
Core. Registra Razor Pages, el `DecimalModelBinderProvider`, el mensaje
español para errores numéricos y las implementaciones de las interfaces:

```csharp
builder.Services.AddScoped<IProductRepository, MySqlProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
```

También configura la cultura `es-BO`, el middleware HTTP, archivos estáticos,
autorización y `MapRazorPages()`. No registra `AddControllers()` ni
`MapControllers()`, por lo que no hay una ruta REST duplicada. La página `/`
continúa siendo la página Home estándar; Productos se abre explícitamente en
`/Products/Index`.

#### `appsettings.json`

Contiene la cadena de conexión nombrada `MySqlConnection`, además de logging y
`AllowedHosts`. La contraseña real no se documenta aquí por seguridad. El
repositorio obtiene esta configuración mediante `IConfiguration`.

#### `Proyecto_Arquitectura_Micromercado.csproj`

Define el proyecto web .NET 10 con nullable reference types e implicit usings.
La única dependencia externa declarada es `MySql.Data` 26.7.0, utilizada por la
persistencia ADO.NET. No se usa Entity Framework.

#### `bdMicroMercadoArqui.sql`

Define el esquema MySQL consumido por el módulo: `PRODUCTO`, `CATEGORIAS`,
`PROVEEDOR`, `LOTE`, `HISTORIAL_PRECIO` y la vista
`vw_productos_con_stock`. Las tablas manejan `estaActivo` para eliminación
lógica. La vista filtra productos activos, calcula el stock a partir de lotes
activos y entrega los campos que usa `ProductListItem`.

### Dominio: `Domain/Products/`

Namespace: `Proyecto_Arquitectura_Micromercado.Domain.Products`.

#### `Product.cs`

Define:

- `Product`: entidad editable del dominio, con identidad, nombre,
  presentación, precios, stock mínimo, categoría, proveedor y estado activo.
- `ProductListItem`: proyección de lectura para la tabla de Productos,
  incluyendo nombres de categoría/proveedor y `StockCalculado`.
- `LookupOption`: record para opciones simples de categorías y proveedores.

Las propiedades editables tienen `Display`, `Required`, `StringLength`,
`Range`, `RegularExpression`, `ProductText`, `SalePrice` y `CostPrice`.
Estas anotaciones producen metadatos para validación server-side y mensajes
para Razor/Unobtrusive.

#### `ProductDtos.cs`

Aunque el archivo contiene reglas y atributos compartidos más que DTOs
tradicionales, concentra la política reutilizable:

- `ProductPriceValidation.DecimalPattern` acepta dígitos con separador `.` o
  `,` y hasta dos decimales.
- `SaleMessage` y `CostMessage` son mensajes cortos compartidos.
- `ProductTextAttribute` rechaza caracteres de control.
- `SalePriceAttribute` exige un precio positivo en múltiplos de `0.10`.
- `CostPriceAttribute` exige un precio positivo y permite cualquier centavo
  con hasta dos decimales.

### Aplicación: `Application/Products/`

Namespace: `Proyecto_Arquitectura_Micromercado.Application.Products`.

#### `IProductRepository.cs`

Abstrae el acceso a datos. Expone consultas de listado, producto individual,
categorías y proveedores, además de crear, actualizar y eliminar lógicamente.
Usa `Task`, listas de solo lectura y `CancellationToken`, sin acoplar la capa
de aplicación a MySQL.

#### `IProductService.cs`

Define el caso de uso del módulo para que los PageModels dependan de una
abstracción. Replica las operaciones públicas del CRUD y de los catálogos.

#### `ProductService.cs`

Implementa `IProductService` y coordina reglas antes de persistir:

1. Verifica que la entidad no sea nula.
2. Normaliza nombre y presentación.
3. Redondea precios a dos decimales con
   `MidpointRounding.AwayFromZero`.
4. Comprueba campos obligatorios, precio de venta mínimo y múltiplo de
   `0.10`, precio de costo positivo, stock no negativo y claves de catálogo
   positivas.
5. Delega la operación validada en `IProductRepository`.

`ToTitleCase` elimina espacios repetidos con
`string.Join(" ", text.Split(..., StringSplitOptions.RemoveEmptyEntries))` y
aplica `CultureInfo.CurrentCulture.TextInfo.ToTitleCase` sobre texto en
minúsculas. Así la normalización queda centralizada y no depende únicamente
del navegador.

### Infraestructura: `Infrastructure/`

#### `Infrastructure/Persistence/MySqlProductRepository.cs`

Namespace: `Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence`.

Implementa `IProductRepository` con `MySqlConnection`, `MySqlCommand` y
lectores ADO.NET:

- `GetAllAsync` consulta `vw_productos_con_stock` y ordena por nombre.
- `GetByIdAsync` consulta `PRODUCTO` únicamente si `estaActivo = 1`.
- `GetCategoriesAsync` y `GetSuppliersAsync` cargan catálogos activos.
- `CreateAsync` inserta el producto y devuelve `LAST_INSERT_ID()`.
- `UpdateAsync` actualiza únicamente registros activos.
- `SoftDeleteAsync` cambia `estaActivo` a `0`.

Los parámetros SQL se agregan mediante `AddProductParameters`, evitando
concatenar valores del usuario. Las conexiones y lectores se liberan con
`await using`, y las operaciones aceptan cancelación.

#### `Infrastructure/Web/DecimalModelBinder.cs`

Namespace: `Proyecto_Arquitectura_Micromercado.Infrastructure.Web`.

`DecimalModelBinder` valida y convierte bindings `decimal` directos. Acepta
coma o punto, rechaza separadores de miles, notación científica y más de dos
decimales, normaliza a punto y convierte con `CultureInfo.InvariantCulture`.
`DecimalModelBinderProvider` lo aplica a propiedades cuyo tipo es exactamente
`decimal`.

Las páginas Create/Edit usan además strings (`ProductPriceInput`) para que la
entrada de formulario no sea bloqueada por la validación HTML5 o por la
cultura del navegador; el binder permanece disponible para otros bindings
decimales de la aplicación.

### Interfaz Razor: `Pages/`

Los PageModels usan el namespace
`Proyecto_Arquitectura_Micromercado.Pages.Products` y reciben
`IProductService` por inyección de dependencias.

#### `Pages/Products/Index.cshtml` y `Index.cshtml.cs`

`Index.cshtml` declara `@page` sin ruta literal y muestra la tabla de productos,
precios, categoría, proveedor y stock calculado. Marca `Stock bajo` cuando el
stock es menor o igual al stock mínimo y ofrece enlaces a Create, Edit y
Delete.

`IndexModel` llama a `IProductService.GetAllAsync()` en `OnGetAsync` y expone
`IReadOnlyList<ProductListItem>`. No contiene SQL ni reglas de persistencia.

#### `Pages/Products/Create.cshtml` y `Create.cshtml.cs`

La vista define el título y reutiliza `_ProductForm`; carga
`_ValidationScriptsPartial` en la sección `Scripts`.

`CreateModel` inicializa los precios como `"0,00"`, carga catálogos en GET y,
en POST, ejecuta `TryParsePrices()` antes de comprobar `ModelState`. Si falla,
recarga los catálogos y devuelve la página. Si todo es válido, llama a
`CreateAsync` y redirige a `/Products/Index`.

#### `Pages/Products/Edit.cshtml` y `Edit.cshtml.cs`

La vista tiene la misma composición que Create, pero usa `EditModel`.
`OnGetAsync` busca el producto por id, devuelve `NotFound()` si no existe,
precarga `Product` y formatea los precios con `"0.00"` e
`InvariantCulture`.

En POST se valida el id oculto, se procesan los precios con el mismo flujo
compartido y se llama a `UpdateAsync`. Si el registro ya no está activo o no
existe, devuelve `NotFound()`; en éxito redirige a `/Products/Index`.

#### `Pages/Products/Delete.cshtml` y `Delete.cshtml.cs`

La vista muestra una confirmación, incluye `Product.Id` como campo oculto y
`@Html.AntiForgeryToken()`. El usuario es informado de que la eliminación es
lógica.

`DeleteModel` carga el producto activo en GET. En POST valida que el id sea
positivo, llama a `SoftDeleteAsync` y redirige al listado; no borra físicamente
filas de MySQL.

#### `Pages/Products/ProductFormModel.cs`

Es la clase base abstracta compartida por Create y Edit:
`Proyecto_Arquitectura_Micromercado.Pages.Products.ProductFormModel`.

Centraliza:

- La propiedad `Product` enlazada con el formulario.
- La propiedad `Input` de tipo `ProductPriceInput`.
- La carga de `Categories` y `Suppliers`.
- `TryParsePrices`, que elimina el estado decimal automático, sustituye coma
  por punto, valida el patrón, convierte con `InvariantCulture`, exige
  valores positivos, aplica el múltiplo de `0.10` solamente a venta y redondea.
- `SetPriceInputs`, que prepara los valores existentes para Edit.

`ProductPriceInput` contiene `PrecioVentaInput` y `PrecioCostoInput` como
strings, decisión que permite aceptar `"30,90"` y `"30.90"` de forma uniforme.

#### `Pages/Products/_ProductForm.cshtml`

Es un partial fuertemente tipado con
`ProductFormModel`. Contiene una sola copia del HTML de los campos, etiquetas,
mensajes de validación, token antiforgery, id oculto, selects y botones.
Create y Edit lo invocan con:

```razor
<partial name="_ProductForm" model="Model" />
```

Esto aplica DRY: cambios de layout, nombres, validaciones visuales o
redirección de cancelar se realizan en un solo lugar. Los precios son inputs
`type="text"` con `inputmode="decimal"` para evitar conflictos de locale y el
stock usa `inputmode="numeric"`.

#### `Pages/Shared/_Layout.cshtml`

Es el layout global de Razor Pages. Define navegación Home, Privacy y
Productos mediante `/Products/Index`, carga Bootstrap/jQuery y renderiza la
sección opcional `Scripts`. La página Home sigue siendo `Pages/Index.cshtml`.

#### `Pages/Shared/_ValidationScriptsPartial.cshtml`

Carga jQuery Validate y jQuery Validate Unobtrusive. Sobrescribe el método
`number` para admitir tanto `,` como `.` como separador decimal antes de que la
validación cliente bloquee el formulario.

#### `Pages/_ViewImports.cshtml`

Registra los tag helpers y los usings globales, incluido:

```razor
@using Proyecto_Arquitectura_Micromercado.Pages.Products
```

#### `Pages/_ViewStart.cshtml`

Selecciona `_Layout` como layout común de las páginas Razor.

#### `Pages/Index.cshtml` y `Pages/Index.cshtml.cs`

Son la página Home estándar de la plantilla Razor Pages. No redirigen
automáticamente al CRUD; el usuario llega a Productos desde el menú.

#### `Pages/Privacy.cshtml` y `Privacy.cshtml.cs`

Conservan la página informativa estándar del proyecto y no participan en el
CRUD.

#### `Pages/Error.cshtml` y `Error.cshtml.cs`

Proporcionan la página de error usada por `UseExceptionHandler("/Error")` en
entornos que no son Development.

## 3. SOLID, Clean Code y patrones aplicados

### SRP

- Las vistas `.cshtml` se ocupan de presentación, binding de controles y
  mensajes visuales.
- Los PageModels `.cshtml.cs` coordinan el ciclo HTTP de cada pantalla.
- `Product` y los atributos del dominio expresan datos y reglas de validación.
- `ProductService` aplica reglas de negocio y normalización.
- `MySqlProductRepository` se ocupa exclusivamente de persistencia MySQL.
- `DecimalModelBinder` se ocupa del binding de decimales.
- `_ProductForm.cshtml` mantiene una única responsabilidad de renderizar el
  formulario compartido.

### OCP y DIP

Los PageModels dependen de `IProductService`, y `ProductService` depende de
`IProductRepository`. Las implementaciones concretas se conectan en
`Program.cs` mediante DI. Es posible sustituir la persistencia o probar la
capa de aplicación con otra implementación sin modificar las páginas.

### DRY

El formulario compartido, `ProductFormModel`, `ProductPriceInput`,
`ProductPriceValidation` y los atributos de validación evitan duplicar
marcado, parsing, mensajes y reglas entre Create y Edit.

### Clean Code y Clean Architecture

Las dependencias apuntan hacia abstracciones: Domain no conoce MySQL ni Razor;
Application coordina casos de uso; Infrastructure contiene detalles externos;
Pages es el adaptador de presentación. Los nombres `GetAllAsync`,
`SoftDeleteAsync`, `TryParsePrices` y `LoadLookupsAsync` expresan intención,
las operaciones son asíncronas y se usan tipos de solo lectura donde
corresponde.

## 4. Validación y reglas de negocio

### Capa de formulario y JavaScript

Los Tag Helpers generan nombres, mensajes y atributos de validación para
Unobtrusive. `_ValidationScriptsPartial.cshtml` permite separadores `,` y `.`.
Los precios no usan `type="number"`: se envían como texto con teclado decimal.
Los campos de categoría y proveedor comienzan en valor `0` para activar
`[Range(1, int.MaxValue)]`.

### DataAnnotations y atributos del dominio

`Product` exige nombre y presentación, limita longitudes, valida claves de
catálogo, impide stock negativo y aplica expresiones regulares de precios.
`SalePriceAttribute` exige venta positiva y múltiplo de `0.10`; `CostPriceAttribute`
solo exige costo positivo. `ProductTextAttribute` bloquea caracteres de control.

### Server Logic

`ProductFormModel.TryParsePrices` normaliza coma/punto y valida manualmente
antes de `ModelState.IsValid`. `ProductService.Validate` es la última barrera:
redondea a dos decimales, normaliza texto y rechaza combinaciones inválidas
antes de tocar la base de datos. Por ello las reglas no dependen solamente de
JavaScript o de un cliente específico.

### Reglas bolivianas implementadas

1. **Precio de venta:** debe ser positivo, tener como máximo dos decimales y
   estar en múltiplos de `0.10 Bs` (`10,50`, `10.90`, `30,00`).
2. **Precio de costo:** debe ser positivo y tener como máximo dos decimales;
   valores como `6,99` y `15.43` son válidos.
3. **Separador decimal:** se aceptan punto y coma en formularios y bindings.
   No se aceptan notación científica, separadores de miles ni más de dos
   decimales.
4. **Redondeo:** los valores persistidos se redondean a dos decimales con
   `MidpointRounding.AwayFromZero`.
5. **Texto:** nombre y presentación se recortan conceptualmente al eliminar
   espacios vacíos repetidos y se convierten a Title Case antes de guardar.
6. **Stock mínimo:** es entero mayor o igual que cero.
7. **Categoría y proveedor:** deben corresponder a opciones seleccionadas con
   identificador positivo.
8. **Eliminación:** siempre es lógica (`estaActivo = 0`).

## 5. Rutas funcionales

| Operación | Ruta | PageModel |
|---|---|---|
| Listar | `/Products/Index` | `IndexModel` |
| Crear | `/Products/Create` | `CreateModel` |
| Editar | `/Products/Edit?id={id}` | `EditModel` |
| Eliminar | `/Products/Delete?id={id}` | `DeleteModel` |
| Inicio | `/` | `Pages.IndexModel` |

Las cuatro páginas CRUD empiezan con `@page` sin rutas hardcodeadas, evitando
ambigüedad de endpoints. Las redirecciones exitosas apuntan explícitamente a
`/Products/Index`.
