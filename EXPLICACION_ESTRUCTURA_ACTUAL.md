# Explicacion de la estructura actual

Este documento describe la implementacion vigente de PuntoMarket. Las rutas son
relativas a la raiz del repositorio:
`Proyecto_Arquitectura_Micromercado - copia/`.

La solucion es una aplicacion ASP.NET Core Razor Pages sobre .NET 10. Todas las
capas viven dentro del proyecto `Proyecto_Arquitectura_Micromercado/`; no existen
proyectos separados para Domain, Application e Infrastructure.

## 1. Estructura de primer nivel

### `Proyecto_Arquitectura_Micromercado/Domain/`

Contiene los objetos y reglas del negocio:

- `Categories/`: entidad `Category`, validadores y proyecciones de categoria.
- `Products/`: `Product`, `ProductListItem`, historial de precios, validadores y
  excepciones de negocio.
- `Suppliers/`: `Supplier`, `SupplierListItem`, validadores y excepciones.
- `Sales/`: `Venta` y `DetalleVenta`.
- `Customers/`: los datos de cliente se manejan como DTOs de consulta y alta
  dentro de Application; no hay una entidad de cliente agregada en Domain.
- `Common/`: `PagedResult<T>` e informacion comun de paginacion.

El dominio no conoce conexiones, SQL, PageModels ni vistas. Las entidades
existentes de categorias, productos y proveedores aun contienen Data Annotations
porque los formularios actuales las utilizan directamente. El modulo de ventas
introduce DTOs propios en Application para no enviar totales ni precios
controlados por el navegador.

### `Proyecto_Arquitectura_Micromercado/Application/`

Define los casos de uso, contratos y servicios:

- `Common/`: `IRepositorioBase`, `IServicioCRUD`, `IUnidadDeTrabajo` y errores
  de persistencia.
- `Categories/`, `Products/` y `Suppliers/`: servicios, contratos segregados,
  DTOs y operaciones de catalogo.
- `Customers/`: consulta de clientes activos, alta minima desde ventas,
  validacion de CI/NIT duplicado y sus excepciones/DTOs.
- `Sales/`: `IVentaRepository`, `IVentaFacade`, `VentaFacade`, `IVentaService` y
  los DTOs `NuevaVentaDto`, `LineaVentaDto` y `ResultadoVentaDto`.
- `Comprobantes/`: servicio, contratos, DTOs de lectura y contrato del generador.
- `Security/`: autenticacion, usuario actual, credenciales, claims y roles.
- `Facades/`: contratos de fachadas para venta, anulacion y actualizacion de
  stock definidos para los casos de uso del dominio.

Los repositorios se declaran aqui, pero sus implementaciones concretas estan en
Infrastructure.

### `Proyecto_Arquitectura_Micromercado/Infrastructure/`

Contiene los adaptadores externos y la composicion tecnica:

- `Persistence/`: repositorios MySQL, `MySqlUnidadDeTrabajo`, ejecucion de
  consultas paginadas y traduccion de errores.
- `Persistence/EnMemoria/`: repositorios y Unidad de Trabajo para pruebas.
- `Persistence/Errores/`: decoradores que convierten errores de persistencia a
  excepciones de Application.
- `Factories/`: Factory Method para elegir el repositorio de cada agregado.
- `Database/`: singleton `DatabaseConnection`.
- `Comprobantes/`: generador HTML de comprobantes.
- `Security/`: hash BCrypt y repositorio MySQL de usuarios.
- `Web/`: model binder decimal, metadatos de campos no enlazables y adaptador
  `IUsuarioActual`.

### `Proyecto_Arquitectura_Micromercado/Pages/`

Es la capa de presentacion Razor Pages:

- `Categories/Index`: listado paginado, alta, edicion, baja logica, verificacion
  de nombre y vista previa de codigo.
- `Products/Index`: listado paginado, alta, edicion, baja logica y verificacion
  de nombre/empaque.
- `Products/History`: historial paginado de precios.
- `Products/Catalog`: catalogo paginado de solo lectura para Administrador y
  Cajero, con busqueda y disponibilidad.
- `Customers/Create`: alta minima de clientes con CI/NIT y razon social.
- `Suppliers/Index`: listado paginado, alta, edicion, baja logica y verificacion
  de empresa.
- `Sales/Index`: consulta de ventas, registro y anulacion.
- `Comprobante/Index`: consulta y renderizado del comprobante de una venta.
- `Login`, `Logout` y `AccesoDenegado`: autenticacion y respuestas de acceso.
- `Shared/`: layout, paginador, buscador, datos de sesion y URL de comprobante.

Los PageModels reciben dependencias por constructor. No crean repositorios
concretos ni ejecutan SQL.

### `wwwroot/`

