(function () {
    var formulario = document.getElementById('filtrosReporte');
    if (!formulario) {
        return;
    }

    var desde = document.getElementById('Filtros_Desde');
    var hasta = document.getElementById('Filtros_Hasta');
    var top = document.getElementById('Filtros_Top');
    var aviso = document.getElementById('errorRangoFechas');
    var mensajeRango = 'La fecha «Desde» no puede ser posterior a la fecha «Hasta».';
    var mensajeTop = 'El valor de «Top» debe ser un número entero entre 1 y 100.';

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

    function mostrar(mensaje) {
        aviso.textContent = mensaje;
        aviso.hidden = mensaje === '';
        desde.setAttribute('aria-invalid', rangoInvertido() ? 'true' : 'false');
        top.setAttribute('aria-invalid', topInvalido() ? 'true' : 'false');
    }

    function validar() {
        if (rangoInvertido()) {
            mostrar(mensajeRango);
            return false;
        }

        if (topInvalido()) {
            mostrar(mensajeTop);
            return false;
        }

        mostrar('');
        return true;
    }

    [desde, hasta, top].forEach(function (campo) {
        campo.addEventListener('change', validar);
    });

    formulario.addEventListener('submit', function (evento) {
        if (!validar()) {
            evento.preventDefault();
        }
    });
})();
