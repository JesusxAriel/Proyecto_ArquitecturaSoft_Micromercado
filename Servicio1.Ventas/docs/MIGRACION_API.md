# Migración de API REST y JWT — Etapa 5

Fecha: 2026-11-04. API ASP.NET Core net10.0; puerto HTTP previsto: 5002.

## 1. Inspección y endpoints implementados

Se revisaron los contratos, fachadas, DTOs y excepciones de Application/Domain, PersistenciaVentasMySql, MySqlUnidadDeTrabajo, la configuración previa de Api y MIGRACION_APPLICATION.md/MIGRACION_INFRASTRUCTURE.md. También se consultó la autorización de Pages/Sales/Historial.cshtml.cs del monolito: Administrador o Cajero para consultas.

No se modificaron Application, Domain ni Infrastructure. Las acciones HTTP llaman a IFachadaVenta, IFachadaAnulacion e IVentaConsultaService reales.

| Método y ruta | Operación | Respuesta correcta |
| --- | --- | --- |
| POST /api/ventas | Registrar venta | 201, ResultadoVentaDto y Location |
| POST /api/ventas/{id}/anular | Anulación lógica/restauración | 204 sin cuerpo |
| GET /api/ventas?incluirAnuladas=false | Historial, filtro booleano opcional | 200, lista de VentaResumenDto |
| GET /api/ventas/{id} | Detalle agrupado y auditoría | 200, VentaDetalleDto |
| GET /api/ventas/total-dia | Total del día | 200, objeto con total |
| GET /health | Salud del proceso, sin dependencias externas | 200, status=ok |

El límite de historial sigue siendo 50, decidido por Application. No se agregaron filtros de fecha, paginación, CRUD ni consultas inventadas.

### Ejemplos de JSON ilustrativos

Registro:

~~~json
{
  "idCliente": 3,
  "lineas": [
    { "idProducto": 1, "cantidad": 5, "precioEsperado": 10.00 }
  ]
}
~~~

Respuesta ilustrativa de registro:

~~~json
{ "idVenta": 42, "total": 50.00 }
~~~

Location apunta a /api/ventas/42, generado con la URL del host. PrecioEsperado es opcional y mantiene la validación original de Application.

Anulación:

~~~json
{ "motivo": "Error de digitación" }
~~~

Historial ilustrativo:

~~~json
[
  {
    "id": 42,
    "nroComprobante": "V-20261104-0042",
    "fechaHora": "2026-11-04T10:00:00",
    "nombreCliente": "Cliente",
    "estado": "COMPLETADA",
    "total": 50.00
  }
]
~~~

El detalle conserva cabecera, NIT/CI, razón social, usuario cajero, estado, total, motivo/fecha/usuario de anulación y lineas. Cada línea contiene cantidad, descripcion, precioUnitario, subtotal y lotes con codigoLote/cantidad, exactamente como el DTO actual.

Total del día y salud:

~~~json
{ "total": 50.00 }
~~~

~~~json
{ "status": "ok" }
~~~

No existen datos de ejemplo en los adaptadores productivos; todos estos JSON son documentación. Las respuestas de negocio en producción proceden de MySQL mediante Application.

## 2. Configuración y validación JWT

Api incorpora Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12 y sus dependencias transitivas. No se añadió ningún paquete a Application/Domain/Infrastructure.

La validación configura explícitamente:

- Firma obligatoria con clave simétrica externa y algoritmo permitido únicamente HS256.
- Validación de issuer, audience, expiración y vigencia temporal.
- RequireExpirationTime y RequireSignedTokens activados.
- ClockSkew cero: los relojes de los servicios deben estar sincronizados.
- MapInboundClaims=false, NameClaimType=unique_name y RoleClaimType=role.
- IncludeErrorDetails=false y desafíos/denegaciones con ProblemDetails seguros.

Contrato previsto aplicado:

| Claim | Interpretación |
| --- | --- |
| sub | Un único entero positivo, identificador real de usuario |
| unique_name | Un único nombre de usuario no vacío |
| nombre | Nombre mostrado; opcional en esta versión y no usado para sustituir el id |
| role | Un único rol no vacío, con el nombre exacto acordado |
| debeCambiarPassword | Un único valor booleano true/false; se admiten sus representaciones de claim booleano o texto parseable |
| exp | Obligatorio; validado por el middleware |

Claims obligatorios inválidos/ausentes producen 401. Un usuario que deba cambiar contraseña se autentica pero las políticas de ventas lo rechazan con 403. No se aceptan 0/1 como sustitutos del booleano ni múltiples roles/sub ambiguos.

Si faltan conjuntamente los tres valores Jwt, la API puede arrancar y responder salud, pero rechaza todos los tokens en los endpoints protegidos. No se genera una clave por defecto. Una configuración parcial o clave menor que 32 bytes UTF-8 provoca un error de configuración al arrancar, sin mostrar el valor.

No hay emisión de JWT, login, refresco de tokens ni administración de usuarios en Ventas. Los tokens generados en Tests usan claves aleatorias efímeras exclusivas de cada host de prueba; no son tokens de producción.

Referencia técnica consultada: [documentación oficial de JWT Bearer de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) y [paquete oficial usado](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.12).

## 3. Roles y autorizaciones

- Política OperarVentas: Administrador o Cajero, autenticados y sin cambio obligatorio de contraseña.
- Política AnularVentas: Administrador, autenticado y sin cambio obligatorio de contraseña.
- Registro, historial, detalle y total diario usan OperarVentas.
- Anulación exige además AnularVentas.
- FallbackPolicy exige autenticación para evitar nuevas rutas anónimas accidentales.
- /health se marca AllowAnonymous deliberadamente.

La política de consultas se conserva del monolito; queda sujeta a ratificación con los demás equipos. Los nombres de roles distinguen mayúsculas. El frontend no determina la autorización.

## 4. Adaptador de IUsuarioActual

Security/UsuarioActualDesdeJwt.cs es un adaptador scoped de HTTP a IUsuarioActual:

- Id procede únicamente de sub validado.
- NombreUsuario procede de unique_name.
- Rol procede de role.
- Sin usuario autenticado o id válido, falla sin asumir un usuario por defecto.

Los cuerpos de entrada no tienen idUsuario, idUsuarioAnulacion ni idCajero. La deserialización rechaza propiedades desconocidas, por lo que intentar enviarlas produce 400. Las fachadas reciben exclusivamente IUsuarioActual desde el contexto autenticado.

Se verificó mediante HTTP que el sub 423 se guarda como usuario del registro, y que una anulación posterior con sub 99 guarda 99 como anulador sin cambiar el cajero original.

## 5. Composición de dependencias

ApiBootstrap centraliza la composición:

| Lifetime | Servicio |
| --- | --- |
| Singleton | DatabaseConnection como fábrica inmutable, TimeProvider.System y DetectorDeInterbloqueoMySql |
| Scoped | PersistenciaVentasMySql |
| Scoped, misma sesión | IUnidadDeTrabajo, IVentaRepository, ILoteRepository, IConPrecioVigente, IClienteActivoConsulta, IVentaConsultaRepository |
| Scoped | IFachadaVenta, IFachadaAnulacion, IFachadaActualizacionStockProducto, IVentaConsultaService e IUsuarioActual |

Las fábricas de puertos resuelven propiedades de la misma PersistenciaVentasMySql. No construyen unidades independientes por repositorio.

DatabaseConnection se instancia de forma diferida, cuando una operación necesita persistencia. /health y rechazos de autenticación/autorización no resuelven ni abren MySQL. Si una solicitud autorizada necesita persistencia sin configuración, recibe 503 seguro.

Tests sustituye únicamente puertos de persistencia controlados, después de los registros de producción. El pipeline HTTP, autenticación JWT, políticas, controladores, fachadas y servicio de consulta permanecen reales. El punto de personalización de ApiBootstrap se usa en código del host de prueba; no existe un endpoint para cambiar configuración o DI.

## 6. Sesiones, transacciones y arranque independiente

