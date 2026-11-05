// Ejecuta el script real con un DOM mínimo y HTTP aislado; nunca abre conexiones.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const scripts = path.resolve(__dirname, '../../frontend/PuntoMarket.Ventas.Frontend/wwwroot/js');

function preparar(refrescar) {
    class Elemento {
        constructor() {
            this.value = ''; this.textContent = ''; this.children = []; this.dataset = {};
            this.listeners = {}; this.classList = { add() {} }; this.disabled = false;
        }
        addEventListener(nombre, fn) { this.listeners[nombre] = fn; }
        dispatchEvent(event) { return this.listeners[event.type]?.(event); }
        setAttribute() {}
        append(...nodos) { this.children.push(...nodos); }
        replaceChildren(...nodos) { this.children = nodos; }
        after(nodo) { this.siguiente = nodo; }
        focus() {}
    }
    const elementos = new Map();
    const obtener = id => {
        if (id === 'crearClienteForm') return null;
        if (!elementos.has(id)) elementos.set(id, new Elemento());
        return elementos.get(id);
    };
    obtener('buscadorProductos').dataset = { busquedaUrl: '?handler=BuscarProductos', precioUrl: '?handler=Precio', stockUrl: '?handler=Stock' };
    obtener('buscadorClientes').dataset.busquedaUrl = '?handler=BuscarClientes';
    const temporizadores = [];
    let posts = 0, lecturasStock = 0;
    const producto = { id: 1, nombre: 'Leche', presentacion: '1L', precioVenta: 10, stockDisponible: 8 };
    const respuesta = cuerpo => ({ ok: true, json: async () => cuerpo });
    const contexto = {
        document: { getElementById: obtener, createElement: () => new Elemento(), querySelector: () => ({ value: 'csrf-prueba' }) },
        location: { href: 'http://localhost:5100/Sales' }, URL, Intl, AbortController, Event,
        setTimeout: fn => { temporizadores.push(fn); }, clearTimeout() {},
        fetch: async (url, opciones) => {
            if (opciones.method === 'POST') {
                posts++;
                assert.equal(opciones.headers.RequestVerificationToken, 'csrf-prueba');
                assert.deepEqual(JSON.parse(opciones.body), { idCliente: 3, lineas: [{ idProducto: 1, cantidad: 1, precioEsperado: 10 }] });
                return respuesta({ idVenta: 42, total: 10 });
            }
            const handler = new URL(url).searchParams.get('handler');
            if (handler === 'BuscarProductos') return respuesta({ productos: [producto] });
            if (handler === 'BuscarClientes') return respuesta([{ id: 3, razonSocial: 'Cliente', nitCi: '123' }]);
            if (handler === 'Precio') return respuesta({ id: 1, precioVenta: 10 });
            if (handler === 'Stock') {
                if (++lecturasStock > 1) return refrescar(respuesta);
                return respuesta({ id: 1, stockDisponible: 8 });
            }
            throw new Error(`GET inesperado: ${url}`);
        }
    };
    contexto.window = contexto;
    vm.createContext(contexto);
    for (const archivo of ['ventas-reglas.js', 'ventas.js']) vm.runInContext(fs.readFileSync(path.join(scripts, archivo), 'utf8'), contexto);
    async function seleccionar(entrada, lista) {
        obtener(entrada).value = 'le';
        obtener(entrada).dispatchEvent(new Event('input'));
        await temporizadores.shift()();
        await obtener(lista).children[0].children[0].dispatchEvent(new Event('click'));
    }
    return { obtener, posts: () => posts, seleccionar };
}

for (const falla of [false, true]) {
    test(`venta confirmada conserva éxito y vacía carrito con refresco ${falla ? 'fallido' : 'correcto'}`, async () => {
        let completar;
        const pendiente = new Promise(resolve => { completar = resolve; });
        const app = preparar(async respuesta => {
            await pendiente;
            if (falla) throw new Error('Sin conexión');
            return respuesta({ id: 1, stockDisponible: 7 });
        });
        assert.equal(app.obtener('confirmarVenta').disabled, true);
        await app.seleccionar('Input_BusquedaProducto', 'resultadosProductos');
        await app.seleccionar('Input_IdentificacionCliente', 'resultadosClientes');
        assert.equal(app.obtener('confirmarVenta').disabled, false);
        const envio = app.obtener('confirmarVenta').dispatchEvent(new Event('click'));
        await new Promise(resolve => setImmediate(resolve));
        assert.match(app.obtener('estadoVenta').textContent, /Identificador: 42.*Total: Bs 10/);
        assert.equal(app.obtener('totalGeneral').textContent, '—');
        assert.equal(app.obtener('resultadosProductos').children.length, 0);
        assert.equal(app.obtener('confirmarVenta').disabled, true);
        await app.obtener('confirmarVenta').dispatchEvent(new Event('click'));
        assert.equal(app.posts(), 1);
        completar();
        await envio;
        assert.match(app.obtener('estadoVenta').className, /alert-success/);
        const stock = app.obtener('estadoVenta').siguiente;
        if (falla) {
            assert.match(stock.className, /alert-warning/);
            assert.match(stock.textContent, /venta está registrada.*No se pudo actualizar/);
        } else {
            assert.match(stock.textContent, /Leche · 1L: 7/);
        }
        await app.obtener('confirmarVenta').dispatchEvent(new Event('click'));
        assert.equal(app.posts(), 1);
        assert.equal(app.obtener('confirmarVenta').disabled, true);
    });
}

test('stock inválido no se anuncia como actualizado', async () => {
    const app = preparar(async respuesta => respuesta({ id: 99, stockDisponible: 7 }));
    await app.seleccionar('Input_BusquedaProducto', 'resultadosProductos');
    await app.seleccionar('Input_IdentificacionCliente', 'resultadosClientes');
    await app.obtener('confirmarVenta').dispatchEvent(new Event('click'));
    assert.match(app.obtener('estadoVenta').className, /alert-success/);
    assert.match(app.obtener('estadoVenta').siguiente.className, /alert-warning/);
});
