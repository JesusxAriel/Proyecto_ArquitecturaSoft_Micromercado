-- SOLO LECTURA. Ejecutar en una sesión autorizada del servidor Demo.
-- No selecciona valores de contraseñas, hashes, tokens ni configuración privada.
SELECT VERSION() AS version, DATABASE() AS base_actual, CURRENT_USER() AS cuenta_efectiva;
SELECT SCHEMA_NAME FROM information_schema.SCHEMATA
WHERE SCHEMA_NAME = 'puntomarket_ventas_demo_local';
SELECT TABLE_NAME, ENGINE, TABLE_COLLATION
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'puntomarket_ventas_demo_local' AND TABLE_NAME IN ('USUARIO','ROL');
SELECT TABLE_NAME, ORDINAL_POSITION, COLUMN_NAME, COLUMN_TYPE,
       IS_NULLABLE, COLUMN_KEY, EXTRA, CHARACTER_SET_NAME, COLLATION_NAME
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'puntomarket_ventas_demo_local' AND TABLE_NAME IN ('USUARIO','ROL')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
-- Los defaults se restringen a columnas no sensibles.
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_DEFAULT
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'puntomarket_ventas_demo_local' AND TABLE_NAME IN ('USUARIO','ROL')
AND COLUMN_NAME IN ('id','idUsuarioActualizacion','estado','estaActivo','debeCambiarPassword','fechaCreacion','fechaActualizacion');
SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, SEQ_IN_INDEX, COLUMN_NAME, SUB_PART
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = 'puntomarket_ventas_demo_local' AND TABLE_NAME IN ('USUARIO','ROL')
ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;
-- Incluye relaciones entrantes desde Ventas y otras tablas.
SELECT k.TABLE_NAME, k.CONSTRAINT_NAME, k.COLUMN_NAME,
       k.REFERENCED_TABLE_NAME, k.REFERENCED_COLUMN_NAME, r.UPDATE_RULE, r.DELETE_RULE
FROM information_schema.KEY_COLUMN_USAGE k
LEFT JOIN information_schema.REFERENTIAL_CONSTRAINTS r
  ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME
WHERE k.CONSTRAINT_SCHEMA='puntomarket_ventas_demo_local'
  AND (k.TABLE_NAME IN ('USUARIO','ROL') OR k.REFERENCED_TABLE_NAME IN ('USUARIO','ROL'))
ORDER BY k.TABLE_NAME,k.CONSTRAINT_NAME,k.ORDINAL_POSITION;
SELECT t.TABLE_NAME,t.CONSTRAINT_NAME,t.ENFORCED,c.CHECK_CLAUSE
FROM information_schema.TABLE_CONSTRAINTS t
JOIN information_schema.CHECK_CONSTRAINTS c
 ON c.CONSTRAINT_SCHEMA=t.CONSTRAINT_SCHEMA AND c.CONSTRAINT_NAME=t.CONSTRAINT_NAME
WHERE t.TABLE_SCHEMA='puntomarket_ventas_demo_local' AND t.TABLE_NAME IN ('USUARIO','ROL');
SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE, EVENT_MANIPULATION, ACTION_TIMING, ACTION_STATEMENT
FROM information_schema.TRIGGERS
WHERE TRIGGER_SCHEMA='puntomarket_ventas_demo_local' AND EVENT_OBJECT_TABLE IN ('USUARIO','ROL');
-- En MySQL 8 SHOW GRANTS no muestra hashes de autenticación.
-- Describe solo esta cuenta; no prueba los permisos de una futura cuenta Usuarios.
SHOW GRANTS FOR CURRENT_USER;
SELECT CURRENT_ROLE() AS roles_activos;
-- La visibilidad de metadatos depende de los permisos de la sesión.
-- Cero filas o definición de trigger oculta no demuestra ausencia sin acceso suficiente.
