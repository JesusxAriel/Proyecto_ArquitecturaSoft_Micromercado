(() => {
    "use strict";

    const buscador = document.getElementById("buscadorClientes");
    if (!buscador) return;

    const entrada = document.getElementById("Input_IdentificacionCliente");
    const idCliente = document.getElementById("Input_IdCliente");
    const resultados = document.getElementById("resultadosClientes");
    const estado = document.getElementById("estadoBusquedaClientes");
    const bloqueBusqueda = document.getElementById("clienteBusqueda");
    const tarjeta = document.getElementById("clienteTarjeta");
    const nombreCliente = document.getElementById("clienteNombre");
    const documentoCliente = document.getElementById("clienteDocumento");
    const cambiar = document.getElementById("cambiarCliente");
    let temporizador;
    let version = 0;

    function seleccionarCliente(cliente) {
        idCliente.value = String(cliente.id);
        nombreCliente.textContent = cliente.razonSocial;
        documentoCliente.textContent = `CI/NIT: ${cliente.nitCi}`;
        bloqueBusqueda.hidden = true;
        tarjeta.hidden = false;
        resultados.replaceChildren();
        estado.textContent = "";
        idCliente.dispatchEvent(new Event("change"));
    }

    function reiniciarCliente(enfocar) {
        clearTimeout(temporizador);
        version++;
        idCliente.value = "";
        nombreCliente.textContent = "";
        documentoCliente.textContent = "";
        tarjeta.hidden = true;
        bloqueBusqueda.hidden = false;
        entrada.value = "";
        resultados.replaceChildren();
        estado.textContent = "";
        idCliente.dispatchEvent(new Event("change"));
        if (enfocar) entrada.focus();
    }

    cambiar.addEventListener("click", () => reiniciarCliente(true));
    document.addEventListener("venta:reiniciar", () => reiniciarCliente(true));

    entrada.addEventListener("keydown", (event) => {
        if (event.key !== "Enter") return;
        event.preventDefault();
        resultados.querySelector("button[data-cliente-id]")?.click();
    });

    entrada.addEventListener("input", () => {
        clearTimeout(temporizador);
        const actual = ++version;
        const termino = entrada.value.trim();
        resultados.replaceChildren();
        resultados.setAttribute("aria-busy", "false");
        if (!VentasReglas.terminoDeBusquedaValido(termino)) {
            estado.textContent = termino ? VentasReglas.MENSAJE_BUSQUEDA_CORTA : "";
            return;
        }

        estado.textContent = "Buscando clientes…";
        temporizador = setTimeout(() => buscar(termino, actual), 300);
    });

    async function buscar(termino, actual) {
        resultados.setAttribute("aria-busy", "true");

        try {
            const url = new URL(buscador.dataset.busquedaUrl, window.location.href);
            url.searchParams.set("termino", termino);
            const clientes = await VentasReglas.conReintento(async () => {
                const respuesta = await VentasReglas.fetchConSesion(url, {
                    headers: { Accept: "application/json" },
                    cache: "no-store"
                });
                if (!respuesta.ok) throw new Error("No se pudo consultar clientes.");
                return respuesta.json();
            });
            if (actual !== version) return;

            resultados.replaceChildren();
            clientes.forEach((cliente) => {
                const fila = document.createElement("li");
                const boton = document.createElement("button");
                boton.type = "button";
                boton.className = "resultado-cliente";
                boton.dataset.clienteId = String(cliente.id);
                boton.setAttribute("aria-label", `Seleccionar ${cliente.razonSocial}, CI/NIT ${cliente.nitCi}`);
                const nombre = document.createElement("span");
                nombre.className = "resultado-principal text-break";
                nombre.textContent = cliente.razonSocial;
                const documento = document.createElement("span");
                documento.className = "resultado-secundario text-break";
                documento.textContent = `CI/NIT: ${cliente.nitCi}`;
                boton.append(nombre, documento);
                boton.addEventListener("click", () => seleccionarCliente(cliente));
                fila.append(boton);
                resultados.append(fila);
            });
            estado.textContent = clientes.length
                ? `${clientes.length} cliente(s) encontrado(s).`
                : "No se encontraron clientes activos con ese CI/NIT o razón social.";
        } catch (error) {
            if (actual !== version) return;
            resultados.replaceChildren();
            estado.textContent = "No se pudo buscar clientes. Intente nuevamente.";
        } finally {
            if (actual === version) resultados.setAttribute("aria-busy", "false");
        }
    }

    const modalCliente = document.getElementById("crearClienteModal");
    if (modalCliente) {
        const formularioCliente = document.getElementById("crearClienteForm");
        const campoNitCi = document.getElementById("crearClienteNitCi");
        const campoRazonSocial = document.getElementById("crearClienteRazonSocial");
        const errorNitCi = document.getElementById("crearClienteNitCiError");
        const errorRazonSocial = document.getElementById("crearClienteRazonSocialError");
        const errorGeneral = document.getElementById("crearClienteError");
        const guardarCliente = document.getElementById("crearClienteGuardar");
        const antiforgeryCliente = document.querySelector('input[name="__RequestVerificationToken"]');
        const modalClienteBootstrap = bootstrap.Modal.getOrCreateInstance(modalCliente);
        let guardandoCliente = false;

        function mostrarErrorCampo(campo, contenedor, mensaje) {
            contenedor.textContent = mensaje ?? "";
            contenedor.hidden = !mensaje;
            campo.classList.toggle("is-invalid", Boolean(mensaje));
            campo.setAttribute("aria-invalid", String(Boolean(mensaje)));
        }

        function mostrarErrorGeneral(mensaje) {
            errorGeneral.textContent = mensaje ?? "";
            errorGeneral.hidden = !mensaje;
        }

        function limpiarFormularioCliente() {
            formularioCliente.reset();
            mostrarErrorCampo(campoNitCi, errorNitCi, "");
            mostrarErrorCampo(campoRazonSocial, errorRazonSocial, "");
            mostrarErrorGeneral("");
        }

        formularioCliente.addEventListener("submit", async (event) => {
            event.preventDefault();
            if (guardandoCliente) return;

            mostrarErrorGeneral("");
            const errores = VentasReglas.validarCliente(campoNitCi.value, campoRazonSocial.value);
            mostrarErrorCampo(campoNitCi, errorNitCi, errores.nitCi);
            mostrarErrorCampo(campoRazonSocial, errorRazonSocial, errores.razonSocial);
            if (errores.nitCi) {
                campoNitCi.focus();
                return;
            }
            if (errores.razonSocial) {
                campoRazonSocial.focus();
                return;
            }

            guardandoCliente = true;
            guardarCliente.disabled = true;
            guardarCliente.textContent = "Guardando…";
            try {
                const respuesta = await VentasReglas.fetchConSesion(formularioCliente.dataset.crearUrl, {
                    method: "POST",
                    credentials: "same-origin",
                    headers: {
                        "Content-Type": "application/json",
                        Accept: "application/json",
                        RequestVerificationToken: antiforgeryCliente.value
                    },
                    body: JSON.stringify({
                        nitCi: campoNitCi.value.trim(),
                        razonSocial: campoRazonSocial.value.trim()
                    })
                });
                const resultado = await respuesta.json().catch(() => null);
                if (!respuesta.ok || !resultado?.exito) {
                    throw new Error(resultado?.mensaje || (respuesta.status === 400
                        ? "No se pudo validar la solicitud. Revise los datos o recargue la página si su sesión expiró."
                        : "No se pudo registrar el cliente. Intente nuevamente."));
                }

                guardandoCliente = false;
                modalClienteBootstrap.hide();
                seleccionarCliente(resultado);
            } catch (error) {
                mostrarErrorGeneral(error instanceof TypeError
                    ? "No se pudo conectar con el servidor. El cliente no fue registrado."
                    : error.message);
            } finally {
                guardandoCliente = false;
                guardarCliente.disabled = false;
                guardarCliente.textContent = "Guardar cliente";
            }
        });

        modalCliente.addEventListener("shown.bs.modal", () => campoNitCi.focus());
        modalCliente.addEventListener("hide.bs.modal", (event) => {
            if (guardandoCliente) event.preventDefault();
        });
        modalCliente.addEventListener("hidden.bs.modal", limpiarFormularioCliente);
    }
})();

