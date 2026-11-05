# Migración de Application — Etapa 3

Fecha: 2026-11-04. Destino net10.0. Se conserva el dominio de la etapa 2.

## 1. Archivos originales consultados

Dentro de Proyecto_Arquitectura_Micromercado/:

- Application/Facades/: FachadaVenta.cs, FachadaAnulacion.cs, FachadaActualizacionStockProducto.cs y sus tres interfaces IFachada*.cs.
- Application/Sales/: todos los archivos (IVentaRepository.cs, IVentaConsultaRepository.cs, IVentaConsultaService.cs, VentaConsultaService.cs, VentaDtos.cs, VentaConsultaDtos.cs y PrecioCambiadoException.cs).
- Application/Stock/: ILoteRepository.cs, IBajaDeLotesService.cs y BajaDeLotesService.cs.
- Application/Common/: IUnidadDeTrabajo.cs, IDetectorDeInterbloqueo.cs e IRepositorioBase.cs.
- Application/Security/IUsuarioActual.cs.
- Application/Products/: IConPrecioVigente.cs y DescripcionDeProducto.cs.
- Application/Customers/: IClienteConsultaRepository.cs y ClienteDtos.cs.
- Application/Comprobantes/: ComprobanteService.cs, IComprobanteRepository.cs, IComprobanteService.cs y ComprobanteDtos.cs.
- Infrastructure/Persistence/EnMemoria/InMemoryLoteRepository.cs e Infrastructure/Web/UsuarioActualEnMemoria.cs, exclusivamente como origen de dobles para Tests.

Dentro de Proyecto_Arquitectura_Micromercado.Tests/: FachadaVentaTests.cs, FachadaAnulacionTests.cs, FachadaActualizacionStockProductoTests.cs, VentaConsultaServiceTests.cs, DoblesDeVenta.cs y RelojFijo.cs.

Referencias de auditoría: AUDITORIA_MONOLITO_VENTAS.md, PREPARACION_REFACTOR_VENTAS.md y docs/MIGRACION_DOMINIO.md de este servicio.

## 2. Archivos migrados

Destino base: src/PuntoMarket.Ventas.Application/.

| Carpeta | Archivos |
| --- | --- |
| Facades | FachadaVenta.cs, FachadaAnulacion.cs, FachadaActualizacionStockProducto.cs, IFachadaVenta.cs, IFachadaAnulacion.cs, IFachadaActualizacionStockProducto.cs |
| Sales | IVentaRepository.cs, IVentaConsultaRepository.cs, IVentaConsultaService.cs, VentaConsultaService.cs, VentaDtos.cs, VentaConsultaDtos.cs, PrecioCambiadoException.cs |
| Stock | ILoteRepository.cs |
| Common | IUnidadDeTrabajo.cs, IDetectorDeInterbloqueo.cs |
| Security | IUsuarioActual.cs |
| Products | IConPrecioVigente.cs, DescripcionDeProducto.cs |
| Customers | IClienteActivoConsulta.cs, nuevo puerto mínimo |

Son 20 archivos de Application. Los namespaces pasan de Proyecto_Arquitectura_Micromercado.Application.* a PuntoMarket.Ventas.Application.* y las referencias al dominio usan PuntoMarket.Ventas.Domain.*.

No se copiaron repositorios concretos a Application. No se cambió ningún csproj, versión de paquete ni referencia de proyecto.

## 3. Puertos reutilizados y creados

- IUnidadDeTrabajo: inicio, confirmación y rollback. Se documenta que ventas y lotes deben compartir la misma transacción.
- IDetectorDeInterbloqueo: clasifica la excepción sin conocer MySQL. No se proporciona un detector concreto de producción.
- IVentaRepository: registro de cabecera/detalles, cierre, lectura para anulación y anulación lógica; conserva las lecturas de ventas existentes.
- ILoteRepository: consulta de lotes, descuento, restauración y operaciones de baja existentes.
- IVentaConsultaRepository: historial limitado y detalle por filas.
- IConPrecioVigente: precio vigente y descripción del producto, sin CRUD administrativo.
- IUsuarioActual: identidad de auditoría sin HttpContext, claims, cookies ni JWT.
- Interfaces de entrada: IFachadaVenta, IFachadaAnulacion, IFachadaActualizacionStockProducto e IVentaConsultaService.
- Nuevo IClienteActivoConsulta: solo ExisteActivoAsync. Sustituye la dependencia de FachadaVenta respecto de IClienteConsultaRepository, que también exponía búsqueda de clientes y verificación de NIT, innecesarias para registrar ventas.

No se migraron el CRUD de productos/categorías/proveedores/usuarios, autenticación, creación de clientes ni sus contratos administrativos. No se incorporaron IRepositorioBase ni implementaciones de persistencia.

## 4. Casos de uso y comportamiento conservado

### Registro

