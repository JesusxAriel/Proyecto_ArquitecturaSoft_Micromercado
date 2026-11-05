> EVIDENCIA ANTERIOR: describe el entorno original o perfiles retirados. Para instalacion nueva usar [README vigente](../../README.md), con SQL publicable y credenciales propias.

# Frontend Razor: consultas reales con sesión local

## Creación de clientes

Servicio1.Ventas es responsable de crear y consultar clientes. `POST /api/clientes`
acepta `nitCi` (hasta 30 caracteres) y `razonSocial` (hasta 150 caracteres), ambos
normalizados con `Trim`. La política `OperarVentas` permite la operación a
Administrador y Cajero. El adaptador MySQL escribe `idUsuarioCreacion` usando el
`sub` del JWT a través de `IUsuarioActual`; no existe un ID fijo ni una escritura
a otra base de datos. El índice `UQ_Cliente_nitCi_activo` se traduce a HTTP 409.

En `/Sales`, el botón `Nuevo cliente` abre un modal protegido por antiforgery. El
frontend envía la solicitud al handler Razor, que llama al cliente HTTP server-side;
el JWT nunca se entrega al navegador. Tras un 201, el cliente se selecciona
automáticamente y el carrito de productos permanece intacto. El formulario bloquea
dobles envíos y muestra errores de validación, duplicado, sesión o conexión.

La comprobación contra MySQL Demo permanece pendiente de autorización: las pruebas
actuales usan dobles y no ejecutan `INSERT` en esa base.

Fecha: 2026-11-04. Resultado: **búsquedas de productos/clientes, consulta de precio/stock y registro de ventas conectados mediante la API**. Anulación de ventas permanece deshabilitada.

## Cómo iniciar y entrar

API y frontend quedaron activos al terminar. No iniciar una segunda instancia si los puertos están ocupados. Para reiniciarlos, detener sus consolas con Ctrl+C. Con MySQL Demo ya iniciado, abrir dos consolas PowerShell desde `Servicio1.Ventas/`.

Primero, API original mediante el lanzador local:

```powershell
dotnet run --project .\tools\PuntoMarket.Ventas.DemoLocal\PuntoMarket.Ventas.DemoLocal.csproj --no-build --no-launch-profile
```

Después, Razor:

```powershell
dotnet run --project .\frontend\PuntoMarket.Ventas.Frontend\PuntoMarket.Ventas.Frontend.csproj --no-build --no-launch-profile -- --environment Development --urls http://localhost:5100
```

Abrir **http://localhost:5100/Sales**. Sin sesión, redirige a **/Demo**. Pulsar **Entrar como Cajero de demostración**. No se solicita ni inventa una contraseña de cajero1: es una identidad local de Development explícita, no el login definitivo de Usuarios.

Buscar `leche`, seleccionar Leche Entera PIL 1L para consultar precio y stock vigentes; buscar `JUAN` o su NIT para seleccionar el cliente existente. Agregar el producto construye un detalle en memoria y permite ajustar cantidades. Detenerse antes de pulsar Confirmar venta para no escribir en MySQL Demo.

Antes de recompilar, detener el frontend en Windows para liberar DLL/apphost. La compilación inicial detectó una instancia anterior bloqueando esos archivos; se detuvo únicamente esa instancia y el lanzador Demo identificado. La compilación final terminó correctamente. Reiniciar Razor invalida las sesiones locales y obliga a entrar de nuevo en /Demo.

## Cliente tipado y handlers

Se añadió `Clients/VentasApiClient.cs`: IVentasApiClient, DTOs propios del frontend y cliente tipado registrado con IHttpClientFactory. Dispone de consultas GET y del POST de registro. No referencia Domain, Application, Infrastructure ni el proyecto API; consume sus contratos HTTP JSON. No consulta MySQL directamente.

| Handler Razor GET | API consumida |
| --- | --- |
| /Sales?handler=BuscarProductos&termino=... | /api/productos?termino=... |
| /Sales?handler=BuscarClientes&termino=... | /api/clientes?termino=... |
| /Sales?handler=Precio&id=... | /api/productos/{id}/precio |
| /Sales?handler=Stock&id=... | /api/productos/{id}/stock |
| /Sales?handler=RegistrarVenta | /api/ventas |

