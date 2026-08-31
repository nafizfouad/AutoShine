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

export default function Sidebar({ isOpen, onToggle }) {
  const { user, logout, isAdmin, isEmployee } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const nav = isAdmin ? adminNav : isEmployee ? employeeNav : customerNav;
  const initials = user ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase() : '?';

  return (
    <aside className={`sidebar${isOpen ? '' : ' sidebar-collapsed'}`}>
      <div className="sidebar-logo">
        <div className="logo-icon">🚗</div>
        {isOpen && <span>Auto<em>Shine</em></span>}
      </div>

      <div className="sidebar-section">
        {isOpen && <div className="sidebar-label">Menu</div>}
        {nav.map(({ label, icon: Icon, path }) => (
          <button
            key={path}
            id={`nav-${path.replace('/', '').replace('-', '_')}`}
            className={`nav-item${location.pathname === path ? ' active' : ''}${!isOpen ? ' nav-item-icon-only' : ''}`}
            onClick={() => navigate(path)}
            title={!isOpen ? label : undefined}
          >
            <Icon className="nav-icon" size={18} />
            {isOpen && label}
          </button>
        ))}
      </div>

      <div className="sidebar-footer">
        <button
          id="nav-profile"
          className={`user-chip-btn${!isOpen ? ' user-chip-btn-collapsed' : ''}`}
          onClick={() => navigate('/profile')}
          title={isOpen ? 'View/edit profile' : `${user?.firstName} ${user?.lastName}`}
        >
          <div className="user-avatar">{initials}</div>
          {isOpen && (
            <div className="user-info">
              <div className="user-name">{user?.firstName} {user?.lastName}</div>
              <div className="user-role">{user?.role}</div>
            </div>
          )}
          {isOpen && <User size={14} style={{ color: 'var(--text-dim)', flexShrink: 0 }} />}
        </button>
        <button
          id="nav-signout"
          className={`nav-item${!isOpen ? ' nav-item-icon-only' : ''}`}
          style={{ marginTop: 8, color: 'var(--danger)' }}
          onClick={() => { logout(); navigate('/login'); }}
          title={!isOpen ? 'Sign Out' : undefined}
        >
          <LogOut size={18} />
          {isOpen && 'Sign Out'}
        </button>
      </div>
    </aside>
  );
}
