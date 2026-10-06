-- Migración 004: el nombre de empresa de un proveedor es único entre los proveedores activos.
-- La comparación ignora mayúsculas y tildes (collation utf8mb4_unicode_ci); los espacios extra
-- se eliminan en el paso 2 y la aplicación los normaliza al guardar.
-- Los proveedores dados de baja (estaActivo = 0) no cuentan.
USE `bdMicroMercadoArqui`;

-- 1) Detectar duplicados existentes. Debe devolver 0 filas; si devuelve filas, limpiar ANTES
--    de seguir (el ALTER del paso 3 fallaría con error 1062).
SELECT TRIM(REGEXP_REPLACE(`nombreEmpresa`, '[[:space:]]+', ' ')) AS nombreEmpresa,
       COUNT(*) AS repeticiones,
       GROUP_CONCAT(`id` ORDER BY `id`) AS ids
FROM `PROVEEDOR`
WHERE `estaActivo` = 1
GROUP BY TRIM(REGEXP_REPLACE(`nombreEmpresa`, '[[:space:]]+', ' '))
HAVING COUNT(*) > 1;

-- Cómo limpiar si aparecen duplicados (dejar activo solo el que se conserva):
--   a) Mover los productos al proveedor que se conserva y dar de baja los sobrantes:
--        UPDATE `PRODUCTO`  SET `idProveedor` = /* id que se conserva */ WHERE `idProveedor` IN (/* ids sobrantes */);
--        UPDATE `PROVEEDOR` SET `estaActivo` = 0 WHERE `id` IN (/* ids sobrantes */);
--   b) O renombrar el sobrante:
--        UPDATE `PROVEEDOR` SET `nombreEmpresa` = 'Nombre distinto' WHERE `id` = /* id */;

-- 2) Quitar espacios sobrantes (inicio, fin y repetidos).
UPDATE `PROVEEDOR`
SET `nombreEmpresa` = TRIM(REGEXP_REPLACE(`nombreEmpresa`, '[[:space:]]+', ' '))
WHERE `nombreEmpresa` <> TRIM(REGEXP_REPLACE(`nombreEmpresa`, '[[:space:]]+', ' '));

-- 3) Índice UNIQUE sobre una columna generada que vale NULL cuando el proveedor está dado de baja.
ALTER TABLE `PROVEEDOR`
  ADD COLUMN `nombreActivo` VARCHAR(150)
      GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nombreEmpresa`, NULL)) VIRTUAL,
  ADD CONSTRAINT `UQ_Proveedor_nombre_activo` UNIQUE (`nombreActivo`);
