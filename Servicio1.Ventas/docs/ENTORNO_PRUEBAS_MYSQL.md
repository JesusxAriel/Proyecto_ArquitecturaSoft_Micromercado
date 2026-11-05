# Entorno MySQL de integración — Etapa 7A

Fecha: 2026-11-04. Estado: preparado y validado estáticamente, pendiente de revisión y autorización para ejecutar.

**En esta etapa no se iniciaron contenedores, no se crearon bases, no se ejecutó SQL ni se ejecutaron las nueve pruebas de integración.**

## 1. Archivos creados y alcance

Dentro de tests/integration-env/:

- compose.yaml: un único servicio mysql-test.
- .env.example: nombres de variables sin usuario ni contraseñas reales.
- .gitignore: excluye .env, variantes locales y .secrets/.
- Validar-Entorno.ps1: revisa el esquema y valida el modelo de Compose sin usar el daemon.

Este informe: docs/ENTORNO_PRUEBAS_MYSQL.md.

Se revisaron MIGRACION_INFRASTRUCTURE.md, COMPLEMENTOS_VENTAS.md, MySqlTestEnvironment.cs, TransaccionesMySqlTests.cs, PrepararEsquema.ps1 y EsquemaPruebas.sql. Se comparó el esquema con bdMicroMercadoArqui.sql original, de solo lectura.

No se modificaron las pruebas, el generador ni el esquema existentes. Tampoco se modificó el Compose del monolito o la aplicación.

## 2. Configuración y aislamiento

| Propiedad | Valor |
| --- | --- |
| Proyecto Compose | puntomarket-ventas-integration |
| Servicio único | mysql-test |
| Imagen | mysql:8.4 |
| Base fija | puntomarket_ventas_test_local |
| Puerto publicado | 127.0.0.1:3307 → 3306/tcp |
| Volumen | puntomarket-ventas-integration_ventas-test-data |
| Red bridge propia | puntomarket-ventas-integration_ventas-test-net |
| Reinicio automático | Desactivado |
| Usuario root remoto | No habilitado; MYSQL_ROOT_HOST=localhost |
| Esquema automático | Ninguno; no se monta docker-entrypoint-initdb.d |
| Ejecución de tests automática | Ninguna |

Red y volumen son recursos administrados por este proyecto, no external. No se usan red host, redes del monolito, volúmenes de desarrollo, container_name global, depends_on de otros servicios, Redis, Nginx ni API.

Las contraseñas se reciben mediante secrets de Compose montados en /run/secrets y MYSQL_ROOT_PASSWORD_FILE/MYSQL_PASSWORD_FILE; no se escriben valores en el YAML ni en variables de entorno de contraseña del contenedor.

La imagen queda configurada con utf8mb4/utf8mb4_unicode_ci, igual que el esquema original, y zona horaria fija -04:00 para alinear fechas de este entorno con America/La_Paz. log-bin-trust-function-creators=1 permite crear triggers/rutinas en este entorno de pruebas con el usuario de la base; no implica cambiar configuración de un servidor existente.

El healthcheck usa mysqladmin ping: indica que el servidor responde, no verifica credenciales, tablas, marcador ni corrección de las pruebas.

## 3. Requisitos y variables externas

Requisitos para una ejecución futura autorizada:

- Docker Engine/Desktop con contenedores Linux y Docker Compose.
- Puerto 3307 libre. Si está ocupado, la publicación debe fallar; no cambiar silenciosamente a 3306.
- Dos archivos privados de contraseña, distintos y exclusivos de este entorno.
- Usuario de pruebas distinto de root, definido externamente.
- .NET 10 y dependencias restauradas para la futura ejecución de pruebas.
- Espacio para un volumen independiente de MySQL.

Variables de Compose:

| Variable | Contenido |
| --- | --- |
| VENTAS_TEST_USER | Usuario exclusivo de la base, distinto de root |
| VENTAS_TEST_ROOT_PASSWORD_FILE | Ruta absoluta al archivo privado de contraseña root |
| VENTAS_TEST_PASSWORD_FILE | Ruta absoluta al archivo privado de contraseña del usuario |

