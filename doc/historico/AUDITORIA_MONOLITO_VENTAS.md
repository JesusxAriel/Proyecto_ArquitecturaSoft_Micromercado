> HISTORICO: describe el monolito o una etapa retirada. Guia vigente: [README](../../README.md). No ejecutar sus comandos como arranque actual.

# Auditoría técnica del monolito PuntoMarket — Módulo de Ventas

**Fecha de revisión:** 2026-11-04  
**Alcance:** análisis estático del código fuente, pruebas y esquema SQL presentes en el segundo entregable.  
**Resultado:** no se modificó ningún archivo existente, configuración, base de datos, Docker, rama ni commit. Este documento es el único archivo creado durante la auditoría.

> **Criterio de evidencia.** Las afirmaciones se basan en los archivos indicados y sus líneas. Cuando un comportamiento no puede demostrarse con el código inspeccionado se marca como **NO VERIFICADO**.

## 1. Resumen ejecutivo

El módulo actual de Ventas es parte de un monolito ASP.NET Core Razor Pages. La entrada HTTP está en `Pages/Sales`, la orquestación principal está en `Application/Facades`, las reglas puras de ventas y FEFO están en `Domain`, y la persistencia MySQL está en `Infrastructure/Persistence`.

El registro de una venta hace lo siguiente:

1. La página acepta únicamente usuarios con rol `Administrador` o `Cajero`.
2. Busca clientes activos y productos activos mediante endpoints Razor JSON.
3. Envía al servidor el cliente, productos, cantidades y el precio que la pantalla mostró.
4. `FachadaVenta` valida el DTO, verifica el cliente, obtiene nuevamente el precio vigente, descuenta lotes con FEFO, inserta cabecera y detalles, calcula el total y cierra la venta.
5. Todo el registro y descuento se intenta ejecutar dentro de una transacción MySQL explícita.
6. La venta queda asociada al `IUsuarioActual.Id`, obtenido de claims de la cookie autenticada.
7. El comprobante se consulta posteriormente y se genera como HTML en el monolito.

La anulación está restringida en la PageModel y en la interfaz a administradores. La fachada bloquea la venta con `FOR UPDATE`, exige que esté `COMPLETADA`, restaura las cantidades originales por lote y marca la venta como `ANULADA`, guardando motivo, fecha y usuario.

La base técnica no coincide todavía con el objetivo del tercer entregable:

- La autenticación real es cookie authentication, no JWT Bearer.
- Las Pages consumen directamente servicios/fachadas del mismo proceso; no existen APIs HTTP del Servicio 1 verificadas.
- El historial usa `IDistributedCache` configurado con Redis.
- MySQL, caché, Razor Pages y generación HTML están registrados en un único `Program.cs`.
- El dominio y la aplicación ya tienen algunas interfaces y pruebas orientadas a puertos, pero todavía existen dependencias fuertes de infraestructura y de la UI que requieren adaptación.

## 2. Inventario de archivos relacionados

### 2.1 Entrada, registro, historial y anulación

| Archivo | Namespace / tipos | Responsabilidad y dependencias | Usado por |
|---|---|---|---|
| `Proyecto_Arquitectura_Micromercado/Pages/Sales/Index.cshtml` | Razor Page | UI de registro; URLs para buscar productos/clientes, crear cliente, registrar venta y abrir comprobante; token antiforgery. | `Index.cshtml.cs`, `wwwroot/js/ventas.js`. |
| `Proyecto_Arquitectura_Micromercado/Pages/Sales/Index.cshtml.cs` | `Proyecto_Arquitectura_Micromercado.Pages.Sales.IndexModel` | Endpoints `OnPostRegistrarVentaAsync`, `OnPostCrearClienteAsync`, `OnGetBuscarClientesAsync`, `OnGetBuscarProductosAsync`; autorización, validación de entrada, límites de tiempo y traducción de errores a JSON. | Razor runtime y JavaScript de ventas. Inyecta servicios de Application. Líneas 15-264. |
| `Proyecto_Arquitectura_Micromercado/Pages/Sales/SaleInputModel.cs` | `SaleInputModel` | Modelo visual de cliente, búsqueda de producto y cantidad. | `Index.cshtml`. No es el DTO definitivo de registro. |
| `Proyecto_Arquitectura_Micromercado/Pages/Sales/Historial.cshtml` | Razor Page | UI del historial, detalle, comprobante y anulación; expone `data-puede-anular`. | `Historial.cshtml.cs`, `wwwroot/js/historial.js`. |
| `Proyecto_Arquitectura_Micromercado/Pages/Sales/Historial.cshtml.cs` | `HistorialModel`, `AnularVentaInput` | `OnGetHistorialAsync`, `OnGetDetalleAsync`, `OnPostAnularVentaAsync`; exige administrador para anular y administrador/cajero para consultar. | Razor runtime y JavaScript de historial. Líneas 1-137. |
| `Proyecto_Arquitectura_Micromercado/wwwroot/js/ventas.js` | IIFE JavaScript | Búsqueda y selección de cliente/producto, creación de cliente, edición del detalle temporal, cálculo visual del total y POST de registro; abre el comprobante tras éxito. | `Pages/Sales/Index.cshtml`. Referencias relevantes: líneas 76, 169, 502-657. |
| `Proyecto_Arquitectura_Micromercado/wwwroot/js/ventas-reglas.js` | IIFE `VentasReglas` | Reglas de UI, validaciones, reintentos, sesión y paginación usadas por ventas/historial. | `ventas.js`, `historial.js`, pruebas JS. |
| `Proyecto_Arquitectura_Micromercado/wwwroot/js/historial.js` | IIFE JavaScript | Consulta de historial, paginación client-side de 10 filas, detalle, enlace de comprobante y POST de anulación. | `Pages/Sales/Historial.cshtml`. Referencias HTTP: líneas 170, 277 y 416. |

### 2.2 Aplicación: ventas, stock, productos, clientes y comprobantes

