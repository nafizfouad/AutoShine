import { useEffect, useState } from 'react';
import AppLayout from '../components/AppLayout';
import { PageLoader, StatusBadge, EmptyState } from '../components/UI';
import { bookingsApi, inventoryApi, usersApi, packagesApi } from '../api/services';
import { useAuth } from '../context/AuthContext';
import { Users, Calendar, Archive, Star, AlertTriangle, TrendingUp } from 'lucide-react';

// ── Admin Dashboard ────────────────────────────────────────────────────────
function AdminDashboard() {
  const [stats, setStats] = useState(null);
  const [alerts, setAlerts] = useState([]);
  const [recentBookings, setRecentBookings] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const [usersRes, bookingsRes, alertsRes, pkgRes] = await Promise.all([
          usersApi.getAll({ page: 1, pageSize: 1 }),
          bookingsApi.getAll({ page: 1, pageSize: 5 }),
          inventoryApi.getAlerts(),
          packagesApi.getAll(false),
        ]);
        setStats({
          users: usersRes.data.data.totalCount,
          bookings: bookingsRes.data.data.totalCount,
          lowStock: alertsRes.data.data.length,
          packages: pkgRes.data.data.length,
        });
        setAlerts(alertsRes.data.data.slice(0, 5));
        setRecentBookings(bookingsRes.data.data.items || []);
      } catch { /* silent */ }
      finally { setLoading(false); }
    })();
  }, []);

  if (loading) return <PageLoader />;

  return (
    <>
      <div className="stats-grid">
        {[
          { label: 'Total Users', value: stats?.users ?? '—', icon: <Users size={20} />, color: '#6366f1', bg: 'rgba(99,102,241,0.15)' },
          { label: 'Total Bookings', value: stats?.bookings ?? '—', icon: <Calendar size={20} />, color: '#10b981', bg: 'rgba(16,185,129,0.15)' },
          { label: 'Low Stock Alerts', value: stats?.lowStock ?? '—', icon: <AlertTriangle size={20} />, color: '#f59e0b', bg: 'rgba(245,158,11,0.15)' },
          { label: 'Active Packages', value: stats?.packages ?? '—', icon: <TrendingUp size={20} />, color: '#3b82f6', bg: 'rgba(59,130,246,0.15)' },
        ].map(({ label, value, icon, color, bg }) => (
          <div className="stat-card" key={label}>
            <div className="stat-icon" style={{ background: bg, color }}>{icon}</div>
            <div className="stat-value">{value}</div>
            <div className="stat-label">{label}</div>
          </div>
        ))}
      </div>

      <div className="dashboard-grid">
        <div className="card">
          <div className="card-header">
            <div><div className="card-title">Recent Bookings</div><div className="card-subtitle">Last 5 bookings across all customers</div></div>
          </div>
          {recentBookings.length === 0 ? <EmptyState icon="📅" title="No bookings yet" /> : (
            <div className="table-wrapper">
              <table>
                <thead><tr><th>Customer</th><th>Package</th><th>Date</th><th>Status</th></tr></thead>
                <tbody>
                  {recentBookings.map((b) => (
                    <tr key={b.id}>
                      <td>{b.customerName}</td>
                      <td>{b.packageName}</td>
                      <td>{new Date(b.startTime).toLocaleDateString()}</td>
                      <td><StatusBadge status={b.status} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="card">
          <div className="card-header">
            <div><div className="card-title">⚠️ Low Stock</div><div className="card-subtitle">Items below threshold</div></div>
          </div>
          {alerts.length === 0
            ? <div className="alert alert-success">✅ All inventory levels are healthy!</div>
            : alerts.map((item) => (
              <div key={item.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 0', borderBottom: '1px solid var(--border-light)' }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600 }}>{item.itemName}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-dim)' }}>{item.sku}</div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div style={{ fontSize: 14, color: 'var(--danger)', fontWeight: 700 }}>{item.currentStock} {item.unit}</div>
                  <div style={{ fontSize: 11, color: 'var(--text-dim)' }}>min: {item.minimumThreshold}</div>
                </div>
              </div>
            ))}
        </div>
      </div>
    </>
  );
}

// ── Employee Dashboard ─────────────────────────────────────────────────────
function EmployeeDashboard() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();

  useEffect(() => {
    bookingsApi.getMy().then((res) => {
      setBookings(res.data.data || []);
    }).catch(() => {}).finally(() => setLoading(false));
  }, []);

  const today = bookings.filter((b) => {
    const d = new Date(b.startTime);
    return d.toDateString() === new Date().toDateString();
  });

  const upcoming = bookings.filter((b) =>
    new Date(b.startTime) > new Date() && b.status !== 'Cancelled'
  ).slice(0, 5);

  if (loading) return <PageLoader />;

  return (
    <>
      <div className="stats-grid">
        {[
          { label: "Today's Jobs", value: today.length, color: '#6366f1', bg: 'rgba(99,102,241,0.15)' },
          { label: 'Pending', value: bookings.filter(b => b.status === 'Pending').length, color: '#f59e0b', bg: 'rgba(245,158,11,0.15)' },
          { label: 'In Progress', value: bookings.filter(b => b.status === 'InProgress').length, color: '#3b82f6', bg: 'rgba(59,130,246,0.15)' },
          { label: 'Completed', value: bookings.filter(b => b.status === 'Completed').length, color: '#10b981', bg: 'rgba(16,185,129,0.15)' },
        ].map(({ label, value, color, bg }) => (
          <div className="stat-card" key={label}>
            <div className="stat-icon" style={{ background: bg, color, fontSize: 20 }}>📋</div>
            <div className="stat-value">{value}</div>
            <div className="stat-label">{label}</div>
          </div>
        ))}
      </div>
      <div className="card">
        <div className="card-header">
          <div><div className="card-title">Upcoming Assignments</div></div>
        </div>
        {upcoming.length === 0 ? <EmptyState icon="🎉" title="All clear!" message="No upcoming bookings." /> : (
          <div className="table-wrapper">
            <table>
              <thead><tr><th>Customer</th><th>Package</th><th>Date & Time</th><th>Status</th></tr></thead>
              <tbody>
                {upcoming.map((b) => (
                  <tr key={b.id}>
                    <td>{b.customerName}</td>
                    <td>{b.packageName}</td>
                    <td>{new Date(b.startTime).toLocaleString()}</td>
                    <td><StatusBadge status={b.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  );
}

// ── Customer Dashboard ─────────────────────────────────────────────────────
function CustomerDashboard() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const { user } = useAuth();

  useEffect(() => {
    bookingsApi.getMy().then((res) => setBookings(res.data.data || []))
      .catch(() => {}).finally(() => setLoading(false));
  }, []);

  const upcoming = bookings.filter((b) => new Date(b.startTime) >= new Date() && b.status !== 'Cancelled');
  const past = bookings.filter((b) => b.status === 'Completed');

  if (loading) return <PageLoader />;

  return (
    <>
      <div className="stats-grid">
        {[
          { label: 'Upcoming', value: upcoming.length, icon: '📅', color: '#6366f1', bg: 'rgba(99,102,241,0.15)' },
          { label: 'Completed', value: past.length, icon: '✅', color: '#10b981', bg: 'rgba(16,185,129,0.15)' },
          { label: 'Total Bookings', value: bookings.length, icon: '📋', color: '#3b82f6', bg: 'rgba(59,130,246,0.15)' },
        ].map(({ label, value, icon, color, bg }) => (
          <div className="stat-card" key={label}>
            <div className="stat-icon" style={{ background: bg, color, fontSize: 20 }}>{icon}</div>
            <div className="stat-value">{value}</div>
            <div className="stat-label">{label}</div>
          </div>
        ))}
      </div>

      <div className="card">
        <div className="card-header">
          <div className="card-title">Upcoming Appointments</div>
        </div>
        {upcoming.length === 0
          ? <EmptyState icon="📅" title="No upcoming appointments" message="Book a service to get started!" />
          : (
            <div className="table-wrapper">
              <table>
                <thead><tr><th>Package</th><th>Employee</th><th>Date & Time</th><th>Price</th><th>Status</th></tr></thead>
                <tbody>
                  {upcoming.map((b) => (
                    <tr key={b.id}>
                      <td style={{ fontWeight: 600 }}>{b.packageName}</td>
                      <td>{b.employeeName || 'TBD'}</td>
                      <td>{new Date(b.startTime).toLocaleString()}</td>
                      <td>${b.packagePrice?.toFixed(2)}</td>
                      <td><StatusBadge status={b.status} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
      </div>
    </>
  );
}

// ── Router ─────────────────────────────────────────────────────────────────
export default function DashboardPage() {
  const { isAdmin, isEmployee } = useAuth();
  const title = isAdmin ? 'Admin Dashboard' : isEmployee ? 'My Schedule' : 'My Dashboard';
  const Body = isAdmin ? AdminDashboard : isEmployee ? EmployeeDashboard : CustomerDashboard;

  return (
    <AppLayout title={title}>
      <Body />
    </AppLayout>
  );
}
