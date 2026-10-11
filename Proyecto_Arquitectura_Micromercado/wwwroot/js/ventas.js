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

        if (detalleTemporal.length === 1) tablaDetalle.replaceChildren();
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
        tablaDetalle.append(fila);
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
