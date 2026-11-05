# Administrador Demo y anulación desde Razor

El acceso es exclusivamente local y temporal. No sustituye la futura autenticación de Usuarios.
Requiere Development, `DemoLocal:Enabled=true` y `DemoLocal:AdminEnabled=true`.
`AdminEnabled` está desactivado por defecto. Los accesos remotos siguen rechazados por el middleware de loopback.
Los formularios de entrada y anulación exigen antiforgery. La API conserva JWT y sus políticas de roles.

## Perfiles y comprobación pendiente

Se inspeccionó la semilla de `database/demo-env/sql/PuntoMarket.Ventas.Demo.sql`:
usuario `admin`, ID 1, rol sembrado 1; usuario `cajero1`, ID 2, rol sembrado 2.
Los perfiles son fijos en el servidor: Administrador y Cajero respectivamente. El navegador no puede suministrar otro ID o rol.
La cookie y el JWT se construyen desde el mismo perfil de la sesión individual. Los JWT permanecen en el servidor;
la sesión dura 30 minutos y el JWT se renueva dentro de ese límite. Logout elimina solo esa sesión.

No se consultó MySQL durante esta implementación. La semilla confirma los IDs de origen, pero no prueba el estado
actual de la base. Antes de una anulación real, verificar mediante una consulta de lectura en
`puntomarket_ventas_demo_local`, puerto 3308, que USUARIO.id=1 corresponde a admin activo con rol Administrador.
No seleccionar passwordHash. No usar otro identificador ni crear usuarios para eludir esta comprobación.

## Arranque

Requisitos: MySQL Demo ya iniciado/importado en 3308, configuración privada existente en
`database/demo-env/.env` y archivos privados en el perfil `puntomarket-demo-secrets`.
No reimportar SQL ni arrancar otra instancia si los puertos 5002/5100 están ocupados.
Si están ejecutándose versiones anteriores, detener sus consolas con Ctrl+C y volver a iniciar.

En una terminal PowerShell, desde la raíz del trabajo:

```powershell
Set-Location 'C:\Users\usuario\Documents\UCB\Arquitectura\PuntoMarket_Entregable3_Trabajo'
dotnet run --project .\Servicio1.Ventas\tools\PuntoMarket.Ventas.DemoLocal\PuntoMarket.Ventas.DemoLocal.csproj --no-launch-profile
```

El lanzador existente carga la configuración privada, realiza comprobaciones de lectura y mantiene la API
en http://localhost:5002. No registra ni anula ventas. No se ejecutó durante esta implementación.

En otra terminal, desde la misma raíz:

```powershell
dotnet run --project .\Servicio1.Ventas\frontend\PuntoMarket.Ventas.Frontend\PuntoMarket.Ventas.Frontend.csproj --no-launch-profile -- --environment Development --urls http://localhost:5100 --DemoLocal:Enabled true --DemoLocal:AdminEnabled true
```

Los argumentos habilitan Administrador solo para ese proceso. No modifican la configuración persistida.
Para deshabilitarlo, reiniciar omitiendo `--DemoLocal:AdminEnabled true`.

Abrir http://localhost:5100/Demo y pulsar **Entrar como Administrador Demo**.
El formulario hace POST a `/Demo?handler=Administrador` y redirige a `/Sales/Historial`.
Para probar los dos perfiles simultáneamente, usar ventanas con cookies independientes, por ejemplo una ventana privada.
Reiniciar Razor invalida sus cookies y formularios antiguos: recargar y volver a entrar.

## Flujo de anulación

Solo Administrador recibe el modal. El botón aparece en ventas COMPLETADA.
Después de cargar el detalle y escribir el motivo, JavaScript envía exactamente `{ idVenta, motivo }`
al handler Razor, con antiforgery. Razor valida ModelState, identificador y motivo de 1 a 255 caracteres,
y llama mediante el cliente existente a `POST /api/ventas/{id}/anular` con JWT server-side.
El Cajero recibe 403 en Razor y también en la API.

La lógica de Application, los repositorios, las transacciones, FEFO y la restauración por lotes no se modificaron.
Ante respuesta confirmada, se recarga el historial. Con el filtro de anuladas desactivado, la venta desaparece;
al activarlo se muestra ANULADA y sin acción Anular.

Ante pérdida de respuesta, timeout, 503 o respuesta de éxito ilegible, se informa resultado desconocido,
se bloquea Confirmar y se ofrece **Consultar nuevamente el estado de la venta** (solo GET).
Si figura ANULADA, no se envía otro POST. Si figura COMPLETADA, se vuelve a mostrar el resumen antes de permitir
una nueva confirmación manual. No hay reintentos automáticos ni implementación de idempotencia.

## Validación automatizada realizada

```powershell
dotnet build .\Servicio1.Ventas\PuntoMarket.Ventas.slnx --no-restore
$env:PUNTOMARKET_MYSQL_TESTS='NO_AUTORIZADO'
dotnet test .\Servicio1.Ventas\tests\PuntoMarket.Ventas.Tests\PuntoMarket.Ventas.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName!~PuntoMarket.Ventas.Tests.Integration'
node --check .\Servicio1.Ventas\frontend\PuntoMarket.Ventas.Frontend\wwwroot\js\historial.js
node --test .\Servicio1.Ventas\tests\javascript\*.test.cjs
```

Resultado: build sin errores ni advertencias; 320 pruebas .NET y 9 JavaScript correctas.
Las nuevas pruebas usan Kestrel con HTTP sustituido o DOM/HTTP aislados, sin conexiones MySQL.
Cubren roles, JWT server-side coherente con la cookie, aislamiento/logout, flags, Production, antiforgery,
datos inválidos, contrato JSON, múltiples clics, resultado desconocido y consulta posterior.
Las pruebas existentes de API cubren rechazo Cajero, motivo, auditoría, restauración y segunda anulación.

## Verificación real pendiente, con autorización separada

1. Confirmar por lectura la identidad de admin descrita arriba y seleccionar una venta COMPLETADA existente.
2. Anotar los IdLote/cantidades de DETALLE_VENTA y las cantidades actuales de esos lotes y del producto.
3. Abrir el historial como Cajero y comprobar ausencia de Anular; entrar como Administrador y revisar el modal y motivo obligatorio.
4. Solo después de autorización explícita, confirmar una única anulación manual.
5. Verificar ANULADA, motivo, fecha e idUsuarioAnulacion=1; cada lote debe recuperar exactamente su cantidad vendida.
6. Verificar coherencia de PRODUCTO.stockActual según triggers. Un lote desactivado no se reactiva al restaurarse.
7. Comprobar que no se ofrece Anular otra vez. Una segunda solicitud controlada, si se autoriza, debe ser 409 sin incremento de stock.

Si no existe una venta COMPLETADA adecuada, su preparación requiere autorización separada.
No se ejecutaron ventas ni anulaciones reales, consultas MySQL, scripts SQL/Docker, integraciones en 3307 ni regeneración del ZIP.
