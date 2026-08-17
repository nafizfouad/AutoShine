import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ThemeProvider } from './context/ThemeContext';
import ProtectedRoute from './components/ProtectedRoute';

import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import BookingsPage from './pages/BookingsPage';
import ServicesPage from './pages/ServicesPage';
import BookPage from './pages/BookPage';
import PackagesPage from './pages/PackagesPage';
import UsersPage from './pages/UsersPage';
import InventoryPage from './pages/InventoryPage';
import ReviewsPage from './pages/ReviewsPage';
import ProfilePage from './pages/ProfilePage';
import ScheduleManagementPage from './pages/ScheduleManagementPage';

function RootRedirect() {
  const { user } = useAuth();
  return <Navigate to={user ? '/dashboard' : '/login'} replace />;
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<RootRedirect />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* All authenticated users */}
      <Route path="/dashboard"  element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
      <Route path="/bookings"   element={<ProtectedRoute><BookingsPage /></ProtectedRoute>} />
      <Route path="/profile"    element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />

      {/* Customer-specific */}
      <Route path="/services" element={<ProtectedRoute roles={['Customer']}><ServicesPage /></ProtectedRoute>} />
      <Route path="/book"     element={<ProtectedRoute roles={['Customer']}><BookPage /></ProtectedRoute>} />

      {/* Admin-only */}
      <Route path="/packages"             element={<ProtectedRoute roles={['Admin']}><PackagesPage /></ProtectedRoute>} />
      <Route path="/users"                element={<ProtectedRoute roles={['Admin']}><UsersPage /></ProtectedRoute>} />
      <Route path="/inventory"            element={<ProtectedRoute roles={['Admin']}><InventoryPage /></ProtectedRoute>} />
      <Route path="/reviews"              element={<ProtectedRoute roles={['Admin']}><ReviewsPage /></ProtectedRoute>} />
      <Route path="/schedule-management" element={<ProtectedRoute roles={['Admin']}><ScheduleManagementPage /></ProtectedRoute>} />

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <BrowserRouter>
          <AppRoutes />
          <Toaster
            position="top-right"
            toastOptions={{
              style: {
                background: 'var(--bg-card)',
                color: 'var(--text)',
                border: '1px solid var(--border)',
                borderRadius: '8px',
                fontSize: '14px',
              },
              success: { iconTheme: { primary: '#10b981', secondary: 'var(--bg-card)' } },
              error: { iconTheme: { primary: '#ef4444', secondary: 'var(--bg-card)' } },
            }}
          />
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  );
}
