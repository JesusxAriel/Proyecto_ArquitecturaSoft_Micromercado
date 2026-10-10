-- ================================================================
-- BASE DE DATOS: bdMicroMercadoArqui   (VERSION 2.2 - FINAL)
-- PROYECTO: Punto Casero POS  |  ARQUITECTURA DE SOFTWARE
-- MOTOR: MySQL 8.0.16+ (CHECK, JSON, columnas generadas, triggers)
-- ================================================================
-- PROCESO PRINCIPAL
--   VENTA     : 1 INSERT Venta, N INSERT Detalle_Venta, N UPDATE stock
--   ANULACION : 1 UPDATE Venta (estado), N UPDATE stock
--
-- IDEA CLAVE DE ESTA VERSION
--   Las FOREIGN KEY solo protegen el DELETE fisico. Pero el sistema
--   usa BAJA LOGICA (estaActivo = 0), que es un UPDATE y las FK NO lo
--   detectan. Por eso, ademas de las FK (RESTRICT), esta version agrega
--   TRIGGERS que bloquean las bajas y cambios peligrosos:
--
--   REGLAS DE INTEGRIDAD
--   1. No se puede desactivar una CATEGORIA / PROVEEDOR / EMPAQUE que
--      tenga PRODUCTOS ACTIVOS.
--   2. No se puede crear, reactivar ni mover un PRODUCTO hacia una
--      categoria / proveedor / empaque inactivo.
--   3. No se puede desactivar un PRODUCTO con stock en lotes activos.
--   4. No se puede crear un LOTE para un producto inactivo, ni un lote
--      ya vencido, ni cambiar un lote de producto.
--   5. Cambiar precios exige indicar QUIEN lo hizo (idUsuarioActualizacion).
--   6. El HISTORIAL_PRECIO es inmutable (no UPDATE / DELETE).
--   7. Las VENTAS no se borran ni se editan: solo se ANULAN. El detalle
--      es inmutable y solo se agrega mientras la venta esta en proceso.
--   8. Una venta solo puede hacerse con cliente y cajero ACTIVOS.
--   9. Siempre debe existir al menos 1 ADMINISTRADOR activo.
--  10. Los roles base (Administrador, Cajero) no se renombran ni desactivan,
--      y no se desactiva un rol con usuarios activos.
--  11. El cliente generico (id = 1) no se puede desactivar ni cambiar.
--
--   La aplicacion debe capturar los errores SQLSTATE '45000' y mostrar
--   el MESSAGE_TEXT al usuario.
--
-- NOTA SOBRE EL STOCK
--   El stock NO se guarda en PRODUCTO. Se controla por LOTE
--   (LOTE.cantidadDisponible) para manejar vencimientos (FEFO: el
--   primero que vence es el primero que sale). El stock de un
--   producto se obtiene con la vista vw_productos_con_stock.
--
-- AUDITORIA
--   idUsuarioAdmin = usuario que registro el dato, e idUsuarioActualizacion =
--   ultimo usuario que lo modifico (catalogos). CLIENTE usa idUsuarioCreacion
--   porque lo registra el cajero. + fechaCreacion / fechaActualizacion.
--   VENTA.idUsuario = cajero que registra la venta; ademas
--   idUsuarioAnulacion / fechaAnulacion / motivoAnulacion.
--   HISTORIAL_PRECIO.idUsuario = quien cambio el precio.
--
-- ROLES
--   Administrador = Gerente / usuario root: control total del sistema.
--   Cajero = registra clientes y ventas.
--   (Los permisos por pantalla/accion se validan en la aplicacion.)
--
-- NOTA: este script usa DELIMITER, ejecutalo en MySQL Workbench,
--       DBeaver o el cliente mysql (no en ejecutores que no lo soporten).
-- ================================================================

DROP DATABASE IF EXISTS `bdMicroMercadoArqui`;
CREATE DATABASE `bdMicroMercadoArqui`
  DEFAULT CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE `bdMicroMercadoArqui`;


-- ================================================================
-- 1. ROL
-- ================================================================
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


