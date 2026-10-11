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

    quitar.addEventListener("click", () => {
        idProducto.value = "";
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
