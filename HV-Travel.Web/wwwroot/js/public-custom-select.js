(function () {
    function findContainer(id) {
        return document.querySelector(`.public-custom-select[data-public-select-id="${id}"]`);
    }

    function closePublicCustomSelect(container) {
        if (!container) return;

        const menu = container.querySelector('.public-custom-select-menu');
        const arrow = container.querySelector('.arrow-icon');

        menu?.classList.remove('visible');
        container.classList.remove('is-open');
        if (arrow) arrow.style.transform = '';
    }

    function closeAllPublicCustomSelects(exceptContainer) {
        document.querySelectorAll('.public-custom-select').forEach(function (container) {
            if (container !== exceptContainer) {
                closePublicCustomSelect(container);
            }
        });
    }

    function togglePublicCustomSelect(id) {
        const container = findContainer(id);
        if (!container) return;

        const menu = container.querySelector('.public-custom-select-menu');
        const arrow = container.querySelector('.arrow-icon');
        const willOpen = !menu.classList.contains('visible');

        closeAllPublicCustomSelects(willOpen ? container : null);

        menu.classList.toggle('visible', willOpen);
        container.classList.toggle('is-open', willOpen);
        if (arrow) arrow.style.transform = willOpen ? 'rotate(180deg)' : '';
    }

    function selectPublicCustomOption(id, value, text) {
        const container = findContainer(id);
        if (!container) return;

        const nativeSelect = container.querySelector('select');
        const label = container.querySelector('.selected-label');
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

        if (label) {
            label.innerText = resolvedText;
        }

        container.querySelectorAll('.public-custom-select-option').forEach(function (option) {
            option.classList.toggle('selected', option.dataset.value === value);
        });

        closePublicCustomSelect(container);
    }

    function syncPublicCustomSelect(container) {
        const nativeSelect = container.querySelector('select');
        const label = container.querySelector('.selected-label');
        if (!nativeSelect || !label) return;

        const nativeOption = nativeSelect.options[nativeSelect.selectedIndex];
        if (nativeOption) {
            label.innerText = nativeOption.text;
        }

        container.querySelectorAll('.public-custom-select-option').forEach(function (option) {
            option.classList.toggle('selected', option.dataset.value === nativeSelect.value);
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.public-custom-select').forEach(function (container) {
            container.classList.add('is-enhanced');
            syncPublicCustomSelect(container);
        });
    });

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.public-custom-select')) {
            closeAllPublicCustomSelects(null);
        }
    });

    window.togglePublicCustomSelect = togglePublicCustomSelect;
    window.selectPublicCustomOption = selectPublicCustomOption;
})();
