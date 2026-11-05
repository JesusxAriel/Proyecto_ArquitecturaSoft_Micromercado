// DOM y HTTP aislados. Ejecuta historial.js y ventas-reglas.js reales, sin red.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const scripts = path.resolve(__dirname, '../../frontend/PuntoMarket.Ventas.Frontend/wwwroot/js');
const esperar = () => new Promise(resolve => setImmediate(resolve));

async function preparar({ administrador = true, estadoInicial = 'COMPLETADA', enviar } = {}) {
    class Elemento {
        constructor() { this.children = []; this.listeners = {}; this.dataset = {}; this.value = ''; this.textContent = ''; }
        addEventListener(nombre, callback) { this.listeners[nombre] = callback; }
        dispatch(nombre, evento = {}) { return this.listeners[nombre]?.(evento); }
        append(...hijos) { this.children.push(...hijos); }
        replaceChildren(...hijos) { this.children = hijos; }
        setAttribute() {}
        after(nodo) { this.siguiente = nodo; }
        focus() {}
    }
    const elementos = new Map();
    const obtener = id => {
        if (!administrador && id === 'anularVentaModal') return null;
        if (!elementos.has(id)) elementos.set(id, new Elemento());
        return elementos.get(id);
    };
    obtener('historialVentasSeccion').dataset = {
        puedeAnular: String(administrador), historialUrl: '?handler=Historial', detalleUrl: '?handler=Detalle',
        anularUrl: '?handler=AnularVenta', comprobanteBase: '/Comprobante/'
    };
    let estado = estadoInicial, posts = 0;
    const peticiones = [];
    const respuesta = (datos, status = 200) => ({ ok: status < 400, status, json: async () => datos });
    const contexto = {
        document: { getElementById: obtener, createElement: () => new Elemento(), querySelector: () => ({ value: 'csrf-prueba' }) },
        location: { href: 'http://localhost:5100/Sales/Historial', assign() {} }, URL, Intl,
        bootstrap: { Modal: { getOrCreateInstance: nodo => ({
            show() { nodo.dispatch('shown.bs.modal'); },
            hide() { nodo.dispatch('hidden.bs.modal'); }
        }) } },
        fetch: async (url, opciones = {}) => {
            if (opciones.method === 'POST') {
                posts++;
                peticiones.push(opciones);
                if (enviar) return enviar(respuesta);
                estado = 'ANULADA';
                return respuesta({ exito: true });
            }
            const handler = new URL(url).searchParams.get('handler');
            if (handler === 'Historial') return respuesta({ ventas: [{ id: 2, nroComprobante: 'V-2', estado,
                fechaHora: '2026-11-04T10:00:00', nombreCliente: 'Cliente', total: 20 }] });
            assert.equal(handler, 'Detalle');
            return respuesta({ id: 2, nroComprobante: 'V-2', estado, lineas: [], total: 20 });
        }
    };
    contexto.window = contexto;
    vm.createContext(contexto);
    for (const archivo of ['ventas-reglas.js', 'historial.js']) vm.runInContext(fs.readFileSync(path.join(scripts, archivo), 'utf8'), contexto);
    await esperar();
    function botones() {
        const resultado = [];
        function visitar(nodo) {
            if (nodo?.dataset?.accion) resultado.push(nodo);
            nodo?.children?.forEach(visitar);
        }
        visitar(obtener('historialVentas'));
        return resultado;
    }
    async function abrir() {
        const boton = botones().find(b => b.dataset.accion === 'anular');
        assert.ok(boton);
        obtener('historialVentas').dispatch('click', { target: { closest: () => boton } });
        await esperar();
    }
    function motivo(valor) {
        obtener('anularVentaMotivo').value = valor;
        obtener('anularVentaMotivo').dispatch('input');
    }
    return { obtener, botones, abrir, motivo, peticiones, posts: () => posts, cambiarEstado: valor => { estado = valor; } };
}

test('Administrador envía idVenta y motivo, conserva antiforgery y bloquea doble clic', async () => {
    let completar;
    const app = await preparar({ enviar: respuesta => new Promise(resolve => { completar = () => resolve(respuesta({ exito: true })); }) });
    await app.abrir();
    assert.equal(app.obtener('confirmarAnulacionVenta').disabled, true);
    await app.obtener('confirmarAnulacionVenta').dispatch('click');
    assert.equal(app.posts(), 0);
    app.motivo('   Corrección   ');
    const envio = app.obtener('confirmarAnulacionVenta').dispatch('click');
    assert.equal(app.obtener('confirmarAnulacionVenta').disabled, true);
    await app.obtener('confirmarAnulacionVenta').dispatch('click');
    assert.equal(app.posts(), 1);
    assert.deepEqual(JSON.parse(app.peticiones[0].body), { idVenta: 2, motivo: 'Corrección' });
    assert.equal(app.peticiones[0].headers.RequestVerificationToken, 'csrf-prueba');
    assert.equal(app.peticiones[0].credentials, 'same-origin');
    completar();
    await envio;
    assert.match(app.obtener('avisoHistorial').textContent, /anulada correctamente/);
});

for (const fallo of ['red', 'timeout', '503']) {
    test(`resultado desconocido ante ${fallo}: requiere consultar detalle antes de reintentar`, async () => {
        const app = await preparar({ enviar: respuesta => {
            if (fallo === '503') return respuesta({ mensaje: 'No disponible' }, 503);
            const error = new Error('Conexión interrumpida');
            error.name = fallo === 'timeout' ? 'TimeoutError' : 'TypeError';
            throw error;
        } });
        await app.abrir(); app.motivo('Error');
        await app.obtener('confirmarAnulacionVenta').dispatch('click');
        const error = app.obtener('anularVentaError');
        assert.match(error.textContent, /desconocido/);
        assert.doesNotMatch(error.textContent, /no fue anulada|anulación falló/i);
        assert.equal(app.obtener('confirmarAnulacionVenta').disabled, true);
        await app.obtener('confirmarAnulacionVenta').dispatch('click');
        assert.equal(app.posts(), 1);
        await error.siguiente.dispatch('click');
        assert.equal(app.obtener('confirmarAnulacionVenta').disabled, false);
        assert.equal(app.posts(), 1);
    });
}

test('consulta posterior descubre ANULADA sin enviar otro POST', async () => {
    const app = await preparar({ enviar: () => { throw new Error('Respuesta perdida'); } });
    await app.abrir(); app.motivo('Error');
    await app.obtener('confirmarAnulacionVenta').dispatch('click');
    app.cambiarEstado('ANULADA');
    await app.obtener('anularVentaError').siguiente.dispatch('click');
    await esperar();
    assert.match(app.obtener('avisoHistorial').textContent, /figura como ANULADA/);
    assert.equal(app.posts(), 1);
    assert.equal(app.botones().some(b => b.dataset.accion === 'anular'), false);
});

test('Cajero y ventas ANULADA no ofrecen botón Anular', async () => {
    for (const opciones of [{ administrador: false }, { estadoInicial: 'ANULADA' }]) {
        const app = await preparar(opciones);
        assert.equal(app.botones().some(b => b.dataset.accion === 'anular'), false);
        assert.equal(app.posts(), 0);
    }
});
