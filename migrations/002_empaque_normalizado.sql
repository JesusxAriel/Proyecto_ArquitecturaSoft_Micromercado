-- Migración 002: normalizar el empaque a la tabla EMPAQUE y relacionarla con PRODUCTO.
-- Aplicar sobre una BD existente (bdMicroMercadoArqui) que todavía tiene
-- PRODUCTO.empaquePresentacion como texto. Conserva los datos existentes.
-- MySQL confirma implícitamente cada DDL: ejecutar el script completo, en orden.
USE `bdMicroMercadoArqui`;

-- 1) Tabla de empaques.
CREATE TABLE IF NOT EXISTS `EMPAQUE` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(100) NOT NULL,
  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Empaque_nombre` (`nombre`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2) Cargar los valores distintos que hoy existen en PRODUCTO (sin distinguir mayúsculas).
INSERT INTO `EMPAQUE` (`nombre`, `idUsuarioAdmin`)
SELECT DISTINCT TRIM(p.`empaquePresentacion`), 1
FROM `PRODUCTO` p
WHERE TRIM(p.`empaquePresentacion`) <> ''
  AND TRIM(p.`empaquePresentacion`) NOT IN (SELECT `nombre` FROM `EMPAQUE`);

-- 3) Nueva columna (nullable mientras se llena) y relleno desde el texto actual.
ALTER TABLE `PRODUCTO` ADD COLUMN `idEmpaque` INT NULL AFTER `nombre`;

UPDATE `PRODUCTO` p
JOIN `EMPAQUE` e ON e.`nombre` = TRIM(p.`empaquePresentacion`)
SET p.`idEmpaque` = e.`id`;

-- 4) Verificación: debe devolver 0. Si no, NO continuar (hay productos con empaque vacío).
SELECT COUNT(*) AS productos_sin_empaque FROM `PRODUCTO` WHERE `idEmpaque` IS NULL;

-- 5) Restricciones: NOT NULL + llave foránea.
ALTER TABLE `PRODUCTO`
  MODIFY COLUMN `idEmpaque` INT NOT NULL,
  ADD KEY `FK_Producto_Empaque` (`idEmpaque`),
  ADD CONSTRAINT `FK_Producto_Empaque` FOREIGN KEY (`idEmpaque`)
      REFERENCES `EMPAQUE` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- 6) La vista pasa a leer el nombre desde EMPAQUE (se conserva el alias empaquePresentacion).
--    Debe redefinirse ANTES de eliminar la columna antigua.
CREATE OR REPLACE VIEW `vw_productos_con_stock` AS
SELECT
    p.id,
    p.nombre,
    p.idEmpaque,
    e.nombre AS empaquePresentacion,
    p.precioVenta,
    p.precioCosto,
    p.stockMinimo,
    p.idCategoria,
    c.nombre AS nombreCategoria,
    p.idProveedor,
    pr.nombreEmpresa AS nombreProveedor,
    p.estaActivo,
    COALESCE(SUM(l.cantidadDisponible), 0) AS stockCalculado
FROM `PRODUCTO` p
INNER JOIN `EMPAQUE` e ON p.idEmpaque = e.id
INNER JOIN `CATEGORIAS` c ON p.idCategoria = c.id
INNER JOIN `PROVEEDOR` pr ON p.idProveedor = pr.id
LEFT JOIN `LOTE` l ON p.id = l.idProducto AND l.estaActivo = 1
WHERE p.estaActivo = 1
GROUP BY p.id;

-- 7) Eliminar la columna de texto antigua.
ALTER TABLE `PRODUCTO` DROP COLUMN `empaquePresentacion`;