| Archivo | Namespace / tipos | Responsabilidad y dependencias | Usado por |
|---|---|---|---|
| `Application/Facades/FachadaVenta.cs` | `FachadaVenta`, `IFachadaVenta` | Caso de uso transaccional de registro; valida cliente, precio, stock, inserta venta/detalles y elimina cache de historial. Depende de `IUnidadDeTrabajo`, `IVentaRepository`, `IConPrecioVigente`, `IFachadaActualizacionStockProducto`, `IUsuarioActual`, reloj, detector de interbloqueos, clientes y consulta de ventas. | `Pages/Sales/Index.cshtml.cs`, pruebas de fachada. |
| `Application/Facades/FachadaAnulacion.cs` | `FachadaAnulacion`, `IFachadaAnulacion` | Caso de uso transaccional de anulación y restauración de lotes. | `Pages/Sales/Historial.cshtml.cs`, pruebas. |
| `Application/Facades/FachadaActualizacionStockProducto.cs` | `FachadaActualizacionStockProducto`, `IFachadaActualizacionStockProducto` | Descuento FEFO, restauración y baja transaccional de lotes vencidos. | Fachadas de venta/anulación y baja automática. |
| `Application/Sales/VentaDtos.cs` | `NuevaVentaDto`, `LineaVentaDto`, `ResultadoVentaDto` | Contratos de entrada/salida del registro. | `IndexModel`, `FachadaVenta`, pruebas. |
| `Application/Sales/IVentaRepository.cs` | Puerto `IVentaRepository` | Contrato de lectura/escritura de ventas y anulación. | Fachadas y adaptador MySQL/en memoria. |
| `Application/Sales/IVentaConsultaRepository.cs` | Puerto y DTOs de consulta | Lista, detalle, filas de detalle y lotes consultados. | `VentaConsultaService`, `MySqlVentaConsultaRepository`. |
| `Application/Sales/IVentaConsultaService.cs` | Puerto `IVentaConsultaService` | Consulta de ventas del día, historial, detalle e invalidación. | Pages y fachadas. |
| `Application/Sales/VentaConsultaService.cs` | `VentaConsultaService` | Historial máximo de 50, cache Redis de 30 segundos, invalidación de ambas variantes y agrupación del detalle por producto/precio. | `HistorialModel`, fachadas, pruebas de cache. Depende directamente de `IDistributedCache`. |
| `Application/Sales/VentaConsultaDtos.cs` | DTOs de resumen/detalle | Forma de salida del historial y detalle. | PageModel y servicio. |
| `Application/Sales/PrecioCambiadoException.cs` | Excepción de negocio | Señala que el precio mostrado al usuario ya no coincide con el vigente. | `IndexModel`, `FachadaVenta`, pruebas. |
| `Application/Stock/ILoteRepository.cs` | Puerto `ILoteRepository` | Consulta, descuento, restauración y baja de lotes. | Fachada de stock y adaptadores. |
| `Application/Stock/IBajaDeLotesService.cs`, `BajaDeLotesService.cs` | Servicio de baja | Expone la baja manual o de sistema de lotes vencidos. | Pages administrativas y hosted service. |
| `Application/Products/IProductService.cs`, `ProductService.cs` | Servicio de productos | Búsqueda para venta (`mínimo 2`, máximo 10), catálogo, precios e historia; también CRUD administrativo. | `IndexModel` usa `BuscarParaVentaAsync`; Pages de productos usan el resto. |
| `Application/Products/IProductRepository.cs` | Puerto compuesto | Incluye `IConPrecioVigente`, `IConBusquedaParaVenta`, catálogo, paginación, relaciones y otras capacidades. | `ProductService`, adaptadores. |
| `Application/Products/BusquedaParaVentaDtos.cs` | `ProductoParaVenta`, `ResultadoBusquedaParaVenta` | Producto, presentación, precio y stock visible en la UI. | Page y JavaScript. |
| `Application/Products/IConPrecioVigente.cs`, `IConBusquedaParaVenta.cs`, `DescripcionDeProducto.cs` | Puertos/DTO | Precio vigente, búsqueda y descripción de producto. | `FachadaVenta`, `ProductService`, repositorio. |
| `Application/Customers/IClienteConsultaService.cs`, `ClienteConsultaService.cs` | Servicio de consulta | Busca clientes activos y verifica existencia activa. | Page de ventas y `FachadaVenta`. |
| `Application/Customers/IClienteConsultaRepository.cs` | Puerto de consulta | `ExisteActivoAsync`, `ExisteNitCiAsync`, `BuscarAsync`. | Servicio y repositorio MySQL. |
| `Application/Customers/IClienteCreacionService.cs`, `ClienteCreacionService.cs` | Servicio de creación | Normaliza, valida DataAnnotations y registra cliente con usuario actual. | Page de ventas. |
| `Application/Customers/IClienteCreacionRepository.cs`, `ClienteCreacionDto.cs`, `ClienteDuplicadoException.cs` | Puerto, DTO y excepción | Registro de cliente y traducción de duplicado activo. | Servicio, Page y repositorio. |
| `Application/Comprobantes/IComprobanteService.cs`, `ComprobanteService.cs` | Servicio de comprobantes | Consulta venta, agrupa líneas por producto/precio y convierte total a letras. | `Pages/Comprobante/Index.cshtml.cs`. |
| `Application/Comprobantes/IComprobanteRepository.cs` | Puerto de lectura | Obtiene la venta y sus líneas para comprobante. | `ComprobanteService`, adaptador MySQL/en memoria. |
| `Application/Comprobantes/IGeneradorComprobante.cs`, `ComprobanteDtos.cs`, `MontoEnLetras.cs` | Puerto, DTOs y conversor | Modelo del comprobante y total escrito en español. | `ComprobanteService`, generador HTML y pruebas. |
| `Application/Common/IUnidadDeTrabajo.cs` | Puerto `IUnidadDeTrabajo` | Iniciar, confirmar y revertir transacción. | Fachadas y adaptador MySQL/en memoria. |
| `Application/Security/IUsuarioActual.cs`, `Roles.cs`, `ClaimsDeIdentidad.cs` | Contexto de usuario y roles | Obtiene id/nombre/rol actual y define `Administrador`, `Cajero`. | Pages, fachadas, repositorios de creación y autenticación. |

### 2.3 Infraestructura y base de datos

