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

test("la anulación solo se confirma con los datos cargados, un motivo con texto y sin otro envío en curso", () => {
    assert.equal(reglas.puedeConfirmarAnulacion({ resumenCargado: true, anulando: false, motivo: "Error de digitación" }), true);
    assert.equal(reglas.puedeConfirmarAnulacion({ resumenCargado: false, anulando: false, motivo: "Error de digitación" }), false);
    assert.equal(reglas.puedeConfirmarAnulacion({ resumenCargado: true, anulando: true, motivo: "Error de digitación" }), false);
    assert.equal(reglas.puedeConfirmarAnulacion({ resumenCargado: true, anulando: false, motivo: "   " }), false);
    assert.equal(reglas.puedeConfirmarAnulacion({ resumenCargado: true, anulando: false, motivo: undefined }), false);
});

test("la búsqueda exige al menos dos caracteres sin contar espacios laterales", () => {
    assert.equal(reglas.terminoDeBusquedaValido(""), false);
    assert.equal(reglas.terminoDeBusquedaValido(" g "), false);
    assert.equal(reglas.terminoDeBusquedaValido(undefined), false);
    assert.equal(reglas.terminoDeBusquedaValido("ga"), true);
    assert.equal(reglas.terminoDeBusquedaValido("  ga  "), true);
    assert.equal(reglas.terminoDeBusquedaValido("gallet;slkf'l;alsjf"), true);
    assert.equal(reglas.MENSAJE_BUSQUEDA_CORTA, "Escriba al menos 2 caracteres para buscar.");
});

test("conReintento no repite una operación que funciona", async () => {
    let llamadas = 0;
    const resultado = await reglas.conReintento(async () => { llamadas++; return "ok"; }, 0, async () => {});
    assert.equal(resultado, "ok");
    assert.equal(llamadas, 1);
});

test("conReintento repite una sola vez tras un fallo y devuelve el segundo resultado", async () => {
    let llamadas = 0;
    const pausas = [];
    const resultado = await reglas.conReintento(async () => {
        llamadas++;
        if (llamadas === 1) throw new Error("falló");
        return "segundo";
    }, 250, async (ms) => { pausas.push(ms); });
    assert.equal(resultado, "segundo");
    assert.equal(llamadas, 2);
    assert.deepEqual(pausas, [250]);
});

test("conReintento no hace un tercer intento y propaga el último error", async () => {
    let llamadas = 0;
    await assert.rejects(
        reglas.conReintento(async () => { llamadas++; throw new Error(`falló ${llamadas}`); }, 0, async () => {}),
        { message: "falló 2" }
    );
    assert.equal(llamadas, 2);
});

test("conReintento no reintenta una cancelación", async () => {
    let llamadas = 0;
    const cancelacion = Object.assign(new Error("cancelada"), { name: "AbortError" });
    await assert.rejects(
        reglas.conReintento(async () => { llamadas++; throw cancelacion; }, 0, async () => {}),
        { name: "AbortError" }
    );
    assert.equal(llamadas, 1);
});

test("el ejecutor secuencial nunca solapa operaciones y junta las solicitudes mientras una corre", async () => {
    let simultaneas = 0;
    let maximo = 0;
    let ejecuciones = 0;
    const solicitar = reglas.crearEjecutorSecuencial(async () => {
        simultaneas++;
        maximo = Math.max(maximo, simultaneas);
        ejecuciones++;
        await new Promise((resolver) => setTimeout(resolver, 5));
        simultaneas--;
    });

    await Promise.all(Array.from({ length: 50 }, () => solicitar()));

    assert.equal(maximo, 1);
    assert.equal(ejecuciones, 2);
});

test("el ejecutor secuencial permite una nueva ejecución cuando la anterior terminó", async () => {
    let ejecuciones = 0;
    const solicitar = reglas.crearEjecutorSecuencial(async () => { ejecuciones++; });

    await solicitar();
    await solicitar();

    assert.equal(ejecuciones, 2);
});

test("el ejecutor secuencial se recupera si una operación falla", async () => {
    let ejecuciones = 0;
    const solicitar = reglas.crearEjecutorSecuencial(async () => {
        ejecuciones++;
        if (ejecuciones === 1) throw new Error("falló");
    });

    await assert.rejects(solicitar(), { message: "falló" });
    await solicitar();

    assert.equal(ejecuciones, 2);
});

test("validarEfectivo acepta importes con hasta dos decimales dentro del máximo", () => {
    for (const valor of ["0", "10", "10.5", "10.05", "0.07", "1000000", "999999.99", " 25.00 "]) {
        assert.equal(reglas.validarEfectivo(valor), null, valor);
    }
});

test("validarEfectivo rechaza vacío, negativo, tres decimales y más del máximo con mensajes claros", () => {
    assert.equal(reglas.validarEfectivo(""), "Ingrese un efectivo pagado válido.");
    assert.equal(reglas.validarEfectivo("abc"), "Ingrese un efectivo pagado válido.");
    assert.equal(reglas.validarEfectivo("-1"), "El efectivo pagado no puede ser negativo.");
    assert.equal(reglas.validarEfectivo("10.005"), "El efectivo pagado admite hasta dos decimales.");
    assert.equal(reglas.validarEfectivo("1000000.01"), "El efectivo pagado no puede superar Bs 1.000.000,00.");
    assert.equal(reglas.validarEfectivo("1e12"), "El efectivo pagado no puede superar Bs 1.000.000,00.");
});
