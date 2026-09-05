DROP DATABASE IF EXISTS `bdMicroMercadoArqui`;
CREATE DATABASE `bdMicroMercadoArqui` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `bdMicroMercadoArqui`;

-- --------------------------------------------------------
-- Estructura de la tabla `CATEGORIAS`
-- --------------------------------------------------------

DROP TABLE IF EXISTS `CATEGORIAS`;
CREATE TABLE `CATEGORIAS` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `descripcion` VARCHAR(255) DEFAULT NULL,
  `codigo` VARCHAR(20) NOT NULL,
  `pasilloUbicacion` VARCHAR(20) DEFAULT NULL,
  `estado` VARCHAR(20) NOT NULL DEFAULT 'ACTIVA',
  `idUsuarioAdmin` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------
-- Estructura de la tabla `PROVEEDOR`
-- --------------------------------------------------------

DROP TABLE IF EXISTS `PROVEEDOR`;
CREATE TABLE `PROVEEDOR` (
  `idProveedor` INT NOT NULL AUTO_INCREMENT,
  `nombreEmpresa` VARCHAR(150) NOT NULL,
  `numeroEmpresa` INT NOT NULL,
  `correoReferencia` VARCHAR(150) DEFAULT NULL,
  `tipoConsigna` BOOLEAN NOT NULL DEFAULT 0,
  `idUsuarioAdmi` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`idProveedor`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------
-- Estructura de la tabla `PRODUCTO`
-- --------------------------------------------------------

DROP TABLE IF EXISTS `PRODUCTO`;
CREATE TABLE `PRODUCTO` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `nombre` VARCHAR(150) NOT NULL,
  `categoria` VARCHAR(100) NOT NULL,
  `precio` DOUBLE NOT NULL DEFAULT 0,
  `stock` INT NOT NULL DEFAULT 0,
  `idProveedor` INT NOT NULL,
  `idUsuarioAdmi` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `FK_Producto_Proveedor` (`idProveedor`),
  CONSTRAINT `FK_Producto_Proveedor` FOREIGN KEY (`idProveedor`) REFERENCES `PROVEEDOR` (`idProveedor`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------
-- Estructura de la tabla `LOTE`
-- --------------------------------------------------------

DROP TABLE IF EXISTS `LOTE`;
CREATE TABLE `LOTE` (
  `idLote` INT NOT NULL AUTO_INCREMENT,
  `idProducto` INT NOT NULL,
  `codigoLote` VARCHAR(50) NOT NULL,
  `cantidadInicial` INT NOT NULL,
  `cantidadDisponible` INT NOT NULL,
  `fechaVencimiento` DATE DEFAULT NULL,
  `estado` VARCHAR(20) NOT NULL DEFAULT 'DISPONIBLE',
  `idUsuarioAdmi` INT NOT NULL,
  `fechaCreacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `fechaActualizacion` DATETIME DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`idLote`),
  KEY `FK_Lote_Producto` (`idProducto`),
  CONSTRAINT `FK_Lote_Producto` FOREIGN KEY (`idProducto`) REFERENCES `PRODUCTO` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================================
-- INSERCIÓN DE DATOS DE PRUEBA
-- ========================================================

-- 1. Insertar CATEGORIAS
INSERT INTO `CATEGORIAS` (`nombre`, `descripcion`, `codigo`, `pasilloUbicacion`, `estado`, `idUsuarioAdmin`) VALUES
('Lácteos y Derivados', 'Leches, yogures, quesos y mantequillas', 'CAT-LAC', 'Pasillo 1', 'ACTIVA', 1),
('Bebidas y Gaseosas', 'Refrescos, jugos, aguas y energizantes', 'CAT-BEB', 'Pasillo 2', 'ACTIVA', 1),
('Abarrotes', 'Arroz, fideos, aceite, azúcar, sal y condimentos', 'CAT-ABA', 'Pasillo 3', 'ACTIVA', 1),
('Embutidos y Frial', 'Salchichas, jamones, chorizos y carnes frías', 'CAT-EMB', 'Pasillo 4', 'ACTIVA', 1),
('Snacks y Galletas', 'Papas fritas, pipocas, chocolates y galletas', 'CAT-SNA', 'Pasillo 5', 'ACTIVA', 1),
('Limpieza del Hogar', 'Detergentes, lavavajillas, desinfectantes y lavandina', 'CAT-LIM', 'Pasillo 6', 'ACTIVA', 1),
('Cuidado Personal', 'Jabones, champús, pasta dental y desodorantes', 'CAT-CUI', 'Pasillo 7', 'ACTIVA', 1),
('Panadería y Repostería', 'Pan fresco, kekes, tostadas y harinas', 'CAT-PAN', 'Pasillo 8', 'ACTIVA', 1);

-- 2. Insertar PROVEEDORES
INSERT INTO `PROVEEDOR` (`nombreEmpresa`, `numeroEmpresa`, `correoReferencia`, `tipoConsigna`, `idUsuarioAdmi`) VALUES
('PIL Andina S.A.', 44112233, 'ventas.cbba@pilandina.com.bo', 0, 1),
('EMBOL S.A. (Coca-Cola)', 44556677, 'pedidos.cbba@embol.com.bo', 0, 1),
('Sofía SHE S.A.', 44889900, 'contacto.cbba@sofia.com.bo', 1, 1),
('CBN (Cervecería Boliviana Nacional)', 44224466, 'distribucion@cbn.com.bo', 0, 1),
('Unilever Bolivia', 44335577, 'atencion@unilever.com', 0, 1),
('Industrias Venado S.A. (Kris)', 44118899, 'ventas@venado.com.bo', 0, 1),
('Arcor Bolivia', 44778811, 'pedidos@arcor.com.bo', 0, 1);

-- 3. Insertar PRODUCTOS (Stock total)
INSERT INTO `PRODUCTO` (`nombre`, `categoria`, `precio`, `stock`, `idProveedor`, `idUsuarioAdmi`) VALUES
-- Lácteos y Derivados (IDs: 1, 2, 3)
('Leche Entera PIL 1L Bag', 'Lácteos y Derivados', 6.50, 120, 1, 1),
('Yogurt Frutado PIL 1kg', 'Lácteos y Derivados', 12.00, 45, 1, 1),
('Mantequilla con Sal PIL 200g', 'Lácteos y Derivados', 14.50, 30, 1, 1),

-- Bebidas y Gaseosas (IDs: 4, 5, 6, 7)
('Coca-Cola Sabor Original 2L', 'Bebidas y Gaseosas', 11.00, 80, 2, 1),
('Fanta Naranja 2L', 'Bebidas y Gaseosas', 10.50, 50, 2, 1),
('Agua Vital Sin Gas 2L', 'Bebidas y Gaseosas', 6.00, 100, 2, 1),
('Cerveza Paceña 620ml', 'Bebidas y Gaseosas', 12.00, 150, 4, 1),

-- Abarrotes (IDs: 8, 9, 10)
('Mayonesa Kris Doypack 500g', 'Abarrotes', 14.00, 60, 6, 1),
('Ketchup Kris Doypack 500g', 'Abarrotes', 12.50, 55, 6, 1),
('Salsa de Tomate Kris 400g', 'Abarrotes', 8.00, 70, 6, 1),

-- Embutidos y Frial (IDs: 11, 12, 13)
('Chorizo Parrillero Sofía 500g', 'Embutidos y Frial', 28.50, 25, 3, 1),
('Jamón Premium Sofía 200g', 'Embutidos y Frial', 18.00, 40, 3, 1),
('Salchicha de Pollo Sofía 500g', 'Embutidos y Frial', 16.50, 35, 3, 1),

-- Snacks y Galletas (IDs: 14, 15)
('Galletas Moka Arcor 110g', 'Snacks y Galletas', 4.50, 90, 7, 1),
('Bon o Bon Leche Display x 18 u', 'Snacks y Galletas', 27.00, 30, 7, 1),

-- Limpieza del Hogar (IDs: 16, 17)
('Detergente OMO Multiacción 800g', 'Limpieza del Hogar', 15.00, 60, 5, 1),
('Lavavajillas Ola Limón 500ml', 'Limpieza del Hogar', 8.50, 40, 5, 1),

-- Cuidado Personal (IDs: 18, 19)
('Jabón Lux Suave 125g', 'Cuidado Personal', 5.50, 80, 5, 1),
('Crema Dental Colgate Triple Acción 90g', 'Cuidado Personal', 9.00, 50, 5, 1),

-- Panadería y Repostería (IDs: 20, 21)
('Tostadas Trigo PIL 200g', 'Panadería y Repostería', 7.50, 40, 1, 1),
('Royal Polvo de Hornear 100g', 'Panadería y Repostería', 6.00, 65, 6, 1);

-- 4. Insertar LOTES con sus fechas de vencimiento correspondientes
INSERT INTO `LOTE` (`idProducto`, `codigoLote`, `cantidadInicial`, `cantidadDisponible`, `fechaVencimiento`, `estado`, `idUsuarioAdmi`) VALUES
-- Leche Entera PIL (Dividida en 2 lotes)
(1, 'LOT-PIL-OCT26', 70, 70, '2026-10-15', 'DISPONIBLE', 1),
(1, 'LOT-PIL-NOV26', 50, 50, '2026-11-20', 'DISPONIBLE', 1),

-- Yogurt y Mantequilla PIL
(2, 'LOT-YOG-SEP26', 45, 45, '2026-09-30', 'DISPONIBLE', 1),
(3, 'LOT-MAN-NOV26', 30, 30, '2026-11-20', 'DISPONIBLE', 1),

-- Gaseosas y Cerveza
(4, 'LOT-COC-MAR27', 80, 80, '2027-03-01', 'DISPONIBLE', 1),
(5, 'LOT-FAN-FEB27', 50, 50, '2027-02-15', 'DISPONIBLE', 1),
(6, 'LOT-VIT-AGO27', 100, 100, '2027-08-10', 'DISPONIBLE', 1),
(7, 'LOT-PAC-MAY27', 150, 150, '2027-05-20', 'DISPONIBLE', 1),

-- Productos Kris
(8, 'LOT-MAY-ENE27', 60, 60, '2027-01-10', 'DISPONIBLE', 1),
(9, 'LOT-KET-FEB27', 55, 55, '2027-02-28', 'DISPONIBLE', 1),
(10, 'LOT-TOM-ABR27', 70, 70, '2027-04-15', 'DISPONIBLE', 1),

-- Embutidos Sofía
(11, 'LOT-CHO-OCT26', 25, 25, '2026-10-05', 'DISPONIBLE', 1),
(12, 'LOT-JAM-SEP26', 40, 40, '2026-09-25', 'DISPONIBLE', 1),
(13, 'LOT-SAL-OCT26', 35, 35, '2026-10-12', 'DISPONIBLE', 1),

-- Galletas Arcor
(14, 'LOT-MOK-ENE27', 90, 90, '2027-01-20', 'DISPONIBLE', 1),
(15, 'LOT-BON-MAR27', 30, 30, '2027-03-15', 'DISPONIBLE', 1),

-- Limpieza (Algunos artículos no vencen)
(16, 'LOT-OMO-2026', 60, 60, NULL, 'DISPONIBLE', 1),
(17, 'LOT-OLA-2026', 40, 40, NULL, 'DISPONIBLE', 1),

-- Cuidado Personal
(18, 'LOT-LUX-2026', 80, 80, NULL, 'DISPONIBLE', 1),
(19, 'LOT-COL-ENE28', 50, 50, '2028-01-01', 'DISPONIBLE', 1),

-- Repostería
(20, 'LOT-TOS-NOV26', 40, 40, '2026-11-01', 'DISPONIBLE', 1),
(21, 'LOT-ROY-JUN27', 65, 65, '2027-06-30', 'DISPONIBLE', 1);