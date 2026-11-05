const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { iniciar } = require("../frontend/PuntoMarket.Ventas.Frontend/wwwroot/js/reportes-paginacion.js");
const { paginar } = require("../frontend/PuntoMarket.Ventas.Frontend/wwwroot/js/ventas-reglas.js");
function elemento() {
    return { hidden: false, disabled: false, value: "10", textContent: "", eventos: {},
        addEventListener(nombre, fn) { this.eventos[nombre] = fn; },
        activar(nombre) { this.eventos[nombre](); } };
}
function tabla(cantidad) {
    const filas = Array.from({ length: cantidad }, (_, id) => ({ id, hidden: false, textContent: `Venta ${id}` }));
    const nodos = Object.fromEntries(["tamano", "rango", "anterior", "siguiente"].map(x => [x, elemento()]));
    const controles = { hidden: true, querySelector: selector => nodos[selector.slice(6, -1)] };
    const resultados = { querySelectorAll: selector => { assert.equal(selector, "tbody tr"); return filas; },
        querySelector: selector => { assert.equal(selector, "[data-paginacion-reportes]"); return controles; } };
    iniciar(resultados, paginar);
    return { filas, controles, ...nodos, visibles: () => filas.filter(f => !f.hidden) };
}
test("83 registros: inicia con diez, navega y respeta la última página", () => {
    const t = tabla(83);
    assert.equal(t.visibles().length, 10);
    assert.equal(t.anterior.disabled, true);
    assert.equal(t.rango.textContent, "Mostrando 1–10 de 83 registros. Página 1 de 9.");
    for (let i = 0; i < 8; i++) t.siguiente.activar("click");
    assert.deepEqual(t.visibles().map(f => f.id), [80, 81, 82]);
    assert.equal(t.siguiente.disabled, true);
    t.anterior.activar("click");
    assert.equal(t.rango.textContent, "Mostrando 71–80 de 83 registros. Página 8 de 9.");
});
test("seleccionar 25 o 50 reinicia a primera página", () => {
    const t = tabla(83);
    for (const cantidad of [25, 50]) {
        t.siguiente.activar("click");
        t.tamano.value = String(cantidad); t.tamano.activar("change");
        assert.equal(t.visibles().length, cantidad);
        assert.equal(t.visibles()[0].id, 0);
        assert.equal(t.anterior.disabled, true);
    }
});
test("resultados menores al tamaño y resultados vacíos", () => {
    for (const cantidad of [0, 1, 7, 10]) {
        const t = tabla(cantidad);
        assert.equal(t.visibles().length, cantidad);
        assert.equal(t.anterior.disabled, true);
        assert.equal(t.siguiente.disabled, true);
        assert.equal(t.controles.hidden, cantidad === 0);
    }
});
test("una nueva consulta reinicia el estado sin prometer filas adicionales al límite API", () => {
    const anterior = tabla(83); anterior.siguiente.activar("click");
    const nueva = tabla(12);
    assert.equal(nueva.rango.textContent, "Mostrando 1–10 de 12 registros. Página 1 de 2.");
    nueva.siguiente.activar("click");
    assert.equal(nueva.visibles().length, 2);
});
test("paginar conserva todas las filas y sus importes originales", () => {
    const t = tabla(83);
    const contenido = t.filas.map(f => f.textContent);
    t.siguiente.activar("click");
    t.tamano.value = "50"; t.tamano.activar("change");
    assert.equal(t.filas.length, 83);
    assert.deepEqual(t.filas.map(f => f.textContent), contenido);
});
test("ambas vistas mantienen formularios y totales fuera del ámbito paginado", () => {
    for (const nombre of ["Ventas", "Categorias"]) {
        const vista = fs.readFileSync(path.join(__dirname, `../frontend/PuntoMarket.Ventas.Frontend/Pages/Reportes/${nombre}.cshtml`), "utf8");
        const inicio = vista.indexOf('<section data-resultados-reporte');
        assert.ok(inicio > vista.indexOf('</form>'));
        assert.ok(inicio > vista.lastIndexOf('reporte.Totales.'));
        const seccion = vista.slice(inicio, vista.indexOf('</section>', inicio));
        assert.ok(seccion.includes('_PaginacionReportes'));
        assert.ok(!seccion.includes('asp-for="Filtro.'));
        assert.ok(vista.includes('name="handler" value="Pdf"'));
        assert.ok(vista.includes('name="handler" value="Excel"'));
    }
    const parcial = fs.readFileSync(path.join(__dirname, '../frontend/PuntoMarket.Ventas.Frontend/Pages/Shared/_PaginacionReportes.cshtml'), "utf8");
    assert.ok(!parcial.includes('name=')); // Ningún parámetro de paginación llega a filtros o exportaciones.
});
