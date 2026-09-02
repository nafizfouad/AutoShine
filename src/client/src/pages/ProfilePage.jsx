import { useState, useEffect } from 'react';
import { profileApi } from '../api/services';
import { useAuth } from '../context/AuthContext';
import AppLayout from '../components/AppLayout';
import toast from 'react-hot-toast';
import { User, Lock, Save, Eye, EyeOff } from 'lucide-react';
import { PageLoader } from '../components/UI';

export default function ProfilePage() {
  const { user, login } = useAuth();
  const [profile, setProfile] = useState(null);
  const [form, setForm] = useState({ firstName: '', lastName: '', phone: '' });
  const [pwForm, setPwForm] = useState({ currentPassword: '', newPassword: '', confirm: '' });
  const [showPw, setShowPw] = useState(false);
  const [saving, setSaving] = useState(false);
  const [changingPw, setChangingPw] = useState(false);

  useEffect(() => {
    profileApi.get().then(r => {
      const p = r.data.data;
      setProfile(p);
      setForm({ firstName: p.firstName, lastName: p.lastName, phone: p.phone || '' });
    });
  }, []);

  const set = (k) => (e) => setForm(f => ({ ...f, [k]: e.target.value }));
  const setPw = (k) => (e) => setPwForm(f => ({ ...f, [k]: e.target.value }));

  const handleSaveProfile = async (e) => {
    e.preventDefault();
    if (!form.firstName.trim() || !form.lastName.trim()) return toast.error('Name is required.');
    setSaving(true);
    try {
      const r = await profileApi.update(form);
      const updated = r.data.data;
      setProfile(updated);
      const token = localStorage.getItem('token');
      login({ ...user, firstName: updated.firstName, lastName: updated.lastName }, token);
      toast.success('Profile updated!');
    } catch { toast.error('Failed to update profile.'); }
    finally { setSaving(false); }
  };

  const handleChangePassword = async (e) => {
    e.preventDefault();
    if (pwForm.newPassword !== pwForm.confirm) return toast.error('Passwords do not match.');
    if (pwForm.newPassword.length < 8) return toast.error('New password must be at least 8 characters.');
    setChangingPw(true);
    try {
      await profileApi.changePassword({ currentPassword: pwForm.currentPassword, newPassword: pwForm.newPassword });
      setPwForm({ currentPassword: '', newPassword: '', confirm: '' });
      toast.success('Password changed successfully!');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Incorrect current password.');
    } finally { setChangingPw(false); }
  };

  if (!profile) return (
    <AppLayout title="My Profile">
      <PageLoader />
    </AppLayout>
  );

  const initials = `${profile.firstName[0]}${profile.lastName[0]}`.toUpperCase();

  return (
    <AppLayout title="My Profile">
      <div className="profile-page">
        {/* Avatar + name header */}
        <div className="profile-header">
          <div className="profile-avatar-ring">{initials}</div>
          <h1 style={{ fontSize: 24, fontWeight: 800 }}>{profile.firstName} {profile.lastName}</h1>
          <p style={{ color: 'var(--text-muted)', fontSize: 14, marginTop: 4 }}>
            {profile.email} · <span className="badge badge-primary">{profile.role}</span>
          </p>
        </div>

        {/* Personal information */}
        <div className="card">
          <div className="card-header">
            <div>
              <div className="card-title">Personal Information</div>
              <div className="card-subtitle">Update your name and contact details</div>
            </div>
            <User size={20} style={{ color: 'var(--text-dim)' }} />
          </div>
          <form onSubmit={handleSaveProfile}>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">First Name</label>
                <input className="form-control" value={form.firstName} onChange={set('firstName')} required />
              </div>
              <div className="form-group">
                <label className="form-label">Last Name</label>
                <input className="form-control" value={form.lastName} onChange={set('lastName')} required />
              </div>
            </div>
            <div className="form-group">
              <label className="form-label">Phone</label>
              <input className="form-control" value={form.phone} onChange={set('phone')} placeholder="e.g. 555-0100" />
            </div>
            <div className="form-group" style={{ marginBottom: 0 }}>
              <label className="form-label">Email Address</label>
              <input className="form-control" value={profile.email} disabled style={{ opacity: 0.6 }} />
              <p className="form-error" style={{ color: 'var(--text-dim)' }}>Email cannot be changed here.</p>
            </div>
            <div style={{ marginTop: 20 }}>
              <button className="btn btn-primary" type="submit" disabled={saving}>
                {saving ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Saving…</> : <><Save size={15} /> Save Changes</>}
              </button>
            </div>
          </form>
        </div>

        {/* Change password */}
        <div className="card">
          <div className="card-header">
            <div>
              <div className="card-title">Change Password</div>
              <div className="card-subtitle">Choose a strong password (min. 8 characters)</div>
            </div>
            <Lock size={20} style={{ color: 'var(--text-dim)' }} />
          </div>
          <form onSubmit={handleChangePassword}>
            <div className="form-group">
              <label className="form-label">Current Password</label>
              <div style={{ position: 'relative' }}>
                <input
                  className="form-control"
                  type={showPw ? 'text' : 'password'}
                  value={pwForm.currentPassword}
                  onChange={setPw('currentPassword')}
                  required
                  style={{ paddingRight: 40 }}
                />
                <button type="button" onClick={() => setShowPw(s => !s)}
                  style={{ position: 'absolute', right: 10, top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', color: 'var(--text-dim)', cursor: 'pointer' }}>
                  {showPw ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">New Password</label>
                <input className="form-control" type="password" value={pwForm.newPassword} onChange={setPw('newPassword')} required minLength={8} />
              </div>
              <div className="form-group">
                <label className="form-label">Confirm New Password</label>
                <input className="form-control" type="password" value={pwForm.confirm} onChange={setPw('confirm')} required />
              </div>
            </div>
            {pwForm.newPassword && pwForm.confirm && pwForm.newPassword !== pwForm.confirm && (
              <p className="form-error" style={{ marginBottom: 12 }}>Passwords do not match.</p>
            )}
            <button className="btn btn-primary" type="submit" disabled={changingPw}>
              {changingPw ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Changing…</> : <><Lock size={15} /> Change Password</>}
            </button>
          </form>
        </div>
      </div>
    </AppLayout>
  );
}
