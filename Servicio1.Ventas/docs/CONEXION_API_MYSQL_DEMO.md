# API Ventas conectada a MySQL Demo: lecturas reales

Actualización posterior: la integración de consultas Razor y la clave privada local compartida se documentan en FRONTEND_CONSULTAS_REALES.md. Este informe conserva el resultado de la fase anterior.

Fecha: 2026-11-04. **Resultado: conexión y consultas HTTP reales verificadas; datos Demo sin cambios.**

## Configuración inspeccionada

`src/PuntoMarket.Ventas.Api/ApiBootstrap.cs` obtiene `ConnectionStrings:Ventas` mediante IConfiguration y crea DatabaseConnection. La configuración original no tiene una conexión predeterminada ni credenciales en appsettings. `DatabaseConnection` utiliza MySqlConnectionStringBuilder y exige nombre de base. Las políticas OperarVentas y AnularVentas y la validación HS256/issuer/audience/expiración/claims siguen intactas.

Se revisaron ProductosController, ClientesController, SeguridadJwt, los contratos/servicios de consulta de Application y los adaptadores MySqlProductoVentaConsultaRepository y MySqlClienteVentaConsultaRepository. Las consultas de estos adaptadores son SELECT parametrizados, sin FOR UPDATE ni escrituras. El stock informativo se obtiene de lotes activos con cantidad positiva y vencimiento nulo o vigente, no de resultados simulados.

## Configuración local implementada

Se añade `tools/PuntoMarket.Ventas.DemoLocal/` con csproj, Program.cs, README.md y exclusiones bin/obj. Es un lanzador de la API original a través de ApiBootstrap, no un servicio alternativo ni una API de prueba. No se modifican código productivo, appsettings, launchSettings ni la solución original del servicio.

Lee `database/demo-env/.env` y su archivo de contraseña externo. Destino fijo: **127.0.0.1:3308 / puntomarket_ventas_demo_local**, cuenta ventas_demo, SslMode Required. Construye la cadena solo en memoria y la entrega al proveedor de configuración del proceso como ConnectionStrings:Ventas; no se almacena ni muestra. La conexión MySQL con TLS requerido fue satisfactoria y se confirmaron DATABASE()/CURRENT_USER(). Required cifra sin validar identidad del servidor; el alcance es demostración local en loopback.

El proceso API se inició en http://localhost:5002 (IPv4 127.0.0.1 e IPv6 ::1). PID observado: **22136**, específico de esta ejecución y no garantía de identidad futura. Se dejó activo para su revisión. El lanzador rechaza otro proceso que ya ocupe 5002. Para un futuro arranque, usar el comando de su README cuando el puerto esté libre; no es necesario copiar secretos a la API.

No se añaden paquetes; el lanzador referencia el proyecto API y reutiliza sus dependencias. No entra en PuntoMarket.Ventas.slnx ni se ejecuta con los tests normales.

## JWT local conservando autorización

El lanzador genera una clave aleatoria de uso exclusivamente local, en memoria. Emisor y audiencia son exclusivos del host Demo. Los tokens HS256 de comprobación duran cinco minutos y nunca se imprimen ni guardan en archivos. Se utiliza la identidad original cajero1, sub 2, role Cajero y debeCambiarPassword=false para las consultas autorizadas.

No se deshabilitan autenticación, firma, expiración o políticas; no se introduce login ni endpoint de emisión. Se verifican además un JWT con firma incorrecta y otro firmado correctamente con un rol sin permisos. La clave no se publica ni se configura en el frontend; se pierde al terminar el proceso y se renueva al reiniciar. Esta solución temporal no sustituye el acuerdo con Usuarios.

El proveedor de logs se suprime solo en el host del lanzador, que emite resultados saneados. No se modificó el logging de la API ni se imprimieron cadenas completas, contraseñas, tokens o hashes.

## Verificación HTTP real

| GET | Contexto | Resultado |
| --- | --- | --- |
| /health | Anónimo | 200 |
| /api/productos?termino=leche | Sin JWT | 401 |
| /api/clientes?termino=JUAN | Sin JWT | 401 |
| /api/productos/1 | JWT con firma incorrecta | 401 |
| /api/productos/1 | JWT válido, rol Observador | 403 |
| /api/productos?termino=leche | JWT Cajero | 200, contiene producto original 1 |
| /api/productos/1 | JWT Cajero | 200, nombre/precio/stock coincidentes con MySQL |
| /api/productos/1/precio | JWT Cajero | 200, precio 6.50 |
| /api/productos/1/stock | JWT Cajero | 200, disponibilidad 120 |
| /api/clientes?termino=JUAN | JWT Cajero | 200, cliente original 2 |
| /api/clientes?termino=4545451226 | JWT Cajero | 200, mismo cliente por NIT |
| /api/productos/2147483647 | JWT Cajero | 404 |
| /api/productos?termino=ZZZ_SIN_COINCIDENCIAS_DEMO | JWT Cajero | 200, lista vacía |