FachadaVenta valida DTO, cliente, líneas, ids y cantidades. Verifica cliente activo antes de abrir la transacción, como el original. Ordena líneas por producto y ejecuta hasta dos intentos si IDetectorDeInterbloqueo reconoce el primer fallo.

Dentro de la unidad de trabajo: inserta cabecera con usuarioActual.Id, consulta precio vigente, rechaza producto inexistente/inactivo y discrepancias con PrecioEsperado; ejecuta la fachada de stock real, genera un detalle por consumo de lote, suma importes, genera comprobante y cierra la venta. Después solicita confirmación.

Ante una excepción dentro del bloque transaccional solicita RevertirAsync(CancellationToken.None) y propaga el error. Se mantienen mensajes de precio/stock con descripción y formato es-BO.

### Stock y anulación

FachadaActualizacionStockProducto llama al AsignadorFefo real del dominio, descuenta cada consumo mediante el puerto y restaura exactamente los pares idLote/cantidad recibidos.

DescontarAsync y RestaurarAsync participan en la transacción externa del caso de venta/anulación; no abren transacciones anidadas. Se conservaron los métodos originales de baja de lotes vencidos de esta misma fachada, incluida su unidad de trabajo y auditoría, para mantener su comportamiento y todas sus pruebas. No se migraron el wrapper BajaDeLotesService, servicios automáticos, Pages ni CRUD de inventario.

FachadaAnulacion valida id y motivo, inicia la unidad, solicita lectura protegida de la venta, exige estado COMPLETADA, restaura los lotes de los detalles, recorta el motivo y marca ANULADA con usuarioActual.Id; confirma o pide rollback ante error. No elimina ventas y no reintenta anulaciones, igual que el monolito.

### Consultas

VentaConsultaService obtiene ventas del día del puerto, solicita historial con límite 50 y filtro incluirAnuladas, devuelve null para ids no positivos o ventas inexistentes y conserva todos los datos del detalle/auditoría.

La agrupación original permanece: producto + precio unitario; cantidades/subtotales se suman y los lotes se agrupan por código dentro de cada línea.

### Comprobantes revisados

ComprobanteService tiene un puerto de lectura separado, agrupación por producto/precio y conversión con MontoEnLetras; no es dependencia de las fachadas ni de VentaConsultaService. No se migró generación/consulta de comprobantes porque no forma parte de los cinco objetivos de esta etapa. Registrar una venta sí conserva la generación del número de comprobante mediante Venta.GenerarNroComprobante. No hay bloqueo técnico de comprobantes para los casos de uso migrados.

## 5. Diferencias respecto al monolito

1. Se elimina completamente Redis/IDistributedCache de VentaConsultaService, así como serialización de caché, TTL de 30 segundos, claves y logging de hit/miss/identificación de réplica.
2. Cada ListarAsync lee directamente IVentaConsultaRepository. No se añade caché alternativa.
3. Se elimina InvalidarHistorialAsync de IVentaConsultaService y sus llamadas después de confirmar registro/anulación. Las fachadas ya no necesitan recibir consultaVentas en su constructor. No queda una implementación vacía para conservar artificialmente la invalidación.
4. Se reduce la consulta de clientes a IClienteActivoConsulta. No cambia la condición de aceptación del cliente.
5. Los dobles en memoria pasan al proyecto Tests con namespaces propios; no son implementaciones de Infrastructure del servicio.

No cambian las validaciones de negocio, cálculos, orden FEFO, auditoría, estados, máximo de intentos ni secuencia de persistencia/confirmación/rollback. No se añadieron reglas para líneas duplicadas, límites de importes o longitud de motivo que no estuvieran en estas fachadas originales.

## 6. Dependencias pendientes de Infrastructure

Los casos de uso se compilan y ejecutan contra puertos. Para usarlos en producción faltan:

- Unidad de trabajo real con conexión/transacción compartida por todos los repositorios participantes.
- Persistencia de venta y detalle, cierre, lectura con bloqueo para anulación y actualización lógica con auditoría.
- Consulta/protección de lotes, descuento concurrente seguro, restauración limitada a cantidadInicial y baja.
- Consultas de historial/detalle y suma de ventas del día.
- Lectura de precio vigente/estado de producto y validación de cliente activo, según la integración acordada.
- Clasificador de deadlocks del proveedor de persistencia.
- Adaptador de identidad en la futura entrada y composición de TimeProvider.

El código documenta estos contratos; no implementa transacciones ficticias. No hay adaptadores MySQL, conexiones, SQL, endpoints ni registros DI nuevos en esta entrega. La API sigue siendo el esqueleto anterior y todavía no compone estos casos de uso.

## 7. Pruebas migradas y regresión

Ubicación: tests/PuntoMarket.Ventas.Tests/Application/ y Doubles/.