-- ================================================================
-- 2. USUARIO (login, sesion y auditoria)
-- ================================================================
CREATE TABLE `USUARIO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `username` VARCHAR(50) NOT NULL,
  `passwordHash` VARCHAR(255) NOT NULL,   -- NUNCA guardar texto plano
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


-- ================================================================
-- 3. CATEGORIAS
-- ================================================================
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

  -- Permite repetir el nombre solo si el registro anterior esta inactivo
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


-- ================================================================
-- 4. PROVEEDOR
--    esAutogestionado = 1 -> el proveedor repone la mercaderia por
--    su cuenta (ej. distribuidor que visita el local).
-- ================================================================
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


-- ================================================================
-- 5. EMPAQUE
-- ================================================================
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


-- ================================================================
-- 6. PRODUCTO  (SIN columna stock: se calcula desde LOTE)
-- ================================================================
CREATE TABLE `PRODUCTO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `idEmpaque` INT NOT NULL,
  `precioVenta` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `precioCosto` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `stockMinimo` INT NOT NULL DEFAULT 10,

  `idCategoria` INT NOT NULL,
  `idProveedor` INT NOT NULL,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,   -- ultimo usuario que modifico
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


-- ================================================================
-- 7. LOTE (control de stock y vencimientos)
-- ================================================================
CREATE TABLE `LOTE` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `idProducto` INT NOT NULL,
  `codigoLote` VARCHAR(50) NOT NULL,
  `cantidadInicial` INT NOT NULL,
  `cantidadDisponible` INT NOT NULL,
  `fechaVencimiento` DATE DEFAULT NULL,   -- NULL = no perecible

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `idUsuarioActualizacion` INT DEFAULT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Lote_producto_codigo` (`idProducto`, `codigoLote`),
  -- Necesario para la FK compuesta de DETALLE_VENTA (lote + producto)
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
  -- RESTRICT: un lote nunca debe desaparecer por borrar un producto
  CONSTRAINT `FK_Lote_Producto` FOREIGN KEY (`idProducto`)
    REFERENCES `PRODUCTO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Lote_UsuarioAdmin` FOREIGN KEY (`idUsuarioAdmin`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Lote_UsuarioActualizacion` FOREIGN KEY (`idUsuarioActualizacion`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ================================================================
-- 8. HISTORIAL_PRECIO  (auditoria INMUTABLE: ver triggers)
-- ================================================================
CREATE TABLE `HISTORIAL_PRECIO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `idProducto` INT NOT NULL,
  `precioVentaAnterior` DECIMAL(10,2) NOT NULL,
  `precioVentaNuevo` DECIMAL(10,2) NOT NULL,
  `precioCostoAnterior` DECIMAL(10,2) DEFAULT NULL,
  `precioCostoNuevo` DECIMAL(10,2) DEFAULT NULL,
  `motivoCambio` VARCHAR(255) DEFAULT 'Ajuste de precio',
  `idUsuario` INT NOT NULL,              -- quien cambio el precio
  `fechaCambio` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  PRIMARY KEY (`id`),
  KEY `FK_HistorialPrecio_Producto` (`idProducto`),
  KEY `FK_HistorialPrecio_Usuario` (`idUsuario`),
  -- RESTRICT: la auditoria no se pierde si se intenta borrar el producto
  CONSTRAINT `FK_HistorialPrecio_Producto` FOREIGN KEY (`idProducto`)
    REFERENCES `PRODUCTO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_HistorialPrecio_Usuario` FOREIGN KEY (`idUsuario`)
    REFERENCES `USUARIO` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ================================================================
-- 9. CLIENTE
-- ================================================================
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


-- ================================================================
-- 10. VENTA (tabla principal del proceso)
--     Se quito `estaActivo`: el ciclo de vida es solo `estado`
--     (COMPLETADA / ANULADA). Una venta NUNCA se borra ni se edita.
-- ================================================================
CREATE TABLE `VENTA` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nroComprobante` VARCHAR(50) NOT NULL,
  `idCliente` INT NOT NULL,
  `idUsuario` INT NOT NULL,                     -- Cajero que registra la venta
  `fechaVenta` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `montoTotal` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `efectivoPagado` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `cambio` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `estado` ENUM('COMPLETADA','ANULADA') NOT NULL DEFAULT 'COMPLETADA',

  -- Auditoria de anulacion
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
  -- Coherencia del estado con los datos de anulacion
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


-- ================================================================
-- 11. DETALLE_VENTA  (relacion N:M  VENTA <-> PRODUCTO)
--     - Registra el LOTE del que salio la mercaderia.
--     - FK COMPUESTA (idLote, idProducto): garantiza que el lote
--       realmente pertenece a ese producto.
--     - subtotal es columna GENERADA: no puede quedar inconsistente.
-- ================================================================
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


-- ================================================================
-- VISTAS
-- ================================================================

-- Vista 1: productos con stock calculado desde LOTE (ignora lotes vencidos)
--   stockCalculado se castea a entero (SUM devuelve DECIMAL en MySQL).
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
    CAST(COALESCE(SUM(l.cantidadDisponible), 0) AS SIGNED) AS stockCalculado,
    (COALESCE(SUM(l.cantidadDisponible), 0) <= p.stockMinimo) AS bajoStockMinimo
FROM `PRODUCTO` p
INNER JOIN `EMPAQUE` e     ON p.idEmpaque = e.id
INNER JOIN `CATEGORIAS` c   ON p.idCategoria = c.id
INNER JOIN `PROVEEDOR` pr  ON p.idProveedor = pr.id
LEFT JOIN `LOTE` l
       ON p.id = l.idProducto
      AND l.estaActivo = 1
      AND (l.fechaVencimiento IS NULL OR l.fechaVencimiento >= CURRENT_DATE())
WHERE p.estaActivo = 1
GROUP BY
    p.id, p.nombre, p.idEmpaque, e.nombre, p.precioVenta, p.precioCosto,
    p.stockMinimo, p.idCategoria, c.nombre, p.idProveedor, pr.nombreEmpresa,
    p.estaActivo;

-- Vista 2: resumen de ventas para tablas / listados de la UI
--   Incluye ANULADAS (filtrar por estado en la UI si hace falta).
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

-- Vista 3: estructura completa del comprobante / recibo
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

-- Vista 4: lotes proximos a vencer (30 dias) o ya vencidos
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


-- ================================================================
-- TRIGGERS DE INTEGRIDAD
-- (los mensajes de SIGNAL no pueden pasar de 128 caracteres)
-- ================================================================
DELIMITER $$

-- ----------------------------------------------------------------
-- ROL: roles base protegidos + no desactivar rol con usuarios activos
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- USUARIO: rol activo + siempre debe quedar 1 Administrador activo
-- ----------------------------------------------------------------
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

    -- Reactivar o cambiar de rol: el rol destino debe estar activo
    IF NEW.estaActivo = 1 AND (OLD.estaActivo = 0 OR NEW.idRol <> OLD.idRol) THEN
        IF NOT EXISTS (SELECT 1 FROM `ROL` WHERE id = NEW.idRol AND estaActivo = 1) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'El rol no existe o esta inactivo';
        END IF;
    END IF;

    -- Nunca dejar el sistema sin Administrador activo
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

-- ----------------------------------------------------------------
-- CATEGORIAS: no desactivar si tiene productos ACTIVOS
--   (el DELETE fisico ya lo bloquea la FK con RESTRICT)
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- PROVEEDOR: no desactivar si tiene productos ACTIVOS
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- EMPAQUE: no desactivar si tiene productos ACTIVOS
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- PRODUCTO (INSERT): categoria, proveedor y empaque deben estar activos
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- PRODUCTO (UPDATE):
--   a) al reactivar o mover de categoria/proveedor/empaque, los
--      destinos deben estar activos
--   b) no desactivar si aun tiene stock en lotes activos
--   c) un cambio de precio exige idUsuarioActualizacion
-- ----------------------------------------------------------------
CREATE TRIGGER `trg_producto_bu`
BEFORE UPDATE ON `PRODUCTO`
FOR EACH ROW
BEGIN
    -- a) padres activos
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

    -- b) baja de producto con stock
    IF OLD.estaActivo = 1 AND NEW.estaActivo = 0 THEN
        IF EXISTS (SELECT 1 FROM `LOTE`
                   WHERE idProducto = OLD.id AND estaActivo = 1 AND cantidadDisponible > 0) THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'No se puede desactivar el producto: aun tiene stock en lotes activos';
        END IF;
    END IF;

    -- c) auditoria obligatoria al cambiar precios
    IF (NEW.precioVenta <> OLD.precioVenta OR NEW.precioCosto <> OLD.precioCosto)
       AND NEW.idUsuarioActualizacion IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Para cambiar precios debe indicar idUsuarioActualizacion';
    END IF;