(() => {
    "use strict";

    const buscador = document.getElementById("buscadorProductos");
    if (!buscador) return;

    const MARCADOR_SIN_CLIENTE = "Seleccione un cliente primero";
    const MARCADOR_CON_CLIENTE = "Buscar producto por nombre o presentación";
    const precio = new Intl.NumberFormat("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    const entrada = document.getElementById("Input_BusquedaProducto");
    const resultados = document.getElementById("resultadosProductos");
    const estado = document.getElementById("estadoBusquedaProductos");
    const avisoDetalle = document.getElementById("avisoDetalle");
    const tablaDetalle = document.getElementById("detalleTemporal");
    const unidadesTotales = document.getElementById("unidadesTotales");
    const totalGeneral = document.getElementById("totalGeneral");
    const idCliente = document.getElementById("Input_IdCliente");
    const efectivoPagado = document.getElementById("efectivoPagado");
    const saldoEfectivo = document.getElementById("saldoEfectivo");
    const estadoVenta = document.getElementById("estadoVenta");
    const confirmarVenta = document.getElementById("confirmarVenta");
    const antiforgery = document.querySelector('input[name="__RequestVerificationToken"]');

    const detalleTemporal = [];
    const filas = new Map();
    let listadoVisible = { productos: [], hayMas: false };
    let temporizador;
    let version = 0;
    let registrandoVenta = false;

    function hayCliente() {
        return idCliente.value.trim() !== "";
    }

    function mostrarAviso(mensaje, advertencia = false) {
        avisoDetalle.textContent = mensaje;
        avisoDetalle.className = `small mt-2 mb-0 ${advertencia ? "text-warning-emphasis" : "text-body-secondary"}`;
    }

    function mostrarEstadoVenta(mensaje, tipo = "info") {
        estadoVenta.textContent = mensaje;
        estadoVenta.className = `alert alert-${tipo} py-2 px-3 small mb-3`;
        estadoVenta.setAttribute("role", tipo === "danger" ? "alert" : "status");
        estadoVenta.hidden = !mensaje;
    }

    function ventaLista() {
        return hayCliente() && detalleTemporal.length > 0;
    }

    function actualizarBotonConfirmar() {
        confirmarVenta.disabled = registrandoVenta || !ventaLista();
    }

    function actualizarSaldo() {
        const texto = efectivoPagado.value.trim();
        saldoEfectivo.className = "small mb-0";
        if (texto === "") {
            saldoEfectivo.textContent = "";
            return;
        }

        const error = VentasReglas.validarEfectivo(texto);
        if (error) {
            saldoEfectivo.textContent = error;
            saldoEfectivo.classList.add("text-danger");
            return;
        }

        const { totalCentavos } = VentasReglas.resumirDetalle(detalleTemporal);
        const saldo = VentasReglas.saldoDeEfectivo(totalCentavos, Number(texto));
        if (saldo.tipo === "cambio") {
            saldoEfectivo.textContent = `Cambio: Bs ${precio.format(saldo.centavos / 100)}`;
            saldoEfectivo.classList.add("text-success");
        } else {
            saldoEfectivo.textContent = `Falta: Bs ${precio.format(saldo.centavos / 100)}`;
            saldoEfectivo.classList.add("text-danger");
        }
    }

    function actualizarResumen() {
        const resumen = VentasReglas.resumirDetalle(detalleTemporal);
        unidadesTotales.textContent = String(resumen.unidades);
        totalGeneral.textContent = precio.format(resumen.totalCentavos / 100);
        actualizarSaldo();
        actualizarBotonConfirmar();
    }

    function habilitarBusqueda() {
        const conCliente = hayCliente();
        entrada.disabled = !conCliente;
        entrada.placeholder = conCliente ? MARCADOR_CON_CLIENTE : MARCADOR_SIN_CLIENTE;
        if (conCliente) return;

        limpiarBusqueda();
    }

    function limpiarBusqueda() {
        clearTimeout(temporizador);
        version++;
        entrada.value = "";
        resultados.replaceChildren();
        resultados.setAttribute("aria-busy", "false");
        estado.textContent = "";
        listadoVisible = { productos: [], hayMas: false };
    }

    idCliente.addEventListener("change", () => {
        mostrarEstadoVenta("");
        habilitarBusqueda();
        actualizarBotonConfirmar();
        if (hayCliente()) entrada.focus();
    });

    efectivoPagado.addEventListener("input", actualizarSaldo);

    function mostrarErrorFila(referencias, mensaje) {
        referencias.error.textContent = mensaje;
        referencias.error.hidden = !mensaje;
        referencias.entrada.classList.toggle("is-invalid", Boolean(mensaje));
        referencias.entrada.setAttribute("aria-invalid", String(Boolean(mensaje)));
    }

    function actualizarFila(item) {
        const referencias = filas.get(item);
        if (!referencias) return;

        referencias.entrada.value = String(item.cantidad);
        referencias.subtotal.textContent = precio.format(item.subtotal);
        referencias.menos.disabled = !VentasReglas.puedeDisminuir(item);
        referencias.mas.disabled = !VentasReglas.puedeAumentar(item);
        mostrarErrorFila(referencias, "");
    }

    function aplicarCambio(item, resultado) {
        const referencias = filas.get(item);
        if (resultado.error) {
            mostrarErrorFila(referencias, resultado.error);
            return false;
        }

        actualizarFila(item);
        actualizarResumen();
        mostrarEstadoVenta("");
        return true;
    }

    function crearFila(item) {
        const fila = document.createElement("tr");

        const celdaProducto = document.createElement("td");
        celdaProducto.className = "text-break";
        const nombre = document.createElement("div");
        nombre.className = "fw-semibold";
        nombre.textContent = item.nombre;
        const presentacion = document.createElement("div");
        presentacion.className = "small text-body-secondary";
        presentacion.textContent = item.presentacion;
        celdaProducto.append(nombre, presentacion);

        const celdaPrecio = document.createElement("td");
        celdaPrecio.className = "text-end text-nowrap";
        celdaPrecio.textContent = precio.format(item.precioUnitario);

        const celdaCantidad = document.createElement("td");
        const control = document.createElement("div");
        control.className = "input-group input-group-sm control-cantidad";
        const menos = document.createElement("button");
        menos.type = "button";
        menos.className = "btn btn-outline-secondary";
        menos.setAttribute("aria-label", `Disminuir la cantidad de ${item.nombre}`);
        menos.innerHTML = '<i class="bi bi-dash-lg" aria-hidden="true"></i>';
        const cantidad = document.createElement("input");
        cantidad.type = "number";
        cantidad.min = "1";
        cantidad.step = "1";
        cantidad.inputMode = "numeric";
        cantidad.className = "form-control text-center";
        cantidad.setAttribute("aria-label", `Cantidad de ${item.nombre}`);
        const mas = document.createElement("button");
        mas.type = "button";
        mas.className = "btn btn-outline-secondary";
        mas.setAttribute("aria-label", `Aumentar la cantidad de ${item.nombre}`);
        mas.innerHTML = '<i class="bi bi-plus-lg" aria-hidden="true"></i>';
        control.append(menos, cantidad, mas);
        const error = document.createElement("div");
        error.className = "text-danger small mt-1";
        error.setAttribute("role", "alert");
        error.hidden = true;
        celdaCantidad.append(control, error);

        const celdaSubtotal = document.createElement("td");
        celdaSubtotal.className = "text-end text-nowrap fw-semibold";

        const celdaQuitar = document.createElement("td");
        celdaQuitar.className = "text-end";
        const quitar = document.createElement("button");
        quitar.type = "button";
        quitar.className = "btn btn-outline-danger btn-sm";
        quitar.setAttribute("aria-label", `Quitar ${item.nombre} del detalle`);
        quitar.innerHTML = '<i class="bi bi-trash" aria-hidden="true"></i>';
        celdaQuitar.append(quitar);

        fila.append(celdaProducto, celdaPrecio, celdaCantidad, celdaSubtotal, celdaQuitar);
        filas.set(item, { entrada: cantidad, menos, mas, error, subtotal: celdaSubtotal });
        actualizarFila(item);

        menos.addEventListener("click", () => aplicarCambio(item, VentasReglas.ajustarCantidad(detalleTemporal, item, -1)));
        mas.addEventListener("click", () => aplicarCambio(item, VentasReglas.ajustarCantidad(detalleTemporal, item, 1)));
        cantidad.addEventListener("input", () => {
            if (cantidad.value.trim() === "") {
                mostrarErrorFila(filas.get(item), "");
                return;
            }
            aplicarCambio(item, VentasReglas.cambiarCantidad(detalleTemporal, item, cantidad.value));
        });
        cantidad.addEventListener("change", () => {
            const resultado = VentasReglas.cambiarCantidad(detalleTemporal, item, cantidad.value);
            if (resultado.error) {
                actualizarFila(item);
                mostrarErrorFila(filas.get(item), resultado.error);
                return;
            }
            aplicarCambio(item, resultado);
        });
        cantidad.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                entrada.focus();
            }
        });
        quitar.addEventListener("click", () => {
            const indice = detalleTemporal.indexOf(item);
            if (indice < 0) return;
            detalleTemporal.splice(indice, 1);
            renderizarDetalle();
            mostrarAviso(`${item.nombre} se quitó del detalle.`);
            (tablaDetalle.querySelector("button[aria-label^='Quitar']") ?? entrada).focus();
        });

        return fila;
    }

    function renderizarDetalle() {
        tablaDetalle.replaceChildren();
        filas.clear();
        if (detalleTemporal.length === 0) {
            const fila = document.createElement("tr");
            const celda = document.createElement("td");
            celda.colSpan = 5;
            celda.className = "text-center text-body-secondary py-4";
            celda.textContent = "Aún no hay productos";
            fila.append(celda);
            tablaDetalle.append(fila);
        } else {
            detalleTemporal.forEach((item) => tablaDetalle.append(crearFila(item)));
        }
        actualizarResumen();
    }

    function agregarProducto(producto) {
        const resultado = VentasReglas.agregarConUnClic(detalleTemporal, producto);
        if (resultado.error) {
            mostrarAviso(resultado.error, true);
            entrada.focus();
            return;
        }

        mostrarEstadoVenta("");
        if (resultado.sinCambios) {
            mostrarAviso(resultado.aviso, true);
            entrada.focus();
            return;
        }

        if (resultado.fusionado) {
            actualizarFila(resultado.item);
            actualizarResumen();
        } else {
            renderizarDetalle();
        }

        mostrarAviso(resultado.aviso ?? `Se agregó ${resultado.item.nombre} (${resultado.total}).`, Boolean(resultado.tope));
        limpiarBusqueda();
        entrada.focus();
    }

    function pintarResultados(listado) {
        listadoVisible = listado;
        resultados.replaceChildren();
        listado.productos.forEach((producto) => {
            const sinStock = Number(producto.stockDisponible) <= 0;
            const fila = document.createElement("li");
            const boton = document.createElement("button");
            boton.type = "button";
            boton.className = "resultado-producto";
            boton.dataset.productoId = String(producto.id);
            boton.disabled = sinStock;
            boton.setAttribute("aria-label", sinStock
                ? `${producto.nombre}, ${producto.presentacion}, sin stock`
                : `Agregar ${producto.nombre}, ${producto.presentacion}, Bs ${precio.format(producto.precioVenta)}, stock ${producto.stockDisponible}`);

            const nombre = document.createElement("span");
            nombre.className = "resultado-principal text-break";
            nombre.textContent = producto.nombre;
            const presentacion = document.createElement("span");
            presentacion.className = "resultado-secundario text-break";
            presentacion.textContent = producto.presentacion;
            const valor = document.createElement("span");
            valor.className = "resultado-precio";
            valor.textContent = `Bs ${precio.format(producto.precioVenta)}`;
            const stock = document.createElement("span");
            stock.className = sinStock ? "badge text-bg-secondary" : "badge text-bg-light border";
            stock.textContent = sinStock ? "Sin stock" : `Stock ${producto.stockDisponible}`;
            boton.append(nombre, presentacion, valor, stock);

            boton.addEventListener("click", () => agregarProducto(producto));
            fila.append(boton);
            resultados.append(fila);
        });
        estado.textContent = listado.productos.length
            ? listado.hayMas
                ? `Mostrando los primeros ${listado.productos.length} productos. Afine la búsqueda para ver otras coincidencias.`
                : `${listado.productos.length} producto(s) encontrado(s).`
            : "No se encontraron productos con ese término.";
    }

    async function buscar(termino, actual) {
        resultados.setAttribute("aria-busy", "true");

        try {
            const url = new URL(buscador.dataset.busquedaUrl, window.location.href);
            url.searchParams.set("termino", termino);
            const listado = await VentasReglas.conReintento(async () => {
                const respuesta = await VentasReglas.fetchConSesion(url, {
                    headers: { Accept: "application/json" },
                    cache: "no-store"
                });
                if (!respuesta.ok) throw new Error("No se pudo consultar productos.");
                return respuesta.json();
            });
            if (actual !== version) return;

            pintarResultados(listado);
        } catch (error) {
            if (actual !== version) return;
            resultados.replaceChildren();
            estado.textContent = "No se pudo buscar productos. Intente nuevamente.";
        } finally {
            if (actual === version) resultados.setAttribute("aria-busy", "false");
        }
    }

    entrada.addEventListener("keydown", (event) => {
        if (event.key !== "Enter") return;
        event.preventDefault();
        const primero = listadoVisible.productos.find((producto) => Number(producto.stockDisponible) > 0);
        if (primero) agregarProducto(primero);
    });

    entrada.addEventListener("input", () => {
        clearTimeout(temporizador);
        const actual = ++version;
        const termino = entrada.value.trim();
        resultados.replaceChildren();
        resultados.setAttribute("aria-busy", "false");
        listadoVisible = { productos: [], hayMas: false };
        if (!VentasReglas.terminoDeBusquedaValido(termino)) {
            estado.textContent = termino ? VentasReglas.MENSAJE_BUSQUEDA_CORTA : "";
            return;
        }

        estado.textContent = "Buscando productos…";
        temporizador = setTimeout(() => buscar(termino, actual), 300);
    });

    async function consultarProducto(referencia) {
        const url = new URL(buscador.dataset.busquedaUrl, window.location.href);
        url.searchParams.set("termino", referencia.nombre);
        const respuesta = await VentasReglas.fetchConSesion(url, { headers: { Accept: "application/json" }, cache: "no-store" });
        if (!respuesta.ok) return null;
        const listado = await respuesta.json();
        return listado.productos.find((candidato) => candidato.id === referencia.id) ?? null;
    }

    async function refrescarStocks() {
        const termino = entrada.value.trim();
        if (VentasReglas.terminoDeBusquedaValido(termino)) buscar(termino, ++version);

        for (const item of [...detalleTemporal]) {
            try {
                const actualizado = await consultarProducto({ id: item.idProducto, nombre: item.nombre });
                if (!actualizado) continue;

                item.stockDisponible = Number(actualizado.stockDisponible);
                actualizarFila(item);
            } catch {
                continue;
            }
        }
    }

    document.addEventListener("venta:anulada", refrescarStocks);

    function reiniciarVenta() {
        detalleTemporal.length = 0;
        efectivoPagado.value = "";
        limpiarBusqueda();
        renderizarDetalle();
        mostrarAviso("");
        document.dispatchEvent(new CustomEvent("venta:reiniciar"));
    }

    function atenderPrecioCambiado(resultado) {
        VentasReglas.quitarProductoDelDetalle(detalleTemporal, resultado.idProducto);
        renderizarDetalle();
        VentasReglas.actualizarProductoEnResultados(
            listadoVisible.productos, resultado.idProducto, resultado.precioActual, resultado.stockDisponible);
        const terminoVigente = entrada.value.trim();
        if (VentasReglas.terminoDeBusquedaValido(terminoVigente)) {
            pintarResultados(listadoVisible);
            buscar(terminoVigente, ++version);
        }
    }

    confirmarVenta.addEventListener("click", async (event) => {
        event.preventDefault();
        if (registrandoVenta) return;

        const cliente = Number(idCliente.value);
        if (!Number.isInteger(cliente) || cliente <= 0 || cliente > 2147483647) {
            mostrarEstadoVenta("Seleccione un cliente para registrar la venta.", "danger");
            return;
        }
        if (detalleTemporal.length === 0 || detalleTemporal.some(item =>
            !Number.isInteger(item.idProducto) || item.idProducto <= 0 || item.idProducto > 2147483647 ||
            !Number.isInteger(item.cantidad) || item.cantidad <= 0 || item.cantidad > 2147483647)) {
            mostrarEstadoVenta("Agregue al menos un producto y revise que las cantidades sean enteras mayores a cero.", "danger");
            return;
        }
        const errorEfectivo = VentasReglas.validarEfectivo(efectivoPagado.value);
        if (errorEfectivo) {
            mostrarEstadoVenta(errorEfectivo, "danger");
            efectivoPagado.focus();
            return;
        }

        const payload = {
            idCliente: cliente,
            efectivoPagado: Number(efectivoPagado.value),
            lineas: detalleTemporal.map(item => ({ idProducto: item.idProducto, cantidad: item.cantidad, precioEsperado: item.precioUnitario }))
        };
        registrandoVenta = true;
        confirmarVenta.disabled = true;
        confirmarVenta.textContent = "Registrando venta…";
        mostrarEstadoVenta("Registrando venta…");
        const ventanaComprobante = window.open("", "_blank");
        try {
            const respuesta = await VentasReglas.fetchConSesion(confirmarVenta.dataset.registroUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    Accept: "application/json",
                    RequestVerificationToken: antiforgery.value
                },
                body: JSON.stringify(payload)
            }, () => ventanaComprobante?.close());
            const resultado = await respuesta.json().catch(() => null);
            if (resultado?.codigo === "precio-cambiado") {
                atenderPrecioCambiado(resultado);
            }
            if (!respuesta.ok || !resultado?.exito) {
                throw new Error(resultado?.mensaje || (respuesta.status === 400
                    ? "No se pudo validar la solicitud. Revise los datos o recargue la página si su sesión expiró."
                    : "No se pudo registrar la venta. Revise su sesión e intente nuevamente."));
            }

            let mensajeExito = "Venta registrada correctamente.";
            if (resultado.idVenta != null) {
                mensajeExito += ` N.º ${resultado.idVenta}.`;
                if (ventanaComprobante) {
                    ventanaComprobante.location.href = confirmarVenta.dataset.comprobanteBase + resultado.idVenta;
                } else {
                    mensajeExito += " Habilite las ventanas emergentes para ver el comprobante.";
                }
            } else {
                ventanaComprobante?.close();
            }
            if (resultado.total != null && Number.isFinite(Number(resultado.total))) {
                mensajeExito += ` Total: Bs ${precio.format(Number(resultado.total))}.`;
            }
            reiniciarVenta();
            mostrarEstadoVenta(mensajeExito, "success");
            document.dispatchEvent(new CustomEvent("venta:registrada"));
        } catch (error) {
            ventanaComprobante?.close();
            mostrarEstadoVenta(error instanceof TypeError
                ? "No se pudo conectar con el servidor. La venta se conserva."
                : error.message, "danger");
        } finally {
            registrandoVenta = false;
            confirmarVenta.textContent = "Confirmar venta";
            actualizarBotonConfirmar();
        }
    });

    habilitarBusqueda();
    renderizarDetalle();
})();