| Archivo | Namespace / tipos | Responsabilidad y dependencias | Usado por |
|---|---|---|---|
| `Infrastructure/Persistence/MySqlVentaRepository.cs` | `MySqlVentaRepository` | SQL de cabecera, detalle, cierre, lectura para anulación y marcado de anulación; usa `MySqlCommand`. | `CreatorVentaRepositoryMySql` y fachada. |
| `Infrastructure/Persistence/MySqlVentaConsultaRepository.cs` | `MySqlVentaConsultaRepository` | SQL de historial y detalle con joins a cliente, usuario, producto y lote. | DI como `IVentaConsultaRepository`. |
| `Infrastructure/Persistence/MySqlLoteRepository.cs` | `MySqlLoteRepository` | SQL de lotes; selección con `FOR UPDATE`, decremento/restauración y desactivación. | `CreatorLoteRepositoryMySql`. |
| `Infrastructure/Persistence/MySqlProductRepository.cs` | `MySqlProductRepository` | Búsqueda para venta, precio vigente y CRUD/consultas administrativas de producto. | `CreatorProductRepositoryMySql`, `IConPrecioVigente`. |
| `Infrastructure/Persistence/MySqlClienteConsultaRepository.cs` | `MySqlClienteConsultaRepository` | Clientes activos, existencia y búsqueda con `LIKE`, máximo 20. | DI como consulta de clientes. |
| `Infrastructure/Persistence/MySqlClienteCreacionRepository.cs` | `MySqlClienteCreacionRepository` | Inserta cliente y captura clave duplicada `UQ_Cliente_nitCi_activo`. | DI como creación de clientes. |
| `Infrastructure/Persistence/MySqlComprobanteRepository.cs` | `MySqlComprobanteRepository` | Lee cabecera y detalle para el comprobante. | DI como `IComprobanteRepository`. |
| `Infrastructure/Persistence/MySqlUnidadDeTrabajo.cs` | `MySqlUnidadDeTrabajo` | Mantiene conexión/transacción activa y presta conexiones fuera de transacción. | Todos los repositorios MySQL. |
| `Infrastructure/Database/DatabaseConnection.cs` | Singleton `DatabaseConnection` | Crea conexiones MySQL a partir de configuración. | `Program.cs`, `MySqlUnidadDeTrabajo`. |
| `Infrastructure/Factories/CreatorRepositorio.cs` | `CreatorRepositorio<T>` | Creador perezoso que conserva una instancia de repositorio. | Creadores concretos. |
| `Infrastructure/Factories/CreatorVentaRepository.cs`, `CreatorVentaRepositoryMySql.cs` | Factory de venta | Selecciona repositorio MySQL y decorador de errores. | Registro DI. |
| `Infrastructure/Factories/CreatorLoteRepository.cs`, `CreatorLoteRepositoryMySql.cs` | Factory de lote | Selecciona repositorio MySQL y decorador de errores. | Registro DI. |
| `Infrastructure/Persistence/Errores/RepositoriosConErroresTraducidos.cs` | Decoradores de errores | Traduce errores de persistencia a excepciones de aplicación. | Creadores MySQL y consultas. |
| `Infrastructure/Comprobantes/GeneradorComprobanteHtml.cs` | `GeneradorComprobanteHtml` | Genera HTML completo, escapa texto con `HtmlEncode`, muestra marca de anulada y permite imprimir. | `Pages/Comprobante/Index.cshtml.cs`. |
| `Infrastructure/Web/UsuarioActualDesdeClaims.cs` | `UsuarioActualDesdeClaims` | Implementa `IUsuarioActual` desde `HttpContext.User`. | DI y casos de uso. |
| `Infrastructure/Cache/OpcionesDeRedis.cs` | Opciones Redis | Configura la conexión Redis. | `Program.cs`. |
| `Infrastructure/Background/BajaAutomaticaDeLotesVencidos.cs` | Hosted service | Ejecuta baja automática de lotes vencidos. | DI en `Program.cs`. |
| `Program.cs` | composición del monolito | Registra Razor Pages, cookie auth, Redis, MySQL, servicios, fachadas, repositorios y hosted service. | Arranque de la aplicación. |
| `bdMicroMercadoArqui.sql` | esquema, vistas, triggers y datos | Define `LOTE`, `CLIENTE`, `VENTA`, `DETALLE_VENTA`, restricciones, vistas, triggers y procedimientos de control. | MySQL. |

### 2.4 Pruebas relacionadas

`Proyecto_Arquitectura_Micromercado.Tests` contiene, entre otras, las siguientes pruebas directamente relevantes:

- `FachadaVentaTests.cs`: registro transaccional, orden de productos, retry de interbloqueo, FEFO, stock insuficiente, cliente inválido y precio cambiado.
- `FachadaAnulacionTests.cs`: restauración, doble anulación, venta inexistente, motivo inválido y exceso de cantidad inicial.
- `AsignadorFefoTests.cs`: vencidos, vencimiento actual, sin vencimiento, empate por id, cantidad cero y stock insuficiente.
- `SalesRegistroHandlerTests.cs`: validación del handler, códigos JSON, errores 400/503, cancelación y no exposición de detalles internos.
- `SalesAnulacionHandlerTests.cs`, `SalesBusquedaHandlerTests.cs`, `SalesClienteHandlerTests.cs`, `SalesHistorialHandlerTests.cs`: endpoints Razor de anulación, búsquedas, clientes e historial.
- `VentaConsultaServiceTests.cs`, `HistorialVentasEnCacheTests.cs`: agrupación, historial y cache Redis mediante doble.
- `ComprobanteTests.cs`, `MontoEnLetrasTests.cs`: DTO, agrupación, HTML, escape, venta anulada y total en letras.
- `UnidadDeTrabajoTests.cs`: ciclo de inicio, confirmación, reversión y protección contra doble inicio.
- `InMemoryLoteRepositoryTests.cs`, `StockActualMySqlTests.cs`: comportamiento de lotes y stock.
- `ArquitecturaHexagonalTests.cs`: restricciones estáticas entre Domain, Application, Infrastructure y Pages.
- `FactoryMethodTests.cs`: creadores, instancia compartida y repositorios en memoria/MySQL.
- `ReglasDeVentasJavaScriptTests.cs` y `js/ventas-reglas.test.cjs`: reglas de UI.