END$$

-- ----------------------------------------------------------------
-- PRODUCTO (AFTER UPDATE): registra el historial de precios.
--   La app puede indicar el motivo ANTES del UPDATE con:
--       SET @motivo_cambio_precio = 'Ajuste del proveedor';
--   y la app SIEMPRE debe enviar idUsuarioActualizacion en el UPDATE.
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- HISTORIAL_PRECIO: inmutable
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- LOTE: producto activo, no nacer vencido, no cambiar de producto
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- CLIENTE: el cliente generico (id = 1) esta protegido
-- ----------------------------------------------------------------
CREATE TRIGGER `trg_cliente_bu`
BEFORE UPDATE ON `CLIENTE`
FOR EACH ROW
BEGIN
    IF OLD.id = 1 AND (NEW.estaActivo <> 1 OR NEW.nitCi <> OLD.nitCi) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cliente generico (id 1) no se puede desactivar ni cambiar de NIT/CI';
    END IF;
END$$

-- ----------------------------------------------------------------
-- VENTA
--   INSERT : solo con cliente y cajero activos, siempre COMPLETADA
--   UPDATE : una venta ANULADA no se toca; una venta cerrada solo
--            puede pasar a ANULADA (sus datos no se editan)
--   DELETE : prohibido
--   (nroComprobante 'TMP-...' = venta aun en proceso dentro del SP)
-- ----------------------------------------------------------------
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

