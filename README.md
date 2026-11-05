# PuntoMarket — Entregable 3

PuntoMarket integra ventas, gestión de usuarios y reportes mediante tres APIs REST .NET 10. Un único frontend Razor Pages consume HTTP/JSON y DTOs. El JWT lo emite Usuarios; el frontend lo valida y lo conserva en memoria del servidor, asociado a una cookie protegida sin JWT.

| Componente | Responsabilidad | Dirección local |
|---|---|---|
| Servicio1.Ventas | Productos, clientes, ventas, historial, anulaciones y comprobantes | http://localhost:5002 |
| Servicio2.Usuarios | Login, roles, altas, bajas lógicas y activación por correo | http://127.0.0.1:5003 |
| Servicio3.Reportes | Consultas SQL, filtros, totales, gráfico y exportación PDF/Excel | http://localhost:5012 |
| Frontend de Ventas | Interfaz común de los tres servicios | http://localhost:5100 |
| MySQL Demo | MySQL 8.4 en Docker, publicado solo en loopback | 127.0.0.1:3308 |

Reportes consulta MySQL directamente, con JOIN SQL y cuenta SELECT independiente. No consume la API de Ventas. Temporalmente los tres servicios comparten la base Demo, con cuentas separadas.

Repositorio: https://github.com/JesusxAriel/Proyecto_ArquitecturaSoft_Micromercado

## Requisitos

- Windows y **PowerShell 7.6** para los scripts locales con ACL y secretos privados.
- SDK **.NET 10** y acceso a NuGet para restaurar paquetes.
- Docker Desktop iniciado, contexto **desktop-linux**, Docker Compose v2 y motor de contenedores Linux.
- MySQL **8.4**, provisto por el compose de Demo; no hace falta instalar un servidor MySQL en Windows.
- Node.js para las pruebas JavaScript. Python 3.13 y Chrome/Chromium son necesarios para las comprobaciones de repositorio/navegador.
- Una cuenta Gmail con contraseña de aplicación, o un servidor SMTP real equivalente, para crear y activar usuarios nuevos por correo.

La ejecución local documentada no necesita Redis. El despliegue antiguo del monolito con Redis/nginx está retirado de este árbol. Los lanzadores DemoLocal verifican destinos, permisos y TLS; no deben utilizarse como despliegue de producción.

## Estructura

    PuntoMarket.slnx
    bdMicroMercadoArqui.sql
    Servicio1.Ventas/
      src/  frontend/PuntoMarket.Ventas.Frontend/  tests/  tools/  database/  docs/
    Servicio2.Usuarios/
      src/  tests/  tools/  database/
    Servicio3.Reportes/
      src/  tests/  tools/
    tools/
      Preparar-CuentasLocales.ps1
      validar_repositorio.py
    doc/
      compatibilidad-usuarios/  integracion/  despliegue/  historico/

La solución raíz incluye los proyectos actuales, las pruebas, lanzadores y utilitarios. Los documentos de historico son evidencias anteriores, no instrucciones de arranque.

## Organización del repositorio

- **Rama principal:** `main`. Solo recibe cambios mediante Pull Requests revisados; no se trabaja directamente sobre ella.
- **Ramas de trabajo:** una por tema, con prefijo según el tipo de cambio: `feat/` (funcionalidad nueva), `fix/` (corrección), `refactor/` (reorganización sin cambiar el comportamiento) y `chore/` (configuración, herramientas y soluciones). Ejemplos: `feat/usuarios-gestion`, `feat/reportes-listado-ventas`, `fix/ajustes-finales`, `chore/solucion-reportes`.
- **Pull Requests:** cada rama se fusiona a `main` mediante un Pull Request que se revisa antes de fusionar. Las ramas ya fusionadas se eliminan.
- **Commits:** en español, pequeños y de un solo tema, con prefijo convencional: `feat:`, `fix:`, `refactor:`, `test:`, `docs:` o `chore:`. Ejemplo: `fix: mostrar los estados de usuario como Pendiente, Activo, Inactivo y Vencido`.
- **Carpetas:** `Servicio1.Ventas` (API de Ventas y frontend), `Servicio2.Usuarios` (Usuarios) y `Servicio3.Reportes` (Reportes). Cada servicio tiene su propia solución y sus pruebas; `PuntoMarket.slnx` agrupa todo.

