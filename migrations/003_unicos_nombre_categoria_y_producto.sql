-- Migración 003: evitar registros duplicados.
--   * CATEGORIAS: el nombre es único entre las categorías activas.
--   * PRODUCTO:   la combinación nombre + empaque es única entre los productos activos.
-- La comparación ignora mayúsculas y tildes porque las columnas usan la collation
-- utf8mb4_unicode_ci. Los espacios extra se eliminan en el paso 2 y la aplicación
-- los normaliza al guardar.
-- Los registros dados de baja (estaActivo = 0) no cuentan, así que se puede volver a
-- crear una categoría o producto que se eliminó antes.
-- Requiere haber aplicado antes la migración 002 (PRODUCTO.idEmpaque).
USE `bdMicroMercadoArqui`;

-- 1) Detectar duplicados existentes (comparando sin espacios extra). Ambas consultas
--    deben devolver 0 filas; si devuelven filas, limpiar ANTES de seguir (el ALTER del
--    paso 3 fallaría con error 1062).
SELECT 'categoria duplicada' AS tipo,
       TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' ')) AS nombre,
       COUNT(*) AS repeticiones,
       GROUP_CONCAT(`id` ORDER BY `id`) AS ids
FROM `CATEGORIAS`
WHERE `estaActivo` = 1
GROUP BY TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' '))
HAVING COUNT(*) > 1;

SELECT 'producto duplicado' AS tipo,
       TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' ')) AS nombre,
       `idEmpaque`,
       COUNT(*) AS repeticiones,
       GROUP_CONCAT(`id` ORDER BY `id`) AS ids
FROM `PRODUCTO`
WHERE `estaActivo` = 1
GROUP BY TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' ')), `idEmpaque`
HAVING COUNT(*) > 1;

-- Cómo limpiar si aparecen duplicados (elegir UNA opción por cada grupo, usando los ids
-- listados arriba y dejando activo solo el registro que se quiere conservar):
--   a) Dar de baja los sobrantes (conserva el historial y las llaves foráneas):
--        UPDATE `CATEGORIAS` SET `estaActivo` = 0 WHERE `id` IN (/* ids sobrantes */);
--        UPDATE `PRODUCTO`   SET `estaActivo` = 0 WHERE `id` IN (/* ids sobrantes */);
--      Antes de dar de baja una categoría, mover sus productos a la que se conserva:
--        UPDATE `PRODUCTO` SET `idCategoria` = /* id que se conserva */ WHERE `idCategoria` IN (/* ids sobrantes */);
--   b) Renombrar el sobrante para que deje de coincidir:
--        UPDATE `PRODUCTO` SET `nombre` = 'Nombre distinto' WHERE `id` = /* id */;

-- 2) Quitar espacios sobrantes (inicio, fin y repetidos) para que la comparación sea exacta.
UPDATE `CATEGORIAS`
SET `nombre` = TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' '))
WHERE `nombre` <> TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' '));

UPDATE `PRODUCTO`
SET `nombre` = TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' '))
WHERE `nombre` <> TRIM(REGEXP_REPLACE(`nombre`, '[[:space:]]+', ' '));

-- 3) Índices UNIQUE sobre columnas generadas que valen NULL cuando el registro está dado de
--    baja (MySQL permite varios NULL en un índice UNIQUE).
ALTER TABLE `CATEGORIAS`
  ADD COLUMN `nombreActivo` VARCHAR(150)
      GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nombre`, NULL)) VIRTUAL,
  ADD CONSTRAINT `UQ_Categorias_nombre_activo` UNIQUE (`nombreActivo`);

ALTER TABLE `PRODUCTO`
  ADD COLUMN `nombreActivo` VARCHAR(150)
      GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nombre`, NULL)) VIRTUAL,
  ADD COLUMN `idEmpaqueActivo` INT
      GENERATED ALWAYS AS (IF(`estaActivo` = 1, `idEmpaque`, NULL)) VIRTUAL,
  ADD CONSTRAINT `UQ_Producto_nombre_empaque_activo` UNIQUE (`nombreActivo`, `idEmpaqueActivo`);