-- ----------------------------------------------------------------
-- DETALLE_VENTA: solo se agrega mientras la venta esta en proceso;
--                despues es inmutable
-- ----------------------------------------------------------------
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


-- ================================================================
-- DATOS DE PRUEBA
-- ================================================================

-- ROLES
INSERT INTO `ROL` (`id`, `nombre`, `descripcion`) VALUES
(1, 'Administrador', 'Gerente / root: control total del sistema'),
(2, 'Cajero', 'Registro de ventas, anulaciones y atencion al cliente');

-- USUARIOS (hash SHA-256 solo para pruebas; en la aplicacion usar bcrypt/argon2)
INSERT INTO `USUARIO` (`id`, `username`, `passwordHash`, `nombreCompleto`, `correo`, `idRol`) VALUES
(1, 'admin',   SHA2('admin123', 256),  'Gerente General (root)', 'gerencia@micromercado.com', 1),
(2, 'cajero1', SHA2('cajero123', 256), 'Carlos Mamani Quispe', 'carlos.mamani@micromercado.com', 2);

-- CATEGORIAS
INSERT INTO `CATEGORIAS` (`id`, `nombre`, `descripcion`, `codigo`, `pasilloUbicacion`, `idUsuarioAdmin`) VALUES
(1, 'Lácteos y Derivados', 'Leches, yogures, quesos y mantequillas', 'CAT-LAC', 'Pasillo 1', 1),
(2, 'Bebidas y Gaseosas', 'Refrescos, jugos, aguas y energizantes', 'CAT-BEB', 'Pasillo 2', 1),
(3, 'Abarrotes', 'Arroz, fideos, aceite, azúcar, sal y condimentos', 'CAT-ABA', 'Pasillo 3', 1),
(4, 'Embutidos y Fiambres', 'Salchichas, jamones, chorizos y carnes frías', 'CAT-EMB', 'Pasillo 4', 1),
(5, 'Snacks y Galletas', 'Papas fritas, pipocas, chocolates y galletas', 'CAT-SNA', 'Pasillo 5', 1),
(6, 'Limpieza del Hogar', 'Detergentes, lavavajillas, desinfectantes y lavandina', 'CAT-LIM', 'Pasillo 6', 1),
(7, 'Cuidado Personal', 'Jabones, champús, pasta dental y desodorantes', 'CAT-CUI', 'Pasillo 7', 1),
(8, 'Panadería y Repostería', 'Pan fresco, kekes, tostadas y harinas', 'CAT-PAN', 'Pasillo 8', 1);

-- PROVEEDORES
INSERT INTO `PROVEEDOR` (`id`, `nombreEmpresa`, `numeroEmpresa`, `correoReferencia`, `esAutogestionado`, `idUsuarioAdmin`) VALUES
(1, 'PIL Andina S.A.', '44112233', 'ventas.cbba@pilandina.com.bo', 0, 1),
(2, 'EMBOL S.A. (Coca-Cola)', '44556677', 'pedidos.cbba@embol.com.bo', 0, 1),
(3, 'Sofía SHE S.A.', '44889900', 'contacto.cbba@sofia.com.bo', 1, 1),
(4, 'CBN (Cervecería Boliviana Nacional)', '44224466', 'distribucion@cbn.com.bo', 0, 1),
(5, 'Unilever Bolivia', '44335577', 'atencion@unilever.com', 0, 1),
(6, 'Industrias Venado S.A. (Kris)', '44118899', 'ventas@venado.com.bo', 0, 1),
(7, 'Arcor Bolivia', '44778811', 'pedidos@arcor.com.bo', 0, 1);

-- EMPAQUES
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

-- PRODUCTOS
-- NOTA: EMPAQUE indica el TIPO de envase. La medida/presentacion
-- (1L, 500g, etc.) va en el nombre del producto, asi se mantiene
-- unico (nombre + empaque).
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

-- LOTES
-- Las fechas de vencimiento son RELATIVAS a la fecha actual para que el
-- script siga funcionando sin importar cuando se ejecute (ningun lote
-- nace vencido). El producto 1 tiene 2 lotes para probar FEFO.
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

