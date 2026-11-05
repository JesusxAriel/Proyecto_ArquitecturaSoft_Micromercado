# Usuarios Demo: procedimiento revisado, NO ejecutado

## Comprobaciones del 2026-11-05

Conexión real desde un proceso .NET Windows a 127.0.0.1:3308, usando el secreto externo existente de Ventas en memoria: MySQL observó **172.20.0.1**, base puntomarket_ventas_demo_local, TLSv1.2 / ECDHE-RSA-AES256-GCM-SHA384. No se modificó Ventas ni se imprimió su secreto. La comprobación está en VerificarOrigen/, herramienta exclusivamente SELECT/SHOW. La conexión restringida falló en TLS; con acceso autorizado funcionó.

Servidor MySQL 8.4.11: TLSv1.2/TLSv1.3, require_secure_transport OFF, skip_name_resolve ON. Ventas y Reportes no exigen SSL por cuenta, pero sus lanzadores sí. La nueva cuenta tendrá REQUIRE SSL. No se tocó configuración del servidor. Root TCP fue rechazado en la inspección previa; la administración por socket mediante el secreto ya montado funciona. Cuenta usuarios_demo ausente en la última consulta.

Logging observado: general_log OFF, slow_query_log OFF, log_raw OFF; binlog ON. Plugins AUDIT observados: sha2_cache_cleaner/mysqlx_cache_cleaner, no un sistema de registro de consultas. No se consultaron archivos de logs ni datos privados.

## Riesgo corregido

El método anterior dependía de SET @password y SQL dinámico con el secreto; esas sentencias no tienen las mismas garantías de reescritura de contraseñas de CREATE USER. Se eliminó ese mecanismo. Preparar-CuentaDemo.sql es ahora una plantilla con host exacto 172.20.0.1 y marcador SIN comillas que no es SQL ejecutable hasta que el cliente lo sustituya. Nunca ejecutar directamente ni guardar una copia sustituida.

Activar-CuentaDemo.ps1 genera 48 bytes aleatorios en memoria (Base64) y sustituye únicamente el literal BY de CREATE USER. El secreto no aparece en argumentos, historial PowerShell, SET SQL, archivos versionados ni salida del cliente. CREATE USER es una sentencia cuya contraseña MySQL reescribe en sus logs con log_raw OFF. No se puede prometer protección frente a procesos administrativos, captura externa de memoria o políticas externas de transcripción; no ejecutar con trazas/depuración/transcripción o captura de parámetros. Se vuelven a comprobar los logs y se bloquea ante auditoría adicional.

El password no se solicita mediante texto de consola: lo genera el helper para evitar historial y errores de escapado. Se guarda antes de crear la cuenta, dentro de una carpeta NUEVA externa protegida por ACL, como DPAPI del usuario Windows y como parte del archivo de conexión que necesita el lanzador. La conexión es texto legible protegido por ACL, no cifrado. No se modifican carpetas/secretos existentes. No hay almacenamiento de secretos en SQL versionado.

## Idempotencia, TLS y permisos

El helper se detiene sin cambios si existe usuarios_demo bajo cualquier host. La plantilla incluye CREATE USER IF NOT EXISTS y omite GRANT si detectó una cuenta existente, pero debe usarse solo con el helper: si existe bajo otro host no debe crearse una segunda cuenta. No hay ALTER USER ni REVOKE. Sin administración concurrente: existe una ventana entre comprobaciones que no debe usarse para administrar la misma cuenta en paralelo. Un fallo puede dejar cuenta/grants parciales; no reintentar automáticamente ni cambiar contraseña, revisar y autorizar reparación.

Cuenta usuarios_demo@172.20.0.1, caching_sha2_password, REQUIRE SSL; solo SELECT/INSERT/UPDATE en USUARIO y SELECT en ROL, sin roles, privilegios globales, DELETE, DDL o GRANT OPTION. Son suficientes para los repositorios actuales, incluida baja lógica mediante UPDATE y creación INSERT ... SELECT FROM ROL. Los triggers requieren un DEFINER válido. CREATE/GRANT tienen commit implícito.

## Comandos futuros exactos: ejecutar SOLO después de autorización

Desde la raíz del workspace, preparar las dependencias de lectura y del lanzador si no están compiladas:

```powershell
dotnet build ./Servicio2.Usuarios/database/VerificarOrigen
dotnet build ./Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal -r win-x64
# Solo lectura; puede ejecutarse para volver a confirmar host y TLS:
dotnet run --project ./Servicio2.Usuarios/database/VerificarOrigen --no-build --no-restore
```

Una vez autorizada expresamente la creación (NO ejecutado en esta revisión):

```powershell
# Debe ser una carpeta NUEVA fuera del workspace.
./Servicio2.Usuarios/database/Activar-CuentaDemo.ps1 -Ejecutar -DirectorioPrivado (Join-Path $env:USERPROFILE 'puntomarket-demo-secrets/usuarios-demo')
```

El helper exige contenedor healthy, host observado 172.20.0.1, cuenta ausente, logs compatibles y tablas presentes. Carga la biblioteca del lanzador; crea carpeta con herencia deshabilitada, acceso del usuario actual y archivos heredando esa protección; genera el secreto; ejecuta TODA la plantilla en una sesión MySQL por stdin de docker exec y verifica grants sin publicarlos. El proceso hijo recibe la credencial administrativa solo desde el secreto montado dentro del contenedor; no aparece en argumentos. No usa --force. Aborta al primer error SQL. Si falla después de crear la cuenta, conserva los secretos privados para revisión sin reintentar.

Después de éxito y verificación:

```powershell
$env:USUARIOS_DEMO_CONNECTION_FILE = Join-Path $env:USERPROFILE 'puntomarket-demo-secrets/usuarios-demo/usuarios-connection.txt'
dotnet run --project ./Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal -r win-x64 --no-launch-profile
```

La conexión generada exige SslMode Required. VerifyCA/VerifyFull requieren preparar confianza/certificados y correspondencia de nombre; no copiar claves privadas del servidor al cliente. Verificar SHOW GRANTS, ssl_type y una conexión de Usuarios por Windows antes de afirmar éxito completo. No usar endpoints que creen/modifiquen usuarios durante la comprobación de arranque. SMTP y login frontend siguen pendientes.

## Alcance de verificación

Se ejecutó únicamente VerificarOrigen y consultas administrativas de lectura. Activar-CuentaDemo.ps1 solo se analizó con el parser PowerShell (sin errores); su ruta de creación/grants NO se ejecutó. La sintaxis CREATE USER/REQUIRE SSL/GRANT/PREPARE es de MySQL 8.4; no se acredita una prueba de activación hasta recibir autorización. No se crearon cuentas ni se concedieron permisos, ni se modificaron registros de negocio o servicios ajenos.
