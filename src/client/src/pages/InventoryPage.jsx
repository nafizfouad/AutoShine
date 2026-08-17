import { useEffect, useState, useRef } from 'react';
import AppLayout from '../components/AppLayout';
import { PageLoader, EmptyState, Pagination, ConfirmModal } from '../components/UI';
import { inventoryApi } from '../api/services';
import { Plus, AlertTriangle, Pencil, Trash2 } from 'lucide-react';
import toast from 'react-hot-toast';

const EMPTY_FORM = { itemName: '', sku: '', currentStock: '', minimumThreshold: '', unit: 'pieces' };


export default function InventoryPage() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [lowOnly, setLowOnly] = useState(false);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [editId, setEditId] = useState(null);
  const [saving, setSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [allItems, setAllItems] = useState([]);          // for autocomplete
  const [suggestions, setSuggestions] = useState([]);    // filtered suggestions
  const autocompleteRef = useRef(null);

  // Close dropdown on outside click
  useEffect(() => {
    const handler = (e) => { if (!autocompleteRef.current?.contains(e.target)) setSuggestions([]); };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const load = async (p = page) => {
    setLoading(true);
    try {
      const res = await inventoryApi.getAll({ page: p, pageSize: 15, ...(lowOnly ? { lowStockOnly: true } : {}) });
      setItems(res.data.data.items || []);
      setTotalPages(res.data.data.totalPages || 1);
    } catch { toast.error('Failed to load inventory.'); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(page); }, [page, lowOnly]);

  // Load all items for autocomplete (no pagination)
  useEffect(() => {
    inventoryApi.getAll({ page: 1, pageSize: 1000 })
      .then(r => setAllItems(r.data.data?.items || []));
  }, []);

  const set = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }));

  const handleItemNameChange = (e) => {
    const val = e.target.value;
    setForm(f => ({ ...f, itemName: val }));
    if (val.trim().length < 1) { setSuggestions([]); return; }
    const q = val.toLowerCase();
    setSuggestions(allItems.filter(i => i.itemName.toLowerCase().includes(q)));
  };

  const selectSuggestion = (item) => {
    setForm(f => ({ ...f, itemName: item.itemName, sku: item.sku, unit: item.unit }));
    setSuggestions([]);
  };

  const openCreate = () => { setForm(EMPTY_FORM); setEditId(null); setModal('form'); };
  const openEdit = (item) => {
    setForm({ itemName: item.itemName, sku: item.sku, currentStock: item.currentStock, minimumThreshold: item.minimumThreshold, unit: item.unit });
    setEditId(item.id);
    setModal('form');
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setSaving(true);
    const payload = { ...form, currentStock: Number(form.currentStock), minimumThreshold: Number(form.minimumThreshold) };
    try {
      if (editId) await inventoryApi.update(editId, payload);
      else await inventoryApi.create(payload);
      toast.success(editId ? 'Item updated.' : 'Item created.');
      setModal(null);
      load(page);
    } catch (err) {
      toast.error(err.response?.data?.message || 'Save failed.');
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await inventoryApi.delete(deleteTarget);
      toast.success('Item deleted.');
      setDeleteTarget(null);
      load(page);
    } catch { toast.error('Delete failed.'); }
    finally { setDeleting(false); }
  };

  return (
    <AppLayout title="Inventory Management">
      <div className="card">
        <div className="card-header">
          <div>
            <div className="card-title">Stock Items</div>
            <div className="card-subtitle">Track parts and supplies</div>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14, color: 'var(--text-muted)', cursor: 'pointer' }}>
              <input type="checkbox" checked={lowOnly} onChange={(e) => { setLowOnly(e.target.checked); setPage(1); }} />
              Low stock only
            </label>
            <button className="btn btn-primary" onClick={openCreate}><Plus size={16} /> Add Item</button>
          </div>
        </div>

        {loading ? <PageLoader /> : items.length === 0
          ? <EmptyState icon="📦" title="No inventory items" />
          : (
            <div className="table-wrapper">
              <table>
                <thead><tr><th>Item Name</th><th>SKU</th><th>Stock</th><th>Min Threshold</th><th>Unit</th><th>Status</th><th>Actions</th></tr></thead>
                <tbody>
                  {items.map((item) => (
                    <tr key={item.id}>
                      <td style={{ fontWeight: 600 }}>{item.itemName}</td>
                      <td style={{ color: 'var(--text-dim)', fontFamily: 'monospace' }}>{item.sku}</td>
                      <td style={{ fontWeight: 700, color: item.isLowStock ? 'var(--danger)' : 'var(--success)' }}>
                        {item.currentStock}
                      </td>
                      <td style={{ color: 'var(--text-muted)' }}>{item.minimumThreshold}</td>
                      <td style={{ color: 'var(--text-muted)' }}>{item.unit}</td>
                      <td>
                        {item.isLowStock
                          ? <span className="badge badge-danger"><AlertTriangle size={10} /> Low Stock</span>
                          : <span className="badge badge-success">In Stock</span>}
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: 6 }}>
                          <button className="btn btn-sm btn-secondary" onClick={() => openEdit(item)}><Pencil size={13} /></button>
                          <button className="btn btn-sm btn-danger" onClick={() => setDeleteTarget(item.id)}><Trash2 size={13} /></button>
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

      {modal === 'form' && (
        <div className="modal-overlay" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2 className="modal-title">{editId ? 'Edit Item' : 'New Inventory Item'}</h2>
              <button className="btn btn-secondary btn-sm" onClick={() => setModal(null)}>✕</button>
            </div>
            <form onSubmit={handleSave}>
              <div className="form-group">
                <label className="form-label">Item Name</label>
                <div className="autocomplete-wrapper" ref={autocompleteRef}>
                  <input
                    className="form-control"
                    value={form.itemName}
                    onChange={handleItemNameChange}
                    onFocus={handleItemNameChange.bind(null, { target: { value: form.itemName } })}
                    placeholder="Type to search or enter new name…"
                    required
                    autoComplete="off"
                  />
                  {suggestions.length > 0 && (
                    <div className="autocomplete-dropdown">
                      {suggestions.map((item) => (
                        <div key={item.id} className="autocomplete-item"
                          onMouseDown={() => selectSuggestion(item)}>
                          <strong>{item.itemName}</strong>
                          <span style={{ color: 'var(--text-dim)', fontSize: 12, marginLeft: 8 }}>{item.sku}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
              {!editId && <div className="form-group"><label className="form-label">SKU</label><input className="form-control" placeholder="e.g. OIL-5W30" value={form.sku} onChange={set('sku')} required /></div>}
              <div className="form-row">
                <div className="form-group"><label className="form-label">Current Stock</label><input className="form-control" type="number" min="0" value={form.currentStock} onChange={set('currentStock')} required /></div>
                <div className="form-group"><label className="form-label">Min Threshold</label><input className="form-control" type="number" min="0" value={form.minimumThreshold} onChange={set('minimumThreshold')} required /></div>
              </div>
              <div className="form-group"><label className="form-label">Unit</label><input className="form-control" placeholder="pieces, liters, sets…" value={form.unit} onChange={set('unit')} /></div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setModal(null)}>Cancel</button>
                <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteTarget && (
        <ConfirmModal title="Delete Item" message="This item will be permanently removed from inventory."
          onConfirm={handleDelete} onCancel={() => setDeleteTarget(null)} loading={deleting} />
      )}
    </AppLayout>
  );
}
