(() => {
    "use strict";
    const productos = document.getElementById("buscadorProductos");
    const clientes = document.getElementById("buscadorClientes");
    if (!productos || !clientes) return;
    const moneda = new Intl.NumberFormat("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    async function consultar(base, parametros, signal) {
        const url = new URL(base, window.location.href);
        Object.entries(parametros).forEach(([nombre, valor]) => url.searchParams.set(nombre, valor));
        const respuesta = await VentasReglas.fetchConSesion(url, { method: "GET", credentials: "same-origin", cache: "no-store", headers: { Accept: "application/json" }, signal });
        const cuerpo = await respuesta.json().catch(() => null);
        if (!respuesta.ok) throw new Error(cuerpo?.mensaje || "No se pudo consultar Ventas. Intente nuevamente.");
        return cuerpo;
    }
    function texto(elemento, contenido, clase) {
        const nodo = document.createElement(elemento);
        nodo.textContent = contenido;
        if (clase) nodo.className = clase;
        return nodo;
    }
    let seleccion = 0;
    const detalle = [];
    let registrando = false;
    const lineasInvalidas = new Set();
    const estadoStock = texto("div", "", "small mt-2");
    estadoStock.setAttribute("role", "status");
    document.getElementById("estadoVenta").after(estadoStock);
    function actualizarPago(totalCentavos) {
        const entrada = document.getElementById("montoPago");
        const salida = document.getElementById("vueltoPago");
        const textoPago = entrada.value.trim();
        salida.className = "small mb-0 mt-1";
        const errorFormato = VentasReglas.validarMontoDePago(textoPago);
        if (errorFormato) {
            salida.textContent = errorFormato;
            salida.classList.add("text-danger");
            return false;
        }
        if (textoPago === "") {
            salida.textContent = "";
            return true;
        }
        const monto = Number(textoPago);
        const errorTotal = VentasReglas.validarPagoContraTotal(totalCentavos, monto);
        if (errorTotal) {
            salida.textContent = errorTotal;
            salida.classList.add("text-danger");
            return false;
        }
        const cambio = VentasReglas.calcularVuelto(totalCentavos, monto);
        salida.textContent = `Cambio: Bs ${moneda.format(cambio.centavos / 100)}`;
        salida.classList.add("text-success");
        return true;
    }
    function actualizarEstado() {
        const total = VentasReglas.resumirDetalle(detalle);
        document.getElementById("totalGeneral").textContent = detalle.length ? moneda.format(total.totalCentavos / 100) : "—";
        document.getElementById("unidadesTotales").textContent = detalle.length ? String(total.unidades) : "—";
        const pagoValido = actualizarPago(total.totalCentavos);
        document.getElementById("confirmarVenta").disabled = registrando || !detalle.length || !Number(document.getElementById("Input_IdCliente").value) || !pagoValido || lineasInvalidas.size > 0;
    }
    function renderizarDetalle() {
        lineasInvalidas.clear();
        const cuerpo = document.getElementById("detalleTemporal");
        if (!detalle.length) {
            const fila = document.createElement("tr");
            const celda = texto("td", "Seleccione un producto para consultar su precio y stock vigentes.", "text-center text-body-secondary py-4");
            celda.colSpan = 6;
            fila.append(celda);
            cuerpo.replaceChildren(fila);
            actualizarEstado();
            return;
        }
        cuerpo.replaceChildren(...detalle.map(item => {
            const fila = document.createElement("tr");
            const celdaProducto = texto("td", `${item.nombre} · ${item.presentacion}`, "text-break");
            fila.append(celdaProducto);
            fila.append(texto("td", moneda.format(item.precioUnitario), "text-end"));
            fila.append(texto("td", String(item.stockDisponible), "columna-cantidad"));
            const cantidad = document.createElement("input");
            cantidad.type = "number"; cantidad.min = "1"; cantidad.max = String(item.stockDisponible); cantidad.value = String(item.cantidad);
            cantidad.className = "form-control form-control-sm"; cantidad.setAttribute("aria-label", `Cantidad de ${item.nombre}`);
            const errorCantidad = texto("div", "", "text-danger small mt-1");
            errorCantidad.setAttribute("role", "alert");
            errorCantidad.hidden = true;
            celdaProducto.append(errorCantidad);
            const validarCantidad = () => {
                if (cantidad.value.trim() === "") { lineasInvalidas.add(item); mostrarErrorCantidad(""); return; }
                const resultado = VentasReglas.cambiarCantidad(detalle, item, cantidad.value);
                if (resultado.error) lineasInvalidas.add(item); else lineasInvalidas.delete(item);
                mostrarErrorCantidad(resultado.error || "");
                actualizarEstado();
            };
            const mostrarErrorCantidad = mensaje => {
                errorCantidad.textContent = mensaje;
                errorCantidad.hidden = !mensaje;
                cantidad.classList.toggle("is-invalid", Boolean(mensaje));
            };
            cantidad.addEventListener("input", validarCantidad);
            cantidad.addEventListener("change", validarCantidad);
            const cantidadCelda = document.createElement("td"); cantidadCelda.className = "columna-cantidad"; cantidadCelda.append(cantidad); fila.append(cantidadCelda);
            fila.append(texto("td", "Listo", "text-end"));
            const quitar = texto("button", "Quitar", "btn btn-sm btn-outline-secondary");
            quitar.type = "button"; quitar.addEventListener("click", () => { VentasReglas.quitarProductoDelDetalle(detalle, item.idProducto); renderizarDetalle(); });
            const acciones = document.createElement("td"); acciones.append(quitar); fila.append(acciones);
            return fila;
        }));
        actualizarEstado();
    }
    function mostrarEstado(mensaje, tipo = "info") {
        const estado = document.getElementById("estadoVenta");
        estado.textContent = mensaje; estado.className = `alert alert-${tipo} py-2 px-3 small mt-3 mb-0`; estado.hidden = false;
    }
    async function verProducto(producto) {
        if (registrando) return;
        const version = ++seleccion;
        const aviso = document.getElementById("avisoDetalle");
        aviso.textContent = "Consultando precio y stock vigentes…";
        try {
            const [precio, stock] = await Promise.all([
                consultar(productos.dataset.precioUrl, { id: producto.id }),
                consultar(productos.dataset.stockUrl, { id: producto.id })
            ]);
            if (version !== seleccion || registrando) return;
            producto.precioVenta = precio.precioVenta; producto.stockDisponible = stock.stockDisponible;
            const resultado = VentasReglas.agregarConUnClic(detalle, producto);
            if (resultado.error) { mostrarEstado(resultado.error, "danger"); return; }
            renderizarDetalle();
            aviso.textContent = "Producto agregado. El precio y stock se validarán nuevamente al registrar.";
        } catch (error) { if (version === seleccion) mostrarEstado(error.message, "danger"); }
    }
    function verCliente(cliente) {
        document.getElementById("Input_IdCliente").value = cliente.id;
        document.getElementById("clienteNombre").textContent = cliente.razonSocial;
        document.getElementById("clienteDocumento").textContent = "CI/NIT: " + cliente.nitCi;
        document.getElementById("clienteBusqueda").hidden = true;
        document.getElementById("clienteTarjeta").hidden = false;
        actualizarEstado();
    }
    const crearClienteForm = document.getElementById("crearClienteForm");
    if (crearClienteForm) {
        let creandoCliente = false;
        crearClienteForm.addEventListener("submit", async event => {
            event.preventDefault();
            if (creandoCliente) return;
            const nitCi = document.getElementById("crearClienteNitCi").value.trim();
            const razonSocial = document.getElementById("crearClienteRazonSocial").value.trim();
            const error = document.getElementById("crearClienteError");
            error.hidden = true;
            error.textContent = "";
            if (!nitCi || nitCi.length > 30 || !razonSocial || razonSocial.length > 150) {
                error.textContent = "Ingrese un CI/NIT y una razón social válidos.";
                error.hidden = false;
                return;
            }
            creandoCliente = true;
            const guardar = document.getElementById("crearClienteGuardar");
            guardar.disabled = true;
            guardar.textContent = "Guardando…";
            try {
                const respuesta = await VentasReglas.fetchConSesion(crearClienteForm.dataset.crearUrl, {
                    method: "POST",
                    credentials: "same-origin",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value,
                        Accept: "application/json"
                    },
                    body: JSON.stringify({ nitCi, razonSocial })
                });
                const cuerpo = await respuesta.json().catch(() => null);
                if (!respuesta.ok) throw new Error(cuerpo?.mensaje || "No se pudo registrar el cliente.");
                verCliente(cuerpo);
                document.getElementById("crearClienteForm").reset();
                bootstrap.Modal.getOrCreateInstance(document.getElementById("crearClienteModal")).hide();
                mostrarEstado("Cliente creado y seleccionado para la venta.", "success");
            } catch (errorCrear) {
                error.textContent = errorCrear.message;
                error.hidden = false;
            } finally {
                creandoCliente = false;
                guardar.disabled = false;
                guardar.textContent = "Guardar cliente";
            }
        });
    }
    document.getElementById("cambiarCliente").addEventListener("click", () => {
        document.getElementById("Input_IdCliente").value = "";
        document.getElementById("clienteBusqueda").hidden = false;
        document.getElementById("clienteTarjeta").hidden = true;
        document.getElementById("Input_IdentificacionCliente").focus();
        actualizarEstado();
    });
    function enlazar(contenedor, entradaId, listaId, estadoId, esProducto) {
        const entrada = document.getElementById(entradaId);
        const lista = document.getElementById(listaId);
        const estado = document.getElementById(estadoId);
        let temporizador, controlador, version = 0;
        entrada.addEventListener("keydown", e => { if (e.key === "Enter") e.preventDefault(); });
        entrada.addEventListener("input", () => {
            clearTimeout(temporizador);
            controlador?.abort();
            const actual = ++version;
            const termino = entrada.value.trim();
            lista.replaceChildren();
            lista.setAttribute("aria-busy", "false");
            if (termino.length < 2 || termino.length > 150) { estado.textContent = "Escriba entre 2 y 150 caracteres."; return; }
            estado.textContent = "Buscando…";
            temporizador = setTimeout(async () => {
                controlador = new AbortController();
                lista.setAttribute("aria-busy", "true");
                try {
                    const cuerpo = await consultar(contenedor.dataset.busquedaUrl, { termino }, controlador.signal);
                    if (actual !== version) return;
                    const elementos = esProducto ? cuerpo.productos : cuerpo;
                    elementos.forEach(item => {
                        const boton = texto("button", "", esProducto ? "resultado-producto" : "resultado-cliente");
                        boton.type = "button";
                        boton.append(texto("span", esProducto ? item.nombre : item.razonSocial, "resultado-principal text-break"));
                        boton.append(texto("span", esProducto ? `${item.presentacion} · Bs ${moneda.format(item.precioVenta)} · Stock: ${item.stockDisponible}` : `CI/NIT: ${item.nitCi}`, "resultado-secundario text-break"));
                        if (esProducto) boton.dataset.productoId = item.id; else boton.dataset.clienteId = item.id;
                        boton.addEventListener("click", () => esProducto ? verProducto(item) : verCliente(item));
                        const fila = document.createElement("li"); fila.append(boton); lista.append(fila);
                    });
                    estado.textContent = elementos.length ? `${elementos.length} resultado(s).${cuerpo.hayMas ? " Refine la búsqueda para ver más." : ""}` : "No se encontraron resultados activos.";
                } catch (error) {
                    if (actual === version && error.name !== "AbortError") estado.textContent = error.message;
                } finally { if (actual === version) lista.setAttribute("aria-busy", "false"); }
            }, 300);
        });
    }
    enlazar(productos, "Input_BusquedaProducto", "resultadosProductos", "estadoBusquedaProductos", true);
    enlazar(clientes, "Input_IdentificacionCliente", "resultadosClientes", "estadoBusquedaClientes", false);
    document.getElementById("confirmarVenta").addEventListener("click", async () => {
        if (registrando || !detalle.length || !Number(document.getElementById("Input_IdCliente").value)) return;
        if (!actualizarPago(VentasReglas.resumirDetalle(detalle).totalCentavos)) return;
        registrando = true; seleccion++; actualizarEstado(); mostrarEstado("Registrando venta…");
        estadoStock.textContent = "";
        const lineasEnviadas = detalle.map(item => ({ ...item }));
        try {
            const respuesta = await VentasReglas.fetchConSesion("?handler=RegistrarVenta", {
                method: "POST", credentials: "same-origin",
                headers: { "Content-Type": "application/json", "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]').value, Accept: "application/json" },
                body: JSON.stringify({ idCliente: Number(document.getElementById("Input_IdCliente").value), lineas: detalle.map(item => ({ idProducto: item.idProducto, cantidad: item.cantidad, precioEsperado: item.precioUnitario })) })
            });
            const cuerpo = await respuesta.json().catch(() => null);
            if (!respuesta.ok) throw new Error(cuerpo?.mensaje || "No se pudo registrar la venta.");
            mostrarEstado(`Venta registrada. Identificador: ${cuerpo.idVenta}. Total: Bs ${moneda.format(cuerpo.total)}.`, "success");
            detalle.splice(0); document.getElementById("montoPago").value = ""; renderizarDetalle();
            // Invalida resultados y búsquedas pendientes con stock anterior a la venta.
            const busqueda = document.getElementById("Input_BusquedaProducto");
            busqueda.value = "";
            busqueda.dispatchEvent(new Event("input"));
            try {
                const stocks = await Promise.all(lineasEnviadas.map(async item => {
                    const stock = await consultar(productos.dataset.stockUrl, { id: item.idProducto });
                    if (stock?.id !== item.idProducto || !Number.isInteger(stock.stockDisponible) || stock.stockDisponible < 0)
                        throw new Error("Respuesta de stock inválida.");
                    return `${item.nombre} · ${item.presentacion}: ${stock.stockDisponible}`;
                }));
                estadoStock.className = "small mt-2 text-success";
                estadoStock.textContent = `Stock disponible consultado después de la venta: ${stocks.join("; ")}.`;
            } catch {
                estadoStock.className = "alert alert-warning mt-2";
                estadoStock.textContent = "La venta está registrada. No se pudo actualizar el stock mostrado. Vuelva a buscar los productos para consultar su disponibilidad.";
            }
        } catch (error) { mostrarEstado(error.message, "danger"); }
        finally { registrando = false; actualizarEstado(); }
    });
    document.getElementById("montoPago").addEventListener("input", actualizarEstado);
    actualizarEstado();
})();
