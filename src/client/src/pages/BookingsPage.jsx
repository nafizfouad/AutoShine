import { useEffect, useState } from 'react';
import AppLayout from '../components/AppLayout';
import { PageLoader, StatusBadge, EmptyState, Pagination, ConfirmModal } from '../components/UI';
import ReviewModal from '../components/ReviewModal';
import { bookingsApi } from '../api/services';
import { useAuth } from '../context/AuthContext';
import toast from 'react-hot-toast';
import { Star } from 'lucide-react';

const STATUS_FLOW = {
  Pending: 'Confirmed',
  Confirmed: 'InProgress',
  InProgress: 'Completed',
};

export default function BookingsPage() {
  const { isAdmin, isEmployee, isCustomer } = useAuth();
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [cancelTarget, setCancelTarget] = useState(null);
  const [cancelling, setCancelling] = useState(false);
  const [updating, setUpdating] = useState(null);
  const [reviewBooking, setReviewBooking] = useState(null);

  const load = async (p = page) => {
    setLoading(true);
    try {
      if (isAdmin) {
        const res = await bookingsApi.getAll({ page: p, pageSize: 10, ...(statusFilter ? { status: statusFilter } : {}) });
        setBookings(res.data.data.items || []);
        setTotalPages(res.data.data.totalPages || 1);
      } else {
        const res = await bookingsApi.getMy();
        let data = res.data.data || [];
        if (statusFilter) data = data.filter((b) => b.status === statusFilter);
        setBookings(data);
        setTotalPages(1);
      }
    } catch { toast.error('Failed to load bookings.'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(page); }, [page, statusFilter]);

  const handleCancel = async () => {
    setCancelling(true);
    try {
      await bookingsApi.cancel(cancelTarget);
      toast.success('Booking cancelled.');
      setCancelTarget(null);
      load(page);
    } catch (err) {
      toast.error(err.response?.data?.message || 'Cancel failed.');
    } finally { setCancelling(false); }
  };

  const handleStatusUpdate = async (bookingId, currentStatus) => {
    const next = STATUS_FLOW[currentStatus];
    if (!next) return;
    setUpdating(bookingId);
    try {
      await bookingsApi.updateStatus(bookingId, next);
      toast.success(`Status updated to ${next}`);
      load(page);
    } catch (err) {
      toast.error(err.response?.data?.message || 'Update failed. Check inventory levels.');
    } finally { setUpdating(null); }
  };

  const STATUSES = ['', 'Pending', 'Confirmed', 'InProgress', 'Completed', 'Cancelled'];

  return (
    <AppLayout title={isAdmin ? 'All Bookings' : 'My Bookings'}>
      <div className="card">
        <div className="card-header">
          <div>
            <div className="card-title">Bookings</div>
            <div className="card-subtitle">
              {isAdmin ? 'Manage all customer appointments' : isEmployee ? 'Your assigned jobs' : 'Your appointment history'}
            </div>
          </div>
          <select className="form-control" style={{ width: 170 }} value={statusFilter}
            onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}>
            {STATUSES.map((s) => <option key={s} value={s}>{s || 'All Statuses'}</option>)}
          </select>
        </div>

        {loading ? <PageLoader /> : bookings.length === 0
          ? <EmptyState icon="📅" title="No bookings found" message={statusFilter ? `No ${statusFilter} bookings.` : 'Nothing to show yet.'} />
          : (
            <div className="table-wrapper">
              <table>
                <thead>
                  <tr>
                    {isAdmin && <th>Customer</th>}
                    <th>Package</th>
                    <th>Employee</th>
                    <th>Date &amp; Time</th>
                    <th>Price</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {bookings.map((b) => (
                    <tr key={b.id}>
                      {isAdmin && <td style={{ fontWeight: 500 }}>{b.customerName}</td>}
                      <td style={{ fontWeight: 600 }}>{b.packageName}</td>
                      <td>{b.employeeName || <span style={{ color: 'var(--text-dim)' }}>Unassigned</span>}</td>
                      <td>
                        <div>{new Date(b.startTime).toLocaleDateString()}</div>
                        <div style={{ fontSize: 12, color: 'var(--text-dim)' }}>
                          {new Date(b.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          {' – '}
                          {new Date(b.endTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </div>
                      </td>
                      <td>${b.packagePrice?.toFixed(2)}</td>
                      <td><StatusBadge status={b.status} /></td>
                      <td>
                        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                          {/* Employee/Admin: advance status */}
                          {(isEmployee || isAdmin) && STATUS_FLOW[b.status] && (
                            <button className="btn btn-sm btn-success" disabled={updating === b.id}
                              onClick={() => handleStatusUpdate(b.id, b.status)}>
                              {updating === b.id ? '…' : `→ ${STATUS_FLOW[b.status]}`}
                            </button>
                          )}
                          {/* Customer: leave review on completed booking */}
                          {isCustomer && b.status === 'Completed' && b.employeeId && (
                            <button className="btn btn-sm btn-secondary" onClick={() => setReviewBooking(b)}
                              title="Leave a review" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                              <Star size={12} /> Review
                            </button>
                          )}
                          {/* Cancel button */}
                          {b.status !== 'Completed' && b.status !== 'Cancelled' && (
                            <button className="btn btn-sm btn-danger" onClick={() => setCancelTarget(b.id)}>
                              Cancel
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <Pagination page={page} totalPages={totalPages} onChange={setPage} />
            </div>
          )}
      </div>

      {cancelTarget && (
        <ConfirmModal
          title="Cancel Booking"
          message="Are you sure you want to cancel this booking? This cannot be undone."
          onConfirm={handleCancel}
          onCancel={() => setCancelTarget(null)}
          loading={cancelling}
        />
      )}

      {reviewBooking && (
        <ReviewModal
          booking={reviewBooking}
          onClose={() => setReviewBooking(null)}
          onSubmitted={() => load(page)}
        />
      )}
    </AppLayout>
  );
}
