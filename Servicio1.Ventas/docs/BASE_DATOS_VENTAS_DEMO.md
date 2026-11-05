> EVIDENCIA ANTERIOR: describe el entorno original o perfiles retirados. Para instalacion nueva usar [README vigente](../../README.md), con SQL publicable y credenciales propias.

# Base independiente de Ventas basada en el SQL original

Fecha: 2026-11-04. Estado: archivos preparados y validación estática satisfactoria; **sin arranque, importación, consultas MySQL ni pruebas de integración**.

## Fuente y alcance

Fuente exclusiva: `bdMicroMercadoArqui.sql` de la raíz. SHA-256: `CE114B889D8EEA391E7E07171FEF2B055BCC2E053746AD1C614344CF79E86930`.

No se utilizó `EsquemaPruebas.sql` para generar el esquema ni los datos. Se consultó el Compose de integración como referencia de aislamiento, sin modificarlo ni acceder a su base. También se revisaron `MIGRACION_INFRASTRUCTURE.md`, `COMPLEMENTOS_VENTAS.md`, `ApiBootstrap.cs`, `DatabaseConnection.cs` y los repositorios MySQL de Infrastructure.

El original contiene `DROP DATABASE IF EXISTS`, `CREATE DATABASE` y `USE bdMicroMercadoArqui`, 11 tablas InnoDB, cuatro vistas con `CREATE OR REPLACE`, 23 triggers, `DROP PROCEDURE IF EXISTS` seguido de `CREATE PROCEDURE sp_arqueo_stock`, nueve bloques de semillas, una actualización global de stock y consultas diagnósticas finales. No contiene ventas iniciales.

## Archivos nuevos

Dentro de `Servicio1.Ventas/database/demo-env/`:

- `compose.yaml`: únicamente `mysql-demo`, imagen `mysql:8.4`.
- `.env.example`: usuario y rutas de archivos privados; ninguna contraseña.
- `.gitignore`: excluye configuración local y archivos privados de esta carpeta.
- `Preparar-CopiaSql.ps1`: transformación reproducible de la fuente original; solo escribe la copia y su manifiesto.
- `manifest.json`: procedencia y huellas de ambos SQL.
- `Validar-Preparacion.ps1`: compara objetos y semillas y valida el modelo de Compose sin crear recursos.
- `sql/PuntoMarket.Ventas.Demo.sql`: copia adaptada, con precondiciones explícitas.
- `sql/Verificar-Base.sql`: consultas de lectura preparadas para una fase posterior; no ejecutadas.
- `README.md`: entrada a los pasos y las precauciones de uso.

Además se crea este informe. No se modifica la API, el frontend, Domain, Application, Infrastructure, las pruebas, la solución ni el monolito.

## Adaptaciones de la copia

| Elemento original | Adaptación y motivo |
| --- | --- |
| `DROP DATABASE` | Eliminado; no hay borrado de bases. |
| `CREATE DATABASE` | Eliminado; el entrypoint de MySQL creará solo la base dedicada en un volumen nuevo. |
| `USE` | Únicamente `puntomarket_ventas_demo_local`. |
| `CREATE OR REPLACE VIEW` | `CREATE VIEW`: no sustituir vistas existentes. El cuerpo permanece idéntico. |
| `DROP PROCEDURE IF EXISTS` | Eliminado; conservar solo la creación original. |
| Actualización masiva de `PRODUCTO.stockActual` tras insertar lotes | Retirada por redundante: los triggers originales `trg_lote_ai/au/ad` ya mantienen ese stock. No se retiran las actualizaciones internas de esos triggers, necesarias para las reglas originales. |
| Dos hashes BCrypt en semillas de `USUARIO` | Sustituidos por `@ventas_demo_admin_password_hash` y `@ventas_demo_cajero_password_hash`; se deberán suministrar desde archivos privados en la misma sesión de importación. No se copian secretos de autenticación a nuevos archivos versionados ni se inventan contraseñas. |
| Consultas diagnósticas finales | Fuera del archivo de inicialización. Se prepara una verificación separada de solo lectura. |

Las 11 definiciones de tabla, sus columnas, tipos, índices, claves foráneas y CHECK, los 23 cuerpos de triggers, el procedimiento y los cuerpos de las cuatro vistas se conservan sin cambios. La validación compara sus textos completos, normalizando únicamente `CREATE OR REPLACE VIEW`.

Las inserciones mantienen su orden, IDs, relaciones y valores originales salvo la externalización indicada. Las fechas `DATE_ADD(CURDATE(), INTERVAL ... DAY)` se mantienen: los vencimientos se calcularán el día de la importación. Los tres lotes sin vencimiento conservan NULL; las fechas históricas de precios no cambian.