El cliente usa timeout de diez segundos, rutas fijas, filtros escapados y Authorization por petición. No comparte DefaultRequestHeaders con tokens. No sigue redirecciones ni utiliza cookies contra la API. El destino de esta fase se limita a loopback HTTP 5002; no hay CORS ni JWT en el navegador.

El PageModel conserva la apariencia/estructura existente y añade autorización Cajero, validación de filtros (no vacíos, máximo 150 caracteres) e identificadores positivos. El JavaScript de Ventas se adaptó para búsquedas con debounce de 300 ms, cancelación de búsquedas obsoletas y renderizado mediante textContent. No usa innerHTML con resultados. La interfaz exige dos caracteres para iniciar una búsqueda.

Se reutilizan estilos, logo y presentación original. El bloque de detalle permite cantidades positivas dentro del stock consultado, selección de cliente y confirmación. La creación de clientes usa `POST /api/clientes` y la anulación se ofrece solo a Administrador. RegistrarVenta exige antiforgery, evita dobles envíos, muestra identificador/total y consulta nuevamente el stock; Historial consulta la API real, ofrece detalle y enlaza comprobantes.

## Seguridad de sesión y JWT

- `FrontendBootstrap` habilita Demo solo cuando IsDevelopment() y DemoLocal:Enabled=true. Fuera de Development, /Demo devuelve 404 aunque se active el flag. El perfil local y appsettings.Development.json activan la función de manera explícita.
- Acceso de demostración limitado a loopback por middleware; AllowedHosts sigue restringido a localhost/loopback. No habilitar este modo para exposición remota o producción.
- El lanzador API crea, si falta, `$env:USERPROFILE/puntomarket-demo-secrets/jwt-demo-key.txt`, fuera del repositorio y heredando la ACL privada existente. No sobrescribe claves existentes. API y Razor leen la misma clave sin emitirla. Esto sustituye la clave exclusivamente efímera del lanzador de la fase anterior; permite procesos separados sin copiar tokens manualmente.
- La clave no es la contraseña MySQL ni uno de los hashes de USUARIO. No se modifican esos secretos, los usuarios o sus permisos.
- Cada entrada local crea un identificador aleatorio nuevo y un registro independiente en IMemoryCache. El ticket de autenticación protegido contiene identidad y un ID de sesión, **no el JWT**. La cookie es HttpOnly, SameSite=Strict, de sesión del navegador y con Secure cuando se utiliza HTTPS. HTTP se admite únicamente para el entorno local solicitado.
- El JWT HS256 se genera y conserva exclusivamente en el registro de memoria del servidor para esa sesión. Contiene sub=2, unique_name=cajero1, role=Cajero y debeCambiarPassword=false, emisor/audiencia locales compatibles con el lanzador. Dura cinco minutos y se renueva en el servidor dentro de la sesión absoluta de treinta minutos. No hay refresh token ni endpoint público de emisión.
- Salir elimina el registro de memoria y la cookie. Reiniciar Razor elimina sesiones y el anillo de DataProtection en memoria. El aviso genérico de DataProtection sobre ausencia de cifrador XML no supone que se haya guardado un anillo en disco: ClavesTemporales conserva elementos en memoria.
- No se exponen tokens en JSON, HTML, JavaScript, localStorage, URLs o logs. El cliente HTTP elimina loggers de IHttpClientFactory; no se muestran secretos en las comprobaciones.
- Login y logout son POST con antiforgery automático de Razor. Las lecturas GET no cambian datos. No se desactivan antiforgery, JWT ni políticas de la API.

Este modo da acceso a cualquier usuario local que pueda abrir la demostración; no verifica credenciales personales ni representa autenticación de producción. El JWT conserva el rol Cajero existente de la API y se usa para la operación de venta solo desde el servidor. La sesión y la clave compartida deberán sustituirse por la integración acordada con Usuarios en otra fase.

## Manejo de errores