## 1. Restaurar y compilar

Abrir PowerShell en la raíz del clon:

~~~powershell
dotnet --version
docker context show
dotnet restore PuntoMarket.slnx
dotnet build PuntoMarket.slnx --no-restore
~~~

Si aparece NU1900, la restauración puede haber usado la caché, pero la consulta de vulnerabilidades de NuGet no se completó. Restablecer acceso a api.nuget.org y repetir; no interpretar esa advertencia como una auditoría de paquetes aprobada.

## 2. Elegir instalación nueva o entorno existente

**Si ya existen la base, volumen o secretos de Demo, no repetir los pasos 3–5.** Usar los archivos privados propios y pasar al arranque. Los scripts de importación y cuentas bloquean estados existentes; no borrar sus registros para forzar otra ejecución.

Para una instalación nueva se generan credenciales propias. El repositorio no contiene contraseñas predefinidas, hashes de usuarios ni la clave JWT del autor. No copiar credenciales de otro equipo.

## 3. Preparar archivos privados propios, sin conectar a MySQL

Estos comandos crean archivos nuevos bajo $env:USERPROFILE/puntomarket-demo-secrets, con ACL restringida. Nunca los sobrescriben:

~~~powershell
./Servicio1.Ventas/database/demo-env/Preparar-SecretosDemo.ps1 -Paso PasswordRoot
./Servicio1.Ventas/database/demo-env/Preparar-SecretosDemo.ps1 -Paso PasswordVentas
./Servicio1.Ventas/database/demo-env/Preparar-SecretosDemo.ps1 -Paso Hashes
./Servicio1.Ventas/database/demo-env/Preparar-SecretosDemo.ps1 -Paso Jwt
~~~

- Las contraseñas MySQL de root y ventas_demo se introducen de forma oculta y deben ser diferentes.
- Hashes solicita contraseñas NUEVAS de los usuarios de bootstrap admin y cajero1, las confirma y genera BCrypt usando la biblioteca ya utilizada por Usuarios. No extrae secretos del SQL ni de un archivo histórico.
- La política coincide con Usuarios: mínimo 8 caracteres, mayúscula, minúscula, dígito y carácter no alfanumérico.
- Jwt genera una única clave aleatoria compartida por los lanzadores y el validador del frontend. No se imprime ni se regenera al arrancar.

Crear la configuración local de Compose, que contiene rutas a archivos externos, no contraseñas:

~~~powershell
$privado = Join-Path $env:USERPROFILE 'puntomarket-demo-secrets'
$rutaEnv = Join-Path (Get-Location) 'Servicio1.Ventas/database/demo-env/.env'
if (Test-Path -LiteralPath $rutaEnv) { throw 'Ya existe .env: revisar, no sobrescribir.' }
$lineas = @(
    'VENTAS_DEMO_USER=ventas_demo'
    'VENTAS_DEMO_ROOT_PASSWORD_FILE=' + (Join-Path $privado 'mysql-root.txt').Replace('\','/')
    'VENTAS_DEMO_PASSWORD_FILE=' + (Join-Path $privado 'mysql-ventas.txt').Replace('\','/')
    'VENTAS_DEMO_ADMIN_HASH_FILE=' + (Join-Path $privado 'admin-password-hash.txt').Replace('\','/')
    'VENTAS_DEMO_CAJERO_HASH_FILE=' + (Join-Path $privado 'cajero-password-hash.txt').Replace('\','/')
)
[IO.File]::WriteAllLines($rutaEnv, $lineas, [Text.UTF8Encoding]::new($false))
./Servicio1.Ventas/database/demo-env/Validar-Preparacion.ps1 -OmitirCompose
./Servicio1.Ventas/database/demo-env/Validar-ConfiguracionPrivada.ps1
~~~

No se debe activar un transcript de PowerShell, mostrar contenido de archivos privados ni añadirlos forzadamente a Git. La .env.example raíz es una referencia pública: dotnet no la carga automáticamente.

## 4. Iniciar MySQL y preparar una base NUEVA

El único compose operativo versionado para Demo arranca MySQL; no despliega las APIs:

~~~powershell
docker compose --project-name puntomarket-ventas-demo --env-file ./Servicio1.Ventas/database/demo-env/.env -f ./Servicio1.Ventas/database/demo-env/compose.yaml up -d --wait --wait-timeout 180 mysql-demo
docker inspect --format '{{.State.Health.Status}}' puntomarket-ventas-demo-mysql-demo-1
~~~

Debe indicar healthy. En un volumen nuevo, la imagen crea la base vacía puntomarket_ventas_demo_local y la cuenta ventas_demo. No monta ni ejecuta automáticamente el SQL. En un volumen existente no cambia su contraseña ni reinicializa datos.

**Solo en una instalación nueva y vacía**, revisar y ejecutar una vez:

~~~powershell
./Servicio1.Ventas/database/demo-env/Importar-EsquemaDemo.ps1 -Importar
./Servicio1.Ventas/database/demo-env/Verificar-ImportacionDemo.ps1
~~~

El importador verifica identidad del contenedor, puertos, volumen, base vacía, FK y logs SQL desactivados. Envía hashes privados y SQL en la misma sesión, por stdin, sin argumentos con secretos ni reintentos. El DDL de MySQL no es atómico: si falla, detenerse y diagnosticar el estado parcial.

La fuente bdMicroMercadoArqui.sql contiene 11 tablas, 4 vistas, 23 triggers, un procedimiento y semillas sintéticas. Conserva idUsuarioActualizacion con índice y FK. La copia Demo y manifest.json se generan con Preparar-CopiaSql.ps1, que solo transforma archivos. Ambos están reconciliados con esta fuente publicable.

La fuente no contiene DROP DATABASE ni hashes incrustados. Tampoco es una migración para bases existentes: **no ejecutar el SQL completo ni reimportar en una Demo con datos**. La migración histórica en doc/compatibilidad-usuarios solo se considera si una base anterior carece de la columna, después de inspección y respaldo propios.

Las semillas contienen dos usuarios ACTIVO de bootstrap:

| Login por correo | Rol | Contraseña |
|---|---|---|
| admin@example.invalid | Administrador | La introducida privadamente para admin |
| cajero@example.invalid | Cajero | La introducida privadamente para cajero1 |

Son identidades sintéticas sin correo entregable. Permiten el primer login sin depender de SMTP. Para una persona real, crear otra cuenta con su dirección real mediante /Users; no usar una contraseña publicada.

## 5. Crear cuentas MySQL independientes, solo en una instalación nueva

Revisar primero este script y ejecutar una sola vez:

~~~powershell
./tools/Preparar-CuentasLocales.ps1 -Aplicar
~~~

El script obtiene el host de origen que MySQL observa desde Windows; no presupone una subred ni concede acceso desde %. Rechaza cuentas existentes bajo cualquier host, archivos de conexión existentes, logs SQL activos y ACL no restrictivas. Crea contraseñas aleatorias y guarda conexiones únicamente en el directorio privado:

| Cuenta | Permisos | Archivo privado |
|---|---|---|
| usuarios_demo | SELECT, INSERT, UPDATE en USUARIO; SELECT en ROL | usuarios-connection.txt |
| reportes_demo | SELECT en la base Demo | reportes-connection.txt |

Ambas requieren SSL; los clientes usan SslMode=Required como mínimo. No reciben permisos globales ni GRANT OPTION. ventas_demo es la cuenta local de esquema utilizada por el importador; su endurecimiento para producción queda pendiente.

Si el script falla después de escribir archivos o enviar SQL, conservar esos archivos: puede existir un estado parcial. No repetir CREATE USER ni cambiar contraseñas automáticamente. Los lanzadores comprueban los permisos antes de arrancar.

El activador histórico de Usuarios en database/ sigue disponible para trazabilidad, pero presupone un host concreto del equipo original. Para un clon nuevo utilizar el procedimiento de este README, no ese supuesto.

## 6. Arrancar las APIs y el frontend

Abrir cuatro terminales PowerShell en la raíz. Se recomienda usar siempre localhost:5100 para el frontend; cambiar entre localhost y 127.0.0.1 separa cookies.

**Terminal 1 — Ventas**

~~~powershell
dotnet run --project ./Servicio1.Ventas/tools/PuntoMarket.Ventas.DemoLocal --no-launch-profile
~~~

Arranca en 5002, entorno LocalDemo. El lanzador realiza comprobaciones GET/SQL de lectura y no crea ventas. La identidad del cliente de comprobación se obtiene de MySQL, para admitir una Demo anterior o las semillas sintéticas nuevas.

**Terminal 2 — Usuarios y SMTP Gmail**

Activar verificación en dos pasos en una cuenta Google y obtener una [contraseña de aplicación](https://support.google.com/mail/answer/185833). Algunas políticas de cuenta pueden impedir esa opción; en ese caso solicitar un SMTP autorizado. No usar la contraseña normal de Gmail ni habilitar acceso inseguro.

~~~powershell
$env:USUARIOS_DEMO_CONNECTION_FILE = Join-Path $env:USERPROFILE 'puntomarket-demo-secrets/usuarios-connection.txt'
$env:Smtp__Host = 'smtp.gmail.com'
$env:Smtp__Port = '587'
$env:Smtp__EnableSsl = 'true'
$env:Smtp__UserName = Read-Host 'Direccion Gmail emisora'
$env:Smtp__FromAddress = $env:Smtp__UserName
$env:Smtp__FromName = 'PuntoMarket'
$env:Activacion__BaseUrl = 'http://localhost:5100'
$smtpSeguro = Read-Host 'Contrasena de aplicacion Gmail (oculta)' -AsSecureString
try {
    $env:Smtp__Password = [Net.NetworkCredential]::new('', $smtpSeguro).Password
    dotnet run --project ./Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal --no-launch-profile
} finally {
    $env:Smtp__Password = $null
    $smtpSeguro.Dispose()
}
~~~

La contraseña solo se mantiene en memoria/entorno de estos procesos; no escribirla en el comando, .env, appsettings, logs ni repositorio. Sin SMTP, login puede funcionar, pero altas y reenvíos no se consideran operativos. El lanzador no envía un correo de prueba al iniciar.

**Terminal 3 — Reportes**

~~~powershell
$env:REPORTES_DEMO_CONNECTION_FILE = Join-Path $env:USERPROFILE 'puntomarket-demo-secrets/reportes-connection.txt'
dotnet run --project ./Servicio3.Reportes/tools/PuntoMarket.Reportes.DemoLocal --no-launch-profile
~~~

**Terminal 4 — Frontend**

~~~powershell
$env:UsuariosApi__BaseUrl = 'http://127.0.0.1:5003/'
$env:VentasApi__BaseUrl = 'http://localhost:5002/'
$env:ReportesApi__BaseUrl = 'http://localhost:5012/'
dotnet run --project ./Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend --no-launch-profile -- --urls http://localhost:5100
~~~

Los lanzadores comparten HS256, issuer PuntoMarket.DemoLocal, audience PuntoMarket.Ventas.DemoLocal y la clave externa propia. Usuarios emite JWT de cinco minutos; el frontend no los renueva automáticamente. No modificar la configuración de firma por servicio.

Comprobaciones sin credenciales ni escrituras:

~~~powershell
Invoke-WebRequest http://localhost:5002/health | Select-Object StatusCode
Invoke-WebRequest http://localhost:5012/api/salud | Select-Object StatusCode
Invoke-WebRequest http://127.0.0.1:5003/swagger/index.html | Select-Object StatusCode
~~~

Swagger también está disponible en 5002 y 5012, exclusivamente en Development/LocalDemo.

## 7. Crear, activar e iniciar sesión

1. Abrir http://localhost:5100/Account/Login. Iniciar sesión con el Administrador de bootstrap y la contraseña elegida.
2. Abrir Usuarios (/Users). El menú y sus handlers son exclusivos del Administrador.
3. Preparar un alta con nombre, apellido, correo REAL y rol obtenido de Usuarios. No se solicita contraseña.
4. Confirmar la recepción del correo. Ante fallo SMTP, consultar el listado antes de repetir: la cuenta puede haberse guardado como PENDIENTE.
5. Abrir el enlace nuevo del correo. /Users/Activar es anónimo, quita el token de la URL después del primer GET y guarda el estado solo en memoria. La cookie de correlación es HttpOnly, SameSite=Lax; el formulario utiliza antiforgery.
6. Definir y confirmar una contraseña conforme a la política. Tras la confirmación, iniciar sesión en /Account/Login con el correo de esa cuenta.

No pegar enlaces de activación o JWT en terminales, Swagger, capturas o incidencias. Los proxies/APM deben omitir consultas y cuerpos sensibles. El reenvío se permite para cuentas pendientes; la edición y reactivación siguen fuera del alcance actual.

Administrador tiene acceso a reportes, usuarios y anulaciones. Cajero tiene acceso operativo a ventas e historial. Un JWT vencido requiere nuevo login; HTTP 403 conserva la sesión, mientras HTTP 401 la invalida. La condición debeCambiarPassword impide entrar a páginas operativas.

## Pruebas y compilación

~~~powershell
dotnet restore PuntoMarket.slnx
dotnet build PuntoMarket.slnx --no-restore
dotnet test PuntoMarket.slnx --no-restore
python tools/validar_repositorio.py
./tools/Test-PreparacionLocal.ps1
./Servicio2.Usuarios/database/Test-Activar-CuentaDemo.ps1
./Servicio2.Usuarios/tools/Test-Probar-JwtUsuariosVentas.ps1
node --test Servicio1.Ventas/tests/ventas-reglas-pago.test.cjs Servicio1.Ventas/tests/reportes-paginacion.test.cjs Servicio1.Ventas/tests/javascript/ventas-registro.test.cjs Servicio1.Ventas/tests/javascript/sesion-routing.test.cjs Servicio1.Ventas/tests/javascript/historial-anulacion.test.cjs
~~~

Las pruebas normales no realizan ventas ni envían correo real. Las pruebas MySQL requieren habilitación explícita y una base aislada con prefijo de pruebas; no apuntarlas a Demo. Consultar [su guía](Servicio1.Ventas/docs/ENTORNO_PRUEBAS_MYSQL.md).

Regresión de activación en navegador, con enlaces ficticios:

~~~powershell
dotnet build Servicio1.Ventas/frontend/PuntoMarket.Ventas.Frontend --no-restore -p:BaseOutputPath=bin/activacion-usuarios/
python Servicio1.Ventas/tests/activacion-cookie-browser.test.py
~~~

CHROME_BIN permite seleccionar Chromium. La prueba crea un frontend temporal con API inalcanzable; no consume tokens reales. Si los binarios por defecto están en uso, detener el proceso correspondiente o compilar/pruebar con -p:BaseOutputPath=bin/repositorio/.

## Detener y reiniciar sin perder datos

- APIs/frontend: Ctrl+C en cada terminal. Volver a ejecutar los comandos del paso 6.
- MySQL: los siguientes comandos conservan el volumen:

~~~powershell
docker compose --project-name puntomarket-ventas-demo --env-file ./Servicio1.Ventas/database/demo-env/.env -f ./Servicio1.Ventas/database/demo-env/compose.yaml stop mysql-demo
docker compose --project-name puntomarket-ventas-demo --env-file ./Servicio1.Ventas/database/demo-env/.env -f ./Servicio1.Ventas/database/demo-env/compose.yaml start mysql-demo
~~~

No usar down -v, eliminar volúmenes, reimportar semillas ni volver a crear cuentas como procedimiento de reinicio.

## Ejecución local frente a Docker

Hoy está preparada la ejecución local de las tres APIs y Razor con MySQL en Docker. No existe un compose conjunto validado de microservicios. El Dockerfile de Usuarios usa contexto raíz, pero por sí solo no configura la base, JWT, SMTP ni frontend.

En una futura contenerización, UsuariosApi__BaseUrl debe usar el DNS del servicio y su puerto INTERNO, por ejemplo http://usuarios:8080/ si ese es el servicio realmente definido. No usar localhost para comunicar contenedores. Ventas/Reportes y sus validaciones actuales de URL, el montaje privado de la clave, TLS, HTTPS/proxy y las sesiones requieren revisión específica antes de afirmar que el despliegue completo funciona.

Las sesiones reales y la activación usan memoria del proceso y Data Protection temporal. Múltiples réplicas requieren afinidad o una solución de estado/claves compartidos revisada; no balancearlas con el nginx histórico sin adaptar el despliegue. No persistir tokens en claro.

## Ejecución con Docker

Es un camino adicional a la ejecución local con los lanzadores DemoLocal: levanta MySQL, las tres APIs y el frontend con un solo comando, sin cuatro terminales ni PowerShell 7. No modifica los lanzadores ni el compose de demostración de `Servicio1.Ventas/database/demo-env`.

### Requisitos

- Docker Desktop iniciado, con Docker Compose v2 y contenedores Linux. No hace falta instalar .NET, MySQL ni Node.js.

### Preparar el archivo .env

1. Copiar el archivo de ejemplo a `.env` en la raíz del repositorio. El archivo `.env` no se versiona.

~~~powershell
Copy-Item .env.example .env
~~~

2. Generar una clave JWT aleatoria y cuatro contraseñas de base de datos. Cada ejecución del comando imprime un valor nuevo, compuesto solo por letras y números. Ejecutarlo cinco veces y pegar cada resultado en `JWT_KEY`, `MYSQL_ROOT_PASSWORD`, `VENTAS_DB_PASSWORD`, `USUARIOS_DB_PASSWORD` y `REPORTES_DB_PASSWORD`.

~~~powershell
docker run --rm alpine:3 sh -c "head -c 48 /dev/urandom | base64 | tr -d '+/=\n'; echo"
~~~

3. Generar los hashes BCrypt de las contraseñas de `admin` y `cajero1`. Cada contraseña debe tener al menos 8 caracteres, con mayúscula, minúscula, número y carácter especial, y no debe contener comillas simples. Reemplazar el texto entre comillas por la contraseña elegida y pegar la línea resultante en `.env`.

~~~powershell
$h = (docker run --rm httpd:2.4-alpine htpasswd -nbBC 11 x 'CambiarPorLaClaveDeAdmin1!' | Out-String).Trim()
"ADMIN_PASSWORD_HASH='" + $h.Substring(2) + "'"

$h = (docker run --rm httpd:2.4-alpine htpasswd -nbBC 11 x 'CambiarPorLaClaveDeCajero1!' | Out-String).Trim()
"CAJERO_PASSWORD_HASH='" + $h.Substring(2) + "'"
~~~

Los hashes deben quedar entre comillas simples en `.env`, porque contienen el carácter `$`. Las contraseñas en claro no se guardan en ningún archivo.

4. Dejar `JWT_ISSUER` y `JWT_AUDIENCE` con los valores del ejemplo: el frontend valida el token con esos mismos valores.
5. Los puertos publicados y los datos de correo son opcionales. Sin `SMTP_*` el inicio de sesión funciona, pero no se pueden crear usuarios por correo. Para que los enlaces de activación apunten al frontend, ajustar `ACTIVACION_BASE_URL` si se cambia el puerto del frontend.

### Comandos

~~~powershell
docker compose up -d --build
docker compose ps
docker compose down
docker compose down -v
~~~

- `docker compose up -d --build` construye las imágenes y levanta los cinco servicios en orden: MySQL, luego Usuarios, Ventas y Reportes, y por último el frontend. La primera vez tarda varios minutos.
- `docker compose down` detiene y elimina los contenedores, pero conserva la base de datos en el volumen `mysql-data`.
- `docker compose down -v` también elimina el volumen: **borra la base de datos**. Al volver a levantar, se crea de nuevo con los datos de ejemplo y las contraseñas del `.env` vigente.

Las contraseñas de las cuentas MySQL y los hashes de `admin` y `cajero1` se aplican solo cuando se crea la base por primera vez. Si se modifican en `.env` después, es necesario ejecutar `docker compose down -v` para que tengan efecto.

### Puertos

| Servicio | Dirección | Variable |
|---|---|---|
| Frontend | http://localhost:5100 | `FRONTEND_PUBLISHED_PORT` |
| Ventas | http://localhost:5002 | `VENTAS_PUBLISHED_PORT` |
| Usuarios | http://localhost:5003 | `USUARIOS_PUBLISHED_PORT` |
| Reportes | http://localhost:5012 | `REPORTES_PUBLISHED_PORT` |
| MySQL | 127.0.0.1:3308 | `MYSQL_PUBLISHED_PORT` |

Todos los puertos se publican solo en el equipo local. Entre contenedores los servicios se comunican por su nombre dentro de la red interna de Docker. Para levantar otra copia sin choques, usar otro `.env` y otro nombre de proyecto, por ejemplo `docker compose -p copia --env-file otro.env up -d --build`, con puertos distintos.

### Usuarios de prueba

| Usuario | Correo | Rol | Contraseña |
|---|---|---|---|
| admin | admin@example.invalid | Administrador | La usada para generar `ADMIN_PASSWORD_HASH` |
| cajero1 | cajero@example.invalid | Cajero | La usada para generar `CAJERO_PASSWORD_HASH` |

Abrir http://localhost:5100/Account/Login e iniciar sesión. El Administrador ve Usuarios y Reportes; el Cajero solo ve las pantallas de ventas.

Las sesiones y la activación por correo viven en la memoria del frontend: al reiniciarlo, las sesiones abiertas se cierran y hay que iniciar sesión de nuevo.

## Problemas frecuentes

| Síntoma | Comprobación segura |
|---|---|
| Puerto ocupado | Detener el proceso propio anterior; los lanzadores no reemplazan procesos. |
| Falta conexión privada | Crear archivos propios en instalación nueva o indicar las rutas externas existentes; nunca imprimir su contenido. |
| Cuenta existente / estado parcial | Detener la preparación. Inspeccionar host, permisos y archivos; no cambiar contraseñas ni repetir CREATE USER. |
| TLS o permisos rechazados | Revisar REQUIRE SSL y privilegios exactos. No desactivar TLS ni reutilizar root o la cuenta SELECT de Reportes. |
| SQL/manifiesto no coinciden | Revisar la fuente y ejecutar Preparar-CopiaSql.ps1 y Validar-Preparacion.ps1 solo sobre archivos. No reimportar la base existente. |
| Correo falla después del alta | Revisar listado/estado PENDIENTE y SMTP. No afirmar envío ni repetir altas a ciegas. |
| Activación no muestra formulario | Reiniciar frontend con la corrección SameSite=Lax, revisar cookie, host y afinidad. Reabrir enlace si sigue válido tras reinicio. |
| Login deja de funcionar a los cinco minutos | Expiración prevista del JWT; iniciar sesión nuevamente. |
| Certificado o red Docker distintos | Revisar origen efectivo y configuración autorizada; no conceder acceso con comodines para ocultar el problema. |

## Estado de verificación y publicación

La limpieza no ejecuta inicialización, creación de cuentas, correo ni operaciones contra la Demo activa. La instalación desde cero se entrega con guardas y validaciones estáticas; debe comprobarse posteriormente en un entorno nuevo aislado, no borrando el volumen existente.

No se incluye configuración privada del autor, dumps, ZIP ni código del monolito. Los archivos locales ignorados necesarios para el entorno existente pueden seguir en este equipo: nunca forzar su incorporación ni empaquetar indiscriminadamente la carpeta. Revisar exclusiones y secretos antes de publicar. No se ha creado ni publicado un repositorio Git como parte de esta preparación.

Más detalle: [integración actual](doc/integracion/INTEGRACION_VENTAS_REPORTES.md), [despliegue](doc/despliegue/ESTADO_DOCKER.md), [limpieza](doc/LIMPIEZA_REPOSITORIO.md).
