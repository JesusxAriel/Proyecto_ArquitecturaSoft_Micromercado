# Explicación de la estructura del proyecto

Este documento explica cómo está organizado el código real de PuntoMarket y cómo
se conectan sus capas. Las rutas se expresan relativas a la raíz del repositorio:
`Proyecto_Arquitectura_Micromercado - copia/`.

La solución es una aplicación ASP.NET Core Razor Pages sobre .NET 10. La
separación actual es por capas `Domain`, `Application`, `Infrastructure` y
`Pages`; no es una solución con proyectos separados por capa: todas las capas
viven dentro del proyecto `Proyecto_Arquitectura_Micromercado/`.

## 1. Carpetas de primer nivel del proyecto

### `Proyecto_Arquitectura_Micromercado/Domain/`

Contiene el modelo del negocio: entidades, proyecciones de listado, historial de
precios y atributos de validación específicos de las entidades.

- `Categories/`: `Category.cs` y `CategoryDtos.cs`.
- `Products/`: `Product.cs`, `ProductDtos.cs` y `ProductPriceHistory.cs`.
- `Suppliers/`: `Supplier.cs` y `SupplierDtos.cs`.

Aquí deben vivir las reglas propias de los datos del negocio, como la forma válida
de un código de categoría o el rango de un precio. No deberían vivir aquí las
consultas SQL, conexiones MySQL, PageModels, vistas Razor, navegación HTTP ni el
registro del contenedor de dependencias.

### `Proyecto_Arquitectura_Micromercado/Application/`

Contiene los contratos y servicios de aplicación para categorías, productos y
proveedores. Los servicios validan/normalizan datos y delegan persistencia en
interfaces de repositorio.

- `Common/`: contratos genéricos reutilizables.
- `Categories/`: contratos y `CategoryService`.
- `Products/`: contratos, extensiones segregadas y `ProductService`.
- `Suppliers/`: contratos, extensiones segregadas y `SupplierService`.

No deberían vivir aquí SQL concreto, `MySqlConnection`, vistas `.cshtml`,
PageModels ni código de presentación. Los repositorios se declaran mediante
interfaces aquí, pero sus implementaciones MySQL están en `Infrastructure`.

### `Proyecto_Arquitectura_Micromercado/Infrastructure/`

Contiene adaptadores técnicos que implementan detalles externos:

- `Persistence/`: `MySqlCategoryRepository`, `MySqlProductRepository` y
  `MySqlSupplierRepository`, que ejecutan SQL con `MySql.Data`.
- `Web/DecimalModelBinder.cs`: binder de ASP.NET Core para interpretar valores
  decimales introducidos desde formularios.

No deberían vivir aquí las entidades del negocio, reglas específicas de validación
de dominio, vistas Razor ni la lógica de navegación de las Pages. Esta capa sí
puede conocer MySQL y ASP.NET Core porque su función es adaptar esos detalles a
los contratos de `Application`.

### `Proyecto_Arquitectura_Micromercado/Pages/`

Es la presentación Razor Pages. Cada página combina una vista `.cshtml` con un
PageModel `.cshtml.cs`. Los PageModels reciben formularios, consultan
`ModelState`, llaman servicios de `Application` y producen páginas,
redirecciones o respuestas HTTP.

- `Categories/`: listado y CRUD de categorías.
- `Products/`: listado, CRUD e historial de precios.
- `Suppliers/`: listado y CRUD de proveedores.
- `Shared/`: layout y recursos Razor compartidos.

No deberían vivir aquí SQL, conexiones MySQL ni reglas de negocio que deban
reutilizarse. La presentación tampoco debería instanciar repositorios concretos:
recibe servicios mediante inyección de dependencias.

### `Proyecto_Arquitectura_Micromercado/Validations/`

Contiene [Validation.cs](Proyecto_Arquitectura_Micromercado/Validations/Validation.cs),
una clase estática con utilidades genéricas para campos obligatorios, longitud,
correo y teléfono.

No debería contener consultas, acceso a base de datos, PageModels ni validaciones
que dependan de una pantalla concreta. En el estado actual, sus métodos no son
llamados por ningún archivo de `Application`; las validaciones que sí participan
en los formularios están definidas como Data Annotations y validadores propios
dentro de `Domain`.

### `Proyecto_Arquitectura_Micromercado/wwwroot/`

