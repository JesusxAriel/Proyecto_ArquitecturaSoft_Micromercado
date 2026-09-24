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
    const categorySpacingAttributes = [
        'data-category-name',
        'data-category-code',
        'data-category-aisle',
        'data-category-description'
    ];
    const lowerConnectorWords = ['y', 'de', 'del', 'la', 'las', 'el', 'los', 'en'];

    function hasCategorySpacingRules(input) {
        return categorySpacingAttributes.some(attribute => input.hasAttribute(attribute));
    }

    function normalizeSpaces(value) {
        return value.replace(/\s+/g, ' ');
    }

    function toTitleCaseWords(value) {
        return value
            .toLowerCase()
            .split(' ')
            .map((word, index) => (index > 0 && lowerConnectorWords.includes(word))
                ? word
                : word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }

    function validateCategorySpacing(input, value) {
        if (/^\s/.test(value)) {
            return 'No se permiten espacios al inicio.';
        }

        if (input.hasAttribute('data-category-code')) {
            return /\s/.test(value) ? 'El código no puede contener espacios.' : '';
        }

        return /\s{2,}/.test(value) ? 'No se permiten espacios dobles.' : '';
    }

    function validateInput(input, showErrors) {
        const value = input.value;
        const error = hasCategorySpacingRules(input) ? $(input).closest('.mb-3').find('.input-error').first() : $(input).siblings('.input-error');
        const spacingMessage = hasCategorySpacingRules(input) ? validateCategorySpacing(input, value) : '';
        let message = '';

        if (input.required && !value.trim()) {
            message = 'Este campo es obligatorio.';
        } else if (spacingMessage) {
            message = spacingMessage;
        } else if (input.type !== 'email' && invalidCharacters.test(value)) {
            input.value = value.replace(invalidCharacters, '');
            message = 'No se permiten caracteres especiales.';
        } else if (input.type === 'email' && input.value && !emailPattern.test(input.value)) {
            message = input.dataset.validationMessage || 'Ingrese un correo electrónico válido con dominio (ejemplo@dominio.com).';
        } else if (input.pattern && input.value && !new RegExp(input.pattern).test(input.value)) {
            message = input.dataset.validationMessage ||
                (input.id === 'createCategoryCode' || input.id === 'editCategoryCode'
                    ? 'El código debe tener exactamente 3 letras. Ej: LAC.'
                    : input.pattern === '^\\d{7,15}$'
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

    $(categorySpacingAttributes.map(attribute => `[${attribute}]`).join(', ')).on('blur', function () {
        let value = normalizeSpaces(this.value.trim());

        if (this.hasAttribute('data-category-name')) {
            value = toTitleCaseWords(value);
        } else if (this.hasAttribute('data-category-code')) {
            value = value.toUpperCase();
        } else if (this.hasAttribute('data-category-aisle')) {
            const match = value.match(/^pasillo\s+([1-8])$/i);
            if (match) {
                value = 'Pasillo ' + match[1];
            }
        }

        this.value = value;
        const form = this.closest('form');
        validateInput(this, Boolean(form) && form.classList.contains('was-validated'));
    });

    $(document).on('click', '[data-bs-target^="#delete"]', function () {
        const button = this;
        const modal = document.querySelector(button.getAttribute('data-bs-target'));
        if (!modal) {
            return;
        }

        const idInput = modal.querySelector('[data-delete-id-input]');
        const nameDisplay = modal.querySelector('[data-delete-name-display]');

        if (idInput) {
            idInput.value = button.dataset.id;
        }

        if (nameDisplay) {
            nameDisplay.textContent = button.dataset.name;
        }
    });

    $('.modal-form').each(function () {
        const form = this;

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