Total: **13 solicitudes GET satisfactorias según su resultado esperado**. Comprobación adicional independiente posterior: /health 200 y /api/productos/1 sin JWT 401; socket de escucha confirmado solo en loopback.

El producto 1 es Leche Entera PIL 1L, precio 6.50 y stock vendible 120 al verificar. Los valores HTTP se cotejan en memoria con lecturas SQL reales de PRODUCTO/LOTE/CLIENTE. También se resuelven los servicios por DI y se exige que sus tipos concretos sean MySqlProductoVentaConsultaRepository y MySqlClienteVentaConsultaRepository. El lanzador no reemplaza registros de servicios ni conecta a 3307.

## Ausencia de cambios de datos

Se ejecutó CHECKSUM TABLE EXTENDED sobre las **11 tablas** antes y después de las consultas. Todos los checksums permanecieron iguales. No se ejecutó POST, PUT, PATCH, DELETE, INSERT, UPDATE, DDL, venta o anulación. Las únicas consultas directas fueron lecturas de identidad, datos para cotejo y checksums. No se accedió a bases del monolito o de integración.

La cuenta mantiene los permisos originales de importación: la comprobación fue de solo lectura por alcance del código y peticiones, no porque se hayan revocado permisos de escritura. No se cambiaron usuarios ni grants.

## Frontend: comprobación estática

Se revisaron Program.cs, _Layout.cshtml, los PageModels de Registro e Historial, vistas y scripts:

- Layout mantiene body data-presentation-only=true.
- Las dos funciones inicializadoras de ventas.js y la de historial.js retornan inmediatamente en este modo.
- PageModels solo tienen OnGet, sin handlers de escritura ni llamadas al API.
- El HttpClient con nombre VentasApi está registrado pero no se consume.
- Controles de confirmación y registro permanecen deshabilitados y PuedeAnular=false.

Hay código POST heredado dentro de los scripts copiados, pero es inalcanzable en este modo de presentación. No se activó ni eliminó. Esta revisión es estática; no se inició el frontend ni se hizo una prueba de navegador en esta fase. Tampoco se integró JWT o consultas gráficas en Razor.

## Restore, build y pruebas normales

Se ejecutaron restore, build y test de PuntoMarket.Ventas.slnx. Antes de las pruebas se retiraron únicamente del entorno del proceso las variables PUNTOMARKET_MYSQL_TESTS, PUNTOMARKET_MYSQL_TEST_CONNECTION y las de conexión/JWT; se restauró su estado previo al terminar. Las nueve integraciones quedaron omitidas por falta de autorización/configuración en ese proceso. No se cambió su código ni se apuntaron a Demo.

Resultado: **269 correctas, 0 fallidas, 9 omitidas (278 en total)**. Los tests HTTP existentes utilizan su aislamiento normal; no el proceso Demo. La comprobación operativa contra Demo descrita arriba es una ejecución separada, explícita y de solo lectura.

El primer restore dentro del sandbox mostró NU1900 al no poder consultar el índice de vulnerabilidades de NuGet. Se repitió fuera del sandbox con --force-evaluate y terminó correctamente. La compilación final de la solución y del lanzador terminó con **0 errores y 0 advertencias**. No se añadieron dependencias externas nuevas.

## Estado final y límites

La API original está conectada a MySQL Demo y responde a consultas reales autenticadas. Abrir un endpoint de negocio directamente en el navegador sin Bearer sigue devolviendo 401, como corresponde. /health no comprueba la base por sí solo; la conexión quedó demostrada por los GET autenticados y sus valores contrastados con SQL.

No se modificaron archivos del monolito, SQL, esquema/datos Demo, MySQL de integración, frontend o código productivo de la API, Application, Domain e Infrastructure. Solo se añadieron el lanzador local y este informe dentro de Servicio1.Ventas; restore/build generan sus artefactos normales. No se crearon commits ni push.

No se registraron ni anularon ventas. La integración gráfica y el mecanismo definitivo de autenticación quedan para una fase posterior autorizada.