| Grupo | Casos |
| --- | ---: |
| Domain previo, sin cambios | 31 |
| FachadaVentaTests migradas | 18 |
| FachadaAnulacionTests migradas | 7 |
| FachadaActualizacionStockProductoTests migradas | 8 |
| VentaConsultaServiceTests originales migradas | 6 |
| RegresionCasosDeUsoTests nuevas | 7 |
| VentaConsultaRegresionTests nuevas | 9 |
| Total | 86 |

Las cuatro suites originales se conservaron completas. Se adaptaron namespaces y constructores, retirando la dependencia de consulta/cache. VentaConsultaServiceTests conserva sus seis pruebas y amplía su doble para las regresiones nuevas.

Se reutilizaron DoblesDeVenta.cs, RelojFijo.cs, InMemoryLoteRepository.cs y UsuarioActualEnMemoria.cs dentro de Tests. ClientesDoble implementa el puerto mínimo. Se retiraron HistorialVentasDoble y ReplicaFija porque ya no hay caché ni invalidaciones. La lectura de ventas del día del doble admite un resultado conocido para comprobar delegación y token, sin simular SQL.

Cobertura ejecutada: registro correcto, cantidades inválidas, cliente/producto inválido, stock insuficiente y nombre del producto, precio esperado cambiado/coincidente, varios productos/lotes FEFO, reintento y agotamiento de deadlocks, rollback solicitado, anulación/restauración, doble anulación, auditoría de cajero/anulador y baja de lotes.

Las regresiones adicionales llaman a las fachadas/servicio reales, comprueban secuencia de puertos y propagación de errores al fallar detalle, cierre, confirmación y marcado de anulación, además de rollback con token no cancelable. Las de consultas comprueban resultados/filtro/límite/token, lecturas sucesivas sin caché, agrupación separada por precio, metadatos de anulación, id inválido, total del día, errores y cancelación.

Los espías de IUnidadDeTrabajo solo observan protocolo: no revierten el estado de los repositorios en memoria ni representan atomicidad MySQL. Las aserciones de rollback verifican su solicitud, orden y ausencia de confirmación exitosa, sin afirmar que una base de datos fue restaurada. La integración transaccional real queda pendiente del adaptador de Infrastructure y sus pruebas.

## 8. Validación y dependencias

Comandos ejecutados desde Servicio1.Ventas/:

~~~powershell
dotnet restore PuntoMarket.Ventas.slnx
dotnet build PuntoMarket.Ventas.slnx --no-restore
dotnet test tests/PuntoMarket.Ventas.Tests/PuntoMarket.Ventas.Tests.csproj --no-restore
~~~

Todos finalizan con código 0. Build: 0 errores, 1 advertencia NU1900. Tests: 86 correctas, 0 fallidas y 0 omitidas.

También se evaluaron ProjectReference, PackageReference y FrameworkReference con dotnet msbuild:

- Domain: sin ProjectReference ni PackageReference.
- Application: una única ProjectReference, a Domain; sin PackageReference.
- Ambos: solo Microsoft.NETCore.App implícito, sin framework ASP.NET Core.
- Sin referencias circulares ni referencias al csproj del monolito.
- Búsqueda en código/csproj de Application y Tests: sin usos de MySql, Infrastructure, HttpContext, Redis, IDistributedCache, Microsoft.AspNetCore ni namespaces del monolito.

No se instalaron paquetes. Las 31 pruebas anteriores de Domain siguen pasando.

## 9. Problemas detectados y límites

- NU1900: fallo SSL consultando vulnerabilidades de NuGet, ya presente en etapas anteriores; no bloquea restore/build/test. No se deshabilitó la auditoría para ocultarlo.
- El rollback original puede ocultar la excepción inicial si el propio RevertirAsync falla. Se conserva y se documenta; no se cambió la política.
- IniciarAsync está antes del try, como el original. Si falla el inicio no se solicita rollback.
- Validación de cliente antes de iniciar y precios dentro de la transacción se conservan; las garantías de consistencia requieren implementación real.
- La seguridad concurrente de stock y anulación, el commit/rollback real y la traducción de deadlocks no quedan demostrados por pruebas en memoria.
- No se incluyeron suites de caché Redis: su comportamiento se ha retirado por instrucción expresa.
- No hay dependencia de infraestructura que bloquee la migración de los cinco objetivos a puertos. Su ejecución productiva y garantías transaccionales quedan pendientes de los adaptadores.

## 10. Integridad y cierre

Se compararon huellas SHA-256 de los 652 archivos originales fuera de Servicio1.Ventas antes y después: ninguno modificado o eliminado, y ningún archivo añadido fuera de la carpeta autorizada.

Solo se crearon o editaron archivos dentro de Servicio1.Ventas. Domain y sus pruebas previas no se editaron. No se modificaron monolito/pruebas originales, SQL, Docker ni frontend. No se ejecutó SQL, no hubo conexiones a bases de datos, instalaciones de Redis, commits ni push.

Etapa 3 finalizada. No se implementaron Infrastructure ni Api.
