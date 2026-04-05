(function () {
    const monthNames = ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'];
    const shellSelector = '.public-date-input-shell, .admin-date-input-shell';
    let activeInput = null;
    let currentDate = new Date();

    function ensureDropdown() {
        let dropdown = document.getElementById('public-date-picker-dropdown');
        if (!dropdown) {
            dropdown = document.createElement('div');
            dropdown.id = 'public-date-picker-dropdown';
            dropdown.className = 'public-date-menu hidden';
            dropdown.setAttribute('aria-hidden', 'true');
            dropdown.innerHTML = `
                <div class="public-date-month-bar">
                    <button type="button" class="public-date-month-action" data-public-date-prev aria-label="Tháng trước">
                        <span class="material-symbols-outlined">chevron_left</span>
                    </button>
                    <div id="public-date-month-label" class="public-date-month-label">Tháng 1, 2026</div>
                    <button type="button" class="public-date-month-action" data-public-date-next aria-label="Tháng sau">
                        <span class="material-symbols-outlined">chevron_right</span>
                    </button>
                </div>
                <div class="public-date-weekdays">
                    <span>T2</span>
                    <span>T3</span>
                    <span>T4</span>
                    <span>T5</span>
                    <span>T6</span>
                    <span>T7</span>
                    <span>CN</span>
                </div>
                <div id="public-date-grid" class="public-date-grid"></div>`;
            document.body.appendChild(dropdown);
        }

        if (!dropdown.dataset.bound) {
            dropdown.dataset.bound = 'true';

            const previousButton = dropdown.querySelector('[data-public-date-prev]');
            if (previousButton) {
                previousButton.addEventListener('click', function (event) {
                    event.preventDefault();
                    currentDate = new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1);
                    renderDatePicker();
                });
            }

            const nextButton = dropdown.querySelector('[data-public-date-next]');
            if (nextButton) {
                nextButton.addEventListener('click', function (event) {
                    event.preventDefault();
                    currentDate = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1);
                    renderDatePicker();
                });
            }
        }

        return dropdown;
    }

    function getDropdown() {
        return ensureDropdown();
    }

    function getShell(input) {
        return input ? input.closest(shellSelector) : null;
    }

    function getOpenShells() {
        return document.querySelectorAll('.public-date-input-shell.is-open, .admin-date-input-shell.is-open');
    }

    function isDateTimeInput(input) {
        return Boolean(input && input.hasAttribute('data-public-datetime-input'));
    }

    function getDateValue(input) {
        if (!input || !input.value) {
            return '';
        }

        return String(input.value).split('T')[0];
    }

    function getTimeValue(input) {
        if (!input || !input.value || !isDateTimeInput(input)) {
            return '';
        }

        return normalizeTimeValue(String(input.value).split('T')[1] || '');
    }

    function normalizeTimeValue(value) {
        if (!value) {
            return '';
        }

        const parts = String(value).split(':');
        if (parts.length < 2) {
            return '';
        }

        return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`;
    }

    function formatDisplayValue(value) {
        if (!value) {
            return '';
        }

        const dateValue = String(value).split('T')[0];
        const parts = dateValue.split('-');
        if (parts.length !== 3) {
            return value;
        }

        return `${parts[2]}/${parts[1]}/${parts[0]}`;
    }

    function formatIsoValue(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function parseInputValue(value) {
        if (!value) {
            return null;
        }

        const dateValue = String(value).split('T')[0];
        const parsed = new Date(`${dateValue}T00:00:00`);
        return Number.isNaN(parsed.getTime()) ? null : parsed;
    }

    function combineDateAndTime(dateValue, timeValue) {
        if (!dateValue) {
            return '';
        }

        const resolvedTime = normalizeTimeValue(timeValue) || '00:00';
        return `${dateValue}T${resolvedTime}`;
    }

    function getTimeInput(shell) {
        return shell ? shell.querySelector('[data-public-time-input]') : null;
    }

    function syncInputShell(input) {
        const shell = getShell(input);
        if (!shell) {
            return;
        }

        const label = shell.querySelector('.public-date-trigger-label');
        if (!label) {
            return;
        }

        const dateValue = getDateValue(input);
        const placeholder = input.dataset.placeholder || 'Chọn ngày';
        const hasValue = Boolean(dateValue);
        label.textContent = hasValue ? formatDisplayValue(dateValue) : placeholder;
        label.classList.toggle('is-placeholder', !hasValue);

        const timeInput = getTimeInput(shell);
        if (timeInput && !timeInput.matches(':focus')) {
            const resolvedTime = getTimeValue(input) || normalizeTimeValue(timeInput.value) || '00:00';
            timeInput.value = resolvedTime;
        }
    }

    function closeDatePicker() {
        const dropdown = getDropdown();
        if (!dropdown) {
            return;
        }

        dropdown.classList.add('hidden');
        dropdown.setAttribute('aria-hidden', 'true');

        getOpenShells().forEach(function (shell) {
            shell.classList.remove('is-open');
            const trigger = shell.querySelector('[data-public-date-trigger]');
            if (trigger) {
                trigger.setAttribute('aria-expanded', 'false');
            }
        });

        activeInput = null;
    }

    function positionDropdown(anchor) {
        const dropdown = getDropdown();
        if (!dropdown || !anchor) {
            return;
        }

        const rect = anchor.getBoundingClientRect();
        const width = Math.min(320, window.innerWidth - 24);
        let left = rect.left;
        const top = rect.bottom + 10;

        if (left + width > window.innerWidth - 12) {
            left = Math.max(12, window.innerWidth - width - 12);
        }

        dropdown.style.width = `${width}px`;
        dropdown.style.left = `${left}px`;
        dropdown.style.top = `${top}px`;
    }

    function renderDatePicker() {
        const dropdown = getDropdown();
        const monthLabel = document.getElementById('public-date-month-label');
        const grid = document.getElementById('public-date-grid');
        if (!dropdown || !monthLabel || !grid) {
            return;
        }

        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();
        monthLabel.textContent = `${monthNames[month]}, ${year}`;
        grid.innerHTML = '';

        const firstDay = new Date(year, month, 1).getDay();
        const startOffset = (firstDay + 6) % 7;
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const selectedDate = activeInput ? parseInputValue(getDateValue(activeInput)) : null;
        const todayValue = formatIsoValue(new Date());

        for (let i = 0; i < startOffset; i++) {
            const placeholder = document.createElement('span');
            placeholder.className = 'public-date-day public-date-day--placeholder';
            placeholder.setAttribute('aria-hidden', 'true');
            grid.appendChild(placeholder);
        }

        for (let day = 1; day <= daysInMonth; day++) {
            const date = new Date(year, month, day);
            const value = formatIsoValue(date);
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'public-date-day';
            button.textContent = String(day);
            button.dataset.value = value;

            if (selectedDate && formatIsoValue(selectedDate) === value) {
                button.classList.add('is-selected');
            }

            if (todayValue === value) {
                button.classList.add('is-today');
            }

            button.addEventListener('click', function (event) {
                event.preventDefault();
                if (!activeInput) {
                    return;
                }

                const shell = getShell(activeInput);
                const timeInput = getTimeInput(shell);
                activeInput.value = isDateTimeInput(activeInput)
                    ? combineDateAndTime(value, timeInput ? timeInput.value : getTimeValue(activeInput))
                    : value;
                activeInput.dispatchEvent(new Event('input', { bubbles: true }));
                activeInput.dispatchEvent(new Event('change', { bubbles: true }));
                syncInputShell(activeInput);
                closeDatePicker();
            });

            grid.appendChild(button);
        }
    }

    function openDatePicker(input) {
        const dropdown = getDropdown();
        const shell = getShell(input);
        if (!dropdown || !shell) {
            return;
        }

        getOpenShells().forEach(function (item) {
            if (item !== shell) {
                item.classList.remove('is-open');
                const trigger = item.querySelector('[data-public-date-trigger]');
                if (trigger) {
                    trigger.setAttribute('aria-expanded', 'false');
                }
            }
        });

        activeInput = input;
        currentDate = parseInputValue(getDateValue(input)) || new Date();
        shell.classList.add('is-open');

        const trigger = shell.querySelector('[data-public-date-trigger]');
        if (trigger) {
            trigger.setAttribute('aria-expanded', 'true');
        }

        renderDatePicker();
        positionDropdown(shell);
        dropdown.classList.remove('hidden');
        dropdown.setAttribute('aria-hidden', 'false');
    }

    function toggleDatePicker(input) {
        const shell = getShell(input);
        if (!shell) {
            return;
        }

        if (shell.classList.contains('is-open')) {
            closeDatePicker();
            return;
        }

        openDatePicker(input);
    }

    function initializeDateShell(shell) {
        const input = shell.querySelector('[data-public-date-input]');
        const trigger = shell.querySelector('[data-public-date-trigger]');
        if (!input || !trigger) {
            return;
        }

        shell.classList.add('is-enhanced');
        trigger.setAttribute('aria-expanded', 'false');
        syncInputShell(input);

        trigger.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            toggleDatePicker(input);
        });

        input.addEventListener('input', function () {
            syncInputShell(input);
            if (activeInput === input) {
                renderDatePicker();
            }
        });

        input.addEventListener('change', function () {
            syncInputShell(input);
        });

        const timeInput = getTimeInput(shell);
        if (timeInput) {
            const syncDateTime = function () {
                const dateValue = getDateValue(input);
                if (!dateValue) {
                    syncInputShell(input);
                    return;
                }

                const combinedValue = combineDateAndTime(dateValue, timeInput.value);
                if (input.value !== combinedValue) {
                    input.value = combinedValue;
                    input.dispatchEvent(new Event('input', { bubbles: true }));
                    input.dispatchEvent(new Event('change', { bubbles: true }));
                }

                syncInputShell(input);
                if (activeInput === input) {
                    renderDatePicker();
                }
            };

            timeInput.addEventListener('input', syncDateTime);
            timeInput.addEventListener('change', syncDateTime);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        ensureDropdown();
        document.querySelectorAll(shellSelector).forEach(initializeDateShell);
    });

    document.addEventListener('click', function (event) {
        if (!event.target.closest(shellSelector) && !event.target.closest('#public-date-picker-dropdown')) {
            closeDatePicker();
        }
    });

    window.addEventListener('resize', function () {
        if (!activeInput) {
            return;
        }

        const shell = getShell(activeInput);
        if (!shell) {
            closeDatePicker();
            return;
        }

        positionDropdown(shell);
    });

    window.addEventListener('scroll', function () {
        if (activeInput) {
            closeDatePicker();
        }
    }, true);

    window.PublicDatePicker = {
        close: closeDatePicker,
        syncAll: function () {
            document.querySelectorAll('[data-public-date-input]').forEach(syncInputShell);
        }
    };
})();