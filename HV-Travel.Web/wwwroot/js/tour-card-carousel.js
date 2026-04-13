(function () {
    const carousels = document.querySelectorAll('[data-tour-card-carousel]');
    if (!carousels.length) {
        return;
    }

    const reduceMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    const desktopQuery = window.matchMedia('(min-width: 1280px)');
    const tabletQuery = window.matchMedia('(min-width: 768px)');

    carousels.forEach((carousel) => {
        const viewport = carousel.querySelector('[data-tour-card-carousel-viewport]');
        const track = carousel.querySelector('[data-tour-card-carousel-track]');
        const items = Array.from(carousel.querySelectorAll('[data-tour-card-carousel-item]'));
        const carouselSection = carousel.closest('section') ?? carousel;
        const controls = carouselSection.querySelector('[data-tour-card-carousel-controls]');
        const prevButton = carouselSection.querySelector('[data-tour-card-carousel-prev]');
        const nextButton = carouselSection.querySelector('[data-tour-card-carousel-next]');

        if (!viewport || !track || !items.length) {
            return;
        }

        let activePage = 0;
        let pageCount = 1;
        let pageWidth = 0;
        let maxTranslate = 0;
        let currentTranslate = 0;
        let pageOffsets = [0];
        let isPointerDown = false;
        let isDragging = false;
        let pointerId = null;
        let pointerType = 'mouse';
        let startX = 0;
        let startY = 0;
        let dragStartTranslate = 0;
        let lastPointerX = 0;
        let lastPointerTime = 0;
        let velocityX = 0;
        let suppressClickUntil = 0;
        let previewTranslate = 0;
        let previewFrame = null;
        const dragThreshold = 10;
        const swipeVelocityThreshold = 0.45;
        const clickSuppressDurationMs = 220;

        const getTransitionValue = () => reduceMotionQuery.matches
            ? 'transform 0ms linear'
            : 'transform 620ms cubic-bezier(0.22, 1, 0.36, 1)';

        const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

        const parseItemsPerPage = (attributeName, fallbackValue) => {
            const rawValue = Number.parseInt(carousel.dataset[attributeName] ?? '', 10);
            return Number.isFinite(rawValue) && rawValue > 0 ? rawValue : fallbackValue;
        };

        const itemsPerPageDesktop = parseItemsPerPage('tourCardCarouselItemsDesktop', 3);
        const itemsPerPageTablet = parseItemsPerPage('tourCardCarouselItemsTablet', 2);
        const itemsPerPageMobile = parseItemsPerPage('tourCardCarouselItemsMobile', 1);

        const getItemsPerPage = () => {
            if (desktopQuery.matches) {
                return itemsPerPageDesktop;
            }

            if (tabletQuery.matches) {
                return itemsPerPageTablet;
            }

            return itemsPerPageMobile;
        };

        const getTranslateForPage = (pageIndex) => {
            if (!pageOffsets.length) {
                return 0;
            }

            const safeIndex = clamp(pageIndex, 0, pageOffsets.length - 1);
            return clamp(pageOffsets[safeIndex] ?? 0, 0, maxTranslate);
        };

        const getNearestPage = (translate) => {
            let nearestPage = 0;
            let smallestDistance = Number.POSITIVE_INFINITY;

            for (let pageIndex = 0; pageIndex < pageOffsets.length; pageIndex += 1) {
                const pageTranslate = pageOffsets[pageIndex] ?? 0;
                const distance = Math.abs(pageTranslate - translate);

                if (distance < smallestDistance) {
                    smallestDistance = distance;
                    nearestPage = pageIndex;
                }
            }

            return nearestPage;
        };

        const updateControls = () => {
            const hasMultiplePages = pageCount > 1;

            if (controls) {
                controls.classList.toggle('hidden', !hasMultiplePages);
                controls.classList.toggle('flex', hasMultiplePages);
            }

            if (prevButton) {
                prevButton.disabled = !hasMultiplePages || activePage === 0;
                prevButton.setAttribute('aria-disabled', prevButton.disabled ? 'true' : 'false');
            }

            if (nextButton) {
                nextButton.disabled = !hasMultiplePages || activePage >= pageCount - 1;
                nextButton.setAttribute('aria-disabled', nextButton.disabled ? 'true' : 'false');
            }
        };

        const applyTransform = (translate, animated) => {
            currentTranslate = clamp(translate, 0, maxTranslate);
            track.style.transition = animated ? getTransitionValue() : 'none';
            track.style.transform = `translate3d(-${currentTranslate}px, 0, 0)`;
        };

        const flushPreviewTransform = () => {
            previewFrame = null;
            applyTransform(previewTranslate, false);
        };

        const schedulePreviewTransform = (translate) => {
            previewTranslate = translate;

            if (previewFrame !== null) {
                return;
            }

            previewFrame = window.requestAnimationFrame(flushPreviewTransform);
        };

        const cancelPreviewTransform = () => {
            if (previewFrame !== null) {
                window.cancelAnimationFrame(previewFrame);
                previewFrame = null;
            }
        };

        const setActivePage = (nextPage, animated) => {
            activePage = clamp(nextPage, 0, Math.max(pageCount - 1, 0));
            applyTransform(getTranslateForPage(activePage), animated);
            updateControls();
        };

        const measure = () => {
            pageWidth = viewport.clientWidth;
            maxTranslate = Math.max(track.scrollWidth - pageWidth, 0);
            const itemsPerPage = getItemsPerPage();
            const offsets = [];

            for (let startIndex = 0; startIndex < items.length; startIndex += itemsPerPage) {
                offsets.push(clamp(items[startIndex]?.offsetLeft ?? 0, 0, maxTranslate));
            }

            pageOffsets = offsets.filter((offset, index) => index === 0 || offset !== offsets[index - 1]);
            pageCount = Math.max(1, pageOffsets.length);
            activePage = clamp(getNearestPage(currentTranslate), 0, pageCount - 1);
            setActivePage(activePage, false);
        };

        const applyEdgeResistance = (rawTranslate) => {
            if (rawTranslate < 0) {
                return rawTranslate * 0.24;
            }

            if (rawTranslate > maxTranslate) {
                return maxTranslate + ((rawTranslate - maxTranslate) * 0.24);
            }

            return rawTranslate;
        };

        const resetPointerState = () => {
            isPointerDown = false;
            isDragging = false;
            pointerId = null;
            pointerType = 'mouse';
            velocityX = 0;
            viewport.classList.remove('cursor-grabbing', 'select-none');
            document.body.classList.remove('select-none');
            window.removeEventListener('pointermove', handlePointerMove);
            window.removeEventListener('pointerup', handlePointerUp);
            window.removeEventListener('pointercancel', handlePointerUp);
        };

        const completeDrag = (event) => {
            const totalDeltaX = event.clientX - startX;
            const swipeDistanceThreshold = Math.min(120, Math.max(52, pageWidth * 0.14));
            let nextPage = activePage;

            if (totalDeltaX >= swipeDistanceThreshold || velocityX >= swipeVelocityThreshold) {
                nextPage -= 1;
            } else if (totalDeltaX <= -swipeDistanceThreshold || velocityX <= -swipeVelocityThreshold) {
                nextPage += 1;
            }

            suppressClickUntil = window.performance.now() + clickSuppressDurationMs;
            setActivePage(nextPage, true);
        };

        function handlePointerMove(event) {
            if (!isPointerDown || event.pointerId !== pointerId) {
                return;
            }

            const deltaX = event.clientX - startX;
            const deltaY = event.clientY - startY;
            const now = window.performance.now();

            if (!isDragging) {
                if (pointerType !== 'mouse' && Math.abs(deltaY) > Math.abs(deltaX) && Math.abs(deltaY) > dragThreshold) {
                    resetPointerState();
                    return;
                }

                if (Math.abs(deltaX) < dragThreshold) {
                    return;
                }

                isDragging = true;
                viewport.classList.add('cursor-grabbing', 'select-none');
                document.body.classList.add('select-none');
                lastPointerX = event.clientX;
                lastPointerTime = now;
            }

            const deltaTime = Math.max(now - lastPointerTime, 1);
            velocityX = (event.clientX - lastPointerX) / deltaTime;
            lastPointerX = event.clientX;
            lastPointerTime = now;

            event.preventDefault();
            schedulePreviewTransform(applyEdgeResistance(dragStartTranslate - deltaX));
        }

        function handlePointerUp(event) {
            if (!isPointerDown || event.pointerId !== pointerId) {
                return;
            }

            cancelPreviewTransform();

            if (isDragging) {
                completeDrag(event);
            } else {
                setActivePage(activePage, true);
            }

            resetPointerState();
        }

        viewport.addEventListener('pointerdown', (event) => {
            if (event.button !== 0 && event.pointerType === 'mouse') {
                return;
            }

            if (isPointerDown) {
                return;
            }

            if (event.target instanceof Element && event.target.closest('[data-tour-card-carousel-prev], [data-tour-card-carousel-next]')) {
                return;
            }

            cancelPreviewTransform();
            isPointerDown = true;
            pointerId = event.pointerId;
            pointerType = event.pointerType;
            startX = event.clientX;
            startY = event.clientY;
            dragStartTranslate = currentTranslate;
            lastPointerX = event.clientX;
            lastPointerTime = window.performance.now();
            velocityX = 0;

            window.addEventListener('pointermove', handlePointerMove, { passive: false });
            window.addEventListener('pointerup', handlePointerUp);
            window.addEventListener('pointercancel', handlePointerUp);
        });

        viewport.addEventListener('dragstart', (event) => {
            if (isPointerDown) {
                event.preventDefault();
            }
        });

        viewport.addEventListener('selectstart', (event) => {
            if (isPointerDown) {
                event.preventDefault();
            }
        });

        carousel.addEventListener('click', (event) => {
            if (window.performance.now() <= suppressClickUntil) {
                event.preventDefault();
                event.stopPropagation();
            }
        }, true);

        prevButton?.addEventListener('click', () => {
            setActivePage(activePage - 1, true);
        });

        nextButton?.addEventListener('click', () => {
            setActivePage(activePage + 1, true);
        });

        const resizeObserver = new ResizeObserver(() => {
            cancelPreviewTransform();
            measure();
        });

        resizeObserver.observe(viewport);
        reduceMotionQuery.addEventListener('change', measure);
        tabletQuery.addEventListener('change', measure);
        desktopQuery.addEventListener('change', measure);
        measure();
    });
})();
