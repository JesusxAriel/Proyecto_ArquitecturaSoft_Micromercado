# Estructura y arquitectura del Micromercado

## 1. Alcance y propósito

El proyecto `Proyecto_Arquitectura_Micromercado` es una aplicación web ASP.NET Core Razor Pages para administrar productos de un micromercado. El alcance implementado comprende:

- Consulta de productos activos con stock calculado.
- Alta, edición y eliminación lógica de productos.
- Carga de categorías y proveedores para los formularios.
- Validación y normalización de datos de producto.
- Registro y consulta del historial de cambios de precios.
- Persistencia en MySQL mediante ADO.NET puro (`MySqlConnection`, `MySqlCommand` y `MySqlDataReader`).

La solución y el script de base de datos están en la raíz del repositorio:

```text
C:\Users\usuario\Documents\UCB\Arquitectura\Proyecto_Arquitectura_Micromercado - copia\
```

## 2. Árbol completo del proyecto

Se omiten únicamente artefactos generados (`bin/`, `obj/`) y dependencias estáticas de terceros bajo `wwwroot/lib/`.

```text
Proyecto_Arquitectura_Micromercado - copia/
├── ESTRUCTURA_Y_ARQUITECTURA.md
├── Proyecto_Arquitectura_Micromercado.slnx
├── bdMicroMercadoArqui.sql
└── Proyecto_Arquitectura_Micromercado/
    ├── Proyecto_Arquitectura_Micromercado.csproj
    ├── Proyecto_Arquitectura_Micromercado.csproj.user
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Application/
    │   └── Products/
    │       ├── IProductRepository.cs
    │       ├── IProductService.cs
    │       └── ProductService.cs
    ├── Domain/
    │   └── Products/
    │       ├── Product.cs
    │       ├── ProductDtos.cs
    │       └── ProductPriceHistory.cs
    ├── Infrastructure/
    │   ├── Persistence/
    │   │   └── MySqlProductRepository.cs
    │   └── Web/
    │       └── DecimalModelBinder.cs
    ├── Pages/
    │   ├── _ViewImports.cshtml
    │   ├── _ViewStart.cshtml
    │   ├── Index.cshtml
    │   ├── Index.cshtml.cs
    │   ├── Privacy.cshtml
    │   ├── Privacy.cshtml.cs
    │   ├── Error.cshtml
    │   ├── Error.cshtml.cs
    │   ├── Products/
    │   │   ├── Index.cshtml
    │   │   ├── Index.cshtml.cs
    │   │   ├── Create.cshtml
    │   │   ├── Create.cshtml.cs
    │   │   ├── Edit.cshtml
    │   │   ├── Edit.cshtml.cs
    │   │   ├── Delete.cshtml
    │   │   ├── Delete.cshtml.cs
    │   │   ├── History.cshtml
    │   │   ├── History.cshtml.cs
    │   │   ├── ProductFormModel.cs
    │   │   └── _ProductForm.cshtml
    │   └── Shared/
    │       ├── _Layout.cshtml
    │       ├── _Layout.cshtml.css
    │       └── _ValidationScriptsPartial.cshtml
    ├── Properties/
    │   └── launchSettings.json
    └── wwwroot/
        ├── css/site.css
        ├── js/site.js
        └── favicon.ico
```

## 3. Responsabilidad de los archivos

### Raíz y configuración

- `Proyecto_Arquitectura_Micromercado.slnx`: solución que agrupa el proyecto web.
- `Proyecto_Arquitectura_Micromercado/Proyecto_Arquitectura_Micromercado.csproj`: proyecto SDK web dirigido a `net10.0`, con `Nullable` e `ImplicitUsings` habilitados y referencia a `MySql.Data` 26.7.0.
- `bdMicroMercadoArqui.sql`: creación, relaciones, vista y datos iniciales de la base de datos.
- `appsettings.json`: cadena `MySqlConnection`, logging y hosts permitidos. La cadena contiene credenciales locales y no debe publicarse.
- `appsettings.Development.json`: configuración específica del entorno de desarrollo.
- `Properties/launchSettings.json`: perfiles y URLs de ejecución local.

### Composición de la aplicación

