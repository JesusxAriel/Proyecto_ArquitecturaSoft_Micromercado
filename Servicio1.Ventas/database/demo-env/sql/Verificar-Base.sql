-- SOLO LECTURA. Preparado para una futura ejecucion autorizada, no ejecutado.
USE `puntomarket_ventas_demo_local`;
SELECT DATABASE() AS baseActual, CURRENT_USER() AS cuenta, VERSION() AS versionMySql,
       @@port AS puertoInterno, @@session.time_zone AS zonaHoraria;
SELECT TABLE_TYPE, TABLE_NAME FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'puntomarket_ventas_demo_local' ORDER BY TABLE_TYPE, TABLE_NAME;
SELECT TRIGGER_NAME, EVENT_MANIPULATION, EVENT_OBJECT_TABLE, ACTION_TIMING
FROM information_schema.TRIGGERS
WHERE TRIGGER_SCHEMA = 'puntomarket_ventas_demo_local' ORDER BY TRIGGER_NAME;
SELECT ROUTINE_NAME, ROUTINE_TYPE FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'puntomarket_ventas_demo_local';
SELECT 'ROL' AS tabla, COUNT(*) AS filas FROM ROL
UNION ALL SELECT 'USUARIO', COUNT(*) FROM USUARIO
UNION ALL SELECT 'CATEGORIAS', COUNT(*) FROM CATEGORIAS
UNION ALL SELECT 'PROVEEDOR', COUNT(*) FROM PROVEEDOR
UNION ALL SELECT 'EMPAQUE', COUNT(*) FROM EMPAQUE
UNION ALL SELECT 'PRODUCTO', COUNT(*) FROM PRODUCTO
UNION ALL SELECT 'LOTE', COUNT(*) FROM LOTE
UNION ALL SELECT 'HISTORIAL_PRECIO', COUNT(*) FROM HISTORIAL_PRECIO
UNION ALL SELECT 'CLIENTE', COUNT(*) FROM CLIENTE
UNION ALL SELECT 'VENTA', COUNT(*) FROM VENTA
UNION ALL SELECT 'DETALLE_VENTA', COUNT(*) FROM DETALLE_VENTA;
-- Esperado vacio: stock almacenado distinto de la suma de lotes activos.
SELECT p.id, p.stockActual, COALESCE(SUM(l.cantidadDisponible), 0) AS sumaLotesActivos
FROM PRODUCTO p LEFT JOIN LOTE l ON l.idProducto = p.id AND l.estaActivo = 1
GROUP BY p.id, p.stockActual
HAVING p.stockActual <> COALESCE(SUM(l.cantidadDisponible), 0);
-- Esperado para las semillas originales: precio 6.50 y stock 120 el dia de importacion.
SELECT p.id, p.nombre, p.precioVenta,
       (SELECT COALESCE(SUM(l.cantidadDisponible), 0) FROM LOTE l
        WHERE l.idProducto = p.id AND l.estaActivo = 1 AND l.cantidadDisponible > 0
          AND (l.fechaVencimiento IS NULL OR l.fechaVencimiento >= CURDATE())) AS stockDisponible
FROM PRODUCTO p WHERE p.id = 1 AND p.estaActivo = 1;
