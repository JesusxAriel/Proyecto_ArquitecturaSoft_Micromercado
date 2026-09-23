// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    const isLogin = document.body.dataset.isLogin === 'true';
    if (!isLogin && !localStorage.getItem('puntoCaseroSession')) {
        window.location.replace('/Login');
        return;
    }

    $('#logoutButton').on('click', function () {
        localStorage.removeItem('puntoCaseroSession');
        window.location.replace('/Login');
    });

    const invalidCharacters = /[*%$@{}\[\]?¿\^]/g;
    const validationPattern = /^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.\,\-]+$/;
    const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

    function validateInput(input, showErrors) {
        const value = input.value;
        const error = $(input).siblings('.input-error');
        let message = '';

        if (input.required && !value.trim()) {
            message = 'Este campo es obligatorio.';
        } else if (invalidCharacters.test(value)) {
            input.value = value.replace(invalidCharacters, '');
            message = 'No se permiten caracteres especiales.';
        } else if (input.type === 'email' && input.value && !emailPattern.test(input.value)) {
            message = input.dataset.validationMessage || 'Ingrese un correo electrónico válido con dominio (ejemplo@dominio.com).';
        } else if (input.pattern && input.value && !new RegExp(input.pattern).test(input.value)) {
            message = input.dataset.validationMessage ||
                (input.pattern === '^\\d{7,15}$'
                    ? 'El teléfono debe contener entre 7 y 15 dígitos numéricos.'
                    : 'El formato ingresado no es válido.');
        } else if (input.type === 'number' && input.value && Number.isNaN(Number(input.value))) {
            message = 'Ingresa un número válido.';
        } else if (input.dataset.sanitize === 'text' && input.value && !validationPattern.test(input.value)) {
            message = 'Usa solo letras, números, espacios, puntos, comas o guiones.';
        }

        invalidCharacters.lastIndex = 0;
        input.setCustomValidity(message);

        if (!showErrors) {
            return !message;
        }

        if (message) {
            error.text(message).removeClass('d-none');
            return false;
        }

        error.addClass('d-none').text('');
        return true;
    }

    function validateForm(form, showErrors) {
        let valid = true;
        $(form).find('[data-validate-input]').each(function () {
            valid = validateInput(this, showErrors) && valid;
        });
        if (showErrors) {
            $(form).find('.modal-save-button, button[type="submit"]').prop('disabled', !valid);
        }
        return valid;
    }

    function capitalizeFirstLetter(input) {
        const value = input.value.trim();
        if (value) {
            input.value = value.charAt(0).toLocaleUpperCase() + value.slice(1);
        }
    }

    $('[data-capitalize="true"]').on('blur', function () {
        capitalizeFirstLetter(this);
    });

    $('.modal-form').each(function () {
        const form = this;

        // Validación diferida: no se muestran errores hasta el primer intento
        // de guardar. Después de ese intento, sí se valida en vivo para dar
        // feedback inmediato mientras el usuario corrige.
        $(form).find('[data-validate-input]').on('input blur change', function () {
            if (form.classList.contains('was-validated')) {
                validateInput(this, true);
                validateForm(form, true);
            }
        });

        $(form).on('submit', function (event) {
            $(this).find('[data-capitalize="true"]').each(function () {
                capitalizeFirstLetter(this);
            });

            const valid = validateForm(form, true) && form.checkValidity();
            form.classList.add('was-validated');

            if (!valid) {
                event.preventDefault();
            }
        });
    });
})();
