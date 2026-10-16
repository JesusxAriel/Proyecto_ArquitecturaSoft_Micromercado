DROP DATABASE IF EXISTS `bdMicroMercadoArqui`;
CREATE DATABASE `bdMicroMercadoArqui`
  DEFAULT CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE `bdMicroMercadoArqui`;

CREATE TABLE `ROL` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(50) NOT NULL,
  `descripcion` VARCHAR(255) DEFAULT NULL,
  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Rol_nombre` (`nombre`),
  CONSTRAINT `CK_Rol_estaActivo` CHECK (`estaActivo` IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `USUARIO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `username` VARCHAR(50) NOT NULL,
  `passwordHash` VARCHAR(255) NOT NULL,
  `nombreCompleto` VARCHAR(150) NOT NULL,
  `correo` VARCHAR(150) DEFAULT NULL,
  `idRol` INT NOT NULL,
  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Usuario_username` (`username`),
  KEY `FK_Usuario_Rol` (`idRol`),
  CONSTRAINT `CK_Usuario_estaActivo` CHECK (`estaActivo` IN (0,1)),
  CONSTRAINT `FK_Usuario_Rol` FOREIGN KEY (`idRol`)
    REFERENCES `ROL` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CATEGORIAS` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(255) DEFAULT NULL,
  `codigo` VARCHAR(20) NOT NULL,
  `pasilloUbicacion` VARCHAR(20) DEFAULT NULL,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  `nombreActivo` VARCHAR(150)
    GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nombre`, NULL)) VIRTUAL,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Categorias_codigo` (`codigo`),
  UNIQUE KEY `UQ_Categorias_nombre_activo` (`nombreActivo`),
  KEY `FK_Categorias_UsuarioAdmin` (`idUsuarioAdmin`),
  KEY `FK_Categorias_UsuarioActualizacion` (`idUsuarioActualizacion`),
  CONSTRAINT `CK_Categorias_estaActivo` CHECK (`estaActivo` IN (0,1)),
  CONSTRAINT `FK_Categorias_UsuarioAdmin` FOREIGN KEY (`idUsuarioAdmin`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Categorias_UsuarioActualizacion` FOREIGN KEY (`idUsuarioActualizacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `PROVEEDOR` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombreEmpresa` VARCHAR(150) NOT NULL,
  `numeroEmpresa` VARCHAR(20) NOT NULL,
  `correoReferencia` VARCHAR(150) DEFAULT NULL,
  `esAutogestionado` TINYINT(1) NOT NULL DEFAULT 0,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  `nombreActivo` VARCHAR(150)
    GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nombreEmpresa`, NULL)) VIRTUAL,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Proveedor_nombre_activo` (`nombreActivo`),
  KEY `FK_Proveedor_UsuarioAdmin` (`idUsuarioAdmin`),
  KEY `FK_Proveedor_UsuarioActualizacion` (`idUsuarioActualizacion`),
  CONSTRAINT `CK_Proveedor_estaActivo` CHECK (`estaActivo` IN (0,1)),
  CONSTRAINT `CK_Proveedor_autogestionado` CHECK (`esAutogestionado` IN (0,1)),
  CONSTRAINT `FK_Proveedor_UsuarioAdmin` FOREIGN KEY (`idUsuarioAdmin`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Proveedor_UsuarioActualizacion` FOREIGN KEY (`idUsuarioActualizacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `EMPAQUE` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(100) NOT NULL,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Empaque_nombre` (`nombre`),
  KEY `FK_Empaque_UsuarioAdmin` (`idUsuarioAdmin`),
  KEY `FK_Empaque_UsuarioActualizacion` (`idUsuarioActualizacion`),
  CONSTRAINT `CK_Empaque_estaActivo` CHECK (`estaActivo` IN (0,1)),
  CONSTRAINT `FK_Empaque_UsuarioAdmin` FOREIGN KEY (`idUsuarioAdmin`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Empaque_UsuarioActualizacion` FOREIGN KEY (`idUsuarioActualizacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `PRODUCTO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `idEmpaque` INT NOT NULL,
  `precioVenta` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `precioCosto` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `stockMinimo` INT NOT NULL DEFAULT 10,
  `stockActual` INT NOT NULL DEFAULT 0,

  `idCategoria` INT NOT NULL,
  `idProveedor` INT NOT NULL,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  `nombreActivo` VARCHAR(150)
    GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nombre`, NULL)) VIRTUAL,
  `idEmpaqueActivo` INT
    GENERATED ALWAYS AS (IF(`estaActivo` = 1, `idEmpaque`, NULL)) VIRTUAL,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Producto_nombre_empaque_activo` (`nombreActivo`, `idEmpaqueActivo`),
  KEY `FK_Producto_Empaque` (`idEmpaque`),
  KEY `FK_Producto_Categoria` (`idCategoria`),
  KEY `FK_Producto_Proveedor` (`idProveedor`),
  KEY `FK_Producto_UsuarioAdmin` (`idUsuarioAdmin`),
  KEY `FK_Producto_UsuarioActualizacion` (`idUsuarioActualizacion`),
  CONSTRAINT `CK_Producto_estaActivo` CHECK (`estaActivo` IN (0,1)),
  CONSTRAINT `CK_Producto_precios` CHECK (`precioVenta` >= 0 AND `precioCosto` >= 0),
  CONSTRAINT `CK_Producto_stockMinimo` CHECK (`stockMinimo` >= 0),
  CONSTRAINT `CK_Producto_stockActual` CHECK (`stockActual` >= 0),
  CONSTRAINT `FK_Producto_Empaque` FOREIGN KEY (`idEmpaque`)
    REFERENCES `EMPAQUE` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Producto_Categoria` FOREIGN KEY (`idCategoria`)
    REFERENCES `CATEGORIAS` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Producto_Proveedor` FOREIGN KEY (`idProveedor`)
    REFERENCES `PROVEEDOR` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Producto_UsuarioAdmin` FOREIGN KEY (`idUsuarioAdmin`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Producto_UsuarioActualizacion` FOREIGN KEY (`idUsuarioActualizacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `LOTE` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `idProducto` INT NOT NULL,
  `codigoLote` VARCHAR(50) NOT NULL,
  `cantidadInicial` INT NOT NULL,
  `cantidadDisponible` INT NOT NULL,
  `fechaVencimiento` DATE DEFAULT NULL,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Lote_producto_codigo` (`idProducto`, `codigoLote`),
  UNIQUE KEY `UQ_Lote_id_producto` (`id`, `idProducto`),
  KEY `IX_Lote_producto_vencimiento` (`idProducto`, `fechaVencimiento`),
  KEY `FK_Lote_UsuarioAdmin` (`idUsuarioAdmin`),
  KEY `FK_Lote_UsuarioActualizacion` (`idUsuarioActualizacion`),
  CONSTRAINT `CK_Lote_estaActivo` CHECK (`estaActivo` IN (0,1)),
  CONSTRAINT `CK_Lote_cantidades` CHECK (
    `cantidadInicial` > 0
    AND `cantidadDisponible` >= 0
    AND `cantidadDisponible` <= `cantidadInicial`
  ),
  CONSTRAINT `FK_Lote_Producto` FOREIGN KEY (`idProducto`)
    REFERENCES `PRODUCTO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Lote_UsuarioAdmin` FOREIGN KEY (`idUsuarioAdmin`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Lote_UsuarioActualizacion` FOREIGN KEY (`idUsuarioActualizacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `HISTORIAL_PRECIO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `idProducto` INT NOT NULL,
  `precioVentaAnterior` DECIMAL(10,2) NOT NULL,
  `precioVentaNuevo` DECIMAL(10,2) NOT NULL,
  `precioCostoAnterior` DECIMAL(10,2) DEFAULT NULL,
  `precioCostoNuevo` DECIMAL(10,2) DEFAULT NULL,
  `motivoCambio` VARCHAR(255) DEFAULT 'Ajuste de precio',
  `idUsuario` INT NOT NULL,
  `fechaCambio` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  KEY `FK_HistorialPrecio_Producto` (`idProducto`),
  KEY `FK_HistorialPrecio_Usuario` (`idUsuario`),
  CONSTRAINT `FK_HistorialPrecio_Producto` FOREIGN KEY (`idProducto`)
    REFERENCES `PRODUCTO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_HistorialPrecio_Usuario` FOREIGN KEY (`idUsuario`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `CLIENTE` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nitCi` VARCHAR(30) NOT NULL,
  `razonSocial` VARCHAR(150) NOT NULL,
  `correo` VARCHAR(150) DEFAULT NULL,
  `telefono` VARCHAR(30) DEFAULT NULL,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioCreacion` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  `nitCiActivo` VARCHAR(30)
    GENERATED ALWAYS AS (IF(`estaActivo` = 1, `nitCi`, NULL)) VIRTUAL,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Cliente_nitCi_activo` (`nitCiActivo`),
  KEY `FK_Cliente_UsuarioCreacion` (`idUsuarioCreacion`),
  KEY `FK_Cliente_UsuarioActualizacion` (`idUsuarioActualizacion`),
  CONSTRAINT `CK_Cliente_estaActivo` CHECK (`estaActivo` IN (0,1)),
  CONSTRAINT `FK_Cliente_UsuarioCreacion` FOREIGN KEY (`idUsuarioCreacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Cliente_UsuarioActualizacion` FOREIGN KEY (`idUsuarioActualizacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `VENTA` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nroComprobante` VARCHAR(50) NOT NULL,
  `idCliente` INT NOT NULL,
  `idUsuario` INT NOT NULL,
  `fechaVenta` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `montoTotal` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `efectivoPagado` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `cambio` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `estado` ENUM('COMPLETADA','ANULADA') NOT NULL DEFAULT 'COMPLETADA',

  `motivoAnulacion` VARCHAR(255) DEFAULT NULL,
  `idUsuarioAnulacion` INT DEFAULT NULL,
  `fechaAnulacion` DATETIME DEFAULT NULL,

  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Venta_nroComprobante` (`nroComprobante`),
  KEY `IX_Venta_fechaVenta` (`fechaVenta`),
  KEY `IX_Venta_usuario_fecha` (`idUsuario`, `fechaVenta`),
  KEY `FK_Venta_Cliente` (`idCliente`),
  KEY `FK_Venta_UsuarioAnulacion` (`idUsuarioAnulacion`),
  CONSTRAINT `CK_Venta_montos` CHECK (
    `montoTotal` >= 0 AND `efectivoPagado` >= 0 AND `cambio` >= 0
    AND `efectivoPagado` >= `montoTotal`
    AND `cambio` = `efectivoPagado` - `montoTotal`
  ),
  CONSTRAINT `CK_Venta_anulacion` CHECK (
    (`estado` = 'COMPLETADA' AND `motivoAnulacion` IS NULL AND `fechaAnulacion` IS NULL)
    OR
    (`estado` = 'ANULADA' AND `motivoAnulacion` IS NOT NULL AND `fechaAnulacion` IS NOT NULL)
  ),
  CONSTRAINT `FK_Venta_Cliente` FOREIGN KEY (`idCliente`)
    REFERENCES `CLIENTE` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Venta_Usuario` FOREIGN KEY (`idUsuario`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Venta_UsuarioAnulacion` FOREIGN KEY (`idUsuarioAnulacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE `DETALLE_VENTA` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `idVenta` INT NOT NULL,
  `idProducto` INT NOT NULL,
  `idLote` INT NOT NULL,
  `cantidad` INT NOT NULL,
  `precioUnitario` DECIMAL(10,2) NOT NULL,
  `subtotal` DECIMAL(10,2)
    GENERATED ALWAYS AS (`cantidad` * `precioUnitario`) STORED,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  KEY `FK_DetalleVenta_Venta` (`idVenta`),
  KEY `FK_DetalleVenta_Producto` (`idProducto`),
  KEY `FK_DetalleVenta_LoteProducto` (`idLote`, `idProducto`),
  CONSTRAINT `CK_DetalleVenta_valores` CHECK (
    `cantidad` > 0 AND `precioUnitario` >= 0
  ),
  CONSTRAINT `FK_DetalleVenta_Venta` FOREIGN KEY (`idVenta`)
    REFERENCES `VENTA` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_DetalleVenta_LoteProducto` FOREIGN KEY (`idLote`, `idProducto`)
    REFERENCES `LOTE` (`id`, `idProducto`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

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
    p.stockActual AS stockCalculado,
    (p.stockActual <= p.stockMinimo) AS bajoStockMinimo
FROM `PRODUCTO` p
INNER JOIN `EMPAQUE` e     ON p.idEmpaque = e.id
INNER JOIN `CATEGORIAS` c   ON p.idCategoria = c.id
INNER JOIN `PROVEEDOR` pr  ON p.idProveedor = pr.id
WHERE p.estaActivo = 1;

CREATE OR REPLACE VIEW `vw_resumen_ventas` AS
SELECT
    v.id AS idVenta,
    v.nroComprobante,
    v.fechaVenta,
    c.nitCi AS nitCiCliente,
    c.razonSocial AS clienteRazonSocial,
    u.nombreCompleto AS registradoPor,
    v.montoTotal,
    v.estado,
    v.motivoAnulacion,
    v.fechaAnulacion
FROM `VENTA` v
INNER JOIN `CLIENTE` c ON v.idCliente = c.id
INNER JOIN `USUARIO` u ON v.idUsuario = u.id;

CREATE OR REPLACE VIEW `vw_comprobante_venta` AS
SELECT
    v.id AS idVenta,
    v.nroComprobante,
    v.fechaVenta,
    c.nitCi AS nitCiCliente,
    c.razonSocial AS clienteRazonSocial,
    u.username AS usuarioCajero,
    u.nombreCompleto AS nombreCajero,
    dv.cantidad,
    p.nombre AS descripcionProducto,
    e.nombre AS presentacionEmpaque,
    l.codigoLote,
    dv.precioUnitario,
    dv.subtotal,
    v.montoTotal,
    v.efectivoPagado,
    v.cambio,
    v.estado
FROM `VENTA` v
INNER JOIN `CLIENTE` c        ON v.idCliente = c.id
INNER JOIN `USUARIO` u        ON v.idUsuario = u.id
INNER JOIN `DETALLE_VENTA` dv ON v.id = dv.idVenta
INNER JOIN `PRODUCTO` p       ON dv.idProducto = p.id
INNER JOIN `EMPAQUE` e        ON p.idEmpaque = e.id
INNER JOIN `LOTE` l           ON dv.idLote = l.id;

CREATE OR REPLACE VIEW `vw_lotes_por_vencer` AS
SELECT
    l.id AS idLote,
    p.nombre AS producto,
    l.codigoLote,
    l.cantidadDisponible,
    l.fechaVencimiento,
    DATEDIFF(l.fechaVencimiento, CURRENT_DATE()) AS diasParaVencer
FROM `LOTE` l
INNER JOIN `PRODUCTO` p ON l.idProducto = p.id
WHERE l.estaActivo = 1
  AND l.cantidadDisponible > 0
  AND l.fechaVencimiento IS NOT NULL
  AND l.fechaVencimiento <= DATE_ADD(CURRENT_DATE(), INTERVAL 30 DAY)
ORDER BY l.fechaVencimiento;

DELIMITER $$

CREATE TRIGGER `trg_rol_bu`
BEFORE UPDATE ON `ROL`
FOR EACH ROW
BEGIN
    IF OLD.nombre IN ('Administrador', 'Cajero')
       AND (NEW.nombre <> OLD.nombre OR NEW.estaActivo = 0) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Los roles base (Administrador, Cajero) no se pueden renombrar ni desactivar';
    END IF;

    IF OLD.estaActivo = 1 AND NEW.estaActivo = 0 THEN
        IF EXISTS (SELECT 1 FROM `USUARIO` WHERE idRol = OLD.id AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'No se puede desactivar el rol: tiene usuarios activos asociados';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_usuario_bi`
BEFORE INSERT ON `USUARIO`
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM `ROL` WHERE id = NEW.idRol AND estaActivo = 1) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El rol no existe o esta inactivo';
    END IF;
END$$

CREATE TRIGGER `trg_usuario_bu`
BEFORE UPDATE ON `USUARIO`
FOR EACH ROW
BEGIN
    DECLARE v_idAdmin INT DEFAULT NULL;

    IF NEW.estaActivo = 1 AND (OLD.estaActivo = 0 OR NEW.idRol <> OLD.idRol) THEN
        IF NOT EXISTS (SELECT 1 FROM `ROL` WHERE id = NEW.idRol AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El rol no existe o esta inactivo';
        END IF;
    END IF;

    SELECT id INTO v_idAdmin FROM `ROL` WHERE nombre = 'Administrador' LIMIT 1;

    IF OLD.estaActivo = 1 AND OLD.idRol = v_idAdmin
       AND (NEW.estaActivo = 0 OR NEW.idRol <> OLD.idRol) THEN
        IF NOT EXISTS (SELECT 1 FROM `USUARIO`
                       WHERE idRol = v_idAdmin AND estaActivo = 1 AND id <> OLD.id) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Debe existir al menos un Administrador activo en el sistema';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_categorias_bu`
BEFORE UPDATE ON `CATEGORIAS`
FOR EACH ROW
BEGIN
    IF OLD.estaActivo = 1 AND NEW.estaActivo = 0 THEN
        IF EXISTS (SELECT 1 FROM `PRODUCTO` WHERE idCategoria = OLD.id AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'No se puede desactivar la categoria: tiene productos activos asociados';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_proveedor_bu`
BEFORE UPDATE ON `PROVEEDOR`
FOR EACH ROW
BEGIN
    IF OLD.estaActivo = 1 AND NEW.estaActivo = 0 THEN
        IF EXISTS (SELECT 1 FROM `PRODUCTO` WHERE idProveedor = OLD.id AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'No se puede desactivar el proveedor: tiene productos activos asociados';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_empaque_bu`
BEFORE UPDATE ON `EMPAQUE`
FOR EACH ROW
BEGIN
    IF OLD.estaActivo = 1 AND NEW.estaActivo = 0 THEN
        IF EXISTS (SELECT 1 FROM `PRODUCTO` WHERE idEmpaque = OLD.id AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'No se puede desactivar el empaque: tiene productos activos asociados';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_producto_bi`
BEFORE INSERT ON `PRODUCTO`
FOR EACH ROW
BEGIN
    IF NEW.estaActivo = 1 THEN
        IF NOT EXISTS (SELECT 1 FROM `CATEGORIAS` WHERE id = NEW.idCategoria AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'La categoria no existe o esta inactiva';
        END IF;
        IF NOT EXISTS (SELECT 1 FROM `PROVEEDOR` WHERE id = NEW.idProveedor AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El proveedor no existe o esta inactivo';
        END IF;
        IF NOT EXISTS (SELECT 1 FROM `EMPAQUE` WHERE id = NEW.idEmpaque AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El empaque no existe o esta inactivo';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_producto_bu`
BEFORE UPDATE ON `PRODUCTO`
FOR EACH ROW
BEGIN
    IF NEW.estaActivo = 1
       AND (OLD.estaActivo = 0
            OR NEW.idCategoria <> OLD.idCategoria
            OR NEW.idProveedor <> OLD.idProveedor
            OR NEW.idEmpaque   <> OLD.idEmpaque) THEN
        IF NOT EXISTS (SELECT 1 FROM `CATEGORIAS` WHERE id = NEW.idCategoria AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'La categoria no existe o esta inactiva';
        END IF;
        IF NOT EXISTS (SELECT 1 FROM `PROVEEDOR` WHERE id = NEW.idProveedor AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El proveedor no existe o esta inactivo';
        END IF;
        IF NOT EXISTS (SELECT 1 FROM `EMPAQUE` WHERE id = NEW.idEmpaque AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El empaque no existe o esta inactivo';
        END IF;
    END IF;

    IF OLD.estaActivo = 1 AND NEW.estaActivo = 0 THEN
        IF EXISTS (SELECT 1 FROM `LOTE`
                   WHERE idProducto = OLD.id AND estaActivo = 1 AND cantidadDisponible > 0) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'No se puede desactivar el producto: aun tiene stock en lotes activos';
        END IF;
    END IF;

    IF (NEW.precioVenta <> OLD.precioVenta OR NEW.precioCosto <> OLD.precioCosto)
       AND NEW.idUsuarioActualizacion IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Para cambiar precios debe indicar idUsuarioActualizacion';
    END IF;
END$$

CREATE TRIGGER `trg_producto_historial_precio`
AFTER UPDATE ON `PRODUCTO`
FOR EACH ROW
BEGIN
    IF OLD.precioVenta <> NEW.precioVenta OR OLD.precioCosto <> NEW.precioCosto THEN
        INSERT INTO `HISTORIAL_PRECIO`
            (`idProducto`, `precioVentaAnterior`, `precioVentaNuevo`,
             `precioCostoAnterior`, `precioCostoNuevo`, `motivoCambio`, `idUsuario`)
        VALUES
            (NEW.id, OLD.precioVenta, NEW.precioVenta,
             OLD.precioCosto, NEW.precioCosto,
             COALESCE(@motivo_cambio_precio, 'Ajuste de precio'),
             NEW.idUsuarioActualizacion);
    END IF;
END$$

CREATE TRIGGER `trg_historial_precio_bu`
BEFORE UPDATE ON `HISTORIAL_PRECIO`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El historial de precios es de auditoria y no se puede modificar';
END$$

CREATE TRIGGER `trg_historial_precio_bd`
BEFORE DELETE ON `HISTORIAL_PRECIO`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El historial de precios es de auditoria y no se puede eliminar';
END$$

CREATE TRIGGER `trg_lote_bi`
BEFORE INSERT ON `LOTE`
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM `PRODUCTO` WHERE id = NEW.idProducto AND estaActivo = 1) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'No se puede crear un lote para un producto inexistente o inactivo';
    END IF;

    IF NEW.fechaVencimiento IS NOT NULL AND NEW.fechaVencimiento < CURDATE() THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'No se puede registrar un lote que ya esta vencido';
    END IF;
END$$

CREATE TRIGGER `trg_lote_bu`
BEFORE UPDATE ON `LOTE`
FOR EACH ROW
BEGIN
    IF NEW.idProducto <> OLD.idProducto THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Un lote no puede cambiarse de producto';
    END IF;

    IF OLD.estaActivo = 0 AND NEW.estaActivo = 1 THEN
        IF NOT EXISTS (SELECT 1 FROM `PRODUCTO` WHERE id = NEW.idProducto AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'No se puede reactivar un lote de un producto inactivo';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_lote_ai`
AFTER INSERT ON `LOTE`
FOR EACH ROW
BEGIN
    IF NEW.estaActivo = 1 AND NEW.cantidadDisponible <> 0 THEN
        UPDATE `PRODUCTO`
        SET `stockActual` = `stockActual` + NEW.cantidadDisponible,
            `fechaActualizacion` = `fechaActualizacion`
        WHERE `id` = NEW.idProducto;
    END IF;
END$$

CREATE TRIGGER `trg_lote_au`
AFTER UPDATE ON `LOTE`
FOR EACH ROW
BEGIN
    DECLARE v_delta INT;
    SET v_delta = IF(NEW.estaActivo = 1, NEW.cantidadDisponible, 0)
                - IF(OLD.estaActivo = 1, OLD.cantidadDisponible, 0);
    IF v_delta <> 0 THEN
        UPDATE `PRODUCTO`
        SET `stockActual` = `stockActual` + v_delta,
            `fechaActualizacion` = `fechaActualizacion`
        WHERE `id` = NEW.idProducto;
    END IF;
END$$

CREATE TRIGGER `trg_lote_ad`
AFTER DELETE ON `LOTE`
FOR EACH ROW
BEGIN
    IF OLD.estaActivo = 1 AND OLD.cantidadDisponible <> 0 THEN
        UPDATE `PRODUCTO`
        SET `stockActual` = `stockActual` - OLD.cantidadDisponible,
            `fechaActualizacion` = `fechaActualizacion`
        WHERE `id` = OLD.idProducto;
    END IF;
END$$

CREATE TRIGGER `trg_cliente_bu`
BEFORE UPDATE ON `CLIENTE`
FOR EACH ROW
BEGIN
    IF OLD.id = 1 AND (NEW.estaActivo <> 1 OR NEW.nitCi <> OLD.nitCi) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cliente generico (id 1) no se puede desactivar ni cambiar de NIT/CI';
    END IF;
END$$

CREATE TRIGGER `trg_venta_bi`
BEFORE INSERT ON `VENTA`
FOR EACH ROW
BEGIN
    IF NEW.estado <> 'COMPLETADA' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Una venta nueva solo puede registrarse como COMPLETADA';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM `CLIENTE` WHERE id = NEW.idCliente AND estaActivo = 1) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cliente no existe o esta inactivo';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM `USUARIO` WHERE id = NEW.idUsuario AND estaActivo = 1) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El usuario no existe o esta inactivo';
    END IF;
END$$

CREATE TRIGGER `trg_venta_bu`
BEFORE UPDATE ON `VENTA`
FOR EACH ROW
BEGIN
    IF OLD.estado = 'ANULADA' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Una venta anulada no se puede modificar';
    END IF;

    IF OLD.nroComprobante NOT LIKE 'TMP-%'
       AND (NEW.nroComprobante <> OLD.nroComprobante
            OR NEW.idCliente   <> OLD.idCliente
            OR NEW.idUsuario <> OLD.idUsuario
            OR NEW.fechaVenta  <> OLD.fechaVenta
            OR NEW.montoTotal  <> OLD.montoTotal
            OR NEW.efectivoPagado <> OLD.efectivoPagado
            OR NEW.cambio      <> OLD.cambio) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Una venta cerrada no se puede editar; solo se puede anular';
    END IF;

    IF NEW.estado = 'ANULADA' THEN
        IF NEW.idUsuarioAnulacion IS NULL
           OR NOT EXISTS (SELECT 1 FROM `USUARIO`
                          WHERE id = NEW.idUsuarioAnulacion AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'La anulacion requiere un usuario activo que la realice';
        END IF;
    END IF;
END$$

CREATE TRIGGER `trg_venta_bd`
BEFORE DELETE ON `VENTA`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Las ventas no se eliminan; se deben anular';
END$$

CREATE TRIGGER `trg_detalle_venta_bi`
BEFORE INSERT ON `DETALLE_VENTA`
FOR EACH ROW
BEGIN
    IF NOT EXISTS (SELECT 1 FROM `VENTA`
                   WHERE id = NEW.idVenta
                     AND estado = 'COMPLETADA'
                     AND nroComprobante LIKE 'TMP-%') THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Solo se puede agregar detalle a una venta que aun esta en proceso';
    END IF;
END$$

CREATE TRIGGER `trg_detalle_venta_bu`
BEFORE UPDATE ON `DETALLE_VENTA`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El detalle de una venta no se puede modificar';
END$$

CREATE TRIGGER `trg_detalle_venta_bd`
BEFORE DELETE ON `DETALLE_VENTA`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El detalle de una venta no se puede eliminar';
END$$

DELIMITER ;

INSERT INTO `ROL` (`id`, `nombre`, `descripcion`) VALUES
(1, 'Administrador', 'Gerente / root: control total del sistema'),
(2, 'Cajero', 'Registro de ventas, anulaciones y atencion al cliente');

INSERT INTO `USUARIO` (`id`, `username`, `passwordHash`, `nombreCompleto`, `correo`, `idRol`) VALUES
(1, 'admin',   '$2a$11$vHYNXh3Q3oaQJ5htY0Z.OuWKDTj8OuOdGsSj3kDPOVYWU59C1tjlG', 'Gerente General (root)', 'gerencia@micromercado.com', 1),
(2, 'cajero1', '$2a$11$5qT4nZN5lpU4t/byugwqv.fe93xMvkjMMXCpvs1oK3cWLElPUZAyG', 'Carlos Mamani Quispe', 'carlos.mamani@micromercado.com', 2);

INSERT INTO `CATEGORIAS` (`id`, `nombre`, `descripcion`, `codigo`, `pasilloUbicacion`, `idUsuarioAdmin`) VALUES
(1, 'Lácteos y Derivados', 'Leches, yogures, quesos y mantequillas', 'CAT-LAC', 'Pasillo 1', 1),
(2, 'Bebidas y Gaseosas', 'Refrescos, jugos, aguas y energizantes', 'CAT-BEB', 'Pasillo 2', 1),
(3, 'Abarrotes', 'Arroz, fideos, aceite, azúcar, sal y condimentos', 'CAT-ABA', 'Pasillo 3', 1),
(4, 'Embutidos y Fiambres', 'Salchichas, jamones, chorizos y carnes frías', 'CAT-EMB', 'Pasillo 4', 1),
(5, 'Snacks y Galletas', 'Papas fritas, pipocas, chocolates y galletas', 'CAT-SNA', 'Pasillo 5', 1),
(6, 'Limpieza del Hogar', 'Detergentes, lavavajillas, desinfectantes y lavandina', 'CAT-LIM', 'Pasillo 6', 1),
(7, 'Cuidado Personal', 'Jabones, champús, pasta dental y desodorantes', 'CAT-CUI', 'Pasillo 7', 1),
(8, 'Panadería y Repostería', 'Pan fresco, kekes, tostadas y harinas', 'CAT-PAN', 'Pasillo 8', 1);

INSERT INTO `PROVEEDOR` (`id`, `nombreEmpresa`, `numeroEmpresa`, `correoReferencia`, `esAutogestionado`, `idUsuarioAdmin`) VALUES
(1, 'PIL Andina S.A.', '44112233', 'ventas.cbba@pilandina.com.bo', 0, 1),
(2, 'EMBOL S.A. (Coca-Cola)', '44556677', 'pedidos.cbba@embol.com.bo', 0, 1),
(3, 'Sofía SHE S.A.', '44889900', 'contacto.cbba@sofia.com.bo', 1, 1),
(4, 'CBN (Cervecería Boliviana Nacional)', '44224466', 'distribucion@cbn.com.bo', 0, 1),
(5, 'Unilever Bolivia', '44335577', 'atencion@unilever.com', 0, 1),
(6, 'Industrias Venado S.A. (Kris)', '44118899', 'ventas@venado.com.bo', 0, 1),
(7, 'Arcor Bolivia', '44778811', 'pedidos@arcor.com.bo', 0, 1);

INSERT INTO `EMPAQUE` (`id`, `nombre`, `idUsuarioAdmin`) VALUES
(1, 'Bolsa', 1),
(2, 'Botella Plástica', 1),
(3, 'Envase Plástico', 1),
(4, 'Botella Vidrio', 1),
(5, 'Paquete', 1),
(6, 'Paquete Sellado', 1),
(7, 'Barra', 1),
(8, 'Tubo', 1),
(9, 'Lata', 1);

INSERT INTO `PRODUCTO` (`id`, `nombre`, `idEmpaque`, `idCategoria`, `precioVenta`, `precioCosto`, `stockMinimo`, `idProveedor`, `idUsuarioAdmin`) VALUES
(1,  'Leche Entera PIL 1L', 1, 1, 6.50, 5.20, 20, 1, 1),
(2,  'Yogurt Frutado PIL 1kg', 2, 1, 12.00, 9.50, 10, 1, 1),
(3,  'Mantequilla con Sal PIL 200g', 3, 1, 14.50, 11.80, 5, 1, 1),
(4,  'Coca-Cola Sabor Original 2L', 2, 2, 11.00, 9.00, 25, 2, 1),
(5,  'Fanta Naranja 2L', 2, 2, 10.50, 8.50, 15, 2, 1),
(6,  'Agua Vital Sin Gas 2L', 2, 2, 6.00, 4.50, 30, 2, 1),
(7,  'Cerveza Paceña 620ml', 4, 2, 12.00, 9.80, 40, 4, 1),
(8,  'Mayonesa Kris 500g', 1, 3, 14.00, 11.20, 12, 6, 1),
(9,  'Ketchup Kris 500g', 1, 3, 12.50, 10.00, 10, 6, 1),
(10, 'Salsa de Tomate Kris 400g', 1, 3, 8.00, 6.20, 15, 6, 1),
(11, 'Chorizo Parrillero Sofía 500g', 5, 4, 28.50, 23.50, 8, 3, 1),
(12, 'Jamón Premium Sofía 200g', 6, 4, 18.00, 14.50, 10, 3, 1),
(13, 'Salchicha de Pollo Sofía 500g', 5, 4, 16.50, 13.00, 10, 3, 1),
(14, 'Galletas Moka Arcor 110g', 5, 5, 4.50, 3.20, 20, 7, 1),
(15, 'Bon o Bon Leche Display x18', 5, 5, 27.00, 21.50, 5, 7, 1),
(16, 'Detergente OMO Multiacción 800g', 1, 6, 15.00, 12.00, 15, 5, 1),
(17, 'Lavavajillas Ola Limón 500ml', 2, 6, 8.50, 6.80, 10, 5, 1),
(18, 'Jabón Lux Suave 125g', 7, 7, 5.50, 4.10, 20, 5, 1),
(19, 'Crema Dental Colgate Triple Acción 90g', 8, 7, 9.00, 7.00, 15, 5, 1),
(20, 'Tostadas Trigo PIL 200g', 1, 8, 7.50, 5.80, 10, 1, 1),
(21, 'Royal Polvo de Hornear 100g', 9, 8, 6.00, 4.50, 15, 6, 1);

INSERT INTO `LOTE` (`id`, `idProducto`, `codigoLote`, `cantidadInicial`, `cantidadDisponible`, `fechaVencimiento`, `idUsuarioAdmin`) VALUES
(1,  1,  'LOT-PIL-A',   70,  70,  DATE_ADD(CURDATE(), INTERVAL 7 DAY),   1),
(2,  1,  'LOT-PIL-B',   50,  50,  DATE_ADD(CURDATE(), INTERVAL 45 DAY),  1),
(3,  2,  'LOT-YOG-A',   45,  45,  DATE_ADD(CURDATE(), INTERVAL 30 DAY),  1),
(4,  3,  'LOT-MAN-A',   30,  30,  DATE_ADD(CURDATE(), INTERVAL 45 DAY),  1),
(5,  4,  'LOT-COC-A',   80,  80,  DATE_ADD(CURDATE(), INTERVAL 150 DAY), 1),
(6,  5,  'LOT-FAN-A',   50,  50,  DATE_ADD(CURDATE(), INTERVAL 130 DAY), 1),
(7,  6,  'LOT-VIT-A',  100, 100,  DATE_ADD(CURDATE(), INTERVAL 300 DAY), 1),
(8,  7,  'LOT-PAC-A',  150, 150,  DATE_ADD(CURDATE(), INTERVAL 220 DAY), 1),
(9,  8,  'LOT-MAY-A',   60,  60,  DATE_ADD(CURDATE(), INTERVAL 90 DAY),  1),
(10, 9,  'LOT-KET-A',   55,  55,  DATE_ADD(CURDATE(), INTERVAL 140 DAY), 1),
(11, 10, 'LOT-TOM-A',   70,  70,  DATE_ADD(CURDATE(), INTERVAL 190 DAY), 1),
(12, 11, 'LOT-CHO-A',   25,  25,  DATE_ADD(CURDATE(), INTERVAL 15 DAY),  1),
(13, 12, 'LOT-JAM-A',   40,  40,  DATE_ADD(CURDATE(), INTERVAL 40 DAY),  1),
(14, 13, 'LOT-SAL-A',   35,  35,  DATE_ADD(CURDATE(), INTERVAL 20 DAY),  1),
(15, 14, 'LOT-MOK-A',   90,  90,  DATE_ADD(CURDATE(), INTERVAL 100 DAY), 1),
(16, 15, 'LOT-BON-A',   30,  30,  DATE_ADD(CURDATE(), INTERVAL 160 DAY), 1),
(17, 16, 'LOT-OMO-A',   60,  60,  NULL, 1),
(18, 17, 'LOT-OLA-A',   40,  40,  NULL, 1),
(19, 18, 'LOT-LUX-A',   80,  80,  NULL, 1),
(20, 19, 'LOT-COL-A',   50,  50,  DATE_ADD(CURDATE(), INTERVAL 450 DAY), 1),
(21, 20, 'LOT-TOS-A',   40,  40,  DATE_ADD(CURDATE(), INTERVAL 25 DAY),  1),
(22, 21, 'LOT-ROY-A',   65,  65,  DATE_ADD(CURDATE(), INTERVAL 250 DAY), 1);

UPDATE `PRODUCTO` p
SET p.`stockActual` = (SELECT COALESCE(SUM(l.`cantidadDisponible`), 0)
                       FROM `LOTE` l
                       WHERE l.`idProducto` = p.`id` AND l.`estaActivo` = 1),
    p.`fechaActualizacion` = p.`fechaActualizacion`;

INSERT INTO `HISTORIAL_PRECIO`
(`idProducto`, `precioVentaAnterior`, `precioVentaNuevo`, `precioCostoAnterior`, `precioCostoNuevo`, `motivoCambio`, `idUsuario`, `fechaCambio`) VALUES
(1,  6.00, 6.50, 5.00, 5.20, 'Ajuste de precio del proveedor PIL', 1, '2026-01-15 08:30:00'),
(4, 10.50, 11.00, 8.50, 9.00, 'Incremento de tarifa de EMBOL', 1, '2026-02-01 10:00:00'),
(11, 26.00, 28.50, 21.00, 23.50, 'Ajuste de precio por temporada Sofía', 1, '2026-03-10 14:15:00');

INSERT INTO `CLIENTE` (`id`, `nitCi`, `razonSocial`, `correo`, `telefono`, `idUsuarioCreacion`) VALUES
(1, '0', 'Sin Nombre / Cliente Genérico', NULL, NULL, 1),
(2, '4545451226', 'JUAN PEREZ VARGAS', 'jperez@gmail.com', '77123456', 2),
(3, '1020304050', 'MARIA ROJAS LOPEZ', 'mrojas@gmail.com', '70711223', 2);

SELECT * FROM `vw_resumen_ventas`;

SELECT v.id, v.montoTotal, SUM(dv.subtotal) AS sumaDetalles,
       (v.montoTotal = SUM(dv.subtotal)) AS coincide
FROM `VENTA` v
INNER JOIN `DETALLE_VENTA` dv ON dv.idVenta = v.id
GROUP BY v.id, v.montoTotal;

SELECT `id`, `nombre`, `stockCalculado`, `bajoStockMinimo`
FROM `vw_productos_con_stock` ORDER BY `id`;

SELECT * FROM `vw_lotes_por_vencer`;
