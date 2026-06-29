import { useEffect, useState } from 'react';
import AppLayout from '../components/AppLayout';
import { PageLoader, EmptyState, ConfirmModal } from '../components/UI';
import { reviewsApi, usersApi } from '../api/services';
import { Trash2, Star } from 'lucide-react';
import toast from 'react-hot-toast';

function StarDisplay({ rating }) {
  return (
    <div className="stars">
      {[1,2,3,4,5].map((n) => (
        <Star key={n} size={14} className={`star${n <= rating ? ' filled' : ''}`} style={{ color: n <= rating ? 'var(--accent)' : 'var(--text-dim)', fill: n <= rating ? 'var(--accent)' : 'none' }} />
      ))}
    </div>
  );
}

export default function ReviewsPage() {
  const [employees, setEmployees] = useState([]);
  const [selectedEmp, setSelectedEmp] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [loading, setLoading] = useState(false);
  const [empLoading, setEmpLoading] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    usersApi.getAll({ role: 'Employee', pageSize: 100, page: 1 })
      .then((res) => {
        const emps = res.data.data.items || [];
        setEmployees(emps);
        if (emps.length > 0) setSelectedEmp(emps[0]);
      }).catch(() => {}).finally(() => setEmpLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedEmp) return;
    setLoading(true);
    reviewsApi.getByEmployee(selectedEmp.id)
      .then((res) => setReviews(res.data.data || []))
      .catch(() => {}).finally(() => setLoading(false));
  }, [selectedEmp]);

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await reviewsApi.delete(deleteTarget);
      toast.success('Review deleted.');
      setDeleteTarget(null);
      if (selectedEmp) {
        const res = await reviewsApi.getByEmployee(selectedEmp.id);
        setReviews(res.data.data || []);
      }
    } catch { toast.error('Delete failed.'); }
    finally { setDeleting(false); }
  };

  const avg = reviews.length > 0 ? (reviews.reduce((s, r) => s + r.rating, 0) / reviews.length).toFixed(1) : '—';

  return (
    <AppLayout title="Review Moderation">
      <div style={{ display: 'grid', gridTemplateColumns: '220px 1fr', gap: 20 }}>
        <div className="card card-sm">
          <div className="card-title" style={{ marginBottom: 12, fontSize: 14 }}>Employees</div>
          {empLoading ? <PageLoader /> : employees.map((emp) => (
            <button key={emp.id} className={`nav-item${selectedEmp?.id === emp.id ? ' active' : ''}`}
              onClick={() => setSelectedEmp(emp)}>
              <div style={{ width: 28, height: 28, borderRadius: '50%', background: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 12, fontWeight: 700 }}>
                {emp.firstName[0]}
              </div>
              {emp.firstName} {emp.lastName}
            </button>
          ))}
        </div>

        <div className="card">
          {selectedEmp && (
            <div className="card-header">
              <div>
                <div className="card-title">{selectedEmp.firstName} {selectedEmp.lastName}</div>
                <div className="card-subtitle">⭐ Avg Rating: {avg} · {reviews.length} review{reviews.length !== 1 ? 's' : ''}</div>
              </div>
            </div>
          )}

          {loading ? <PageLoader /> : reviews.length === 0
            ? <EmptyState icon="⭐" title="No reviews yet" message="Reviews appear after customers complete bookings." />
            : reviews.map((r) => (
              <div key={r.id} style={{ padding: '16px 0', borderBottom: '1px solid var(--border-light)', display: 'flex', gap: 14 }}>
                <div style={{ width: 36, height: 36, borderRadius: '50%', background: 'var(--bg-hover)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, flexShrink: 0 }}>
                  {r.customerName?.[0]}
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                    <span style={{ fontWeight: 600, fontSize: 14 }}>{r.customerName}</span>
                    <span style={{ fontSize: 12, color: 'var(--text-dim)' }}>{new Date(r.createdAt).toLocaleDateString()}</span>
                  </div>
                  <StarDisplay rating={r.rating} />
                  {r.comment && <p style={{ fontSize: 13, color: 'var(--text-muted)', marginTop: 6 }}>{r.comment}</p>}
                </div>
                <button className="btn btn-sm btn-danger btn-icon" onClick={() => setDeleteTarget(r.id)}>
                  <Trash2 size={13} />
                </button>
              </div>
            ))}
        </div>
      </div>

      {deleteTarget && (
        <ConfirmModal title="Delete Review" message="Remove this review permanently?"
          onConfirm={handleDelete} onCancel={() => setDeleteTarget(null)} loading={deleting} />
      )}
    </AppLayout>
  );
}