-- HISTORIAL DE PRECIOS (datos historicos de ejemplo)
INSERT INTO `HISTORIAL_PRECIO`
(`idProducto`, `precioVentaAnterior`, `precioVentaNuevo`, `precioCostoAnterior`, `precioCostoNuevo`, `motivoCambio`, `idUsuario`, `fechaCambio`) VALUES
(1,  6.00, 6.50, 5.00, 5.20, 'Ajuste de precio del proveedor PIL', 1, '2026-01-15 08:30:00'),
(4, 10.50, 11.00, 8.50, 9.00, 'Incremento de tarifa de EMBOL', 1, '2026-02-01 10:00:00'),
(11, 26.00, 28.50, 21.00, 23.50, 'Ajuste de precio por temporada Sofía', 1, '2026-03-10 14:15:00');

-- CLIENTES
INSERT INTO `CLIENTE` (`id`, `nitCi`, `razonSocial`, `correo`, `telefono`, `idUsuarioCreacion`) VALUES
(1, '0', 'Sin Nombre / Cliente Genérico', NULL, NULL, 1),
(2, '4545451226', 'JUAN PEREZ VARGAS', 'jperez@gmail.com', '77123456', 2),
(3, '1020304050', 'MARIA ROJAS LOPEZ', 'mrojas@gmail.com', '70711223', 2);


-- ================================================================
-- PROCEDIMIENTOS ALMACENADOS
-- ================================================================
DELIMITER $$

