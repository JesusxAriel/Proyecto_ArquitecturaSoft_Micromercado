# Comprobación previa del entorno MySQL Demo

Actualización: la importación posterior autorizada ya finalizó correctamente; ver `IMPORTACION_DEMO_RESULTADOS.md`. Las secciones siguientes conservan los resultados de las etapas previas.

## Estado actualizado: contenedor iniciado, esquema pendiente

Fecha: 2026-11-04. Arranque autorizado exclusivamente para MySQL Demo, completado satisfactoriamente.

| Comprobación | Resultado actual |
| --- | --- |
| Contexto y motor | desktop-linux; servidor Linux disponible. |
| Puerto antes de crear recursos | 3308 libre, sin listeners. |
| Recursos Demo anteriores | No se encontraron contenedores, redes ni volúmenes del nombre previsto antes del arranque. |
| Configuración privada y copia SQL | Validaciones satisfactorias, sin emitir secretos. |
| Directorio y proyecto Compose | Servicio1.Ventas/database/demo-env/; puntomarket-ventas-demo. Directorio confirmado por etiqueta del contenedor. |
| Servicio y contenedor | mysql-demo; puntomarket-ventas-demo-mysql-demo-1. |
| Estado final | running y healthy. |
| Puerto publicado | Exclusivamente 127.0.0.1:3308 → 3306/tcp; conexión TCP local comprobada. |
| Volumen creado | puntomarket-ventas-demo_ventas-demo-data. |
| Red creada | puntomarket-ventas-demo_ventas-demo-net. |
| Aislamiento | Red única y volumen de datos propios, etiquetas del proyecto Demo verificadas; sin recursos externos compartidos. |
| Base | puntomarket_ventas_demo_local, existente. |
| Objetos existentes | 0 tablas, 0 vistas, 0 triggers, 0 rutinas y 0 eventos. |
| Cuenta Demo | ventas_demo autenticada; ALL PRIVILEGES sobre la base Demo y únicamente USAGE global. |
| Instalación de triggers | log_bin_trust_function_creators=1, como estaba previsto. |
| Importación | No realizada. Base vacía lista para importar, pendiente de nueva autorización. |

Comando ejecutado desde la carpeta Demo:

```powershell
docker compose --project-name puntomarket-ventas-demo --env-file .env -f compose.yaml up -d --wait --no-recreate mysql-demo
```

Solo se creó/inició este servicio con su red y volumen. No hubo errores de arranque y no fue necesario consultar logs. Las comprobaciones posteriores utilizaron SELECT de metadatos y SHOW GRANTS con la cuenta Demo, leyendo su contraseña desde el secreto ya montado dentro del contenedor, sin incluirla en argumentos ni salida. No se ejecutó el script de importación, DDL ni DML de aplicación. Las bases de monolito e integración no se consultaron ni modificaron.

La primera comprobación automática de grants no reconoció los guiones bajos escapados que devuelve MySQL. Se revisó el formato y se repitieron solo las consultas de lectura con normalización del nombre. No se modificaron permisos; la comprobación final fue satisfactoria.

No se modificó la API ni el frontend. El siguiente paso es revisar este resultado y autorizar por separado la importación de la copia adaptada original. La base está lista para **importar**, todavía no tiene el esquema requerido por la API.

## Inspección anterior (registro histórico)

La configuración privada se completó y validó posteriormente; ver `CONFIGURACION_PRIVADA_DEMO.md`. El informe siguiente conserva el resultado de la inspección previa a esa preparación.

Fecha: 2026-11-04. Resultado: **detenido antes del arranque por falta de configuración privada**.

