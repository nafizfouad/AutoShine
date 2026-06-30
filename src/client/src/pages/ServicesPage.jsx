import { useEffect, useState } from 'react';
import AppLayout from '../components/AppLayout';
import { PageLoader, EmptyState } from '../components/UI';
import { packagesApi, bookingsApi } from '../api/services';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { Clock, DollarSign, Wrench } from 'lucide-react';
import toast from 'react-hot-toast';

export default function ServicesPage() {
  const [packages, setPackages] = useState([]);
  const [loading, setLoading] = useState(true);
  const [booking, setBooking] = useState(null); // {pkg, step}
  const [slots, setSlots] = useState([]);
  const [slotsLoading, setSlotsLoading] = useState(false);
  const [selectedDate, setSelectedDate] = useState('');
  const [selectedSlot, setSelectedSlot] = useState(null);
  const [creating, setCreating] = useState(false);
  const { isCustomer } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    packagesApi.getAll(true).then((res) => setPackages(res.data.data || []))
      .catch(() => {}).finally(() => setLoading(false));
  }, []);

  const fetchSlots = async (date) => {
    if (!booking?.pkg || !date) return;
    setSlotsLoading(true);
    setSlots([]);
    setSelectedSlot(null);
    try {
      const res = await bookingsApi.getAvailableSlots(booking.pkg.id, date);
      setSlots(res.data.data || []);
    } catch { toast.error('Failed to load slots.'); }
    finally { setSlotsLoading(false); }
  };

  const handleDateChange = (e) => {
    setSelectedDate(e.target.value);
    fetchSlots(e.target.value);
  };

  const handleBook = async () => {
    if (!selectedSlot) return;
    setCreating(true);
    try {
      await bookingsApi.create({
        packageId: booking.pkg.id,
        startTime: selectedSlot.startTime,
        preferredEmployeeId: null,
      });
      toast.success('Booking confirmed! 🎉');
      setBooking(null);
      navigate('/bookings');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Booking failed.');
    } finally { setCreating(false); }
  };

  const minDate = new Date().toISOString().split('T')[0];

  return (
    <AppLayout title="Service Packages">
      {loading ? <PageLoader /> : (
        <>
          <p style={{ color: 'var(--text-muted)', marginBottom: 24, fontSize: 14 }}>
            Choose a service package to book an appointment.
          </p>
          <div className="package-grid">
            {packages.map((pkg) => (
              <div className="package-card" key={pkg.id}>
                <div className="package-name">{pkg.name}</div>
                <div className="package-desc">{pkg.description}</div>
                <div className="package-price">${pkg.price.toFixed(2)} <span>per service</span></div>
                <div className="package-meta">
                  <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    <Clock size={13} /> {pkg.estimatedDurationMinutes} min
                  </span>
                  <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    <Wrench size={13} /> {pkg.items?.length ?? 0} items
                  </span>
                </div>
                {isCustomer && (
                  <button className="btn btn-primary" style={{ marginTop: 18, width: '100%', justifyContent: 'center' }}
                    onClick={() => setBooking({ pkg })}>
                    Book Now
                  </button>
                )}
              </div>
            ))}
          </div>
        </>
      )}

      {/* Booking Modal */}
      {booking && (
        <div className="modal-overlay" onClick={() => setBooking(null)}>
          <div className="modal modal-wide" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">Book — {booking.pkg.name}</h2>
              <button className="btn btn-secondary btn-sm" onClick={() => setBooking(null)}>✕</button>
            </div>

            <div className="form-group">
              <label className="form-label">Select Date</label>
              <input className="form-control" type="date" min={minDate} value={selectedDate} onChange={handleDateChange} />
            </div>

            {slotsLoading && <PageLoader text="Loading available slots…" />}

            {!slotsLoading && selectedDate && slots.length === 0 && (
              <EmptyState icon="📅" title="No slots available" message="Try a different date." />
            )}

            {slots.length > 0 && (
              <div className="form-group">
                <label className="form-label">Available Time Slots</label>
                <div className="slots-grid">
                  {slots.map((slot, i) => {
                    const label = `${new Date(slot.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} – ${new Date(slot.endTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`;
                    const sel = selectedSlot?.startTime === slot.startTime;
                    return (
                      <button key={i} className={`slot-btn${sel ? ' selected' : ''}`} onClick={() => setSelectedSlot(slot)}>
                        {label}
                      </button>
                    );
                  })}
                </div>
              </div>
            )}

            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setBooking(null)}>Cancel</button>
              <button className="btn btn-primary" disabled={!selectedSlot || creating} onClick={handleBook}>
                {creating ? 'Confirming…' : 'Confirm Booking'}
              </button>
            </div>
          </div>
        </div>
      )}
    </AppLayout>
  );
}