-- ----------------------------------------------------------------
-- sp_RegistrarVenta
--   Realiza TODO el proceso en UNA transaccion:
--     1 INSERT en VENTA
--     N INSERT en DETALLE_VENTA   (uno por cada lote consumido)
--     N UPDATE en LOTE            (descuento de stock, FEFO)
--   Si algo falla (stock insuficiente, producto inexistente, pago
--   insuficiente, cliente/usuario inactivo...) se hace ROLLBACK total.
--
--   p_detalles es un JSON con los productos de la venta:
--     '[{"idProducto":14,"cantidad":1},{"idProducto":10,"cantidad":2}]'
--   (idProducto y cantidad deben ser numeros enteros)
-- ----------------------------------------------------------------
DROP PROCEDURE IF EXISTS `sp_RegistrarVenta`$$
CREATE PROCEDURE `sp_RegistrarVenta`(
    IN  p_idCliente      INT,
    IN  p_idUsuario      INT,
    IN  p_efectivoPagado DECIMAL(10,2),
    IN  p_detalles       JSON,
    OUT p_idVenta        INT,
    OUT p_nroComprobante VARCHAR(50)
)
BEGIN
    DECLARE v_n          INT DEFAULT 0;
    DECLARE v_i          INT DEFAULT 0;
    DECLARE v_idProducto INT;
    DECLARE v_cantidad   INT;
    DECLARE v_tipoId     VARCHAR(30);
    DECLARE v_tipoCant   VARCHAR(30);
    DECLARE v_precio     DECIMAL(10,2);
    DECLARE v_restante   INT;
    DECLARE v_idLote     INT;
    DECLARE v_disponible INT;
    DECLARE v_tomar      INT;
    DECLARE v_total      DECIMAL(10,2) DEFAULT 0.00;

    -- Ante cualquier error: deshacer todo y propagar el error
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    -- Validaciones de entrada
    IF p_detalles IS NULL OR JSON_TYPE(p_detalles) <> 'ARRAY'
       OR JSON_LENGTH(p_detalles) = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La venta debe tener al menos un producto';
    END IF;

    IF p_efectivoPagado IS NULL OR p_efectivoPagado < 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El efectivo pagado es invalido';
    END IF;

    SET v_n = JSON_LENGTH(p_detalles);

    START TRANSACTION;

    -- 1 INSERT en VENTA (el trigger valida cliente y cajero activos;
    --   el nro de comprobante definitivo se asigna abajo)
    INSERT INTO `VENTA`
        (`nroComprobante`, `idCliente`, `idUsuario`, `fechaVenta`,
         `montoTotal`, `efectivoPagado`, `cambio`, `estado`)
    VALUES
        (CONCAT('TMP-', UUID()), p_idCliente, p_idUsuario, NOW(),
         0, 0, 0, 'COMPLETADA');

    SET p_idVenta = LAST_INSERT_ID();

    -- Recorrer cada producto del JSON
    WHILE v_i < v_n DO
        SET v_tipoId   = JSON_TYPE(JSON_EXTRACT(p_detalles, CONCAT('$[', v_i, '].idProducto')));
        SET v_tipoCant = JSON_TYPE(JSON_EXTRACT(p_detalles, CONCAT('$[', v_i, '].cantidad')));

        IF COALESCE(v_tipoId, '')   NOT IN ('INTEGER', 'UNSIGNED INTEGER')
           OR COALESCE(v_tipoCant, '') NOT IN ('INTEGER', 'UNSIGNED INTEGER') THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Detalle invalido: idProducto y cantidad deben ser enteros';
        END IF;

        SET v_idProducto = JSON_UNQUOTE(JSON_EXTRACT(p_detalles, CONCAT('$[', v_i, '].idProducto')));
        SET v_cantidad   = JSON_UNQUOTE(JSON_EXTRACT(p_detalles, CONCAT('$[', v_i, '].cantidad')));

        IF v_cantidad <= 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Detalle invalido: la cantidad debe ser mayor a 0';
        END IF;

        -- Precio vigente del producto. FOR SHARE: si alguien esta
        -- cambiando el precio en este instante, la venta espera.
        SET v_precio = NULL;
        SELECT `precioVenta` INTO v_precio
        FROM `PRODUCTO`
        WHERE `id` = v_idProducto AND `estaActivo` = 1
        FOR SHARE;

        IF v_precio IS NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Producto inexistente o inactivo';
        END IF;

        -- Descontar de los lotes: primero el que vence antes (FEFO),
        -- ignorando lotes vencidos. Los lotes sin vencimiento van al final.
        SET v_restante = v_cantidad;

        WHILE v_restante > 0 DO
            SET v_idLote = NULL;

            SELECT `id`, `cantidadDisponible` INTO v_idLote, v_disponible
            FROM `LOTE`
            WHERE `idProducto` = v_idProducto
              AND `estaActivo` = 1
              AND `cantidadDisponible` > 0
              AND (`fechaVencimiento` IS NULL OR `fechaVencimiento` >= CURDATE())
            ORDER BY (`fechaVencimiento` IS NULL), `fechaVencimiento`, `id`
            LIMIT 1
            FOR UPDATE;

            IF v_idLote IS NULL THEN
                SIGNAL SQLSTATE '45000'
                    SET MESSAGE_TEXT = 'Stock insuficiente para uno de los productos';
            END IF;

            SET v_tomar = LEAST(v_restante, v_disponible);

            -- N INSERT en DETALLE_VENTA (subtotal es columna generada)
            INSERT INTO `DETALLE_VENTA`
                (`idVenta`, `idProducto`, `idLote`, `cantidad`, `precioUnitario`)
            VALUES
                (p_idVenta, v_idProducto, v_idLote, v_tomar, v_precio);

            -- N UPDATE de stock en LOTE
            UPDATE `LOTE`
            SET `cantidadDisponible` = `cantidadDisponible` - v_tomar
            WHERE `id` = v_idLote;

            SET v_total    = v_total + (v_tomar * v_precio);
            SET v_restante = v_restante - v_tomar;
        END WHILE;

        SET v_i = v_i + 1;
    END WHILE;

    -- Validar el pago
    IF p_efectivoPagado < v_total THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El efectivo pagado es menor al monto total';
    END IF;

    SET p_nroComprobante = CONCAT('V-', DATE_FORMAT(NOW(), '%Y%m%d'), '-', LPAD(p_idVenta, 4, '0'));

    -- Cerrar la venta con totales y comprobante definitivo
    UPDATE `VENTA`
    SET `nroComprobante` = p_nroComprobante,
        `montoTotal`     = v_total,
        `efectivoPagado` = p_efectivoPagado,
        `cambio`         = p_efectivoPagado - v_total
    WHERE `id` = p_idVenta;

    COMMIT;
END$$


