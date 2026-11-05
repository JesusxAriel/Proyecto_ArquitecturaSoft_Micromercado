-- MIGRACION REVISABLE MySQL 8.4. NO EJECUTADA. Requiere autorización separada.
-- Solo USUARIO en puntomarket_ventas_demo_local; no ejecutar el SQL principal.
-- El usuario confirmó que la columna falta en la Demo; se vuelve a comprobar aquí.
-- Ejecutar secuencialmente en UNA sesión, sin DDL concurrente y con respaldo aprobado.
-- Requiere visibilidad de metadatos y permisos ALTER/INDEX/REFERENCES apropiados.
-- No crea rutinas ni cuentas; no deshabilita validación de claves foráneas.
-- El ALTER hace commit implícito y puede bloquear/reconstruir internamente la tabla.

SELECT VERSION() AS version_servidor, DATABASE() AS base_sesion;
SET @pm_u_tabla_ok = (
  SELECT COUNT(*) FROM information_schema.TABLES
  WHERE TABLE_SCHEMA='puntomarket_ventas_demo_local' AND TABLE_NAME='USUARIO' AND ENGINE='InnoDB'
);
SET @pm_u_id_ok = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA='puntomarket_ventas_demo_local' AND TABLE_NAME='USUARIO'
    AND COLUMN_NAME='id' AND DATA_TYPE='int' AND COLUMN_TYPE NOT LIKE '%unsigned%' AND COLUMN_KEY='PRI'
);
SET @pm_u_creador_ok = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA='puntomarket_ventas_demo_local' AND TABLE_NAME='USUARIO' AND COLUMN_NAME='idUsuarioCreacion'
);
SET @pm_u_columna_existe = (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA='puntomarket_ventas_demo_local' AND TABLE_NAME='USUARIO' AND COLUMN_NAME='idUsuarioActualizacion'
);
SET @pm_u_indice_ocupado = (
  SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA='puntomarket_ventas_demo_local' AND TABLE_NAME='USUARIO' AND INDEX_NAME='FK_Usuario_Actualizador'
);
SET @pm_u_fk_ocupada = (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA='puntomarket_ventas_demo_local' AND CONSTRAINT_NAME='FK_Usuario_Actualizador'
);
-- Si existe la columna, no altera ni supone correctos su tipo, índice o FK:
-- revisar las consultas de verificación al final. Si hay conflicto, no aplica DDL.
SET @pm_u_sql = CASE
  WHEN @pm_u_columna_existe > 0 THEN
    'SELECT ''OMITIDA: la columna ya existe; comprobar tipo, indice y FK en la verificacion final'' AS resultado'
  WHEN @pm_u_tabla_ok <> 1 OR @pm_u_id_ok <> 1 OR @pm_u_creador_ok <> 1 THEN
    'SELECT ''BLOQUEADA: tabla, motor o columnas base incompatibles/no visibles; no se aplico ALTER'' AS resultado'
  WHEN @pm_u_indice_ocupado > 0 OR @pm_u_fk_ocupada > 0 THEN
    'SELECT ''BLOQUEADA: nombre de indice o restriccion ocupado; no se aplico ALTER'' AS resultado'
  ELSE
    'ALTER TABLE `puntomarket_ventas_demo_local`.`USUARIO`
       ADD COLUMN `idUsuarioActualizacion` INT DEFAULT NULL AFTER `idUsuarioCreacion`,
       ADD KEY `FK_Usuario_Actualizador` (`idUsuarioActualizacion`),
       ADD CONSTRAINT `FK_Usuario_Actualizador` FOREIGN KEY (`idUsuarioActualizacion`)
         REFERENCES `puntomarket_ventas_demo_local`.`USUARIO` (`id`)
         ON DELETE RESTRICT ON UPDATE CASCADE'
END;
PREPARE pm_u_migracion FROM @pm_u_sql;
EXECUTE pm_u_migracion;
DEALLOCATE PREPARE pm_u_migracion;

-- VERIFICACION (también puede ejecutarse independientemente, solo lectura).
-- Esperado: INT signed, nullable YES, default NULL, después de idUsuarioCreacion.
SELECT COLUMN_NAME,COLUMN_TYPE,IS_NULLABLE,COLUMN_DEFAULT,ORDINAL_POSITION
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA='puntomarket_ventas_demo_local' AND TABLE_NAME='USUARIO'
  AND COLUMN_NAME IN ('idUsuarioCreacion','idUsuarioActualizacion')
ORDER BY ORDINAL_POSITION;
-- Esperado: índice no único, una columna idUsuarioActualizacion, posición 1.
SELECT INDEX_NAME,NON_UNIQUE,SEQ_IN_INDEX,COLUMN_NAME
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA='puntomarket_ventas_demo_local' AND TABLE_NAME='USUARIO'
  AND INDEX_NAME='FK_Usuario_Actualizador';
-- Esperado: USUARIO.id, DELETE RESTRICT, UPDATE CASCADE.
SELECT k.CONSTRAINT_NAME,k.COLUMN_NAME,k.REFERENCED_TABLE_NAME,k.REFERENCED_COLUMN_NAME,
       r.DELETE_RULE,r.UPDATE_RULE
FROM information_schema.KEY_COLUMN_USAGE k
JOIN information_schema.REFERENTIAL_CONSTRAINTS r
  ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME AND r.TABLE_NAME=k.TABLE_NAME
WHERE k.CONSTRAINT_SCHEMA='puntomarket_ventas_demo_local' AND k.TABLE_NAME='USUARIO'
  AND k.CONSTRAINT_NAME='FK_Usuario_Actualizador';
-- No actualiza registros; los valores existentes de la columna nueva quedan NULL.
-- No borrar la columna para repetir: una segunda ejecución omite el ALTER.
