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
    const montoPago = document.getElementById("montoPago");
    const vueltoPago = document.getElementById("vueltoPago");
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

    function actualizarVuelto() {
        const texto = montoPago.value.trim();
        vueltoPago.className = "small mb-0 mt-1";
        const error = VentasReglas.validarMontoDePago(texto);
        if (error) {
            vueltoPago.textContent = error;
            vueltoPago.classList.add("text-danger");
            return;
        }
        if (texto === "") {
            vueltoPago.textContent = "";
            return;
        }

        const { totalCentavos } = VentasReglas.resumirDetalle(detalleTemporal);
        const saldo = VentasReglas.calcularVuelto(totalCentavos, Number(texto));
        if (saldo.tipo === "vuelto") {
            vueltoPago.textContent = `Vuelto: Bs ${precio.format(saldo.centavos / 100)}`;
            vueltoPago.classList.add("text-success");
        } else {
            vueltoPago.textContent = `Falta Bs ${precio.format(saldo.centavos / 100)}`;
            vueltoPago.classList.add("text-danger");
        }
    }

    function actualizarResumen() {
        const resumen = VentasReglas.resumirDetalle(detalleTemporal);
        unidadesTotales.textContent = String(resumen.unidades);
        totalGeneral.textContent = precio.format(resumen.totalCentavos / 100);
        actualizarVuelto();
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

    montoPago.addEventListener("input", actualizarVuelto);

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

    function reiniciarVenta() {
        detalleTemporal.length = 0;
        montoPago.value = "";
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
        const payload = {
            idCliente: cliente,
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