(() => {
    "use strict";

    const seccion = document.getElementById("historialVentasSeccion");
    if (!seccion) return;

    const cuerpo = document.getElementById("historialVentas");
    const estado = document.getElementById("estadoHistorial");
    const actualizar = document.getElementById("actualizarHistorial");
    const mostrarAnuladas = document.getElementById("mostrarAnuladas");
    const puedeAnular = seccion.dataset.puedeAnular === "true";
    const precio = new Intl.NumberFormat("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const modalDetalle = document.getElementById("detalleVentaModal");
    const detalleError = document.getElementById("detalleVentaError");
    const detalleComprobante = document.getElementById("detalleVentaComprobante");
    const detalleCliente = document.getElementById("detalleVentaCliente");
    const detalleFecha = document.getElementById("detalleVentaFecha");
    const detalleUsuario = document.getElementById("detalleVentaUsuario");
    const detalleEstado = document.getElementById("detalleVentaEstado");
    const detalleProductos = document.getElementById("detalleVentaProductos");
    const detalleTotal = document.getElementById("detalleVentaTotal");
    const detalleEfectivo = document.getElementById("detalleVentaEfectivo");
    const detalleCambio = document.getElementById("detalleVentaCambio");
    const detalleAnulacion = document.getElementById("detalleVentaAnulacion");
    const detalleMotivo = document.getElementById("detalleVentaMotivo");
    const detalleUsuarioAnulacion = document.getElementById("detalleVentaUsuarioAnulacion");
    const detalleFechaAnulacion = document.getElementById("detalleVentaFechaAnulacion");
    const detalleModal = bootstrap.Modal.getOrCreateInstance(modalDetalle);
    const modalAnulacion = document.getElementById("anularVentaModal");
    const anulacionCarga = document.getElementById("anularVentaCarga");
    const anulacionErrorDetalle = document.getElementById("anularVentaErrorDetalle");
    const anulacionResumen = document.getElementById("anularVentaResumen");
    const anulacionComprobante = document.getElementById("anularVentaComprobante");
    const anulacionFecha = document.getElementById("anularVentaFecha");
    const anulacionCliente = document.getElementById("anularVentaCliente");
    const anulacionUsuario = document.getElementById("anularVentaUsuario");
    const anulacionProductos = document.getElementById("anularVentaProductos");
    const anulacionTotal = document.getElementById("anularVentaTotal");
    const anulacionMotivo = document.getElementById("anularVentaMotivo");
    const anulacionError = document.getElementById("anularVentaError");
    const confirmarAnulacion = document.getElementById("confirmarAnulacionVenta");
    const anulacionModal = bootstrap.Modal.getOrCreateInstance(modalAnulacion);
    const aviso = document.getElementById("avisoHistorial");
    const antiforgery = document.querySelector('input[name="__RequestVerificationToken"]');
    const ventasPorId = new Map();
    let versionDetalle = 0;
    let ventaEnAnulacion = null;
    let resumenAnulacionCargado = false;
    let versionAnulacion = 0;
    let anulando = false;

    function dosDigitos(numero) {
        return String(numero).padStart(2, "0");
    }

    function formatearFecha(valor, conSegundos = false) {
        const fecha = new Date(valor);
        if (Number.isNaN(fecha.getTime())) return "";
        const segundos = conSegundos ? `:${dosDigitos(fecha.getSeconds())}` : "";
        return `${dosDigitos(fecha.getDate())}/${dosDigitos(fecha.getMonth() + 1)}/${fecha.getFullYear()} ${dosDigitos(fecha.getHours())}:${dosDigitos(fecha.getMinutes())}${segundos}`;
    }

    function crearBadgeEstado(valor) {
        const anulada = valor === "ANULADA";
        const badge = document.createElement("span");
        badge.className = `badge bg-${anulada ? "danger" : "success"}`;
        badge.textContent = anulada ? "Anulada" : "Completada";
        return badge;
    }

    function crearFilaMensaje(mensaje, icono) {
        const fila = document.createElement("tr");
        const celda = document.createElement("td");
        celda.colSpan = 6;
        celda.className = "text-center py-4";
        const marca = document.createElement("i");
        marca.className = `bi ${icono} d-block fs-2 text-muted mb-2`;
        marca.setAttribute("aria-hidden", "true");
        const texto = document.createElement("p");
        texto.className = "text-muted mb-0";
        texto.textContent = mensaje;
        celda.append(marca, texto);
        fila.append(celda);
        return fila;
    }

    function crearBotonAccion(texto, accion, venta, clase) {
        const boton = document.createElement("button");
        boton.type = "button";
        boton.className = `btn ${clase} btn-sm`;
        boton.textContent = texto;
        boton.dataset.accion = accion;
        boton.dataset.idVenta = String(venta.id);
        boton.setAttribute("aria-label", accion === "anular"
            ? `Anular la venta ${venta.nroComprobante}`
            : `Ver detalle de la venta ${venta.nroComprobante}`);
        return boton;
    }

    function renderizar(ventas) {
        cuerpo.replaceChildren();
        ventasPorId.clear();
        ventas.forEach((venta) => ventasPorId.set(venta.id, venta));
        if (ventas.length === 0) {
            cuerpo.append(crearFilaMensaje(
                mostrarAnuladas.checked
                    ? "No hay ventas registradas."
                    : "No hay ventas completadas para mostrar.",
                "bi-receipt"));
            return;
        }

        ventas.forEach((venta) => {
            const fila = document.createElement("tr");
            [
                [venta.nroComprobante, "text-nowrap"],
                [formatearFecha(venta.fechaHora), "text-nowrap"],
                [venta.nombreCliente, "text-break"]
            ].forEach(([valor, clase]) => {
                const celda = document.createElement("td");
                celda.className = clase;
                celda.textContent = valor;
                fila.append(celda);
            });

            const celdaEstado = document.createElement("td");
            celdaEstado.append(crearBadgeEstado(venta.estado));

            const celdaTotal = document.createElement("td");
            celdaTotal.className = "text-end text-nowrap";
            celdaTotal.textContent = `Bs ${precio.format(venta.total)}`;

            const acciones = document.createElement("td");
            acciones.className = "text-end text-nowrap";
            acciones.append(crearBotonAccion("Ver detalle", "detalle", venta, "btn-outline-primary"));
            if (puedeAnular && venta.estado === "COMPLETADA") {
                acciones.append(" ", crearBotonAccion("Anular", "anular", venta, "btn-outline-danger"));
            }

            fila.append(celdaEstado, celdaTotal, acciones);
            cuerpo.append(fila);
        });
    }

    async function consultarHistorial() {
        cuerpo.setAttribute("aria-busy", "true");
        actualizar.disabled = true;
        estado.textContent = "Cargando historial…";

        try {
            const url = new URL(seccion.dataset.historialUrl, window.location.href);
            url.searchParams.set("incluirAnuladas", String(mostrarAnuladas.checked));
            const respuesta = await VentasReglas.fetchConSesion(url, {
                headers: { Accept: "application/json" },
                cache: "no-store"
            });
            if (!respuesta.ok) throw new Error("No se pudo cargar el historial de ventas.");
            const datos = await respuesta.json();

            renderizar(datos.ventas);
            estado.textContent = datos.ventas.length
                ? `${datos.ventas.length} venta(s) en el historial.`
                : "No hay ventas para mostrar.";
        } catch {
            cuerpo.replaceChildren(crearFilaMensaje(
                "No se pudo cargar el historial de ventas. Intente nuevamente.",
                "bi-exclamation-triangle"));
            estado.textContent = "No se pudo cargar el historial de ventas.";
        } finally {
            cuerpo.setAttribute("aria-busy", "false");
            actualizar.disabled = false;
        }
    }

    const cargarHistorial = VentasReglas.crearEjecutorSecuencial(consultarHistorial);

    function crearFilaDetalle(mensaje) {
        const fila = document.createElement("tr");
        const celda = document.createElement("td");
        celda.colSpan = 5;
        celda.className = "text-center text-muted py-4";
        celda.textContent = mensaje;
        fila.append(celda);
        return fila;
    }

    function llenarLineas(cuerpoTabla, lineas) {
        cuerpoTabla.replaceChildren();
        lineas.forEach((linea) => {
            const fila = document.createElement("tr");
            [
                [String(linea.cantidad), "text-center"],
                [linea.descripcion, "text-break"],
                [VentasReglas.describirLotes(linea.lotes), "text-break"],
                [precio.format(linea.precioUnitario), "text-end text-nowrap"],
                [precio.format(linea.subtotal), "text-end text-nowrap"]
            ].forEach(([valor, clase]) => {
                const celda = document.createElement("td");
                celda.className = clase;
                celda.textContent = valor;
                fila.append(celda);
            });
            cuerpoTabla.append(fila);
        });
    }

    function limpiarDetalle() {
        detalleError.textContent = "";
        detalleError.hidden = true;
        detalleAnulacion.hidden = true;
        [detalleComprobante, detalleCliente, detalleFecha, detalleUsuario, detalleEfectivo, detalleCambio]
            .forEach((elemento) => { elemento.textContent = "Cargando…"; });
        detalleTotal.textContent = "Cargando…";
        detalleTotal.className = "fw-semibold text-muted";
        detalleEstado.textContent = "Cargando…";
        detalleEstado.className = "badge bg-secondary";
        detalleProductos.replaceChildren(crearFilaDetalle("Cargando detalle…"));
    }

    function mostrarErrorDetalle(mensaje) {
        limpiarDetalle();
        [detalleComprobante, detalleCliente, detalleFecha, detalleUsuario, detalleEfectivo, detalleCambio, detalleTotal]
            .forEach((elemento) => { elemento.textContent = "—"; });
        detalleEstado.textContent = "—";
        detalleProductos.replaceChildren(crearFilaDetalle("No hay productos para mostrar."));
        detalleError.textContent = mensaje;
        detalleError.hidden = false;
    }

    function mostrarDetalle(detalle) {
        const anulada = detalle.estado === "ANULADA";
        detalleError.hidden = true;
        detalleComprobante.textContent = detalle.nroComprobante;
        detalleCliente.textContent = `${detalle.razonSocialCliente} (CI/NIT: ${detalle.nitCiCliente})`;
        detalleFecha.textContent = formatearFecha(detalle.fechaHora, true);
        detalleUsuario.textContent = detalle.usuarioCajero;
        detalleEstado.textContent = anulada ? "Anulada" : "Completada";
        detalleEstado.className = `badge bg-${anulada ? "danger" : "success"}`;
        detalleTotal.textContent = precio.format(detalle.total);
        detalleTotal.className = "fw-semibold";
        detalleEfectivo.textContent = precio.format(detalle.efectivoPagado);
        detalleCambio.textContent = precio.format(detalle.cambio);

        llenarLineas(detalleProductos, detalle.lineas);

        detalleAnulacion.hidden = !anulada;
        if (anulada) {
            detalleMotivo.textContent = detalle.motivoAnulacion ?? "";
            detalleUsuarioAnulacion.textContent = detalle.usuarioAnulacion ?? "";
            detalleFechaAnulacion.textContent = detalle.fechaAnulacion ? formatearFecha(detalle.fechaAnulacion, true) : "";
        }
    }

    async function abrirDetalle(idVenta) {
        const actual = ++versionDetalle;
        limpiarDetalle();
        detalleModal.show();

        try {
            const url = new URL(seccion.dataset.detalleUrl, window.location.href);
            url.searchParams.set("idVenta", String(idVenta));
            const respuesta = await VentasReglas.fetchConSesion(url, {
                headers: { Accept: "application/json" },
                cache: "no-store"
            });
            const datos = await respuesta.json().catch(() => null);
            if (actual !== versionDetalle) return;
            if (!respuesta.ok) {
                throw new Error(datos?.mensaje || "No se pudo cargar el detalle de la venta. Intente nuevamente.");
            }

            mostrarDetalle(datos);
        } catch (error) {
            if (actual !== versionDetalle) return;
            mostrarErrorDetalle(error instanceof TypeError
                ? "No se pudo conectar con el servidor. Intente nuevamente."
                : error.message);
        }
    }

    modalDetalle.addEventListener("hidden.bs.modal", () => {
        versionDetalle++;
    });

    cuerpo.addEventListener("click", (event) => {
        const boton = event.target.closest("button[data-accion]");
        if (!boton) return;

        const idVenta = Number(boton.dataset.idVenta);
        if (boton.dataset.accion === "detalle") abrirDetalle(idVenta);
        if (boton.dataset.accion === "anular") abrirAnulacion(idVenta);
    });

    function mostrarAviso(mensaje) {
        aviso.textContent = mensaje;
        aviso.hidden = false;
    }

    function mostrarErrorAnulacion(mensaje) {
        anulacionError.textContent = mensaje;
        anulacionError.hidden = !mensaje;
    }

    function actualizarBotonAnulacion() {
        confirmarAnulacion.disabled = !VentasReglas.puedeConfirmarAnulacion({
            resumenCargado: resumenAnulacionCargado,
            anulando,
            motivo: anulacionMotivo.value
        });
    }

    function mostrarResumenAnulacion(datos) {
        anulacionComprobante.textContent = datos.nroComprobante;
        anulacionFecha.textContent = formatearFecha(datos.fechaHora, true);
        anulacionCliente.textContent = `${datos.razonSocialCliente} (CI/NIT: ${datos.nitCiCliente})`;
        anulacionUsuario.textContent = datos.usuarioCajero;
        llenarLineas(anulacionProductos, datos.lineas);
        anulacionTotal.textContent = precio.format(datos.total);
        anulacionResumen.hidden = false;
    }

    async function abrirAnulacion(idVenta) {
        const venta = ventasPorId.get(idVenta);
        if (!venta || !puedeAnular) return;

        const actual = ++versionAnulacion;
        ventaEnAnulacion = venta;
        resumenAnulacionCargado = false;
        aviso.hidden = true;
        anulacionMotivo.value = "";
        mostrarErrorAnulacion("");
        anulacionCarga.hidden = false;
        anulacionErrorDetalle.hidden = true;
        anulacionResumen.hidden = true;
        actualizarBotonAnulacion();
        anulacionModal.show();

        try {
            const url = new URL(seccion.dataset.detalleUrl, window.location.href);
            url.searchParams.set("idVenta", String(idVenta));
            const respuesta = await VentasReglas.fetchConSesion(url, {
                headers: { Accept: "application/json" },
                cache: "no-store"
            });
            const datos = await respuesta.json().catch(() => null);
            if (actual !== versionAnulacion) return;
            if (!respuesta.ok) {
                throw new Error(datos?.mensaje || "No se pudieron cargar los datos de la venta. Intente nuevamente.");
            }
            if (datos.estado !== "COMPLETADA") {
                throw new Error("La venta ya fue anulada.");
            }

            mostrarResumenAnulacion(datos);
            resumenAnulacionCargado = true;
        } catch (error) {
            if (actual !== versionAnulacion) return;
            anulacionErrorDetalle.textContent = error instanceof TypeError
                ? "No se pudo conectar con el servidor. Intente nuevamente."
                : error.message;
            anulacionErrorDetalle.hidden = false;
        } finally {
            if (actual === versionAnulacion) {
                anulacionCarga.hidden = true;
                actualizarBotonAnulacion();
            }
        }
    }

    async function anularVenta() {
        if (anulando || !ventaEnAnulacion) return;

        const motivo = anulacionMotivo.value.trim();
        if (!motivo) return;

        const venta = ventaEnAnulacion;
        anulando = true;
        confirmarAnulacion.textContent = "Anulando…";
        mostrarErrorAnulacion("");
        actualizarBotonAnulacion();

        try {
            const respuesta = await VentasReglas.fetchConSesion(seccion.dataset.anularUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    Accept: "application/json",
                    RequestVerificationToken: antiforgery.value
                },
                body: JSON.stringify({ idVenta: venta.id, motivo })
            });
            const resultado = await respuesta.json().catch(() => null);
            if (!respuesta.ok || !resultado?.exito) {
                throw new Error(resultado?.mensaje || (respuesta.status === 400
                    ? "No se pudo validar la solicitud. Revise los datos o recargue la página si su sesión expiró."
                    : "No se pudo anular la venta. Revise su sesión e intente nuevamente."));
            }

            anulando = false;
            anulacionModal.hide();
            mostrarAviso(`La venta ${venta.nroComprobante} fue anulada correctamente.`);
            cargarHistorial();
            document.dispatchEvent(new CustomEvent("venta:anulada"));
        } catch (error) {
            mostrarErrorAnulacion(error instanceof TypeError
                ? "No se pudo conectar con el servidor. La venta no fue anulada."
                : error.message);
        } finally {
            anulando = false;
            confirmarAnulacion.textContent = "Confirmar anulación";
            actualizarBotonAnulacion();
        }
    }

    anulacionMotivo.addEventListener("input", actualizarBotonAnulacion);
    confirmarAnulacion.addEventListener("click", anularVenta);
    modalAnulacion.addEventListener("shown.bs.modal", () => anulacionMotivo.focus());
    modalAnulacion.addEventListener("hide.bs.modal", (event) => {
        if (anulando) event.preventDefault();
    });
    modalAnulacion.addEventListener("hidden.bs.modal", () => {
        versionAnulacion++;
        ventaEnAnulacion = null;
        resumenAnulacionCargado = false;
        anulacionResumen.hidden = true;
        anulacionErrorDetalle.hidden = true;
        anulacionMotivo.value = "";
        mostrarErrorAnulacion("");
        actualizarBotonAnulacion();
    });

    actualizar.addEventListener("click", cargarHistorial);
    mostrarAnuladas.addEventListener("change", cargarHistorial);
    document.addEventListener("venta:registrada", cargarHistorial);

    cargarHistorial();
})();
