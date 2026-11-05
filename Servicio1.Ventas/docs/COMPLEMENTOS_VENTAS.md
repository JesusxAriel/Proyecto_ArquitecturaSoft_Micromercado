# Complementos de Ventas — Etapa 6

Fecha: 2026-11-04. Implementación incremental de consultas de productos/clientes y comprobantes. Domain y las reglas transaccionales de registro/anulación no se modificaron.

## 1. Inspección y operaciones reales del frontend

Fuentes revisadas en el monolito:

- Pages/Sales/Index.cshtml y Index.cshtml.cs.
- wwwroot/js/ventas.js y ventas-reglas.js.
- Pages/Comprobante/Index.cshtml y su PageModel.
- Application/Products/ProductService.cs, IConBusquedaParaVenta.cs y BusquedaParaVentaDtos.cs.
- Application/Customers/ClienteConsultaService.cs, IClienteConsultaRepository.cs y DTOs.
- Application/Comprobantes/: contratos, servicio, DTOs, generador y MontoEnLetras.
- Infrastructure/Comprobantes/GeneradorComprobanteHtml.cs.
- Infrastructure/Persistence/MySqlProductRepository.cs, MySqlClienteConsultaRepository.cs, MySqlComprobanteRepository.cs y SqlLike.cs; repositorios actuales de ventas, lotes y precio del servicio.
- bdMicroMercadoArqui.sql: tablas, stockActual, lotes/vencimientos, vistas, columnas de detalle/comprobante y triggers.
- Informes MIGRACION_APPLICATION.md, MIGRACION_INFRASTRUCTURE.md y MIGRACION_API.md.

Se revisaron también ComprobanteTests.cs, MontoEnLetrasTests.cs e InMemoryComprobanteRepository.cs originales para reutilizarlos.

Operaciones de la pantalla:

| Uso | Contrato original observado |
| --- | --- |
| Buscar productos por nombre/presentación | GET con termino; envoltura productos, total, hayMas, mensaje |
| Elegir producto | id, nombre, presentacion, precioVenta, stockDisponible |
| Deshabilitar o limitar unidades | stockDisponible; mantiene cantidades ya agregadas al detalle |
| Buscar clientes por NIT/CI o razón social | Array de id, nitCi, razonSocial |
| Registrar | idCliente y líneas con idProducto, cantidad, precioEsperado |
| Abrir comprobante después del registro | La UI usa idVenta y abre /Comprobante/{idVenta} |
| Imprimir | HTML con botón window.print(), sin generación PDF |
| Crear cliente desde modal | Existe en el monolito, pero su propiedad entre equipos todavía no está confirmada |

El JavaScript usa búsqueda desde dos caracteres para ambos recursos, debounce y reintento de lecturas. El servidor original de productos exige mínimo dos; el servicio original de clientes acepta un carácter no vacío. Se mantiene esa diferencia de servidor.

## 2. Endpoints definitivos

Todos los endpoints de negocio requieren JWT y la política OperarVentas: Administrador o Cajero, sin contraseña pendiente de cambio.

| Ruta GET | Resultado |
| --- | --- |
| /api/productos?termino=leche | Búsqueda de productos activos, máximo 10 y hayMas |
| /api/productos/{id} | Producto activo con precio y disponibilidad |
| /api/productos/{id}/precio | id y precioVenta |
| /api/productos/{id}/stock | id y stockDisponible |
| /api/clientes?termino=123 | Clientes activos por NIT/CI o razón social, máximo 20 |
| /api/ventas/{id}/comprobante | Comprobante en JSON |
| /api/ventas/{id}/comprobante/html | Comprobante imprimible en HTML |

El logo original se copió a wwwroot/images/logo.jpg del servicio y se sirve por GET /images/logo.jpg como recurso público de marca. No expone datos de venta ni credenciales. No se copió ni modificó el frontend.

Las rutas de negocio envían Cache-Control no-store. Sin resultados, las búsquedas responden 200 con colección vacía. Producto inexistente/inactivo o comprobante inexistente: 404 ProblemDetails. Id no positivo o término mayor que 150 caracteres: 400. Sin JWT: 401; rol insuficiente o contraseña pendiente: 403. Los errores técnicos conservan el manejo seguro de etapa 5.

No se añadieron rutas POST/PUT/PATCH/DELETE para estos recursos ni CRUD administrativos.

## 3. DTOs y ejemplos

### Productos

ProductoParaVenta:

