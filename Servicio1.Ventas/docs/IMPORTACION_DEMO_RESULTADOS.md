> EVIDENCIA ANTERIOR: describe el entorno original o perfiles retirados. Para instalacion nueva usar [README vigente](../../README.md), con SQL publicable y credenciales propias.

# Importación y verificación de MySQL Demo

Fecha: 2026-11-04. **Resultado: importación correcta, verificada y lista para conectar posteriormente la API Ventas. No se modificó su configuración.**

## Destino y precondiciones

| Elemento | Comprobado |
| --- | --- |
| Contexto | desktop-linux |
| Contenedor | puntomarket-ventas-demo-mysql-demo-1 |
| Proyecto / servicio | puntomarket-ventas-demo / mysql-demo |
| Directorio Compose | Servicio1.Ventas/database/demo-env/ |
| Publicación | Solo 127.0.0.1:3308 → 3306/tcp |
| Base | puntomarket_ventas_demo_local |
| MySQL | 8.4.11 |
| Red / volumen | puntomarket-ventas-demo_ventas-demo-net / puntomarket-ventas-demo_ventas-demo-data |
| Salud inicial y final | running / healthy |
| Objetos antes de importar | 0 tablas/vistas, 0 triggers, 0 rutinas y 0 eventos |
| Cuenta | ventas_demo autenticada, permisos de importación sobre la base Demo |
| Variables privadas | Archivos disponibles; ambos hashes coincidentes con el original antes de importar |
| Configuración de seguridad | foreign_key_checks=1, log_bin_trust_function_creators=1; general_log y slow_query_log desactivados antes de enviar hashes |

La comprobación de destino vacío se realizó con SELECT de metadatos en la cuenta Demo. Se validaron también etiquetas del contenedor, directorio Compose, publicación exclusiva, red y volumen. No se conectó al MySQL original ni al de integración en 3307.

## Importación única

Único archivo de esquema/datos ejecutado: `database/demo-env/sql/PuntoMarket.Ventas.Demo.sql`.

- SHA-256 de la fuente original, leída solo para verificar procedencia: `CE114B889D8EEA391E7E07171FEF2B055BCC2E053746AD1C614344CF79E86930`.
- SHA-256 de la copia importada: `499A0B22D5E4CA78497E195928EF916D079A0D8B00A082287EB241B04AE63514`.
- Ambas huellas permanecen iguales al manifiesto después de importar.
- Se comprobó la ausencia de instrucciones DROP, CREATE/ALTER DATABASE y CREATE OR REPLACE, y el único USE dirigido a la base Demo.
- Los cuerpos originales de tablas, vistas, triggers y procedimiento, y los bloques de semillas adaptados, superaron la comparación estática.
- Se definieron las dos variables de hashes y se envió la copia completa a una **misma sesión MySQL** mediante entrada estándar privada. No se escribieron archivos SQL temporales con hashes ni se incluyeron secretos en argumentos o salida.
- La cuenta obtuvo la contraseña desde el secreto ya montado dentro del contenedor. No se usó root para importar.
- Una sola llamada de importación, código de salida **0**, sin `--force`, sin reconexión automática y sin reintentos.
- No hubo salida adicional ni advertencias del cliente MySQL. Esto no equivale a una auditoría de todos los posibles avisos de deprecación del servidor.

Se añadió `database/demo-env/Importar-EsquemaDemo.ps1` para hacer explícitos estos controles. El registro local `.importacion-iniciada.local.json`, excluido por `.gitignore`, quedó en estado IMPORTADA. Su existencia bloquea otra ejecución automática; además, el control de base vacía impide volver a importar sobre los objetos creados. No retirar el registro para reintentar.

## Objetos creados

| Tipo | Esperados | Encontrados |
| --- | ---: | ---: |
| Tablas | 11 | 11 |
| Vistas | 4 | 4 |
| Triggers | 23 | 23 |
| Procedimientos | 1 | 1 |