## Tablas necesarias y datos iniciales

| Tabla | Filas originales | Necesidad |
| --- | ---: | --- |
| ROL | 2 | Roles de los usuarios referenciados; dependencias de triggers. |
| USUARIO | 2 | Cajero, administrador, anulación y auditoría; claves foráneas. |
| CATEGORIAS | 8 | Relaciones obligatorias y validación de producto activo. |
| PROVEEDOR | 7 | Relaciones obligatorias y validación de producto activo. |
| EMPAQUE | 9 | Presentación en búsqueda y descripción de productos. |
| PRODUCTO | 21 | Precio vigente, catálogo y stock agregado. |
| LOTE | 22 | Stock disponible, FEFO, descuento y restauración. |
| HISTORIAL_PRECIO | 3 | Auditoría original y destino del trigger de precios. |
| CLIENTE | 3 | Cliente activo, NIT/CI, razón social y comprobante. |
| VENTA | 0 | Cabeceras, historial, cierre y anulación. |
| DETALLE_VENTA | 0 | Líneas, lote/producto y subtotal generado. |

Total: 77 filas originales. No se añaden productos, clientes, usuarios, ventas ni marcadores de integración. Conservar las tablas auxiliares no añade sus operaciones administrativas al microservicio. `USUARIO` y `ROL` permanecen como dependencia transitoria del esquema original; la integración futura con Usuarios requerirá un acuerdo separado.

Se conservan `vw_productos_con_stock`, `vw_resumen_ventas`, `vw_comprobante_venta`, `vw_lotes_por_vencer` y `sp_arqueo_stock`, aunque los repositorios actuales consultan directamente las tablas. Mantenerlos evita perder las capacidades definidas en la fuente; no implementa el microservicio Reportes.

## Compatibilidad con los adaptadores existentes

| Adaptador | Correspondencia revisada |
| --- | --- |
| MySqlVentaRepository | Cabecera temporal `TMP-`, detalle en proceso, cierre y anulación compatibles con triggers de VENTA/DETALLE_VENTA; columnas de auditoría presentes. |
| MySqlLoteRepository | Cantidades inicial/disponible, estado y vencimiento presentes; CHECK limita restauración; los triggers actualizan stockActual sin que el repositorio lo descuente otra vez. |
| MySqlPrecioVigenteRepository | PRODUCTO.precioVenta/estaActivo y EMPAQUE.nombre. |
| MySqlProductoVentaConsultaRepository | PRODUCTO + EMPAQUE y suma de lotes activos, positivos y no vencidos; consultas de lectura sin FOR UPDATE. |
| MySqlClienteActivoConsulta / MySqlClienteVentaConsultaRepository | CLIENTE.id/nitCi/razonSocial/estaActivo. |
| MySqlVentaConsultaRepository | VENTA, CLIENTE, USUARIO, DETALLE_VENTA, PRODUCTO y LOTE; nombres de cajero/anulador y código de lote. |
| MySqlComprobanteRepository | Número, fecha, estado, total, cliente, username del cajero y subtotal histórico generado en DETALLE_VENTA. |

No se detectaron columnas faltantes ni nombres de esquema fijados en las consultas. Se conserva el uso de mayúsculas de tablas, importante en MySQL sobre Linux. La clave foránea compuesta DETALLE_VENTA(idLote,idProducto) referencia el índice UNIQUE original LOTE(id,idProducto), sin depender de claves referenciadas no únicas.

