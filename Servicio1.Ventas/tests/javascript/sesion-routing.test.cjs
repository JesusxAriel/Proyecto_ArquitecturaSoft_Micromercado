const test = require("node:test");
const assert = require("node:assert/strict");
const reglas = require("../../frontend/PuntoMarket.Ventas.Frontend/wwwroot/js/ventas-reglas.js");
test("la entrada única es Account/Login e ignora destinos ajenos", () => {
    const ubicacion = { pathname: "/Sales/Historial", search: "?filtro=x" };
    assert.ok(reglas.urlDeLogin(ubicacion).startsWith("/Account/Login?"));
    assert.ok(reglas.urlDeLogin(ubicacion, "/Account/Login").startsWith("/Account/Login?"));
    assert.ok(reglas.urlDeLogin(ubicacion, "https://example.invalid").startsWith("/Account/Login?"));
    assert.ok(reglas.urlDeLogin(ubicacion).includes(encodeURIComponent("/Sales/Historial?filtro=x")));
});
test("401 redirige al mecanismo indicado sin repetir el POST", async () => {
    let llamadas = 0, destino, expiraciones = 0;
    const respuesta = { status: 401, headers: { get: nombre => nombre === "X-PuntoMarket-Login" ? "/Account/Login" : null } };
    reglas.fetchConSesion("?handler=Operacion", { method: "POST" }, () => expiraciones++, async () => { llamadas++; return respuesta; },
        { pathname: "/Sales", search: "", assign: url => { destino = url; } });
    await new Promise(setImmediate);
    assert.equal(llamadas, 1); assert.equal(expiraciones, 1);
    assert.ok(destino.startsWith("/Account/Login?"));
});
test("403 no cambia de identidad ni redirige", async () => {
    let navegaciones = 0;
    const respuesta = { status: 403 };
    const resultado = await reglas.fetchConSesion("?handler=Operacion", {}, () => { throw Error("No expirar"); }, async () => respuesta,
        { assign: () => navegaciones++ });
    assert.equal(resultado, respuesta); assert.equal(navegaciones, 0);
});
