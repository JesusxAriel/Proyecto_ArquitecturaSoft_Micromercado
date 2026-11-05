const test = require("node:test");
const assert = require("node:assert/strict");
const reglas = require("../frontend/PuntoMarket.Ventas.Frontend/wwwroot/js/ventas-reglas.js");

test("validarPagoContraTotal exige cubrir el total en centavos", () => {
    assert.equal(reglas.validarPagoContraTotal(2510, 25.09), "El pago debe ser igual o mayor al total.");
    assert.equal(reglas.validarPagoContraTotal(2510, 25.1), null);
    assert.equal(reglas.validarPagoContraTotal(2510, 30), null);
});

test("calcularVuelto conserva precisión monetaria para pagos decimales", () => {
    assert.deepEqual(reglas.calcularVuelto(2510, 30), { tipo: "vuelto", centavos: 490 });
    assert.deepEqual(reglas.calcularVuelto(2510, 25.1), { tipo: "vuelto", centavos: 0 });
});