~~~json
{
  "id": 1,
  "nombre": "Leche",
  "presentacion": "Bolsa 1L",
  "precioVenta": 10.50,
  "stockDisponible": 8
}
~~~

ResultadoBusquedaParaVenta:

~~~json
{
  "productos": [
    { "id": 1, "nombre": "Leche", "presentacion": "Bolsa 1L", "precioVenta": 10.50, "stockDisponible": 8 }
  ],
  "hayMas": false,
  "total": 1,
  "mensaje": null
}
~~~

Se conservan los nombres esperados por la UI. Total es el número de resultados devueltos, no un COUNT global. Se consulta un elemento adicional para calcular hayMas. Los productos con stock cero siguen visibles, como en la pantalla original.

Los endpoints específicos devuelven:

~~~json
{ "id": 1, "precioVenta": 10.50 }
~~~

~~~json
{ "id": 1, "stockDisponible": 8 }
~~~

### Clientes

ClienteConsultaDto:

~~~json
[
  { "id": 3, "nitCi": "12345", "razonSocial": "Cliente Uno" }
]
~~~

Entrada de búsquedas: termino opcional, recortado en Application. Productos: menos de dos caracteres devuelve vacío sin llamar al puerto. Clientes: término vacío devuelve vacío; el resto busca en documento/nombre. Se añade máximo de 150 caracteres para acotar consultas; no se permite al cliente ampliar límites de resultados.

### Comprobantes

Se migraron los DTOs originales: VentaParaComprobante, DetalleDeVenta, ComprobanteDto y LineaComprobanteDto.

ComprobanteDto contiene nroComprobante, fechaVenta, nitCiCliente, razonSocialCliente, estado, lineas, total, totalEnLetras, fechaGeneracion, usuarioGenerador y estaAnulada.

Cada línea contiene cantidad, descripcion, precioUnitario e importe. No contiene cantidades o precios inventados: los datos se leen de VENTA/DETALLE_VENTA y referencias históricas. Los ejemplos anteriores son únicamente documentación.

## 4. Application: casos de uso y puertos

Nuevos archivos:

- Products/ProductoVentaConsulta.cs: ProductoParaVenta, ResultadoBusquedaParaVenta, IProductoVentaConsultaRepository, IProductoVentaConsultaService y ProductoVentaConsultaService.
- Customers/ClienteVentaConsulta.cs: ClienteConsultaDto, IClienteVentaConsultaRepository, IClienteVentaConsultaService y ClienteVentaConsultaService.

Los servicios normalizan, limitan y propagan cancelación. El servicio de productos obtiene hoy mediante el mismo TimeProvider usado por las fachadas/FEFO y lo pasa al puerto de lectura.

Se migraron todos los archivos originales de Application/Comprobantes, cambiando namespaces a PuntoMarket.Ventas.Application.Comprobantes:

- IComprobanteRepository.cs.
- IComprobanteService.cs.
- IGeneradorComprobante.cs.
- ComprobanteDtos.cs.
- ComprobanteService.cs.
- MontoEnLetras.cs.

El servicio de comprobantes conserva agrupación por producto/precio, sumas de cantidades/importes y conversión de total a letras. Mantiene fechaGeneracion igual a fecha de la venta y usuarioGenerador igual al cajero original, no al usuario que consulta. Una venta anulada conserva su comprobante con marca ANULADA.

Los puertos son de lectura y no exponen conexiones, MySQL, HttpContext ni operaciones administrativas. No se modificaron contratos previos: se añadieron contratos mínimos para las necesidades ahora autorizadas. Application continúa referenciando únicamente Domain; no se añadieron paquetes.

## 5. Infrastructure y composición

Adaptadores nuevos:

- MySqlProductoVentaConsultaRepository: búsquedas/detalle/precio/stock, mediante SELECT parametrizados.
- MySqlClienteVentaConsultaRepository: solo lectura de clientes activos.
- MySqlComprobanteRepository: consultas originales de cabecera y detalles, sin el decorador de errores ausente de Application; los errores de proveedor siguen siendo manejados por Api.
- GeneradorComprobanteHtml: generador original adaptado únicamente de namespace.
- PatronBusqueda: patrón literal LIKE con ESCAPE '!' y parámetros.

PersistenciaVentasMySql incorpora ProductosConsulta, ClientesConsulta y Comprobantes sobre su misma unidad de trabajo. Api registra los puertos y servicios scoped y el generador HTML singleton sin estado.

