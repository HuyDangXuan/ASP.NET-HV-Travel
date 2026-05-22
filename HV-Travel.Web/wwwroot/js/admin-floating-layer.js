(function () {
    if (window.AdminFloatingLayer && typeof window.AdminFloatingLayer.attach === 'function') {
        return;
    }

    const HOST_ID = 'admin-floating-layer';

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    function getHost() {
        let host = document.getElementById(HOST_ID);
        if (!host) {
            host = document.createElement('div');
            host.id = HOST_ID;
            host.className = 'admin-floating-layer';
            host.setAttribute('aria-hidden', 'true');
            document.body.appendChild(host);
        }

        return host;
    }

    function ensurePlaceholder(element) {
        if (!element) return null;

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
        if (!element) return null;

        const host = getHost();
        if (element.parentNode === host) {
            return element;
        }

        ensurePlaceholder(element);
        host.appendChild(element);
        return element;
    }

    function detach(element) {
        if (!element) return;

        const placeholder = element._floatingPlaceholder;
        if (placeholder && placeholder.parentNode) {
            placeholder.parentNode.insertBefore(element, placeholder);
            placeholder.remove();
        } else if (element.parentNode === getHost()) {
            element.remove();
        }

        element._floatingPlaceholder = null;
    }

    function resolveWidth(element, anchorRect, settings, viewportPadding) {
        if (Number.isFinite(settings.width) && settings.width > 0) {
            return clamp(
                settings.width,
                settings.minWidth ?? 0,
                settings.maxWidth ?? window.innerWidth - (viewportPadding * 2)
            );
        }

        const measuredWidth = element.getBoundingClientRect().width;
        const computedWidth = Number.parseFloat(window.getComputedStyle(element).width || '');
        const fallbackWidth = measuredWidth || computedWidth || anchorRect.width;

        return clamp(
            fallbackWidth,
            settings.minWidth ?? 0,
            settings.maxWidth ?? window.innerWidth - (viewportPadding * 2)
        );
    }

    function positionElement(element, anchor, options) {
        if (!element || !anchor) return 'bottom';

        const settings = options || {};
        const viewportPadding = settings.viewportPadding ?? 12;
        const offset = settings.offset ?? 10;
        const preferredPlacement = settings.placement ?? 'auto';
        const align = settings.align ?? 'left';
        const anchorRect = anchor.getBoundingClientRect();
        const width = resolveWidth(element, anchorRect, settings, viewportPadding);
        const maxWidth = Math.max(0, window.innerWidth - (viewportPadding * 2));

        element.style.setProperty('left', '0px', 'important');
        element.style.setProperty('top', '0px', 'important');
        element.style.setProperty('right', 'auto', 'important');
        element.style.setProperty('bottom', 'auto', 'important');
        element.style.setProperty('width', `${Math.round(width)}px`, 'important');
        element.style.setProperty('max-width', `${Math.round(maxWidth)}px`, 'important');

        const measuredHeight = element.getBoundingClientRect().height;
        const availableBottom = Math.max(0, window.innerHeight - anchorRect.bottom - offset - viewportPadding);
        const availableTop = Math.max(0, anchorRect.top - offset - viewportPadding);
        const minHeight = settings.minHeight ?? 220;
        const minVisibleHeight = settings.minVisibleHeight ?? 160;
        const shouldOpenUp = preferredPlacement === 'top'
            || (preferredPlacement === 'auto' && availableBottom < Math.min(minHeight, measuredHeight) && availableTop > availableBottom);
        const placement = shouldOpenUp ? 'top' : 'bottom';
        const maxHeight = Math.max(
            minVisibleHeight,
            placement === 'top' ? availableTop : availableBottom
        );
        const effectiveHeight = Math.min(measuredHeight, maxHeight);

        let left = anchorRect.left;
        if (align === 'right') {
            left = anchorRect.right - width;
        } else if (align === 'center') {
            left = anchorRect.left + ((anchorRect.width - width) / 2);
        }

        left = clamp(left, viewportPadding, Math.max(viewportPadding, window.innerWidth - width - viewportPadding));

        let top = placement === 'top'
            ? anchorRect.top - effectiveHeight - offset
            : anchorRect.bottom + offset;

        top = clamp(top, viewportPadding, Math.max(viewportPadding, window.innerHeight - effectiveHeight - viewportPadding));

        element.style.setProperty('left', `${Math.round(left)}px`, 'important');
        element.style.setProperty('top', `${Math.round(top)}px`, 'important');
        element.style.setProperty('max-height', `${Math.round(maxHeight)}px`, 'important');
        element.dataset.placement = placement;
        element.dataset.align = align;

        return placement;
    }

    window.AdminFloatingLayer = {
        getHost,
        attach,
        detach,
        positionElement
    };
})();
