# 🛒 PuntoMarket - Sistema de Gestión para Micromercado

## 📋 Descripción del Proyecto

PuntoMarket es una aplicación web para la gestión operativa de un micromercado. Centraliza la administración de productos, inventario, categorías, proveedores, precios y operaciones relacionadas con el control de caja.

El sistema está desarrollado con ASP.NET Core Razor Pages sobre .NET 10 y combina dos patrones de arquitectura: **monolito** y **arquitectura hexagonal**. Es un único proyecto desplegable cuyas carpetas se organizan por **responsabilidad técnica** (presentación, aplicación, dominio e infraestructura), no por módulos de negocio. La regla de dependencia de la hexagonal mantiene las reglas del negocio libres de la interfaz y del acceso a datos.

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
PuntoMarket/
├── README.md
├── doc/
├── bdMicroMercadoArqui.sql
├── Proyecto_Arquitectura_Micromercado.slnx
├── Proyecto_Arquitectura_Micromercado.Tests/
└── Proyecto_Arquitectura_Micromercado/
    ├── Domain/                  entidades, excepciones y reglas puras del negocio
    ├── Application/             casos de uso y puertos
    │   ├── Facades/             las tres fachadas del proceso de venta
    │   ├── Security/            login, IUsuarioActual, IPasswordHasher, roles
    │   └── Common/              IUnidadDeTrabajo, paginación y contratos comunes
    ├── Infrastructure/          adaptadores de salida
    │   ├── Factories/           Factory Method de repositorios
    │   ├── Persistence/         repositorios MySQL, en memoria y decoradores de errores
    │   ├── Database/            DatabaseConnection
    │   ├── Security/            adaptador BCrypt
    │   └── Web/                 adaptadores web (usuario actual, binders)
    ├── Pages/                   adaptador de entrada (Razor Pages)
    ├── wwwroot/
    ├── Program.cs
    ├── appsettings.example.json
    └── Proyecto_Arquitectura_Micromercado.csproj
```

### Responsabilidad de las carpetas

- `Pages/`: adaptador de entrada. Vistas `.cshtml`, PageModels y handlers HTTP. Solo conoce a `Application`.
- `Application/`: casos de uso (servicios y fachadas) y los puertos que el resto del sistema debe implementar.
- `Domain/`: entidades, excepciones de negocio y reglas que no dependen de ninguna tecnología.
- `Infrastructure/`: adaptadores de salida que implementan los puertos de `Application` (MySQL, BCrypt, claims de la cookie).
- `wwwroot/`: CSS, JavaScript, imágenes y demás recursos estáticos.
- `doc/`: informe y documentación del proyecto.

Dentro de cada capa, las subcarpetas (`Products`, `Categories`, `Suppliers`) solo agrupan archivos por el recurso que gestionan; no son módulos independientes. Comparten el mismo ensamblado, el mismo contenedor de dependencias y la misma base de datos.

### CRUD centralizado de Proveedores

El CRUD de Proveedores está centralizado en [`Pages/Suppliers/Index.cshtml`](./Proyecto_Arquitectura_Micromercado/Pages/Suppliers/Index.cshtml) y [`Pages/Suppliers/Index.cshtml.cs`](./Proyecto_Arquitectura_Micromercado/Pages/Suppliers/Index.cshtml.cs).

La vista contiene los modales de:

- Crear proveedor.
- Editar proveedor.
- Eliminar proveedor.

Cada modal utiliza `asp-page-handler="Create"`, `asp-page-handler="Edit"` o `asp-page-handler="Delete"`. Los handlers correspondientes reciben los formularios y delegan las operaciones en `ISupplierService`. No se necesitan páginas independientes para estas tres operaciones.

## 🏗️ Arquitectura: monolito hexagonal por responsabilidad técnica

### Flujo de dependencias

```text
Pages (adaptador de entrada) ──► Application (puertos, servicios, fachadas) ──► Domain
                                        ▲