- `Program.cs`, namespace global de la aplicación: crea el host, configura Razor Pages, registra `DecimalModelBinderProvider`, establece la cultura `es-BO`, registra las dependencias y configura el pipeline HTTP.
  - `IProductRepository` se enlaza con `MySqlProductRepository`.
  - `IProductService` se enlaza con `ProductService`.
- `Pages/_ViewImports.cshtml`: imports Razor, namespace de páginas y Tag Helpers.
- `Pages/_ViewStart.cshtml`: establece la configuración común de vistas.
- `Pages/Shared/_Layout.cshtml`: layout Bootstrap, navegación y scripts. Incluye enlaces a `/Products/Index` y `/Products/History`.
- `Pages/Shared/_Layout.cshtml.css`: estilos específicos del layout.
- `Pages/Shared/_ValidationScriptsPartial.cshtml`: jQuery Validation, jQuery Unobtrusive Validation y aceptación de punto o coma decimal en cliente.
- `wwwroot/css/site.css` y `wwwroot/js/site.js`: recursos estáticos globales.

### Dominio: `Proyecto_Arquitectura_Micromercado.Domain.Products`

- `Domain/Products/Product.cs`
  - `Product`: entidad editable con `Id`, nombre, presentación, precios, stock mínimo, categoría, proveedor y estado activo.
  - `ProductListItem`: DTO de lectura para la tabla de productos, incluyendo nombres de categoría/proveedor y stock calculado.
  - `LookupOption`: record para opciones de categorías y proveedores.
- `Domain/Products/ProductDtos.cs`
  - `ProductPriceValidation`: patrón decimal y mensajes compartidos.
  - `ProductTextAttribute`: rechaza caracteres de control.
  - `SalePriceAttribute`: exige precio de venta positivo con precisión de décimas.
  - `CostPriceAttribute`: exige precio de costo positivo.
- `Domain/Products/ProductPriceHistory.cs`
  - `ProductPriceHistory`: DTO de auditoría con `Id`, `IdProducto`, `NombreProducto`, precios anterior/nuevo de venta y costo, `MotivoCambio`, `IdUsuario` y `FechaCambio`.
  - No contiene acceso a datos ni lógica de presentación; representa el registro que circula entre aplicación, persistencia y vista.

### Aplicación: `Proyecto_Arquitectura_Micromercado.Application.Products`

- `IProductRepository.cs`: contrato de persistencia:
  - `GetAllAsync`
  - `GetByIdAsync`
  - `GetCategoriesAsync`
  - `GetSuppliersAsync`
  - `GetPriceHistoryAsync`
  - `AddPriceHistoryAsync`
  - `CreateAsync`
  - `UpdateAsync`
  - `SoftDeleteAsync`
- `IProductService.cs`: contrato de casos de uso, incluyendo `GetPriceHistoryAsync`.
- `ProductService.cs`: coordina validación, normalización y delegación al repositorio.
  - `CreateAsync` y `UpdateAsync` llaman a `Validate`.
  - `GetPriceHistoryAsync` delega en `repository.GetPriceHistoryAsync(cancellationToken)`.
  - Normaliza nombre y presentación a Title Case, elimina espacios extra y redondea precios a dos decimales.

### Infraestructura

- `Infrastructure/Persistence/MySqlProductRepository.cs`
  - Implementa `IProductRepository`.
  - Abre conexiones con `OpenConnectionAsync`.
  - Usa consultas SQL explícitas, parámetros y `await using`.
  - `GetAllAsync` consulta `vw_productos_con_stock`.
  - `GetByIdAsync` obtiene un producto activo.
  - `GetCategoriesAsync` y `GetSuppliersAsync` reutilizan `GetLookupAsync`.
  - `CreateAsync` inserta un producto y obtiene `LAST_INSERT_ID()`.
  - `SoftDeleteAsync` cambia `estaActivo` a cero.
  - `GetPriceHistoryAsync` consulta `HISTORIAL_PRECIO` con `INNER JOIN PRODUCTO`, proyectando `p.nombre AS NombreProducto`, y ordena por `h.fechaCambio DESC`.
  - `AddPriceHistoryAsync` ejecuta el `INSERT` parametrizado de auditoría.
  - `UpdateAsync` usa una transacción: bloquea y lee los precios actuales con `FOR UPDATE`, actualiza el producto y, si cambia venta o costo, inserta el historial antes de confirmar.
