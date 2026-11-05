(function (raiz) {
    "use strict";
    // Solo cambia visibilidad de las filas ya recibidas. No toca formularios, totales ni API.
    function iniciar(resultados, paginar) {
        const filas = Array.from(resultados.querySelectorAll("tbody tr"));
        const controles = resultados.querySelector("[data-paginacion-reportes]");
        const tamano = controles.querySelector("[data-tamano]");
        const rango = controles.querySelector("[data-rango]");
        const anterior = controles.querySelector("[data-anterior]");
        const siguiente = controles.querySelector("[data-siguiente]");
        let paginaActual = 1;
        tamano.value = "10";
        function mostrar() {
            const cantidad = [10, 25, 50].includes(Number(tamano.value)) ? Number(tamano.value) : 10;
            const pagina = paginar(filas, paginaActual, cantidad);
            paginaActual = pagina.pagina;
            filas.forEach((fila, indice) => { fila.hidden = indice < pagina.desde - 1 || indice >= pagina.hasta; });
            controles.hidden = filas.length === 0;
            rango.textContent = `Mostrando ${pagina.desde}–${pagina.hasta} de ${pagina.total} registros. Página ${pagina.pagina} de ${pagina.totalPaginas}.`;
            anterior.disabled = pagina.pagina <= 1;
            siguiente.disabled = pagina.pagina >= pagina.totalPaginas;
        }
        anterior.addEventListener("click", () => { paginaActual--; mostrar(); });
        siguiente.addEventListener("click", () => { paginaActual++; mostrar(); });
        tamano.addEventListener("change", () => { paginaActual = 1; mostrar(); });
        mostrar();
    }
    if (typeof module !== "undefined" && module.exports) module.exports = { iniciar };
    else document.querySelectorAll("[data-resultados-reporte]").forEach(resultados => iniciar(resultados, raiz.VentasReglas.paginar));
})(typeof window !== "undefined" ? window : globalThis);