Infrastructure (adaptadores de salida) ─┘  implementa los puertos de salida
```

- `Pages` depende de `Application` y nunca de `Infrastructure` ni de los repositorios.
- `Application` depende de `Domain` y define los puertos; no conoce a `Infrastructure` ni a `Pages`.
- `Infrastructure` depende de `Application` para implementar sus puertos (`IProductRepository`, `IUnidadDeTrabajo`, `IUsuarioActual`, `IPasswordHasher`).
- `Domain` no depende de nadie.

Esta regla se comprueba automáticamente en `ArquitecturaHexagonalTests`, que revisa los `using` de `Domain`, `Application` e `Infrastructure` y también las vistas y PageModels de `Pages`. Si alguien la rompe, la prueba falla e indica archivo y línea.

### Puertos

| Tipo | Puertos |
|---|---|
| De entrada (casos de uso) | `IProductService`, `ICategoryService`, `ISupplierService`, `IAutenticacionService` y las fachadas |
| De salida (lo que `Infrastructure` implementa) | `IProductRepository`, `ICategoryRepository`, `ISupplierRepository`, `IPriceHistoryRepository`, `IUsuarioRepository`, `IUnidadDeTrabajo`, `IUsuarioActual`, `IPasswordHasher` |

### Fachadas del proceso de venta (Facade)

La carpeta `Application/Facades/` define las tres fachadas acordadas para el proceso principal:

| Fachada | Quién la usa | Rol |
|---|---|---|
| `IFachadaVenta` | Las Razor Pages | Registrar una venta completa de forma atómica. |
| `IFachadaAnulacion` | Las Razor Pages | Anular una venta y devolver el stock. |
| `IFachadaActualizacionStockProducto` | Solo las dos anteriores | Concentrar los cambios de stock; ambas la consumen bajo la misma `IUnidadDeTrabajo`. |

La Page conoce una sola interfaz y llama un método; la coordinación de colaboradores y de la transacción queda dentro de la fachada. Por ahora están definidas solo como estructura: sus implementaciones, sus firmas finales y su registro en `Program.cs` corresponden a la parte transaccional del proyecto.

### Patrones de diseño

- **Facade:** las tres fachadas descritas arriba.
- **Factory Method:** `Infrastructure/Factories/`. `CreatorRepositorio<T>` es el creador abstracto y cada `Creator*Repository*` concreto (MySQL o en memoria) decide qué repositorio construir. `Program.cs` es el único lugar donde se elige el motor de persistencia.
- **Repository:** los puertos `I*Repository` aíslan al negocio del SQL.
- **Decorator:** `Infrastructure/Persistence/Errores/` envuelve cada repositorio MySQL para traducir los errores del driver a `ErrorDePersistenciaException`.
- **Unit of Work:** `IUnidadDeTrabajo` y `MySqlUnidadDeTrabajo` delimitan una transacción compartida por varios repositorios.
- **Singleton:** `DatabaseConnection` guarda la cadena de conexión en una única instancia, creada una sola vez al arrancar. Existe para que la configuración de acceso a la base viva en un solo lugar inmutable. Además se registra en el contenedor de dependencias para que los repositorios la reciban por constructor y se puedan probar con otra cadena.

### Flujo de una operación

1. El usuario completa un formulario o modal Razor.
2. El PageModel recibe la solicitud mediante un handler `OnPostAsync`.
3. ASP.NET Core ejecuta el model binding y las validaciones.
4. El PageModel invoca un servicio o una fachada de `Application`.
5. Application normaliza los datos, aplica las reglas de negocio y llama a los puertos de salida.
6. El adaptador de `Infrastructure` abre una conexión MySQL y ejecuta SQL parametrizado.
7. El resultado vuelve al PageModel, que muestra un mensaje o redirige.

Las dependencias se registran en [`Program.cs`](./Proyecto_Arquitectura_Micromercado/Program.cs). Por ejemplo, el repositorio de productos se resuelve a través de su creador:

```csharp
builder.Services.AddScoped<CreatorProductRepository, CreatorProductRepositoryMySql>();
builder.Services.AddScoped<IProductRepository>(sp =>
    sp.GetRequiredService<CreatorProductRepository>().ObtenerRepositorio());
builder.Services.AddScoped<IProductService, ProductService>();
```

### Nota sobre el stock

El stock no es una columna de `PRODUCTO`: se lleva por lote en `LOTE.cantidadDisponible`, con salida FEFO (primero en vencer, primero en salir). El stock de un producto es la suma de sus lotes vigentes y se consulta con la vista `vw_productos_con_stock`. Por eso la venta descuenta de `LOTE` y la anulación restaura en `LOTE`. Esta decisión fue aprobada por el magister.

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

El archivo `appsettings.json` no se versiona porque contiene la contraseña local. Copie la plantilla [`appsettings.example.json`](./Proyecto_Arquitectura_Micromercado/appsettings.example.json) como `appsettings.json` en la misma carpeta y reemplace los valores de la cadena `MySqlConnection`:

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

### 3. Usuarios de prueba

El script crea dos usuarios con contraseñas protegidas con BCrypt. Estas credenciales son solo para desarrollo y pruebas; cámbielas antes de cualquier uso real.

| Usuario | Contraseña | Rol | Acceso |
|---|---|---|---|
| `admin` | `admin123` | Administrador | Todo el sistema: inventario, categorías, proveedores y caja. |
| `cajero1` | `cajero123` | Cajero | Solo caja y ventas: registro, historial y comprobante. |

La sesión se cierra tras 30 minutos sin actividad. La identidad del usuario (id, nombre de usuario y rol) viaja en los claims de la cookie de autenticación y es la única fuente de los datos de auditoría; la `Session` solo guarda datos de conveniencia, como el nombre para mostrar y la hora de inicio.

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

- Contraseñas protegidas con BCrypt (factor de trabajo 11) mediante el puerto `IPasswordHasher`.
- Autenticación por cookie con dos roles (Administrador y Cajero) y autorización por `[Authorize(Roles = ...)]`.
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

El monolito hexagonal permite mantener una aplicación fácil de ejecutar y, al mismo tiempo, separar por responsabilidad técnica la presentación, los casos de uso, el dominio y la persistencia, con la dependencia siempre apuntando hacia el núcleo. Esta decisión es adecuada para un sistema de gestión de micromercado porque evita la complejidad operativa de los microservicios cuando el alcance del sistema todavía puede resolverse dentro de una única aplicación.

La estructura favorece el trabajo colaborativo: cada equipo puede trabajar en una funcionalidad o capa concreta, las interfaces reducen el acoplamiento y los repositorios aíslan las consultas SQL. Además, el uso de operaciones asíncronas evita bloquear los hilos de la aplicación durante el acceso a MySQL.

## 📄 Alcance académico

PuntoMarket se presenta como un proyecto académico de arquitectura de software, desarrollo web y persistencia de datos. Para un despliegue productivo deberían añadirse o reforzarse autenticación, autorización por roles, gestión segura de secretos, auditoría de usuarios, observabilidad, copias de seguridad y políticas de disponibilidad de MySQL.
