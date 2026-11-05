# Configuración JWT de Servicio 2

El emisor de tokens lee la sección `Jwt` de la configuración de ASP.NET Core.
Para configuración mediante variables de entorno, establece:

| Variable | Requisito |
|---|---|
| `Jwt__Issuer` | Emisor no vacío, igual al configurado por los servicios validadores. |
| `Jwt__Audience` | Audiencia no vacía, igual a la esperada por los servicios validadores. |
| `Jwt__Key` | Secreto aleatorio de al menos 32 bytes UTF-8 para HS256. No utilizar valores de ejemplo ni guardarlo en el repositorio. |
| `Jwt__ExpireMinutes` | Duración positiva del token en minutos. Si se omite, el valor predeterminado es 60. |

En desarrollo, usa User Secrets o variables de entorno. En despliegues, inyecta
el secreto desde el gestor de secretos del entorno. La misma clave, emisor y
audiencia deben configurarse en los validadores autorizados; no incluyas la
clave en `appsettings.json`, archivos versionados, imágenes o logs.


## DemoLocal en el workspace integrado

El host se comparte mediante `src/PuntoMarket.Usuarios.Api/UsuariosBootstrap.cs`; no sustituye repositorios ni casos de uso. Domain y la lógica de negocio no se modificaron. LocalDemo fija HTTP **127.0.0.1:5003** y omite HTTPS redirection exclusivamente en ese entorno. Los demás entornos conservan la redirección existente. Swagger se registra y habilita solo en Development/LocalDemo; Production, Staging y Testing no registran sus servicios ni middleware.

### Configuración privada necesaria

- Archivo existente de clave JWT: `puntomarket-demo-secrets/jwt-demo-key.txt` bajo el perfil del usuario. Es el mismo que usan Ventas y Reportes. No se crea, sobrescribe ni imprime.
- `USUARIOS_DEMO_CONNECTION_FILE`: ruta absoluta a un archivo privado fuera del workspace que contenga la conexión MySQL completa. Protegerlo con ACL del usuario; no guardarlo en Git ni pegar su contenido en el chat.
- Destino obligatorio: servidor `127.0.0.1`, puerto `3308`, base `puntomarket_ventas_demo_local`.
- Cuenta exclusiva de Usuarios, distinta de root y ventas_demo. Debe tener permisos directos de tabla: **SELECT, INSERT, UPDATE en USUARIO; SELECT en ROL**, sin roles, GRANT OPTION ni otros privilegios. El lanzador inspecciona SHOW GRANTS y rechaza permisos más amplios, incluso permisos sobre toda la base. Una cuenta SELECT-only de Reportes no sirve. La exclusividad de uso de la cuenta debe garantizarla el operador.
- TLS obligatorio: SslMode Required, VerifyCA o VerifyFull. Required cifra pero no autentica el servidor; VerifyCA/VerifyFull requieren la confianza/certificados correspondientes. No se fuerza una versión TLS ni se deshabilitan comprobaciones.

El lanzador verifica conexión/base y privilegios mediante lecturas; no ejecuta migraciones, escrituras, login de prueba ni envío de correo. No crea cuentas ni modifica permisos. Si falta configuración, se detiene. No imprime cadenas, contraseñas, JWT ni excepciones completas. El host Demo suprime logging para no publicar detalles privados.

JWT se inyecta en memoria: issuer `PuntoMarket.DemoLocal`, audience `PuntoMarket.Ventas.DemoLocal`, HS256 (emisor y validador existentes), expiración Demo explícita de cinco minutos. El frontend sigue usando su mecanismo actual y no se integra en esta etapa.

### Restaurar, compilar y probar (Windows x64)

Desde la raíz del workspace:

```powershell
dotnet restore ./Servicio2.Usuarios/PuntoMarket.Usuarios.sln -r win-x64
dotnet restore ./Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal -r win-x64
dotnet build ./Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal -r win-x64 --no-restore
dotnet test ./Servicio2.Usuarios/PuntoMarket.Usuarios.sln -r win-x64 --no-restore
```

Las dos pruebas MySQL existentes usan `MICROMERCADO_PRUEBAS_MYSQL`; dejarlas omitidas si no hay una base aislada de pruebas configurada. No dirigir pruebas de escritura a Demo. Los nuevos tests usan HTTP loopback con puertos efímeros y una clave aleatoria solo de prueba, sin leer secretos ni usar MySQL. Comprueban los cinco entornos, UI/JSON/recursos Swagger, 401 sin JWT, 403 Cajero y JWT inválido. La compatibilidad de claims/firma se prueba contra parámetros estrictos equivalentes a Demo; no afirma haber llamado a Ventas o Reportes reales.

### Arranque

```powershell
# Sustituir por un archivo privado REAL preparado por el operador.
$env:USUARIOS_DEMO_CONNECTION_FILE = '<RUTA_ABSOLUTA_REAL_FUERA_DEL_WORKSPACE>'
dotnet run --project ./Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal -r win-x64 --no-launch-profile
```

Con la configuración válida, abrir `http://localhost:5003/swagger` (el listener solo acepta IPv4 loopback; también `http://127.0.0.1:5003/swagger`). JSON en `/swagger/v1/swagger.json`. Detener únicamente esa consola con Ctrl+C. El lanzador rechaza reemplazar procesos que ocupen 5003. La documentación es pública en los entornos locales; sus operaciones mantienen JWT/roles. Try it out puede cambiar datos o enviar correos: no ejecutarlo durante verificaciones de solo lectura.

### SMTP y activación

No se proporciona SMTP ficticio. Para crear cuentas o reenviar activaciones se necesita configuración REAL externa `Smtp:Host`, `Port`, `FromAddress`, `FromName`, `UserName`/`Password` cuando corresponda, `EnableSsl`, y `Activacion:BaseUrl`. La API admite los proveedores de configuración existentes, incluidas variables de entorno con `__`. La configuración SMTP no se valida ni se prueba enviando correos al arrancar DemoLocal.

Los enlaces existentes apuntan a `/Users/Activar` en el frontend. Esa integración sigue pendiente. No usar creación/reenvío antes de preparar SMTP y la página: la creación puede persistir una cuenta pendiente antes de fallar el correo.

### Resultado de esta preparación

Restore de la solución y el lanzador realizado. Build del lanzador y sus cuatro proyectos sin errores ni advertencias. La primera restauración restringida falló por conexión TLS a NuGet; la restauración autorizada funcionó. La auditoría NuGet se deshabilitó en las ejecuciones de verificación (`-p:NuGetAudit=false`), sin cambiar archivos de configuración; no se acredita auditoría de vulnerabilidades.

El lanzamiento real se intentó sin conexión configurada y se detuvo de forma controlada con el aviso de `USUARIOS_DEMO_CONNECTION_FILE`, antes de abrir MySQL. No se afirma que el Swagger real de 5003 esté actualmente activo ni que exista ya la cuenta exclusiva. No se modificaron Ventas, Reportes, frontend, JWT privado o base de datos.

Resultado final de pruebas: **132 correctas, 0 fallidas, 2 omitidas** (integración MySQL no configurada). Incluye seis casos nuevos de host/OpenAPI y compatibilidad JWT.
