(function (raiz) {
    "use strict";

    const LIMITE_NIT_CI = 30;
    const LIMITE_RAZON_SOCIAL = 150;

    function validarCliente(nitCi, razonSocial) {
        const errores = {};
        const nit = String(nitCi ?? "").trim();
        const razon = String(razonSocial ?? "").trim();

        if (nit === "") {
            errores.nitCi = "Ingrese el CI/NIT.";
        } else if (nit.length > LIMITE_NIT_CI) {
            errores.nitCi = `El CI/NIT admite hasta ${LIMITE_NIT_CI} caracteres.`;
        }

        if (razon === "") {
            errores.razonSocial = "Ingrese la razón social.";
        } else if (razon.length > LIMITE_RAZON_SOCIAL) {
            errores.razonSocial = `La razón social admite hasta ${LIMITE_RAZON_SOCIAL} caracteres.`;
        }

        return errores;
    }

    function unidadesEnDetalle(detalle, idProducto, itemExcluido) {
        return detalle
            .filter((item) => item.idProducto === idProducto && item !== itemExcluido)
            .reduce((total, item) => total + item.cantidad, 0);
    }

    function validarCantidadContraStock(nombre, stockDisponible, otrasUnidades, cantidadNueva) {
        if (stockDisponible === null || stockDisponible === undefined || !Number.isFinite(Number(stockDisponible))) {
            return null;
        }

        const total = otrasUnidades + cantidadNueva;
        if (total > Number(stockDisponible)) {
            return `La cantidad total de ${nombre} en el detalle (${total}) supera el stock disponible (${Number(stockDisponible)}).`;
        }

        return null;
    }

    function describirLotes(lotes) {
        return (lotes ?? []).map((lote) => `${lote.codigoLote} (${lote.cantidad})`).join(", ");
    }

    const api = { validarCliente, unidadesEnDetalle, validarCantidadContraStock, describirLotes };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = api;
    } else {
        raiz.VentasReglas = api;
    }
})(typeof window !== "undefined" ? window : globalThis);
