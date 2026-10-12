(() => {
    "use strict";

    const buscador = document.getElementById("buscadorClientes");
    if (!buscador) return;

    const entrada = document.getElementById("Input_IdentificacionCliente");
    const idCliente = document.getElementById("Input_IdCliente");
    const resultados = document.getElementById("resultadosClientes");
    const estado = document.getElementById("estadoBusquedaClientes");
    const seleccionado = document.getElementById("clienteSeleccionado");
    const quitar = document.getElementById("quitarCliente");
    let temporizador;
    let consulta;
    let version = 0;

    quitar.addEventListener("click", () => {
        idCliente.value = "";
        seleccionado.textContent = "Ningún cliente seleccionado.";
        quitar.hidden = true;
        idCliente.dispatchEvent(new Event("change"));
    });

    entrada.addEventListener("keydown", (event) => {
        if (event.key === "Enter") event.preventDefault();
    });

    entrada.addEventListener("input", () => {
        clearTimeout(temporizador);
        consulta?.abort();
        const actual = ++version;
        const termino = entrada.value.trim();
        resultados.replaceChildren();
        resultados.setAttribute("aria-busy", "false");
        estado.textContent = termino ? "Buscando clientes…" : "";
        if (!termino) return;

        temporizador = setTimeout(() => buscar(termino, actual), 300);
    });

    async function buscar(termino, actual) {
        const controlador = new AbortController();
        consulta = controlador;
        resultados.setAttribute("aria-busy", "true");

        try {
            const url = new URL(buscador.dataset.busquedaUrl, window.location.href);
            url.searchParams.set("termino", termino);
            const respuesta = await fetch(url, {
                signal: controlador.signal,
                headers: { Accept: "application/json" },
                cache: "no-store"
            });
            if (!respuesta.ok) throw new Error("No se pudo consultar clientes.");
            const clientes = await respuesta.json();
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
                boton.textContent = "Seleccionar";
                boton.setAttribute("aria-label", `Seleccionar ${cliente.razonSocial}, CI/NIT ${cliente.nitCi}`);
                boton.addEventListener("click", () => {
                    idCliente.value = String(cliente.id);
                    seleccionado.textContent = `Cliente seleccionado: ${cliente.razonSocial} — CI/NIT: ${cliente.nitCi}`;
                    quitar.hidden = false;
                    idCliente.dispatchEvent(new Event("change"));
                });
                fila.append(nombre, documento, boton);
                resultados.append(fila);
            });
            estado.textContent = clientes.length
                ? `${clientes.length} cliente(s) encontrado(s).`
                : "No se encontraron clientes activos con ese CI/NIT o razón social.";
        } catch (error) {
            if (actual !== version || error.name === "AbortError") return;
            resultados.replaceChildren();
            estado.textContent = "No se pudo buscar clientes. Intente nuevamente.";
        } finally {
            if (actual === version) resultados.setAttribute("aria-busy", "false");
        }
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
    const modalCantidad = new bootstrap.Modal(modalEdicion);
    let itemEnEdicion = null;
    let productoSeleccionado = null;
    let temporizador;
    let consulta;
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
        cantidadEdicion.value = String(item.cantidad);
        mostrarErrorEdicion();
        modalCantidad.show();
    }

    function guardarCambiosCantidad(event) {
        event.preventDefault();
        if (!itemEnEdicion) return;

        const unidades = cantidadEdicion.valueAsNumber;
        if (!Number.isSafeInteger(unidades) || unidades <= 0 || !cantidadEdicion.validity.valid) {
            mostrarErrorEdicion("Ingrese una cantidad entera superior a 0.");
            cantidadEdicion.focus();
            return;
        }

        const subtotal = Math.round((itemEnEdicion.precioUnitario * unidades + Number.EPSILON) * 100) / 100;
        if (!Number.isFinite(subtotal)) {
            mostrarErrorEdicion("El subtotal no es válido. Ingrese otra cantidad.");
            cantidadEdicion.focus();
            return;
        }

        mostrarErrorEdicion();
        itemEnEdicion.cantidad = unidades;
        itemEnEdicion.subtotal = subtotal;
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

    function actualizarConfirmacionVenta() {
        const lista = ventaListaParaConfirmar();
        registrarVenta.disabled = !lista;
        confirmarVenta.disabled = registrandoVenta || (!ventaRegistrada && !lista);
        confirmacionCliente.textContent = clienteSeleccionado.textContent;
        confirmacionProductos.textContent = productosDistintos.textContent;
        confirmacionUnidades.textContent = unidadesTotales.textContent;
        confirmacionTotal.textContent = totalGeneral.textContent;
    }

    idCliente.addEventListener("change", actualizarConfirmacionVenta);
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
        estadoConfirmacion.hidden = false;
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
        if (!Number.isFinite(efectivo) || efectivo < 0 || !efectivoPagado.validity.valid) {
            mostrarEstadoConfirmacion("Ingrese un efectivo pagado válido, mayor o igual a cero y con hasta dos decimales.", "danger");
            return;
        }

        const payload = {
            idCliente: cliente,
            efectivoPagado: efectivo,
            lineas: detalleTemporal.map(item => ({ idProducto: item.idProducto, cantidad: item.cantidad }))
        };
        registrandoVenta = true;
        confirmarVenta.disabled = true;
        confirmarVenta.textContent = "Registrando venta...";
        mostrarEstadoConfirmacion("Registrando venta...");
        const inicioRegistro = performance.now();
        async function esperarTiempoMinimoRegistro() {
            const tiempoRestante = 800 - (performance.now() - inicioRegistro);
            if (tiempoRestante > 0) {
                await new Promise(resolve => setTimeout(resolve, tiempoRestante));
            }
        }
        try {
            const respuesta = await fetch(confirmarVenta.dataset.registroUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    Accept: "application/json",
                    RequestVerificationToken: antiforgery.value
                },
                body: JSON.stringify(payload)
            });
            const resultado = await respuesta.json().catch(() => null);
            if (!respuesta.ok || !resultado?.exito) {
                throw new Error(resultado?.mensaje || (respuesta.status === 400
                    ? "No se pudo validar la solicitud. Revise los datos o recargue la página si su sesión expiró."
                    : "No se pudo registrar la venta. Revise su sesión e intente nuevamente."));
            }

            let mensajeExito = "Venta registrada correctamente.";
            if (resultado.idVenta != null) mensajeExito += ` N.º ${resultado.idVenta}.`;
            if (resultado.total != null && Number.isFinite(Number(resultado.total))) {
                mensajeExito += ` Total definitivo: Bs ${precio.format(Number(resultado.total))}.`;
            }
            await esperarTiempoMinimoRegistro();
            mostrarEstadoConfirmacion(mensajeExito, "success");
            ventaRegistrada = true;
            botonesCancelar.forEach(boton => { boton.hidden = true; });
        } catch (error) {
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

        if (detalleTemporal.some((item) => item.idProducto === productoSeleccionado.id)) {
            mostrarEstadoDetalle("El producto ya está en el detalle. No se agregó otra fila.", true);
            return;
        }

        const precioUnitario = Number(productoSeleccionado.precioVenta);
        const subtotal = Math.round((precioUnitario * unidades + Number.EPSILON) * 100) / 100;
        if (!Number.isFinite(precioUnitario) || !Number.isFinite(subtotal)) {
            mostrarEstadoDetalle("El precio del producto no es válido. Seleccione el producto nuevamente.", true);
            return;
        }

        const item = {
            idProducto: productoSeleccionado.id,
            nombre: productoSeleccionado.nombre,
            presentacion: productoSeleccionado.presentacion,
            precioUnitario,
            cantidad: unidades,
            subtotal
        };
        detalleTemporal.push(item);

        renderizarDetalle();
        mostrarEstadoDetalle(`${item.nombre} agregado al detalle temporal.`);
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
        consulta?.abort();
        const actual = ++version;
        const termino = entrada.value.trim();
        resultados.replaceChildren();
        resultados.setAttribute("aria-busy", "false");
        estado.textContent = termino ? "Buscando productos…" : "";
        if (!termino) return;

        temporizador = setTimeout(() => buscar(termino, actual), 300);
    });

    async function buscar(termino, actual) {
        const controlador = new AbortController();
        consulta = controlador;
        resultados.setAttribute("aria-busy", "true");

        try {
            const url = new URL(buscador.dataset.busquedaUrl, window.location.href);
            url.searchParams.set("termino", termino);
            const respuesta = await fetch(url, {
                signal: controlador.signal,
                headers: { Accept: "application/json" },
                cache: "no-store"
            });
            if (!respuesta.ok) throw new Error("No se pudo consultar productos.");
            const listado = await respuesta.json();
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
                const resumen = `${producto.presentacion} — Precio de venta: Bs ${precio.format(producto.precioVenta)} — Stock disponible (informativo): ${producto.stockDisponible}`;
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
                ? listado.total > listado.productos.length
                    ? `Mostrando ${listado.productos.length} de ${listado.total} productos. Afine la búsqueda para ver otras coincidencias.`
                    : `${listado.productos.length} producto(s) encontrado(s).`
                : "No se encontraron productos con ese término.";
        } catch (error) {
            if (actual !== version || error.name === "AbortError") return;
            resultados.replaceChildren();
            estado.textContent = "No se pudo buscar productos. Intente nuevamente.";
        } finally {
            if (actual === version) resultados.setAttribute("aria-busy", "false");
        }
    }
})();