401 solicita volver a entrar a Demo; 403 informa falta de permisos; 404 indica recurso inexistente; 409 pide repetir la consulta; 503 informa indisponibilidad. Timeout, conexión caída, JSON inválido y errores técnicos inesperados se normalizan a 503 sin transmitir cuerpos internos de la API. Filtros e identificadores inválidos devuelven 400. Los handlers AJAX sin sesión devuelven 401 JSON, no HTML de login como si fueran datos. Las páginas protegidas redirigen a Demo cuando no hay sesión.

## Pruebas automatizadas

Se añadió referencia de Tests al frontend y `Frontend/ConsultasRazorTests.cs`; no se alteraron los casos anteriores ni se añadieron paquetes. Las pruebas nuevas usan claves aleatorias y un handler HTTP aislado de fixture: nunca sustituyen el cliente de ejecución real ni acceden a MySQL.

Doce casos nuevos:

- Seis variantes de mapeo HTTP: 401, 403, 404, 409, 503 y normalización de 500 a 503.
- Separación de sesiones/JWT y logout sin afectar otra sesión.
- Modo deshabilitado impide crear sesiones.
- Renovación del JWT a los seis minutos y expiración de sesión a los treinta minutos.
- Flujo Razor real sobre Kestrel: acceso anónimo 401, antiforgery obligatorio, entrada local, cookie HttpOnly/SameSite Strict, HTML sin JWT, consulta, filtro inválido, POST sin token rechazado, CrearCliente 405, logout y aislamiento de cliente anónimo.
- Cliente tipado: POST de venta con autorización por petición en servidor y respuesta de identificador/total.
- Production no ofrece acceso Demo aunque el flag esté activado.

- Restore, build y test de PuntoMarket.Ventas.slnx completados. El frontend se compila transitivamente desde Tests; el lanzador se compiló aparte. Compilación final: **0 errores, 0 advertencias**. Suite final: **281 correctas, 0 fallidas, 9 omitidas, total 290**. Las 9 omitidas son las integraciones MySQL condicionadas al entorno. Las pruebas JavaScript de reglas terminaron **51/51**; no se conectaron a 3308 ni a Demo.

## Verificación real y navegador

El lanzador repitió sus 13 GET autenticados reales contra MySQL Demo, incluidos 401/403 y cotejo directo con datos SQL. API usa los repositorios originales de Infrastructure, sin sustituciones.

Se abrió /Sales en el navegador integrado, se entró mediante el formulario local con antiforgery, se buscó `leche`, se consultó Leche Entera PIL 1L y se buscó/seleccionó JUAN PEREZ VARGAS. Resultado visible: **precio 6,50, stock 120**, cliente real y detalle en memoria listo para confirmar. No se pulsó Confirmar venta; por tanto, no se registró ninguna venta real. Se mantuvieron logo, colores y tarjetas de la interfaz original. La sesión observada antes de la compilación final quedó invalidada por el reinicio; volver a entrar es el comportamiento esperado.

Además se añadió y ejecutó `database/demo-env/Verificar-FrontendLecturas.ps1`: cliente HTTP con cookie independiente, sin JWT en el navegador ni acceso a la clave. Verificó lecturas reales; no se añadió una ejecución de POST de venta para mantener la base Demo intacta:

| Comprobación | Resultado |
| --- | --- |
| Handler de productos sin sesión | 401 |
| GET /Demo, POST de sesión con antiforgery y GET /Sales | 200 / 302 / 200 |
| BuscarProductos (leche) | 200, incluye producto 1 |
| Precio (id 1) | 200, 6.50 |
| Stock (id 1) | 200, 120 |
| BuscarClientes por JUAN y NIT original | 200, cliente 2 |
| Precio de ID inexistente | 404 |
| Búsqueda sin coincidencias | 200, lista vacía |
| HTML de /Sales | Sin JWT ni Bearer |
| CHECKSUM TABLE EXTENDED antes/después | Las 11 tablas permanecen idénticas |

Para repetir explícitamente esta comprobación local desde Servicio1.Ventas, con ambos procesos activos:

```powershell
.\database\demo-env\Verificar-FrontendLecturas.ps1
```

