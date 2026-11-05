# OpenAPI y Swagger UI de PuntoMarket.Ventas

La inspección inicial no encontró AddOpenApi, AddSwaggerGen, Swagger UI ni paquetes de documentación en la API.
Se incorporó un solo generador: Swashbuckle.AspNetCore.SwaggerGen 10.2.3 y la UI
Swashbuckle.AspNetCore.SwaggerUI 10.2.3. SwaggerGen incorpora transitivamente Swagger y Microsoft.OpenApi;
no se agregó Microsoft.AspNetCore.OpenApi ni el metapaquete que incorpora herramientas de generación en build.
La versión utilizada ya estaba disponible en la caché local y dispone de destino net10.0.
Referencias: [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore),
[paquete SwaggerGen](https://www.nuget.org/packages/Swashbuckle.AspNetCore.SwaggerGen/10.2.3).

## Arranque y direcciones

Desde la raíz del trabajo, con el puerto 5002 libre:

```powershell
dotnet run --project .\Servicio1.Ventas\src\PuntoMarket.Ventas.Api\PuntoMarket.Ventas.Api.csproj --no-launch-profile -- --environment Development --urls http://localhost:5002
```

- UI: http://localhost:5002/swagger/index.html (también /swagger).
- Documento: http://localhost:5002/swagger/v1/swagger.json.
- Salud: http://localhost:5002/health.

No hace falta configurar JWT o MySQL para visualizar la documentación o consultar health.
Los endpoints protegidos siguen requiriendo configuración válida y autenticación.
La configuración privada existente se debe aportar mediante proveedores de configuración seguros del proceso;
no introducir claves de firma o cadenas de conexión en Swagger ni en comandos publicados.

El lanzador `tools/PuntoMarket.Ventas.DemoLocal` fija actualmente el entorno **LocalDemo**, por lo que
no expone Swagger. Cambiar solo ASPNETCORE_ENVIRONMENT no supera ese argumento explícito.
No se modificó ese lanzador ni se amplió la condición a LocalDemo. Para Swagger, iniciar el proyecto API
directamente en Development. No iniciar dos procesos sobre 5002 ni detener procesos ajenos.

En Production, Staging y LocalDemo no se registran ni el generador ni los middlewares de Swagger.
Las rutas inexistentes pueden devolver 401 al usuario anónimo por la política fallback existente;
eso no implica que Swagger esté instalado en ese entorno.

## Operaciones documentadas

| Grupo | Operaciones |
|---|---|
| Productos | GET /api/productos; GET /api/productos/{id}; GET /api/productos/{id}/precio; GET /api/productos/{id}/stock |
| Clientes | GET /api/clientes; POST /api/clientes |
| Ventas | GET /api/ventas; POST /api/ventas; GET /api/ventas/{id}; GET /api/ventas/total-dia; POST /api/ventas/{id}/anular |
| Comprobantes | GET /api/ventas/{id}/comprobante; GET /api/ventas/{id}/comprobante/html |
| Health | GET /health |
| Recursos | GET /images/logo.jpg, recurso público auxiliar existente |

La documentación describe parámetros de ruta y consulta, cuerpos JSON, esquemas de respuestas y tipos
application/json, application/problem+json y text/html donde corresponden. Incluye 200, 201, 204, 400, 401,
403, 404, 409, 503 y el error interno 500 según la operación. No documenta 404 para anulación: la lógica actual
devuelve 409 tanto para venta inexistente como ya anulada.

Health comprueba el proceso, no la disponibilidad MySQL. El stock consultado es informativo y no reserva unidades.
El historial devuelve las últimas 50 ventas. Crear clientes y registrar ventas admite Cajero o Administrador;
anular exige Administrador. Todos requieren debeCambiarPassword=false.

## Authorize y obtención de JWT

En **Authorize**, pegar únicamente un JWT válido, sin el prefijo `Bearer`.
Swagger UI agrega `Authorization: Bearer <JWT>` a las peticiones protegidas.
La configuración no persiste la autorización entre recargas. No contiene tokens, claves o credenciales precargadas.
La API mantiene todas sus validaciones de firma HS256, emisor, audiencia, expiración, identidad, rol y cambio
de contraseña. La cookie del frontend Razor no sirve para autenticar la API.

Sin autenticación se pueden probar GET /health y GET /images/logo.jpg. La UI y el JSON OpenAPI son públicos
exclusivamente en Development. No se requiere Bearer para esos recursos.

Actualmente no existe un login ni endpoint seguro que entregue JWT al usuario de Swagger en este servicio.
Razor genera y conserva sus JWT exclusivamente en el servidor: no se añadió una ruta para leerlos ni se debe
extraerlos desde la sesión, memoria del proceso, logs o herramientas del navegador.

Para probar endpoints protegidos desde Swagger o Postman queda pendiente una de estas alternativas:

1. Integrar Usuarios y obtener el token mediante su login autorizado, con los claims y emisor/audiencia acordados.
2. Autorizar por separado una herramienta CLI de desarrollo local que emita tokens cortos para perfiles Demo
   cerrados y verificados, con emisor/audiencia y clave privados exclusivos de desarrollo. Debería ejecutarse
   explícitamente, impedir perfiles arbitrarios, evitar logs/persistencia del token y no publicar la clave.

La segunda alternativa es solo una propuesta; no se implementó ni se generaron tokens manuales.
No hay botón que convierta la cookie Razor en un JWT exportable.

## Verificación no destructiva

Las pruebas OpenApiHttpTests levantan hosts locales con JWT y conexión MySQL sin configurar. Solo consultan
UI, su inicializador, documento, health y una ruta de negocio sin autenticación (401). Comprueban esquemas,
grupos, rutas, cuerpos, respuestas, Bearer, exclusiones públicas y ausencia de Swagger fuera de Development.
No ejecutan POST de negocio ni abren conexiones a MySQL.

```powershell
dotnet build .\Servicio1.Ventas\PuntoMarket.Ventas.slnx --no-restore
$env:PUNTOMARKET_MYSQL_TESTS='NO_AUTORIZADO'
dotnet test .\Servicio1.Ventas\tests\PuntoMarket.Ventas.Tests\PuntoMarket.Ventas.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName!~PuntoMarket.Ventas.Tests.Integration'
```

Swagger Try it out ejecuta operaciones reales, no simulaciones. No se registraron ni anularon ventas,
no se crearon clientes y no se modificó MySQL durante esta implementación. Las pruebas existentes de negocio
utilizan dobles aislados; las integraciones MySQL permanecen excluidas.
