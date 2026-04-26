(function () {
    let enhancedSelectCounter = 0;

    function findContainer(id) {
        return document.querySelector(`.custom-select-container[data-id="${id}"]`);
    }

    function resolveContainer(idOrContainer) {
        if (!idOrContainer) return null;
        if (typeof idOrContainer === 'string') return findContainer(idOrContainer);
        if (idOrContainer instanceof Element && idOrContainer.classList.contains('custom-select-container')) {
            return idOrContainer;
        }

        return idOrContainer.closest ? idOrContainer.closest('.custom-select-container') : null;
    }

    function getNativeSelect(container) {
        if (!container) return null;

        if (container.dataset.enhancedFor) {
            return document.getElementById(container.dataset.enhancedFor);
        }

        return container.querySelector('select');
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function closeCustomSelect(idOrContainer) {
        const container = resolveContainer(idOrContainer);
        if (!container) return;

        const menu = container.querySelector('.options-menu');
        const arrow = container.querySelector('.arrow-icon');
        const trigger = container.querySelector('.custom-select-trigger');
        const section = container.closest('.section-card');

        menu?.classList.remove('visible');
        container.classList.remove('active');
        if (section) section.classList.remove('active-section');
        if (arrow) arrow.style.transform = '';
        if (trigger) trigger.setAttribute('aria-expanded', 'false');
    }

    function closeAllCustomSelects(exceptContainer) {
        const resolvedException = resolveContainer(exceptContainer);
        document.querySelectorAll('.custom-select-container').forEach(function (container) {
            if (container !== resolvedException) {
                closeCustomSelect(container);
            }
        });
    }

    function measureMenuHeight(menu) {
        if (!menu) return 0;

        const previousDisplay = menu.style.display;
        const previousVisibility = menu.style.visibility;
        const previousPointerEvents = menu.style.pointerEvents;

        menu.style.visibility = 'hidden';
        menu.style.pointerEvents = 'none';
        menu.style.display = 'block';

        const height = menu.getBoundingClientRect().height || menu.scrollHeight || 0;

        menu.style.display = previousDisplay;
        menu.style.visibility = previousVisibility;
        menu.style.pointerEvents = previousPointerEvents;

        return height;
    }

    function updateMenuDirection(container) {
        const menu = container?.querySelector('.options-menu');
        if (!container || !menu) return;

        container.classList.remove('drop-up');

        const menuHeight = measureMenuHeight(menu);
        const triggerRect = container.getBoundingClientRect();
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight || 0;
        const spaceBelow = viewportHeight - triggerRect.bottom;
        const spaceAbove = triggerRect.top;
        const shouldDropUp = menuHeight > 0 && spaceBelow < menuHeight + 16 && spaceAbove > spaceBelow;

        container.classList.toggle('drop-up', shouldDropUp);
    }

    function toggleCustomSelect(idOrContainer) {
        const container = resolveContainer(idOrContainer);
        if (!container || container.classList.contains('disabled')) return;

        const menu = container.querySelector('.options-menu');
        const arrow = container.querySelector('.arrow-icon');
        const trigger = container.querySelector('.custom-select-trigger');
        const section = container.closest('.section-card');
        const willOpen = !!menu && !menu.classList.contains('visible');

        closeAllCustomSelects(willOpen ? container : null);

        if (willOpen) {
            updateMenuDirection(container);
        }

        menu?.classList.toggle('visible', willOpen);
        container.classList.toggle('active', willOpen);
        if (section) section.classList.toggle('active-section', willOpen);
        if (arrow) arrow.style.transform = willOpen ? 'rotate(180deg)' : '';
        if (trigger) trigger.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
    }

    function setSelectedState(container, selectedValue, selectedText) {
        const label = container.querySelector('.selected-label');
        if (label) {
            label.innerText = selectedText;
        }

        container.querySelectorAll('.custom-option').forEach(function (option) {
            const isSelected = option.dataset.value === selectedValue;
            option.classList.toggle('selected', isSelected);
            option.setAttribute('aria-selected', isSelected ? 'true' : 'false');

            const existingCheck = option.querySelector('.check-icon');
            if (existingCheck && !isSelected) {
                existingCheck.remove();
            }

            if (isSelected && !existingCheck) {
                option.insertAdjacentHTML('beforeend', '<span class="material-symbols-outlined text-lg check-icon">check</span>');
            }
        });
    }

    function selectCustomOption(idOrContainer, value, text) {
        const container = resolveContainer(idOrContainer);
        if (!container) return;

        const nativeSelect = getNativeSelect(container);
        let resolvedText = text;

        if (nativeSelect) {
            nativeSelect.value = value;
            const nativeOption = Array.from(nativeSelect.options).find(function (option) {
                return option.value === value;
            });

            if (nativeOption) {
                resolvedText = nativeOption.text;
            }

            nativeSelect.dispatchEvent(new Event('change', { bubbles: true }));
            nativeSelect.dispatchEvent(new Event('input', { bubbles: true }));
        }

        setSelectedState(container, value, resolvedText);
        closeCustomSelect(container);
    }

    function buildOptionMarkup(option, isSelected) {
        const dotMarkup = '<span class="custom-option-dot"></span>';
        const checkMarkup = isSelected ? '<span class="material-symbols-outlined text-lg check-icon">check</span>' : '';
        const disabledClass = option.disabled ? ' disabled' : '';
        const optionValue = escapeHtml(option.value);
        const optionText = escapeHtml(option.text);

        return `
            <div class="custom-option${isSelected ? ' selected' : ''}${disabledClass}" data-value="${optionValue}" data-label="${optionText}" role="option" aria-selected="${isSelected ? 'true' : 'false'}">
                <span class="custom-option-main">${dotMarkup}<span>${optionText}</span></span>
                ${checkMarkup}
            </div>`;
    }

    function syncEnhancedSelect(container) {
        const nativeSelect = getNativeSelect(container);
        const label = container.querySelector('.selected-label');
        const menu = container.querySelector('.options-menu');
        const trigger = container.querySelector('.custom-select-trigger');
        if (!nativeSelect || !label || !menu || !trigger) return;

        const selectedOption = nativeSelect.options[nativeSelect.selectedIndex];
        const fallbackLabel = container.dataset.placeholder || 'Chọn';
        label.innerText = selectedOption ? selectedOption.text : fallbackLabel;

        menu.innerHTML = Array.from(nativeSelect.options).map(function (option) {
            const isSelected = option.value === nativeSelect.value;
            return buildOptionMarkup(option, isSelected);
        }).join('');

        menu.querySelectorAll('.custom-option').forEach(function (optionNode) {
            const optionValue = optionNode.dataset.value;
            const linkedOption = Array.from(nativeSelect.options).find(function (option) {
                return option.value === optionValue;
            });

            if (linkedOption && !linkedOption.disabled) {
                optionNode.addEventListener('click', function () {
                    selectCustomOption(container, linkedOption.value, linkedOption.text);
                });
            }
        });

        trigger.disabled = nativeSelect.disabled;
        container.classList.toggle('disabled', nativeSelect.disabled);
    }

    function syncManualSelect(container) {
        const nativeSelect = getNativeSelect(container);
        const label = container.querySelector('.selected-label');
        if (!nativeSelect || !label) return;

        const selectedOption = container.querySelector(`.custom-option[data-value="${nativeSelect.value}"]`);
        const nativeOption = nativeSelect.options[nativeSelect.selectedIndex];
        if (selectedOption) {
            setSelectedState(
                container,
                nativeSelect.value,
                nativeOption ? nativeOption.text : (selectedOption.dataset.label || selectedOption.textContent.trim())
            );
        }
    }

    function syncCustomSelect(container) {
        if (!container) return;
        if (container.dataset.enhancedFor) {
            syncEnhancedSelect(container);
            return;
        }

        syncManualSelect(container);
    }

    function ensureSelectId(select) {
        if (!select.id) {
            enhancedSelectCounter += 1;
            select.id = `admin-native-select-${enhancedSelectCounter}`;
        }

        return select.id;
    }

    function buildEnhancedContainer(select) {
        const variant = (select.dataset.adminSelect || 'default').trim().toLowerCase();
        const caption = (select.dataset.adminSelectCaption || '').trim();
        const leadingIcon = (select.dataset.adminSelectIcon || '').trim();
        const selectId = ensureSelectId(select);
        const generatedId = `AdminSelect${selectId}`;
        const container = document.createElement('div');

        container.className = `custom-select-container admin-enhanced-select ${variant === 'compact' ? 'compact-select' : ''}`;
        container.dataset.id = generatedId;
        container.dataset.enhancedFor = selectId;
        container.dataset.placeholder = (select.dataset.adminSelectPlaceholder || '').trim();
        if (!caption) {
            container.classList.add('no-caption');
        }

        const leadingMarkup = leadingIcon
            ? `<span class="custom-select-leading material-symbols-outlined">${escapeHtml(leadingIcon)}</span>`
            : '';

        container.innerHTML = `
            <button type="button" class="custom-select-trigger" aria-haspopup="listbox" aria-expanded="false">
                <div class="custom-select-main">
                    ${leadingMarkup}
                    <div class="custom-select-meta">
                        ${caption ? `<span class="custom-select-caption">${escapeHtml(caption)}</span>` : ''}
                        <span class="selected-label"></span>
                    </div>
                </div>
                <span class="material-symbols-outlined transition-transform duration-300 arrow-icon">expand_more</span>
            </button>
            <div class="options-menu" role="listbox"></div>`;

        select.classList.add('admin-native-select-input');
        select.dataset.adminSelectBound = 'true';
        select.insertAdjacentElement('afterend', container);

        const trigger = container.querySelector('.custom-select-trigger');
        trigger?.addEventListener('click', function () {
            toggleCustomSelect(container);
        });

        select.addEventListener('change', function () {
            syncEnhancedSelect(container);
        });

        select.addEventListener('input', function () {
            syncEnhancedSelect(container);
        });

        syncEnhancedSelect(container);
    }

    function initializeAdminSelects(root) {
        const scope = root || document;
        scope.querySelectorAll('select[data-admin-select]').forEach(function (select) {
            if (select.dataset.adminSelectBound === 'true') return;
            buildEnhancedContainer(select);
        });

        scope.querySelectorAll('.custom-select-container').forEach(syncCustomSelect);
    }

    document.addEventListener('DOMContentLoaded', function () {
        initializeAdminSelects(document);
    });

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.custom-select-container')) {
            closeAllCustomSelects(null);
        }
    });

    window.toggleCustomSelect = toggleCustomSelect;
    window.selectCustomOption = selectCustomOption;
    window.initializeAdminSelects = initializeAdminSelects;
})();
