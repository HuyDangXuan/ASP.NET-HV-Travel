(function () {
    function findContainer(id) {
        return document.querySelector(`.custom-select-container[data-id="${id}"]`);
    }

    function closeCustomSelect(container) {
        if (!container) return;

        const menu = container.querySelector('.options-menu');
        const arrow = container.querySelector('.arrow-icon');
        const section = container.closest('.section-card');

        menu?.classList.remove('visible');
        container.classList.remove('active');
        if (section) section.classList.remove('active-section');
        if (arrow) arrow.style.transform = '';
    }

    function closeAllCustomSelects(exceptContainer) {
        document.querySelectorAll('.custom-select-container').forEach(function (container) {
            if (container !== exceptContainer) {
                closeCustomSelect(container);
            }
        });
    }

    function toggleCustomSelect(id) {
        const container = findContainer(id);
        if (!container) return;

        const menu = container.querySelector('.options-menu');
        const arrow = container.querySelector('.arrow-icon');
        const section = container.closest('.section-card');
        const willOpen = !menu.classList.contains('visible');

        closeAllCustomSelects(willOpen ? container : null);

        menu.classList.toggle('visible', willOpen);
        container.classList.toggle('active', willOpen);
        if (section) section.classList.toggle('active-section', willOpen);
        if (arrow) arrow.style.transform = willOpen ? 'rotate(180deg)' : '';
    }

    function selectCustomOption(id, value, text) {
        const container = findContainer(id);
        if (!container) return;

        const label = container.querySelector('.selected-label');
        const nativeSelect = container.querySelector('select');
        let resolvedText = text;

        if (nativeSelect) {
            nativeSelect.value = value;
            nativeSelect.dispatchEvent(new Event('change', { bubbles: true }));
            nativeSelect.dispatchEvent(new Event('input', { bubbles: true }));

            const nativeOption = Array.from(nativeSelect.options).find(function (option) {
                return option.value === value;
            });

            if (nativeOption) {
                resolvedText = nativeOption.text;
            }
        }

        if (label) label.innerText = resolvedText;

        container.querySelectorAll('.custom-option').forEach(function (option) {
            const isSelected = option.dataset.value === value;
            option.classList.toggle('selected', isSelected);

            const existingCheck = option.querySelector('.check-icon');
            if (existingCheck && !isSelected) {
                existingCheck.remove();
            }

            if (isSelected && !existingCheck) {
                option.insertAdjacentHTML('beforeend', '<span class="material-symbols-outlined text-lg check-icon">check</span>');
            }
        });

        closeCustomSelect(container);
    }

    function syncCustomSelect(container) {
        const nativeSelect = container.querySelector('select');
        const label = container.querySelector('.selected-label');
        if (!nativeSelect || !label) return;

        const selectedOption = container.querySelector(`.custom-option[data-value="${nativeSelect.value}"]`);
        const nativeOption = nativeSelect.options[nativeSelect.selectedIndex];
        if (selectedOption) {
            label.innerText = nativeOption ? nativeOption.text : (selectedOption.dataset.label || selectedOption.textContent.trim());
            container.querySelectorAll('.custom-option').forEach(function (option) {
                const isSelected = option === selectedOption;
                option.classList.toggle('selected', isSelected);

                const existingCheck = option.querySelector('.check-icon');
                if (existingCheck && !isSelected) {
                    existingCheck.remove();
                }

                if (isSelected && !existingCheck) {
                    option.insertAdjacentHTML('beforeend', '<span class="material-symbols-outlined text-lg check-icon">check</span>');
                }
            });
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.custom-select-container').forEach(syncCustomSelect);
    });

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.custom-select-container')) {
            closeAllCustomSelects(null);
        }
    });

    window.toggleCustomSelect = toggleCustomSelect;
    window.selectCustomOption = selectCustomOption;
})();