Contiene recursos publicos: `css/site.css`, `js/site.js`, `js/ventas.js`,
imagenes, favicon y librerias frontend. `ventas.js` mantiene la interaccion del
detalle temporal, busqueda, edicion/eliminacion de lineas, confirmacion y envio
del registro de venta.

## 2. Contratos comunes y paginacion

`Application/Common/IRepositorioBase.cs` define operaciones asincronas genericas
para obtener listado, obtener por ID, crear, actualizar y realizar baja logica.
`IServicioCRUD` expresa el mismo ciclo de vida para los servicios.

Los listados grandes usan contratos segregados como `IConListadoPaginado`,
`IConConsultaPorProveedor` e `IConVerificacionDeNombreYEmpaque`. El resultado es
`Domain/Common/PagedResult<T>`, que conserva los elementos de la pagina, pagina
actual, tamano de pagina y total de registros. `PagedSqlRunner` ejecuta la
consulta de datos y la consulta de conteo.

Categorias, productos y proveedores tienen PageModels con paginacion. Las
operaciones de alta, edicion y baja se mantienen en el mismo endpoint
`Index.cshtml.cs` mediante handlers nombrados.

## 3. Servicios y repositorios por modulo

### Categorias

`CategoryService` implementa `ICategoryService` y coordina el repositorio de
categorias. Ademas de CRUD y baja logica, expone el listado paginado, verificacion
de nombres, generacion/preview de codigos y validaciones de categoria.

La implementacion MySQL es `MySqlCategoryRepository`. El PageModel requiere rol
Administrador.

### Productos

`ProductService` implementa `IProductService`. Usa `IProductRepository` y
`IPriceHistoryRepository` para:

- validar y normalizar datos;
- listar productos y filtrar por proveedor;
- verificar duplicados de nombre y empaque;
- actualizar precios y registrar el historial;
- consultar el historial y realizar baja logica.

`ProductListItem` y `LookupOption` son proyecciones para no cargar mas datos de
los necesarios en los listados y catalogos. El repositorio MySQL usa
`MySqlProductRepository` y `MySqlPriceHistoryRepository`.

El mismo servicio expone `GetPagedForSalesAsync`, usado por el catalogo de
consulta y por la busqueda de productos en ventas. Esta operacion devuelve
informacion de solo lectura, incluyendo el stock calculado, y no permite
modificar el catalogo.

### Proveedores

`SupplierService` implementa `ISupplierService`. Valida nombre, telefono,
correo, duplicados y la relacion con productos antes de crear, actualizar o
desactivar. `SupplierFormModel` es el modelo de formulario actual: convierte la
entrada de la pagina a `Supplier` y evita enlazar directamente campos de estado
o auditoria.

`MySqlSupplierRepository` devuelve `SupplierListItem`, soporta paginacion y
verifica si el proveedor tiene productos antes de la baja.

### Clientes

`Application/Customers` separa dos responsabilidades:

- `IClienteConsultaService` y `IClienteConsultaRepository` buscan clientes
  activos por coincidencias de CI/NIT o razon social y verifican si un CI/NIT
  ya existe.
- `IClienteCreacionService` y `IClienteCreacionRepository` registran un cliente
  minimo con CI/NIT y razon social, normalizan los textos y asocian el usuario
  creador mediante `IUsuarioActual`.

`ClienteDuplicadoException` representa el conflicto de un CI/NIT activo
repetido. La pagina `Customers/Create` realiza la verificacion remota antes del
POST y vuelve a validar en el servicio/repositorio para evitar duplicados por
concurrencia. El alta esta disponible para Administrador y Cajero porque forma
parte del flujo de ventas.

## 4. Ventas y stock

El modulo esta dividido en dominio, contratos de Application, repositorio MySQL
y fachada:

```text
Pages/Sales/Index.cshtml
        |
        v
Pages/Sales/Index.cshtml.cs
        |
        | NuevaVentaDto: cliente + efectivo pagado + productos + cantidades
        v
IVentaFacade -> VentaFacade
        |
        | inicia Unidad de Trabajo
        v
IVentaRepository -> MySqlVentaRepository
        |
        | llama procedimientos almacenados y lee resultados
        v
MySQL: VENTA, DETALLE_VENTA, PRODUCTO y CLIENTE
```

La interfaz busca clientes bajo demanda por CI/NIT o razon social y genera el
detalle temporal en el navegador. Antes de registrar, el usuario debe
seleccionar un cliente, ingresar efectivo pagado y agregar al menos una linea
con cantidades enteras mayores que cero. El servidor recibe solamente
`IdCliente`, `EfectivoPagado`, `IdProducto` y `Cantidad`. El total y los precios
se obtienen/calculan en el servidor, por lo que no se confia en valores
enviados por el cliente.