Contiene recursos estáticos servidos directamente:

- `css/site.css`: estilos globales.
- `js/site.js`: comportamiento JavaScript global.
- `images/logo.jpg`: logo de la aplicación.
- `favicon.ico`: icono del sitio.
- `lib/`: bibliotecas frontend de terceros.

No deberían vivir aquí código C# de negocio, repositorios, servicios ni secretos de
configuración. `wwwroot` contiene recursos que pueden ser enviados al navegador.

## 2. Contratos genéricos de `Application/Common`

### `Application/Common/IRepositorioBase.cs`

Declara:

```csharp
IRepositorioBase<TEntidad, TListItem, TId>
```

Sus parámetros significan:

- `TEntidad`: entidad completa usada para obtener por ID, crear y actualizar.
  Ejemplos actuales: `Product`, `Supplier` y `Category`.
- `TListItem`: tipo de cada elemento devuelto por el listado. Permite que un
  listado sea una proyección distinta de la entidad editable. Ejemplos:
  `ProductListItem`, `SupplierListItem` y `Category`.
- `TId`: tipo del identificador. En los tres repositorios actuales es `int`.

El contrato declara:

- `GetAllAsync`: devuelve `IReadOnlyList<TListItem>`.
- `GetByIdAsync`: devuelve `TEntidad?`.
- `CreateAsync`: recibe `TEntidad` y devuelve `TId`.
- `UpdateAsync`: recibe `TEntidad` y devuelve `bool`.
- `SoftDeleteAsync`: recibe `TId` y devuelve `bool`.

Los métodos son asíncronos y reciben `CancellationToken`. Las interfaces concretas
pueden agregar operaciones específicas mediante interfaces segregadas.

### `Application/Common/IServicioCRUD.cs`

Declara:

```csharp
IServicioCRUD<TEntidad, TId, TCrearDto, TActualizarDto>
```

Sus parámetros significan:

- `TEntidad`: tipo que devuelve `GetByIdAsync`.
- `TId`: tipo del identificador y del resultado de creación.
- `TCrearDto`: tipo que recibe `CreateAsync`.
- `TActualizarDto`: tipo que recibe `UpdateAsync`.

En el estado actual, los servicios usan la entidad como ambos DTOs:

```text
Producto:  IServicioCRUD<Product, int, Product, Product>
Categoría:  IServicioCRUD<Category, int, Category, Category>
Proveedor:  IServicioCRUD<Supplier, int, Supplier, Supplier>
```

Esto significa que todavía no existen DTOs separados de creación y actualización
en los contratos de servicios. Los métodos del contrato son
`GetByIdAsync`, `CreateAsync`, `UpdateAsync` y `SoftDeleteAsync`.

## 3. Contratos por entidad

### Categoría

La entidad está en `Domain/Categories/Category.cs`.

- Repositorio:
  `Application/Categories/ICategoryRepository.cs`.
- Implementación:
  `Infrastructure/Persistence/MySqlCategoryRepository.cs`.
- Servicio:
  `Application/Categories/ICategoryService.cs`.
- Implementación:
  `Application/Categories/CategoryService.cs`.

`ICategoryRepository` hereda de:

```csharp
IRepositorioBase<Category, Category, int>
```

Además conserva sus métodos sincrónicos originales:
`GetActive`, `GetById`, `Add`, `Update` y `Delete`.

`ICategoryService` hereda de:

```csharp
IServicioCRUD<Category, int, Category, Category>
```

También conserva sus métodos sincrónicos originales:
`GetActive`, `GetById`, `Create`, `Update` y `Delete`.

No usa `IConCatalogo`, `IConHistorialPrecios`, `IConRegistroHistorial`,
`IConListado` ni `IConBusqueda`. La categoría no tiene una interfaz segregada
adicional en el código actual.

### Producto

La entidad está en `Domain/Products/Product.cs`.

- Repositorio:
  `Application/Products/IProductRepository.cs`.
- Implementación:
  `Infrastructure/Persistence/MySqlProductRepository.cs`.
- Servicio:
  `Application/Products/IProductService.cs`.
- Implementación:
  `Application/Products/ProductService.cs`.

`IProductRepository` hereda de:

```csharp
IRepositorioBase<Product, ProductListItem, int>
IConCatalogo
IConHistorialPrecios
IConRegistroHistorial
```

