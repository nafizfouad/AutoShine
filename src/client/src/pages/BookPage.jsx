import { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { packagesApi, bookingsApi, usersApi } from '../api/services';
import Calendar from '../components/Calendar';
import toast from 'react-hot-toast';
import { Clock, DollarSign, CheckCircle, ChevronRight, ChevronLeft, User } from 'lucide-react';

const STEPS = ['Service', 'Date', 'Time & Staff', 'Confirm'];

const fmt = (dt) => new Date(dt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
const fmtDate = (dt) => new Date(dt).toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

export default function BookPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();

  const [step, setStep] = useState(0);
  const [packages, setPackages] = useState([]);
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [selectedDate, setSelectedDate] = useState(null);
  const [slots, setSlots] = useState([]);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState(null);
  const [selectedEmployee, setSelectedEmployee] = useState(null); // null = no preference
  const [notes, setNotes] = useState('');
  const [booking, setBooking] = useState(false);
  const [done, setDone] = useState(false);

  useEffect(() => {
    packagesApi.getAll(true).then(r => {
      const pkgs = r.data.data;
      setPackages(pkgs);
      const preId = params.get('serviceId');
      if (preId) {
        const pre = pkgs.find(p => p.id === parseInt(preId));
        if (pre) { setSelectedPackage(pre); setStep(1); }
      }
    });
  }, []);

  // Load slots when date or package changes
  useEffect(() => {
    if (!selectedDate || !selectedPackage) return;
    setSlots([]); setSelectedSlot(null); setSelectedEmployee(null);
    setLoadingSlots(true);
    bookingsApi.getAvailableSlots(selectedPackage.id, selectedDate.toISOString())
      .then(r => setSlots(r.data.data || []))
      .catch(() => setSlots([]))
      .finally(() => setLoadingSlots(false));
  }, [selectedDate, selectedPackage]);

  const handleBook = async () => {
    if (!selectedSlot || !selectedPackage) return;
    setBooking(true);
    try {
      await bookingsApi.create({
        packageId: selectedPackage.id,
        startTime: selectedSlot.startTime,
        preferredEmployeeId: selectedEmployee?.id ?? null,
        notes,
      });
      setDone(true);
      toast.success('Booking confirmed! 🎉');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to create booking.');
    } finally { setBooking(false); }
  };

  const stepClass = (i) =>
    i < step ? 'done' : i === step ? 'active' : '';

  if (done) {
    return (
      <div className="page fade-in" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '60vh' }}>
        <div style={{ textAlign: 'center' }}>
          <CheckCircle size={64} style={{ color: 'var(--success)', marginBottom: 20 }} />
          <h2 style={{ fontSize: 26, fontWeight: 800, marginBottom: 8 }}>Booking Confirmed!</h2>
          <p style={{ color: 'var(--text-muted)', marginBottom: 28 }}>
            Your <strong>{selectedPackage?.name}</strong> appointment on <strong>{fmtDate(selectedSlot?.startTime)}</strong><br />
            at <strong>{fmt(selectedSlot?.startTime)}</strong> has been booked.
          </p>
          <div style={{ display: 'flex', gap: 12, justifyContent: 'center' }}>
            <button className="btn btn-secondary" onClick={() => navigate('/bookings')}>View My Bookings</button>
            <button className="btn btn-primary" onClick={() => { setDone(false); setStep(0); setSelectedPackage(null); setSelectedDate(null); setSelectedSlot(null); }}>Book Another</button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="page fade-in">
      <h1 style={{ fontSize: 22, fontWeight: 800, marginBottom: 8 }}>Book a Service</h1>
      <p style={{ color: 'var(--text-muted)', fontSize: 14, marginBottom: 32 }}>Follow the steps to schedule your appointment</p>

      {/* Step indicator */}
      <div className="wizard-steps">
        {STEPS.map((label, i) => (
          <>
            <div key={label} className={`wizard-step ${stepClass(i)}`}>
              <div className="wizard-step-num">{i < step ? '✓' : i + 1}</div>
              <span className="wizard-step-label">{label}</span>
            </div>
            {i < STEPS.length - 1 && <div key={`div-${i}`} className={`wizard-divider ${i < step ? 'done' : ''}`} />}
          </>
        ))}
      </div>

      {/* Step 0 — Service selection */}
      {step === 0 && (
        <div>
          <h2 style={{ fontSize: 16, fontWeight: 700, marginBottom: 16 }}>Choose a Service</h2>
          <div className="package-grid">
            {packages.map(pkg => (
              <div
                key={pkg.id}
                className={`service-select-card ${selectedPackage?.id === pkg.id ? 'selected' : ''}`}
                onClick={() => setSelectedPackage(pkg)}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 10 }}>
                  <div style={{ fontWeight: 700, fontSize: 16 }}>{pkg.name}</div>
                  {selectedPackage?.id === pkg.id && <CheckCircle size={18} style={{ color: 'var(--primary-light)' }} />}
                </div>
                <p style={{ fontSize: 13, color: 'var(--text-muted)', marginBottom: 14 }}>{pkg.description}</p>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontSize: 22, fontWeight: 800, color: 'var(--primary-light)' }}>${pkg.price}</span>
                  <span style={{ fontSize: 12, color: 'var(--text-dim)', display: 'flex', alignItems: 'center', gap: 4 }}>
                    <Clock size={12} /> {pkg.estimatedDurationMinutes} min
                  </span>
                </div>
              </div>
            ))}
          </div>
          <div style={{ marginTop: 28 }}>
            <button className="btn btn-primary btn-lg" disabled={!selectedPackage} onClick={() => setStep(1)}>
              Continue <ChevronRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* Step 1 — Date */}
      {step === 1 && (
        <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: 32, alignItems: 'start' }}>
          <div>
            <h2 style={{ fontSize: 16, fontWeight: 700, marginBottom: 16 }}>Choose a Date</h2>
            <Calendar
              selected={selectedDate}
              onChange={setSelectedDate}
              disabledDays={[0, 6]}  // disabled Sun & Sat by default
              minDate={new Date()}
            />
          </div>
          <div className="card" style={{ alignSelf: 'start' }}>
            <div className="card-title" style={{ marginBottom: 12 }}>Selected Service</div>
            <div style={{ fontWeight: 700, fontSize: 16 }}>{selectedPackage?.name}</div>
            <p style={{ color: 'var(--text-muted)', fontSize: 13, margin: '6px 0 16px' }}>{selectedPackage?.description}</p>
            <div style={{ display: 'flex', gap: 16 }}>
              <span style={{ fontSize: 13, color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: 4 }}>
                <Clock size={13} /> {selectedPackage?.estimatedDurationMinutes} min
              </span>
              <span style={{ fontSize: 13, color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: 4 }}>
                <DollarSign size={13} /> ${selectedPackage?.price}
              </span>
            </div>
            {selectedDate && (
              <div style={{ marginTop: 16, padding: '10px 14px', background: 'rgba(99,102,241,0.08)', borderRadius: 'var(--radius-sm)', color: 'var(--primary-light)', fontWeight: 600, fontSize: 14 }}>
                📅 {fmtDate(selectedDate)}
              </div>
            )}
          </div>
          <div style={{ display: 'flex', gap: 10 }}>
            <button className="btn btn-secondary" onClick={() => setStep(0)}><ChevronLeft size={15} /> Back</button>
            <button className="btn btn-primary" disabled={!selectedDate} onClick={() => setStep(2)}>
              Continue <ChevronRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* Step 2 — Slot + Employee */}
      {step === 2 && (
        <div>
          <h2 style={{ fontSize: 16, fontWeight: 700, marginBottom: 4 }}>Available Time Slots</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: 13, marginBottom: 20 }}>{fmtDate(selectedDate)} · {selectedPackage?.name}</p>

          {loadingSlots && <div className="page-loader"><span className="spinner" /> Loading available slots…</div>}

          {!loadingSlots && slots.length === 0 && (
            <div className="empty-state">
              <div className="empty-icon">📅</div>
              <h3>No Available Slots</h3>
              <p>Try a different date — weekdays only, 9 AM – 5 PM (break: 1–2 PM)</p>
              <button className="btn btn-secondary" style={{ marginTop: 16 }} onClick={() => setStep(1)}>Change Date</button>
            </div>
          )}

          {!loadingSlots && slots.length > 0 && (
            <>
              <div className="slot-grid" style={{ marginBottom: 28 }}>
                {slots.map((slot, i) => (
                  <div
                    key={i}
                    className={`slot-card ${selectedSlot === slot ? 'selected' : ''}`}
                    onClick={() => { setSelectedSlot(slot); setSelectedEmployee(null); }}
                  >
                    <div style={{ fontWeight: 600 }}>{fmt(slot.startTime)}</div>
                    <div style={{ fontSize: 11, color: 'var(--text-dim)', marginTop: 2 }}>→ {fmt(slot.endTime)}</div>
                    <div style={{ fontSize: 11, color: 'var(--success)', marginTop: 4 }}>
                      {slot.availableEmployees?.length} staff free
                    </div>
                  </div>
                ))}
              </div>

              {selectedSlot && (
                <div>
                  <h3 style={{ fontSize: 15, fontWeight: 700, marginBottom: 12 }}>Staff Preference</h3>

                  {/* No preference option */}
                  <div
                    className={`employee-option ${selectedEmployee === null && selectedSlot ? 'selected' : ''}`}
                    onClick={() => setSelectedEmployee(null)}
                  >
                    <div className="emp-avatar">🎲</div>
                    <div>
                      <div style={{ fontWeight: 600 }}>No Preference</div>
                      <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>First available staff will be assigned</div>
                    </div>
                    {selectedEmployee === null && <CheckCircle size={16} style={{ color: 'var(--primary-light)', marginLeft: 'auto' }} />}
                  </div>

                  {selectedSlot.availableEmployees?.map(emp => (
                    <div
                      key={emp.id}
                      className={`employee-option ${selectedEmployee?.id === emp.id ? 'selected' : ''}`}
                      onClick={() => setSelectedEmployee(emp)}
                    >
                      <div className="emp-avatar">{emp.name[0]}</div>
                      <div>
                        <div style={{ fontWeight: 600 }}>{emp.name}</div>
                        <div style={{ fontSize: 12, color: 'var(--success)' }}>Available</div>
                      </div>
                      {selectedEmployee?.id === emp.id && <CheckCircle size={16} style={{ color: 'var(--primary-light)', marginLeft: 'auto' }} />}
                    </div>
                  ))}
                </div>
              )}
            </>
          )}

          <div style={{ display: 'flex', gap: 10, marginTop: 28 }}>
            <button className="btn btn-secondary" onClick={() => setStep(1)}><ChevronLeft size={15} /> Back</button>
            <button className="btn btn-primary" disabled={!selectedSlot} onClick={() => setStep(3)}>
              Continue <ChevronRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* Step 3 — Confirm */}
      {step === 3 && (
        <div style={{ maxWidth: 520 }}>
          <h2 style={{ fontSize: 16, fontWeight: 700, marginBottom: 20 }}>Review & Confirm</h2>
          <div className="card" style={{ marginBottom: 20 }}>
            {[
              ['Service', selectedPackage?.name],
              ['Date', fmtDate(selectedDate)],
              ['Time', `${fmt(selectedSlot?.startTime)} – ${fmt(selectedSlot?.endTime)}`],
              ['Staff', selectedEmployee ? selectedEmployee.name : 'No preference (auto-assign)'],
              ['Price', `$${selectedPackage?.price}`],
            ].map(([label, val]) => (
              <div key={label} style={{ display: 'flex', justifyContent: 'space-between', padding: '10px 0', borderBottom: '1px solid var(--border-light)' }}>
                <span style={{ color: 'var(--text-muted)', fontSize: 14 }}>{label}</span>
                <span style={{ fontWeight: 600, fontSize: 14 }}>{val}</span>
              </div>
            ))}
          </div>
          <div className="form-group">
            <label className="form-label">Notes (optional)</label>
            <textarea className="form-control" rows={3} placeholder="Any special requests or details…" value={notes} onChange={e => setNotes(e.target.value)} />
          </div>
          <div style={{ display: 'flex', gap: 10, marginTop: 8 }}>
            <button className="btn btn-secondary" onClick={() => setStep(2)}><ChevronLeft size={15} /> Back</button>
            <button className="btn btn-primary btn-lg" disabled={booking} onClick={handleBook}>
              {booking ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Booking…</> : <><CheckCircle size={16} /> Confirm Booking</>}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