| Comprobación | Resultado |
| --- | --- |
| Motor Docker Desktop | Accesible; servidor Linux, versión 29.9.1. |
| Contexto | desktop-linux. |
| Puerto TCP 3308 | Sin listeners en el momento de la inspección. |
| Contenedores con etiqueta del proyecto puntomarket-ventas-demo | Ninguno. |
| Redes cuyo nombre contiene puntomarket-ventas-demo | Ninguna. |
| Volúmenes cuyo nombre contiene puntomarket-ventas-demo | Ninguno. |
| Configuración privada database/demo-env/.env | No existe. Solo está .env.example. |
| Credenciales MySQL y hashes de semillas | No verificables: no hay configuración de rutas privadas. No se buscaron ni leyeron secretos fuera del repositorio. |
| Aislamiento declarado en compose.yaml | Un único mysql-demo, base puntomarket_ventas_demo_local, loopback 3308, red y volumen propios; sin recursos externos ni referencias al monolito/integración. |
| Arranque, estado healthy e importación | No realizados. |
| Objetos, 77 filas y coherencia del stock en servidor | Pendientes; no se abrió ninguna conexión MySQL. |

La primera consulta al motor fue bloqueada por el sandbox; la inspección de solo lectura se repitió con permiso fuera del sandbox y terminó correctamente. No se crearon ni modificaron recursos Docker. No se consultó el MySQL original ni el de integración en 3307.

## Preparación privada necesaria

1. Crear una carpeta privada fuera del repositorio, exclusiva de demostración (por ejemplo `$env:USERPROFILE/puntomarket-demo-secrets/`) con acceso restringido al usuario local.
2. Crear `mysql-root.txt` y `mysql-ventas.txt` con contraseñas nuevas y distintas, una por archivo, sin comillas añadidas. No reutilizar las de integración, desarrollo o monolito. No enviarlas al chat ni pasarlas como argumentos de terminal.
3. Crear `admin-password-hash.txt` y `cajero-password-hash.txt` con los respectivos hashes BCrypt **originales** de las filas de USUARIO (`admin` y `cajero1`) del script fuente, sin comillas SQL. No inventar hashes ni contraseñas sustitutas. Se pueden copiar con un editor local a esos archivos privados, sin mostrarlos en consola ni en el chat. Estos hashes no son las contraseñas de conexión MySQL.
4. Copiar `.env.example` a `.env` dentro de `Servicio1.Ventas/database/demo-env/` y configurar las rutas siguientes. Este archivo local está excluido por el .gitignore de esa carpeta. Contiene rutas, no secretos:

```dotenv
VENTAS_DEMO_USER=ventas_demo
VENTAS_DEMO_ROOT_PASSWORD_FILE=$env:USERPROFILE/puntomarket-demo-secrets/mysql-root.txt
VENTAS_DEMO_PASSWORD_FILE=$env:USERPROFILE/puntomarket-demo-secrets/mysql-ventas.txt
VENTAS_DEMO_ADMIN_HASH_FILE=$env:USERPROFILE/puntomarket-demo-secrets/admin-password-hash.txt
VENTAS_DEMO_CAJERO_HASH_FILE=$env:USERPROFILE/puntomarket-demo-secrets/cajero-password-hash.txt
```

Las dos últimas variables son rutas propuestas para el posterior importador controlado; Compose no las consume ni carga automáticamente. Al retomar se verificarán los archivos y se cargarán los hashes privadamente en las variables de sesión `@ventas_demo_admin_password_hash` y `@ventas_demo_cajero_password_hash`, en la misma conexión que importe el SQL. No ejecutar todavía el SQL manualmente: sin esas variables, la importación puede quedar parcial.

No se han creado estos archivos privados ni el .env, ni se han inventado credenciales. Cuando estén preparados, basta informar que existen; no enviar su contenido. Se repetirá la inspección antes del arranque. La autorización recibida cubre iniciar e importar, pero su ejecución queda detenida por la instrucción explícita de no continuar si faltan valores.

## Estado para conectar la API

**Todavía no lista**: el contenedor no se ha creado y el esquema no se ha importado. Las verificaciones de 11 tablas, cuatro vistas, 23 triggers, un procedimiento y 77 filas siguen pendientes. No se realizaron ventas ni anulaciones ni se modificó configuración de API/frontend. El SQL original y la copia adaptada permanecen sin cambios.
