# 🛒 PuntoMarket - Sistema de Gestión para Micromercado

## 📋 Descripción del Proyecto

PuntoMarket es una aplicación web para la gestión operativa de un micromercado. Centraliza la administración de productos, inventario, categorías, proveedores, precios y operaciones relacionadas con el control de caja.

El sistema está desarrollado con ASP.NET Core Razor Pages sobre .NET 10 y utiliza una arquitectura monolítica modular en N-Capas. La separación entre Presentation, Application, Domain e Infrastructure permite mantener el proyecto organizado, facilitar el trabajo colaborativo y ampliar funcionalidades sin mezclar la interfaz, las reglas de negocio y el acceso a datos.

## 🛠️ Tecnologías Utilizadas

| Área | Tecnología | Aplicación |
|---|---|---|
| Backend | .NET 10 / C# | Plataforma, lenguaje y lógica de servidor. |
| Framework web | ASP.NET Core Razor Pages | Vistas Razor, PageModels y handlers HTTP. |
| Base de datos | MySQL | Persistencia relacional del micromercado. |
| Acceso a datos | `MySql.Data` 26.7.0 y ADO.NET | Conexiones, comandos SQL, lectores y transacciones. |
| Frontend | HTML5, Razor, CSS3 | Formularios, tablas, modales y estilos. |
| Componentes visuales | Bootstrap | Grid, botones, alertas, modales y navegación responsive. |
| Interactividad | JavaScript y jQuery | Llenado de modales, eventos y validaciones de interfaz. |
| Validación | Data Annotations | Reglas declarativas en entidades y DTOs. |
| Validación cliente | jQuery Validation Unobtrusive | Validación generada desde los modelos Razor. |
| Valores decimales | `DecimalModelBinder` | Soporte de coma y punto decimal. |
| Cultura | `es-BO` | Formatos regionales de números y mensajes. |

La configuración del proyecto se encuentra en [`Proyecto_Arquitectura_Micromercado.csproj`](./Proyecto_Arquitectura_Micromercado/Proyecto_Arquitectura_Micromercado.csproj), donde se define `net10.0`, `Nullable`, `ImplicitUsings` y la dependencia `MySql.Data`.

## 📁 Estructura del Repositorio

El repositorio se organiza de la siguiente manera:

```text
Proyecto_Arquitectura_Micromercado - copia/
├── README.md
├── doc/
│   └── .gitkeep
├── bdMicroMercadoArqui.sql
├── Proyecto_Arquitectura_Micromercado.slnx
└── Proyecto_Arquitectura_Micromercado/
    ├── Application/
    │   ├── Categories/
    │   ├── Products/
    │   └── Suppliers/
    ├── Domain/
    │   ├── Categories/
    │   ├── Products/
    │   └── Suppliers/
    ├── Infrastructure/
    │   ├── Persistence/
    │   └── Web/
    ├── Pages/
    │   ├── Categories/
    │   ├── Products/
    │   ├── Suppliers/
    │   │   ├── Index.cshtml
    │   │   └── Index.cshtml.cs
    │   └── Shared/
    ├── Properties/
    ├── Validations/
    ├── wwwroot/
    ├── Program.cs
    ├── appsettings.json
    └── Proyecto_Arquitectura_Micromercado.csproj
```

### Responsabilidad de las carpetas

- `Pages/`: capa de presentación. Contiene las vistas `.cshtml`, sus PageModels y los handlers de las solicitudes.
- `Pages/Products/`: catálogo, inventario, formularios de productos e historial de precios.
- `Pages/Categories/`: administración de categorías.
- `Pages/Suppliers/`: administración de proveedores.
- `Pages/Shared/`: layout, navbar y recursos Razor compartidos.
- `Application/`: contratos y servicios de aplicación para Productos, Categorías y Proveedores.
- `Domain/`: entidades del negocio, DTOs y validadores específicos.
- `Infrastructure/Persistence/`: repositorios concretos que ejecutan SQL contra MySQL.
- `Infrastructure/Web/`: adaptadores web, incluido `DecimalModelBinder`.
- `Validations/`: espacio reservado para validaciones transversales de la solución.
- `wwwroot/`: CSS, JavaScript, imágenes y demás recursos estáticos.
- `doc/`: carpeta destinada al informe y documentación del proyecto.

### CRUD centralizado de Proveedores

El CRUD de Proveedores está centralizado en [`Pages/Suppliers/Index.cshtml`](./Proyecto_Arquitectura_Micromercado/Pages/Suppliers/Index.cshtml) y [`Pages/Suppliers/Index.cshtml.cs`](./Proyecto_Arquitectura_Micromercado/Pages/Suppliers/Index.cshtml.cs).

La vista contiene los modales de:

- Crear proveedor.
- Editar proveedor.
- Eliminar proveedor.

Cada modal utiliza `asp-page-handler="Create"`, `asp-page-handler="Edit"` o `asp-page-handler="Delete"`. Los handlers correspondientes reciben los formularios y delegan las operaciones en `ISupplierService`. No se necesitan páginas independientes para estas tres operaciones.

