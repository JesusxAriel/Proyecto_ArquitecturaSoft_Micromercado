# Migración de Infrastructure — Etapa 4

Fecha: 2026-11-04. .NET 10. Adaptadores reales MySQL; sin API REST ni JWT.

## 1. Archivos originales consultados

En Proyecto_Arquitectura_Micromercado/:

- Infrastructure/Persistence/MySqlVentaRepository.cs.
- Infrastructure/Persistence/MySqlVentaConsultaRepository.cs.
- Infrastructure/Persistence/MySqlLoteRepository.cs.
- Infrastructure/Persistence/MySqlProductRepository.cs, especialmente ObtenerPrecioVigenteAsync y ObtenerDescripcionAsync.
- Infrastructure/Persistence/MySqlClienteConsultaRepository.cs.
- Infrastructure/Persistence/MySqlUnidadDeTrabajo.cs.
- Infrastructure/Database/DatabaseConnection.cs.
- Infrastructure/Persistence/DetectorDeInterbloqueoMySql.cs.
- Infrastructure/Persistence/Errores/ErroresDePersistencia.cs y RepositoriosConErroresTraducidos.cs.
- Infrastructure/Factories/CreatorRepositorio.cs, CreatorVentaRepository.cs, CreatorVentaRepositoryMySql.cs y CreatorLoteRepositoryMySql.cs.
- Proyecto_Arquitectura_Micromercado.csproj: versión de MySql.Data.

También se consultaron bdMicroMercadoArqui.sql (tablas, claves, restricciones, triggers y estructura para preparar el esquema), StockActualMySqlTests.cs y UnidadDeTrabajoTests.cs originales, y los informes previos de migración/auditoría de esta conversación. Se revisaron los puertos actuales de Application.

## 2. Adaptadores implementados

Bajo src/PuntoMarket.Ventas.Infrastructure/:

| Archivo | Responsabilidad |
| --- | --- |
| Database/DatabaseConnection.cs | Fábrica inmutable de conexiones, sin singleton ni conexión global |
| Persistence/MySqlUnidadDeTrabajo.cs | Conexión/transacción por operación, alquiler y liberación |
| Persistence/MySqlVentaRepository.cs | Cabecera, detalle, cierre, consulta, total diario y anulación |
| Persistence/MySqlVentaConsultaRepository.cs | Historial y detalle con filas y datos de auditoría |
| Persistence/MySqlLoteRepository.cs | Bloqueo, descuento condicionado, restauración y baja |
| Persistence/MySqlPrecioVigenteRepository.cs | Solo precio activo y descripción; extraído del repositorio de productos |
| Persistence/MySqlClienteActivoConsulta.cs | Solo existencia de cliente activo |
| Persistence/DetectorDeInterbloqueoMySql.cs | Reconoce MySqlException.Number 1213, incluso envuelta |
| Persistence/PersistenciaVentasMySql.cs | Agrupa adaptadores sobre una misma unidad por operación |
| Properties/AssemblyInfo.cs | Acceso interno limitado al ensamblado Tests para verificar alquileres |

Namespaces propios PuntoMarket.Ventas.Infrastructure.*. No se copiaron CRUD administrativos, adaptadores Redis, autenticación, generación HTTP ni fábricas de entidades administrativas.

Se reutiliza MySql.Data 26.7.0, exactamente la versión del monolito, como PackageReference exclusiva de Infrastructure. Tests añade una referencia a Infrastructure para las pruebas correspondientes. Domain, Application, Api y sus archivos de proyecto no se editaron.

## 3. Contratos de Application satisfechos

- IUnidadDeTrabajo → MySqlUnidadDeTrabajo.
- IVentaRepository → MySqlVentaRepository.
- IVentaConsultaRepository → MySqlVentaConsultaRepository.
- ILoteRepository → MySqlLoteRepository.
- IConPrecioVigente → MySqlPrecioVigenteRepository.
- IClienteActivoConsulta → MySqlClienteActivoConsulta.
- IDetectorDeInterbloqueo → DetectorDeInterbloqueoMySql.

