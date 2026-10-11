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