Las interfaces segregadas viven en:

- `Application/Products/IConCatalogo.cs`: `GetCategoriesAsync` y
  `GetSuppliersAsync`.
- `Application/Products/IConHistorialPrecios.cs`: `GetPriceHistoryAsync`.
- `Application/Products/IConRegistroHistorial.cs`:
  `AddPriceHistoryAsync`.

El listado `ProductListItem` proviene de `GetAllAsync` del contrato base, por lo
que `IProductRepository` no hereda una interfaz de listado adicional.

`IProductService` hereda de:

```csharp
IServicioCRUD<Product, int, Product, Product>
IConListado
IConCatalogo
IConHistorialPrecios
```

En el servicio, `IConListado` vive en
`Application/Products/IConListado.cs` y declara el listado como
`IReadOnlyList<ProductListItem>`. El servicio no hereda `IConRegistroHistorial`
porque el servicio actual no expone públicamente el registro directo del
historial; el repositorio lo usa internamente durante la actualización de precios.

### Proveedor

La entidad está en `Domain/Suppliers/Supplier.cs`.

- Repositorio:
  `Application/Suppliers/ISupplierRepository.cs`.
- Implementación:
  `Infrastructure/Persistence/MySqlSupplierRepository.cs`.
- Servicio:
  `Application/Suppliers/ISupplierService.cs`.
- Implementación:
  `Application/Suppliers/SupplierService.cs`.

`ISupplierRepository` hereda de:

```csharp
IRepositorioBase<Supplier, SupplierListItem, int>
IConBusqueda
```

`IConBusqueda` vive en `Application/Suppliers/IConBusqueda.cs` y declara
`ExistsNombreEmpresaAsync`.

El listado `SupplierListItem` proviene de `GetAllAsync` del contrato base. La
interfaz segregada `IConListado` está en
`Application/Suppliers/IConListado.cs`; el servicio sí la hereda, pero el
repositorio no la vuelve a declarar porque el método ya está incluido en
`IRepositorioBase`.

`ISupplierService` hereda de:

```csharp
IServicioCRUD<Supplier, int, Supplier, Supplier>
IConListado
IConBusqueda
```

No usa interfaces de catálogos ni de historial de precios.

## 4. Validación de entradas

### Categorías

La entidad y sus atributos están en
`Domain/Categories/Category.cs`:

- `Id`: sin Data Annotation.
- `Name`: `[Display]`, `[Required]`, `[StringLength(150)]` y `[CategoryName]`.
- `Description`: `[Display]`, `[StringLength(255)]` y
  `[CategoryDescription]`.
- `Code`: `[Display]`, `[Required]`, `[StringLength(20)]` y `[CategoryCode]`.
- `AisleLocation`: `[Display]`, `[StringLength(20)]` y `[AisleLocation]`.
- `IsActive`, `AdminUserId`, `CreatedAt` y `UpdatedAt`: sin Data Annotation.

Los atributos personalizados están implementados en
`Domain/Categories/CategoryDtos.cs`:

- `CategoryNameAttribute`: patrón de nombre con letras y caracteres permitidos.
- `CategoryCodeAttribute`: letras, números y guiones.
- `AisleLocationAttribute`: formato `Pasillo 1` hasta `Pasillo 8`.
- `CategoryDescriptionAttribute`: letras, números y puntuación permitida.

Las validaciones de formulario se disparan en:

- `Pages/Categories/Create.cshtml.cs:22`: `if (!ModelState.IsValid)`.
- `Pages/Categories/Edit.cshtml.cs:34`: `if (!ModelState.IsValid)`.
- `Pages/Categories/Index.cshtml.cs:50`: edición desde la página de listado.

### Productos

La entidad y sus atributos están en `Domain/Products/Product.cs`:

- `Id`: sin Data Annotation.
- `Nombre`: `[Display]`, `[Required]`, `[StringLength(150)]` y
  `[ProductText]`.
- `EmpaquePresentacion`: `[Display]`, `[Required]`,
  `[StringLength(100)]` y `[ProductText]`.
- `PrecioVenta`: `[Display]`, `[RegularExpression]` y `[SalePrice]`.
- `PrecioCosto`: `[Display]`, `[RegularExpression]` y `[CostPrice]`.
- `StockMinimo`: `[Display]` y `[Range(0, int.MaxValue)]`.
- `IdCategoria`: `[Range(1, int.MaxValue)]`.
- `IdProveedor`: `[Range(1, int.MaxValue)]`.
- `MotivoCambio` y `EstaActivo`: sin Data Annotation.