No se alteró ningún contrato ni implementación de Application o Domain. El usuario de auditoría sigue siendo un puerto de entrada pendiente de la API; Infrastructure recibe los ids y los persiste, sin implementar identidad.

Las fábricas/decoradores originales se inspeccionaron, pero no se copiaron. Su traducción exige ErrorDePersistenciaException, que no forma parte de Application actual. Para no modificar sus contratos sin autorización, los adaptadores propagan MySqlException; las fachadas ya capturan Exception para rollback y el detector reconoce el código del proveedor. Las excepciones de stock del dominio se conservan. No hay cambio en las firmas ni una dependencia de compilación de Application hacia MySQL; la futura API deberá mapear los fallos técnicos sin exponer detalles de SQL.

## 4. Diseño de conexión y transacción

DatabaseConnection conserva únicamente una cadena normalizada y crea MySqlConnection nuevas. No abre conexiones en el constructor ni contiene credenciales predeterminadas.

PersistenciaVentasMySql crea una unidad y entrega esa misma instancia a ventas, lotes, precios, clientes y consultas. Debe crearse una sesión por caso de uso y liberarse con await using. No compartir la sesión entre solicitudes ni utilizarla en paralelo.

MySqlUnidadDeTrabajo:

1. IniciarAsync abre una conexión y comienza una transacción real del proveedor; rechaza transacciones anidadas.
2. AlquilarAsync devuelve la misma conexión y MySqlTransaction a todos los participantes durante la transacción. Disponer un alquiler prestado no cierra la conexión compartida.
3. Las lecturas ordinarias fuera de transacción obtienen una conexión propia, que su alquiler libera.
4. Escrituras y lecturas con FOR UPDATE exigen una transacción activa: fallan antes de abrir conexión si se intenta utilizarlas en autocommit.
5. ConfirmarAsync ejecuta CommitAsync. Si falla conserva los recursos para el rollback solicitado por Application.
6. RevertirAsync ejecuta RollbackAsync y libera conexión/transacción en finally.
7. DisposeAsync revierte una transacción incompleta y libera recursos incluso ante fallos. Es idempotente.
8. Si falla abrir la conexión/iniciar la transacción o una conexión de lectura propia, se dispone la conexión creada.
9. Una unidad dispuesta rechaza nuevas operaciones.

Application conserva la responsabilidad de confirmar únicamente después de completar todos los pasos. No se crean transacciones independientes dentro de cada repositorio. No se implementan transacciones ficticias en producción.

La sesión es una ayuda de composición de persistencia, no un contenedor global ni un endpoint. En la futura composición se deberá mantener una sola unidad por operación, incluso si se decide reemplazar la sesión por registros scoped en DI.

## 5. SQL reutilizado y ajustes técnicos

Se conservan nombres y columnas reales, parámetros y lógica del monolito:

- VENTA: cabecera temporal TMP-UUID(), idCliente/idUsuario, NOW(), estado COMPLETADA.
- DETALLE_VENTA: idVenta, idProducto, idLote, cantidad y precioUnitario. subtotal sigue generado por MySQL.
- Cierre: actualización de nroComprobante y montoTotal.
- Anulación: lectura de VENTA con FOR UPDATE, lectura de sus detalles e identificación exacta de lotes; actualización de estado, motivo, idUsuarioAnulacion y fechaAnulacion.
- Total diario: SUM de montoTotal, DATE(fechaVenta)=CURDATE(), solo COMPLETADA.
- Historial: join CLIENTE, filtro incluirAnuladas, orden fecha/id descendente, límite parametrizado.
- Detalle: joins CLIENTE, USUARIO cajero/anulador, PRODUCTO y LOTE; filas ordenadas por id.
- Precio: PRODUCTO activo con FOR UPDATE. Descripción: PRODUCTO + EMPAQUE.
- Cliente: SELECT EXISTS sobre CLIENTE activo.
- Lotes: activos con stock y FOR UPDATE; vencidos con stock y bloqueo para baja.
- Descuento: UPDATE condicionado a cantidadDisponible >= cantidad.
- Restauración: UPDATE condicionado a cantidadDisponible + cantidad <= cantidadInicial.
- Baja: estaActivo=0 e idUsuarioActualizacion, conservando el comportamiento original.

