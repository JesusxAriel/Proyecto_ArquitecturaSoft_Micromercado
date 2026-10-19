USE `bdMicroMercadoArqui`;

ALTER TABLE `VENTA` DROP CHECK `CK_Venta_montos`;

ALTER TABLE `VENTA`
  MODIFY COLUMN `efectivoPagado` DECIMAL(10,2) DEFAULT NULL,
  MODIFY COLUMN `cambio` DECIMAL(10,2) DEFAULT NULL;

ALTER TABLE `VENTA` ADD CONSTRAINT `CK_Venta_montos` CHECK (`montoTotal` >= 0);

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
    v.estado
FROM `VENTA` v
INNER JOIN `CLIENTE` c        ON v.idCliente = c.id
INNER JOIN `USUARIO` u        ON v.idUsuario = u.id
INNER JOIN `DETALLE_VENTA` dv ON v.id = dv.idVenta
INNER JOIN `PRODUCTO` p       ON dv.idProducto = p.id
INNER JOIN `EMPAQUE` e        ON p.idEmpaque = e.id
INNER JOIN `LOTE` l           ON dv.idLote = l.id;

DELIMITER $$

DROP TRIGGER IF EXISTS `trg_venta_bu`$$
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
            OR NEW.montoTotal  <> OLD.montoTotal) THEN
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

DELIMITER ;
