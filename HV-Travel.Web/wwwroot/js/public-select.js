(() => {
    const SELECTOR = 'select.public-select';
    const ENHANCED_ATTR = 'data-public-select-enhanced';
    let activeShell = null;

    const closeShell = (shell) => {
        if (!shell) {
            return;
        }

        const trigger = shell.querySelector('.public-select-trigger');
        const panel = shell.querySelector('.public-select-panel');
        shell.classList.remove('is-open');
        trigger?.setAttribute('aria-expanded', 'false');
        if (panel) {
            panel.hidden = true;
        }

        if (activeShell === shell) {
            activeShell = null;
        }
    };

    const closeActiveShell = () => {
        closeShell(activeShell);
    };

    const enabledOptions = (shell) => Array.from(shell.querySelectorAll('.public-select-option:not(:disabled)'));

    const focusOption = (shell, direction) => {
        const options = enabledOptions(shell);
        if (!options.length) {
            return;
        }

        const activeElement = document.activeElement;
        const currentIndex = options.indexOf(activeElement);
        const selectedIndex = options.findIndex((option) => option.getAttribute('aria-selected') === 'true');
        const fallbackIndex = selectedIndex >= 0 ? selectedIndex : 0;
        const nextIndex = currentIndex >= 0
            ? (currentIndex + direction + options.length) % options.length
            : fallbackIndex;

        options[nextIndex].focus();
    };

    const syncShellFromSelect = (select, shell) => {
        const triggerLabel = shell.querySelector('.public-select-value');
        const options = Array.from(shell.querySelectorAll('.public-select-option'));
        const selected = select.options[select.selectedIndex];
        const selectedValue = selected?.value ?? '';
        const selectedText = selected?.textContent?.trim() || selectedValue;

        if (triggerLabel) {
            triggerLabel.textContent = selectedText;
        }

        shell.classList.toggle('is-disabled', select.disabled);
        const trigger = shell.querySelector('.public-select-trigger');
        if (trigger) {
            trigger.disabled = select.disabled;
        }

        options.forEach((option) => {
            const isSelected = option.dataset.value === selectedValue;
            option.classList.toggle('is-selected', isSelected);
            option.setAttribute('aria-selected', isSelected ? 'true' : 'false');
        });
    };

    const openShell = (shell, focusSelected = false) => {
        if (shell.classList.contains('is-disabled')) {
            return;
        }

        if (activeShell && activeShell !== shell) {
            closeShell(activeShell);
        }

        const trigger = shell.querySelector('.public-select-trigger');
        const panel = shell.querySelector('.public-select-panel');
        if (!panel) {
            return;
        }

        shell.classList.add('is-open');
        trigger?.setAttribute('aria-expanded', 'true');
        panel.hidden = false;
        activeShell = shell;

        if (focusSelected) {
            const selectedOption = shell.querySelector('.public-select-option.is-selected:not(:disabled)');
            (selectedOption || enabledOptions(shell)[0])?.focus();
        }
    };

    const chooseOption = (select, shell, option) => {
        if (option.disabled) {
            return;
        }

        const previousValue = select.value;
        select.value = option.dataset.value ?? '';
        syncShellFromSelect(select, shell);

        if (previousValue !== select.value) {
            select.dispatchEvent(new Event('change', { bubbles: true }));
        }

        closeShell(shell);
        shell.querySelector('.public-select-trigger')?.focus();
    };

    const buildShell = (select) => {
        const shell = document.createElement('div');
        shell.className = 'public-select-shell';
        if (select.disabled) {
            shell.classList.add('is-disabled');
        }
        if (select.classList.contains('mt-2')) {
            shell.classList.add('public-select-shell--mt-2');
        }

        const selectId = select.id || `public-select-${Math.random().toString(36).slice(2)}`;
        const triggerId = `${selectId}-trigger`;
        const panelId = `${selectId}-panel`;

        const trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.id = triggerId;
        trigger.className = 'public-select-trigger';
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');
        trigger.setAttribute('aria-controls', panelId);
        trigger.disabled = select.disabled;

        const value = document.createElement('span');
        value.className = 'public-select-value';

        const icon = document.createElement('span');
        icon.className = 'material-symbols-outlined public-select-icon';
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = 'expand_more';

        trigger.append(value, icon);

        const panel = document.createElement('div');
        panel.id = panelId;
        panel.className = 'public-select-panel';
        panel.setAttribute('role', 'listbox');
        panel.setAttribute('aria-labelledby', triggerId);
        panel.hidden = true;

        Array.from(select.options).forEach((nativeOption) => {
            const option = document.createElement('button');
            option.type = 'button';
            option.className = 'public-select-option';
            option.dataset.value = nativeOption.value;
            option.textContent = nativeOption.textContent?.trim() || nativeOption.value;
            option.setAttribute('role', 'option');
            option.disabled = nativeOption.disabled;

            option.addEventListener('click', () => chooseOption(select, shell, option));
            option.addEventListener('keydown', (event) => {
                if (event.key === 'Escape') {
                    event.preventDefault();
                    closeShell(shell);
                    trigger.focus();
                    return;
                }

                if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                    event.preventDefault();
                    focusOption(shell, event.key === 'ArrowDown' ? 1 : -1);
                    return;
                }

                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    chooseOption(select, shell, option);
                }
            });

            panel.append(option);
        });

        trigger.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();
            shell.classList.contains('is-open') ? closeShell(shell) : openShell(shell);
        });

        trigger.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                shell.classList.contains('is-open') ? closeShell(shell) : openShell(shell, true);
                return;
            }

            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault();
                openShell(shell, true);
                if (event.key === 'ArrowUp') {
                    focusOption(shell, -1);
                }
            }
        });

        shell.addEventListener('click', (event) => event.stopPropagation());
        shell.append(trigger, panel);
        syncShellFromSelect(select, shell);
        return shell;
    };

    const enhanceSelect = (select) => {
        if (select.getAttribute(ENHANCED_ATTR) === 'true') {
            return;
        }

        const shell = buildShell(select);
        select.setAttribute(ENHANCED_ATTR, 'true');
        select.classList.add('public-select-native');
        select.hidden = true;
        select.insertAdjacentElement('afterend', shell);
        select.addEventListener('change', () => syncShellFromSelect(select, shell));
    };

    const initPublicSelects = (root = document) => {
        root.querySelectorAll(SELECTOR).forEach(enhanceSelect);
    };

    document.addEventListener('click', closeActiveShell);
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            closeActiveShell();
        }
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => initPublicSelects());
    } else {
        initPublicSelects();
    }

    window.HVTravelPublicSelect = {
        init: initPublicSelects
    };
})();