Ajuste técnico: la lectura de detalles para anulación añade ORDER BY id para un recorrido determinista, sin cambiar lotes, cantidades ni reglas. Además se impide autocommit en las operaciones transaccionales. No se alteró el script SQL original.

La capa de consulta se adaptó para ejecutar directamente el SQL sin el decorador de errores original. No se modificaron sus agrupaciones de Application ni se introdujo caché.

## 6. Stock, restricciones y concurrencia

Se conserva la selección FEFO de Domain; el repositorio bloquea los lotes y el algoritmo determina el orden por fecha/nulos/id. El precio/producto se bloquea antes de descontar, como en el monolito.

- El descuento condicionado evita cantidades disponibles negativas.
- La restauración condicionada impide superar cantidadInicial.
- La venta conserva cada idLote y cantidad en sus detalles.
- ObtenerParaAnularAsync bloquea la cabecera hasta commit/rollback; una segunda anulación por la fachada debe observar ANULADA y no volver a restaurar.
- El detector recorre InnerException y reconoce únicamente el código 1213. No confunde lock timeout 1205 con deadlock. Application conserva su máximo de dos intentos de registro.

Restricciones verificadas estáticamente en el SQL: tablas InnoDB, CK_Lote_cantidades, FK_DetalleVenta_LoteProducto, subtotal generado, checks de cantidades/precios y estado/anulación. Triggers relevantes: validación de cliente/usuario activo al crear venta, inmutabilidad de venta anulada y de detalles, validación de anulación y mantenimiento de PRODUCTO.stockActual por delta de LOTE.

No se vuelve a actualizar stockActual desde C#: lo hacen los triggers dentro de la misma transacción. Estas garantías están implementadas en código y dependen del esquema real; su funcionamiento contra un servidor aún no fue verificado en esta ejecución.

## 7. Pruebas ejecutadas y pendientes

Se mantuvieron sin editar las 86 pruebas previas.

Archivos nuevos:

- tests/PuntoMarket.Ventas.Tests/Infrastructure/PersistenciaMySqlTests.cs.
- tests/PuntoMarket.Ventas.Tests/Infrastructure/ProteccionEntornoMySqlTests.cs.
- tests/PuntoMarket.Ventas.Tests/Integration/MySqlTestEnvironment.cs.
- tests/PuntoMarket.Ventas.Tests/Integration/TransaccionesMySqlTests.cs.
- tests/PuntoMarket.Ventas.Tests/Integration/PrepararEsquema.ps1.
- tests/PuntoMarket.Ventas.Tests/Integration/EsquemaPruebas.sql, generado localmente; NO ejecutado.

Resultados:

| Grupo | Correctas | Fallidas | Omitidas |
| --- | ---: | ---: | ---: |
| Domain/Application anteriores | 86 | 0 | 0 |
| Unitarias nuevas de Infrastructure/protección | 16 | 0 | 0 |
| Integración MySQL preparada | 0 | 0 | 9 |
| Total descubierto | 102 | 0 | 9 |

Las unitarias verifican fábrica sin conexiones globales, validación de configuración, cierre/idempotencia, rechazo de autocommit de todas las operaciones bloqueantes o de escritura, cancelación antes de abrir conexión, rechazo de falsos deadlocks y restricciones del nombre de base aislada. No abren conexiones a MySQL.

Las nueve integraciones llaman a los adaptadores y casos de uso reales y están preparadas para verificar:

1. Commit, detalles FEFO, descuento, stockActual y consultas.
2. Rollback de cabecera/detalles/lotes al fallar un segundo producto.
3. Restauración por anulación, auditoría y rechazo de la segunda anulación.
4. Fallo del segundo lote al restaurar por encima del inicial y rollback del lote restaurado previamente.
5. Dos ventas concurrentes que solicitan más que el stock total.
6. Dos anulaciones concurrentes de la misma venta.
7. Rollback al disponer una sesión sin confirmar.
8. Identidad de conexión/transacción, rechazo de anidamiento y reutilización después de cerrar.
9. Clasificación de errores MySqlException reales 1213/1205 emitidos por SIGNAL. Este último caso verifica clasificación, no produce un deadlock real.

Cada caso de datos crea sus propias referencias y lotes mediante SQL de fixture sobre la base dedicada; no usa ids ni datos del monolito. La concurrencia utiliza dos sesiones y una barrera de salida. Los escenarios dejan sus datos de prueba para inspección: no hacen DROP, DELETE, TRUNCATE ni deshabilitan constraints/triggers.

No se ejecutó ninguna integración: faltan configuración y autorización explícitas de una base aislada. Las omisiones se reportan mediante Skip de xUnit, no como pruebas correctas con retorno temprano.

## 8. Configuración MySQL y preparación aislada

### Uso productivo futuro

La cadena se recibe por constructor, sin credenciales incrustadas. Ejemplo de composición futura (no añadido a Program.cs):

~~~csharp
var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__Ventas")
    ?? throw new InvalidOperationException("Falta configuración MySQL de Ventas.");
var conexiones = new DatabaseConnection(cadena);
await using var persistencia = new PersistenciaVentasMySql(conexiones);
// Entregar persistencia.Unidad/Ventas/Lotes/Precios/Clientes/Consultas
// a las fachadas de Application de una sola operación.
~~~

DatabaseConnection puede compartirse como fábrica inmutable; las sesiones/unidades/conexiones activas no. Las opciones de host, usuario, contraseña, TLS, tiempos de espera y pooling se suministran externamente. Api aún no lee esta configuración ni compone fachadas.

### Integración: activación deliberada

Requiere un servidor y base exclusivamente de pruebas. No reutiliza ConnectionStrings__Ventas ni MICROMERCADO_PRUEBAS_MYSQL.

1. Un administrador debe crear y autorizar una base vacía dedicada con nombre puntomarket_ventas_test_<sufijo>, y un usuario restringido a ella.
2. Generar el esquema desde Servicio1.Ventas, sin conectar al servidor:

~~~powershell
./tests/PuntoMarket.Ventas.Tests/Integration/PrepararEsquema.ps1 -DatabaseName puntomarket_ventas_test_local
~~~

El script solo lee el original y genera EsquemaPruebas.sql. Extrae tablas, vistas, triggers y rutinas; excluye DROP/CREATE DATABASE y los datos semilla originales. Añade USE con nombre validado y una tabla PUNTOMARKET_TEST_GUARD con el propósito SOLO_PRUEBAS_VENTAS. El archivo generado ya está disponible para el nombre local indicado. No es una migración ejecutada.

3. Después de revisar el destino y autorizar la importación, importar manualmente ese esquema a la base vacía dedicada. No ejecutar el script original directamente: contiene DROP DATABASE del monolito. No se importó ningún esquema en esta ejecución.
4. Proporcionar explícitamente en el entorno del ejecutor:
   - PUNTOMARKET_MYSQL_TESTS con valor exacto AUTORIZADO.
   - PUNTOMARKET_MYSQL_TEST_CONNECTION con la cadena de la base exclusiva.
5. Ejecutar desde Servicio1.Ventas:

~~~powershell
dotnet test tests/PuntoMarket.Ventas.Tests/PuntoMarket.Ventas.Tests.csproj --no-restore --filter "Categoria=MySqlIntegration"
~~~

Antes de cualquier escritura, el fixture valida nombre, SELECT DATABASE(), marcador, triggers requeridos y motores InnoDB. Configuración ausente significa Skip; configuración explícita pero insegura/incompleta provoca fallo. El prefijo/marcador no prueban por sí solos aislamiento operativo: el administrador debe garantizar que la base es dedicada.

