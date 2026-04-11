(function () {
    const carousels = document.querySelectorAll('[data-carousel]');
    if (!carousels.length) {
        return;
    }

    const reduceMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    const activeDotClasses = ['w-12', 'bg-white', 'shadow-lg', 'shadow-slate-950/25'];
    const inactiveDotClasses = ['w-8', 'bg-white/35'];

    carousels.forEach((carousel) => {
        const track = carousel.querySelector('[data-carousel-track]');
        const slides = Array.from(carousel.querySelectorAll('[data-carousel-slide]'));
        const prevButton = carousel.querySelector('[data-carousel-prev]');
        const nextButton = carousel.querySelector('[data-carousel-next]');
        const dots = Array.from(carousel.querySelectorAll('[data-carousel-dot]'));
        const intervalMs = Number(carousel.getAttribute('data-autoplay-interval') || '5000');

        if (!track || slides.length <= 1) {
            return;
        }

        let activeIndex = 0;
        let autoplayHandle = null;
        let pointerStartX = null;

        const sync = () => {
            track.style.transform = `translate3d(-${activeIndex * 100}%, 0, 0)`;

            slides.forEach((slide, index) => {
                slide.setAttribute('aria-hidden', index === activeIndex ? 'false' : 'true');
            });

            dots.forEach((dot, index) => {
                const isActive = index === activeIndex;
                activeDotClasses.forEach((className) => dot.classList.toggle(className, isActive));
                inactiveDotClasses.forEach((className) => dot.classList.toggle(className, !isActive));
                dot.setAttribute('aria-pressed', isActive ? 'true' : 'false');
            });
        };

        const goTo = (nextIndex) => {
            activeIndex = (nextIndex + slides.length) % slides.length;
            sync();
        };

        const stopAutoplay = () => {
            if (autoplayHandle) {
                window.clearInterval(autoplayHandle);
                autoplayHandle = null;
            }
        };

        const startAutoplay = () => {
            stopAutoplay();
            if (reduceMotionQuery.matches) {
                return;
            }

            autoplayHandle = window.setInterval(() => {
                goTo(activeIndex + 1);
            }, intervalMs);
        };

        prevButton?.addEventListener('click', () => {
            goTo(activeIndex - 1);
            startAutoplay();
        });

        nextButton?.addEventListener('click', () => {
            goTo(activeIndex + 1);
            startAutoplay();
        });

        dots.forEach((dot, index) => {
            dot.addEventListener('click', () => {
                goTo(index);
                startAutoplay();
            });
        });

        carousel.addEventListener('mouseenter', stopAutoplay);
        carousel.addEventListener('mouseleave', startAutoplay);
        carousel.addEventListener('focusin', stopAutoplay);
        carousel.addEventListener('focusout', startAutoplay);

        carousel.addEventListener('pointerdown', (event) => {
            pointerStartX = event.clientX;
        });

        carousel.addEventListener('pointerup', (event) => {
            if (pointerStartX === null) {
                return;
            }

            const deltaX = event.clientX - pointerStartX;
            pointerStartX = null;

            if (Math.abs(deltaX) < 40) {
                return;
            }

            if (deltaX > 0) {
                goTo(activeIndex - 1);
            } else {
                goTo(activeIndex + 1);
            }

            startAutoplay();
        });

        carousel.addEventListener('pointercancel', () => {
            pointerStartX = null;
        });

        reduceMotionQuery.addEventListener('change', startAutoplay);
        sync();
        startAutoplay();
    });
})();
