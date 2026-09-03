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
  `fechaActualizacion` DATETIME DEFAULT NULL,
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
  `fechaCreacion` DATE NOT NULL DEFAULT (CURRENT_DATE),
  `fechaActualizacion` DATE DEFAULT NULL,
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
  `fechaVencimiento` DATE DEFAULT NULL,
  `idProveedor` INT NOT NULL,
  `idUsuarioAdmi` INT NOT NULL,
  `fechaCreacion` DATE NOT NULL DEFAULT (CURRENT_DATE),
  `fechaActualizacion` DATE DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `FK_Producto_Proveedor` (`idProveedor`),
  CONSTRAINT `FK_Producto_Proveedor` FOREIGN KEY (`idProveedor`) REFERENCES `PROVEEDOR` (`idProveedor`) ON DELETE CASCADE ON UPDATE CASCADE
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

-- 3. Insertar PRODUCTOS
INSERT INTO `PRODUCTO` (`nombre`, `categoria`, `precio`, `stock`, `fechaVencimiento`, `idProveedor`, `idUsuarioAdmi`) VALUES

-- Lácteos y Derivados
('Leche Entera PIL 1L Bag', 'Lácteos y Derivados', 6.50, 120, '2026-10-15', 1, 1),
('Yogurt Frutado PIL 1kg', 'Lácteos y Derivados', 12.00, 45, '2026-09-30', 1, 1),
('Mantequilla con Sal PIL 200g', 'Lácteos y Derivados', 14.50, 30, '2026-11-20', 1, 1),

-- Bebidas y Gaseosas
('Coca-Cola Sabor Original 2L', 'Bebidas y Gaseosas', 11.00, 80, '2027-03-01', 2, 1),
('Fanta Naranja 2L', 'Bebidas y Gaseosas', 10.50, 50, '2027-02-15', 2, 1),
('Agua Vital Sin Gas 2L', 'Bebidas y Gaseosas', 6.00, 100, '2027-08-10', 2, 1),
('Cerveza Paceña 620ml', 'Bebidas y Gaseosas', 12.00, 150, '2027-05-20', 4, 1),

-- Abarrotes
('Mayonesa Kris Doypack 500g', 'Abarrotes', 14.00, 60, '2027-01-10', 6, 1),
('Ketchup Kris Doypack 500g', 'Abarrotes', 12.50, 55, '2027-02-28', 6, 1),
('Salsa de Tomate Kris 400g', 'Abarrotes', 8.00, 70, '2027-04-15', 6, 1),

-- Embutidos y Frial
('Chorizo Parrillero Sofía 500g', 'Embutidos y Frial', 28.50, 25, '2026-10-05', 3, 1),
('Jamón Premium Sofía 200g', 'Embutidos y Frial', 18.00, 40, '2026-09-25', 3, 1),
('Salchicha de Pollo Sofía 500g', 'Embutidos y Frial', 16.50, 35, '2026-10-12', 3, 1),

-- Snacks y Galletas
('Galletas Moka Arcor 110g', 'Snacks y Galletas', 4.50, 90, '2027-01-20', 7, 1),
('Bon o Bon Leche Display x 18 u', 'Snacks y Galletas', 27.00, 30, '2027-03-15', 7, 1),

-- Limpieza del Hogar
('Detergente OMO Multiacción 800g', 'Limpieza del Hogar', 15.00, 60, NULL, 5, 1),
('Lavavajillas Ola Limón 500ml', 'Limpieza del Hogar', 8.50, 40, NULL, 5, 1),

-- Cuidado Personal
('Jabón Lux Suave 125g', 'Cuidado Personal', 5.50, 80, NULL, 5, 1),
('Crema Dental Colgate Triple Acción 90g', 'Cuidado Personal', 9.00, 50, '2028-01-01', 5, 1),

-- Panadería y Repostería
('Tostadas Trigo PIL 200g', 'Panadería y Repostería', 7.50, 40, '2026-11-01', 1, 1),
('Royal Polvo de Hornear 100g', 'Panadería y Repostería', 6.00, 65, '2027-06-30', 6, 1);