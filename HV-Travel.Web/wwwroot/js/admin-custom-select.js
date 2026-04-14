(function () {
    let enhancedSelectCounter = 0;
    let activeContainer = null;

    function createFloatingLayerApi() {
        function getHost() {
            let host = document.getElementById('admin-floating-layer');
            if (!host) {
                host = document.createElement('div');
                host.id = 'admin-floating-layer';
                host.className = 'admin-floating-layer';
                document.body.appendChild(host);
            }

            return host;
        }

        function ensurePlaceholder(element) {
            if (!element._floatingPlaceholder && element.parentNode) {
                const placeholder = document.createElement('span');
                placeholder.hidden = true;
                placeholder.setAttribute('aria-hidden', 'true');
                element.parentNode.insertBefore(placeholder, element);
                element._floatingPlaceholder = placeholder;
            }

            return element._floatingPlaceholder;
        }

        function attach(element) {
            if (!element) return;
            ensurePlaceholder(element);
            getHost().appendChild(element);
        }

        function detach(element) {
            if (!element) return;

            const placeholder = element._floatingPlaceholder;
            if (placeholder && placeholder.parentNode) {
                placeholder.parentNode.insertBefore(element, placeholder);
                placeholder.remove();
                element._floatingPlaceholder = null;
            }
        }

        function clamp(value, min, max) {
            return Math.min(Math.max(value, min), max);
        }

        function positionElement(element, anchor, options) {
            if (!element || !anchor) return 'bottom';

            const settings = options || {};
            const anchorRect = anchor.getBoundingClientRect();
            const viewportPadding = settings.viewportPadding ?? 12;
            const offset = settings.offset ?? 10;
            const preferredPlacement = settings.placement ?? 'auto';
            const requestedWidth = settings.width ?? anchorRect.width;
            const width = clamp(requestedWidth, settings.minWidth ?? 180, window.innerWidth - (viewportPadding * 2));

            element.style.left = '0px';
            element.style.top = '0px';
            element.style.width = `${width}px`;
            element.style.maxWidth = `${window.innerWidth - (viewportPadding * 2)}px`;

            const measuredHeight = element.getBoundingClientRect().height;
            const availableBottom = window.innerHeight - anchorRect.bottom - offset - viewportPadding;
            const availableTop = anchorRect.top - offset - viewportPadding;
            const minHeight = settings.minHeight ?? 220;

            const shouldOpenUp = preferredPlacement === 'top'
                || (preferredPlacement === 'auto' && availableBottom < minHeight && availableTop > availableBottom);

            const placement = shouldOpenUp ? 'top' : 'bottom';
            const maxHeight = Math.max(
                settings.minVisibleHeight ?? 160,
                placement === 'top' ? availableTop : availableBottom
            );

            let left = anchorRect.left;
            if (settings.align === 'right') {
                left = anchorRect.right - width;
            } else if (settings.align === 'center') {
                left = anchorRect.left + ((anchorRect.width - width) / 2);
            }

            left = clamp(left, viewportPadding, window.innerWidth - width - viewportPadding);

            let top;
            if (placement === 'top') {
                top = anchorRect.top - measuredHeight - offset;
                top = Math.max(viewportPadding, top);
            } else {
                top = anchorRect.bottom + offset;
                top = Math.min(window.innerHeight - measuredHeight - viewportPadding, top);
            }

            element.style.left = `${Math.round(left)}px`;
            element.style.top = `${Math.round(top)}px`;
            element.style.maxHeight = `${Math.round(maxHeight)}px`;
            element.dataset.placement = placement;
            return placement;
        }

        return {
            getHost,
            attach,
            detach,
            positionElement
        };
    }

    const floatingLayer = window.AdminFloatingLayer || createFloatingLayerApi();
    window.AdminFloatingLayer = floatingLayer;

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

    function getTrigger(container) {
        return container?.querySelector('.custom-select-trigger') || null;
    }

    function setMenuReference(container, menu) {
        if (!container || !(menu instanceof Element)) return null;
        container._floatingMenu = menu;
        return menu;
    }

    function cancelPendingReveal(menu) {
        if (!menu?._revealFrame) return;

        cancelAnimationFrame(menu._revealFrame);
        menu._revealFrame = null;
    }

    function getMenu(container) {
        if (!container) return null;
        if (container._floatingMenu instanceof Element) {
            return container._floatingMenu;
        }

        const menu = container.querySelector('.options-menu');
        return setMenuReference(container, menu);
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function applyMenuVariantClasses(container, menu) {
        if (!container || !menu) return;

        menu.classList.toggle('bulk-select-menu', container.classList.contains('bulk-select'));
        menu.classList.toggle('compact-select-menu', container.classList.contains('compact-select'));
    }

    function positionFloatingMenu(container) {
        const menu = getMenu(container);
        const trigger = getTrigger(container);
        if (!menu || !trigger || !menu.classList.contains('portal-attached')) return;

        const placement = floatingLayer.positionElement(menu, trigger, {
            placement: 'auto',
            offset: 10,
            minHeight: 220,
            minWidth: trigger.getBoundingClientRect().width,
            width: Math.max(trigger.getBoundingClientRect().width, menu.scrollWidth || 0)
        });

        const isDropUp = placement === 'top';
        container.classList.toggle('drop-up', isDropUp);
        menu.classList.toggle('drop-up-menu', isDropUp);
    }

    function closeCustomSelect(idOrContainer) {
        const container = resolveContainer(idOrContainer);
        if (!container) return;

        const menu = getMenu(container);
        const arrow = container.querySelector('.arrow-icon');
        const trigger = getTrigger(container);
        const section = container.closest('.section-card');

        cancelPendingReveal(menu);
        menu?.classList.remove('visible', 'menu-mounted', 'drop-up-menu', 'portal-attached');
        if (menu) {
            const placeholder = menu._floatingPlaceholder;
            if (placeholder && placeholder.parentNode) {
                floatingLayer.detach(menu);
            } else if (container.isConnected) {
                container.appendChild(menu);
                if (placeholder) {
                    placeholder.remove();
                    menu._floatingPlaceholder = null;
                }
            } else if (menu.parentNode) {
                menu.parentNode.removeChild(menu);
                if (placeholder) {
                    placeholder.remove();
                    menu._floatingPlaceholder = null;
                }
            }

            menu.style.removeProperty('left');
            menu.style.removeProperty('top');
            menu.style.removeProperty('width');
            menu.style.removeProperty('max-width');
            menu.style.removeProperty('max-height');
            menu.style.removeProperty('visibility');
            menu.removeAttribute('data-placement');
        }

        container.classList.remove('active', 'drop-up');
        if (section) section.classList.remove('active-section');
        if (arrow) arrow.style.transform = '';
        if (trigger) trigger.setAttribute('aria-expanded', 'false');

        if (activeContainer === container) {
            activeContainer = null;
        }
    }

    function closeAllCustomSelects(exceptContainer) {
        const resolvedException = resolveContainer(exceptContainer);
        document.querySelectorAll('.custom-select-container').forEach(function (container) {
            if (container !== resolvedException) {
                closeCustomSelect(container);
            }
        });
    }

    function openCustomSelect(container) {
        if (!container || container.classList.contains('disabled')) return;

        const menu = getMenu(container);
        const arrow = container.querySelector('.arrow-icon');
        const trigger = getTrigger(container);
        const section = container.closest('.section-card');
        if (!menu || !trigger) return;

        closeAllCustomSelects(container);
        applyMenuVariantClasses(container, menu);
        floatingLayer.attach(menu);
        cancelPendingReveal(menu);
        menu.classList.add('portal-attached', 'menu-mounted');
        menu.classList.remove('visible');
        menu.style.visibility = 'hidden';
        positionFloatingMenu(container);

        container.classList.add('active');
        if (section) section.classList.add('active-section');
        if (arrow) arrow.style.transform = 'rotate(180deg)';
        trigger.setAttribute('aria-expanded', 'true');
        activeContainer = container;
        menu.getBoundingClientRect();
        menu._revealFrame = requestAnimationFrame(function () {
            menu._revealFrame = null;
            if (activeContainer !== container || !menu.classList.contains('portal-attached')) {
                return;
            }

            menu.style.visibility = '';
            menu.classList.add('visible');
        });
    }

    function toggleCustomSelect(idOrContainer) {
        const container = resolveContainer(idOrContainer);
        if (!container || container.classList.contains('disabled')) return;

        const menu = getMenu(container);
        const willOpen = !!menu && !menu.classList.contains('visible');

        if (!willOpen) {
            closeCustomSelect(container);
            return;
        }

        openCustomSelect(container);
    }

    function setSelectedState(container, selectedValue, selectedText) {
        const label = container.querySelector('.selected-label');
        const menu = getMenu(container);
        if (label) {
            label.innerText = selectedText;
        }

        menu?.querySelectorAll('.custom-option').forEach(function (option) {
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

    function bindOptionClicks(container, nativeSelect, menu) {
        menu.querySelectorAll('.custom-option').forEach(function (optionNode) {
            const optionValue = optionNode.dataset.value;
            const linkedOption = Array.from(nativeSelect.options).find(function (option) {
                return option.value === optionValue;
            });

            if (linkedOption && !linkedOption.disabled) {
                optionNode.addEventListener('click', function (event) {
                    event.stopPropagation();
                    selectCustomOption(container, linkedOption.value, linkedOption.text);
                });
            }
        });
    }

    function syncEnhancedSelect(container) {
        const nativeSelect = getNativeSelect(container);
        const label = container.querySelector('.selected-label');
        const menu = getMenu(container);
        const trigger = getTrigger(container);
        if (!nativeSelect || !label || !menu || !trigger) return;

        const selectedOption = nativeSelect.options[nativeSelect.selectedIndex];
        const fallbackLabel = container.dataset.placeholder || 'Chọn';
        label.innerText = selectedOption ? selectedOption.text : fallbackLabel;

        menu.innerHTML = Array.from(nativeSelect.options).map(function (option) {
            const isSelected = option.value === nativeSelect.value;
            return buildOptionMarkup(option, isSelected);
        }).join('');

        bindOptionClicks(container, nativeSelect, menu);

        trigger.disabled = nativeSelect.disabled;
        container.classList.toggle('disabled', nativeSelect.disabled);
    }

    function syncManualSelect(container) {
        const nativeSelect = getNativeSelect(container);
        const label = container.querySelector('.selected-label');
        const menu = getMenu(container);
        if (!nativeSelect || !label) return;

        const selectedOption = menu?.querySelector(`.custom-option[data-value="${nativeSelect.value}"]`);
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
        const classNames = ['custom-select-container', 'admin-enhanced-select'];

        if (variant === 'compact') {
            classNames.push('compact-select');
        } else if (variant === 'bulk') {
            classNames.push('bulk-select');
        }

        container.className = classNames.join(' ');
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
        setMenuReference(container, container.querySelector('.options-menu'));

        select.classList.add('admin-native-select-input');
        select.dataset.adminSelectBound = 'true';
        select.insertAdjacentElement('afterend', container);

        const trigger = getTrigger(container);
        trigger?.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            toggleCustomSelect(container);
        });

        trigger?.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault();
                openCustomSelect(container);
            }

            if (event.key === 'Escape') {
                closeCustomSelect(container);
            }
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

        scope.querySelectorAll('.custom-select-container').forEach(function (container) {
            getMenu(container);
            syncCustomSelect(container);
        });
    }

    function handleFloatingReposition() {
        if (!activeContainer) return;

        const trigger = getTrigger(activeContainer);
        if (!trigger || !document.body.contains(trigger)) {
            closeCustomSelect(activeContainer);
            return;
        }

        positionFloatingMenu(activeContainer);
    }

    document.addEventListener('DOMContentLoaded', function () {
        initializeAdminSelects(document);
    });

    document.addEventListener('click', function (event) {
        const insideContainer = event.target.closest('.custom-select-container');
        const insideFloatingMenu = event.target.closest('.options-menu.portal-attached');
        if (!insideContainer && !insideFloatingMenu) {
            closeAllCustomSelects(null);
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeAllCustomSelects(null);
        }
    });

    window.addEventListener('resize', handleFloatingReposition);
    window.addEventListener('scroll', handleFloatingReposition, true);

    window.toggleCustomSelect = toggleCustomSelect;
    window.selectCustomOption = selectCustomOption;
    window.initializeAdminSelects = initializeAdminSelects;
})();