No se ejecutaron pruebas ni comandos que cambien datos como parte de esta auditoría; la existencia y alcance anterior se verificó leyendo los archivos.

## 3. Arquitectura actual y mapa de dependencias

### 3.1 Capas observadas

```text
Pages / wwwroot
  -> Application.Facades, Application.Services, DTOs, Security
      -> Domain (Venta, DetalleVenta, AsignadorFefo, LoteDisponible)
      -> Application ports (repositories, UoW, cache, current user)
          <- Infrastructure.Persistence (MySql*)
          <- Infrastructure.Comprobantes
          <- Infrastructure.Web / Cache / Factories
              -> MySql.Data, ASP.NET Core, Redis
```

`Program.cs` compone todos los adaptadores en el mismo proceso. Las Pages no conocen directamente `Infrastructure` ni `MySql.Data`, una regla comprobada por `ArquitecturaHexagonalTests`, pero Application sí contiene una dependencia explícita de `Microsoft.Extensions.Caching.Distributed` en `VentaConsultaService`.

### 3.2 Entidades y reglas de dominio

- `Domain/Sales/Venta.cs`: estado, total, número de comprobante, cliente, usuario, detalles; impide agregar detalles a una venta anulada y completar una venta sin detalles.
- `Domain/Sales/DetalleVenta.cs`: producto, lote, cantidad, precio y subtotal calculado.
- `Domain/Sales/EstadoVenta.cs`: constantes de estado.
- `Domain/Stock/AsignadorFefo.cs`: filtra cantidad positiva y lotes no vencidos, ordena por fecha de vencimiento y luego id de lote, y lanza `StockInsuficienteException`.
- `Domain/Stock/ConsumoDeLote.cs`, `LoteDisponible.cs`, `ResultadoBajaDeLotes.cs`: valores de stock.
- `Domain/Stock/RestauracionExcedeCantidadInicialException.cs`, `StockInsuficienteException.cs`: errores de regla de stock.

### 3.3 Puertos, fachadas y adaptadores

Las fachadas son servicios de aplicación que coordinan varios puertos. `FachadaVenta` y `FachadaAnulacion` no son entidades de dominio: conocen UoW, repositorios, cache y usuario actual. Los repositorios MySQL son adaptadores de salida. Las Razor Pages son adaptadores de entrada.

El acceso a MySQL está concentrado en repositorios y `MySqlUnidadDeTrabajo`, pero el SQL conoce nombres concretos de tablas, vistas, triggers y columnas. El comprobante HTML es un adaptador de presentación en `Infrastructure/Comprobantes`, aunque su consulta es invocada por una Page del monolito.

### 3.4 Barreras para migrar directamente a hexagonal

1. `Program.cs` mezcla composición de UI, cookie authentication, Redis, MySQL, usuarios administrativos, productos administrativos, proveedores, categorías y ventas.
2. `VentaConsultaService` depende de `IDistributedCache`, `ILogger` e `IIdentificadorReplica`; la caché debería quedar detrás de un puerto del Servicio 1 si se conserva.
3. `IProductRepository` es un contrato grande que incluye CRUD administrativo, categorías, proveedores, historial de precios y búsqueda para venta. Para Ventas conviene un puerto mínimo de consulta de precio/producto.
4. `FachadaVenta` y `FachadaAnulacion` dependen de `IUsuarioActual`, cuyo origen actual es `HttpContext` y claims de cookie.
5. Las Pages esperan JSON y redirecciones del mismo sitio y manejan antiforgery/cookie; un frontend separado necesitará contratos HTTP y autenticación JWT.
6. El comprobante es HTML generado dentro del monolito y usa `/images/logo.jpg`; no es todavía un contrato API independiente.
7. Clientes, productos, lotes y ventas comparten la misma unidad de trabajo y conexión. Esto es viable con una base compartida, pero exige definir claramente la propiedad de tablas y los límites transaccionales.

## 4. Flujo completo de registro de venta

### 4.1 Entrada y selección

1. El usuario abre `Pages/Sales/Index.cshtml`; el `IndexModel.OnGet()` no hace consultas.
2. `[Authorize(Roles = Roles.AdministradorYCajero)]` protege la Page.
3. `ventas.js` consulta `OnGetBuscarClientesAsync` tras un debounce. `ClienteConsultaService.BuscarAsync` ignora término vacío y `MySqlClienteConsultaRepository.BuscarAsync` filtra `estaActivo = 1`, busca por `nitCi` o `razonSocial` y devuelve máximo 20.
4. Al seleccionar un cliente, JavaScript guarda su id en `Input_IdCliente` y habilita la búsqueda de productos.
5. `OnGetBuscarProductosAsync` invoca `ProductService.BuscarParaVentaAsync`. El servicio normaliza, exige al menos dos caracteres y limita a 10 resultados.
6. `MySqlProductRepository.BuscarParaVentaAsync` consulta productos activos y devuelve id, nombre, presentación, precio de venta y stock calculado. El SQL completo de ese método no se reprodujo aquí, pero el contrato y el adaptador están verificados.
7. JavaScript agrega líneas al detalle temporal, conserva el precio mostrado y calcula subtotales/total en la interfaz. Ese cálculo visual no sustituye al cálculo server-side.
8. Puede abrirse el modal de nuevo cliente. `OnPostCrearClienteAsync` invoca `ClienteCreacionService.CrearAsync`, que normaliza, valida DataAnnotations y registra con `IUsuarioActual.Id`. El índice único de cliente activo protege duplicados.

### 4.2 Confirmación HTTP

El botón envía a `OnPostRegistrarVentaAsync` un JSON equivalente a:

```json
{
  "idCliente": 4,
  "lineas": [
    { "idProducto": 8, "cantidad": 2, "precioEsperado": 9.5 }
  ]
}
```

El PageModel:

