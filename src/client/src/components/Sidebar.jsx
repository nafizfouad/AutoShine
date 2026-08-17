import { useAuth } from '../context/AuthContext';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, Calendar, Package, Users, Archive,
  Star, LogOut, Car, CalendarClock, User
} from 'lucide-react';

const adminNav = [
  { label: 'Dashboard', icon: LayoutDashboard, path: '/dashboard' },
  { label: 'Bookings', icon: Calendar, path: '/bookings' },
  { label: 'Packages', icon: Package, path: '/packages' },
  { label: 'Users', icon: Users, path: '/users' },
  { label: 'Inventory', icon: Archive, path: '/inventory' },
  { label: 'Schedules', icon: CalendarClock, path: '/schedule-management' },
  { label: 'Reviews', icon: Star, path: '/reviews' },
];

const employeeNav = [
  { label: 'My Schedule', icon: LayoutDashboard, path: '/dashboard' },
  { label: 'My Bookings', icon: Calendar, path: '/bookings' },
];

const customerNav = [
  { label: 'Dashboard', icon: LayoutDashboard, path: '/dashboard' },
  { label: 'Services', icon: Package, path: '/services' },
  { label: 'Book a Service', icon: Car, path: '/book' },
  { label: 'My Bookings', icon: Calendar, path: '/bookings' },
];

export default function Sidebar() {
  const { user, logout, isAdmin, isEmployee } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const nav = isAdmin ? adminNav : isEmployee ? employeeNav : customerNav;
  const initials = user ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase() : '?';

  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <div className="logo-icon">🚗</div>
        <span>Auto<em>Shine</em></span>
      </div>

      <div className="sidebar-section">
        <div className="sidebar-label">Menu</div>
        {nav.map(({ label, icon: Icon, path }) => (
          <button
            key={path}
            id={`nav-${path.replace('/', '').replace('-', '_')}`}
            className={`nav-item${location.pathname === path ? ' active' : ''}`}
            onClick={() => navigate(path)}
          >
            <Icon className="nav-icon" size={18} />
            {label}
          </button>
        ))}
      </div>

      <div className="sidebar-footer">
        {/* Clicking name/avatar goes to profile */}
        <button
          id="nav-profile"
          className="user-chip-btn"
          onClick={() => navigate('/profile')}
          title="View/edit profile"
        >
          <div className="user-avatar">{initials}</div>
          <div className="user-info">
            <div className="user-name">{user?.firstName} {user?.lastName}</div>
            <div className="user-role">{user?.role}</div>
          </div>
          <User size={14} style={{ color: 'var(--text-dim)', flexShrink: 0 }} />
        </button>
        <button
          id="nav-signout"
          className="nav-item"
          style={{ marginTop: 8, color: 'var(--danger)' }}
          onClick={() => { logout(); navigate('/login'); }}
        >
          <LogOut size={18} />
          Sign Out
        </button>
      </div>
    </aside>
  );
}
