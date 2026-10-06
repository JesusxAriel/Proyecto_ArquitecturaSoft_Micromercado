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

    function parseDecimal(value) {
        return Number(String(value).trim().replace(',', '.'));
    }

    // data-gte-field="#otroCampo": el valor de este campo debe ser >= al del otro campo.
    function isLessThanField(input) {
        const other = document.querySelector(input.dataset.gteField);
        if (!other || !input.value.trim() || !other.value.trim()) {
            return false;
        }

        const value = parseDecimal(input.value);
        const limit = parseDecimal(other.value);
        return !Number.isNaN(value) && !Number.isNaN(limit) && value < limit;
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
                    : 'El formato ingresado no es válido.');
        } else if (input.dataset.gteField && isLessThanField(input)) {
            message = input.dataset.gteMessage || 'El valor no puede ser menor al del campo relacionado.';
        } else if (input.type === 'number' && input.value && Number.isNaN(Number(input.value))) {
            message = 'Ingresa un número válido.';
        } else if (input.dataset.sanitize === 'text' && input.value && !validationPattern.test(input.value)) {
            message = 'Usa solo letras, números, espacios, puntos, comas o guiones.';
        } else if (input._uniqueError) {
            message = input._uniqueError;
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
        let blocked = false;
        $(form).find('[data-validate-input]').each(function () {
            const fieldValid = validateInput(this, showErrors);
            valid = fieldValid && valid;

            // Un duplicado no deshabilita Guardar: al hacer clic el foco va al campo repetido.
            if (!fieldValid && !(this._uniqueError && this.validationMessage === this._uniqueError)) {
                blocked = true;
            }
        });
        if (showErrors) {
            $(form).find('.modal-save-button, button[type="submit"]').prop('disabled', blocked);
        }
        return valid;
    }

    // ---- Validación de unicidad en vivo ---------------------------------------------------
    // Un campo con data-unique-url consulta ese endpoint (GET "?handler=CheckUnique") al escribir
    // (con espera) y al salir del campo, y muestra el error junto a él. data-unique-fields indica
    // qué valores se envían: "parametro=#selector,...". Lo usan categorías, productos y proveedores.
    const uniqueDelay = 400;

    function uniqueFields(anchor) {
        if (!anchor._uniqueFields) {
            anchor._uniqueFields = (anchor.dataset.uniqueFields || '')
                .split(',')
                .map(pair => pair.split('='))
                .filter(parts => parts.length === 2)
                .map(([name, selector]) => ({ name: name.trim(), element: document.querySelector(selector.trim()) }))
                .filter(field => field.element);
        }

        return anchor._uniqueFields;
    }

    function uniqueQuery(anchor) {
        const params = new URLSearchParams();
        uniqueFields(anchor).forEach(field => params.set(field.name, field.element.value.trim()));
        return params.toString();
    }

    function uniqueAnchors(form) {
        return Array.from(form.querySelectorAll('[data-unique-url]'));
    }

    function resetUnique(anchor) {
        anchor._uniqueSequence = (anchor._uniqueSequence || 0) + 1; // descarta respuestas en vuelo
        anchor._uniqueError = '';
        anchor._uniqueCheckedKey = null;
        clearTimeout(anchor._uniqueTimer);
    }

    async function checkUnique(anchor) {
        const key = uniqueQuery(anchor);
        const sequence = anchor._uniqueSequence = (anchor._uniqueSequence || 0) + 1;
        let result = null;

        if (anchor.value.trim()) {
            try {
                const response = await fetch(anchor.dataset.uniqueUrl + '&' + key, {
                    cache: 'no-store',
                    headers: { Accept: 'application/json' }
                });

                if (response.ok) {
                    result = await response.json();
                }
            } catch {
                // Sin respuesta del servidor: no se bloquea, el servidor valida de todos modos al guardar.
            }
        }

        if (sequence !== anchor._uniqueSequence) {
            return; // llegó tarde: ya hay una consulta más reciente
        }

        anchor._uniqueError = result && result.duplicate
            ? (result.message || 'Ya existe un registro con esos datos.')
            : '';
        anchor._uniqueCheckedKey = key;

        validateInput(anchor, true);
        const form = anchor.closest('form');
        if (form && form.classList.contains('was-validated')) {
            validateForm(form, true);
        }
    }

    function bindUnique(anchor) {
        if (anchor._uniqueBound) {
            return; // idempotente: nunca se enlazan dos veces los mismos listeners
        }

        anchor._uniqueBound = true;
        resetUnique(anchor);

        const schedule = delay => {
            clearTimeout(anchor._uniqueTimer);
            anchor._uniqueTimer = setTimeout(() => checkUnique(anchor), delay);
        };

        const onChange = () => {
            if (anchor._uniqueError) {
                anchor._uniqueError = '';
                validateInput(anchor, true);
            }

            schedule(uniqueDelay);
        };

        uniqueFields(anchor).forEach(field => {
            field.element.addEventListener('input', onChange);
            field.element.addEventListener('change', onChange);
        });

        anchor.addEventListener('blur', () => {
            if (anchor.value.trim() && anchor._uniqueCheckedKey !== uniqueQuery(anchor)) {
                schedule(0);
            }
        });
    }

    function focusUniqueError(form) {
        const anchor = uniqueAnchors(form).find(candidate => candidate._uniqueError);
        if (anchor) {
            anchor.focus();
        }

        return Boolean(anchor);
    }

    window.initUniqueChecks = function (root) {
        $(root || document).find('[data-unique-url]').each(function () {
            bindUnique(this);
        });
    };

    // Cada vez que un modal se abre o se cierra se descarta el estado de la comprobación anterior.
    $(document).on('show.bs.modal hidden.bs.modal', '.modal', function () {
        $(this).find('[data-unique-url]').each(function () {
            resetUnique(this);
        });
    });

    // ---- Vista previa del código generado --------------------------------------------------
    // El servidor calcula el código con el mismo servicio que usa al guardar (no se repite el algoritmo aquí).
    function bindCodePreview(target) {
        const source = document.querySelector(target.dataset.codePreviewSource);
        const note = target.parentElement.querySelector('[data-code-preview-message]');
        let timer = null;
        let sequence = 0;

        function show(code, message) {
            target.value = code;
            if (note) {
                note.textContent = message;
                note.classList.toggle('d-none', !message);
            }
        }

        async function refresh() {
            const name = source.value.trim();
            const current = ++sequence;

            if (!name) {
                show('', '');
                return;
            }

            try {
                const response = await fetch(target.dataset.codePreviewUrl + '&name=' + encodeURIComponent(name), {
                    cache: 'no-store',
                    headers: { Accept: 'application/json' }
                });

                if (!response.ok || current !== sequence) {
                    return;
                }

                const data = await response.json();
                show(data.code || '', data.message || '');
            } catch {
                // Sin respuesta: el campo conserva su placeholder y el servidor asigna el código al guardar.
            }
        }

        source.addEventListener('input', function () {
            clearTimeout(timer);
            timer = setTimeout(refresh, 300);
        });

        $(source.closest('.modal')).on('show.bs.modal hidden.bs.modal', function () {
            clearTimeout(timer);
            sequence++;
            show('', '');
        });

        if (source.value.trim()) {
            refresh(); // el modal se reabrió con un nombre devuelto por el servidor
        }
    }

    $('[data-code-preview-url]').each(function () {
        bindCodePreview(this);
    });

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

    // Búsqueda de los listados paginados: el servidor filtra, así que se envía el formulario GET
    // poco después de dejar de escribir y se devuelve el foco al campo al recargar.
    $('[data-auto-search]').each(function () {
        const input = this;
        let timer = null;

        input.addEventListener('input', function () {
            clearTimeout(timer);
            timer = setTimeout(function () {
                input.form.requestSubmit();
            }, 450);
        });

        if (new URLSearchParams(window.location.search).has('Q')) {
            input.focus();
            input.setSelectionRange(input.value.length, input.value.length);
        }
    });

    $(document).on('change', '[data-auto-submit]', function () {
        this.form.submit();
    });

    window.initUniqueChecks(document);

    $('.modal-form').each(function () {
        const form = this;

        $(form).find('[data-validate-input]').on('input blur change', function () {
            if (form.classList.contains('was-validated')) {
                validateInput(this, true);
                validateForm(form, true);
            }
        });

        $(form).on('submit', function (event) {
            if (form.dataset.submitting === 'true') {
                // Segundo envío mientras el primero sigue en curso (doble clic o Enter repetido).
                event.preventDefault();
                return;
            }

            $(this).find('[data-capitalize="true"]').each(function () {
                capitalizeFirstLetter(this);
            });

            const valid = validateForm(form, true) && form.checkValidity();
            form.classList.add('was-validated');

            if (!valid) {
                event.preventDefault();
                focusUniqueError(form);
                return;
            }

            // Si el valor actual aún no se consultó (se escribió y se pulsó Guardar enseguida) se
            // consulta primero; solo si no hay duplicado se envía el formulario.
            const pending = uniqueAnchors(form).filter(anchor =>
                anchor.value.trim() && anchor._uniqueCheckedKey !== uniqueQuery(anchor));

            if (pending.length > 0) {
                event.preventDefault();

                if (form.dataset.checking !== 'true') {
                    form.dataset.checking = 'true';
                    Promise.all(pending.map(checkUnique)).finally(function () {
                        delete form.dataset.checking;

                        if (!focusUniqueError(form)) {
                            form.requestSubmit();
                        }
                    });
                }

                return;
            }

            // Otros handlers del mismo formulario pueden cancelar el envío después de este,
            // así que se decide al terminar el evento si realmente se está enviando.
            setTimeout(function () {
                if (event.originalEvent && event.originalEvent.defaultPrevented) {
                    return;
                }

                form.dataset.submitting = 'true';
                $(form).find('.modal-save-button, button[type="submit"]').prop('disabled', true);
            }, 0);
        });

        // Al volver con "atrás" el navegador puede restaurar la página con el envío bloqueado.
        window.addEventListener('pageshow', function () {
            delete form.dataset.submitting;
            $(form).find('.modal-save-button, button[type="submit"]').prop('disabled', false);
        });
    });
})();