Cada solicitud de negocio tiene su scope y una sesión. Los repositorios participantes usan la misma unidad; Application conserva inicio, confirmación, rollback y reintento de deadlocks.

El contenedor dispone los servicios scoped asíncronamente al terminar la solicitud. DisposeAsync de MySqlUnidadDeTrabajo es idempotente, también cuando el contenedor dispone sus alias y la sesión. Las pruebas comprueban identidad de los puertos/unidad en un scope, separación entre scopes y disposición. Las dos solicitudes HTTP concurrentes de prueba usan sesiones distintas.

No se modificó la implementación transaccional de Infrastructure. Las pruebas HTTP no demuestran commit o rollback real MySQL: sus unidades de prueba observan la coordinación, sin implementar atomicidad.

El arranque predeterminado usa 0.0.0.0:5002 cuando no se configura otra URL/puerto. launchSettings.json también prepara localhost:5002. Para respetar explícitamente variables del entorno al usar dotnet run, utilizar --no-launch-profile.

MVC registra servicios de Data Protection aunque esta API no utiliza cookies. Se configuró un repositorio temporal en memoria para sus claves internas, para no leer/escribir el anillo del perfil del usuario de Windows. No contiene la clave JWT ni persiste claves en archivos. El aviso de ausencia de cifrador XML se refiere a este repositorio volátil, no a almacenamiento en disco. El logging de la API usa consola y no depende de permisos del registro de eventos de Windows.

## 7. Manejo de errores y validación

Se utiliza ProblemDetails con application/problem+json. Los fallos manejados incluyen status, title, instance y traceId; errores de negocio conocidos añaden code. La validación de modelos añade errors mediante ApiController.

| Situación | HTTP |
| --- | ---: |
| Sin token, firma/issuer/audience/algoritmo/claims/expiración inválidos | 401 y WWW-Authenticate: Bearer |
| Rol no autorizado o contraseña pendiente | 403 |
| JSON inválido, ids/cantidades no positivos, líneas vacías/nulas, propiedades desconocidas, motivo vacío o mayor que 255 | 400 |
| Detalle inexistente, cliente/producto inexistente o inactivo | 404 |
| Stock insuficiente, precio cambiado, restauración excedida, venta no anulable | 409 |
| Conflictos MySQL conocidos 1213/1205/1062/1452/3819/1644 | 409 genérico |
| Fallo de DbException no clasificado o configuración MySQL pendiente | 503 |
| Error no reconocido | 500 genérico |

Precio cambiado incluye idProducto y precioVigente, sin mensajes técnicos. Ejemplo ilustrativo:

~~~json
{
  "type": "https://httpstatuses.com/409",
  "title": "El precio vigente cambió. Actualice el detalle de venta.",
  "status": 409,
  "instance": "/api/ventas",
  "traceId": "identificador-de-solicitud",
  "code": "precio_cambiado",
  "idProducto": 1,
  "precioVigente": 10.00
}
~~~

Application actualmente comunica algunas condiciones mediante InvalidOperationException y mensajes concretos. Api reconoce una lista cerrada de esos mensajes, sin devolver el mensaje de una excepción arbitraria. El error de anulación «no existe o ya fue anulada» permanece ambiguo: devuelve 409, porque el caso de uso no permite distinguir las dos condiciones sin modificarlo.

Los modelos HTTP añaden validación de formato y longitud de motivo compatible con la UI/schema originales; no cambian reglas de FEFO, precios o ventas. Las excepciones MySQL no se devuelven directamente. No se incluyen stack traces, cadenas de conexión, contraseñas ni SQL en las respuestas.

## 8. Pruebas y resultados ejecutados

Archivos de prueba nuevos:

- tests/PuntoMarket.Ventas.Tests/Api/ApiTestHost.cs.
- tests/PuntoMarket.Ventas.Tests/Api/VentasHttpTests.cs.

Las pruebas usan Kestrel real en loopback y puerto efímero con HttpClient; no se añade TestServer/WebApplicationFactory ni un esquema falso de autenticación. Los JWT pasan por el validador Bearer real. Los dobles viven únicamente en Tests y no conectan a MySQL.

