import { useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const MONTHS = ['January','February','March','April','May','June',
                 'July','August','September','October','November','December'];

export default function Calendar({ selected, onChange, disabledDays = [0], minDate = new Date() }) {
  const today = new Date(); today.setHours(0,0,0,0);
  const min = new Date(minDate); min.setHours(0,0,0,0);

  const [view, setView] = useState(() => {
    const d = selected ? new Date(selected) : new Date();
    return { year: d.getFullYear(), month: d.getMonth() };
  });

  const prevMonth = () => setView(v =>
    v.month === 0 ? { year: v.year - 1, month: 11 } : { year: v.year, month: v.month - 1 });
  const nextMonth = () => setView(v =>
    v.month === 11 ? { year: v.year + 1, month: 0 } : { year: v.year, month: v.month + 1 });

  const firstDay = new Date(view.year, view.month, 1).getDay();
  const daysInMonth = new Date(view.year, view.month + 1, 0).getDate();
  const daysInPrev = new Date(view.year, view.month, 0).getDate();

  const cells = [];
  for (let i = firstDay - 1; i >= 0; i--)
    cells.push({ day: daysInPrev - i, month: view.month - 1, year: view.month === 0 ? view.year - 1 : view.year, other: true });
  for (let d = 1; d <= daysInMonth; d++)
    cells.push({ day: d, month: view.month, year: view.year, other: false });
  while (cells.length % 7 !== 0)
    cells.push({ day: cells.length - daysInMonth - firstDay + 1, month: view.month + 1, year: view.month === 11 ? view.year + 1 : view.year, other: true });

  const isToday = (c) => !c.other && c.day === today.getDate() && c.month === today.getMonth() && c.year === today.getFullYear();
  const isSelected = (c) => {
    if (!selected || c.other) return false;
    const s = new Date(selected);
    return c.day === s.getDate() && c.month === s.getMonth() && c.year === s.getFullYear();
  };
  const isDisabled = (c) => {
    const d = new Date(c.year, c.month, c.day);
    return c.other || d < min || disabledDays.includes(d.getDay());
  };

  const handleClick = (c) => {
    if (isDisabled(c)) return;
    onChange(new Date(c.year, c.month, c.day));
  };

  return (
    <div className="calendar">
      <div className="calendar-header">
        <button className="calendar-nav" onClick={prevMonth}><ChevronLeft size={14} /></button>
        <span className="calendar-month">{MONTHS[view.month]} {view.year}</span>
        <button className="calendar-nav" onClick={nextMonth}><ChevronRight size={14} /></button>
      </div>
      <div className="calendar-grid">
        {DAYS.map(d => <div key={d} className="calendar-day-label">{d}</div>)}
        {cells.map((c, i) => (
          <div
            key={i}
            onClick={() => handleClick(c)}
            className={[
              'calendar-day',
              c.other ? 'other-month' : '',
              isToday(c) && !isSelected(c) ? 'today' : '',
              isSelected(c) ? 'selected' : '',
              isDisabled(c) ? 'disabled' : '',
            ].filter(Boolean).join(' ')}
          >
            {c.day}
          </div>
        ))}
      </div>
    </div>
  );
}