## 🏗️ Arquitectura en N-Capas

La aplicación implementa una arquitectura monolítica modular con cuatro capas lógicas:

### Presentation

Está representada por `Pages/`. Las vistas Razor muestran la información y los PageModels coordinan cada solicitud. Esta capa recibe entradas, valida `ModelState`, llama a los servicios y devuelve una página o una redirección.

### Application

Está representada por `Application/`. Sus interfaces (`IProductService`, `ICategoryService` e `ISupplierService`) definen los casos de uso. Las implementaciones normalizan datos, aplican reglas de negocio y delegan la persistencia en repositorios.

### Domain

Está representada por `Domain/`. Contiene `Product`, `Category`, `Supplier`, `ProductPriceHistory` y los DTOs relacionados. Las entidades expresan propiedades, anotaciones y validaciones propias del negocio sin depender de MySQL ni de Razor Pages.

### Infrastructure

Está representada por `Infrastructure/`. Contiene `MySqlProductRepository`, `MySqlCategoryRepository`, `MySqlSupplierRepository` y los adaptadores técnicos. Esta capa implementa las interfaces de persistencia con ADO.NET y consultas parametrizadas.

### Flujo de una operación

1. El usuario completa un formulario o modal Razor.
2. El PageModel recibe la solicitud mediante un handler `OnPostAsync`.
3. ASP.NET Core ejecuta el model binding y las validaciones.
4. El PageModel invoca un servicio de `Application`.
5. El servicio normaliza los datos y aplica reglas de negocio.
6. El repositorio de `Infrastructure` abre una conexión MySQL y ejecuta SQL parametrizado.
7. El resultado vuelve al PageModel, que muestra un mensaje o redirige a la página correspondiente.

Las dependencias se registran en [`Program.cs`](./Proyecto_Arquitectura_Micromercado/Program.cs):

```csharp
builder.Services.AddScoped<IProductRepository, MySqlProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<ICategoryRepository, MySqlCategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<ISupplierRepository, MySqlSupplierRepository>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
```

## ✨ Funcionalidades Principales

### Productos e inventario

- Crear, consultar, editar y eliminar lógicamente productos.
- Asociar cada producto con una categoría y un proveedor.
- Gestionar precio de venta, precio de costo y stock mínimo.
- Calcular y mostrar el stock disponible.
- Solicitar un motivo al cambiar precios.
- Registrar automáticamente los cambios en `HISTORIAL_PRECIO`.
- Consultar el historial desde `Pages/Products/History.cshtml`.

### Categorías

- Crear, editar y eliminar lógicamente categorías.
- Validar nombre, código, descripción y ubicación de pasillo.
- Mostrar mensajes de éxito mediante `StatusMessage`.
- Ejecutar las operaciones con servicios y repositorios asíncronos.

### Proveedores

- Crear, editar y eliminar lógicamente proveedores.
- Utilizar formularios modales reutilizables en la vista principal.
- Validar nombre de empresa, teléfono, correo y campos obligatorios.
- Aplicar la regla de correo obligatorio para proveedores autogestionados.
- Prevenir duplicidades mediante reglas de aplicación y consultas parametrizadas.

### Interfaz y experiencia de usuario

- Navbar responsive basada en Bootstrap.
- Estado activo y hover con una paleta consistente:
  - Fondo principal: `#1E293B`.
  - Fondo destacado: `#334155`.
  - Texto base: `rgba(255, 255, 255, 0.75)`.
  - Texto activo: `#FFFFFF`.
  - Naranja principal: `#F59E0B`.
- Alertas de confirmación mediante `TempData`.
- Soporte de formatos decimales de la cultura `es-BO`.
- Protección antiforgery en formularios POST.

## 🗄️ Base de Datos

El script [`bdMicroMercadoArqui.sql`](./bdMicroMercadoArqui.sql) contiene la creación de la base de datos, tablas, relaciones, vistas y datos iniciales del sistema.

Entre las estructuras principales se encuentran:

- `PRODUCTO`.
- `CATEGORIAS`.
- `PROVEEDOR`.
- `HISTORIAL_PRECIO`.
- Tablas relacionadas con usuarios, inventario y control de caja.

Los repositorios de `Infrastructure/Persistence/` utilizan `MySqlConnection`, `MySqlCommand` y `MySqlDataReader`. Los valores recibidos desde formularios se envían como parámetros (`@id`, `@nombre`, `@codigo`, `@idUsuarioAdmin`, etc.), evitando concatenar entradas del usuario en las sentencias SQL y reduciendo el riesgo de inyección SQL.

La actualización de precios de productos utiliza una transacción SQL mediante `BeginTransactionAsync`. El flujo bloquea el registro actual, actualiza el precio, inserta el historial y confirma con `CommitAsync`. Si ocurre un error, se ejecuta `RollbackAsync`, evitando que el producto quede actualizado sin su correspondiente registro de auditoría.