- exige cliente y lista de líneas;
- exige id de producto positivo, cantidad positiva y `PrecioEsperado`;
- devuelve `400` para entrada inválida;
- usa un límite de escritura de 30 segundos mediante `EjecutorDePeticiones`;
- construye `NuevaVentaDto` y `LineaVentaDto`;
- traduce `PrecioCambiadoException` a `400` con código `precio-cambiado`;
- traduce errores de negocio (`InvalidOperationException`, `ArgumentException`, `ValidationException`) a `400`;
- devuelve `503` para fallos inesperados o timeout sin exponer detalles internos.

### 4.3 Secuencia real de llamadas

```text
IndexModel.OnPostRegistrarVentaAsync
  -> FachadaVenta.RegistrarVentaAsync(NuevaVentaDto)
     -> Validar(dto)
     -> IClienteConsultaRepository.ExisteActivoAsync(idCliente)
     -> ordenar Lineas por IdProducto
     -> RegistrarEnUnaTransaccionAsync
        -> IUnidadDeTrabajo.IniciarAsync
        -> IVentaRepository.InsertarCabeceraAsync(idCliente, IUsuarioActual.Id)
        -> por cada LineaVentaDto:
           -> IConPrecioVigente.ObtenerPrecioVigenteAsync
           -> comparar PrecioEsperado
           -> FachadaActualizacionStockProducto.DescontarAsync
              -> ILoteRepository.ObtenerVigentesParaDescuentoAsync
                 -> SELECT ... FOR UPDATE
              -> AsignadorFefo.Asignar
              -> ILoteRepository.DescontarAsync por cada ConsumoDeLote
           -> IVentaRepository.InsertarDetalleAsync por lote consumido
        -> Venta.GenerarNroComprobante(fecha local, idVenta)
        -> IVentaRepository.CerrarVentaAsync(idVenta, nro, total)
        -> IUnidadDeTrabajo.ConfirmarAsync
        -> IVentaConsultaService.InvalidarHistorialAsync
        -> ResultadoVentaDto
  -> JSON { exito, idVenta, total }
  -> ventas.js abre /Comprobante/{idVenta}
```

### 4.4 Cálculos y validaciones

- La fachada exige cliente positivo, al menos una línea y cantidades/productos positivos.
- El cliente se comprueba activo antes de abrir la transacción.
- El precio se consulta dentro de la transacción. Si difiere de `PrecioEsperado`, se aborta con mensaje y precio vigente.
- El total se calcula en servidor sumando `consumo.Cantidad * precio` por cada lote.
- Cada lote consumido produce un detalle `DETALLE_VENTA`; por eso una línea lógica puede producir varios detalles físicos.
- La base calcula `subtotal` como columna generada `cantidad * precioUnitario`.
- El número se forma como `V-yyyymmdd-id` mediante `Venta.GenerarNroComprobante`.
- No se calcula ni persiste efectivo recibido o cambio; la UI los maneja sólo como ayuda opcional.

### 4.5 FEFO y transacción

`AsignadorFefo.Asignar`:

1. descarta lotes con cantidad cero;
2. descarta fechas anteriores a hoy;
3. ordena vencimiento más próximo primero;
4. pone lotes sin vencimiento al final;
5. desempata por `IdLote`;
6. reparte la cantidad y lanza `StockInsuficienteException` si no alcanza.

`MySqlLoteRepository.ObtenerVigentesParaDescuentoAsync` filtra lote activo, cantidad positiva y usa `FOR UPDATE`. Los decrementos son condicionales (`cantidadDisponible >= cantidad`). `FachadaVenta` ordena previamente las líneas por producto para reducir el riesgo de interbloqueos y reintenta la operación completa una vez si `IDetectorDeInterbloqueo` reconoce el error.

La transacción comienza en `IUnidadDeTrabajo.IniciarAsync` antes de insertar la cabecera y termina en `ConfirmarAsync` después de cerrar la venta. Cualquier excepción del bloque provoca `RevertirAsync(CancellationToken.None)`. El cache se invalida después del commit, no forma parte de la transacción MySQL.

### 4.6 Fallos y comprobante

- Cliente inválido: falla antes de iniciar la transacción.
- Precio cambiado, stock insuficiente, error SQL o excepción de negocio: se revierte la transacción.
- Interbloqueo reconocido: se revierte y se reintenta una sola vez.
- Error de cache al invalidar: `VentaConsultaService` lo registra como warning y no convierte el registro confirmado en fallo.
- Tras recibir `idVenta`, el JavaScript abre `/Comprobante/{idVenta}`.
- `Pages/Comprobante/Index.cshtml.cs` exige administrador/cajero, consulta `IComprobanteService`, y `GeneradorComprobanteHtml` devuelve HTML.
- El comprobante muestra el cajero guardado en la venta, no el usuario que abre el comprobante. El texto se escapa con `WebUtility.HtmlEncode`.

## 5. Flujo completo de anulación

1. El usuario entra a `Pages/Sales/Historial.cshtml`; sólo administrador o cajero puede consultar la Page.
2. `HistorialModel.PuedeAnular` es verdadero sólo para `Roles.Administrador`. JavaScript oculta el botón para otros roles y sólo lo muestra para ventas completadas.
3. El POST llega a `OnPostAnularVentaAsync` con `idVenta` y `motivo`.
4. El PageModel vuelve a comprobar administrador, presencia de entrada, `ModelState`, id positivo y motivo requerido/máximo 255 caracteres.
5. `FachadaAnulacion.AnularVentaAsync` valida id y motivo no vacío antes de iniciar transacción.
6. `IVentaRepository.ObtenerParaAnularAsync` ejecuta `SELECT ... FOR UPDATE` y carga los detalles con lote y cantidad.
7. La fachada exige que la venta exista y su estado sea `COMPLETADA`; si no, lanza `"La venta no existe o ya fue anulada."`.
8. `FachadaActualizacionStockProducto.RestaurarAsync` recorre cada `ConsumoDeLote` y llama `ILoteRepository.RestaurarAsync`.
9. MySQL restaura sólo si `cantidadDisponible + cantidad <= cantidadInicial`.
10. `IVentaRepository.MarcarAnuladaAsync` actualiza estado, motivo recortado, usuario actual y `NOW()`.
11. `IUnidadDeTrabajo.ConfirmarAsync` confirma; luego se invalidan las dos claves de historial Redis.
12. Si falla cualquier etapa, se revierte la transacción y el PageModel devuelve `400` para errores de negocio o respuesta genérica de ejecución.

