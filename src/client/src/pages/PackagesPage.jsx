import { useEffect, useState } from 'react';
import AppLayout from '../components/AppLayout';
import { PageLoader, EmptyState, ConfirmModal } from '../components/UI';
import { packagesApi, inventoryApi } from '../api/services';
import { Plus, Pencil, Trash2, Clock, DollarSign } from 'lucide-react';
import toast from 'react-hot-toast';

const EMPTY_FORM = { name: '', description: '', price: '', estimatedDurationMinutes: '', isActive: true, items: [] };

export default function PackagesPage() {
  const [packages, setPackages] = useState([]);
  const [inventory, setInventory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [editId, setEditId] = useState(null);
  const [saving, setSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const [pkgRes, invRes] = await Promise.all([packagesApi.getAll(false), inventoryApi.getAll({ page: 1, pageSize: 100 })]);
      setPackages(pkgRes.data.data || []);
      setInventory(invRes.data.data.items || []);
    } catch { toast.error('Failed to load packages.'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }));

  const addItem = () => setForm((f) => ({ ...f, items: [...f.items, { inventoryItemId: inventory[0]?.id || 0, quantityRequired: 1 }] }));
  const removeItem = (i) => setForm((f) => ({ ...f, items: f.items.filter((_, idx) => idx !== i) }));
  const setItem = (i, k, v) => setForm((f) => ({ ...f, items: f.items.map((it, idx) => idx === i ? { ...it, [k]: k === 'quantityRequired' ? Number(v) : Number(v) } : it) }));

  const openCreate = () => { setForm(EMPTY_FORM); setEditId(null); setModal('form'); };
  const openEdit = (pkg) => {
    setForm({
      name: pkg.name, description: pkg.description, price: pkg.price,
      estimatedDurationMinutes: pkg.estimatedDurationMinutes, isActive: pkg.isActive,
      items: pkg.items.map((i) => ({ inventoryItemId: i.inventoryItemId, quantityRequired: i.quantityRequired })),
    });
    setEditId(pkg.id);
    setModal('form');
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setSaving(true);
    const payload = { ...form, price: Number(form.price), estimatedDurationMinutes: Number(form.estimatedDurationMinutes) };
    try {
      if (editId) await packagesApi.update(editId, payload);
      else await packagesApi.create(payload);
      toast.success(editId ? 'Package updated.' : 'Package created.');
      setModal(null);
      load();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Save failed.');
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await packagesApi.delete(deleteTarget);
      toast.success('Package deactivated.');
      setDeleteTarget(null);
      load();
    } catch { toast.error('Delete failed.'); }
    finally { setDeleting(false); }
  };

  return (
    <AppLayout title="Service Packages">
      <div className="card-header" style={{ marginBottom: 20 }}>
        <div className="card-title">Manage Packages</div>
        <button className="btn btn-primary" onClick={openCreate}><Plus size={16} /> New Package</button>
      </div>

      {loading ? <PageLoader /> : packages.length === 0
        ? <EmptyState icon="📦" title="No packages yet" action={<button className="btn btn-primary" onClick={openCreate}>Create Package</button>} />
        : (
          <div className="package-grid">
            {packages.map((pkg) => (
              <div className="package-card" key={pkg.id}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div className="package-name">{pkg.name}</div>
                  {!pkg.isActive && <span className="badge badge-danger">Inactive</span>}
                </div>
                <div className="package-desc">{pkg.description}</div>
                <div className="package-price">${pkg.price.toFixed(2)}</div>
                <div className="package-meta">
                  <span><Clock size={13} /> {pkg.estimatedDurationMinutes} min</span>
                  <span>🧪 {pkg.items?.length ?? 0} ingredients</span>
                </div>
                {pkg.items?.length > 0 && (
                  <div style={{ marginTop: 12, padding: '10px', background: 'var(--bg)', borderRadius: 'var(--radius-sm)' }}>
                    {pkg.items.map((item, i) => (
                      <div key={i} style={{ fontSize: 12, color: 'var(--text-muted)', padding: '2px 0' }}>
                        • {item.itemName} × {item.quantityRequired}
                      </div>
                    ))}
                  </div>
                )}
                <div style={{ display: 'flex', gap: 8, marginTop: 16 }}>
                  <button className="btn btn-sm btn-secondary" onClick={() => openEdit(pkg)}><Pencil size={13} /> Edit</button>
                  <button className="btn btn-sm btn-danger" onClick={() => setDeleteTarget(pkg.id)}><Trash2 size={13} /></button>
                </div>
              </div>
            ))}
          </div>
        )}

      {modal === 'form' && (
        <div className="modal-overlay" onClick={() => setModal(null)}>
          <div className="modal modal-wide" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">{editId ? 'Edit Package' : 'New Package'}</h2>
              <button className="btn btn-secondary btn-sm" onClick={() => setModal(null)}>✕</button>
            </div>
            <form onSubmit={handleSave}>
              <div className="form-group"><label className="form-label">Package Name</label><input className="form-control" value={form.name} onChange={set('name')} required /></div>
              <div className="form-group"><label className="form-label">Description</label><textarea className="form-control" value={form.description} onChange={set('description')} /></div>
              <div className="form-row">
                <div className="form-group"><label className="form-label">Price ($)</label><input className="form-control" type="number" step="0.01" value={form.price} onChange={set('price')} required /></div>
                <div className="form-group"><label className="form-label">Duration (min)</label><input className="form-control" type="number" value={form.estimatedDurationMinutes} onChange={set('estimatedDurationMinutes')} required /></div>
              </div>

              <div style={{ marginBottom: 16 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                  <label className="form-label" style={{ marginBottom: 0 }}>Recipe Items</label>
                  <button type="button" className="btn btn-sm btn-secondary" onClick={addItem}><Plus size={13} /> Add Item</button>
                </div>
                {form.items.map((item, i) => (
                  <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 8, alignItems: 'center' }}>
                    <select className="form-control" value={item.inventoryItemId}
                      onChange={(e) => setItem(i, 'inventoryItemId', e.target.value)}>
                      {inventory.map((inv) => <option key={inv.id} value={inv.id}>{inv.itemName}</option>)}
                    </select>
                    <input className="form-control" style={{ width: 80 }} type="number" min="1" value={item.quantityRequired}
                      onChange={(e) => setItem(i, 'quantityRequired', e.target.value)} />
                    <button type="button" className="btn btn-sm btn-danger" onClick={() => removeItem(i)}><Trash2 size={13} /></button>
                  </div>
                ))}
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setModal(null)}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving…' : 'Save Package'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteTarget && (
        <ConfirmModal title="Deactivate Package" message="This will hide the package from new bookings."
          onConfirm={handleDelete} onCancel={() => setDeleteTarget(null)} loading={deleting} />
      )}
    </AppLayout>
  );
}
