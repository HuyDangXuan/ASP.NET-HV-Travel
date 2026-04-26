(function () {
    const PICKER_SELECTOR = '.admin-picker[data-admin-picker]';
    const WEEKDAYS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
    let activePicker = null;
    let panel = null;
    let panelState = null;
    const floatingLayer = window.AdminFloatingLayer;
    if (!floatingLayer) return;

    function getMode(picker) {
        return (picker?.dataset.adminPicker || 'date').trim().toLowerCase();
    }

    function getInput(picker) {
        return picker?.querySelector('.admin-picker-input') || null;
    }

    function getTrigger(picker) {
        return picker?.querySelector('.admin-picker-trigger') || null;
    }

    function resolveStepMinutes(picker) {
        const input = getInput(picker);
        const datasetStep = Number.parseInt(picker?.dataset.pickerStep || '', 10);
        if (Number.isFinite(datasetStep) && datasetStep > 0) {
            return datasetStep;
        }

        const inputStep = Number.parseInt(input?.getAttribute('step') || '', 10);
        if (Number.isFinite(inputStep) && inputStep > 0) {
            return inputStep >= 60 ? Math.max(1, Math.round(inputStep / 60)) : inputStep;
        }

        return 5;
    }

    function pad(value) {
        return String(value).padStart(2, '0');
    }

    function roundDateToStep(date, stepMinutes) {
        const rounded = new Date(date.getTime());
        rounded.setSeconds(0, 0);

        if (!stepMinutes || stepMinutes <= 1) {
            return rounded;
        }

        const minute = rounded.getMinutes();
        const normalized = Math.round(minute / stepMinutes) * stepMinutes;
        rounded.setMinutes(normalized, 0, 0);
        return rounded;
    }

    function parsePickerValue(value, mode) {
        if (!value) return null;

        if (mode === 'date') {
            const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
            if (!match) return null;
            return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), 0, 0, 0, 0);
        }

        const match = /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})/.exec(value);
        if (!match) return null;

        return new Date(
            Number(match[1]),
            Number(match[2]) - 1,
            Number(match[3]),
            Number(match[4]),
            Number(match[5]),
            0,
            0
        );
    }

    function formatValue(date, mode) {
        if (!date) return '';

        if (mode === 'date') {
            return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
        }

        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    }

    function formatDisplayValue(date, mode) {
        if (!date) return '';

        if (mode === 'date') {
            return new Intl.DateTimeFormat('vi-VN', {
                weekday: 'short',
                day: '2-digit',
                month: '2-digit',
                year: 'numeric'
            }).format(date);
        }

        const datePart = new Intl.DateTimeFormat('vi-VN', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        }).format(date);

        return `${datePart} • ${pad(date.getHours())}:${pad(date.getMinutes())}`;
    }

    function formatMonthTitle(date) {
        return new Intl.DateTimeFormat('vi-VN', {
            month: 'long',
            year: 'numeric'
        }).format(date);
    }

    function ensurePickerDate(date, stepMinutes) {
        return roundDateToStep(date || new Date(), stepMinutes);
    }

    function commitPickerValue(picker, date) {
        const input = getInput(picker);
        const mode = getMode(picker);
        if (!input) return;

        input.value = formatValue(date, mode);
        syncPickerTrigger(picker);
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function syncPickerTrigger(picker) {
        const input = getInput(picker);
        const trigger = getTrigger(picker);
        const label = trigger?.querySelector('.admin-picker-label');
        if (!input || !trigger || !label) return;

        const mode = getMode(picker);
        const placeholder = picker.dataset.pickerPlaceholder
            || (mode === 'datetime' ? 'Chọn ngày giờ' : 'Chọn ngày');
        const parsedValue = parsePickerValue(input.value, mode);

        label.textContent = parsedValue ? formatDisplayValue(parsedValue, mode) : placeholder;
        trigger.classList.toggle('is-empty', !parsedValue);
        trigger.disabled = input.disabled;
        picker.classList.toggle('disabled', input.disabled);
    }

    function buildTriggerMarkup(picker) {
        const mode = getMode(picker);
        const icon = (picker.dataset.pickerIcon || (mode === 'datetime' ? 'schedule' : 'calendar_month')).trim();
        const trigger = document.createElement('button');

        trigger.type = 'button';
        trigger.className = `admin-picker-trigger admin-picker-trigger-${mode}`;
        trigger.innerHTML = `
            <span class="admin-picker-trigger-main">
                <span class="material-symbols-outlined admin-picker-trigger-icon">${icon}</span>
                <span class="admin-picker-label"></span>
            </span>
            <span class="material-symbols-outlined admin-picker-trigger-chevron">expand_more</span>`;

        return trigger;
    }

    function ensureTrigger(picker) {
        let trigger = getTrigger(picker);
        if (!trigger) {
            trigger = buildTriggerMarkup(picker);
            picker.appendChild(trigger);
        }

        return trigger;
    }

    function ensurePanel() {
        if (panel) return panel;

        panel = document.createElement('div');
        panel.className = 'admin-picker-panel hidden';
        floatingLayer.getHost().appendChild(panel);

        panel.addEventListener('click', function (event) {
            event.stopPropagation();

            const action = event.target.closest('[data-picker-action]');
            if (!action || !activePicker || !panelState) return;

            const actionName = action.dataset.pickerAction;
            if (actionName === 'prev-month') {
                panelState.viewDate = new Date(panelState.viewDate.getFullYear(), panelState.viewDate.getMonth() - 1, 1);
                renderPanel();
                return;
            }

            if (actionName === 'next-month') {
                panelState.viewDate = new Date(panelState.viewDate.getFullYear(), panelState.viewDate.getMonth() + 1, 1);
                renderPanel();
                return;
            }

            if (actionName === 'clear') {
                commitPickerValue(activePicker, null);
                closeActivePicker();
                return;
            }

            if (actionName === 'today') {
                commitPickerValue(activePicker, new Date());
                closeActivePicker();
                return;
            }

            if (actionName === 'now') {
                panelState.pendingDate = ensurePickerDate(new Date(), panelState.stepMinutes);
                panelState.viewDate = new Date(panelState.pendingDate.getFullYear(), panelState.pendingDate.getMonth(), 1);
                renderPanel();
                return;
            }

            if (actionName === 'apply' && panelState.mode === 'datetime') {
                commitPickerValue(activePicker, panelState.pendingDate);
                closeActivePicker();
                return;
            }

            const dayButton = event.target.closest('[data-picker-day]');
            if (!dayButton) return;

            const nextValue = parsePickerValue(dayButton.dataset.pickerDay, 'date');
            if (!nextValue) return;

            if (panelState.mode === 'date') {
                commitPickerValue(activePicker, nextValue);
                closeActivePicker();
                return;
            }

            const nextDateTime = new Date(
                nextValue.getFullYear(),
                nextValue.getMonth(),
                nextValue.getDate(),
                panelState.pendingDate.getHours(),
                panelState.pendingDate.getMinutes(),
                0,
                0
            );

            panelState.pendingDate = nextDateTime;
            panelState.viewDate = new Date(nextDateTime.getFullYear(), nextDateTime.getMonth(), 1);
            renderPanel();
        });

        panel.addEventListener('change', function (event) {
            if (!activePicker || !panelState || panelState.mode !== 'datetime') return;

            if (event.target.matches('[data-picker-time-part="hour"]')) {
                panelState.pendingDate.setHours(Number(event.target.value) || 0);
            }

            if (event.target.matches('[data-picker-time-part="minute"]')) {
                panelState.pendingDate.setMinutes(Number(event.target.value) || 0);
            }
        });

        return panel;
    }

    function buildDayButton(dayDate, today, selectedDate) {
        const isToday = today && dayDate.toDateString() === today.toDateString();
        const isSelected = selectedDate && dayDate.toDateString() === selectedDate.toDateString();
        const classes = ['admin-picker-day'];

        if (isSelected) {
            classes.push('is-selected');
        } else if (isToday) {
            classes.push('is-today');
        }

        return `
            <button type="button" class="${classes.join(' ')}" data-picker-day="${formatValue(dayDate, 'date')}">
                ${dayDate.getDate()}
            </button>`;
    }

    function buildCalendarMarkup() {
        const firstDay = new Date(panelState.viewDate.getFullYear(), panelState.viewDate.getMonth(), 1);
        const daysInMonth = new Date(panelState.viewDate.getFullYear(), panelState.viewDate.getMonth() + 1, 0).getDate();
        const startOffset = (firstDay.getDay() + 6) % 7;
        const today = new Date();
        const selectedDate = panelState.mode === 'date'
            ? panelState.selectedDate
            : panelState.pendingDate;
        const cells = [];

        for (let i = 0; i < startOffset; i++) {
            cells.push('<span class="admin-picker-day admin-picker-day-placeholder"></span>');
        }

        for (let day = 1; day <= daysInMonth; day++) {
            const dayDate = new Date(panelState.viewDate.getFullYear(), panelState.viewDate.getMonth(), day);
            cells.push(buildDayButton(dayDate, today, selectedDate));
        }

        return cells.join('');
    }

    function buildTimeOptions(max, selectedValue) {
        const values = [];
        for (let index = 0; index <= max; index += 1) {
            const label = pad(index);
            values.push(`<option value="${index}"${index === selectedValue ? ' selected' : ''}>${label}</option>`);
        }

        return values.join('');
    }

    function buildMinuteOptions(stepMinutes, selectedValue) {
        const values = [];
        for (let minute = 0; minute < 60; minute += stepMinutes) {
            const label = pad(minute);
            values.push(`<option value="${minute}"${minute === selectedValue ? ' selected' : ''}>${label}</option>`);
        }

        return values.join('');
    }

    function buildTimeMarkup() {
        if (panelState.mode !== 'datetime') {
            return '';
        }

        return `
            <div class="admin-picker-time-section">
                <div class="admin-picker-time-header">
                    <div>
                        <p class="admin-picker-eyebrow">Khung giờ</p>
                        <p class="admin-picker-time-hint">Bước ${panelState.stepMinutes} phút</p>
                    </div>
                </div>
                <div class="admin-picker-time-grid">
                    <label class="admin-picker-time-field">
                        <span>Giờ</span>
                        <select class="admin-picker-time-select" data-picker-time-part="hour">
                            ${buildTimeOptions(23, panelState.pendingDate.getHours())}
                        </select>
                    </label>
                    <label class="admin-picker-time-field">
                        <span>Phút</span>
                        <select class="admin-picker-time-select" data-picker-time-part="minute">
                            ${buildMinuteOptions(panelState.stepMinutes, panelState.pendingDate.getMinutes())}
                        </select>
                    </label>
                </div>
            </div>`;
    }

    function buildFooterMarkup() {
        if (panelState.mode === 'datetime') {
            return `
                <div class="admin-picker-footer">
                    <button type="button" class="admin-picker-secondary" data-picker-action="clear">Xóa</button>
                    <div class="admin-picker-footer-actions">
                        <button type="button" class="admin-picker-secondary" data-picker-action="now">Bây giờ</button>
                        <button type="button" class="admin-picker-primary" data-picker-action="apply">Áp dụng</button>
                    </div>
                </div>`;
        }

        return `
            <div class="admin-picker-footer admin-picker-footer-compact">
                <button type="button" class="admin-picker-secondary" data-picker-action="clear">Xóa</button>
                <button type="button" class="admin-picker-primary" data-picker-action="today">Hôm nay</button>
            </div>`;
    }

    function renderPanel() {
        if (!panel || !panelState) return;

        panel.innerHTML = `
            <div class="admin-picker-surface">
                <div class="admin-picker-header">
                    <div>
                        <p class="admin-picker-eyebrow">${panelState.mode === 'datetime' ? 'Date & time' : 'Calendar'}</p>
                        <h3 class="admin-picker-title">${formatMonthTitle(panelState.viewDate)}</h3>
                    </div>
                    <div class="admin-picker-nav">
                        <button type="button" class="admin-picker-nav-btn" data-picker-action="prev-month" aria-label="Tháng trước">
                            <span class="material-symbols-outlined">chevron_left</span>
                        </button>
                        <button type="button" class="admin-picker-nav-btn" data-picker-action="next-month" aria-label="Tháng sau">
                            <span class="material-symbols-outlined">chevron_right</span>
                        </button>
                    </div>
                </div>
                <div class="admin-picker-weekdays">
                    ${WEEKDAYS.map((day) => `<span>${day}</span>`).join('')}
                </div>
                <div class="admin-picker-days">
                    ${buildCalendarMarkup()}
                </div>
                ${buildTimeMarkup()}
                ${buildFooterMarkup()}
            </div>`;

        positionActivePanel();
    }

    function buildPanelState(picker) {
        const mode = getMode(picker);
        const input = getInput(picker);
        const stepMinutes = resolveStepMinutes(picker);
        const selectedDate = parsePickerValue(input?.value || '', mode);
        const pendingDate = mode === 'datetime'
            ? ensurePickerDate(selectedDate || new Date(), stepMinutes)
            : (selectedDate || new Date());

        return {
            mode,
            stepMinutes,
            selectedDate,
            pendingDate,
            viewDate: new Date((selectedDate || pendingDate).getFullYear(), (selectedDate || pendingDate).getMonth(), 1)
        };
    }

    function positionActivePanel() {
        if (!activePicker || !panel || panel.classList.contains('hidden')) return;

        const trigger = getTrigger(activePicker);
        if (!trigger) return;

        floatingLayer.positionElement(panel, trigger, {
            placement: 'auto',
            offset: 12,
            minHeight: panelState?.mode === 'datetime' ? 420 : 320,
            minWidth: trigger.getBoundingClientRect().width,
            width: panelState?.mode === 'datetime'
                ? Math.max(trigger.getBoundingClientRect().width, 392)
                : Math.max(trigger.getBoundingClientRect().width, 344)
        });
    }

    function closeActivePicker() {
        if (activePicker) {
            activePicker.classList.remove('is-open');
            const trigger = getTrigger(activePicker);
            trigger?.setAttribute('aria-expanded', 'false');
        }

        if (panel) {
            panel.classList.add('hidden');
            panel.innerHTML = '';
            panel.removeAttribute('data-placement');
            panel.style.removeProperty('left');
            panel.style.removeProperty('top');
            panel.style.removeProperty('width');
            panel.style.removeProperty('max-width');
            panel.style.removeProperty('max-height');
        }

        activePicker = null;
        panelState = null;
    }

    function openPicker(picker) {
        const input = getInput(picker);
        const trigger = getTrigger(picker);
        if (!picker || !input || !trigger || input.disabled) return;

        ensurePanel();

        if (activePicker === picker && !panel.classList.contains('hidden')) {
            closeActivePicker();
            return;
        }

        closeActivePicker();
        activePicker = picker;
        panelState = buildPanelState(picker);
        picker.classList.add('is-open');
        trigger.setAttribute('aria-expanded', 'true');
        panel.classList.remove('hidden');
        panel.style.visibility = 'hidden';
        renderPanel();
        panel.style.visibility = '';
    }

    function initializePicker(picker) {
        if (!picker || picker.dataset.pickerBound === 'true') return;

        const input = getInput(picker);
        if (!input) return;

        input.tabIndex = -1;
        input.setAttribute('aria-hidden', 'true');

        const trigger = ensureTrigger(picker);
        trigger.setAttribute('aria-haspopup', 'dialog');
        trigger.setAttribute('aria-expanded', 'false');

        trigger.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            openPicker(picker);
        });

        trigger.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp' || event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openPicker(picker);
            }

            if (event.key === 'Escape') {
                closeActivePicker();
            }
        });

        input.addEventListener('change', function () {
            syncPickerTrigger(picker);
        });

        input.addEventListener('input', function () {
            syncPickerTrigger(picker);
        });

        picker.dataset.pickerBound = 'true';
        syncPickerTrigger(picker);
    }

    function initializeAdminDateTimePickers(root) {
        const scope = root || document;
        if (scope.matches && scope.matches(PICKER_SELECTOR)) {
            initializePicker(scope);
            return;
        }

        scope.querySelectorAll(PICKER_SELECTOR).forEach(initializePicker);
    }

    function handleDocumentClick(event) {
        const insidePicker = event.target.closest('.admin-picker');
        const insidePanel = event.target.closest('.admin-picker-panel');
        if (!insidePicker && !insidePanel) {
            closeActivePicker();
        }
    }

    function handleGlobalReposition() {
        if (!activePicker || !document.body.contains(activePicker)) {
            closeActivePicker();
            return;
        }

        positionActivePanel();
    }

    document.addEventListener('DOMContentLoaded', function () {
        initializeAdminDateTimePickers(document);
    });

    document.addEventListener('click', handleDocumentClick);
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeActivePicker();
        }
    });

    window.addEventListener('resize', handleGlobalReposition);
    window.addEventListener('scroll', handleGlobalReposition, true);

    window.AdminDateTimePicker = {
        init: initializeAdminDateTimePickers,
        close: closeActivePicker,
        sync: syncPickerTrigger,
        formatDisplayValue: formatDisplayValue
    };
})();
