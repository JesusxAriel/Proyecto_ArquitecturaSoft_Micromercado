const test = require("node:test");
const assert = require("node:assert/strict");
const reglas = require("../../Proyecto_Arquitectura_Micromercado/wwwroot/js/ventas-reglas.js");

test("un cliente completo no tiene errores", () => {
    assert.deepEqual(reglas.validarCliente("123456", "Ana Pérez"), {});
});

test("el CI/NIT y la razón social vacíos o con espacios se rechazan con el mismo mensaje del servidor", () => {
    assert.deepEqual(reglas.validarCliente("   ", ""), {
        nitCi: "Ingrese el CI/NIT.",
        razonSocial: "Ingrese la razón social."
    });
});

test("los límites de longitud se miden sin espacios laterales", () => {
    assert.deepEqual(reglas.validarCliente(` ${"1".repeat(30)} `, ` ${"a".repeat(150)} `), {});
    assert.equal(reglas.validarCliente("1".repeat(31), "Ana").nitCi, "El CI/NIT admite hasta 30 caracteres.");
    assert.equal(reglas.validarCliente("123", "a".repeat(151)).razonSocial, "La razón social admite hasta 150 caracteres.");
});

test("unidadesEnDetalle suma las filas del producto y puede excluir una", () => {
    const a = { idProducto: 1, cantidad: 3 };
    const b = { idProducto: 1, cantidad: 4 };
    const c = { idProducto: 2, cantidad: 9 };
    assert.equal(reglas.unidadesEnDetalle([a, b, c], 1), 7);
    assert.equal(reglas.unidadesEnDetalle([a, b, c], 1, a), 4);
    assert.equal(reglas.unidadesEnDetalle([a, b, c], 3), 0);
});

test("la cantidad dentro del stock es válida, incluido el límite exacto", () => {
    assert.equal(reglas.validarCantidadContraStock("Leche", 10, 4, 6), null);
    assert.equal(reglas.validarCantidadContraStock("Leche", 10, 0, 10), null);
});

test("la cantidad que supera el stock devuelve un mensaje claro con el total", () => {
    assert.equal(
        reglas.validarCantidadContraStock("Leche", 10, 4, 7),
        "La cantidad total de Leche en el detalle (11) supera el stock disponible (10)."
    );
});

test("sin stock conocido no se valida en el cliente", () => {
    assert.equal(reglas.validarCantidadContraStock("Leche", undefined, 0, 999), null);
    assert.equal(reglas.validarCantidadContraStock("Leche", null, 0, 999), null);
    assert.equal(reglas.validarCantidadContraStock("Leche", "abc", 0, 999), null);
});

test("describirLotes lista cada lote con su cantidad", () => {
    assert.equal(
        reglas.describirLotes([{ codigoLote: "LOT-A", cantidad: 5 }, { codigoLote: "LOT-B", cantidad: 3 }]),
        "LOT-A (5), LOT-B (3)"
    );
    assert.equal(reglas.describirLotes([]), "");
    assert.equal(reglas.describirLotes(undefined), "");
});

const leche = { id: 1, nombre: "Leche", presentacion: "Bolsa", precioVenta: 6.5, stockDisponible: 10 };

test("agregarAlDetalle crea una fila con el stock del producto", () => {
    const detalle = [];
    const resultado = reglas.agregarAlDetalle(detalle, leche, 4);
    assert.equal(resultado.fusionado, false);
    assert.equal(detalle.length, 1);
    assert.deepEqual(detalle[0], {
        idProducto: 1, nombre: "Leche", presentacion: "Bolsa", precioUnitario: 6.5,
        stockDisponible: 10, cantidad: 4, subtotal: 26
    });
});

test("agregarAlDetalle junta el mismo producto en una sola fila y suma las cantidades", () => {
    const detalle = [];
    reglas.agregarAlDetalle(detalle, leche, 4);
    const resultado = reglas.agregarAlDetalle(detalle, leche, 5);
    assert.equal(resultado.fusionado, true);
    assert.equal(resultado.total, 9);
    assert.equal(detalle.length, 1);
    assert.equal(detalle[0].cantidad, 9);
    assert.equal(detalle[0].subtotal, 58.5);
});

test("agregarAlDetalle rechaza la suma que supera el stock y no cambia el detalle", () => {
    const detalle = [];
    reglas.agregarAlDetalle(detalle, leche, 4);
    const resultado = reglas.agregarAlDetalle(detalle, leche, 7);
    assert.equal(resultado.error, "La cantidad total de Leche en el detalle (11) supera el stock disponible (10).");
    assert.equal(resultado.campo, "cantidad");
    assert.equal(detalle[0].cantidad, 4);
});

test("agregarAlDetalle rechaza un precio que no es un número", () => {
    const detalle = [];
    const resultado = reglas.agregarAlDetalle(detalle, { ...leche, precioVenta: "x" }, 1);
    assert.equal(resultado.campo, "producto");
    assert.equal(detalle.length, 0);
});

test("modificarCantidad acepta hasta el stock y recalcula el subtotal", () => {
    const detalle = [];
    reglas.agregarAlDetalle(detalle, leche, 4);
    const resultado = reglas.modificarCantidad(detalle, detalle[0], 10);
    assert.equal(resultado.error, undefined);
    assert.equal(detalle[0].cantidad, 10);
    assert.equal(detalle[0].subtotal, 65);
});

test("modificarCantidad rechaza una cantidad mayor al stock y conserva la anterior", () => {
    const detalle = [];
    reglas.agregarAlDetalle(detalle, leche, 4);
    const resultado = reglas.modificarCantidad(detalle, detalle[0], 11);
    assert.equal(resultado.error, "La cantidad total de Leche en el detalle (11) supera el stock disponible (10).");
    assert.equal(detalle[0].cantidad, 4);
    assert.equal(detalle[0].subtotal, 26);
});
