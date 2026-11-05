# Migración del dominio de Ventas y Stock — Etapa 2

Fecha: 2026-11-04. SDK .NET 10.0.401; destino net10.0.

## 1. Archivos originales consultados

Bajo Proyecto_Arquitectura_Micromercado/:

- Domain/Sales/Venta.cs
- Domain/Sales/DetalleVenta.cs
- Domain/Sales/EstadoVenta.cs
- Domain/Stock/AsignadorFefo.cs
- Domain/Stock/ConsumoDeLote.cs
- Domain/Stock/LoteDisponible.cs
- Domain/Stock/ResultadoBajaDeLotes.cs
- Domain/Stock/StockInsuficienteException.cs
- Domain/Stock/RestauracionExcedeCantidadInicialException.cs
- Application/Facades/FachadaVenta.cs y FachadaActualizacionStockProducto.cs: inspección de dependencias y llamadas al dominio.
- Infrastructure/Persistence/EnMemoria/InMemoryLoteRepository.cs: inspección de dependencias y ubicación de la restauración.

Bajo Proyecto_Arquitectura_Micromercado.Tests/:

- AsignadorFefoTests.cs: lectura completa y migración.
- FachadaActualizacionStockProductoTests.cs e InMemoryLoteRepositoryTests.cs: lectura completa para delimitar pruebas puras frente a orquestación/persistencia.
- FachadaVentaTests.cs, FachadaAnulacionTests.cs, DoblesDeVenta.cs: búsqueda de referencias y revisión de dependencias.
- CatalogBajaDeLotesHandlerTests.cs, HistorialAutorizacionTests.cs, StockActualMySqlTests.cs y BajaAutomaticaDeLotesVencidosTests.cs: revisión de dependencias de las pruebas relacionadas.

Se consultó AUDITORIA_MONOLITO_VENTAS.md. Se buscó en los archivos de pruebas originales el uso de Venta, DetalleVenta, EstadoVenta y métodos de las entidades; no se localizaron pruebas unitarias directas de las entidades de Sales. No se ejecutó la solución ni las pruebas del monolito.

## 2. Archivos copiados y adaptados

Destino base: src/PuntoMarket.Ventas.Domain/.

| Origen relativo a Domain/ del monolito | Destino relativo al nuevo Domain |
| --- | --- |
| Sales/Venta.cs | Sales/Venta.cs |
| Sales/DetalleVenta.cs | Sales/DetalleVenta.cs |
| Sales/EstadoVenta.cs | Sales/EstadoVenta.cs |
| Stock/AsignadorFefo.cs | Stock/AsignadorFefo.cs |
| Stock/ConsumoDeLote.cs | Stock/ConsumoDeLote.cs |
| Stock/LoteDisponible.cs | Stock/LoteDisponible.cs |
| Stock/ResultadoBajaDeLotes.cs | Stock/ResultadoBajaDeLotes.cs |
| Stock/StockInsuficienteException.cs | Stock/StockInsuficienteException.cs |
| Stock/RestauracionExcedeCantidadInicialException.cs | Stock/RestauracionExcedeCantidadInicialException.cs |

Las diez clases constituyen todo el contenido original de Domain/Sales y Domain/Stock. Se comprobó por comparación textual que el único cambio en cada archivo es el prefijo del namespace.

ResultadoBajaDeLotes se conserva como contrato puro existente de Stock. Su inclusión no implementa un caso de uso de baja ni un proceso automático.

## 3. Clases descartadas y justificación

No se descartó ninguna clase de Domain/Sales ni Domain/Stock.

No se copiaron entidades de Products, Categories, Suppliers ni Common: ninguna de las diez clases las necesita. Venta conserva IdCliente, IdUsuario e IdProducto como identificadores, sin incorporar modelos de Usuarios, Clientes o Productos.

Quedan fuera de esta etapa las fachadas, DTOs de Application, puertos, repositorios, unidades de trabajo, servicios de consulta/comprobantes, caché, adaptadores web y procesos automáticos. No se sustituyeron por implementaciones ficticias.

## 4. Cambios de namespaces

- Proyecto_Arquitectura_Micromercado.Domain.Sales → PuntoMarket.Ventas.Domain.Sales.
- Proyecto_Arquitectura_Micromercado.Domain.Stock → PuntoMarket.Ventas.Domain.Stock.
- En la copia de AsignadorFefoTests.cs, el using apunta al nuevo Domain.Stock y el namespace de pruebas pasa a PuntoMarket.Ventas.Tests.Domain.Stock.

