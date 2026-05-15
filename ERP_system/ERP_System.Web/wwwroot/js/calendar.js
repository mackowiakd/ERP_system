document.addEventListener('DOMContentLoaded', function () {
    // DOM Elements
    const calendarView = document.getElementById('calendar-view');
    const eventsList = document.getElementById('events-list');
    const currentDateElement = document.getElementById('current-date');
    const todayBtn = document.getElementById('today-btn');
    const prevBtn = document.getElementById('prev-btn');
    const nextBtn = document.getElementById('next-btn');
    const viewOptions = document.querySelectorAll('.view-option');
    const addEventBtn = document.getElementById('add-event-btn');
    const eventModal = document.getElementById('event-modal');
    const eventDetailsModal = document.getElementById('event-details-modal');
    const closeBtns = document.querySelectorAll('.close-btn');
    const eventForm = document.getElementById('event-form');
    const detailsTitle = document.getElementById('details-title');
    const detailsDate = document.getElementById('details-date');
    const detailsTime = document.getElementById('details-time');
    const detailsID = document.getElementById('details-id');
    const detailsDescription = document.getElementById('details-description');
    const detailsDescription2 = document.getElementById('details-description2');
    const detailsDescription3 = document.getElementById('details-description3');
    const deleteEventBtn = document.getElementById('delete-event-btn');
    const editEventBtn = document.getElementById('edit-event-btn');
    
    // Stop Recurrence button
    const stopRecurrenceBtn = document.createElement('button');
    stopRecurrenceBtn.id = 'stop-recurrence-btn';
    stopRecurrenceBtn.textContent = 'Przerwij cykl';
    stopRecurrenceBtn.className = 'btn-secondary';
    stopRecurrenceBtn.style.backgroundColor = '#f6c23e';
    stopRecurrenceBtn.style.color = 'white';
    stopRecurrenceBtn.style.display = 'none';
    stopRecurrenceBtn.style.marginLeft = '10px';
    stopRecurrenceBtn.style.padding = '5px 10px';
    stopRecurrenceBtn.style.border = 'none';
    stopRecurrenceBtn.style.borderRadius = '4px';
    stopRecurrenceBtn.style.cursor = 'pointer';
    
    if (deleteEventBtn && deleteEventBtn.parentNode) {
        deleteEventBtn.parentNode.appendChild(stopRecurrenceBtn);
    }

    const closeDetailsBtn = document.getElementById('close-details-btn');
    const dateInput = document.getElementById('event-date');
    const amountInput = document.getElementById('event-amount');

    // App State
    let currentView = 'month';
    let currentDate = new Date();
    let events = [];
    let selectedEventId = null;

    // Initialize the app
    init();

    function init() {
        fetchEvents().then(() => {
            switchView(currentView);
            renderEventsList();
            setupEventListeners();
        });
    }

    async function fetchEvents() {
        try {
            let start, end;
            if (currentView === 'month') {
                start = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
                end = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0);
            } else if (currentView === 'week') {
                start = new Date(currentDate);
                start.setDate(currentDate.getDate() - currentDate.getDay());
                end = new Date(start);
                end.setDate(start.getDate() + 6);
            } else {
                start = new Date(currentDate);
                end = new Date(currentDate);
            }

            const bufferStart = new Date(start);
            bufferStart.setMonth(start.getMonth() - 1);
            const bufferEnd = new Date(end);
            bufferEnd.setMonth(end.getMonth() + 1);

            const url = `/api/calendar-events?start=${bufferStart.toISOString()}&end=${bufferEnd.toISOString()}&t=${new Date().getTime()}`;
            const response = await fetch(url);
            if (response.ok) {
                events = await response.json();
            } else {
                console.error('Failed to fetch events');
            }
        } catch (error) {
            console.error('Error fetching events:', error);
        }
    }

    function setupEventListeners() {
        stopRecurrenceBtn.addEventListener('click', stopRecurrence);
        
        todayBtn.addEventListener('click', async () => {
            currentDate = new Date();
            await fetchEvents();
            renderCalendar();
            renderEventsList();
        });
        prevBtn.addEventListener('click', async () => {
            navigatePrevious();
            await fetchEvents();
            renderCalendar();
            renderEventsList();
        });
        nextBtn.addEventListener('click', async () => {
            navigateNext();
            await fetchEvents();
            renderCalendar();
            renderEventsList();
        });

        viewOptions.forEach(option => {
            option.addEventListener('click', async () => {
                currentView = option.dataset.view;
                await fetchEvents();
                switchView(option.dataset.view);
                renderEventsList();
            });
        });

        addEventBtn.addEventListener('click', () => {
            window.location.href = '/new-invoice?returnUrl=' + encodeURIComponent('/calendar');
        });

        deleteEventBtn.addEventListener('click', deleteEvent);
        editEventBtn.addEventListener('click', editEvent);
        closeDetailsBtn.addEventListener('click', closeModals);
        
        closeBtns.forEach(btn => {
            btn.addEventListener('click', closeModals);
        });

        window.addEventListener('click', (e) => {
            if (e.target === eventDetailsModal || e.target === eventModal) {
                closeModals();
            }
        });
    }

    function renderCalendar() {
        calendarView.innerHTML = '';
        switch (currentView) {
            case 'day': renderDayView(); break;
            case 'week': renderWeekView(); break;
            case 'month': renderMonthView(); break;
        }
        updateCurrentDateDisplay();
    }

    function renderMonthView() {
        const monthContainer = document.createElement('div');
        monthContainer.className = 'month-view';
        const firstDay = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
        const lastDay = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0);
        const daysInMonth = lastDay.getDate();
        const startingDay = firstDay.getDay();

        const monthHeader = document.createElement('div');
        monthHeader.className = 'month-header';
        const dayNames = ['Ndz', 'Pon', 'Wt', 'Śr', 'Czw', 'Pt', 'Sob'];
        dayNames.forEach(day => {
            const dayElement = document.createElement('div');
            dayElement.className = 'day-header';
            dayElement.textContent = day;
            monthHeader.appendChild(dayElement);
        });
        monthContainer.appendChild(monthHeader);

        const daysGrid = document.createElement('div');
        daysGrid.className = 'month-days';

        for (let i = 0; i < startingDay; i++) {
            const prevMonthDay = new Date(currentDate.getFullYear(), currentDate.getMonth(), 0 - (startingDay - i - 1));
            daysGrid.appendChild(createDayCell(prevMonthDay, true));
        }

        const today = new Date();
        for (let i = 1; i <= daysInMonth; i++) {
            const dayDate = new Date(currentDate.getFullYear(), currentDate.getMonth(), i);
            const isToday = dayDate.getDate() === today.getDate() && dayDate.getMonth() === today.getMonth() && dayDate.getFullYear() === today.getFullYear();
            daysGrid.appendChild(createDayCell(dayDate, false, isToday));
        }

        const totalCells = Math.ceil((startingDay + daysInMonth) / 7) * 7;
        const remainingCells = totalCells - (startingDay + daysInMonth);
        for (let i = 1; i <= remainingCells; i++) {
            const nextMonthDay = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, i);
            daysGrid.appendChild(createDayCell(nextMonthDay, true));
        }

        monthContainer.appendChild(daysGrid);
        calendarView.appendChild(monthContainer);
    }

    function createDayCell(date, isOtherMonth, isToday = false) {
        const dayCell = document.createElement('div');
        dayCell.className = `day-cell ${isOtherMonth ? 'other-month' : ''} ${isToday ? 'current-day' : ''}`;
        const dayNumber = document.createElement('div');
        dayNumber.className = 'day-number';
        dayNumber.textContent = date.getDate();
        dayCell.appendChild(dayNumber);

        const dayEvents = getEventsForDate(date);
        let expenseTotal = 0, incomeTotal = 0;

        dayEvents.forEach(evt => {
            const val = Number(evt.amount) || 0;
            if (val < 0) expenseTotal += val; else incomeTotal += val;
        });

        if (dayEvents.length > 0) {
            const summaryDiv = document.createElement('div');
            summaryDiv.className = 'day-summary';
            if (expenseTotal < 0) {
                const expEl = document.createElement('div');
                expEl.style.color = '#e74a3b';
                expEl.textContent = expenseTotal.toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' });
                summaryDiv.appendChild(expEl);
            }
            if (incomeTotal > 0) {
                const incEl = document.createElement('div');
                incEl.style.color = '#1cc88a';
                incEl.textContent = '+' + incomeTotal.toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' });
                summaryDiv.appendChild(incEl);
            }
            const countEl = document.createElement('div');
            countEl.style.fontSize = '0.65rem';
            countEl.style.color = '#858796';
            countEl.textContent = `(${dayEvents.length} tr.)`;
            summaryDiv.appendChild(countEl);
            dayCell.appendChild(summaryDiv);
        }

        dayCell.addEventListener('click', () => {
            currentDate = new Date(date);
            if (isOtherMonth && currentView === 'month') renderCalendar();
            else switchView('day');
        });

        return dayCell;
    }

    function renderEventsList() {
        if (!eventsList) return;
        eventsList.innerHTML = '';
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Deduplicate: only one upcoming instance per recurring rule
        const recurringCandidates = events
            .filter(e => (e.isRecurring === true || String(e.id).startsWith('rec_')) && new Date(e.startTime) >= today)
            .sort((a, b) => new Date(a.startTime) - new Date(b.startTime));

        const uniqueUpcoming = [];
        const seenRules = new Set();

        recurringCandidates.forEach(event => {
            const idStr = String(event.id);
            let ruleId = idStr;
            if (idStr.startsWith('rec_')) {
                const parts = idStr.split('_');
                if (parts.length >= 2) ruleId = parts[1];
            }
            if (!seenRules.has(ruleId)) {
                seenRules.add(ruleId);
                uniqueUpcoming.push(event);
            }
        });

        if (uniqueUpcoming.length === 0) {
            eventsList.innerHTML = '<div class="no-events">Brak nadchodzących transakcji cyklicznych</div>';
            return;
        }

        uniqueUpcoming.forEach(event => {
            const item = document.createElement('div');
            item.className = 'event-item';
            item.style.borderLeft = `5px solid ${event.color || '#f6c23e'}`;
            item.style.padding = '10px';
            item.style.marginBottom = '10px';
            item.style.backgroundColor = '#f8f9fa';
            item.style.borderRadius = '4px';
            item.style.cursor = 'pointer';

            const amountVal = Number(event.amount) || 0;
            item.innerHTML = `
                <div style="display:flex; justify-content:space-between; align-items: center;">
                    <div>
                        <div style="font-weight:bold;">${event.title}</div>
                        <div style="color:#858796; font-size:0.85em;"><i class="far fa-calendar"></i> ${new Date(event.startTime).toLocaleDateString('pl-PL')}</div>
                    </div>
                    <div style="color:${amountVal < 0 ? '#e74a3b' : '#1cc88a'}; font-weight:bold;">${amountVal.toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' })}</div>
                </div>
            `;
            item.addEventListener('click', () => showEventDetails(event.id));
            eventsList.appendChild(item);
        });
    }

    function renderDayView() {
        const container = document.createElement('div');
        container.style.padding = '20px';
        container.innerHTML = `<h2>${currentDate.toLocaleDateString('pl-PL', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })}</h2>`;
        const dayEvents = getEventsForDate(currentDate).sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
        if (dayEvents.length === 0) container.innerHTML += '<p style="color:#888; text-align:center;">Brak transakcji</p>';
        else dayEvents.forEach(e => container.appendChild(createEventListItem(e)));
        calendarView.appendChild(container);
    }

    function renderWeekView() {
        const container = document.createElement('div');
        container.style.padding = '20px';
        const start = new Date(currentDate); start.setDate(currentDate.getDate() - currentDate.getDay());
        const end = new Date(start); end.setDate(start.getDate() + 6);
        container.innerHTML = `<h2>Tydzień ${start.toLocaleDateString('pl-PL', { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString('pl-PL', { month: 'short', day: 'numeric', year: 'numeric' })}</h2>`;
        const weekEvents = [];
        for (let i = 0; i < 7; i++) {
            const d = new Date(start); d.setDate(start.getDate() + i);
            weekEvents.push(...getEventsForDate(d));
        }
        weekEvents.sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
        if (weekEvents.length === 0) container.innerHTML += '<p style="color:#888; text-align:center;">Brak transakcji</p>';
        else weekEvents.forEach(e => container.appendChild(createEventListItem(e, true)));
        calendarView.appendChild(container);
    }

    function createEventListItem(event, showDate = false) {
        const item = document.createElement('div');
        item.className = 'event-item-row';
        item.style.backgroundColor = '#fff'; item.style.border = '1px solid #e0e0e0'; item.style.borderRadius = '8px';
        item.style.padding = '15px'; item.style.marginBottom = '10px'; item.style.display = 'flex'; item.style.alignItems = 'center';
        item.style.borderLeft = `5px solid ${event.color}`;
        if (!event.isRecurring) item.style.cursor = 'pointer';
        const timeStr = formatTime(new Date(event.startTime));
        const dateStr = showDate ? new Date(event.startTime).toLocaleDateString('pl-PL', { month: 'short', day: 'numeric' }) + ', ' : '';
        item.innerHTML = `
            <div style="flex:1;">
                <div style="font-weight:bold; font-size:1.1em;">${event.title}${event.isRecurring ? ' <i class="fas fa-redo-alt"></i>' : ''}</div>
                <div style="color:#666; font-size:0.9em;"><i class="far fa-clock"></i> ${dateStr}${timeStr}</div>
                ${event.description ? `<div style="color:#888; font-size:0.9em;">${event.description}</div>` : ''}
            </div>
            <div style="font-weight:bold; color:${event.color}; font-size:1.1em;">${Number(event.amount).toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' })}</div>
        `;
        if (!event.isRecurring) item.addEventListener('click', () => showEventDetails(event.id));
        return item;
    }

    function getEventsForDate(date) {
        const dStr = toLocalDateString(date);
        return events.filter(e => toLocalDateString(new Date(e.startTime)) === dStr);
    }

    function toLocalDateString(date) {
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    }

    function updateCurrentDateDisplay() {
        if (currentView === 'day') currentDateElement.textContent = currentDate.toLocaleDateString('pl-PL', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
        else if (currentView === 'month') currentDateElement.textContent = currentDate.toLocaleDateString('pl-PL', { month: 'long', year: 'numeric' });
        else {
            const s = new Date(currentDate); s.setDate(currentDate.getDate() - currentDate.getDay());
            const e = new Date(s); e.setDate(s.getDate() + 6);
            currentDateElement.textContent = `${s.toLocaleDateString('pl-PL', { month: 'short', day: 'numeric' })} - ${e.toLocaleDateString('pl-PL', { month: 'short', day: 'numeric', year: 'numeric' })}`;
        }
    }

    function switchView(view) {
        currentView = view;
        viewOptions.forEach(opt => opt.classList.toggle('active', opt.dataset.view === view));
        renderCalendar();
    }

    function navigatePrevious() {
        if (currentView === 'day') currentDate.setDate(currentDate.getDate() - 1);
        else if (currentView === 'week') currentDate.setDate(currentDate.getDate() - 7);
        else currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    }

    function navigateNext() {
        if (currentView === 'day') currentDate.setDate(currentDate.getDate() + 1);
        else if (currentView === 'week') currentDate.setDate(currentDate.getDate() + 7);
        else currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    }

    function closeModals() {
        eventDetailsModal.style.display = 'none';
        eventModal.style.display = 'none';
    }

    function showEventDetails(eventId) {
        const event = events.find(e => e.id === eventId);
        if (!event) return;
        selectedEventId = eventId;
        detailsTitle.textContent = event.title;
        detailsDate.textContent = new Date(event.startTime).toLocaleDateString('pl-PL', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
        detailsTime.textContent = formatTime(new Date(event.startTime));
        detailsID.textContent = `ID: ${event.id}`;
        detailsDescription.textContent = event.description || 'Brak opisu';
        detailsDescription2.textContent = event.description2 || '';
        detailsDescription3.textContent = event.description3 || '';
        
        if (event.isRecurring) {
            editEventBtn.style.display = 'none';
            deleteEventBtn.style.display = 'none';
            stopRecurrenceBtn.style.display = 'none';
        } else {
            editEventBtn.style.display = 'inline-block';
            deleteEventBtn.style.display = 'inline-block';
            stopRecurrenceBtn.style.display = event.hasRecurringRule ? 'inline-block' : 'none';
        }
        eventDetailsModal.style.display = 'flex';
    }

    async function stopRecurrence() {
        if (!selectedEventId) return;
        if (!confirm('Przerwać cykl?')) return;
        const endpoint = selectedEventId.startsWith('trans_') ? '/api/transactions/stop-recurring/' : '/api/invoices/stop-recurring/';
        const res = await fetch(endpoint + selectedEventId, { method: 'POST' });
        if ((await res.json()).success) {
            alert('Cykl przerwany');
            await fetchEvents(); renderCalendar(); renderEventsList(); closeModals();
        }
    }

    function editEvent() {
        const event = events.find(e => e.id === selectedEventId);
        if (!event) return;
        document.getElementById('event-title').value = event.title || "";
        document.getElementById('event-amount').value = event.totalGross || event.amount || 0;
        const dt = new Date(event.startTime);
        dateInput.value = `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, '0')}-${String(dt.getDate()).padStart(2, '0')}`;
        
        eventForm.onsubmit = async (e) => {
            e.preventDefault();
            const payload = {
                invoiceNumber: document.getElementById('event-title').value,
                issueDate: dateInput.value,
                totalGross: parseFloat(document.getElementById('event-amount').value),
                type: parseInt(document.getElementById('event-type').value),
                notes: document.getElementById('event-description3').value,
                status: parseInt(document.getElementById('event-status').value)
            };
            const res = await fetch('/api/invoices/' + selectedEventId, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            if ((await res.json()).success) {
                await fetchEvents(); renderCalendar(); renderEventsList(); closeModals();
            }
        };
        closeModals();
        eventModal.style.display = 'flex';
    }

    async function deleteEvent() {
        if (!selectedEventId || !confirm('Usunąć?')) return;
        const res = await fetch('/transactions?id=' + selectedEventId, { method: 'DELETE' });
        if (res.ok) { await fetchEvents(); renderCalendar(); renderEventsList(); closeModals(); }
    }

    function formatTime(date) { return date.toLocaleTimeString('pl-PL', { hour: 'numeric', minute: '2-digit' }); }
});