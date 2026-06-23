export function Spinner() {
  return <div className="spinner" />;
}

export function PageLoader({ text = 'Loading...' }) {
  return (
    <div className="page-loader">
      <Spinner /> {text}
    </div>
  );
}

export function EmptyState({ icon = '📋', title, message, action }) {
  return (
    <div className="empty-state">
      <div className="empty-icon">{icon}</div>
      <h3>{title}</h3>
      {message && <p>{message}</p>}
      {action && <div style={{ marginTop: 16 }}>{action}</div>}
    </div>
  );
}

export function StatusBadge({ status }) {
  const map = {
    Pending: 'badge-warning',
    Confirmed: 'badge-info',
    InProgress: 'badge-primary',
    Completed: 'badge-success',
    Cancelled: 'badge-danger',
    Admin: 'badge-danger',
    Employee: 'badge-info',
    Customer: 'badge-success',
  };
  return <span className={`badge ${map[status] || 'badge-muted'}`}>{status}</span>;
}

export function Pagination({ page, totalPages, onChange }) {
  if (totalPages <= 1) return null;
  return (
    <div className="pagination">
      <button className="page-btn" disabled={page <= 1} onClick={() => onChange(page - 1)}>‹</button>
      {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
        <button key={p} className={`page-btn${p === page ? ' active' : ''}`} onClick={() => onChange(p)}>{p}</button>
      ))}
      <button className="page-btn" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>›</button>
    </div>
  );
}

export function ConfirmModal({ title, message, onConfirm, onCancel, loading }) {
  return (
    <div className="modal-overlay" onClick={onCancel}>
      <div className="modal" style={{ maxWidth: 380 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2 className="modal-title">{title}</h2>
        </div>
        <p style={{ color: 'var(--text-muted)', fontSize: 14 }}>{message}</p>
        <div className="modal-footer">
          <button className="btn btn-secondary" onClick={onCancel}>Cancel</button>
          <button className="btn btn-danger" onClick={onConfirm} disabled={loading}>
            {loading ? 'Deleting...' : 'Confirm'}
          </button>
        </div>
      </div>
    </div>
  );
}