| Grupo | Correctas | Fallidas | Omitidas |
| --- | ---: | ---: | ---: |
| Pruebas previas de etapas 2 a 4 | 102 | 0 | 0 |
| HTTP de Ventas, JWT y políticas | 47 | 0 | 0 |
| Composición, salud e identidad | 4 | 0 | 0 |
| Integración MySQL previa | 0 | 0 | 9 |
| Total descubierto | 153 | 0 | 9 |

Se probaron los cinco endpoints sin token, firma incorrecta, expiración, issuer/audience incorrectos, algoritmo HS384 rechazado, ausencia de exp, sub inválido y falta del indicador de contraseña.

Se probaron 403 por roles, mayúsculas incorrectas y contraseña pendiente; registro de Administrador/Cajero; anulación exclusiva de Administrador y segunda anulación; auditoría del sub; consultas JSON, filtros/límite, validación de cuerpos/rutas, precio/stock, entidades no disponibles, errores técnicos sin filtración de información y aislamiento/disposición de scopes.

Las nueve integraciones MySQL siguen explícitamente omitidas por falta de base aislada autorizada. No se contaron como correctas ni se ejecutó SQL.

## 9. Endpoints pendientes y contratos faltantes

Se inspeccionaron los puertos actuales antes de decidir:

| Necesidad | Situación actual | Pendiente |
| --- | --- | --- |
| Buscar productos | No hay puerto de búsqueda en Application migrado | Contrato mínimo de búsqueda y adaptador real |
| Mostrar precio por HTTP | IConPrecioVigente existe para registrar ventas, con FOR UPDATE y transacción obligatoria en MySQL | Caso de consulta/alcance transaccional acordado antes de exponerlo |
| Disponibilidad de stock por producto | ILoteRepository expone lotes bloqueados para consumo, no consulta de stock visible | Puerto/caso de consulta de disponibilidad sin reutilizar indebidamente descuento |
| Buscar clientes | IClienteActivoConsulta solo verifica id activo | Contrato mínimo de búsqueda y acuerdo de propiedad con Usuarios |
| Comprobante | Solo se genera su número al registrar; no se migró el servicio de comprobantes | Decisión de alcance y contratos |

No se añadieron endpoints simulados ni se copiaron CRUD completos. Ninguno de los cinco endpoints obligatorios requiere ajustar Application, por lo que se preservó completamente.

## 10. Configuración y acuerdos pendientes

Variables exactas del entorno:

| Variable | Uso |
| --- | --- |
| ConnectionStrings__Ventas | Cadena MySQL externa |
| Jwt__Key | Secreto HS256 como texto UTF-8, mínimo 32 bytes; no se decodifica como Base64 |
| Jwt__Issuer | Emisor exacto acordado con Usuarios |
| Jwt__Audience | Audiencia exacta acordada con Usuarios |
| ASPNETCORE_URLS | URL HTTP; ejemplo http://0.0.0.0:5002 |
| ASPNETCORE_HTTP_PORTS | Alternativa estándar de puerto cuando no se configura URL; ejemplo 5002 |

Ejemplo de estructura, con marcadores que deben sustituirse externamente y nunca guardarse como secretos reales:

~~~text
ConnectionStrings__Ventas=Server=<host>;Port=3306;Database=<base>;User ID=<usuario>;Password=<secreto externo>;
Jwt__Key=<secreto externo compartido con Usuarios de al menos 32 bytes UTF-8>
Jwt__Issuer=<issuer acordado con Usuarios>
Jwt__Audience=<audience acordada con Usuarios>
ASPNETCORE_URLS=http://0.0.0.0:5002
~~~

Los marcadores no son configuración ejecutable lista para producción. La API no carga automáticamente archivos .env. No hay claves en appsettings.json ni launchSettings.json.

Arranque sin MySQL, desde Servicio1.Ventas:

~~~powershell
dotnet run --project src/PuntoMarket.Ventas.Api/PuntoMarket.Ventas.Api.csproj --no-build --no-restore --no-launch-profile
~~~

