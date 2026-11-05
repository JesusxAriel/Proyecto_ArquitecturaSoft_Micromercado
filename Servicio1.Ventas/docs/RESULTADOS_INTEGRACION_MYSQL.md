# Resultados de integración MySQL — Fase 7B

Fecha: 2026-11-04. Ejecución expresamente autorizada por el usuario, limitada a las nueve pruebas MySQL existentes.

## Verificaciones previas

- Contexto Docker: desktop-linux.
- Contenedor: puntomarket-ventas-integration-mysql-test-1, running y healthy.
- Proyecto/servicio: puntomarket-ventas-integration / mysql-test.
- Única publicación: 127.0.0.1:3307 hacia 3306/tcp.
- Base: puntomarket_ventas_test_local; usuario: ventas_test.
- Conexión comprobada desde el host mediante MySql.Data 26.7.0 con TLS VerifyCA y cifrado de sesión activo.
- La CA pública se obtuvo exclusivamente del contenedor verificado; no se copiaron claves privadas.
- Marcador comprobado: PUNTOMARKET_TEST_GUARD, id=1, proposito=SOLO_PRUEBAS_VENTAS.
- Esquema comprobado: 12 tablas InnoDB, cuatro vistas, 23 triggers y un procedimiento.
- Los siete triggers requeridos por el fixture están presentes.

Se revisaron MySqlTestEnvironment.cs y TransaccionesMySqlTests.cs antes de ejecutarlos. Todas sus conexiones usan la variable exclusiva de pruebas. Los fixtures insertan datos propios identificados por sufijos aleatorios; las operaciones de venta/anulación usan esos identificadores. La actualización de cantidad que fuerza una restauración inválida afecta únicamente al lote creado por ese caso. No hay DROP DATABASE, inicializaciones del monolito ni limpieza global.

## Ejecución y credenciales

Comando ejecutado desde Servicio1.Ventas, mediante un lanzador local:

~~~powershell
dotnet test tests/PuntoMarket.Ventas.Tests/PuntoMarket.Ventas.Tests.csproj --no-restore --filter "Categoria=MySqlIntegration" --logger "console;verbosity=normal"
~~~

El lanzador verificó primero la conexión TLS y el esquema. Después asignó PUNTOMARKET_MYSQL_TESTS=AUTORIZADO y PUNTOMARKET_MYSQL_TEST_CONNECTION únicamente al entorno del proceso hijo dotnet test, sin colocarlas en argumentos, configuración global ni archivos.

La contraseña se leyó del archivo privado de pruebas existente, sin mostrar su contenido. La cadena de conexión completa no se imprimió ni se guardó. La salida del proceso se capturó y se filtró para eliminar cualquier coincidencia con secreto/cadena antes de guardarla o mostrarla.

Los artefactos locales de ejecución, el certificado CA público y el log filtrado están bajo tests/integration-env/.runtime-validation/, excluido del control de versiones. Se añadió esa exclusión a tests/integration-env/.gitignore. No se añadieron paquetes o referencias a la solución del microservicio: el lanzador local usa la versión del proveedor ya existente.

Se confirmó al finalizar que las dos variables de activación no están presentes en el proceso de control.

## Resultados

| Prueba | Resultado |
| --- | --- |
| Commit_persiste_venta_detalles_FEFO_stock_y_consultas | Correcta |
| Un_error_en_el_segundo_producto_revierte_cabecera_detalle_y_stock | Correcta |
| Anulacion_restaura_lotes_y_una_segunda_anulacion_no_restaura_de_nuevo | Correcta |
| Restauracion_que_excede_el_limite_revierte_tambien_el_lote_previamente_restaurado | Correcta |
| Dos_ventas_concurrentes_no_venden_mas_que_el_stock | Correcta |
| Dos_anulaciones_concurrentes_solo_restauran_una_vez | Correcta |
| Dispose_revierte_una_transaccion_incompleta_y_libera_su_conexion | Correcta |
| La_sesion_reutiliza_la_misma_conexion_y_transaccion_y_rechaza_anidamiento | Correcta |
| El_detector_reconoce_el_codigo_mysql_1213_incluso_envuelto_pero_no_1205 | Correcta |

Total: **9 correctas, 0 fallidas, 0 omitidas**. Código de salida: 0. Tiempo total informado por VSTest: 6,1194 segundos.

Solo se ejecutó la categoría MySqlIntegration. Las 269 pruebas anteriores no se volvieron a ejecutar en esta autorización; su resultado previo sigue siendo el de etapa 6.

## Errores y límites de evidencia

No hubo errores de pruebas ni fue necesario analizar un fallo o modificar código/SQL para conseguir resultados correctos.

Se mantuvo una advertencia NU1900 de Infrastructure: no se pudo consultar el índice/datos de vulnerabilidades de NuGet. No impidió compilar ni ejecutar la categoría. No se deshabilitó la auditoría.

La prueba de clasificación usa SIGNAL para obtener excepciones reales 1213/1205 del proveedor: verifica el clasificador, no genera un deadlock real del motor. Las dos pruebas concurrentes verifican los escenarios existentes de dos operaciones; una sola ejecución no demuestra todas las intercalaciones ni sustituye pruebas de carga.

Los escenarios transaccionales sí se ejecutaron contra MySQL real y verificaron commit, rollback, stock, restauración y doble anulación según sus aserciones. No se probaron aquí los endpoints HTTP con una base real ni las consultas complementarias de etapa 6 contra MySQL.

## Estado posterior

El contenedor sigue running y healthy. Conserva la publicación exclusiva 127.0.0.1:3307. Su red dedicada contiene únicamente ese contenedor.

No se eliminaron ni recrearon contenedores, volúmenes o bases. No se ejecutó limpieza: los datos generados por las pruebas permanecen en la base dedicada para inspección. No se importó nuevamente el esquema.

No se modificaron código de producción, pruebas existentes, estructura SQL, monolito, sus bases o recursos de otros servicios. Solo se prepararon artefactos locales de ejecución y este informe. No hubo commits ni push.

## Recomendaciones para la siguiente fase

1. Revisar y aceptar este resultado antes de ampliar el alcance.
2. Autorizar por separado una regresión completa si se desea un resultado conjunto de las 278 pruebas.
3. Preparar integración real de las consultas de productos/clientes/comprobantes y escenarios HTTP sobre esta misma base dedicada.
4. Ampliar concurrencia y reintento de deadlocks con escenarios deterministas y repetición controlada, sin reutilizar bases de otros servicios.
5. Mantener secretos externos, aislamiento y datos actuales; cualquier reinicio limpio o retirada del entorno debe autorizarse expresamente.

La fase se detiene aquí para revisión.