- `Infrastructure/Web/DecimalModelBinder.cs`
  - `DecimalModelBinder`: acepta formatos con punto o coma, valida hasta dos decimales y convierte usando cultura invariante.
  - `DecimalModelBinderProvider`: aplica el binder a propiedades `decimal`.

### Presentación: `Proyecto_Arquitectura_Micromercado.Pages`

- `Pages/Index.cshtml` y `Index.cshtml.cs`: página inicial.
- `Pages/Privacy.cshtml` y `Privacy.cshtml.cs`: página informativa.
- `Pages/Error.cshtml` y `Error.cshtml.cs`: página de errores de producción.

#### CRUD de productos

- `Pages/Products/Index.cshtml` y `Index.cshtml.cs`
  - Ruta `/Products/Index`.
  - Lista productos activos, precios, proveedores, categorías y stock.
  - Enlaza a crear, editar, eliminar e historial.
- `Pages/Products/Create.cshtml` y `Create.cshtml.cs`
  - Ruta `/Products/Create`.
  - Inicializa entradas de precios y carga catálogos.
  - En POST parsea precios, valida el estado del modelo y llama a `ProductService.CreateAsync`.
- `Pages/Products/Edit.cshtml` y `Edit.cshtml.cs`
  - Ruta `/Products/Edit?id=...`.
  - Carga el producto, rellena `ProductPriceInput`, reutiliza catálogos y llama a `ProductService.UpdateAsync`.
- `Pages/Products/Delete.cshtml` y `Delete.cshtml.cs`
  - Ruta `/Products/Delete?id=...`.
  - Confirma eliminación lógica mediante `SoftDeleteAsync`.
- `Pages/Products/ProductFormModel.cs`
  - Clase abstracta base de Create y Edit.
  - Expone `Product`, `ProductPriceInput`, categorías y proveedores.
  - Centraliza `LoadLookupsAsync`, `TryParsePrices`, `SetPriceInputs` y validación de entradas decimales.
- `Pages/Products/_ProductForm.cshtml`
  - Partial compartida por Create y Edit mediante `<partial name="_ProductForm" model="Model" />`.
  - Contiene una única definición del formulario, anti-forgery token, controles, mensajes de validación y botones.
  - Esta reutilización aplica DRY y evita divergencias entre alta y edición.

#### Historial de auditoría

- `Pages/Products/History.cshtml.cs`
  - `HistoryModel` inyecta `IProductService`.
  - `OnGetAsync` llama a `GetPriceHistoryAsync(cancellationToken)`.
  - Expone `IReadOnlyList<ProductPriceHistory> PriceHistory { get; private set; }`.
- `Pages/Products/History.cshtml`
  - Ruta `/Products/History`.
  - Presenta una tabla Bootstrap con producto, precios anteriores/nuevos, motivo, usuario y fecha.
  - Formatea importes como `Bs. N2`, fechas como `dd/MM/yyyy HH:mm` y costos nulos como `—`.

## 4. Flujo CRUD y flujo de auditoría

### Lectura

1. El navegador solicita `/Products/Index`.
2. `IndexModel.OnGetAsync` recibe `IProductService`.
3. `ProductService.GetAllAsync` delega a `MySqlProductRepository.GetAllAsync`.
4. El repositorio consulta `vw_productos_con_stock` y materializa `ProductListItem`.
5. La vista renderiza la tabla y acciones.

### Alta

1. `CreateModel.OnGetAsync` carga categorías/proveedores y valores iniciales.
2. `_ProductForm.cshtml` envía `Product` y `ProductPriceInput`.
3. `CreateModel.OnPostAsync` ejecuta `TryParsePrices` y verifica `ModelState`.
4. `ProductService.Validate` normaliza y valida.
5. `MySqlProductRepository.CreateAsync` ejecuta un `INSERT` parametrizado.

### Edición y auditoría

1. `EditModel.OnGetAsync` carga el producto activo y muestra sus precios.
2. En POST, `TryParsePrices` acepta punto o coma y obtiene los valores decimales.
3. `ProductService.UpdateAsync` valida y delega.
4. `MySqlProductRepository.UpdateAsync` inicia una transacción y obtiene `precioVenta` y `precioCosto` actuales con `SELECT ... FOR UPDATE`.
5. Ejecuta el `UPDATE PRODUCTO`.
6. Si cambia cualquiera de los precios, crea `ProductPriceHistory` con:
   - `IdProducto`: producto editado.
   - `PrecioVentaAnterior` y `PrecioVentaNuevo`.
   - `PrecioCostoAnterior` y `PrecioCostoNuevo`.
   - `MotivoCambio`: `"Actualización de precio"`.
   - `IdUsuario`: `SystemAdminId`, actualmente `1`.
   - `FechaCambio`: la genera MySQL con `CURRENT_TIMESTAMP`.
