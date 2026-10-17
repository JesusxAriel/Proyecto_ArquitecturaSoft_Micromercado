(() => {
    "use strict";

    const buscador = document.getElementById("buscadorClientes");
    if (!buscador) return;

    async function fetchConSesion(url, opciones, alExpirar) {
        const respuesta = await fetchConSesion(url, opciones);
        if (respuesta.status !== 401) return respuesta;

        alExpirar?.();
        window.location.assign(VentasReglas.urlDeLogin(window.location));
        return new Promise(() => { });
    }

    const entrada = document.getElementById("Input_IdentificacionCliente");
    const idCliente = document.getElementById("Input_IdCliente");
    const resultados = document.getElementById("resultadosClientes");
    const estado = document.getElementById("estadoBusquedaClientes");
    const seleccionado = document.getElementById("clienteSeleccionado");
    const quitar = document.getElementById("quitarCliente");
    let temporizador;
    let version = 0;

    function actualizarSeleccionCliente() {
        resultados.querySelectorAll("button[data-cliente-id]").forEach((boton) => {
            const activo = boton.dataset.clienteId === idCliente.value;
            boton.textContent = activo ? "Seleccionado" : "Seleccionar";
            boton.setAttribute("aria-pressed", String(activo));
            boton.className = `btn btn-${activo ? "primary" : "outline-primary"} btn-sm mt-2`;
        });
    }

    quitar.addEventListener("click", () => {
        idCliente.value = "";
        seleccionado.textContent = "Ningún cliente seleccionado.";
        quitar.hidden = true;
        actualizarSeleccionCliente();
        idCliente.dispatchEvent(new Event("change"));
    });

    entrada.addEventListener("keydown", (event) => {
        if (event.key === "Enter") event.preventDefault();
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
                const respuesta = await fetchConSesion(url, {
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
                fila.className = "list-group-item";
                const nombre = document.createElement("div");
                nombre.className = "fw-semibold text-break";
                nombre.textContent = cliente.razonSocial;
                const documento = document.createElement("div");
                documento.className = "small text-muted text-break";
                documento.textContent = `CI/NIT: ${cliente.nitCi}`;
                const boton = document.createElement("button");
                boton.type = "button";
                boton.className = "btn btn-outline-primary btn-sm mt-2";
                boton.dataset.clienteId = String(cliente.id);
                boton.textContent = "Seleccionar";
                boton.setAttribute("aria-label", `Seleccionar ${cliente.razonSocial}, CI/NIT ${cliente.nitCi}`);
                boton.setAttribute("aria-pressed", String(String(cliente.id) === idCliente.value));
                boton.addEventListener("click", () => {
                    idCliente.value = String(cliente.id);
                    seleccionado.textContent = `Cliente seleccionado: ${cliente.razonSocial} — CI/NIT: ${cliente.nitCi}`;
                    quitar.hidden = false;
                    actualizarSeleccionCliente();
                    idCliente.dispatchEvent(new Event("change"));
                });
                fila.append(nombre, documento, boton);
                resultados.append(fila);
            });
            actualizarSeleccionCliente();
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

        function seleccionarClienteCreado(cliente) {
            idCliente.value = String(cliente.id);
            seleccionado.textContent = `Cliente seleccionado: ${cliente.razonSocial} — CI/NIT: ${cliente.nitCi}`;
            quitar.hidden = false;
            idCliente.dispatchEvent(new Event("change"));
            entrada.value = cliente.nitCi;
            entrada.dispatchEvent(new Event("input"));
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
                const respuesta = await fetchConSesion(formularioCliente.dataset.crearUrl, {
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
                seleccionarClienteCreado(resultado);
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

    const entrada = document.getElementById("Input_BusquedaProducto");
    const idProducto = document.getElementById("Input_IdProducto");
    const resultados = document.getElementById("resultadosProductos");
    const estado = document.getElementById("estadoBusquedaProductos");
    const seleccionado = document.getElementById("productoSeleccionado");
    const quitar = document.getElementById("quitarProducto");
    const precio = new Intl.NumberFormat("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const cantidad = document.getElementById("Input_Cantidad");
    const agregar = document.getElementById("agregarAlDetalle");
    const tablaDetalle = document.getElementById("detalleTemporal");
    const estadoDetalle = document.getElementById("estadoDetalle");
    const detalleTemporal = [];
    const ayudaAgregar = document.getElementById("ayudaAgregarAlDetalle");
    const clienteDelDetalle = document.getElementById("clienteDelDetalle");
    const productosDistintos = document.getElementById("productosDistintos");
    const unidadesTotales = document.getElementById("unidadesTotales");
    const totalGeneral = document.getElementById("totalGeneral");
    const idCliente = document.getElementById("Input_IdCliente");
    const clienteSeleccionado = document.getElementById("clienteSeleccionado");
    const registrarVenta = document.getElementById("registrarVenta");
    const confirmarVenta = document.getElementById("confirmarVenta");
    const efectivoPagado = document.getElementById("efectivoPagado");
    const antiforgery = document.querySelector('input[name="__RequestVerificationToken"]');
    let registrandoVenta = false;
    let ventaRegistrada = false;
    const modalConfirmacion = document.getElementById("confirmarVentaModal");
    const botonesCancelar = modalConfirmacion.querySelectorAll('[data-bs-dismiss="modal"]');
    const confirmacionCliente = document.getElementById("confirmacionCliente");
    const confirmacionProductos = document.getElementById("confirmacionProductosDistintos");
    const confirmacionUnidades = document.getElementById("confirmacionUnidadesTotales");
    const confirmacionTotal = document.getElementById("confirmacionTotalGeneral");
    const confirmacionDetalle = document.getElementById("confirmacionDetalle");
    const confirmacionEfectivo = document.getElementById("confirmacionEfectivo");
    const confirmacionSaldo = document.getElementById("confirmacionSaldo");
    const estadoConfirmacion = document.getElementById("estadoConfirmacionVenta");
    const confirmacionModal = bootstrap.Modal.getOrCreateInstance(modalConfirmacion, {
        backdrop: "static",
        keyboard: false
    });
    const modalEdicion = document.getElementById("editarCantidadModal");
    const formularioEdicion = document.getElementById("editarCantidadForm");
    const productoEdicion = document.getElementById("editarCantidadProducto");
    const cantidadEdicion = document.getElementById("editarCantidadInput");
    const errorEdicion = document.getElementById("editarCantidadError");
    const stockEdicion = document.getElementById("editarCantidadStock");
    const modalCantidad = new bootstrap.Modal(modalEdicion);
    let itemEnEdicion = null;
    let productoSeleccionado = null;
    let temporizador;
    let version = 0;

    function actualizarSeleccion() {
        resultados.querySelectorAll("button[data-producto-id]").forEach((boton) => {
            const activo = boton.dataset.productoId === idProducto.value;
            boton.textContent = activo ? "Seleccionado" : "Seleccionar";
            boton.setAttribute("aria-pressed", String(activo));
            boton.className = `btn btn-${activo ? "primary" : "outline-primary"} btn-sm mt-2`;
        });
    }

    function mostrarEstadoDetalle(mensaje, esError = false) {
        estadoDetalle.textContent = mensaje;
        estadoDetalle.className = `small mt-3 mb-0 text-${esError ? "danger" : "success"}`;
    }

    function mostrarErrorEdicion(mensaje = "") {
        errorEdicion.textContent = mensaje;
        errorEdicion.hidden = !mensaje;
        cantidadEdicion.classList.toggle("is-invalid", Boolean(mensaje));
        cantidadEdicion.setAttribute("aria-invalid", String(Boolean(mensaje)));
    }

    function abrirEdicion(item) {
        itemEnEdicion = item;
        productoEdicion.textContent = item.nombre;
        stockEdicion.textContent = Number.isFinite(item.stockDisponible)
            ? `Stock disponible: ${item.stockDisponible}`
            : "";
        cantidadEdicion.value = String(item.cantidad);
        mostrarErrorEdicion();
        modalCantidad.show();
    }

    function guardarCambiosCantidad(event) {
        event.preventDefault();
        if (!itemEnEdicion) return;

        const unidades = cantidadEdicion.valueAsNumber;
        if (!Number.isSafeInteger(unidades) || unidades <= 0 || !cantidadEdicion.validity.valid) {
            mostrarErrorEdicion("Ingrese una cantidad entera mayor a cero.");
            cantidadEdicion.focus();
            return;
        }

        const resultado = VentasReglas.modificarCantidad(detalleTemporal, itemEnEdicion, unidades);
        if (resultado.error) {
            mostrarErrorEdicion(resultado.error);
            cantidadEdicion.focus();
            return;
        }

        mostrarErrorEdicion();
        renderizarDetalle();
        mostrarEstadoDetalle(`${itemEnEdicion.nombre}: cantidad actualizada.`);
        modalCantidad.hide();
    }

    formularioEdicion.addEventListener("submit", guardarCambiosCantidad);
    modalEdicion.addEventListener("shown.bs.modal", () => cantidadEdicion.focus());
    modalEdicion.addEventListener("hidden.bs.modal", () => {
        itemEnEdicion = null;
        mostrarErrorEdicion();
    });

    function actualizarResumenVenta() {
        const unidades = detalleTemporal.reduce((total, item) => total + item.cantidad, 0);
        const totalCentavos = detalleTemporal.reduce((total, item) => total + Math.round(item.subtotal * 100), 0);

        productosDistintos.textContent = String(detalleTemporal.length);
        unidadesTotales.textContent = String(unidades);
        totalGeneral.textContent = (totalCentavos / 100).toFixed(2);
        actualizarConfirmacionVenta();
    }

    function ventaListaParaConfirmar() {
        return idCliente.value.trim() !== "" && detalleTemporal.length > 0;
    }

    function actualizarDesgloseConfirmacion() {
        confirmacionDetalle.replaceChildren();

        if (detalleTemporal.length === 0) {
            const fila = document.createElement("tr");
            const celda = document.createElement("td");
            celda.colSpan = 4;
            celda.className = "text-center text-muted py-3";
            celda.textContent = "Sin productos.";
            fila.append(celda);
            confirmacionDetalle.append(fila);
        } else {
            detalleTemporal.forEach((item) => {
                const fila = document.createElement("tr");
                const producto = document.createElement("td");
                producto.className = "text-break";
                producto.textContent = `${item.nombre} — ${item.presentacion}`;

                const precioUnitario = document.createElement("td");
                precioUnitario.className = "text-end text-nowrap";
                precioUnitario.textContent = precio.format(item.precioUnitario);

                const unidades = document.createElement("td");
                unidades.className = "text-center";
                unidades.textContent = String(item.cantidad);

                const subtotal = document.createElement("td");
                subtotal.className = "text-end text-nowrap";
                subtotal.textContent = precio.format(item.subtotal);

                fila.append(producto, precioUnitario, unidades, subtotal);
                confirmacionDetalle.append(fila);
            });
        }

        const efectivo = efectivoPagado.valueAsNumber;
        const errorEfectivo = efectivoPagado.value.trim() ? VentasReglas.validarEfectivo(efectivoPagado.value) : null;
        if (!efectivoPagado.value.trim() || errorEfectivo || !Number.isFinite(efectivo) || !efectivoPagado.validity.valid) {
            confirmacionEfectivo.textContent = "Pendiente";
            confirmacionSaldo.textContent = errorEfectivo ?? "Ingrese un efectivo válido para calcular el cambio o faltante.";
            confirmacionSaldo.className = "small text-muted mb-0";
            return;
        }

        confirmacionEfectivo.textContent = precio.format(efectivo);
        const totalCentavos = detalleTemporal.reduce(
            (total, item) => total + Math.round(item.subtotal * 100), 0);
        const diferenciaCentavos = Math.round(efectivo * 100) - totalCentavos;

        if (diferenciaCentavos >= 0) {
            confirmacionSaldo.textContent = `Cambio estimado: Bs ${precio.format(diferenciaCentavos / 100)}`;
            confirmacionSaldo.className = "small text-success mb-0";
        } else {
            confirmacionSaldo.textContent = `Falta: Bs ${precio.format(Math.abs(diferenciaCentavos) / 100)}`;
            confirmacionSaldo.className = "small text-danger mb-0";
        }
    }

    function actualizarConfirmacionVenta() {
        const lista = ventaListaParaConfirmar();
        registrarVenta.disabled = !lista;
        confirmarVenta.disabled = registrandoVenta || (!ventaRegistrada && !lista);
        confirmacionCliente.textContent = clienteSeleccionado.textContent;
        confirmacionProductos.textContent = productosDistintos.textContent;
        confirmacionUnidades.textContent = unidadesTotales.textContent;
        confirmacionTotal.textContent = totalGeneral.textContent;
        actualizarDesgloseConfirmacion();
    }

    efectivoPagado.addEventListener("input", actualizarDesgloseConfirmacion);
    function actualizarClienteVenta() {
        const tieneCliente = idCliente.value.trim() !== "";
        agregar.disabled = !tieneCliente;

        if (!tieneCliente) {
            clienteDelDetalle.textContent = "Cliente de esta venta: Ningún cliente seleccionado.";
            ayudaAgregar.textContent = "Seleccione un cliente para habilitar esta acción.";
            ayudaAgregar.className = "small text-muted mt-2 mb-0";
            return;
        }

        const datosCliente = clienteSeleccionado.textContent.replace(/^Cliente seleccionado:\s*/, "");
        clienteDelDetalle.textContent = `Cliente de esta venta: ${datosCliente}`;
        ayudaAgregar.textContent = "Cliente seleccionado. Ya puede agregar productos al detalle.";
        ayudaAgregar.className = "small text-success mt-2 mb-0";
    }

    idCliente.addEventListener("change", () => {
        actualizarClienteVenta();
        actualizarConfirmacionVenta();
    });
    registrarVenta.addEventListener("click", (event) => {
        event.preventDefault();
        event.stopPropagation();
        if (registrandoVenta || ventaRegistrada) return;
        actualizarResumenVenta();
        if (!ventaListaParaConfirmar()) return;
        estadoConfirmacion.textContent = "";
        estadoConfirmacion.hidden = true;
        confirmacionModal.show();
    });
    function mostrarEstadoConfirmacion(mensaje, tipo = "info") {
        estadoConfirmacion.textContent = mensaje;
        estadoConfirmacion.className = `alert alert-${tipo} mb-0`;
        estadoConfirmacion.setAttribute("role", tipo === "danger" ? "alert" : "status");
        estadoConfirmacion.hidden = false;
        estadoConfirmacion.scrollIntoView({ block: "nearest" });
    }

    modalConfirmacion.addEventListener("hide.bs.modal", (event) => {
        if (registrandoVenta) event.preventDefault();
    });

    modalConfirmacion.addEventListener("hidden.bs.modal", () => {
        if (!ventaRegistrada) return;
        ventaRegistrada = false;
        detalleTemporal.length = 0;
        document.getElementById("quitarCliente").click();
        const busquedaCliente = document.getElementById("Input_IdentificacionCliente");
        busquedaCliente.value = "";
        busquedaCliente.dispatchEvent(new Event("input"));
        quitar.click();
        entrada.value = "";
        entrada.dispatchEvent(new Event("input"));
        cantidad.value = "1";
        efectivoPagado.value = "";
        renderizarDetalle();
        estadoConfirmacion.textContent = "";
        estadoConfirmacion.hidden = true;
        confirmarVenta.textContent = "Confirmar venta";
        botonesCancelar.forEach(boton => { boton.hidden = false; });
    });

    confirmarVenta.addEventListener("click", async (event) => {
        event.preventDefault();
        event.stopPropagation();
        if (registrandoVenta) return;
        if (ventaRegistrada) {
            confirmacionModal.hide();
            return;
        }
        const cliente = Number(idCliente.value);
        if (!Number.isInteger(cliente) || cliente <= 0 || cliente > 2147483647) {
            mostrarEstadoConfirmacion("Seleccione un cliente para registrar la venta.", "danger");
            return;
        }
        if (detalleTemporal.length === 0 || detalleTemporal.some(item =>
            !Number.isInteger(item.idProducto) || item.idProducto <= 0 || item.idProducto > 2147483647 ||
            !Number.isInteger(item.cantidad) || item.cantidad <= 0 || item.cantidad > 2147483647)) {
            mostrarEstadoConfirmacion("Agregue al menos un producto y revise que las cantidades sean enteras mayores a cero.", "danger");
            return;
        }
        const efectivo = efectivoPagado.valueAsNumber;
        const errorEfectivo = VentasReglas.validarEfectivo(efectivoPagado.value);
        if (errorEfectivo || !Number.isFinite(efectivo) || !efectivoPagado.validity.valid) {
            mostrarEstadoConfirmacion(errorEfectivo ?? "Ingrese un efectivo pagado válido.", "danger");
            return;
        }

        const payload = {
            idCliente: cliente,
            efectivoPagado: efectivo,
            lineas: detalleTemporal.map(item => ({ idProducto: item.idProducto, cantidad: item.cantidad, precioEsperado: item.precioUnitario }))
        };
        registrandoVenta = true;
        confirmarVenta.disabled = true;
        confirmarVenta.textContent = "Registrando venta…";
        const ventanaComprobante = window.open("", "_blank");
        mostrarEstadoConfirmacion("Registrando venta…");
        const inicioRegistro = performance.now();
        async function esperarTiempoMinimoRegistro() {
            const tiempoRestante = 800 - (performance.now() - inicioRegistro);
            if (tiempoRestante > 0) {
                await new Promise(resolve => setTimeout(resolve, tiempoRestante));
            }
        }
        try {
            const respuesta = await fetchConSesion(confirmarVenta.dataset.registroUrl, {
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
                VentasReglas.quitarProductoDelDetalle(detalleTemporal, resultado.idProducto);
                renderizarDetalle();
                actualizarConfirmacionVenta();
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
                mensajeExito += ` Total definitivo: Bs ${precio.format(Number(resultado.total))}.`;
            }
            await esperarTiempoMinimoRegistro();
            mostrarEstadoConfirmacion(mensajeExito, "success");
            ventaRegistrada = true;
            document.dispatchEvent(new CustomEvent("venta:registrada"));
            botonesCancelar.forEach(boton => { boton.hidden = true; });
        } catch (error) {
            ventanaComprobante?.close();
            await esperarTiempoMinimoRegistro();
            mostrarEstadoConfirmacion(error instanceof TypeError
                ? "No se pudo conectar con el servidor. La venta temporal se conserva."
                : error.message, "danger");
        } finally {
            registrandoVenta = false;
            confirmarVenta.textContent = ventaRegistrada ? "Cerrar" : "Confirmar venta";
            registrarVenta.disabled = !ventaListaParaConfirmar();
            confirmarVenta.disabled = !ventaRegistrada && !ventaListaParaConfirmar();
        }
    });

    actualizarClienteVenta();
    actualizarResumenVenta();

    function renderizarDetalle() {
        actualizarResumenVenta();
        tablaDetalle.replaceChildren();
        if (detalleTemporal.length === 0) {
            const fila = document.createElement("tr");
            const celda = document.createElement("td");
            celda.colSpan = 6;
            celda.className = "text-center text-muted py-4";
            celda.textContent = "Todavía no hay productos en el detalle.";
            fila.append(celda);
            tablaDetalle.append(fila);
            return;
        }

        detalleTemporal.forEach((item, indice) => {
            const fila = document.createElement("tr");
            [
                [item.nombre, "text-break"],
                [item.presentacion, "text-break"],
                [precio.format(item.precioUnitario), "text-end"],
                [String(item.cantidad), "text-center"],
                [precio.format(item.subtotal), "text-end"]
            ].forEach(([valor, clase]) => {
                const celda = document.createElement("td");
                celda.textContent = valor;
                celda.className = clase;
                fila.append(celda);
            });

            const acciones = document.createElement("td");
            const editar = document.createElement("button");
            editar.type = "button";
            editar.className = "btn btn-outline-primary btn-sm me-2";
            editar.textContent = "Editar";
            editar.addEventListener("click", () => abrirEdicion(item));

            const eliminar = document.createElement("button");
            eliminar.type = "button";
            eliminar.className = "btn btn-outline-danger btn-sm";
            eliminar.textContent = "Eliminar";
            eliminar.addEventListener("click", () => {
                detalleTemporal.splice(indice, 1);
                renderizarDetalle();
                mostrarEstadoDetalle(`${item.nombre} eliminado del detalle temporal.`);
            });
            acciones.append(editar, eliminar);
            fila.append(acciones);
            tablaDetalle.append(fila);
        });
    }

    agregar.addEventListener("click", () => {
        if (!productoSeleccionado) {
            mostrarEstadoDetalle("Seleccione un producto antes de agregarlo al detalle.", true);
            return;
        }

        const unidades = cantidad.valueAsNumber;
        if (!Number.isSafeInteger(unidades) || unidades <= 0 || !cantidad.validity.valid) {
            mostrarEstadoDetalle("Ingrese una cantidad entera mayor a cero.", true);
            cantidad.focus();
            return;
        }

        const resultado = VentasReglas.agregarAlDetalle(detalleTemporal, productoSeleccionado, unidades);
        if (resultado.error) {
            mostrarEstadoDetalle(resultado.error, true);
            if (resultado.campo === "cantidad") cantidad.focus();
            return;
        }

        renderizarDetalle();
        mostrarEstadoDetalle(resultado.fusionado
            ? `${resultado.item.nombre}: se sumaron ${unidades} unidad(es) a la fila existente (${resultado.total} en total).`
            : `${resultado.item.nombre} agregado al detalle temporal.`);
    });

    quitar.addEventListener("click", () => {
        idProducto.value = "";
        productoSeleccionado = null;
        mostrarEstadoDetalle("");
        seleccionado.textContent = "Ningún producto seleccionado.";
        quitar.hidden = true;
        actualizarSeleccion();
    });

    entrada.addEventListener("keydown", (event) => {
        if (event.key === "Enter") event.preventDefault();
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

        estado.textContent = "Buscando productos…";
        temporizador = setTimeout(() => buscar(termino, actual), 300);
    });

    function resumenProducto(producto) {
        return `${producto.presentacion} — Precio de venta: Bs ${precio.format(producto.precioVenta)} — Stock disponible (informativo): ${producto.stockDisponible}`;
    }

    async function consultarProducto(referencia) {
        const url = new URL(buscador.dataset.busquedaUrl, window.location.href);
        url.searchParams.set("termino", referencia.nombre);
        const respuesta = await fetchConSesion(url, { headers: { Accept: "application/json" }, cache: "no-store" });
        if (!respuesta.ok) return null;
        const listado = await respuesta.json();
        return listado.productos.find((candidato) => candidato.id === referencia.id) ?? null;
    }

    async function refrescarStocks() {
        const termino = entrada.value.trim();
        if (VentasReglas.terminoDeBusquedaValido(termino)) buscar(termino, ++version);

        const referencias = new Map();
        if (productoSeleccionado) referencias.set(productoSeleccionado.id, productoSeleccionado);
        detalleTemporal.forEach((item) => {
            if (!referencias.has(item.idProducto)) {
                referencias.set(item.idProducto, { id: item.idProducto, nombre: item.nombre });
            }
        });

        for (const [id, referencia] of referencias) {
            try {
                const actualizado = await consultarProducto(referencia);
                if (!actualizado) continue;

                if (productoSeleccionado?.id === id) {
                    productoSeleccionado.stockDisponible = actualizado.stockDisponible;
                    seleccionado.textContent = `Producto seleccionado: ${productoSeleccionado.nombre} — ${resumenProducto(productoSeleccionado)}`;
                }
                detalleTemporal
                    .filter((item) => item.idProducto === id)
                    .forEach((item) => { item.stockDisponible = Number(actualizado.stockDisponible); });
            } catch {
                continue;
            }
        }
    }

    document.addEventListener("venta:anulada", refrescarStocks);
    document.addEventListener("venta:registrada", refrescarStocks);

    async function buscar(termino, actual) {
        resultados.setAttribute("aria-busy", "true");

        try {
            const url = new URL(buscador.dataset.busquedaUrl, window.location.href);
            url.searchParams.set("termino", termino);
            const listado = await VentasReglas.conReintento(async () => {
                const respuesta = await fetchConSesion(url, {
                    headers: { Accept: "application/json" },
                    cache: "no-store"
                });
                if (!respuesta.ok) throw new Error("No se pudo consultar productos.");
                return respuesta.json();
            });
            if (actual !== version) return;

            resultados.replaceChildren();
            listado.productos.forEach((producto) => {
                const fila = document.createElement("li");
                fila.className = "list-group-item";
                const nombre = document.createElement("div");
                nombre.className = "fw-semibold text-break";
                nombre.textContent = producto.nombre;
                const datos = document.createElement("div");
                datos.className = "small text-muted text-break";
                const resumen = resumenProducto(producto);
                datos.textContent = resumen;
                const boton = document.createElement("button");
                boton.type = "button";
                boton.dataset.productoId = String(producto.id);
                boton.setAttribute("aria-label", `Seleccionar ${producto.nombre}, ${producto.presentacion}`);
                boton.addEventListener("click", () => {
                    idProducto.value = String(producto.id);
                    productoSeleccionado = producto;
                    mostrarEstadoDetalle("");
                    seleccionado.textContent = `Producto seleccionado: ${producto.nombre} — ${resumen}`;
                    quitar.hidden = false;
                    actualizarSeleccion();
                });
                fila.append(nombre, datos, boton);
                resultados.append(fila);
            });
            actualizarSeleccion();
            estado.textContent = listado.productos.length
                ? listado.hayMas
                    ? `Mostrando los primeros ${listado.productos.length} productos. Afine la búsqueda para ver otras coincidencias.`
                    : `${listado.productos.length} producto(s) encontrado(s).`
                : "No se encontraron productos con ese término.";
        } catch (error) {
            if (actual !== version) return;
            resultados.replaceChildren();
            estado.textContent = "No se pudo buscar productos. Intente nuevamente.";
        } finally {
            if (actual === version) resultados.setAttribute("aria-busy", "false");
        }
    }
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
            const respuesta = await fetchConSesion(url, {
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
            const respuesta = await fetchConSesion(url, {
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
            const respuesta = await fetchConSesion(url, {
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
            const respuesta = await fetchConSesion(seccion.dataset.anularUrl, {
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