La tabla `VENTA`, sus checks y triggers complementan el caso de uso: una venta anulada no puede modificarse, una venta no se elimina y una anulación exige usuario activo, motivo y fecha. El bloqueo `FOR UPDATE` evita que dos anulaciones concurrentes pasen simultáneamente por la lectura de estado dentro de esta ruta.

### Riesgos de anulación

- `MarcarAnuladaAsync` actualiza por id sin una condición SQL adicional `estado = 'COMPLETADA'`. La protección efectiva depende del bloqueo previo y de que todas las escrituras pasen por el flujo/triggers; una escritura externa podría tener un comportamiento distinto.
- La restauración permite actualizar por `idLote` sin filtrar `estaActivo = 1`. Si un lote inactivo se restaura, el stock podría aumentar en la fila pero no participar en ventas activas. **Riesgo potencial, no demostrado por una prueba de integración.**
- Si el rollback falla, el `catch` puede propagar el error del rollback en lugar del error original. **Riesgo potencial.**
- La invalidación Redis ocurre después del commit y es best-effort; una réplica puede servir historial antiguo hasta 30 segundos si Redis no está disponible. Esto está cubierto como degradación en `HistorialVentasEnCacheTests`.

## 6. Productos, clientes, stock y FEFO

### Productos necesarios para Ventas

Necesarios:

- `ProductService.BuscarParaVentaAsync`.
- `IConBusquedaParaVenta` y `IConPrecioVigente`.
- `MySqlProductRepository.BuscarParaVentaAsync`, `ObtenerPrecioVigenteAsync` y descripción.
- DTOs `ProductoParaVenta`, `ResultadoBusquedaParaVenta`.
- Vista/consulta de stock calculado para mostrar disponibilidad.

Necesarios indirectamente:

- `ILoteRepository` y `MySqlLoteRepository` para existencia física, FEFO, descuento y restauración.
- `AsignadorFefo` y excepciones de stock.
- `IVentaRepository` para persistir lote consumido en cada detalle.

Exclusivamente administrativos o fuera del núcleo mínimo de Ventas:

- `ProductService.CreateAsync`, `UpdateAsync`, `SoftDeleteAsync`.
- Validación de nombre/empaque, categorías, proveedores, historial de precios y `GetPagedAsync`.
- Pages y servicios de categorías, proveedores y CRUD completo de productos.
- `BajaDeLotesService` y `BajaAutomaticaDeLotesVencidos` son operación de inventario; deben decidirse como dependencia compartida o como responsabilidad de otro servicio.

### Clientes necesarios para Ventas

- Consulta: `ClienteConsultaService.BuscarAsync` y `ExisteActivoAsync`.
- Registro durante la venta: `ClienteCreacionService.CrearAsync`.
- Persistencia: `MySqlClienteConsultaRepository` y `MySqlClienteCreacionRepository`.
- La venta guarda `idCliente`; el detalle de venta posterior obtiene NIT/razón social mediante join.

No se verificó un endpoint/API independiente de Usuarios para clientes; actualmente cliente es tabla y funcionalidad del monolito.

## 7. Base de datos y auditoría

El script `bdMicroMercadoArqui.sql` define:

- `LOTE`: cantidad inicial, disponible, vencimiento, activo y usuarios de creación/actualización.
- `CLIENTE`: NIT/CI, razón social, activo, usuario de creación/actualización y unicidad de NIT activo.
- `VENTA`: comprobante, cliente, usuario cajero, fecha, total, estado y metadatos de anulación.
- `DETALLE_VENTA`: producto, lote, cantidad, precio unitario y subtotal generado.

Auditoría de venta:

- Creación: `VENTA.idUsuario` y `fechaVenta`; el usuario viene de `IUsuarioActual.Id`.
- Anulación: `motivoAnulacion`, `idUsuarioAnulacion` y `fechaAnulacion`; la fecha la genera MySQL.
- Cliente nuevo: `idUsuarioCreacion`.
- Lote: `idUsuarioAdmin` y `idUsuarioActualizacion`; el descuento/restauración de venta no actualiza explícitamente esos campos.

Los triggers verifican que:

- una venta nueva sea `COMPLETADA`;
- cliente y usuario estén activos;
- una venta anulada no se modifique ni elimine;
- el detalle sólo se inserte mientras la cabecera tenga comprobante temporal;
- la anulación tenga usuario activo.

El estado temporal se implementa insertando `TMP-UUID()` y actualizando después a `V-yyyymmdd-id`. Si el proceso falla, la transacción revierte también la cabecera temporal.

## 8. Patrones de diseño existentes

### Facade — implementado

`FachadaVenta`, `FachadaAnulacion` y `FachadaActualizacionStockProducto` coordinan múltiples servicios/repositorios y exponen casos de uso de alto nivel. La evidencia es la secuencia transaccional de sus métodos `RegistrarVentaAsync`, `AnularVentaAsync` y `DescontarAsync`; no se clasifican sólo por el nombre.

### Repository — implementado

`IVentaRepository`, `IVentaConsultaRepository`, `ILoteRepository`, `IProductRepository`, `IClienteConsultaRepository`, `IClienteCreacionRepository` e `IComprobanteRepository` son puertos con adaptadores MySQL y dobles en memoria. Los adaptadores ejecutan SQL o almacenan estado de prueba.

### Unit of Work — implementado

`IUnidadDeTrabajo` define inicio/confirmación/reversión. `MySqlUnidadDeTrabajo` conserva una conexión y `MySqlTransaction`, y los repositorios alquilan esa misma transacción cuando está activa. Las fachadas delimitan explícitamente el bloque.

### Factory Method / Factory — implementado

`CreatorRepositorio<TRepositorio>` declara `CrearRepositorio` y memoiza el resultado. `CreatorVentaRepositoryMySql` y `CreatorLoteRepositoryMySql` crean el adaptador MySQL decorado. `FactoryMethodTests` verifica selección, memoización y dobles en memoria.