El script solo hace POST para la sesión local; no registra ventas, no crea clientes ni ejecuta SQL de escritura. Espera las semillas originales y fallará si operaciones futuras autorizadas cambian el precio/stock de referencia.

## Alcance e integridad

Archivos cambiados: frontend Program/FrontendBootstrap, cliente/DTOs, sesiones, páginas Demo, PageModel/vista/JavaScript de Sales, layout, perfil y configuración Development; referencia y archivo nuevo de pruebas; lanzador Demo para cargar la clave privada compartida; verificador y documentación. No se modifica Domain, Application, Infrastructure, endpoints o políticas de la API, monolito, SQL, red/volumen de integración o datos Demo. No se ejecutaron ventas ni anulaciones, ni se hicieron commits o push.

Pendientes: integración definitiva Usuarios y registro de clientes por acuerdo entre equipos. La anulación de ventas requiere una sesión Administrador válida; la sesión Demo disponible continúa siendo únicamente Cajero.
## Registro de ventas desde Razor

La página `/Sales` mantiene el JWT exclusivamente en el servidor: el cliente Razor lo obtiene desde la sesión Cajero demo y lo agrega al `POST /api/ventas` mediante `VentasApiClient`. El navegador solo envía el cookie de sesión y el token antiforgery en el encabezado `RequestVerificationToken`.

El detalle valida cliente, cantidades y stock informativo en el navegador, pero el precio, stock definitivo y FEFO se validan únicamente en Application/Domain/Infrastructure. La respuesta muestra el identificador y total de la venta; después consulta nuevamente el stock. Los conflictos de precio, stock, autorización y sesión se muestran sin exponer tokens ni detalles internos.

La comprobación manual debe detenerse antes de pulsar «Confirmar venta» si no se desea escribir en MySQL Demo.

El campo opcional «Con cuánto paga» funciona solo en el navegador: acepta efectivo con hasta dos decimales, exige que el importe cubra el total, calcula el cambio en centavos y no modifica el total ni se envía al API. El importe y el cambio no se guardan en MySQL porque el modelo actual no contempla esos datos.

## Anulación desde el historial

La API ya existente conserva la autorización exclusiva para el rol Administrador mediante la política `AnularVentas`. La fachada existente restaura las cantidades en los lotes originales, registra motivo y usuario de anulación dentro de la misma transacción y rechaza una segunda anulación sin modificar nuevamente el stock.

El frontend expone el botón `Anular` y su modal únicamente cuando la sesión Razor tiene rol Administrador. El modal exige confirmación explícita, motivo de hasta 255 caracteres, antiforgery y bloquea envíos duplicados. El handler Razor llama al cliente HTTP server-side; este adjunta el JWT solo en el servidor a `POST /api/ventas/{id}/anular`. Después de una respuesta exitosa se recarga el historial y la venta aparece como `ANULADA`, sin ofrecer otra anulación.

La sesión Demo local de esta fase solo crea usuarios Cajero, por lo que no se habilitó un acceso Administrador artificial ni se ejecutó una anulación real. La opción no aparece visualmente para Cajero; la autorización real de Administrador queda pendiente de una sesión válida del microservicio Usuarios.

## Historial y comprobantes reales

`/Sales/Historial` consulta `GET /api/ventas` mediante el cliente HTTP del frontend y conserva el JWT únicamente en el servidor. Cada venta ofrece «Ver comprobante», que abre `/Comprobante/{idVenta}` en una pestaña nueva. La página Razor protegida solicita `GET /api/ventas/{idVenta}/comprobante/html` y devuelve el HTML generado por el comprobante existente del microservicio; no duplica datos, lógica de importes ni acceso a MySQL.

El HTML incluye número, fecha, cliente, productos, cantidades, precios unitarios, importes, total, total en letras, cajero y el botón «Imprimir Comprobante». El ID de ruta se utiliza directamente en la petición al API, por lo que un comprobante no se resuelve desde otra venta. Errores 401/403/404/503 se traducen a mensajes HTML sin exponer cuerpos internos ni tokens. La comprobación visual mostró los comprobantes #1 (Bs 95,50) y #2 (Bs 360,00), coincidentes con historial y detalle.
