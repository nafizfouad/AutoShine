import { useState, useEffect } from 'react';
import { bookingsApi, reviewsApi } from '../api/services';
import { useAuth } from '../context/AuthContext';
import toast from 'react-hot-toast';

function StarPicker({ value, onChange }) {
  const [hovered, setHovered] = useState(0);
  return (
    <div className="stars" style={{ gap: 6 }}>
      {[1, 2, 3, 4, 5].map((n) => (
        <button
          key={n}
          type="button"
          className="star"
          style={{
            fontSize: 24,
            color: n <= (hovered || value) ? 'var(--accent)' : 'var(--text-dim)',
            background: 'none', border: 'none', padding: 0,
          }}
          onMouseEnter={() => setHovered(n)}
          onMouseLeave={() => setHovered(0)}
          onClick={() => onChange(n)}
        >
          ★
        </button>
      ))}
    </div>
  );
}

export default function ReviewModal({ booking, onClose, onSubmitted }) {
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [loading, setLoading] = useState(false);
  const [existingReview, setExistingReview] = useState(null);
  const [checking, setChecking] = useState(true);

  useEffect(() => {
    reviewsApi.getByBooking(booking.id)
      .then((res) => setExistingReview(res.data.data))
      .catch(() => setExistingReview(null))
      .finally(() => setChecking(false));
  }, [booking.id]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!rating) return toast.error('Please select a rating.');
    setLoading(true);
    try {
      await reviewsApi.create({
        bookingId: booking.id,
        employeeId: booking.employeeId,
        rating,
        comment,
      });
      toast.success('Review submitted! Thank you.');
      onSubmitted?.();
      onClose();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to submit review.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2 className="modal-title">Rate Your Service</h2>
          <button className="btn btn-secondary btn-sm" onClick={onClose}>✕</button>
        </div>

        {checking ? (
          <div className="page-loader">Checking review status…</div>
        ) : existingReview ? (
          <div>
            <div className="alert alert-success">✅ You've already reviewed this booking.</div>
            <div style={{ display: 'flex', gap: 6 }}>
              {[1,2,3,4,5].map((n) => (
                <span key={n} style={{ fontSize: 22, color: n <= existingReview.rating ? 'var(--accent)' : 'var(--text-dim)' }}>★</span>
              ))}
            </div>
            {existingReview.comment && (
              <p style={{ marginTop: 10, fontSize: 14, color: 'var(--text-muted)' }}>{existingReview.comment}</p>
            )}
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={onClose}>Close</button>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: 6, fontSize: 13, color: 'var(--text-muted)' }}>
              <strong>{booking.packageName}</strong> with {booking.employeeName || 'your technician'}
            </div>
            <div className="form-group">
              <label className="form-label">Your Rating</label>
              <StarPicker value={rating} onChange={setRating} />
            </div>
            <div className="form-group">
              <label className="form-label">Comment (optional)</label>
              <textarea
                className="form-control"
                placeholder="Share your experience…"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                rows={4}
              />
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="btn btn-primary" disabled={loading || !rating}>
                {loading ? 'Submitting…' : 'Submit Review'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
