(() => {
    "use strict";

    const seccion = document.getElementById("historialVentasSeccion");
    if (!seccion) return;

    const TAMANO_PAGINA = 10;

    const cuerpo = document.getElementById("historialVentas");
    const estado = document.getElementById("estadoHistorial");
    const actualizar = document.getElementById("actualizarHistorial");
    const mostrarAnuladas = document.getElementById("mostrarAnuladas");
    const paginacion = document.getElementById("paginacionHistorial");
    const rango = document.getElementById("rangoHistorial");
    const paginaAnterior = document.getElementById("paginaAnterior");
    const paginaSiguiente = document.getElementById("paginaSiguiente");
    const aviso = document.getElementById("avisoHistorial");
    const puedeAnular = seccion.dataset.puedeAnular === "true";
    const precio = new Intl.NumberFormat("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const modalDetalle = document.getElementById("detalleVentaModal");
    const detalleError = document.getElementById("detalleVentaError");
    const detalleComprobante = document.getElementById("detalleVentaComprobante");
    const detalleCliente = document.getElementById("detalleVentaCliente");
    const detalleFecha = document.getElementById("detalleVentaFecha");
    const detalleUsuario = document.getElementById("detalleVentaUsuario");
    const detalleEstado = document.getElementById("detalleVentaEstado");
    const detalleProductos = document.getElementById("detalleVentaProductos");
    const detalleTotal = document.getElementById("detalleVentaTotal");
    const detalleAnulacion = document.getElementById("detalleVentaAnulacion");
    const detalleMotivo = document.getElementById("detalleVentaMotivo");
    const detalleUsuarioAnulacion = document.getElementById("detalleVentaUsuarioAnulacion");
    const detalleFechaAnulacion = document.getElementById("detalleVentaFechaAnulacion");
    const detalleEnlaceComprobante = document.getElementById("detalleVentaEnlaceComprobante");
    const detalleModal = bootstrap.Modal.getOrCreateInstance(modalDetalle);
    const ventasPorId = new Map();
    let ventas = [];
    let paginaActual = 1;
    let versionDetalle = 0;

    function dosDigitos(numero) {
        return String(numero).padStart(2, "0");
    }

    function formatearFecha(valor, conSegundos = false) {
        const fecha = new Date(valor);
        if (Number.isNaN(fecha.getTime())) return "";
        const segundos = conSegundos ? `:${dosDigitos(fecha.getSeconds())}` : "";
        return `${dosDigitos(fecha.getDate())}/${dosDigitos(fecha.getMonth() + 1)}/${fecha.getFullYear()} ${dosDigitos(fecha.getHours())}:${dosDigitos(fecha.getMinutes())}${segundos}`;
    }

    function urlDeComprobante(idVenta) {
        return `${seccion.dataset.comprobanteBase}${idVenta}`;
    }

    function crearBadgeEstado(valor) {
        const anulada = valor === "ANULADA";
        const badge = document.createElement("span");
        badge.className = `badge bg-${anulada ? "danger" : "success"}`;
        badge.textContent = anulada ? "Anulada" : "Completada";
        return badge;
    }

    function crearFilaMensaje(mensaje, icono) {
        const fila = document.createElement("tr");
        const celda = document.createElement("td");
        celda.colSpan = 6;
        celda.className = "text-center py-4";
        const marca = document.createElement("i");
        marca.className = `bi ${icono} d-block fs-2 text-muted mb-2`;
        marca.setAttribute("aria-hidden", "true");
        const texto = document.createElement("p");
        texto.className = "text-muted mb-0";
        texto.textContent = mensaje;
        celda.append(marca, texto);
        fila.append(celda);
        return fila;
    }

    function crearBotonAccion(texto, accion, venta, clase) {
        const boton = document.createElement("button");
        boton.type = "button";
        boton.className = `btn ${clase} btn-sm`;
        boton.textContent = texto;
        boton.dataset.accion = accion;
        boton.dataset.idVenta = String(venta.id);
        boton.setAttribute("aria-label", accion === "anular"
            ? `Anular la venta ${venta.nroComprobante}`
            : `Ver detalle de la venta ${venta.nroComprobante}`);
        return boton;
    }

    function crearEnlaceComprobante(venta) {
        const enlace = document.createElement("a");
        enlace.className = "btn btn-outline-secondary btn-sm";
        enlace.href = urlDeComprobante(venta.id);
        enlace.target = "_blank";
        enlace.rel = "noopener";
        enlace.textContent = "Comprobante";
        enlace.setAttribute("aria-label", `Abrir el comprobante de la venta ${venta.nroComprobante}`);
        return enlace;
    }

    function renderizarPagina() {
        const pagina = VentasReglas.paginar(ventas, paginaActual, TAMANO_PAGINA);
        paginaActual = pagina.pagina;
        cuerpo.replaceChildren();
        if (ventas.length === 0) {
            paginacion.hidden = true;
            cuerpo.append(crearFilaMensaje(
                mostrarAnuladas.checked
                    ? "No hay ventas registradas."
                    : "No hay ventas completadas para mostrar.",
                "bi-receipt"));
            return;
        }

        pagina.items.forEach((venta) => {
            const fila = document.createElement("tr");
            [
                [venta.nroComprobante, "text-nowrap"],
                [formatearFecha(venta.fechaHora), "text-nowrap"],
                [venta.nombreCliente, "text-break"]
            ].forEach(([valor, clase]) => {
                const celda = document.createElement("td");
                celda.className = clase;
                celda.textContent = valor;
                fila.append(celda);
            });

            const celdaEstado = document.createElement("td");
            celdaEstado.append(crearBadgeEstado(venta.estado));

            const celdaTotal = document.createElement("td");
            celdaTotal.className = "text-end text-nowrap";
            celdaTotal.textContent = `Bs ${precio.format(venta.total)}`;

            const acciones = document.createElement("td");
            acciones.className = "text-end text-nowrap";
            acciones.append(crearBotonAccion("Ver detalle", "detalle", venta, "btn-outline-primary"), " ", crearEnlaceComprobante(venta));
            if (VentasReglas.puedeMostrarAnular(puedeAnular, venta.estado)) {
                acciones.append(" ", crearBotonAccion("Anular", "anular", venta, "btn-outline-danger"));
            }

            fila.append(celdaEstado, celdaTotal, acciones);
            cuerpo.append(fila);
        });

        paginacion.hidden = pagina.totalPaginas <= 1;
        rango.textContent = `Mostrando ${pagina.desde}–${pagina.hasta} de ${pagina.total} ventas. Página ${pagina.pagina} de ${pagina.totalPaginas}.`;
        paginaAnterior.disabled = pagina.pagina <= 1;
        paginaSiguiente.disabled = pagina.pagina >= pagina.totalPaginas;
    }

    function cambiarPagina(diferencia) {
        paginaActual += diferencia;
        renderizarPagina();
    }

    paginaAnterior.addEventListener("click", () => cambiarPagina(-1));
    paginaSiguiente.addEventListener("click", () => cambiarPagina(1));

    async function consultarHistorial() {
        cuerpo.setAttribute("aria-busy", "true");
        actualizar.disabled = true;
        estado.textContent = "Cargando historial…";

        try {
            const url = new URL(seccion.dataset.historialUrl, window.location.href);
            url.searchParams.set("incluirAnuladas", String(mostrarAnuladas.checked));
            const respuesta = await VentasReglas.fetchConSesion(url, {
                headers: { Accept: "application/json" },
                cache: "no-store"
            });
            if (!respuesta.ok) throw new Error("No se pudo cargar el historial de ventas.");
            const datos = await respuesta.json();

            ventas = datos.ventas;
            ventasPorId.clear();
            ventas.forEach((venta) => ventasPorId.set(venta.id, venta));
            renderizarPagina();
            estado.textContent = ventas.length
                ? `${ventas.length} venta(s) en el historial.`
                : "No hay ventas para mostrar.";
        } catch {
            ventas = [];
            paginacion.hidden = true;
            cuerpo.replaceChildren(crearFilaMensaje(
                "No se pudo cargar el historial de ventas. Intente nuevamente.",
                "bi-exclamation-triangle"));
            estado.textContent = "No se pudo cargar el historial de ventas.";
        } finally {
            cuerpo.setAttribute("aria-busy", "false");
            actualizar.disabled = false;
        }
    }

    const cargarHistorial = VentasReglas.crearEjecutorSecuencial(consultarHistorial);

    function crearFilaDetalle(mensaje) {
        const fila = document.createElement("tr");
        const celda = document.createElement("td");
        celda.colSpan = 5;
        celda.className = "text-center text-muted py-4";
        celda.textContent = mensaje;
        fila.append(celda);
        return fila;
    }

    function llenarLineas(cuerpoTabla, lineas) {
        cuerpoTabla.replaceChildren();
        lineas.forEach((linea) => {
            const fila = document.createElement("tr");
            [
                [String(linea.cantidad), "text-center"],
                [linea.descripcion, "text-break"],
                [VentasReglas.describirLotes(linea.lotes), "text-break"],
                [precio.format(linea.precioUnitario), "text-end text-nowrap"],
                [precio.format(linea.subtotal), "text-end text-nowrap"]
            ].forEach(([valor, clase]) => {
                const celda = document.createElement("td");
                celda.className = clase;
                celda.textContent = valor;
                fila.append(celda);
            });
            cuerpoTabla.append(fila);
        });
    }

    function limpiarDetalle() {
        detalleError.textContent = "";
        detalleError.hidden = true;
        detalleAnulacion.hidden = true;
        detalleEnlaceComprobante.hidden = true;
        [detalleComprobante, detalleCliente, detalleFecha, detalleUsuario, detalleTotal]
            .forEach((elemento) => { elemento.textContent = "Cargando…"; });
        detalleEstado.textContent = "Cargando…";
        detalleEstado.className = "badge bg-secondary";
        detalleProductos.replaceChildren(crearFilaDetalle("Cargando detalle…"));
    }

    function mostrarErrorDetalle(mensaje) {
        limpiarDetalle();
        [detalleComprobante, detalleCliente, detalleFecha, detalleUsuario, detalleTotal]
            .forEach((elemento) => { elemento.textContent = "—"; });
        detalleEstado.textContent = "—";
        detalleProductos.replaceChildren(crearFilaDetalle("No hay productos para mostrar."));
        detalleError.textContent = mensaje;
        detalleError.hidden = false;
    }

    function mostrarDetalle(detalle) {
        const anulada = detalle.estado === "ANULADA";
        detalleError.hidden = true;
        detalleComprobante.textContent = detalle.nroComprobante;
        detalleCliente.textContent = `${detalle.razonSocialCliente} (CI/NIT: ${detalle.nitCiCliente})`;
        detalleFecha.textContent = formatearFecha(detalle.fechaHora, true);
        detalleUsuario.textContent = detalle.usuarioCajero;
        detalleEstado.textContent = anulada ? "Anulada" : "Completada";
        detalleEstado.className = `badge bg-${anulada ? "danger" : "success"}`;
        detalleTotal.textContent = precio.format(detalle.total);
        detalleEnlaceComprobante.href = urlDeComprobante(detalle.id);
        detalleEnlaceComprobante.hidden = false;

        llenarLineas(detalleProductos, detalle.lineas);

        detalleAnulacion.hidden = !anulada;
        if (anulada) {
            detalleMotivo.textContent = detalle.motivoAnulacion ?? "";
            detalleUsuarioAnulacion.textContent = detalle.usuarioAnulacion ?? "";
            detalleFechaAnulacion.textContent = detalle.fechaAnulacion ? formatearFecha(detalle.fechaAnulacion, true) : "";
        }
    }

    async function obtenerDetalle(idVenta) {
        const url = new URL(seccion.dataset.detalleUrl, window.location.href);
        url.searchParams.set("idVenta", String(idVenta));
        const respuesta = await VentasReglas.fetchConSesion(url, {
            headers: { Accept: "application/json" },
            cache: "no-store"
        });
        const datos = await respuesta.json().catch(() => null);
        return { respuesta, datos };
    }

    async function abrirDetalle(idVenta) {
        const actual = ++versionDetalle;
        limpiarDetalle();
        detalleModal.show();

        try {
            const { respuesta, datos } = await obtenerDetalle(idVenta);
            if (actual !== versionDetalle) return;
            if (!respuesta.ok) {
                throw new Error(datos?.mensaje || "No se pudo cargar el detalle de la venta. Intente nuevamente.");
            }

            mostrarDetalle(datos);
        } catch (error) {
            if (actual !== versionDetalle) return;
            mostrarErrorDetalle(error instanceof TypeError
                ? "No se pudo conectar con el servidor. Intente nuevamente."
                : error.message);
        }
    }

    modalDetalle.addEventListener("hidden.bs.modal", () => {
        versionDetalle++;
    });

    function mostrarAviso(mensaje) {
        aviso.textContent = mensaje;
        aviso.hidden = false;
    }

    const modalAnulacion = document.getElementById("anularVentaModal");
    let abrirAnulacion = () => {};

    if (modalAnulacion && puedeAnular) {
        const anulacionCarga = document.getElementById("anularVentaCarga");
        const anulacionErrorDetalle = document.getElementById("anularVentaErrorDetalle");
        const anulacionResumen = document.getElementById("anularVentaResumen");
        const anulacionComprobante = document.getElementById("anularVentaComprobante");
        const anulacionFecha = document.getElementById("anularVentaFecha");
        const anulacionCliente = document.getElementById("anularVentaCliente");
        const anulacionUsuario = document.getElementById("anularVentaUsuario");
        const anulacionProductos = document.getElementById("anularVentaProductos");
        const anulacionTotal = document.getElementById("anularVentaTotal");
        const anulacionMotivo = document.getElementById("anularVentaMotivo");
        const anulacionError = document.getElementById("anularVentaError");
        const confirmarAnulacion = document.getElementById("confirmarAnulacionVenta");
        const anulacionModal = bootstrap.Modal.getOrCreateInstance(modalAnulacion);
        const antiforgery = document.querySelector('input[name="__RequestVerificationToken"]');
        let ventaEnAnulacion = null;
        let resumenAnulacionCargado = false;
        let versionAnulacion = 0;
        let anulando = false;

        function mostrarErrorAnulacion(mensaje) {
            anulacionError.textContent = mensaje;
            anulacionError.hidden = !mensaje;
        }

        function actualizarBotonAnulacion() {
            confirmarAnulacion.disabled = !VentasReglas.puedeConfirmarAnulacion({
                resumenCargado: resumenAnulacionCargado,
                anulando,
                motivo: anulacionMotivo.value
            });
        }

        function mostrarResumenAnulacion(datos) {
            anulacionComprobante.textContent = datos.nroComprobante;
            anulacionFecha.textContent = formatearFecha(datos.fechaHora, true);
            anulacionCliente.textContent = `${datos.razonSocialCliente} (CI/NIT: ${datos.nitCiCliente})`;
            anulacionUsuario.textContent = datos.usuarioCajero;
            llenarLineas(anulacionProductos, datos.lineas);
            anulacionTotal.textContent = precio.format(datos.total);
            anulacionResumen.hidden = false;
        }

        abrirAnulacion = async (idVenta) => {
            const venta = ventasPorId.get(idVenta);
            if (!venta) return;

            const actual = ++versionAnulacion;
            ventaEnAnulacion = venta;
            resumenAnulacionCargado = false;
            aviso.hidden = true;
            anulacionMotivo.value = "";
            mostrarErrorAnulacion("");
            anulacionCarga.hidden = false;
            anulacionErrorDetalle.hidden = true;
            anulacionResumen.hidden = true;
            actualizarBotonAnulacion();
            anulacionModal.show();

            try {
                const { respuesta, datos } = await obtenerDetalle(idVenta);
                if (actual !== versionAnulacion) return;
                if (!respuesta.ok) {
                    throw new Error(datos?.mensaje || "No se pudieron cargar los datos de la venta. Intente nuevamente.");
                }
                if (datos.estado !== "COMPLETADA") {
                    throw new Error("La venta ya fue anulada.");
                }

                mostrarResumenAnulacion(datos);
                resumenAnulacionCargado = true;
            } catch (error) {
                if (actual !== versionAnulacion) return;
                anulacionErrorDetalle.textContent = error instanceof TypeError
                    ? "No se pudo conectar con el servidor. Intente nuevamente."
                    : error.message;
                anulacionErrorDetalle.hidden = false;
            } finally {
                if (actual === versionAnulacion) {
                    anulacionCarga.hidden = true;
                    actualizarBotonAnulacion();
                }
            }
        };

        async function anularVenta() {
            if (anulando || !ventaEnAnulacion) return;

            const motivo = anulacionMotivo.value.trim();
            if (!motivo) return;

            const venta = ventaEnAnulacion;
            anulando = true;
            confirmarAnulacion.textContent = "Anulando…";
            mostrarErrorAnulacion("");
            actualizarBotonAnulacion();

            try {
                const respuesta = await VentasReglas.fetchConSesion(seccion.dataset.anularUrl, {
                    method: "POST",
                    credentials: "same-origin",
                    headers: {
                        "Content-Type": "application/json",
                        Accept: "application/json",
                        RequestVerificationToken: antiforgery.value
                    },
                    body: JSON.stringify({ idVenta: venta.id, motivo })
                });
                const resultado = await respuesta.json().catch(() => null);
                if (!respuesta.ok || !resultado?.exito) {
                    throw new Error(resultado?.mensaje || (respuesta.status === 400
                        ? "No se pudo validar la solicitud. Revise los datos o recargue la página si su sesión expiró."
                        : "No se pudo anular la venta. Revise su sesión e intente nuevamente."));
                }

                anulando = false;
                anulacionModal.hide();
                mostrarAviso(`La venta ${venta.nroComprobante} fue anulada correctamente.`);
                cargarHistorial();
            } catch (error) {
                mostrarErrorAnulacion(error instanceof TypeError
                    ? "No se pudo conectar con el servidor. La venta no fue anulada."
                    : error.message);
            } finally {
                anulando = false;
                confirmarAnulacion.textContent = "Confirmar anulación";
                actualizarBotonAnulacion();
            }
        }

        anulacionMotivo.addEventListener("input", actualizarBotonAnulacion);
        confirmarAnulacion.addEventListener("click", anularVenta);
        modalAnulacion.addEventListener("shown.bs.modal", () => anulacionMotivo.focus());
        modalAnulacion.addEventListener("hide.bs.modal", (event) => {
            if (anulando) event.preventDefault();
        });
        modalAnulacion.addEventListener("hidden.bs.modal", () => {
            versionAnulacion++;
            ventaEnAnulacion = null;
            resumenAnulacionCargado = false;
            anulacionResumen.hidden = true;
            anulacionErrorDetalle.hidden = true;
            anulacionMotivo.value = "";
            mostrarErrorAnulacion("");
            actualizarBotonAnulacion();
        });
    }

    cuerpo.addEventListener("click", (event) => {
        const boton = event.target.closest("button[data-accion]");
        if (!boton) return;

        const idVenta = Number(boton.dataset.idVenta);
        if (boton.dataset.accion === "detalle") abrirDetalle(idVenta);
        if (boton.dataset.accion === "anular") abrirAnulacion(idVenta);
    });

    actualizar.addEventListener("click", cargarHistorial);
    mostrarAnuladas.addEventListener("change", () => {
        paginaActual = 1;
        cargarHistorial();
    });

    cargarHistorial();
})();