7. Inserta el historial en la misma transacción.
8. Confirma únicamente cuando la actualización afecta una fila; de lo contrario revierte.

La transacción evita que el producto quede actualizado sin su auditoría correspondiente o que se registre un cambio cuando la actualización no se realizó.

### Consulta del historial

1. El usuario entra a `/Products/History`.
2. `HistoryModel.OnGetAsync` invoca `IProductService.GetPriceHistoryAsync`.
3. El repositorio ejecuta:

```sql
SELECT h.id, h.idProducto, p.nombre AS NombreProducto,
       h.precioVentaAnterior, h.precioVentaNuevo,
       h.precioCostoAnterior, h.precioCostoNuevo,
       h.motivoCambio, h.idUsuario, h.fechaCambio
FROM HISTORIAL_PRECIO h
INNER JOIN PRODUCTO p ON p.id = h.idProducto
ORDER BY h.fechaCambio DESC;
```

4. `History.cshtml` muestra los registros más recientes primero.

## 5. Base de datos y persistencia

`bdMicroMercadoArqui.sql` define, entre otras, las siguientes estructuras:

- `PRODUCTO`: entidad principal, con precios `DECIMAL(10,2)`, referencias a categoría/proveedor, estado lógico y usuario administrador.
- `CATEGORIAS` y `PROVEEDOR`: fuentes de los catálogos de formularios.
- `LOTE`: existencias que alimentan el stock calculado.
- `HISTORIAL_PRECIO`:

| Campo | Tipo | Propósito |
|---|---|---|
| `id` | `INT AUTO_INCREMENT` | Identificador del evento |
| `idProducto` | `INT` | Producto afectado y FK a `PRODUCTO` |
| `precioVentaAnterior` | `DECIMAL(10,2)` | Valor anterior de venta |
| `precioVentaNuevo` | `DECIMAL(10,2)` | Valor nuevo de venta |
| `precioCostoAnterior` | `DECIMAL(10,2) NULL` | Valor anterior de costo |
| `precioCostoNuevo` | `DECIMAL(10,2) NULL` | Valor nuevo de costo |
| `motivoCambio` | `VARCHAR(255)` | Motivo del cambio |
| `idUsuario` | `INT` | Usuario responsable |
| `fechaCambio` | `DATETIME` | Fecha, por defecto `CURRENT_TIMESTAMP` |

La FK `FK_HistorialPrecio_Producto` usa `ON DELETE CASCADE`. En la aplicación, la operación normal es eliminación lógica de `PRODUCTO`, por lo que el historial permanece consultable.

La vista `vw_productos_con_stock` combina producto, categoría, proveedor y lotes para entregar `stockCalculado`, evitando almacenar stock derivado en `PRODUCTO`.

## 6. Patrones de diseño, SOLID y Clean Code

### SRP

- Las vistas `.cshtml` se encargan de renderizar HTML y formularios.
- Los Code-Behind `.cshtml.cs` coordinan solicitudes HTTP y PageModel.
- `Product` y `ProductPriceHistory` representan datos del dominio.
- `ProductService` aplica reglas de aplicación y delega casos de uso.
- `MySqlProductRepository` encapsula SQL y acceso a MySQL.
- `DecimalModelBinder` resuelve exclusivamente el binding decimal.

### OCP y DIP

`ProductService` depende de `IProductRepository`, no de MySQL directamente. Los PageModels dependen de `IProductService`. `Program.cs` conecta abstracciones con implementaciones mediante:

```csharp
builder.Services.AddScoped<IProductRepository, MySqlProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
```

Esto permite sustituir persistencia o servicio sin modificar las páginas consumidoras.

### DRY