`VentaFacade.RegistrarVentaAsync` rechaza una venta sin lineas, inicia una
transaccion, valida el efectivo, llama a `RegistrarVentaConSpAsync`, confirma si
todo termina bien y revierte ante un error. El procedimiento valida stock,
calcula el total y determina el cambio a devolver. `AnularVentaAsync` usa el
mismo patron y registra la anulacion logica. `MySqlVentaRepository` tambien
expone el listado de ventas y el total vendido durante el dia.

Los procedimientos almacenados de ventas estan definidos en
`bdMicroMercadoArqui.sql`. El script contempla registro de cabecera y detalle,
actualizacion de stock, generacion del numero de comprobante y anulacion con
restauracion de stock. La pagina principal muestra las metricas de venta del
dia.

La busqueda de productos de ventas usa `GetPagedForSalesAsync` con un maximo de
10 resultados por consulta y no almacena resultados en el navegador cuando no
hay termino de busqueda. El script `wwwroot/js/ventas.js` controla la seleccion
del cliente, el catalogo de productos, el detalle temporal, la prevencion de
productos repetidos, la edicion/eliminacion de cantidades y el resumen previo.
Antes del envio se muestra un desglose con subtotal, total, efectivo y cambio;
la confirmacion final devuelve el identificador de la venta para abrir el
comprobante.

La pagina `Products/Catalog` es una vista de solo lectura para consultar
productos disponibles desde el menu principal. Usa el mismo contrato de
paginacion y la misma busqueda orientada a ventas, pero no expone handlers de
creacion, edicion ni baja.

## 5. Comprobantes

El flujo de comprobante es independiente del registro:

1. `Pages/Comprobante/Index.cshtml.cs` recibe `idVenta`.
2. `IComprobanteService` pide a `IComprobanteRepository` la cabecera y el
   detalle.
3. `MySqlComprobanteRepository` consulta `VENTA`, `CLIENTE` y
   `DETALLE_VENTA`.
4. `IGeneradorComprobante` recibe `ComprobanteDto`.
5. `GeneradorComprobanteHtml` genera el HTML que se muestra/imprime.

`Pages/Shared/ComprobanteUrl.cs` centraliza la ruta
`/Comprobante/{idVenta}`. La pantalla permite abrir el comprobante de una venta
registrada y esta protegida para Administrador y Cajero.

El comprobante HTML actual muestra el numero de comprobante, fecha de venta,
cliente, CI/NIT, estado, detalle de productos, cantidades, precios unitarios,
importes, total y total en letras. Tambien incluye la fecha/hora de generacion
y el usuario que lo genero. `ComprobanteDto.EstaAnulada` permite mostrar el
estado de una venta anulada de forma diferenciada.

## 6. Persistencia, Factory Method y Unidad de Trabajo

`Program.cs` selecciona la implementacion MySQL mediante creadores concretos:

```text
CreatorCategoryRepository      -> CreatorCategoryRepositoryMySql
CreatorProductRepository       -> CreatorProductRepositoryMySql
CreatorPriceHistoryRepository  -> CreatorPriceHistoryRepositoryMySql
CreatorSupplierRepository      -> CreatorSupplierRepositoryMySql
CreatorVentaRepository         -> CreatorVentaRepositoryMySql
```

Cada creador expone `ObtenerRepositorio()`. El creador es `Scoped` y memoriza el
repositorio, de modo que una peticion HTTP reutiliza una sola instancia. Para
cambiar el motor de persistencia se cambia el tipo concreto registrado en
`Program.cs`, sin modificar PageModels ni servicios.

La conexion se obtiene desde `ConnectionStrings:MySqlConnection` y se registra
mediante `DatabaseConnection`. `MySqlUnidadDeTrabajo` tambien es `Scoped` y
comparte la conexion entre repositorios durante una transaccion. Se registra
como tipo concreto para los adaptadores MySQL y como `IUnidadDeTrabajo` para
Application.

Para pruebas existen implementaciones en memoria de repositorios y de la Unidad
de Trabajo. El codigo de produccion, en cambio, selecciona actualmente las
implementaciones MySQL.

Los repositorios de clientes, comprobantes y usuarios se registran directamente
como adaptadores MySQL en `Program.cs`; no pasan por los creadores del Factory
Method porque actualmente no tienen una implementacion alternativa seleccionada
por ese patron.

## 7. Autenticacion, sesion y autorizacion

La autenticacion usa cookies de ASP.NET Core:

- `Login` recibe `CredencialesDeUsuario` y delega en `IAutenticacionService`.
- `MySqlUsuarioRepository` obtiene el usuario y el hash.
- `BCryptPasswordHasher` verifica la contrasena.
- `PrincipalDeSesion` crea los claims de ID, nombre de usuario y rol.
- `Logout` cierra la sesion.
- `UsuarioActualDesdeClaims` adapta los claims a `IUsuarioActual`.