InnoDB, utf8mb4/utf8mb4_unicode_ci, CHECK, columnas generadas y DELIMITER del cliente mysql son adecuados para MySQL 8.4. Se configura zona horaria `-04:00` y `log_bin_trust_function_creators=1` para permitir instalar los triggers con el usuario limitado a la base dedicada. Es un ajuste exclusivamente local de demostración; no se propone para producción. Referencias: [triggers MySQL 8.4](https://dev.mysql.com/doc/refman/8.4/en/triggers.html) y [rutinas y registro binario](https://dev.mysql.com/doc/refman/8.4/en/faqs-stored-procs.html).

Esta es compatibilidad **estática**, no una importación validada en servidor. Los nueve tests que ya pasaron en 3307 no certifican esta nueva copia. No deben ejecutarse contra la demostración: este entorno no incluye el marcador de seguridad de integración ni su nombre de base.

## Aislamiento y configuración privada

| Recurso | Valor previsto |
| --- | --- |
| Proyecto Compose | puntomarket-ventas-demo |
| Servicio | mysql-demo |
| Contenedor esperado | puntomarket-ventas-demo-mysql-demo-1 |
| Base | puntomarket_ventas_demo_local |
| Publicación | 127.0.0.1:3308 → 3306 del contenedor |
| Volumen | puntomarket-ventas-demo_ventas-demo-data |
| Red bridge propia | puntomarket-ventas-demo_ventas-demo-net |
| Usuario de ejemplo | ventas_demo; distinto de root |

Sin redes o volúmenes `external`, sin montajes del monolito, sin montaje de scripts SQL y sin dependencia de integración en 3307. Compose publica solo el puerto clásico MySQL; no publica 33060. Root queda limitado a localhost del contenedor. No hay inspección ni cambios de recursos Docker en esta fase; la ausencia de colisiones se comprobará antes del futuro arranque. El puerto está configurado, no reservado.

Cuando se autorice preparar credenciales, crear fuera del repositorio archivos exclusivos de esta demostración (por ejemplo en una carpeta privada distinta de `puntomarket-test-secrets`). Deben tener ACL de acceso restringido. No reutilizar contraseñas del monolito ni de integración. Copiar `.env.example` a `.env` y configurar únicamente:

- `VENTAS_DEMO_USER`: usuario dedicado no root.
- `VENTAS_DEMO_ROOT_PASSWORD_FILE`: ruta absoluta al archivo privado de root.
- `VENTAS_DEMO_PASSWORD_FILE`: ruta absoluta al archivo privado del usuario de demostración.

No se ha creado `.env` ni ningún archivo de contraseñas. Los archivos privados no deben colocarse en esta carpeta, aunque existan exclusiones locales. Compose secrets en Docker local son archivos montados, no una bóveda cifrada. El entrypoint oficial admite variables `_FILE`, crea el usuario con permisos sobre `MYSQL_DATABASE` y solo inicializa en un directorio de datos nuevo: [código oficial de la imagen](https://github.com/docker-library/mysql/blob/master/8.4/docker-entrypoint.sh).

Los hashes de las dos semillas USUARIO son distintos de las contraseñas de conexión MySQL. Antes de importar se requerirán los hashes originales en archivos privados; su carga se hará en memoria, sin argumentos visibles, sin logs ni archivos SQL temporales con secretos, y con parámetros o un canal de entrada privado. No se generan ni sustituyen por valores inventados. Si faltan, detenerse **antes de ejecutar DDL**. Las variables de sesión deben definirse en la misma conexión que importe toda la copia. El SQL por sí solo no comprueba esta precondición antes de crear tablas: no ejecutarlo manualmente de forma aislada.

## Inicialización posterior por puntos de control (no ejecutada)

1. Con autorización de arranque: verificar contexto Docker Linux, disponibilidad de 3308, etiquetas del proyecto y ausencia de contenedor/red/volumen anteriores con estos nombres. Si existen, detenerse y revisar; no reutilizar, recrear ni borrar automáticamente.
2. Verificar archivos privados por existencia/permisos sin mostrar su contenido, usuario no root y modelo resuelto de Compose. Validar con `docker compose --project-name puntomarket-ventas-demo --env-file .env -f compose.yaml config --quiet` desde `database/demo-env/`. No imprimir `config` con variables privadas de otros entornos.
3. Solo después de autorización explícita, el comando previsto será `docker compose --project-name puntomarket-ventas-demo --env-file .env -f compose.yaml up -d --wait mysql-demo`. No se ha ejecutado. En volumen nuevo el entrypoint crea la base vacía y las cuentas propias. Ningún SQL del repositorio se importa automáticamente.
4. Detenerse para revisar contenedor healthy, etiquetas, puerto, volumen/red y destino. Healthcheck comprueba respuesta del servidor; no acredita esquema ni permisos.
5. Con autorización separada de importación: comprobar DATABASE()/CURRENT_USER(), permisos sobre la base dedicada y **cero tablas, vistas, triggers, rutinas y eventos** en esa base. Revisar de nuevo las huellas de fuente/copia y los hashes privados de semillas. Si aparece cualquier objeto o falta configuración, detenerse.
6. Importar una única vez mediante el cliente `mysql` del contenedor de demostración, con cuenta dedicada y base explícita, cargando primero las variables de hashes en esa misma sesión. Sin `--force` y sin imprimir secretos. No usar el SQL original directamente ni EsquemaPruebas.sql. El procedimiento concreto de importación segura queda para esa autorización; no se incluye un importador ejecutable en esta fase.
7. Ante cualquier error, guardar solo código/línea/mensaje saneado y detenerse. DDL hace commits implícitos: puede quedar una importación parcial. No intentar rollback global, reimportar, borrar tablas ni eliminar volumen para resolverlo automáticamente.

Para detener el contenedor más adelante, usar `docker compose --project-name puntomarket-ventas-demo --env-file .env -f compose.yaml stop mysql-demo`. No se incluye ningún comando de eliminación de recursos. Cambiar `.env` o las contraseñas en archivos tras inicializar no modifica automáticamente las cuentas ya creadas.

## Configuración posterior de API (sin editarla ahora)

`ApiBootstrap` ya consume `ConnectionStrings:Ventas`; `DatabaseConnection` exige una base y habilita variables de usuario. Para un proceso API ejecutado en Windows se usará la variable **de proceso** `ConnectionStrings__Ventas`, construida en memoria desde el secreto privado. Parámetros previstos:

| Parámetro | Valor |
| --- | --- |
| Server | 127.0.0.1 |
| Port | 3308 |
| Database | puntomarket_ventas_demo_local |
| User ID | Usuario dedicado de `.env`, inicialmente ventas_demo |
| Password | Leída del archivo privado; nunca literal en código, argumentos o informes |
| SslMode | Required para cifrado local; verificar el soporte al conectar, sin desactivar TLS automáticamente |

No se guarda una cadena de conexión completa ni se cambia appsettings. En la futura fase, utilizar MySqlConnectionStringBuilder para codificar correctamente contraseñas con caracteres especiales y pasar la configuración solo al proceso API; no imprimirla ni persistirla. `Required` cifra pero no valida la identidad del servidor: este diseño es local de demostración, no configuración de producción. La API mantiene su puerto 5002. JWT sigue siendo configuración privada independiente; no se han creado tokens, claves ni autenticación. El ID de usuario del futuro JWT deberá corresponder al usuario activo original en USUARIO (IDs 1 y 2); conservar esas filas no implementa login ni integra Usuarios. No conectar todavía el frontend.

## Verificación futura de base lista

Después de importar, ejecutar con autorización `sql/Verificar-Base.sql`: exclusivamente lecturas, sin passwordHash. Comprobar 11 tablas, cuatro vistas, 23 triggers y el procedimiento; cotejar nombres/definiciones con la copia, claves foráneas y CHECK mediante metadatos. Confirmar los conteos de semillas de la tabla anterior, VENTA/DETALLE_VENTA vacías y ausencia de diferencias entre PRODUCTO.stockActual y suma de lotes activos.

El día de importación, producto 1 (Leche Entera PIL 1L) tiene precio 6.50 y 120 unidades en sus lotes originales (70 y 50). Después los vencimientos relativos pueden reducir disponibilidad: no confundir stock activo almacenado con stock vendible no vencido. Verificar los tres lotes con vencimiento NULL y conservar FEFO del dominio. Confirmar permisos de la cuenta, healthy y publicación externa únicamente 127.0.0.1:3308 mediante inspect del puerto del contenedor (MySQL reportará su puerto interno 3306).

En otra fase autorizada se podrá probar la API por lecturas y después demostrar una venta/anulación real contra esta base. Ninguna comprobación de esta fase actual escribió o consultó MySQL.

## Validación realizada e integridad

`Validar-Preparacion.ps1` terminó correctamente: definiciones originales conservadas (11/4/23/1), nueve bloques de semillas equivalentes (77 filas) salvo externalización de hashes, huellas fuente/copia válidas, único USE dedicado y ausencia de DROP, CREATE DATABASE, ALTER DATABASE y CREATE OR REPLACE. Docker Compose resolvió satisfactoriamente el modelo usando solo `.env.example`; puerto, base, imagen, red y volumen propios comprobados. Se validó la configuración fuera del sandbox porque este bloqueaba el acceso a la configuración local de Docker; no se utilizó el motor para crear recursos.

No se ha confirmado disponibilidad actual de 3308 ni ausencia de recursos existentes: son requisitos del futuro punto de control de arranque. No se ha descargado imagen, iniciado contenedor, creado base, importado SQL o ejecutado pruebas. No se necesitó compilar porque no se cambió código .NET.

Solo se crean archivos bajo `Servicio1.Ventas/database/demo-env/` y este informe. La comparación SHA-256 antes/después de los 652 archivos fuera de Servicio1.Ventas no detectó modificaciones, altas ni bajas; incluye el SQL original. No se cambian archivos existentes del microservicio, ni recursos Docker del monolito o de integración. No se crean commits ni push.
