(function () {
    const STORAGE_KEYS = {
        wishlist: 'hvtravel_wishlist',
        recent: 'hvtravel_recent',
        planner: 'hvtravel_trip_planner'
    };

    const currencyFormatter = new Intl.NumberFormat('vi-VN');
    const wishlistActiveClasses = ['border-primary', 'bg-primary', 'text-white', 'shadow-primary/30', 'dark:border-primary', 'dark:bg-primary', 'dark:text-white'];
    const wishlistInactiveClasses = ['border-white/50', 'bg-white/90', 'text-slate-600', 'shadow-slate-900/15', 'dark:border-white/10', 'dark:bg-slate-950/80', 'dark:text-slate-200'];
    const parseItems = (key) => {
        try {
            return JSON.parse(localStorage.getItem(key) || '[]');
        } catch {
            return [];
        }
    };

    const saveItems = (key, items) => localStorage.setItem(key, JSON.stringify(items.slice(0, 6)));

    const buildItemFromCard = (card) => ({
        id: card.dataset.tourId,
        name: card.dataset.tourName,
        price: Number(card.dataset.tourPrice || 0),
        location: card.dataset.tourLocation,
        url: card.dataset.tourUrl,
        image: card.dataset.tourImage
    });

    const isWishlisted = (id) => parseItems(STORAGE_KEYS.wishlist).some((entry) => entry.id === id);
    const isInPlanner = (id) => parseItems(STORAGE_KEYS.planner).some((entry) => entry.id === id);

    const setHeartState = (button, active) => {
        button.setAttribute('aria-pressed', active ? 'true' : 'false');
        wishlistActiveClasses.forEach((className) => button.classList.toggle(className, active));
        wishlistInactiveClasses.forEach((className) => button.classList.toggle(className, !active));

        const icon = button.querySelector('[data-tour-heart-icon]');
        if (icon) {
            icon.textContent = active ? 'favorite' : 'favorite_border';
        }
    };

    const syncWishlistButtons = () => {
        document.querySelectorAll('[data-tour-wishlist-toggle]').forEach((button) => {
            const card = button.closest('[data-tour-id]');
            if (!card) {
                return;
            }

            setHeartState(button, isWishlisted(card.dataset.tourId));
        });
    };

    const setPlannerState = (button, active) => {
        button.setAttribute('aria-pressed', active ? 'true' : 'false');
        button.dataset.plannerActive = active ? 'true' : 'false';

        const icon = button.querySelector('[data-tour-planner-icon]');
        if (icon) {
            icon.textContent = active ? 'playlist_add_check' : 'playlist_add';
        }
    };

    const syncPlannerButtons = () => {
        document.querySelectorAll('[data-tour-planner-toggle]').forEach((button) => {
            const card = button.closest('[data-tour-id]');
            if (!card) {
                return;
            }

            setPlannerState(button, isInPlanner(card.dataset.tourId));
        });
    };

    const toCardMarkup = (item) => `
        <a href="${item.url}" class="rounded-[1.5rem] border border-slate-200 bg-white px-4 py-4 transition hover:border-primary/30 hover:shadow-lg dark:border-slate-800 dark:bg-slate-950">
            <div class="flex items-center justify-between gap-4">
                <div>
                    <p class="text-sm font-black text-slate-900 dark:text-white">${item.name}</p>
                    <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">${item.location}</p>
                </div>
                <span class="text-sm font-black text-primary">${currencyFormatter.format(item.price)}\u20ab</span>
            </div>
        </a>`;

    const render = () => {
        const wishlistShell = document.getElementById('wishlist-shell');
        const recentShell = document.getElementById('recently-viewed-shell');
        const wishlist = parseItems(STORAGE_KEYS.wishlist);
        const recent = parseItems(STORAGE_KEYS.recent);

        if (wishlistShell) {
            const wishlistEmptyText = wishlistShell.dataset.emptyText || 'Chưa có tour nào trong wishlist.';
            wishlistShell.innerHTML = wishlist.length
                ? wishlist.map(toCardMarkup).join('')
                : `<p class="text-sm text-slate-500 dark:text-slate-400">${wishlistEmptyText}</p>`;
        }

        if (recentShell) {
            const recentEmptyText = recentShell.dataset.emptyText || 'Chưa có tour nào vừa xem.';
            recentShell.innerHTML = recent.length
                ? recent.map(toCardMarkup).join('')
                : `<p class="text-sm text-slate-500 dark:text-slate-400">${recentEmptyText}</p>`;
        }

        syncWishlistButtons();
        syncPlannerButtons();
    };

    document.querySelectorAll('.record-view').forEach((link) => {
        link.addEventListener('click', () => {
            const card = link.closest('[data-tour-id]');
            if (!card) {
                return;
            }

            const item = buildItemFromCard(card);
            const recent = parseItems(STORAGE_KEYS.recent).filter((entry) => entry.id !== item.id);
            recent.unshift(item);
            saveItems(STORAGE_KEYS.recent, recent);
        });
    });

    document.querySelectorAll('[data-tour-wishlist-toggle]').forEach((button) => {
        button.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();

            const card = button.closest('[data-tour-id]');
            if (!card) {
                return;
            }

            const item = buildItemFromCard(card);
            const wishlist = parseItems(STORAGE_KEYS.wishlist).filter((entry) => entry.id !== item.id);
            const currentlySaved = isWishlisted(item.id);

            if (!currentlySaved) {
                wishlist.unshift(item);
            }

            saveItems(STORAGE_KEYS.wishlist, wishlist);
            render();
        });
    });

    document.querySelectorAll('[data-tour-planner-toggle]').forEach((button) => {
        button.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();

            const card = button.closest('[data-tour-id]');
            if (!card) {
                return;
            }

            const item = buildItemFromCard(card);
            const planner = parseItems(STORAGE_KEYS.planner).filter((entry) => entry.id !== item.id);
            const currentlySaved = isInPlanner(item.id);

            if (!currentlySaved) {
                planner.unshift(item);
            }

            saveItems(STORAGE_KEYS.planner, planner);
            window.dispatchEvent(new CustomEvent('hvtravel:trip-planner-changed'));
            render();
        });
    });

    render();
})();
