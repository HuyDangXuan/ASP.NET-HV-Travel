(function () {
    let activeTourInput = null;
    let tourCurrentDate = new Date();

    function getDropdown() {
        return document.getElementById('tour-calendar-dropdown');
    }

    function renderTourCalendar() {
        const monthYearEl = document.getElementById('tour-calendar-month-year');
        const daysEl = document.getElementById('tour-calendar-days');
        if (!daysEl || !monthYearEl) return;

        const year = tourCurrentDate.getFullYear();
        const month = tourCurrentDate.getMonth();
        const monthNames = ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'];
        monthYearEl.innerText = `${monthNames[month]}, ${year}`;

        daysEl.innerHTML = '';
        const firstDay = new Date(year, month, 1).getDay();
        const startOffset = (firstDay + 6) % 7;
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const prevMonthDays = new Date(year, month, 0).getDate();

        for (let i = 0; i < startOffset; i++) {
            const dayNum = prevMonthDays - startOffset + 1 + i;
            const span = document.createElement('span');
            span.className = 'h-8 w-8 flex items-center justify-center text-[11px] text-slate-300 dark:text-slate-600';
            span.innerText = dayNum;
            daysEl.appendChild(span);
        }

        const selectedVal = activeTourInput ? activeTourInput.value : null;
        const selectedDate = selectedVal ? new Date(selectedVal) : null;

        for (let i = 1; i <= daysInMonth; i++) {
            const btn = document.createElement('button');
            btn.type = 'button';
            const date = new Date(year, month, i);

            let className = 'h-8 w-8 rounded-lg text-[11px] flex items-center justify-center transition-all ';

            const isSelected = selectedDate && date.toDateString() === selectedDate.toDateString();
            const today = new Date();
            const isToday = date.toDateString() === today.toDateString();

            if (isSelected) {
                className += 'bg-primary text-white font-bold shadow-sm shadow-primary/30';
            } else {
                className += 'hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 ';
                if (isToday) className += 'ring-1 ring-primary/50 text-primary font-bold';
            }

            btn.className = className;
            btn.innerText = i;
            btn.onclick = function (event) {
                event.stopPropagation();
                selectTourDate(date);
            };

            daysEl.appendChild(btn);
        }
    }

    function closeTourCalendar() {
        const dropdown = getDropdown();
        if (!dropdown) return;

        dropdown.classList.remove('scale-100', 'opacity-100');
        setTimeout(function () {
            dropdown.classList.add('hidden');
        }, 200);
        activeTourInput = null;
    }

    function openTourCalendar(input) {
        activeTourInput = input;
        const dropdown = getDropdown();
        if (!dropdown) return;

        const rect = input.getBoundingClientRect();
        dropdown.style.top = rect.bottom + 8 + 'px';
        dropdown.style.left = rect.left + 'px';

        if (rect.left + 320 > window.innerWidth) {
            dropdown.style.left = rect.right - 320 + 'px';
        }

        if (input.value) {
            const valDate = new Date(input.value);
            if (!isNaN(valDate)) tourCurrentDate = valDate;
        } else {
            tourCurrentDate = new Date();
        }

        renderTourCalendar();
        dropdown.classList.remove('hidden');
        requestAnimationFrame(function () {
            dropdown.classList.add('scale-100', 'opacity-100');
        });
    }

    function changeTourMonth(offset) {
        tourCurrentDate.setMonth(tourCurrentDate.getMonth() + offset);
        renderTourCalendar();
    }

    function selectTourDate(date) {
        if (activeTourInput) {
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            activeTourInput.value = `${year}-${month}-${day}`;
            activeTourInput.dispatchEvent(new Event('input', { bubbles: true }));
        }

        closeTourCalendar();
    }

    function reindexStartDates() {
        const items = document.querySelectorAll('#start-dates-container .date-item input');
        items.forEach(function (input, idx) {
            input.name = `StartDates[${idx}]`;
        });
    }

    function addStartDate() {
        const container = document.getElementById('start-dates-container');
        if (!container) return;

        const index = container.querySelectorAll('.date-item').length;
        const html = `
            <div class="relative group date-item">
                <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-[18px] group-hover:text-primary transition-colors z-20">calendar_today</span>
                <input name="StartDates[${index}]" type="text" readonly
                       onclick="openTourCalendar(this)" placeholder="Chọn ngày..."
                       class="date-field-custom pl-10 pr-10 cursor-pointer" />
                <button type="button" onclick="removeDate(this)" class="absolute right-2 top-1/2 -translate-y-1/2 size-8 flex items-center justify-center text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-all z-20">
                    <span class="material-symbols-outlined text-[18px]">delete</span>
                </button>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', html);
    }

    function removeDate(btn) {
        btn.closest('.date-item')?.remove();
        reindexStartDates();
    }

    document.addEventListener('click', function (event) {
        const tourCalendar = getDropdown();
        if (tourCalendar && !tourCalendar.classList.contains('hidden')) {
            if (!event.target.closest('#tour-calendar-dropdown') && !event.target.closest('.date-field-custom')) {
                closeTourCalendar();
            }
        }
    });

    window.openTourCalendar = openTourCalendar;
    window.changeTourMonth = changeTourMonth;
    window.selectTourDate = selectTourDate;
    window.closeTourCalendar = closeTourCalendar;
    window.addStartDate = addStartDate;
    window.removeDate = removeDate;
})();