El esquema requiere una versión MySQL compatible con las características originales, incluidas CHECK, columnas generadas, triggers y rutinas. No se provisionaron servidores ni contenedores.

## 9. Riesgos conservados o detectados

- No se afirma validación real de commit/rollback/concurrencia hasta ejecutar las integraciones en el entorno autorizado.
- Venta toma bloqueo de PRODUCTO antes de LOTE; restauración/baja actualizan LOTE y sus triggers actualizan PRODUCTO. Puede haber inversión de orden de bloqueos. Se conserva para no cambiar reglas; registro reintenta deadlocks, anulación no.
- MarcarAnuladaAsync conserva WHERE por id; la protección de doble anulación depende del bloqueo previo y estado comprobado por la fachada, más triggers. No usar escrituras aisladas por fuera del flujo.
- RestaurarAsync conserva la posibilidad de restaurar un lote inactivo; la cantidad de esa fila puede crecer sin integrarse en stockActual vendible. No se cambió esta regla.
- El descuento/restauración conserva la auditoría de lote del monolito: no asigna idUsuarioActualizacion. La venta/anulación sí conserva usuario.
- Cliente activo se valida antes de iniciar la transacción; el trigger de inserción vuelve a exigirlo.
- CURDATE()/NOW() del servidor y TimeProvider de Application pueden diferir de zona horaria. No se cambiaron reglas de fecha/comprobante.
- El detalle de consulta usa dos SELECT; fuera de una transacción una anulación concurrente puede producir una lectura de cabecera de un instante anterior. Se preserva la consulta original.
- Un rollback o disposición que falle puede ocultar el error inicial. Los recursos se liberan en finally, pero Application mantiene su política previa.
- Un fallo de conexión durante commit puede dejar resultado incierto; no se añadió idempotencia.
- Se propagan excepciones del proveedor en lugar del decorador ausente de Application; la futura API necesitará mapearlas de forma segura.
- NU1900: no se pudo consultar el índice/datos de vulnerabilidades de NuGet. No se deshabilitó la auditoría.

## 10. Compilación y referencias

Comandos ejecutados desde Servicio1.Ventas:

~~~powershell
dotnet restore PuntoMarket.Ventas.slnx
dotnet build PuntoMarket.Ventas.slnx --no-restore
dotnet test tests/PuntoMarket.Ventas.Tests/PuntoMarket.Ventas.Tests.csproj --no-restore
~~~

Todos finalizaron con código 0. Build: 0 errores, 3 advertencias NU1900 (Infrastructure, Api por dependencia transitiva y Tests). Tests: 102 correctas, 0 fallidas, 9 omitidas de 111 descubiertas.

Referencias inspeccionadas:

| Proyecto | Referencias de proyecto | Paquetes directos |
| --- | --- | --- |
| Domain | Ninguna | Ninguno |
| Application | Domain | Ninguno |
| Infrastructure | Application, Domain | MySql.Data 26.7.0 |
| Api | Application, Infrastructure | Ninguno |
| Tests | Domain, Application, Infrastructure | Test SDK y xUnit previos |

No hay referencias circulares ni referencias al csproj del monolito. Application/Domain continúan sin dependencias técnicas externas. Api permanece con su código de arranque previo, sin endpoints ni JWT.

## 11. Integridad y cierre

Comparación SHA-256 de los 652 archivos originales fuera de Servicio1.Ventas antes y después: 0 modificados, 0 eliminados y 0 archivos nuevos fuera de la carpeta autorizada.

Solo se crearon o editaron archivos dentro de Servicio1.Ventas. No se modificaron el monolito, sus pruebas, SQL original, frontend, Docker Compose ni Dockerfile originales. No se ejecutó SQL, no hubo conexiones a una base de datos, migraciones, contenedores, Redis/Nginx, commits, ramas ni push.

Etapa 4 finalizada como implementación y preparación de pruebas. La validación de integración real queda pendiente del entorno aislado. No se avanzó con API REST ni JWT.