Se mantienen nombres, firmas, visibilidad, tipos, mensajes de excepciones y comportamiento. No se alteraron las aserciones ni datos de la prueba original de FEFO.

## 5. Dependencias detectadas

- Venta depende de DetalleVenta, EstadoVenta y tipos de .NET: DateTime, decimal, List y LINQ.
- DetalleVenta calcula Cantidad * PrecioUnitario, sin servicios externos.
- AsignadorFefo depende de LoteDisponible, ConsumoDeLote, StockInsuficienteException, DateOnly, colecciones, LINQ y Math.
- Los tres records de Stock solo usan tipos de .NET.
- Las dos excepciones heredan de InvalidOperationException; StockInsuficienteException admite una excepción interna.
- No hay dependencias adicionales de dominio ni necesidad de inventar clases sustitutas.

Verificación ejecutada:

~~~powershell
dotnet list src/PuntoMarket.Ventas.Domain/PuntoMarket.Ventas.Domain.csproj reference
dotnet msbuild src/PuntoMarket.Ventas.Domain/PuntoMarket.Ventas.Domain.csproj -getItem:ProjectReference,PackageReference,FrameworkReference
~~~

Resultado: ProjectReference y PackageReference vacíos. Única referencia de framework: Microsoft.NETCore.App, implícita. project.assets.json tiene targets net10.0 y libraries sin dependencias. No hay referencias a Application, Infrastructure, Api, al csproj del monolito, ASP.NET Core ni MySQL.

No se añadieron paquetes a ningún proyecto. Tests conserva las referencias y versiones xUnit/Test SDK de la etapa 1.

## 6. Pruebas migradas y adicionales

Destino base: tests/PuntoMarket.Ventas.Tests/Domain/.

| Archivo | Procedencia | Casos ejecutados |
| --- | --- | ---: |
| Stock/AsignadorFefoTests.cs | Original, solo namespaces adaptados | 12 |
| Stock/AsignadorFefoConservacionTests.cs | Nuevo, caracterización complementaria | 6 |
| Sales/VentaTests.cs | Nuevo, caracterización de entidades existentes | 13 |
| Total | | 31 |

Los 12 casos originales corresponden a 11 métodos: la teoría de cantidad inválida ejecuta dos casos.

Cobertura FEFO original: lote suficiente, reparto por vencimiento, lotes sin vencimiento al final, exclusión de vencidos, aceptación del lote que vence hoy, exclusión de cantidad cero, desempate por id con fecha y sin fecha, stock insuficiente, cantidades cero/negativas y entrada vacía.

Cobertura adicional FEFO: cuatro cantidades solicitadas, suma exacta consumida, límites por lote, identificadores sin repetición en la salida para la entrada de prueba, conservación de cantidad inicial = restante + consumo, exclusión de vencidos, entrada sin mutación tanto en éxito como en insuficiencia y rechazo de entrada nula. No se simula un repositorio: se llama directamente al algoritmo real.

Las pruebas nuevas de Ventas caracterizan estado inicial, agregación, subtotal decimal y su recálculo, total asignado positivo frente a cero/negativo, rechazo de detalles en venta anulada sin distinguir mayúsculas, finalización sin/con detalles y formato de comprobante.

### Pruebas originales relacionadas no migradas

No falta ninguna dependencia para ejecutar AsignadorFefoTests: se migró completo.

Las siguientes suites no se pueden copiar cambiando únicamente namespaces dentro del alcance autorizado:

| Archivo original | Dependencias que faltan deliberadamente en esta etapa |
| --- | --- |
| FachadaActualizacionStockProductoTests.cs | FachadaActualizacionStockProducto, ILoteRepository, IUnidadDeTrabajo, contexto de usuario, InMemoryLoteRepository y auxiliares de reloj/UoW |
| FachadaVentaTests.cs | FachadaVenta, contratos de Application/Sales, puertos de clientes/precios/UoW, repositorios en memoria y contexto de usuario |
| FachadaAnulacionTests.cs | FachadaAnulacion, FachadaVenta, contratos de Application/Sales y repositorios en memoria |
| InMemoryLoteRepositoryTests.cs | Adaptador InMemoryLoteRepository y puerto ILoteRepository; prueban descuento/restauración persistidos, no solo la asignación pura |
| CatalogBajaDeLotesHandlerTests.cs | PageModel de Products, IBajaDeLotesService y ASP.NET Core |
| HistorialAutorizacionTests.cs | Pages/Sales, fachadas, seguridad, adaptadores en memoria y ASP.NET Core |
| StockActualMySqlTests.cs | MySql.Data, DatabaseConnection, persistencia MySQL, fachadas, background service y base externa configurada con MICROMERCADO_PRUEBAS_MYSQL |
| BajaAutomaticaDeLotesVencidosTests.cs | IBajaDeLotesService, servicio de Infrastructure/Background, DI y logging |

DoblesDeVenta.cs contiene auxiliares dependientes de contratos de Application/Common, Customers, Products y Sales; no es una suite pura de dominio y no se copió.

La excepción RestauracionExcedeCantidadInicialException sí se conserva, pero la comprobación de límites al restaurar vive en repositorios/fachadas del monolito. No se implementó esa operación en Domain para hacer pasar pruebas ajenas a esta etapa. Tampoco se trasladaron tests de comprobantes, consultas, JavaScript o CRUD: sus objetos bajo prueba no son estas clases del dominio.

Estas suites no migradas no se contabilizan como pruebas omitidas de la ejecución nueva.

## 7. Resultado de pruebas

Comando ejecutado desde Servicio1.Ventas/:

~~~powershell
dotnet test tests/PuntoMarket.Ventas.Tests/PuntoMarket.Ventas.Tests.csproj --no-restore --logger "console;verbosity=normal"
~~~

Resultado: código 0, 31 correctas, 0 fallidas y 0 omitidas. El adaptador xUnit descubrió y ejecutó pruebas reales en .NET 10.0.12.

## 8. Restauración y compilación

Comandos ejecutados desde Servicio1.Ventas/:

~~~powershell
dotnet restore PuntoMarket.Ventas.slnx
dotnet build PuntoMarket.Ventas.slnx --no-restore
~~~

Ambos finalizaron con código 0. Compilación: 0 errores, 1 advertencia NU1900 del proyecto Tests por fallo SSL al obtener datos de vulnerabilidades de NuGet. La advertencia también aparece en restore/test. No impidió restaurar, compilar ni ejecutar pruebas; la consulta de vulnerabilidades no pudo completarse.

## 9. Diferencias funcionales respecto al monolito

No se introdujeron diferencias funcionales en las diez clases: son idénticas salvo namespaces. El archivo original de pruebas FEFO también es idéntico tras normalizar sus dos namespaces.

FEFO conserva el orden: fecha no nula primero, fecha ascendente y luego IdLote ascendente. Los lotes sin vencimiento se consumen al final, también ordenados por id. Se aceptan fechas iguales a hoy; se excluyen fechas anteriores y cantidades no positivas. Si no alcanza el stock se lanza StockInsuficienteException; el algoritmo calcula consumos y no modifica los lotes de entrada.

Se preservan particularidades de Venta: Total usa el valor asignado solo si es mayor que cero; de lo contrario suma subtotales. FechaHora sigue usando DateTime.Now. No se añadieron validaciones de cantidades/precios ni nuevas restricciones de transición de estado. CompletarVenta solo exige detalles y establece COMPLETADA, tal como el original.

El servicio aún no registra ni anula ventas de extremo a extremo: Application, Infrastructure y Api conservan el esqueleto de la etapa 1. Las pruebas de esta etapa verifican reglas puras; no demuestran transacciones, restauración persistida, concurrencia MySQL ni integración HTTP.

## 10. Integridad y cierre de alcance

Se compararon las huellas SHA-256 de los 652 archivos preexistentes fuera de Servicio1.Ventas antes y después del trabajo. Resultado: 0 modificados, 0 eliminados y 0 archivos añadidos fuera de la carpeta autorizada.

Solo se crearon o editaron archivos dentro de Servicio1.Ventas. No se modificaron el monolito, sus pruebas, SQL, Docker ni configuración de autenticación. No se ejecutó SQL ni se accedió a bases de datos; no se instalaron Redis/Nginx ni se realizaron commits, ramas o push.

Etapa 2 finalizada. La implementación de Application queda pendiente de autorización.
