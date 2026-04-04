(function () {
    const STORAGE_KEYS = {
        wishlist: 'hvtravel_wishlist',
        recent: 'hvtravel_recent'
    };

    const currencyFormatter = new Intl.NumberFormat('vi-VN');

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

    const setHeartState = (button, active) => {
        button.setAttribute('aria-pressed', active ? 'true' : 'false');
        button.classList.toggle('is-active', active);

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

    const toCardMarkup = (item) => `
        <a href="${item.url}" class="rounded-2xl border border-border-light px-4 py-4 transition hover:border-primary dark:border-border-dark">
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

    render();
})();