Se cotejaron todos los nombres con las definiciones de la copia; no aparecieron objetos adicionales. Las cuatro vistas se consultaron correctamente. `sp_arqueo_stock(30)`, cuyo cuerpo original es exclusivamente de lectura, se ejecutó para comprobar su disponibilidad sin crear ni modificar datos.

## Filas iniciales

| Tabla | Filas verificadas |
| --- | ---: |
| ROL | 2 |
| USUARIO | 2 |
| CATEGORIAS | 8 |
| PROVEEDOR | 7 |
| EMPAQUE | 9 |
| PRODUCTO | 21 |
| LOTE | 22 |
| HISTORIAL_PRECIO | 3 |
| CLIENTE | 3 |
| VENTA | 0 |
| DETALLE_VENTA | 0 |
| **Total** | **77** |

Los 21 productos, tres clientes y 22 lotes están activos. Se verificó que admin (ID 1) y cajero1 (ID 2) existen con estaActivo=1 y estado=ACTIVO. Sus hashes se compararon exactamente, mediante resultado booleano, con los archivos privados previamente cotejados con el original. No se devolvieron hashes al informe ni a la terminal.

## Integridad y stock

- Se encontraron las **26 claves foráneas** del script y se comprobaron con consultas anti-join, incluida la relación compuesta lote/producto de DETALLE_VENTA. Resultado: **0 referencias huérfanas**.
- `foreign_key_checks` permaneció habilitado en las sesiones; no se desactivaron restricciones para importar.
- **0 productos** con stockActual distinto de la suma de cantidades disponibles de sus lotes activos.
- **0 lotes** con cantidades fuera de rango o distintas de su cantidad inicial.
- Stock total inicial en lotes: **1315 unidades**.
- Tres lotes sin vencimiento; cero lotes vencidos al importar.
- Producto 1: precio **6.50**, stock **120**, conforme a las dos partidas originales de 70 y 50 unidades.
- Fecha local de base al verificar: **2026-11-04**. Se conservaron las expresiones de vencimiento relativas originales; con el paso de los días puede cambiar la disponibilidad vendible.

La coherencia de stock confirma que los triggers originales calcularon correctamente los acumulados durante las semillas, sin la actualización masiva redundante retirada de la copia. No se alteraron reglas de negocio, estructura o datos para resolver errores.

`database/demo-env/Verificar-ImportacionDemo.ps1` contiene estas comprobaciones de lectura. Su resumen saneado se guarda en `.verificacion.local.json`, excluido por `.gitignore`; no contiene secretos. Puede volver a consultar el estado, aunque los conteos iniciales dejarán de ser aplicables después de ventas u otras operaciones autorizadas.

## Advertencias y siguiente fase

No hubo fallos de importación ni diferencias de conteos, nombres, hashes o coherencia de stock. La verificación fue de estructura, semillas, relaciones y consultas; **no se realizaron ventas, anulaciones ni pruebas funcionales transaccionales sobre Demo**.

MySQL Demo está listo como destino para la configuración posterior de `ConnectionStrings:Ventas` en la API, usando 127.0.0.1:3308, la base dedicada y su secreto privado. La API todavía no se ha conectado ni se ha validado su acceso contra esta base. Conservar los usuarios originales no implementa login ni resuelve la integración JWT/Usuarios.

Los permisos actuales incluyen DDL porque se necesitaban para importar; antes de una puesta en producción deberá acordarse la separación de cuentas de despliegue y ejecución. No se cambiaron permisos en esta fase. Los hashes originales se conservaron por solicitud; no se ha validado una contraseña de inicio de sesión ni se recomienda reutilizarlos para producción.

No se ejecutó el script original sin adaptar ni EsquemaPruebas.sql. No se ejecutó DROP DATABASE o DROP TABLE, no se sobrescribieron objetos y no se tocaron las bases original o de integración, la API o el frontend. Los únicos cambios de código fueron scripts operativos y documentación dentro de Servicio1.Ventas/database/demo-env/ y Servicio1.Ventas/docs/; los SQL original y adaptado permanecen intactos. No hubo commits ni push.

Se detiene el trabajo antes de configurar cualquier otro componente.
