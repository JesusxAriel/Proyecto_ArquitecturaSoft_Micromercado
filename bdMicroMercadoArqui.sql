DROP DATABASE IF EXISTS `bdMicroMercadoArqui`;
CREATE DATABASE `bdMicroMercadoArqui` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `bdMicroMercadoArqui`;

-- ========================================================
-- 1. TABLA PRINCIPAL 1: CATEGORIAS
-- Atributos Independientes: nombre, descripcion, codigo, pasilloUbicacion (4 Atributos)
-- ========================================================
DROP TABLE IF EXISTS `CATEGORIAS`;
CREATE TABLE `CATEGORIAS` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(255) DEFAULT NULL,
  `codigo` VARCHAR(20) NOT NULL,
  `pasilloUbicacion` VARCHAR(20) DEFAULT NULL,
  
  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1, -- 1: Activo, 0: Eliminado (Soft Delete)
  `idUsuarioAdmin` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Categorias_codigo` (`codigo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================================
-- 2. TABLA PRINCIPAL 2: PROVEEDOR
-- Atributos Independientes: nombreEmpresa, numeroEmpresa, correoReferencia, esAutogestionado (4 Atributos)
-- ========================================================
DROP TABLE IF EXISTS `PROVEEDOR`;
CREATE TABLE `PROVEEDOR` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombreEmpresa` VARCHAR(150) NOT NULL,
  `numeroEmpresa` VARCHAR(20) NOT NULL,
  `correoReferencia` VARCHAR(150) DEFAULT NULL,
  `esAutogestionado` TINYINT(1) NOT NULL DEFAULT 0, -- 0: Compra directa, 1: Consigna / Reposición propia
  
  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1, -- 1: Activo, 0: Eliminado (Soft Delete)
  `idUsuarioAdmin` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================================
-- 2.1 TABLA: EMPAQUE (catalogo normalizado de presentaciones)
-- PRODUCTO.idEmpaque referencia a esta tabla.
-- ========================================================
DROP TABLE IF EXISTS `EMPAQUE`;
CREATE TABLE `EMPAQUE` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(100) NOT NULL,

  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1, -- 1: Activo, 0: Eliminado (Soft Delete)
  `idUsuarioAdmin` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UQ_Empaque_nombre` (`nombre`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================================
-- 3. TABLA PRINCIPAL 3: PRODUCTO
-- Atributos Independientes: nombre, precioVenta, precioCosto, stockMinimo + idEmpaque (FK a EMPAQUE)
-- NOTA: Se eliminó el atributo 'stock' ya que se calcula dinámicamente desde LOTE.
-- ========================================================
DROP TABLE IF EXISTS `PRODUCTO`;
CREATE TABLE `PRODUCTO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `idEmpaque` INT NOT NULL,
  `precioVenta` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `precioCosto` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `stockMinimo` INT NOT NULL DEFAULT 10,
  
  -- Llaves Foráneas Relacionales
  `idCategoria` INT NOT NULL,
  `idProveedor` INT NOT NULL,
  
  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1, -- 1: Activo, 0: Eliminado (Soft Delete)
  `idUsuarioAdmin` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  
  PRIMARY KEY (`id`),
  KEY `FK_Producto_Empaque` (`idEmpaque`),
  KEY `FK_Producto_Categoria` (`idCategoria`),
  KEY `FK_Producto_Proveedor` (`idProveedor`),
  CONSTRAINT `FK_Producto_Empaque` FOREIGN KEY (`idEmpaque`) REFERENCES `EMPAQUE` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Producto_Categoria` FOREIGN KEY (`idCategoria`) REFERENCES `CATEGORIAS` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_Producto_Proveedor` FOREIGN KEY (`idProveedor`) REFERENCES `PROVEEDOR` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================================
-- 4. TABLA DE CONTROL DE VENCIMIENTOS: LOTE
-- ========================================================
DROP TABLE IF EXISTS `LOTE`;
CREATE TABLE `LOTE` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `idProducto` INT NOT NULL,
  `codigoLote` VARCHAR(50) NOT NULL,
  `cantidadInicial` INT NOT NULL,
  `cantidadDisponible` INT NOT NULL,
  `fechaVencimiento` DATE DEFAULT NULL,
  
  `estaActivo` TINYINT(1) NOT NULL DEFAULT 1,
  `idUsuarioAdmin` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  
  PRIMARY KEY (`id`),
  KEY `FK_Lote_Producto` (`idProducto`),
  CONSTRAINT `FK_Lote_Producto` FOREIGN KEY (`idProducto`) REFERENCES `PRODUCTO` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================================
-- 5. TABLA DE HISTORIAL DE PRECIOS DE PRODUCTOS (Modificado según Patrón MER 1.2.B)
-- ========================================================
DROP TABLE IF EXISTS `HISTORIAL_PRECIO`;
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
  CONSTRAINT `FK_HistorialPrecio_Producto` FOREIGN KEY (`idProducto`) REFERENCES `PRODUCTO` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================================
-- VISTA RECOMENDADA PARA CONSULTAR PRODUCTO CON STOCK CALCULADO
-- ========================================================
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


-- ==============================================================================
-- INSERCIÓN DE DATOS DE PRUEBA
-- ==============================================================================

-- 1. CATEGORIAS
INSERT INTO `CATEGORIAS` (`id`, `nombre`, `descripcion`, `codigo`, `pasilloUbicacion`, `estaActivo`, `idUsuarioAdmin`) VALUES
(1, 'Lácteos y Derivados', 'Leches, yogures, quesos y mantequillas', 'CAT-LAC', 'Pasillo 1', 1, 1),
(2, 'Bebidas y Gaseosas', 'Refrescos, jugos, aguas y energizantes', 'CAT-BEB', 'Pasillo 2', 1, 1),
(3, 'Abarrotes', 'Arroz, fideos, aceite, azúcar, sal y condimentos', 'CAT-ABA', 'Pasillo 3', 1, 1),
(4, 'Embutidos y Frial', 'Salchichas, jamones, chorizos y carnes frías', 'CAT-EMB', 'Pasillo 4', 1, 1),
(5, 'Snacks y Galletas', 'Papas fritas, pipocas, chocolates y galletas', 'CAT-SNA', 'Pasillo 5', 1, 1),
(6, 'Limpieza del Hogar', 'Detergentes, lavavajillas, desinfectantes y lavandina', 'CAT-LIM', 'Pasillo 6', 1, 1),
(7, 'Cuidado Personal', 'Jabones, champús, pasta dental y desodorantes', 'CAT-CUI', 'Pasillo 7', 1, 1),
(8, 'Panadería y Repostería', 'Pan fresco, kekes, tostadas y harinas', 'CAT-PAN', 'Pasillo 8', 1, 1);

-- 2. PROVEEDOR
INSERT INTO `PROVEEDOR` (`id`, `nombreEmpresa`, `numeroEmpresa`, `correoReferencia`, `esAutogestionado`, `estaActivo`, `idUsuarioAdmin`) VALUES
(1, 'PIL Andina S.A.', '44112233', 'ventas.cbba@pilandina.com.bo', 0, 1, 1),
(2, 'EMBOL S.A. (Coca-Cola)', '44556677', 'pedidos.cbba@embol.com.bo', 0, 1, 1),
(3, 'Sofía SHE S.A.', '44889900', 'contacto.cbba@sofia.com.bo', 1, 1, 1),
(4, 'CBN (Cervecería Boliviana Nacional)', '44224466', 'distribucion@cbn.com.bo', 0, 1, 1),
(5, 'Unilever Bolivia', '44335577', 'atencion@unilever.com', 0, 1, 1),
(6, 'Industrias Venado S.A. (Kris)', '44118899', 'ventas@venado.com.bo', 0, 1, 1),
(7, 'Arcor Bolivia', '44778811', 'pedidos@arcor.com.bo', 0, 1, 1);

-- 2.1 EMPAQUE
INSERT INTO `EMPAQUE` (`id`, `nombre`, `idUsuarioAdmin`) VALUES
(1, 'Bolsa 1L', 1),
(2, 'Botella Plástica 1kg', 1),
(3, 'Envase Plástico 200g', 1),
(4, 'Botella Plástica 2L', 1),
(5, 'Botella Vidrio 620ml', 1),
(6, 'Doypack 500g', 1),
(7, 'Doypack 400g', 1),
(8, 'Paquete 500g', 1),
(9, 'Paquete Sellado 200g', 1),
(10, 'Paquete 110g', 1),
(11, 'Display x 18 u', 1),
(12, 'Bolsa 800g', 1),
(13, 'Botella Plástica 500ml', 1),
(14, 'Barra 125g', 1),
(15, 'Tubo 90g', 1),
(16, 'Bolsa 200g', 1),
(17, 'Lata 100g', 1);

-- 3. PRODUCTO (sin columna stock)
INSERT INTO `PRODUCTO` (`id`, `nombre`, `idEmpaque`, `idCategoria`, `precioVenta`, `precioCosto`, `stockMinimo`, `estaActivo`, `idProveedor`, `idUsuarioAdmin`) VALUES
(1, 'Leche Entera PIL', 1, 1, 6.50, 5.20, 20, 1, 1, 1),
(2, 'Yogurt Frutado PIL', 2, 1, 12.00, 9.50, 10, 1, 1, 1),
(3, 'Mantequilla con Sal PIL', 3, 1, 14.50, 11.80, 5, 1, 1, 1),
(4, 'Coca-Cola Sabor Original', 4, 2, 11.00, 9.00, 25, 1, 2, 1),
(5, 'Fanta Naranja', 4, 2, 10.50, 8.50, 15, 1, 2, 1),
(6, 'Agua Vital Sin Gas', 4, 2, 6.00, 4.50, 30, 1, 2, 1),
(7, 'Cerveza Paceña', 5, 2, 12.00, 9.80, 40, 1, 4, 1),
(8, 'Mayonesa Kris', 6, 3, 14.00, 11.20, 12, 1, 6, 1),
(9, 'Ketchup Kris', 6, 3, 12.50, 10.00, 10, 1, 6, 1),
(10, 'Salsa de Tomate Kris', 7, 3, 8.00, 6.20, 15, 1, 6, 1),
(11, 'Chorizo Parrillero Sofía', 8, 4, 28.50, 23.50, 8, 1, 3, 1),
(12, 'Jamón Premium Sofía', 9, 4, 18.00, 14.50, 10, 1, 3, 1),
(13, 'Salchicha de Pollo Sofía', 8, 4, 16.50, 13.00, 10, 1, 3, 1),
(14, 'Galletas Moka Arcor', 10, 5, 4.50, 3.20, 20, 1, 7, 1),
(15, 'Bon o Bon Leche', 11, 5, 27.00, 21.50, 5, 1, 7, 1),
(16, 'Detergente OMO Multiacción', 12, 6, 15.00, 12.00, 15, 1, 5, 1),
(17, 'Lavavajillas Ola Limón', 13, 6, 8.50, 6.80, 10, 1, 5, 1),
(18, 'Jabón Lux Suave', 14, 7, 5.50, 4.10, 20, 1, 5, 1),
(19, 'Crema Dental Colgate Triple Acción', 15, 7, 9.00, 7.00, 15, 1, 5, 1),
(20, 'Tostadas Trigo PIL', 16, 8, 7.50, 5.80, 10, 1, 1, 1),
(21, 'Royal Polvo de Hornear', 17, 8, 6.00, 4.50, 15, 1, 6, 1);

-- 4. LOTE
INSERT INTO `LOTE` (`idProducto`, `codigoLote`, `cantidadInicial`, `cantidadDisponible`, `fechaVencimiento`, `estaActivo`, `idUsuarioAdmin`) VALUES
(1, 'LOT-PIL-OCT26', 70, 70, '2026-10-15', 1, 1),
(1, 'LOT-PIL-NOV26', 50, 50, '2026-11-20', 1, 1),
(2, 'LOT-YOG-SEP26', 45, 45, '2026-09-30', 1, 1),
(3, 'LOT-MAN-NOV26', 30, 30, '2026-11-20', 1, 1),
(4, 'LOT-COC-MAR27', 80, 80, '2027-03-01', 1, 1),
(5, 'LOT-FAN-FEB27', 50, 50, '2027-02-15', 1, 1),
(6, 'LOT-VIT-AGO27', 100, 100, '2027-08-10', 1, 1),
(7, 'LOT-PAC-MAY27', 150, 150, '2027-05-20', 1, 1),
(8, 'LOT-MAY-ENE27', 60, 60, '2027-01-10', 1, 1),
(9, 'LOT-KET-FEB27', 55, 55, '2027-02-28', 1, 1),
(10, 'LOT-TOM-ABR27', 70, 70, '2027-04-15', 1, 1),
(11, 'LOT-CHO-OCT26', 25, 25, '2026-10-05', 1, 1),
(12, 'LOT-JAM-SEP26', 40, 40, '2026-09-25', 1, 1),
(13, 'LOT-SAL-OCT26', 35, 35, '2026-10-12', 1, 1),
(14, 'LOT-MOK-ENE27', 90, 90, '2027-01-20', 1, 1),
(15, 'LOT-BON-MAR27', 30, 30, '2027-03-15', 1, 1),
(16, 'LOT-OMO-2026', 60, 60, NULL, 1, 1),
(17, 'LOT-OLA-2026', 40, 40, NULL, 1, 1),
(18, 'LOT-LUX-2026', 80, 80, NULL, 1, 1),
(19, 'LOT-COL-ENE28', 50, 50, '2028-01-01', 1, 1),
(20, 'LOT-TOS-NOV26', 40, 40, '2026-11-01', 1, 1),
(21, 'LOT-ROY-JUN27', 65, 65, '2027-06-30', 1, 1);

-- 5. HISTORIAL_PRECIO
INSERT INTO `HISTORIAL_PRECIO` (`idProducto`, `precioVentaAnterior`, `precioVentaNuevo`, `precioCostoAnterior`, `precioCostoNuevo`, `motivoCambio`, `idUsuario`, `fechaCambio`) VALUES
(1, 6.00, 6.50, 5.00, 5.20, 'Ajuste de precio del proveedor PIL', 1, '2026-01-15 08:30:00'),
(4, 10.50, 11.00, 8.50, 9.00, 'Incremento de tarifa de EMBOL', 1, '2026-02-01 10:00:00'),
(11, 26.00, 28.50, 21.00, 23.50, 'Ajuste de precio por temporada Sofía', 1, '2026-03-10 14:15:00');