- `_ProductForm.cshtml` evita duplicar el formulario entre Create y Edit.
- `ProductFormModel` concentra la lógica compartida de ambos flujos.
- `ProductPriceInput` separa el texto introducido por el usuario del `decimal` de dominio.
- `ProductPriceValidation`, `ProductTextAttribute`, `SalePriceAttribute` y `CostPriceAttribute` centralizan validaciones.
- `GetLookupAsync` evita duplicar la lectura de categorías y proveedores.
- `AddPriceHistoryParameters` reutiliza el mapeo parametrizado para inserciones de auditoría.

### Clean Architecture y ADO.NET

La dirección de dependencias es:

```text
Pages -> Application -> Domain
Infrastructure -> Application + Domain
Program.cs -> compone todas las implementaciones
```

El dominio no conoce Razor ni MySQL. La infraestructura implementa los contratos de aplicación con ADO.NET puro, consultas parametrizadas y liberación asíncrona mediante `await using`.

## 7. Reglas de negocio y validaciones

### Validación en frontend

`Pages/Shared/_ValidationScriptsPartial.cshtml` habilita jQuery Validation y jQuery Unobtrusive Validation. Sobrescribe el validador numérico para aceptar `10,50` y `10.50`.

`_ProductForm.cshtml` muestra mensajes por campo y envía anti-forgery token.

### Validación de modelo

`Product` usa DataAnnotations:

- Nombre y presentación obligatorios, con longitudes máximas.
- Categoría y proveedor mayores que cero.
- Stock mínimo no negativo.
- Precio de venta con patrón decimal y `SalePriceAttribute`.
- Precio de costo con patrón decimal y `CostPriceAttribute`.
- Texto sin caracteres de control mediante `ProductTextAttribute`.

### Validación de servidor

`ProductFormModel.TryParsePrices`:

- acepta punto o coma decimal;
- rechaza valores vacíos, formatos inválidos y más de dos decimales;
- exige valores positivos;
- exige que el precio de venta sea múltiplo de `0.10`;
- redondea a dos decimales con `MidpointRounding.AwayFromZero`.

`ProductService.Validate`:

- verifica que el producto no sea nulo;
- elimina espacios duplicados y extremos;
- convierte nombre y presentación a Title Case;
- redondea ambos precios;
- exige precio de venta mínimo de `0.10`;
- exige precio de costo positivo;
- valida stock, categoría y proveedor.

### Binding decimal

`DecimalModelBinderProvider` se registra en `Program.cs` con prioridad cero para propiedades `decimal`. `DecimalModelBinder` normaliza la coma a punto y usa `CultureInfo.InvariantCulture`, mientras la aplicación usa `es-BO` para la cultura de solicitudes y presentación.

### Auditoría

Solo se genera una entrada de `HISTORIAL_PRECIO` cuando `precioVenta` o `precioCosto` difieren del valor almacenado. Los cambios de nombre, presentación, stock, categoría o proveedor no generan por sí mismos un evento de precio.

Los valores anterior y nuevo se capturan en el mismo flujo transaccional. El motivo y usuario se asignan actualmente en infraestructura (`"Actualización de precio"` y `SystemAdminId = 1`), y la fecha la asigna MySQL.

## 8. Rutas principales

| Ruta | Página | Operación |
|---|---|---|
| `/` | `Pages/Index.cshtml` | Inicio |
| `/Products/Index` | `Pages/Products/Index.cshtml` | Listar productos |
| `/Products/Create` | `Pages/Products/Create.cshtml` | Crear producto |
| `/Products/Edit?id={id}` | `Pages/Products/Edit.cshtml` | Editar producto |
| `/Products/Delete?id={id}` | `Pages/Products/Delete.cshtml` | Eliminación lógica |
| `/Products/History` | `Pages/Products/History.cshtml` | Consultar auditoría de precios |
| `/Privacy` | `Pages/Privacy.cshtml` | Privacidad |

## 9. Flujo de ejecución

1. `Program.cs` registra servicios y middleware.
2. Razor Pages resuelve un PageModel por solicitud.
3. El PageModel usa `IProductService`.
4. El servicio valida o delega el caso de uso mediante `IProductRepository`.
5. La infraestructura abre `MySqlConnection`, ejecuta SQL parametrizado y materializa DTOs.
6. La página renderiza el resultado mediante el layout y componentes parciales.

Este diseño mantiene aislados dominio, casos de uso, persistencia y presentación, y deja la auditoría integrada en el punto donde efectivamente se modifica el precio.