### Strategy — no verificado como patrón explícito

`AsignadorFefo` encapsula un algoritmo de selección y podría evolucionar a una estrategia, pero es una clase estática única. No existe una interfaz de estrategias intercambiables ni selección polimórfica; por tanto, no se clasifica como Strategy implementado.

### Singleton — implementado, aunque no solicitado

`DatabaseConnection` mantiene una instancia estática protegida por lock. `Program.cs` la registra como singleton lógico y `DatabaseConnectionTests` no fue localizada en el inventario relacionado. Esto introduce estado global de conexión/configuración.

## 9. Dependencias técnicas

Verificadas en `Proyecto_Arquitectura_Micromercado.csproj` y `Program.cs`:

- ASP.NET Core Razor Pages y MVC Model Binding.
- .NET `net10.0`.
- `MySql.Data` para conexiones, comandos y transacciones.
- `Microsoft.Extensions.Caching.StackExchangeRedis` para `IDistributedCache`.
- `BCrypt.Net-Next` para autenticación de usuarios del monolito.
- Cookie authentication con login `/Login`, logout `/Logout` y control de sesión.
- `DataProtection` con persistencia de claves para cookies; el valor de ruta/configuración no se copia en este informe.
- Bootstrap, jQuery y JavaScript del frontend.

No se verificó:

- JWT Bearer en el código actual: `Program.cs` sólo registra `CookieAuthenticationDefaults.AuthenticationScheme`.
- Cliente HTTP para consumir Servicios 1, 2 o 3.
- API controllers de Ventas.
- Redis como requisito de dominio; sólo es usado por el historial.

## 10. Pruebas existentes y cobertura observable

Las pruebas cubren bien el núcleo de reglas:

- FEFO determinista y rechazo de stock insuficiente.
- Registro con varios lotes, reintento de interbloqueo y rollback.
- Anulación y restauración, incluidos casos de doble anulación.
- Validaciones del handler y respuestas HTTP/JSON.
- Cache hit/miss, expiración, invalidación y degradación cuando Redis falla.
- Generación y escape de HTML del comprobante.
- Reglas de límites y búsqueda de productos/clientes.
- Contratos estáticos de dependencias entre capas.

No queda demostrado por las pruebas inspeccionadas:

- Concurrencia real contra varias conexiones MySQL y dos ventas simultáneas del mismo lote.
- Idempotencia HTTP frente a doble click/reintento del cliente después de commit y antes de recibir respuesta.
- Integración end-to-end Razor + MySQL + Redis + navegador.
- Validación JWT Bearer, porque no existe esa autenticación en el monolito actual.
- Restauración de un lote inactivo en una base real.
- Fallo del commit o del rollback de MySQL.
- Consistencia entre réplicas cuando Redis está caído durante una escritura.

## 11. Riesgos identificados

| Área | Clasificación | Evidencia / riesgo |
|---|---|---|
| Transacciones | Comportamiento verificado | Registro y anulación inician, confirman o revierten UoW; pruebas verifican los contadores. |
| Rollback | Riesgo potencial | El rollback usa `CancellationToken.None`; si la conexión falla o el rollback falla, puede ocultar la excepción original. |
| Concurrencia de stock | Mitigación verificada | `FOR UPDATE` y UPDATE condicional protegen lotes en la ruta MySQL. No hay prueba concurrente real. |
| Orden de bloqueos | Mitigación verificada | Las líneas se ordenan por producto y existe retry de un interbloqueo. |
| Doble venta HTTP | Riesgo potencial | No existe clave de idempotencia ni token de operación; un reenvío válido puede crear otra venta. |
| Doble anulación | Mitigación verificada | `FOR UPDATE`, estado comprobado y trigger de venta anulada; falta condición de estado en el UPDATE. |
| Restauración | Riesgo potencial | `RestaurarAsync` no filtra lote activo; puede alterar stock no vendible si el lote fue desactivado. |
| FEFO | Comportamiento verificado | Algoritmo y pruebas cubren vencimiento, nulos, empate e insuficiencia. |
| Auditoría | Comportamiento verificado | Usuario de creación/anulación y fecha de anulación se persisten. |
| Auditoría de stock | Riesgo potencial | Descuento/restauración no escribe usuario de actualización del lote. |
| Cache | Comportamiento verificado | TTL 30 s, dos claves e invalidación best-effort. |
| Cache obsoleta | Riesgo potencial | Si invalidar Redis falla tras commit, historial puede quedar obsoleto hasta TTL. |
| Comprobante | Mitigación verificada | Consulta datos persistidos, agrupa líneas y escapa texto HTML. |
| Comprobante tras anulación | Comportamiento verificado | Muestra estado `ANULADA`, pero el DTO del comprobante no incluye motivo/usuario/fecha de anulación. |
| Autorización | Comportamiento verificado | Pages y POST de anulación comprueban roles. |
| Autorización distribuida | Riesgo de migración | La autorización actual depende de cookie/claims del monolito; no demuestra validación JWT entre servicios. |
| Validación de precio | Mitigación verificada | Precio mostrado se compara con precio vigente dentro del caso de uso. |
| Datos de entrada | Mitigación parcial | La Page valida DTO, pero la fachada no limita explícitamente decimal, longitud de listas o duplicados de líneas. |

## 12. Clasificación para migración a arquitectura hexagonal

### A. Reutilizables casi directamente

- `Domain/Sales/Venta.cs`, `DetalleVenta.cs`, `EstadoVenta.cs`.
- `Domain/Stock/AsignadorFefo.cs`, `ConsumoDeLote.cs`, `LoteDisponible.cs`, excepciones de stock.
- DTOs de venta y stock que no contienen ASP.NET Core.
- Interfaces pequeñas: `IVentaRepository`, `IVentaConsultaRepository`, `ILoteRepository`, `IUnidadDeTrabajo`, `IComprobanteRepository`.
- Reglas puras de agrupación de `VentaConsultaService` y `ComprobanteService`, después de extraer dependencias técnicas.
- Pruebas unitarias de FEFO, fachadas y comprobantes como base de regresión.

### B. Reutilizables con adaptaciones

