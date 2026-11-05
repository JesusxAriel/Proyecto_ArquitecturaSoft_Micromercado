(function () {
    var formulario = document.getElementById('filtrosReporte');
    if (!formulario) {
        return;
    }

    var desde = document.getElementById('Filtros_Desde');
    var hasta = document.getElementById('Filtros_Hasta');
    var top = document.getElementById('Filtros_Top');
    var avisoRango = document.getElementById('errorRangoFechas');
    var avisoTop = document.querySelector('[data-valmsg-for="Filtros.Top"]');
    var mensajeRango = 'La fecha «Desde» no puede ser posterior a la fecha «Hasta».';
    var mensajeTop = 'El valor de «Máximo de categorías» debe ser un número entero entre 1 y 100.';

    function rangoInvertido() {
        return desde.value !== '' && hasta.value !== '' && desde.value > hasta.value;
    }

    function topInvalido() {
        if (top.value === '') {
            return false;
        }

        var valor = Number(top.value);
        return !Number.isInteger(valor) || valor < 1 || valor > 100;
    }

    function mostrarErrorDeRango(mensaje) {
        avisoRango.textContent = mensaje;
        avisoRango.hidden = mensaje === '';
        desde.setAttribute('aria-invalid', mensaje === '' ? 'false' : 'true');
    }

    function mostrarErrorDeTop(mensaje) {
        avisoTop.textContent = mensaje;
        avisoTop.className = mensaje === ''
            ? 'text-danger field-validation-valid'
            : 'text-danger field-validation-error';
        top.setAttribute('aria-invalid', mensaje === '' ? 'false' : 'true');
    }

    function validar() {
        var rangoValido = !rangoInvertido();
        var topValido = !topInvalido();

        mostrarErrorDeRango(rangoValido ? '' : mensajeRango);
        mostrarErrorDeTop(topValido ? '' : mensajeTop);

        return rangoValido && topValido;
    }

    [desde, hasta, top].forEach(function (campo) {
        campo.addEventListener('change', validar);
    });

    formulario.addEventListener('submit', function (evento) {
        if (!validar()) {
            evento.preventDefault();
            evento.stopImmediatePropagation();
        }
    });
})();