La cookie expira a los 30 minutos, tiene renovacion deslizante y es `HttpOnly`.
La sesion de ASP.NET Core tambien tiene un timeout de 30 minutos.

Los roles se centralizan en `Application/Security/Roles.cs`:

- `Administrador`: categorias, productos, proveedores, historial y funciones de
  administracion.
- `AdministradorYCajero`: ventas, consulta del catalogo, alta minima de
  clientes, comprobantes y operaciones permitidas para ambos perfiles.

`Index` y `Privacy` requieren usuario autenticado. Las paginas de administracion
de categorias, productos, proveedores y el historial administrativo exigen
Administrador. `Products/Catalog`,
`Customers/Create`, `Sales/Index` y `Comprobante/Index` permiten
Administrador y Cajero. `Login`, `Logout` y `AccesoDenegado` permiten acceso
anonimo cuando corresponde.

`CamposNoEnlazablesProvider` evita que el navegador imponga campos de estado y
auditoria como `EstaActivo`, `AdminUserId`, fechas o usuario de modificacion.

## 8. Validacion de datos

Las entidades de categorias, productos y proveedores conservan validaciones
de formulario mediante Data Annotations y atributos propios:

- Categoria: nombre, codigo, descripcion y ubicacion de pasillo.
- Producto: texto, precios, stock minimo, categoria, proveedor y relacion entre
  precio de venta y costo.
- Proveedor: nombre, telefono, correo y correo obligatorio cuando es
  autogestionado.
- Cliente: CI/NIT obligatorio con longitud maxima de 30 caracteres y razon
  social obligatoria.
- Venta: cliente, efectivo pagado, al menos una linea y cantidades mayores que
  cero.

Los PageModels comprueban `ModelState.IsValid` antes de llamar al servicio.
`ProductFormModel` valida adicionalmente precios y motivo del cambio.
`SupplierFormModel` concentra el binding del formulario de proveedores.
`SaleInputModel` contiene los campos exclusivos de la pantalla de ventas y no
representa una venta persistida. El POST de ventas valida nuevamente el cuerpo
JSON antes de delegar a `VentaFacade`.

La creacion de clientes devuelve errores de validacion, duplicidad y
persistencia en la propia pagina. Las busquedas y verificaciones remotas
deshabilitan la cache de respuesta para reflejar el estado actual de la base.

`DecimalModelBinder` acepta los formatos decimales usados en la cultura
`es-BO`, y `Program.cs` registra el mensaje comun para numeros invalidos.
`Validations/Validation.cs` continua existiendo como utilidad generica, pero no
es el mecanismo principal utilizado por los formularios actuales.

## 9. Arranque y middleware

`Program.cs` configura:

- Razor Pages y `DecimalModelBinderProvider`;
- metadatos para bloquear campos no enlazables;
- autenticacion por cookies, autorizacion y sesion;
- `IHttpContextAccessor`, servicios de usuario actual y dependencias de cada
  modulo;
- cultura `es-BO` como cultura unica soportada;
- `UseExceptionHandler("/Error")` y HSTS fuera de Development;
- redireccion HTTPS;
- archivos estaticos y mapeo de Razor Pages.

El proyecto usa los paquetes `MySql.Data` y `BCrypt.Net-Next`. La cadena de
conexion de ejemplo esta en `appsettings.example.json`. La configuracion local
de desarrollo no debe depender de un `appsettings.Development.json` versionado;
el README documenta el uso de User Secrets para la cadena real.

## 10. Pruebas y dobles de infraestructura

`Proyecto_Arquitectura_Micromercado.Tests/` cubre, entre otros:

- arquitectura y dependencias hexagonales;
- Factory Method y Unidad de Trabajo;
- autenticacion, BCrypt y usuario actual;
- validacion y baja de proveedores;
- servicios de categorias y productos en memoria;
- cambio de precios e historial;
- alta de clientes y validacion de CI/NIT duplicado;
- registro de ventas mediante el handler y validacion del detalle;
- comprobantes, monto en letras y campos no enlazables.

Los repositorios en memoria permiten probar servicios sin MySQL. Los decoradores
de `Persistence/Errores` se encargan de traducir errores del adaptador antes de
exponerlos a Application.

## 11. Flujo resumido de una peticion protegida

```text
Cookie de autenticacion
        |
        v
Authorize / rol requerido
        |
        v
Razor Page + PageModel
        |
        v
Servicio o Fachada de Application
        |
        v
Contrato de repositorio
        |
        v
Factory Method -> repositorio MySQL
        |
        v
Unidad de Trabajo / DatabaseConnection
        |
        v
MySQL
```

Esta es la estructura implementada actualmente. Las carpetas o patrones
descritos en documentos de propuestas anteriores, como una reorganizacion
futura completa por proyectos o un Factory Method aun no aplicado, no forman
parte del estado vigente salvo las clases registradas en `Program.cs`.