Las consultas NO llaman a IConPrecioVigente transaccional, NO ejecutan FOR UPDATE y NO inician/confirmar/revierten transacciones para mostrar información. Al no haber transacción activa, AlquilarAsync abre una conexión propia y la libera con await using.

La búsqueda MySQL conserva filtros de producto/cliente activo y orden estable por nombre/id. Los caracteres %, _ y ! del término se escapan como literales; los valores siempre se parametrizan. No depende del modo de escape de barras de MySQL.

No se añadieron repositorios en memoria a producción ni dependencias nuevas de NuGet.

## 6. Stock: diferencia deliberada de lectura

El monolito mostraba PRODUCTO.stockActual, mantenido por triggers como suma de lotes activos. Esa cifra puede incluir lotes vencidos pendientes de baja; Domain/AsignadorFefo no permite venderlos.

La nueva disponibilidad muestra la suma de LOTE.cantidadDisponible para lotes:

- Del producto consultado.
- Activos.
- Con cantidad positiva.
- Sin fecha de vencimiento o con vencimiento igual/posterior a hoy.

Es una adaptación del modelo de lectura para alinearlo con FEFO, no un cambio de las reglas del dominio ni una modificación de stockActual/SQL/triggers. Se usa el día local del TimeProvider de Application; el SQL compara contra @hoy, no contra un reloj adicional del servidor.

No se filtran los productos solo por tener stock: un producto activo sin lotes vendibles devuelve cero. Los productos inactivos no aparecen y su consulta individual devuelve 404.

La información es orientativa, no reserva existencias. POST /api/ventas sigue comprobando cliente, precio y lotes con bloqueo dentro de la transacción original y puede rechazar un precio/stock que haya cambiado desde la lectura.

## 7. Comprobantes HTML

Se conserva HTML completo, estilos de impresión, botón window.print(), importes es-BO, escape con WebUtility.HtmlEncode, datos del cliente y cajero original, total en letras y sello de venta anulada.

El precio del comprobante es precioUnitario de DETALLE_VENTA, no el precio vigente del catálogo. No se crea ni actualiza la venta al consultar o imprimir. No se genera PDF.

El repositorio conserva joins a CLIENTE, USUARIO y PRODUCTO sin exigir que sigan activos: una venta histórica debe poder consultarse aunque esas referencias ya no estén activas.

Se copió el logo original sin alterarlo. El resto del frontend no se copió.

## 8. Pruebas

Se conservaron y ejecutaron las 153 pruebas anteriores. Se amplió el helper ApiTestHost con un callback opcional de registros para inyectar puertos de lectura; no se sustituyeron las políticas ni fachadas/controladores reales.

| Grupo | Casos correctos |
| --- | ---: |
| Pruebas anteriores | 153 |
| ComprobanteTests originales migradas | 24 |
| MontoEnLetrasTests originales migradas | 41 |
| ConsultasComplementariasTests nuevas | 21 |
| ComplementosHttpTests nuevas | 30 |
| Total | 269 |

Nueve integraciones MySQL previas permanecen omitidas. Total descubierto: 278; fallidas: 0.

Cobertura nueva/migrada:

- Términos vacíos/cortos, sin coincidencias, normalización, longitud máxima y cancelación.
- Límites 10/20, hayMas y fecha de referencia FEFO propagada.
- Campos de precio/stock, stock cero y respuesta a entidades inactivas/no existentes.
- Metacaracteres LIKE como literales.
- Invariante estática: SQL de catálogo/clientes sin escrituras ni bloqueos.
- Autorización JWT de todas las rutas nuevas y contraseña pendiente.
- Validación de filtros/ids antes de leer y respuestas JSON/HTML coherentes.
- Comprobantes existentes/inexistentes/anulados, agrupación por producto/precio, montos a letras, formato e impresión.
- Escape HTML y preservación del cajero/fecha de la venta frente al usuario que consulta.
- Las consultas HTTP no invocan ningún puerto previo de escritura ni unidad transaccional; no cambian los datos de los dobles.

Límite de evidencia: las pruebas unitarias/HTTP usan puertos controlados de lectura. No ejecutan el SQL real ni demuestran contra MySQL el filtrado de inactivos, la suma de lotes/vencimientos o el rendimiento. Las condiciones SQL fueron inspeccionadas; la prueba contra servidor sigue pendiente del entorno aislado. No se considera una prueba de integración correcta por tener dobles o por inspeccionar SQL.