## ⚙️ Configuración de la Base de Datos

### 1. Crear la base de datos

Inicie MySQL mediante MySQL Workbench, XAMPP u otra herramienta compatible. Después, ejecute el archivo:

```text
bdMicroMercadoArqui.sql
```

El script debe ejecutarse antes de iniciar la aplicación para que exista la base de datos `bdMicroMercadoArqui` y sus tablas.

### 2. Configurar `appsettings.json`

Edite [`appsettings.json`](./Proyecto_Arquitectura_Micromercado/appsettings.json) y reemplace los valores de la cadena `MySqlConnection`:

```json
{
  "ConnectionStrings": {
    "MySqlConnection": "Server=localhost;Port=3306;Database=bdMicroMercadoArqui;Uid=root;Pwd=SU_PASSWORD;"
  }
}
```

Los campos que deben revisarse o reemplazarse son:

| Campo | Descripción |
|---|---|
| `Server` | Host del servidor MySQL, por ejemplo `localhost` o una dirección IP. |
| `Port` | Puerto del servidor MySQL, normalmente `3306`. |
| `Database` | Nombre de la base creada por el script, normalmente `bdMicroMercadoArqui`. |
| `Uid` | Usuario de MySQL. |
| `Pwd` | Contraseña del usuario de MySQL. |

No incluya contraseñas reales en commits o documentación pública. En entornos compartidos se recomienda utilizar variables de entorno, User Secrets o un gestor de secretos.

## 🚀 Instalación y Ejecución Local

### Requisitos previos

- .NET 10 SDK.
- MySQL Server.
- MySQL Workbench, XAMPP o cliente equivalente.
- Git.

Verifique el SDK:

```bash
dotnet --version
```

### Clonar el proyecto

```bash
git clone <URL_DEL_REPOSITORIO>
cd "Proyecto_Arquitectura_Micromercado - copia"
```

### Restaurar dependencias

```bash
dotnet restore
```

### Compilar la solución

```bash
dotnet build
```

### Ejecutar la aplicación

```bash
dotnet run --project Proyecto_Arquitectura_Micromercado
```

Las URLs pueden consultarse en `Proyecto_Arquitectura_Micromercado/Properties/launchSettings.json` o en la salida de la consola. La configuración utilizada por el proyecto contempla normalmente:

- `http://localhost:5222`
- `https://localhost:7205`

La aplicación debe ejecutarse con el servidor MySQL activo y con una cadena de conexión válida.

## ✅ Validaciones y buenas prácticas

El sistema valida los datos en dos niveles:

### Frontend

- `asp-for` para generar nombres e identificadores coherentes.
- Atributos de Data Annotations reflejados en HTML.
- jQuery Validation Unobtrusive.
- Reglas JavaScript para interacciones de modales y motivos de cambio de precio.
- Mensajes de validación visibles en formularios.

### Backend

- `ModelState.IsValid` en los handlers.
- `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]` y validadores personalizados.
- Normalización de espacios y datos antes de persistir.
- Verificación de duplicidades.
- Eliminación lógica mediante estados activos.
- Consultas SQL parametrizadas.
- `CancellationToken` en operaciones asíncronas.

La validación del servidor es la autoridad final y no depende exclusivamente de las restricciones del navegador.

## 🔐 Seguridad y consistencia

- Uso de parámetros SQL para mitigar inyección SQL.
- Uso de `@Html.AntiForgeryToken()` en formularios de modificación.
- Separación de responsabilidades mediante interfaces e inyección de dependencias.
- Eliminación lógica para conservar trazabilidad.
- Transacciones para mantener sincronizados los precios y `HISTORIAL_PRECIO`.
- No publicación de credenciales reales.

## 📚 Documentación adicional

- [`doc/`](./doc/): carpeta destinada al informe y documentación técnica del proyecto.
- [`bdMicroMercadoArqui.sql`](./bdMicroMercadoArqui.sql): script de creación y datos iniciales de la base de datos.

## 🎓 Justificación de la arquitectura

La arquitectura en N-Capas permite mantener una aplicación monolítica fácil de ejecutar y, al mismo tiempo, separar la presentación, los casos de uso, el dominio y la persistencia. Esta decisión es adecuada para un sistema de gestión de micromercado porque evita la complejidad operativa de los microservicios cuando el alcance del sistema todavía puede resolverse dentro de una única aplicación.

La estructura favorece el trabajo colaborativo: cada equipo puede trabajar en una funcionalidad o capa concreta, las interfaces reducen el acoplamiento y los repositorios aíslan las consultas SQL. Además, el uso de operaciones asíncronas evita bloquear los hilos de la aplicación durante el acceso a MySQL.

## 📄 Alcance académico

PuntoMarket se presenta como un proyecto académico de arquitectura de software, desarrollo web y persistencia de datos. Para un despliegue productivo deberían añadirse o reforzarse autenticación, autorización por roles, gestión segura de secretos, auditoría de usuarios, observabilidad, copias de seguridad y políticas de disponibilidad de MySQL.