- `FachadaVenta`: conservar el caso de uso, pero cambiar el contexto de usuario por un puerto alimentado desde JWT y reducir dependencias de consultas/cache.
- `FachadaAnulacion`: conservar la orquestación, fortalecer actualización condicional e idempotencia.
- `FachadaActualizacionStockProducto`: conservar FEFO, aclarar propiedad de lotes y auditoría.
- `VentaConsultaService`: extraer cache a un puerto; decidir si Redis se elimina o se mantiene fuera del núcleo.
- `ProductService`: separar capacidades de venta de CRUD, categorías, proveedores e historial.
- `ClienteConsultaService`/`ClienteCreacionService`: adaptar al contrato que exponga el Servicio 2 o definir acceso compartido autorizado.
- `ComprobanteService` y `MontoEnLetras`: reutilizables si el comprobante pertenece al Servicio 1; cambiar el adaptador HTML/API.
- `MySqlVentaRepository`, `MySqlVentaConsultaRepository`, `MySqlLoteRepository`: reutilizar SQL con una unidad de trabajo/adaptador de infraestructura del nuevo servicio.
- Pruebas de handlers: adaptar a pruebas de endpoints HTTP del servicio.

### C. Deben convertirse en puertos o adaptadores

- `UsuarioActualDesdeClaims` y `ClaimsDeIdentidad`: adaptador de entrada JWT; no debe depender de cookie ni `HttpContext` dentro del núcleo.
- `MySqlUnidadDeTrabajo` y `DatabaseConnection`: adaptadores de persistencia.
- `MySql*Repository`: adaptadores de salida.
- `GeneradorComprobanteHtml`: adaptador de presentación; el caso de uso debe depender de `IGeneradorComprobante`.
- `VentaConsultaService` + `IDistributedCache`: puerto de cache o eliminación de cache.
- `Creator*Repository` y decoradores de errores: composición/adaptadores; no forman el dominio.
- `EjecutorDePeticiones`, antiforgery y binding Razor: adaptadores de entrada/frontend.

### D. Pertenecen al frontend

- `Pages/Sales/Index.cshtml`, `Historial.cshtml`, `SaleInputModel.cs`.
- `Pages/Sales/Index.cshtml.cs`, `Historial.cshtml.cs` como lógica de UI/adaptadores HTTP, salvo los contratos que se reescriban como endpoints.
- `wwwroot/js/ventas.js`, `ventas-reglas.js`, `historial.js`, `wwwroot/css/ventas.css`.
- `Pages/Comprobante/Index.cshtml.cs` si el frontend será responsable de presentar/abrir comprobantes.

### E. No son necesarios para el Servicio 1

- CRUD administrativo completo de categorías, proveedores y productos.
- Administración de usuarios, cambio de contraseña, activación y correo.
- BCrypt y login cookie del monolito, si la autenticación pasa al Servicio 2.
- `Infrastructure/Background/BajaAutomaticaDeLotesVencidos` y `BajaDeLotesService`, salvo decisión explícita de que inventario/lotes sea responsabilidad de Ventas.
- Pages administrativas de productos, categorías, proveedores y usuarios.
- Nginx y Docker no son parte del núcleo del Servicio 1; sólo se integran en despliegue. El usuario indicó que la solución final no usará Nginx.

## 13. Límite propuesto para el Servicio 1

El Servicio 1 debería ser dueño de los casos de uso:

- registrar venta;
- consultar historial y detalle;
- anular venta;
- consultar precio/producto disponible para venta;
- descontar y restaurar stock por lote si el equipo decide que lotes pertenecen a Ventas;
- generar/servir comprobante;
- registrar auditoría de venta y anulación.

Clientes y usuarios son datos compartidos, pero su propiedad funcional debe acordarse con el Servicio 2. La venta sólo debería depender de contratos de consulta/validación del usuario y cliente, no de CRUD administrativo de Usuarios. Productos administrativos, categorías y proveedores no deben migrarse al Servicio 1 si no son necesarios para los casos de venta.

## 14. Preguntas y decisiones pendientes

1. ¿El Servicio 1 será dueño de `LOTE`/stock o existirá un servicio de inventario no contemplado en los tres servicios?
2. ¿Ventas consultará clientes y usuarios por HTTP al Servicio 2, o se permitirán puertos de lectura sobre la base compartida?
3. ¿El comprobante HTML seguirá siendo responsabilidad de Ventas o del frontend?
4. ¿Se conservará Redis para historial? Si no, debe retirarse `IDistributedCache` del caso de uso.
5. ¿Se requiere idempotencia para `POST /ventas` y `POST /ventas/{id}/anulacion`?
6. ¿Se debe registrar auditoría de cada descuento/restauración de lote con usuario y operación?
7. ¿El token JWT contendrá id, nombre y rol con nombres compatibles con los claims actuales?
8. ¿La consulta de historial seguirá limitada a 50 registros y paginada en frontend, o se expondrá paginación server-side?
9. ¿Se permite anular cualquier venta completada o debe existir ventana temporal, motivo tipificado o control adicional?
10. ¿Debe el comprobante de una venta anulada mostrar motivo, fecha y usuario de anulación?
11. ¿Se aceptan ventas con líneas duplicadas del mismo producto o deben consolidarse antes de reservar stock?
12. ¿Cómo se gestionará una caída después del commit y antes de que el cliente reciba `idVenta`?

## Conclusión

El módulo actual contiene una base aprovechable para el núcleo de Ventas: fachadas transaccionales, puertos de repositorio, UoW, entidades de venta, FEFO determinista y pruebas unitarias. Sin embargo, todavía es un módulo de un monolito Razor Pages: su autenticación es por cookies, sus entradas son Pages, el historial está acoplado a Redis y la composición reúne responsabilidades administrativas que no deben pasar al Servicio 1.

La migración debe preservar primero las invariantes demostradas por las pruebas: precio vigente, cliente activo, FEFO, bloqueo de lotes, rollback, auditoría de usuario y prevención de doble anulación. Los riesgos que requieren decisión antes de implementar son idempotencia, propiedad del stock/lotes, contrato con Usuarios/Clientes, estrategia de cache y forma final del comprobante.
