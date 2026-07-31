import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { authApi } from '../api/services';
import { useAuth } from '../context/AuthContext';
import toast from 'react-hot-toast';
import { LogIn, Mail, Lock, AlertCircle } from 'lucide-react';

export default function LoginPage() {
  const [form, setForm] = useState({ email: '', password: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await authApi.login(form);
      const { token, ...userData } = res.data.data;
      login(userData, token);
      toast.success(`Welcome back, ${userData.fullName}!`);
      navigate('/dashboard');
    } catch (err) {
      const msg = err.response?.data?.message;
      setError(msg || 'Invalid email or password. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        {/* Logo */}
        <div className="auth-logo">
          <div className="logo-icon">🚗</div>
          <div className="logo-text">Auto<em>Shine</em></div>
        </div>

        <h1 className="auth-title">Sign in to your account</h1>
        <p className="auth-subtitle">Enter your credentials to access the dashboard</p>

        {error && (
          <div className="alert alert-danger">
            <AlertCircle size={15} />
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label className="form-label" htmlFor="email">
              Email Address
            </label>
            <div style={{ position: 'relative' }}>
              <Mail
                size={15}
                style={{
                  position: 'absolute', left: 12, top: '50%',
                  transform: 'translateY(-50%)', color: 'var(--text-dim)',
                  pointerEvents: 'none',
                }}
              />
              <input
                id="email"
                className="form-control"
                type="email"
                placeholder="you@autoshine.com"
                value={form.email}
                onChange={set('email')}
                style={{ paddingLeft: 36 }}
                required
                autoComplete="email"
                autoFocus
              />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="password">
              Password
            </label>
            <div style={{ position: 'relative' }}>
              <Lock
                size={15}
                style={{
                  position: 'absolute', left: 12, top: '50%',
                  transform: 'translateY(-50%)', color: 'var(--text-dim)',
                  pointerEvents: 'none',
                }}
              />
              <input
                id="password"
                className="form-control"
                type="password"
                placeholder="••••••••"
                value={form.password}
                onChange={set('password')}
                style={{ paddingLeft: 36 }}
                required
                autoComplete="current-password"
              />
            </div>
          </div>

          <button
            id="login-submit"
            className="btn btn-primary btn-lg"
            style={{ width: '100%', justifyContent: 'center', marginTop: 8 }}
            type="submit"
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner" style={{ width: 16, height: 16 }} /> Signing in…
              </>
            ) : (
              <>
                <LogIn size={16} /> Sign In
              </>
            )}
          </button>
        </form>

        <div className="auth-footer" style={{ marginTop: 24 }}>
          Don't have an account?{' '}
          <Link to="/register">Create one</Link>
        </div>
      </div>
    </div>
  );
}
