import { useEffect, useState } from 'react';
import AppLayout from '../components/AppLayout';
import { PageLoader, EmptyState, Pagination, ConfirmModal } from '../components/UI';
import { usersApi } from '../api/services';
import { StatusBadge } from '../components/UI';
import { Plus, Search, Pencil, Trash2 } from 'lucide-react';
import toast from 'react-hot-toast';

const ROLES = ['Admin', 'Employee', 'Customer'];

const EMPTY_FORM = { firstName: '', lastName: '', email: '', password: '', phone: '', role: 'Employee' };

export default function UsersPage() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [modal, setModal] = useState(null); // null | 'create' | 'edit'
  const [editUser, setEditUser] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);

  const load = async (p = page) => {
    setLoading(true);
    try {
      const res = await usersApi.getAll({ page: p, pageSize: 10, ...(roleFilter ? { role: roleFilter } : {}), ...(search ? { search } : {}) });
      setUsers(res.data.data.items || []);
      setTotalPages(res.data.data.totalPages || 1);
    } catch { toast.error('Failed to load users.'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(page); }, [page, roleFilter, search]);

  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }));

  const openCreate = () => { setForm(EMPTY_FORM); setEditUser(null); setModal('create'); };
  const openEdit = (u) => {
    setForm({ firstName: u.firstName, lastName: u.lastName, email: u.email, password: '', phone: u.phone, role: u.role });
    setEditUser(u);
    setModal('edit');
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setSaving(true);
    try {
      if (modal === 'create') {
        await usersApi.create(form);
        toast.success('User created.');
      } else {
        await usersApi.update(editUser.id, { firstName: form.firstName, lastName: form.lastName, phone: form.phone, isActive: true });
        toast.success('User updated.');
      }
      setModal(null);
      load(page);
    } catch (err) {
      toast.error(err.response?.data?.message || 'Save failed.');
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await usersApi.delete(deleteTarget);
      toast.success('User deactivated.');
      setDeleteTarget(null);
      load(page);
    } catch { toast.error('Delete failed.'); }
    finally { setDeleting(false); }
  };

  return (
    <AppLayout title="User Management">
      <div className="card">
        <div className="card-header">
          <div className="card-title">All Users</div>
          <div style={{ display: 'flex', gap: 8 }}>
            <div style={{ position: 'relative' }}>
              <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--text-dim)' }} />
              <input className="form-control" style={{ paddingLeft: 30, width: 200 }} placeholder="Search…"
                value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
            </div>
            <select className="form-control" style={{ width: 140 }} value={roleFilter}
              onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}>
              <option value="">All Roles</option>
              {ROLES.map((r) => <option key={r}>{r}</option>)}
            </select>
            <button className="btn btn-primary" onClick={openCreate}><Plus size={16} /> Add User</button>
          </div>
        </div>

        {loading ? <PageLoader /> : users.length === 0
          ? <EmptyState icon="👥" title="No users found" />
          : (
            <div className="table-wrapper">
              <table>
                <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Role</th><th>Status</th><th>Actions</th></tr></thead>
                <tbody>
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td style={{ fontWeight: 600 }}>{u.firstName} {u.lastName}</td>
                      <td style={{ color: 'var(--text-muted)' }}>{u.email}</td>
                      <td>{u.phone}</td>
                      <td><StatusBadge status={u.role} /></td>
                      <td><span className={`badge ${u.isActive ? 'badge-success' : 'badge-danger'}`}>{u.isActive ? 'Active' : 'Inactive'}</span></td>
                      <td>
                        <div style={{ display: 'flex', gap: 6 }}>
                          <button className="btn btn-sm btn-secondary" onClick={() => openEdit(u)}><Pencil size={13} /></button>
                          <button className="btn btn-sm btn-danger" onClick={() => setDeleteTarget(u.id)}><Trash2 size={13} /></button>
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

      {modal && (
        <div className="modal-overlay" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">{modal === 'create' ? 'Add User' : 'Edit User'}</h2>
              <button className="btn btn-secondary btn-sm" onClick={() => setModal(null)}>✕</button>
            </div>
            <form onSubmit={handleSave}>
              <div className="form-row">
                <div className="form-group"><label className="form-label">First Name</label><input className="form-control" value={form.firstName} onChange={set('firstName')} required /></div>
                <div className="form-group"><label className="form-label">Last Name</label><input className="form-control" value={form.lastName} onChange={set('lastName')} required /></div>
              </div>
              {modal === 'create' && <>
                <div className="form-group"><label className="form-label">Email</label><input className="form-control" type="email" value={form.email} onChange={set('email')} required /></div>
                <div className="form-group"><label className="form-label">Password</label><input className="form-control" type="password" value={form.password} onChange={set('password')} required /></div>
                <div className="form-group"><label className="form-label">Role</label>
                  <select className="form-control" value={form.role} onChange={set('role')}>
                    {ROLES.map((r) => <option key={r}>{r}</option>)}
                  </select>
                </div>
              </>}
              <div className="form-group"><label className="form-label">Phone</label><input className="form-control" value={form.phone} onChange={set('phone')} required /></div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setModal(null)}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteTarget && (
        <ConfirmModal title="Deactivate User" message="This will deactivate the user's account. Continue?"
          onConfirm={handleDelete} onCancel={() => setDeleteTarget(null)} loading={deleting} />
      )}
    </AppLayout>
  );
}