Los atributos personalizados están implementados en
`Domain/Products/ProductDtos.cs`:

- `ProductTextAttribute`: rechaza caracteres de control.
- `SalePriceAttribute`: exige un precio mayor que cero con precisión de décimas.
- `CostPriceAttribute`: exige un precio mayor que cero.
- `ProductPriceValidation.DecimalPattern`: patrón de entrada decimal con coma o
  punto y hasta dos decimales, usado por `RegularExpression`.

Las validaciones de formulario se disparan en:

- `Pages/Products/Create.cshtml.cs:22`: `if (!pricesValid || !ModelState.IsValid)`.
- `Pages/Products/Edit.cshtml.cs:33`: `if (!pricesValid || !ModelState.IsValid)`.
- `Pages/Products/Index.cshtml.cs:49`: edición desde el listado.

Los PageModels de Producto también validan explícitamente los precios antes de
esa condición; esa comprobación adicional está en `ProductFormModel.cs` y no
reemplaza el `ModelState` generado por Data Annotations.

### Proveedores

La entidad y sus atributos están en `Domain/Suppliers/Supplier.cs`:

- `Id`: sin Data Annotation.
- `NombreEmpresa`: `[Display]`, `[Required]`, `[StringLength(150)]` y
  `[SupplierText]`.
- `NumeroEmpresa`: `[Display]`, `[Required]` y `[Telefono]`.
- `CorreoReferencia`: `[Display]`, `[EmailAddress]` y `[StringLength(150)]`.
- `EsAutogestionado` y `EstaActivo`: sin Data Annotation.

Los atributos y constantes personalizadas están en
`Domain/Suppliers/SupplierDtos.cs`:

- `SupplierTextAttribute`: rechaza caracteres de control.
- `TelefonoAttribute`: exige entre 6 y 20 dígitos.
- `SupplierValidation`: contiene los patrones y mensajes usados por la entidad,
  incluyendo el mensaje de correo obligatorio para proveedores autogestionados.

`Supplier` implementa `IValidatableObject`. Su método `Validate` agrega un error
cuando `EsAutogestionado` es verdadero y `CorreoReferencia` está vacío.

Las validaciones de formulario se disparan en:

- `Pages/Suppliers/Create.cshtml.cs:14`: `if (!ModelState.IsValid)`.
- `Pages/Suppliers/Edit.cshtml.cs:27`: `if (!ModelState.IsValid)`.
- `Pages/Suppliers/Index.cshtml.cs:43`: creación desde el listado.
- `Pages/Suppliers/Index.cshtml.cs:84`: edición desde el listado, junto con la
  condición `EditSupplier.Id <= 0`.

## 5. `Validations/Validation.cs`

El archivo `Proyecto_Arquitectura_Micromercado/Validations/Validation.cs`
declara la clase estática `Validation` con estos métodos públicos:

- `IsRequired(string? value)`: devuelve `true` si el texto no es nulo, vacío ni
  compuesto solo por espacios.
- `HasValidLength(string? value, int maxLength)`: devuelve `true` si el valor no
  es nulo y su longitud no supera `maxLength`.
- `IsValidEmail(string? email)`: acepta valores vacíos y, cuando hay valor, valida
  una forma básica `texto@dominio.extensión` mediante expresión regular.
- `IsValidPhoneNumber(string? number)`: exige un valor no vacío compuesto por entre
  6 y 20 dígitos.

No se encontró ningún uso de `Validation.IsRequired`,
`Validation.HasValidLength`, `Validation.IsValidEmail` ni
`Validation.IsValidPhoneNumber` en `Application`, `Domain`, `Infrastructure` o
`Pages`. Por tanto, en el estado actual esta clase no participa en los flujos de
validación observados. Las validaciones de `CategoryService` son independientes:
ese servicio usa directamente `Regex.IsMatch` y constantes de
`CategoryValidation`.

## 6. Arranque e inyección de dependencias

En `Proyecto_Arquitectura_Micromercado/Program.cs` se registran estos pares,
todos con ciclo de vida `Scoped`:

```csharp
IProductRepository  -> MySqlProductRepository
IProductService     -> ProductService
ICategoryRepository -> MySqlCategoryRepository
ICategoryService    -> CategoryService
ISupplierRepository -> MySqlSupplierRepository
ISupplierService    -> SupplierService
```

No se registran servicios con `Singleton` ni `Transient` en el archivo actual.

Otras configuraciones relevantes:

- `AddRazorPages()`: habilita Razor Pages.
- `DecimalModelBinderProvider`: se inserta primero para interpretar entradas
  decimales de los formularios; el mensaje de número inválido se configura como
  `"Ingrese un número válido."`.
- `UseRequestLocalization`: establece `es-BO` como cultura y cultura de UI
  predeterminadas y como única cultura soportada.
- `UseExceptionHandler("/Error")`: en entornos que no son Development redirige
  las excepciones no controladas a la página `/Error`.
- `UseHsts()`: en esos mismos entornos activa HSTS.
- `UseHttpsRedirection()`: redirige solicitudes HTTP a HTTPS.
- `UseAuthorization()`: habilita el middleware de autorización; este archivo no
  registra autenticación.

## 7. Flujo real: crear un Producto

El flujo observable en el código es:

```text
Pages/Products/Create.cshtml
        |
        v
Pages/Products/Create.cshtml.cs
OnPostAsync(...)                         [línea 19]
        |
        | valida pricesValid y ModelState.IsValid
        v
IProductService.CreateAsync(Product, cancellationToken)
        |
        v
ProductService.CreateAsync(...)          [Application/Products/ProductService.cs:24]
        |
        | Validate(product), normaliza y redondea precios
        v
IProductRepository.CreateAsync(Product, cancellationToken)
        |
        v
MySqlProductRepository.CreateAsync(...)  [Infrastructure/Persistence/...:154]
        |
        | INSERT INTO PRODUCTO
        | SELECT LAST_INSERT_ID()
        | asigna product.Id y devuelve el ID
        v
MySQL
```

La vista envía el formulario al PageModel. El PageModel comprueba
`ModelState.IsValid` en `Create.cshtml.cs:22` y luego llama a
`ProductService.CreateAsync` a través de la interfaz `IProductService` en
`Create.cshtml.cs:28`. El servicio valida el objeto y delega en
`IProductRepository.CreateAsync`; la implementación concreta ejecuta el `INSERT`
en MySQL, obtiene el ID autogenerado y lo asigna a `product.Id`.

## Archivos revisados

Para escribir esta explicación se revisaron directamente:

- `Program.cs`.
- `Application/Common/IRepositorioBase.cs`.
- `Application/Common/IServicioCRUD.cs`.
- `Application/Categories/ICategoryRepository.cs`,
  `ICategoryService.cs` y `CategoryService.cs`.
- `Application/Products/IProductRepository.cs`, `IProductService.cs`,
  `IConCatalogo.cs`, `IConHistorialPrecios.cs`, `IConRegistroHistorial.cs`,
  `IConListado.cs` y `ProductService.cs`.
- `Application/Suppliers/ISupplierRepository.cs`, `ISupplierService.cs`,
  `IConBusqueda.cs`, `IConListado.cs` y `SupplierService.cs`.
- `Domain/Categories/Category.cs` y `CategoryDtos.cs`.
- `Domain/Products/Product.cs`, `ProductDtos.cs` y `ProductPriceHistory.cs`.
- `Domain/Suppliers/Supplier.cs` y `SupplierDtos.cs`.
- `Infrastructure/Persistence/MySqlCategoryRepository.cs`,
  `MySqlProductRepository.cs` y `MySqlSupplierRepository.cs`.
- `Infrastructure/Web/DecimalModelBinder.cs`.
- `Validations/Validation.cs`.
- `Pages/Categories/Create.cshtml.cs`, `Edit.cshtml.cs` e `Index.cshtml.cs`.
- `Pages/Products/Create.cshtml.cs`, `Edit.cshtml.cs`, `Index.cshtml.cs`,
  `ProductFormModel.cs` e `History.cshtml.cs`.
- `Pages/Suppliers/Create.cshtml.cs`, `Edit.cshtml.cs`, `Index.cshtml.cs`,
  `SupplierFormModel.cs` y `Delete.cshtml.cs`.
- `README.md`, para contrastar la descripción arquitectónica existente con el
  código.