-- ----------------------------------------------------------------
-- sp_AnularVenta  (eliminacion logica)
--     1 UPDATE en VENTA (estado = ANULADA)
--     N UPDATE en LOTE  (restaura el stock de cada lote usado)
--   Valida que la venta exista, que NO este anulada ya, y que el
--   usuario que anula este activo (lo valida tambien el trigger).
--   Que ROL puede anular (ej. solo Administrador) se controla en la
--   aplicacion.
-- ----------------------------------------------------------------
DROP PROCEDURE IF EXISTS `sp_AnularVenta`$$
CREATE PROCEDURE `sp_AnularVenta`(
    IN p_idVenta            INT,
    IN p_idUsuarioAnulacion INT,
    IN p_motivo             VARCHAR(255)
)
BEGIN
    DECLARE v_estado   VARCHAR(20);
    DECLARE v_idLote   INT;
    DECLARE v_cantidad INT;
    DECLARE v_done     INT DEFAULT 0;

    -- Cursor sobre los detalles de la venta
    DECLARE curDetalles CURSOR FOR
        SELECT `idLote`, `cantidad`
        FROM `DETALLE_VENTA`
        WHERE `idVenta` = p_idVenta;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_motivo IS NULL OR TRIM(p_motivo) = '' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Debe indicar el motivo de la anulacion';
    END IF;

    IF p_idUsuarioAnulacion IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Debe indicar el usuario que anula la venta';
    END IF;

    START TRANSACTION;

    -- Bloquear la venta y validar su estado
    SET v_estado = NULL;
    SELECT `estado` INTO v_estado
    FROM `VENTA`
    WHERE `id` = p_idVenta
    FOR UPDATE;

    IF v_estado IS NULL OR v_estado <> 'COMPLETADA' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La venta no existe o ya fue anulada';
    END IF;

    -- 1 UPDATE en VENTA
    UPDATE `VENTA`
    SET `estado`             = 'ANULADA',
        `motivoAnulacion`    = TRIM(p_motivo),
        `idUsuarioAnulacion` = p_idUsuarioAnulacion,
        `fechaAnulacion`     = NOW()
    WHERE `id` = p_idVenta;

    -- N UPDATE en LOTE (restaurar stock)
    SET v_done = 0;   -- reiniciar: el SELECT INTO anterior pudo activar el handler
    OPEN curDetalles;
    read_loop: LOOP
        FETCH curDetalles INTO v_idLote, v_cantidad;
        IF v_done = 1 THEN
            LEAVE read_loop;
        END IF;

        UPDATE `LOTE`
        SET `cantidadDisponible` = `cantidadDisponible` + v_cantidad
        WHERE `id` = v_idLote;
    END LOOP;
    CLOSE curDetalles;

    COMMIT;
END$$

DELIMITER ;


-- ================================================================
-- VENTA DE PRUEBA (usa el procedimiento, asi el stock queda coherente)
-- Cajero 2 (Carlos Mamani) vende al cliente 2 (Juan Perez):
--   1 x Galletas Moka (4.50) + 2 x Salsa de Tomate (16.00)
--   + 1 x Tostadas (7.50) = 28.00  |  paga 50.00  |  cambio 22.00
-- ================================================================
CALL sp_RegistrarVenta(
    2, 2, 50.00,
    '[{"idProducto":14,"cantidad":1},{"idProducto":10,"cantidad":2},{"idProducto":20,"cantidad":1}]',
    @idVenta, @nroComprobante
);


-- ================================================================
-- CONSULTAS DE VERIFICACION
-- ================================================================

-- Venta registrada y su comprobante
SELECT @idVenta AS idVenta, @nroComprobante AS nroComprobante;
SELECT * FROM `vw_resumen_ventas`;
SELECT * FROM `vw_comprobante_venta` WHERE `idVenta` = @idVenta;

-- Control cruzado: el total de la venta debe ser igual a la suma de sus detalles
SELECT v.id, v.montoTotal, SUM(dv.subtotal) AS sumaDetalles,
       (v.montoTotal = SUM(dv.subtotal)) AS coincide
FROM `VENTA` v
INNER JOIN `DETALLE_VENTA` dv ON dv.idVenta = v.id
GROUP BY v.id, v.montoTotal;

-- Stock calculado (Galletas, Salsa y Tostadas ya descontados)
SELECT `id`, `nombre`, `stockCalculado`, `bajoStockMinimo`
FROM `vw_productos_con_stock` ORDER BY `id`;

-- Lotes por vencer
SELECT * FROM `vw_lotes_por_vencer`;


-- ----------------------------------------------------------------
-- PRUEBAS OPCIONALES (descomentar de a una; todas deben dar ERROR
-- salvo las marcadas como OK)
-- ----------------------------------------------------------------

-- ===== VENTAS =====
-- 1) Anular la venta (OK): el stock vuelve a su valor original
-- CALL sp_AnularVenta(@idVenta, 2, 'Error en el registro del cliente');
-- SELECT `id`, `nombre`, `stockCalculado` FROM `vw_productos_con_stock` WHERE `id` IN (10, 14, 20);

-- 2) Anular otra vez: "La venta no existe o ya fue anulada"
-- CALL sp_AnularVenta(@idVenta, 2, 'Intento duplicado');

-- 3) Stock insuficiente: error y NO queda nada guardado
-- CALL sp_RegistrarVenta(1, 2, 99999.00, '[{"idProducto":1,"cantidad":9999}]', @id2, @nro2);

