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

    const EFECTIVO_MAXIMO = 1000000;

    function validarEfectivo(valor) {
        const texto = String(valor ?? "").trim();
        const efectivo = Number(texto);

        if (texto === "" || !Number.isFinite(efectivo)) {
            return "Ingrese un efectivo pagado válido.";
        }

        if (efectivo < 0) {
            return "El efectivo pagado no puede ser negativo.";
        }

        if (efectivo > EFECTIVO_MAXIMO) {
            return "El efectivo pagado no puede superar Bs 1.000.000,00.";
        }

        if (Math.abs(efectivo * 100 - Math.round(efectivo * 100)) > 1e-6) {
            return "El efectivo pagado admite hasta dos decimales.";
        }

        return null;
    }

    function urlDeLogin(ubicacion) {
        const retorno = `${ubicacion?.pathname ?? "/"}${ubicacion?.search ?? ""}`;
        return `/Login?ReturnUrl=${encodeURIComponent(retorno)}&SesionVencida=true`;
    }

    async function fetchConSesion(
        url,
        opciones,
        alExpirar,
        solicitar = (...argumentos) => raiz.fetch(...argumentos),
        ubicacion = raiz.location) {
        const respuesta = await solicitar(url, opciones);
        if (respuesta.status !== 401) return respuesta;

        alExpirar?.();
        ubicacion.assign(urlDeLogin(ubicacion));
        return new Promise(() => { });
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

    function redondear(valor) {
        return Math.round((valor + Number.EPSILON) * 100) / 100;
    }

    function agregarAlDetalle(detalle, producto, unidades) {
        const stockDisponible = Number(producto.stockDisponible);
        const errorStock = validarCantidadContraStock(
            producto.nombre,
            stockDisponible,
            unidadesEnDetalle(detalle, producto.id),
            unidades);
        if (errorStock) {
            return { error: errorStock, campo: "cantidad" };
        }

        const precioUnitario = Number(producto.precioVenta);
        const existente = detalle.find((item) => item.idProducto === producto.id);
        const total = (existente?.cantidad ?? 0) + unidades;
        const subtotal = redondear(precioUnitario * total);
        if (!Number.isFinite(precioUnitario) || !Number.isFinite(subtotal)) {
            return { error: "El precio del producto no es válido. Seleccione el producto nuevamente.", campo: "producto" };
        }

        if (existente) {
            existente.cantidad = total;
            existente.subtotal = subtotal;
            existente.stockDisponible = stockDisponible;
            return { item: existente, fusionado: true, total };
        }

        const item = {
            idProducto: producto.id,
            nombre: producto.nombre,
            presentacion: producto.presentacion,
            precioUnitario,
            stockDisponible,
            cantidad: unidades,
            subtotal
        };
        detalle.push(item);
        return { item, fusionado: false, total };
    }

    function modificarCantidad(detalle, item, unidades) {
        const errorStock = validarCantidadContraStock(
            item.nombre,
            item.stockDisponible,
            unidadesEnDetalle(detalle, item.idProducto, item),
            unidades);
        if (errorStock) {
            return { error: errorStock };
        }

        const subtotal = redondear(item.precioUnitario * unidades);
        if (!Number.isFinite(subtotal)) {
            return { error: "El subtotal no es válido. Ingrese otra cantidad." };
        }

        item.cantidad = unidades;
        item.subtotal = subtotal;
        return { item };
    }

    function quitarProductoDelDetalle(detalle, idProducto) {
        let quitadas = 0;
        for (let indice = detalle.length - 1; indice >= 0; indice--) {
            if (detalle[indice].idProducto === idProducto) {
                detalle.splice(indice, 1);
                quitadas++;
            }
        }
        return quitadas;
    }

    function actualizarProductoEnResultados(productos, idProducto, precioActual, stockDisponible) {
        const producto = (productos ?? []).find((candidato) => candidato.id === idProducto);
        const precioNuevo = Number(precioActual);
        if (!producto || precioActual === null || precioActual === undefined || !Number.isFinite(precioNuevo)) {
            return false;
        }

        producto.precioVenta = precioNuevo;
        if (stockDisponible !== null && stockDisponible !== undefined && Number.isFinite(Number(stockDisponible))) {
            producto.stockDisponible = Number(stockDisponible);
        }
        return true;
    }

    function puedeConfirmarAnulacion(estado) {
        return Boolean(estado.resumenCargado)
            && !estado.anulando
            && String(estado.motivo ?? "").trim() !== "";
    }

    const LONGITUD_MINIMA_BUSQUEDA = 2;
    const MENSAJE_BUSQUEDA_CORTA = "Escriba al menos 2 caracteres para buscar.";

    function terminoDeBusquedaValido(termino) {
        return String(termino ?? "").trim().length >= LONGITUD_MINIMA_BUSQUEDA;
    }

    async function conReintento(operacion, esperaMs = 400, pausar = (ms) => new Promise((resolver) => setTimeout(resolver, ms))) {
        try {
            return await operacion();
        } catch (error) {
            if (error?.name === "AbortError") throw error;
            await pausar(esperaMs);
            return operacion();
        }
    }

    function crearEjecutorSecuencial(operacion) {
        let ejecutando = false;
        let pendiente = false;

        return async function solicitar() {
            if (ejecutando) {
                pendiente = true;
                return;
            }

            ejecutando = true;
            try {
                do {
                    pendiente = false;
                    await operacion();
                } while (pendiente);
            } finally {
                ejecutando = false;
            }
        };
    }

    const api = {
        crearEjecutorSecuencial,
        MENSAJE_BUSQUEDA_CORTA,
        terminoDeBusquedaValido,
        conReintento,
        puedeConfirmarAnulacion,
        validarCliente,
        validarEfectivo,
        urlDeLogin,
        fetchConSesion,
        unidadesEnDetalle,
        validarCantidadContraStock,
        describirLotes,
        agregarAlDetalle,
        modificarCantidad,
        quitarProductoDelDetalle,
        actualizarProductoEnResultados
    };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = api;
    } else {
        raiz.VentasReglas = api;
    }
})(typeof window !== "undefined" ? window : globalThis);