## 9. Pendientes por acuerdos del equipo

- Crear/modificar clientes: NO implementado. El modal original queda pendiente hasta acordar si corresponde a Usuarios u otro responsable. Este servicio solo consulta clientes activos.
- No se implementó CRUD de productos, categorías, proveedores ni usuarios.
- Confirmar con frontend las rutas REST y la adaptación de sesión/cookies a Bearer.
- El frontend original abre una URL de comprobante con window.open. Una navegación normal no añade Authorization: Bearer; deberá obtener HTML mediante una petición autenticada y presentarlo/imprimirlo de forma acordada. No colocar JWT en query strings.
- Si se usa HTML en Blob o en otro origen, acordar la base de resolución de /images/logo.jpg; el recurso está servido por la API.
- Mantener los acuerdos JWT, CORS, origen/TLS y manejo de 401/403 pendientes de etapa 5.
- No se integraron realmente Usuarios, Reportes ni frontend.

## 10. Riesgos de integración y diferencias

- Aunque los nombres de los datos de búsqueda se conservan, el frontend original usa handlers Razor, antiforgery/cookie y envolturas de errores distintas. No queda integrado simplemente cambiando una URL; necesita adaptar autenticación y ProblemDetails.
- El registro original del frontend esperaba exito y precioActual/codigo precio-cambiado; la API de etapa 5 usa 201/409 y su contrato de errores. No se cambió ese contrato en esta etapa.
- La nueva disponibilidad excluye vencidos y puede ser menor que stockActual: diferencia documentada, coherente con FEFO.
- Precio/stock pueden cambiar entre consulta y venta; se mantienen comprobaciones transaccionales posteriores.
- El comprobante usa cabecera/detalle en SELECT separados, como el original. Una anulación concurrente puede reflejarse en una lectura posterior; no se promete una instantánea atómica sin acordar una estrategia adicional.
- Los nombres de producto/cliente/cajero provienen de referencias actuales, mientras que cantidades/precios/importes de venta son históricos. Se conserva el diseño original.
- La suma del stock se representa como int, igual que la interfaz original; datos inconsistentes que excedan ese rango fallarán de forma segura.
- No se migró el reintento específico de lecturas del repositorio administrativo ni se añadió caché. Se mantiene propagación segura de errores de persistencia y el frontend puede acordar sus reintentos.
- No se ejecutaron pruebas SQL/MySQL ni se midió el costo de las búsquedas LIKE y suma correlacionada. Usan los nombres e índices existentes, sin migraciones.
- La longitud máxima de filtro 150 es un límite nuevo de consulta, alineado con los campos de nombre/razón social. No modifica reglas de registro.
- Se conservan puerto 5002, roles y validación JWT de etapa 5.

## 11. Validación final

Desde Servicio1.Ventas:

~~~powershell
dotnet restore PuntoMarket.Ventas.slnx
dotnet build PuntoMarket.Ventas.slnx --no-restore
dotnet test PuntoMarket.Ventas.slnx --no-restore
~~~

Los tres comandos finalizaron con código 0.

- Build: 0 errores y 1 advertencia NU1900 de Infrastructure por imposibilidad de consultar el índice/datos de vulnerabilidades de NuGet.
- Tests: 269 correctas, 0 fallidas, 9 omitidas; 278 descubiertas.
- Sin paquetes nuevos, dependencias circulares ni referencias al csproj del monolito.
- Application sigue sin dependencia de Infrastructure, Api, ASP.NET Core o MySQL.
- Las nueve integraciones MySQL se omiten explícitamente por ausencia de entorno dedicado autorizado.

## 12. Integridad y cierre

Comparación SHA-256 de los 652 archivos originales fuera de Servicio1.Ventas antes/después: 0 modificados, 0 eliminados y 0 archivos añadidos fuera de la carpeta autorizada del proyecto.

Solo se crearon/modificaron archivos dentro de Servicio1.Ventas: nuevos contratos/servicios de lectura y comprobantes, adaptadores MySQL/HTML, registros de composición, controladores, logo copiado, pruebas y documentación. Domain no se editó. No se modificó monolito, pruebas originales, frontend, SQL original ni Docker Compose.

No se ejecutó SQL ni se conectó a MySQL, no se iniciaron contenedores, no se instalaron paquetes ni se hicieron commits, ramas o push.

Etapa 6 finalizada. Docker e integración real de microservicios quedan fuera de este trabajo.
