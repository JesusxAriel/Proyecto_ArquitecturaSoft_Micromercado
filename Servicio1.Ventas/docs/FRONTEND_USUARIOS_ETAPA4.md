# Etapa 4: único acceso mediante Usuarios

Sustituye el modo de coexistencia documentado en las etapas 1–3. La API Usuarios es el único emisor del JWT utilizado por Razor; el frontend valida firma HS256/issuer/audience/nbf/exp y no firma ni renueva tokens. Se conserva la configuración externa compartida, sin modificar APIs ni secretos.

## Acceso

/Account/Login es el único login. Las páginas del frontend requieren autenticación por defecto, salvo los endpoints explícitamente anónimos de Account y la redirección /Demo. El correo determina la cuenta en Usuarios; no existe selector de rol. Tras éxito, ReturnUrl local se respeta; sin ReturnUrl se abre /Sales. ReturnUrl externo se rechaza.

/Demo y sus antiguos handlers solo redirigen al login real y nunca crean sesión. Los POST mantienen antiforgery. Los flags DemoLocal antiguos no habilitan nada. SesionesDemo.cs se conservó como texto histórico fuera del proyecto compilado, en docs/historico/frontend-demo/SesionesDemo.cs.txt. No se registra ni se ejecuta.

La cookie PuntoMarket.Demo.Session ya no tiene esquema autenticador y se elimina al recibirla. Cualquier entrada cacheada de la sesión histórica es inaccesible desde los clientes y la autorización nuevos. Reiniciar el frontend aplica el código y vacía la memoria del proceso anterior. Nunca se convierten identidades o tokens Demo a Usuarios.

Solo se registra el esquema de cookie UsuariosPreparado (nombre interno conservado para compatibilidad). SesionesUsuario mantiene el JWT exclusivamente en memoria, con ID aleatorio y límite exp. La cookie contiene un ticket protegido con ID/claims mínimos, nunca JWT ni contraseña. Sin persistencia ni renovación automática; la validación rechaza sesiones invalidadas, vencidas, claims incoherentes y debeCambiarPassword=true. Los tickets vencidos/no válidos se eliminan al llegar una petición. En HTTP loopback Secure depende del esquema; en un despliegue real usar HTTPS.

## Permisos y errores

Cajero y Administrador acceden a ventas, historial y comprobantes. Solo Administrador accede a reportes y anulación. La navegación usa exclusivamente los claims validados y logout es POST con antiforgery.

401 remoto invalida el ID de sesión utilizado y borra su cookie; no afecta otros usuarios y no reintenta operaciones. Los handlers devuelven 401 y X-PuntoMarket-Login=/Account/Login. Los helpers AJAX dirigen al mismo login sin repetir el POST. Las descargas fallidas conservan 401 en lugar de simular un archivo; la página muestra la entrada al login al quedar anónima. 403 conserva sesión y comunica acceso denegado. Expiración obliga a introducir credenciales de nuevo: no se conserva la contraseña para relogin silencioso.

## Prueba manual posterior (NO realizada en esta etapa)

1. Mantener Usuarios en 127.0.0.1:5003, Ventas en 5002 y Reportes en 5012 con sus lanzadores y configuración privada existentes. No ejecutar scripts SQL o de cuentas.
2. Detener únicamente el frontend anterior y ejecutar desde el workspace:

```powershell
dotnet run --project ./Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend --no-launch-profile -- --environment Development --urls http://localhost:5100
```

3. Abrir http://localhost:5100: debe dirigir a /Account/Login. /Demo debe dirigir al mismo login, sin perfiles.
4. Introducir en el formulario privado las credenciales existentes de Cajero. No pegarlas en comandos, capturas, chat o logs. Comprobar navegación/historial y consultas de lectura; no confirmar ventas ni crear clientes.
5. Cerrar sesión mediante el botón Salir. Verificar que las páginas protegidas piden login. Repetir con Administrador y consultar reportes/exportaciones sin anular ventas.
6. Esperar el vencimiento del JWT (cinco minutos DemoLocal), luego consultar: debe solicitar nuevo login. Verificar que no se vuelve a una identidad Demo.
7. Una cuenta con cambio obligatorio no obtiene sesión operativa: aparece mensaje seguro. El flujo de cambio y SMTP siguen fuera de esta etapa.

## Validación automática

Se adaptaron fixtures a JWT de un Usuarios simulado; no se leen claves privadas ni se ejecutan logins/operaciones reales. Se conserva la regresión de búsqueda, registro y anulación simulados, roles, contratos, errores, reportes, exportación y paginación. Se prueban las rutas Demo retiradas, cookie antigua ignorada/borrada, esquema único, ausencia de emisor compilado, expiración sin renovación, antiforgery, ReturnUrl, aislamiento y ausencia de secretos en HTML/cookies/logs.

Comandos de verificación:

```powershell
dotnet test ./Servicio1.Ventas/tests/PuntoMarket.Ventas.Tests --no-restore -p:BaseOutputPath=bin/usuarios-etapa4/ -v quiet
node --test ./Servicio1.Ventas/tests/javascript/*.test.cjs ./Servicio1.Ventas/tests/reportes-paginacion.test.cjs ./Servicio1.Ventas/tests/ventas-reglas-pago.test.cjs
```

No se modificaron las tres APIs, MySQL, cuentas, contraseñas ni archivos privados. No se hicieron commits. Las pruebas MySQL no configuradas continúan omitidas.


## Inventario de archivos de la etapa

En frontend/PuntoMarket.Ventas.Frontend se modificaron:

- FrontendBootstrap.cs
- Configuration/PreparacionUsuarios.cs
- Security/AccesoUsuariosPreparado.cs
- Security/ResolucionSesion.cs
- appsettings.Development.json (retirada de flags de acceso por perfiles; sin secretos)
- Pages/Demo/Index.cshtml y Pages/Demo/Index.cshtml.cs
- Pages/Account/Login.cshtml y Pages/Account/Login.cshtml.cs
- Pages/Account/Logout.cshtml
- Pages/Shared/_Layout.cshtml
- wwwroot/js/ventas-reglas.js, ventas.js e historial.js

Security/SesionesDemo.cs se trasladó a docs/historico/frontend-demo/SesionesDemo.cs.txt como historial no compilado.

Pruebas modificadas en tests/PuntoMarket.Ventas.Tests/Frontend: AccountUsuariosTests.cs, ConsultasRazorTests.cs, ReportesClienteTests.cs, UsuariosEtapa1Tests.cs. Nuevo helper SesionRealPrueba.cs, emisor únicamente de JWT sintéticos de prueba. También se actualizó tests/javascript/sesion-routing.test.cjs.

Documentación nueva: este archivo y el historial no compilado indicado.

Resultado final: build del frontend con **0 errores y 0 advertencias**; **394 pruebas .NET correctas, 0 fallidas, 9 MySQL omitidas**; **20 pruebas JavaScript correctas**. No se ejecutó login real ni comprobación manual.