.env.example deja los tres valores vacíos: Compose rechaza variables obligatorias ausentes. No contiene passwords de ejemplo utilizables ni credenciales reales.

En Windows pueden usarse rutas C:/...; guardar los archivos secretos fuera del repositorio con permisos limitados al usuario que ejecuta Docker. No reutilizar credenciales del monolito o de otros servicios. .gitignore también excluye .secrets/ por si se elige esa ubicación local; en esta etapa no se creó ningún archivo secreto ni .env.

Los secrets de Compose local son montajes de archivos: no constituyen una bóveda cifrada. La protección del archivo de origen y del acceso a Docker sigue siendo responsabilidad del operador. La imagen oficial crea el usuario con permisos sobre MYSQL_DATABASE, no como administrador global.

Referencias: [imagen oficial MySQL y variables _FILE](https://hub.docker.com/_/mysql), [secrets de Compose](https://docs.docker.com/compose/how-tos/use-secrets/), [publicación de puertos en Compose](https://docs.docker.com/reference/compose-file/services/#ports).

## 4. Revisión del esquema

Esquema existente: tests/PuntoMarket.Ventas.Tests/Integration/EsquemaPruebas.sql.

Resultado estático:

- Único USE: puntomarket_ventas_test_local.
- Sin DROP DATABASE, CREATE DATABASE, DEFINER explícito ni SET GLOBAL.
- 12 tablas: las 11 del dominio original y PUNTOMARKET_TEST_GUARD.
- 23 triggers, cuatro vistas y un procedimiento almacenado.
- La sección de estructura del monolito se conserva exactamente.
- No se incluyen las filas semilla originales ni sus contraseñas/hash.
- El INSERT de HISTORIAL_PRECIO aparece dentro de su trigger; no es una carga de datos inicial independiente.
- El único dato de inicialización agregado es el marcador de seguridad.
- DELIMITER $$ y DELIMITER ; delimitan triggers/procedimiento para el cliente mysql.
- Tablas InnoDB, CHECK, columnas generadas, claves foráneas, vistas y triggers corresponden a características admitidas en MySQL 8.4.
- No se fuerza mysql_native_password, deshabilitado por defecto en MySQL 8.4.

SHA-256 del esquema revisado:

~~~text
E7D4ADC097E529B86EDEE7AFFB026F78BE4A495AC0358D97559B5F8BABACBE13
~~~

**Importación prevista solo sobre una base vacía.** El archivo no es idempotente: CREATE TABLE falla si las tablas ya existen; CREATE OR REPLACE VIEW puede reemplazar vistas y DROP PROCEDURE IF EXISTS sp_arqueo_stock elimina únicamente esa rutina dentro de la base seleccionada. No debe ejecutarse sobre una base con datos ni usarse con --force para ignorar errores.

El original bdMicroMercadoArqui.sql contiene DROP DATABASE del monolito y NO debe usarse para inicializar este entorno. El Compose tampoco lo monta.

La compatibilidad descrita es una revisión de estructura y características, no una importación/parsing real realizado por MySQL. La validación efectiva del servidor, permisos y DDL queda pendiente de autorización.

## 5. Validación segura disponible ahora

Desde Servicio1.Ventas:

~~~powershell
./tests/integration-env/Validar-Entorno.ps1
~~~

El script:

1. Lee el esquema local y comprueba base única, ausencia de operaciones globales, siete triggers exigidos por el fixture y marcador.
2. Utiliza valores temporales de interpolación que no son credenciales y rutas de archivos inexistentes de validación; no crea ni lee esos secretos.
3. Invoca únicamente docker compose config --format json, con proyecto, archivo Compose y .env.example explícitos.
4. Inspecciona que exista un solo servicio, puerto loopback fijo, base fija, red/volumen no externos, passwords por secrets y ninguna activación de pruebas.
5. Restaura las variables de proceso anteriores en finally.
6. No invoca Docker up, pull, exec, down, SQL ni dotnet test.

Resultado ejecutado: código 0, Compose válido y comprobaciones estáticas correctas. Versiones observadas: Docker CLI 29.9.1 y Compose v5.6.0.

La CLI puede avisar que no puede leer `$env:USERPROFILE/.docker/config.json` por permisos; eso no impide validar el YAML/modelo. No se solicita acceso a credenciales Docker ni se comprueba el estado del daemon durante la validación estática.

No se crearon redes o volúmenes: los nombres de la tabla anterior son los que Compose resolverá al ejecutarse.

## 6. Pasos futuros, únicamente después de autorización

Los comandos de esta sección son documentación para la próxima etapa. **No se ejecutaron en 7A.**

### Preparar configuración privada

Desde Servicio1.Ventas/tests/integration-env:

~~~powershell
Copy-Item .env.example .env
~~~

Completar .env con usuario y rutas a los dos archivos secretos privados. Los valores de contraseña solo van en esos archivos externos, una contraseña por archivo. No incluir credenciales en el informe, terminal compartida o archivos versionados.

Revisar que no se esté usando un proyecto homónimo anterior. La validación actual no consulta recursos del daemon ni sabe si un volumen antiguo ya existe.

Validar la configuración real sin mostrarla:

~~~powershell
docker compose --project-name puntomarket-ventas-integration --env-file .env -f compose.yaml config --quiet
~~~

### Iniciar el servidor aislado

Solo después de autorizar la creación del contenedor/base/volumen:

~~~powershell
docker compose --project-name puntomarket-ventas-integration --env-file .env -f compose.yaml up -d --wait
docker compose --project-name puntomarket-ventas-integration --env-file .env -f compose.yaml ps
~~~

El entrypoint de la imagen inicializará la base vacía y las cuentas únicamente si el volumen es nuevo. Todavía no importará el esquema y no ejecutará las pruebas.

### Importar el esquema: paso manual separado

Requiere autorización de SQL sobre esta base dedicada, después de comprobar que está vacía.

Desde Servicio1.Ventas/tests/integration-env, copiar únicamente el archivo revisado:

~~~powershell
docker compose --project-name puntomarket-ventas-integration --env-file .env -f compose.yaml cp ../PuntoMarket.Ventas.Tests/Integration/EsquemaPruebas.sql mysql-test:/tmp/ventas-test-schema.sql
docker compose --project-name puntomarket-ventas-integration --env-file .env -f compose.yaml exec mysql-test bash
~~~

Dentro de esa shell del contenedor, usar el usuario de pruebas y pedir contraseña de forma interactiva:

~~~sh
mysql --protocol=socket --user="$MYSQL_USER" --password --database=puntomarket_ventas_test_local --batch < /tmp/ventas-test-schema.sql
~~~

No usar --force. La contraseña debe introducirse en el prompt; no escribirla en el comando. La redirección suministra el SQL en modo batch y el prompt se atiende en la terminal interactiva.

Si la importación falla, detenerse y revisar el error. MySQL DDL no es una importación atómica y puede dejar estructura parcial. No reintentar sobre esa estructura ni borrar/recrear volúmenes automáticamente.

### Detener conservando datos

Desde la carpeta del entorno:

~~~powershell
docker compose --project-name puntomarket-ventas-integration --env-file .env -f compose.yaml stop
~~~

Para retirar solo los contenedores y la red de este proyecto, conservando el volumen:

~~~powershell
docker compose --project-name puntomarket-ventas-integration --env-file .env -f compose.yaml down
~~~

No se proporcionan comandos de limpieza global o eliminación de volúmenes. No usar prune ni el Compose original para administrar este entorno.

## 7. Requisitos y activación de las nueve pruebas

El fixture actual exige:

- PUNTOMARKET_MYSQL_TESTS con valor exacto AUTORIZADO.
- PUNTOMARKET_MYSQL_TEST_CONNECTION con cadena exclusiva de pruebas.
- Nombre de base que cumpla puntomarket_ventas_test_<sufijo>.
- SELECT DATABASE() igual al nombre esperado.
- PUNTOMARKET_TEST_GUARD: fila id=1 y proposito=SOLO_PRUEBAS_VENTAS.
- Siete triggers presentes: trg_venta_bi, trg_venta_bu, trg_venta_bd, trg_detalle_venta_bi, trg_lote_ai, trg_lote_au, trg_lote_bu.
- Motores InnoDB para las tablas principales inspeccionadas.

Configuración futura de conexión: host 127.0.0.1, puerto 3307, base puntomarket_ventas_test_local y usuario dedicado. Contraseña desde la fuente privada; no desde un archivo versionado. El proveedor MySql.Data 26.7.0 ya está en Infrastructure. Acordar las opciones TLS/autenticación de la conexión local al ejecutar; no copiar una cadena de producción.

Compose NO define las variables PUNTOMARKET_MYSQL_TESTS ni PUNTOMARKET_MYSQL_TEST_CONNECTION, y esta etapa no las activa. Tampoco usa ConnectionStrings__Ventas ni MICROMERCADO_PRUEBAS_MYSQL.

No se incluyen comandos de ejecución de pruebas en este procedimiento de 7A: su autorización sigue pendiente. Las nueve pruebas escribirán datos propios, comprobarán commit/rollback, stock, restauración, concurrencia, disposición y clasificación de errores cuando se autoricen.

La última validación de aplicación sigue siendo la de etapa 6: 269 correctas y nueve omitidas. No se volvió a ejecutar ninguna suite en 7A.

## 8. Medidas que evitan destinos accidentales y riesgos

- Proyecto, base, puerto, archivo Compose y red/volumen propios; los comandos documentados usan -f y --project-name explícitos para no seleccionar el Compose del monolito.
- Interfaz publicada fija 127.0.0.1; no 0.0.0.0 ni IPv6 externo. No se publica otro puerto de MySQL.
- Cuenta normal limitada a la base de pruebas y root no habilitado remotamente.
- Sin montaje automático de SQL, rutas de datos del monolito ni activación automática de tests.
- USE del esquema fijado a la base aislada; el fixture comprueba base/marcador antes de escribir.
- Secretos externos, archivos locales ignorados y ninguna contraseña real creada o almacenada en esta entrega.

Límites y riesgos:

- El prefijo/marcador no demuestran aislamiento operativo por sí solos. Un operador con acceso a Docker podría conectar otros contenedores a la red o reutilizar deliberadamente recursos; eso queda fuera de la configuración preparada.
- Los tests actuales permiten otros hosts/puertos si alguien les proporciona otra cadena con base/marcador válidos. Para este entorno, usar exclusivamente loopback:3307. No se modificaron esos contratos/pruebas.
- El puerto 3307 puede estar ocupado; la disponibilidad real queda pendiente de iniciar el entorno autorizado.
- mysql:8.4 fija la familia LTS, no un digest inmutable. La imagen exacta deberá registrarse al descargarla; no se hizo pull.
- Los secretos externos se aplican en la primera inicialización; cambiar archivos/variables no rota automáticamente cuentas en un volumen ya inicializado.
- El volumen conserva datos después de down y de futuras pruebas. Las pruebas no limpian ventas mediante DELETE porque los triggers lo impiden.
- El marcador se crea al final del esquema, pero no reemplaza comprobar que la importación terminó sin errores y que se usó una base vacía.
- La zona horaria del runner debe ser coherente con -04:00 para escenarios que dependen de hoy.
- No se afirma que el entorno arranque o el SQL importe correctamente hasta realizar esas comprobaciones en una etapa autorizada.

## 9. Integridad y cierre

Comparación SHA-256 de los 652 archivos originales fuera de Servicio1.Ventas: ninguno modificado o eliminado y ningún archivo nuevo fuera de la carpeta del servicio.

No se modificaron el monolito, su Compose, SQL original, frontend ni otros microservicios. No se ejecutaron SQL, creación de bases, contenedores ni pruebas. No se crearon commits, ramas o push.

Etapa 7A finalizada para revisión. La ejecución del entorno y las integraciones requieren autorización posterior.