GET /health es liveness del proceso, no readiness de base de datos ni validación de que JWT esté configurado. Sin configuración JWT, las rutas protegidas responden 401. Con JWT correcto pero sin cadena MySQL, una operación protegida responde 503.

Acuerdos necesarios:

- Confirmar issuer/audience, formato exacto del secreto y su rotación con Usuarios.
- Confirmar tipo de sub, unicidad de role, uso de unique_name/nombre y representación de debeCambiarPassword. Esta versión rechaza tokens incompatibles en lugar de asumir valores.
- Confirmar la política de consultas Administrador/Cajero y la restricción de contraseña pendiente.
- No se verificó compatibilidad con tokens reales del Servicio 2.
- No hay consulta en línea de usuario activo ni revocación de JWT; la vigencia de identidad depende del emisor y la expiración.
- Acordar rutas, errores, CORS/orígenes y transporte HTTPS para la integración del frontend. No se habilitó CORS abierto ni integración con otros servicios.
- La configuración JWT se fija al construir el host; la fábrica de conexión queda fija al resolverse. Coordinar reinicio/rotación.
- No hay idempotencia HTTP nueva ni garantías transaccionales verificadas contra MySQL. Persisten los riesgos documentados en MIGRACION_INFRASTRUCTURE.md.
- La clasificación por mensajes de InvalidOperationException es deliberadamente limitada; futuros cambios deberían usar errores tipados acordados, sin modificar Application en esta etapa.

## 11. Validación final

Desde Servicio1.Ventas:

~~~powershell
dotnet restore PuntoMarket.Ventas.slnx
dotnet build PuntoMarket.Ventas.slnx --no-restore
dotnet test tests/PuntoMarket.Ventas.Tests/PuntoMarket.Ventas.Tests.csproj --no-restore
~~~

Resultado final de los tres comandos: código 0. Build: 0 errores y 1 advertencia NU1900 de Infrastructure, por consulta de vulnerabilidades/índice NuGet no disponible. Tests: 153 correctas, 0 fallidas, 9 omitidas, 162 descubiertas.

La primera restauración de JWT falló por SSL/NU1301. Se completó con acceso ampliado autorizado a NuGet. La restauración final ordinaria terminó correctamente. No se deshabilitó la auditoría de paquetes.

Comprobación adicional con el proceso independiente en puerto 5002, sin configuración de MySQL/JWT:

- Kestrel anunció http://0.0.0.0:5002.
- GET http://127.0.0.1:5002/health devolvió 200 y {"status":"ok"}.
- GET /api/ventas sin token devolvió 401, WWW-Authenticate: Bearer y ProblemDetails.
- El proceso de verificación fue detenido.

Referencias: Domain no tiene referencias externas; Application solo referencia Domain; Infrastructure referencia Application/Domain; Api conserva referencias a Application/Infrastructure y añade JWT Bearer; Tests añade referencia a Api. No hay dependencias circulares ni referencias al proyecto del monolito.

## 12. Integridad y archivos entregados

API: ApiBootstrap.cs, Program.cs, Contracts/VentaSolicitudes.cs, Controllers/VentasController.cs, Errors/ApiExceptionHandler.cs, Security/SeguridadJwt.cs, Security/UsuarioActualDesdeJwt.cs, Security/ClavesTemporales.cs y Properties/launchSettings.json, además de la referencia de paquete en el csproj.

Tests: dos archivos nuevos bajo Api/ y referencia al proyecto Api. Documentación: este informe y actualización del README del servicio.

Se compararon huellas SHA-256 de los 652 archivos originales fuera de Servicio1.Ventas: 0 modificados, 0 eliminados y 0 archivos añadidos fuera de la carpeta autorizada del proyecto.

Solo se crearon o editaron archivos de proyecto dentro de Servicio1.Ventas. No se modificaron monolito, sus pruebas, SQL original, frontend, Docker Compose ni Dockerfile originales. No se implementaron Usuarios/Reportes ni se emitieron tokens de producción. No se ejecutó SQL, no hubo conexiones a MySQL, contenedores, commits, ramas ni push.

Etapa 5 finalizada. No se realizó integración real entre microservicios ni Docker Compose definitivo.