-- 4) 100 unidades de Leche (OK): consume el lote que vence primero y
--    completa con el siguiente (genera 2 filas en DETALLE_VENTA)
-- CALL sp_RegistrarVenta(1, 2, 1000.00, '[{"idProducto":1,"cantidad":100}]', @id3, @nro3);
-- SELECT * FROM `vw_comprobante_venta` WHERE `idVenta` = @id3;

-- 5) Cantidad no entera: "Detalle invalido: idProducto y cantidad deben ser enteros"
-- CALL sp_RegistrarVenta(1, 2, 100.00, '[{"idProducto":1,"cantidad":"2"}]', @id4, @nro4);

-- 6) Editar o borrar una venta / su detalle:
-- UPDATE `DETALLE_VENTA` SET `cantidad` = 99 WHERE `idVenta` = @idVenta;
-- DELETE FROM `DETALLE_VENTA` WHERE `idVenta` = @idVenta;
-- UPDATE `VENTA` SET `montoTotal` = 1 WHERE `id` = @idVenta;
-- DELETE FROM `VENTA` WHERE `id` = @idVenta;

-- ===== CATALOGOS (bajas peligrosas) =====
-- 7) Desactivar categoria / proveedor / empaque CON productos activos
-- UPDATE `CATEGORIAS` SET `estaActivo` = 0, `idUsuarioActualizacion` = 1 WHERE `id` = 1;
-- UPDATE `PROVEEDOR` SET `estaActivo` = 0, `idUsuarioActualizacion` = 1 WHERE `id` = 1;
-- UPDATE `EMPAQUE`   SET `estaActivo` = 0, `idUsuarioActualizacion` = 1 WHERE `id` = 1;

-- 8) Borrar fisicamente una categoria con productos (lo frena la FK, error 1451)
-- DELETE FROM `CATEGORIAS` WHERE `id` = 1;

-- 9) Desactivar un producto que todavia tiene stock
-- UPDATE `PRODUCTO` SET `estaActivo` = 0, `idUsuarioActualizacion` = 1 WHERE `id` = 1;

-- 10) Crear un producto en una categoria inactiva (primero crear y desactivar una categoria vacia)
-- INSERT INTO `CATEGORIAS` (`nombre`, `codigo`, `idUsuarioAdmin`) VALUES ('Temporal', 'CAT-TMP', 1);
-- UPDATE `CATEGORIAS` SET `estaActivo` = 0, `idUsuarioActualizacion` = 1 WHERE `codigo` = 'CAT-TMP';  -- OK (vacia)
-- INSERT INTO `PRODUCTO` (`nombre`, `idEmpaque`, `idCategoria`, `idProveedor`, `idUsuarioAdmin`)
--   VALUES ('Prueba', 1, (SELECT `id` FROM `CATEGORIAS` WHERE `codigo` = 'CAT-TMP'), 1, 1);

-- ===== PRECIOS =====
-- 11) Cambiar precio SIN indicar usuario: error
-- UPDATE `PRODUCTO` SET `precioVenta` = 7.00 WHERE `id` = 1;

-- 12) Cambiar precio CON usuario y motivo (OK) -> queda en HISTORIAL_PRECIO
-- SET @motivo_cambio_precio = 'Prueba de auditoria';
-- UPDATE `PRODUCTO` SET `precioVenta` = 7.00, `idUsuarioActualizacion` = 1 WHERE `id` = 1;
-- SELECT * FROM `HISTORIAL_PRECIO` ORDER BY `id` DESC LIMIT 1;

-- 13) Modificar o borrar el historial: error
-- UPDATE `HISTORIAL_PRECIO` SET `precioVentaNuevo` = 1 WHERE `id` = 1;
-- DELETE FROM `HISTORIAL_PRECIO` WHERE `id` = 1;

-- ===== USUARIOS / ROLES / CLIENTES =====
-- 14) Desactivar al unico Administrador, o quitarle el rol
-- UPDATE `USUARIO` SET `estaActivo` = 0 WHERE `id` = 1;
-- UPDATE `USUARIO` SET `idRol` = 2 WHERE `id` = 1;

-- 15) Desactivar o renombrar un rol base / rol con usuarios activos
-- UPDATE `ROL` SET `estaActivo` = 0 WHERE `id` = 2;

-- 16) Desactivar el cliente generico
-- UPDATE `CLIENTE` SET `estaActivo` = 0 WHERE `id` = 1;

-- 17) Lote ya vencido, o lote para producto inactivo
-- INSERT INTO `LOTE` (`idProducto`, `codigoLote`, `cantidadInicial`, `cantidadDisponible`, `fechaVencimiento`, `idUsuarioAdmin`)
--   VALUES (1, 'LOT-VENCIDO', 10, 10, DATE_SUB(CURDATE(), INTERVAL 1 DAY), 1);
