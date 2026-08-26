import { useState, useEffect } from 'react';
import { scheduleApi, usersApi } from '../api/services';
import toast from 'react-hot-toast';
import { Plus, Trash2, Calendar, AlertCircle } from 'lucide-react';

const DAY_NAMES = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const DAY_BITS  = [1, 2, 4, 8, 16, 32, 64];

const decodeDays = (mask) => DAY_NAMES.filter((_, i) => mask & DAY_BITS[i]).join(', ');

const initTemplate = {
  employeeId: '',
  startDate: '',
  endDate: '',
  workingDays: 62, // Mon-Fri default
  workStartTime: '09:00',
  workEndTime: '17:00',
  breakStartTime: '13:00',
  breakEndTime: '14:00',
};

const initLeave = { employeeId: '', date: '', reason: '' };

export default function ScheduleManagementPage() {
  const [employees, setEmployees] = useState([]);
  const [templates, setTemplates] = useState([]);
  const [leaves, setLeaves] = useState([]);
  const [tab, setTab] = useState('templates');

  const [showTemplateForm, setShowTemplateForm] = useState(false);
  const [showLeaveForm, setShowLeaveForm] = useState(false);
  const [tForm, setTForm] = useState(initTemplate);
  const [lForm, setLForm] = useState(initLeave);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    usersApi.getAll({ role: 'Employee' }).then(r => {
      const all = r.data.data?.items || r.data.data || [];
      setEmployees(all.filter(u => u.role === 'Employee'));
    });
    loadTemplates();
    loadLeaves();
  }, []);

  const loadTemplates = () =>
    scheduleApi.getAllTemplates().then(r => setTemplates(r.data.data || []));
  const loadLeaves = () =>
    scheduleApi.getAllLeaves().then(r => setLeaves(r.data.data || []));

  const toggleDay = (bit) =>
    setTForm(f => ({ ...f, workingDays: f.workingDays ^ bit }));

  const handleCreateTemplate = async (e) => {
    e.preventDefault();
    if (!tForm.employeeId) return toast.error('Select an employee.');
    if (!tForm.workingDays) return toast.error('Select at least one working day.');
    setSaving(true);
    try {
      await scheduleApi.createTemplate({
        ...tForm,
        employeeId: parseInt(tForm.employeeId),
        workStartTime: tForm.workStartTime + ':00',
        workEndTime: tForm.workEndTime + ':00',
        breakStartTime: tForm.breakStartTime ? tForm.breakStartTime + ':00' : null,
        breakEndTime: tForm.breakEndTime ? tForm.breakEndTime + ':00' : null,
      });
      toast.success('Schedule template created!');
      setShowTemplateForm(false);
      setTForm(initTemplate);
      loadTemplates();
    } catch { toast.error('Failed to create template.'); }
    finally { setSaving(false); }
  };

  const handleDeleteTemplate = async (id) => {
    if (!confirm('Delete this schedule template?')) return;
    await scheduleApi.deleteTemplate(id);
    toast.success('Template deleted.');
    loadTemplates();
  };

  const handleCreateLeave = async (e) => {
    e.preventDefault();
    if (!lForm.employeeId || !lForm.date) return toast.error('Employee and date are required.');
    setSaving(true);
    try {
      await scheduleApi.createLeave({
        ...lForm,
        employeeId: parseInt(lForm.employeeId),
      });
      toast.success('Leave day recorded!');
      setShowLeaveForm(false);
      setLForm(initLeave);
      loadLeaves();
    } catch { toast.error('Failed to record leave.'); }
    finally { setSaving(false); }
  };

  const handleDeleteLeave = async (id) => {
    await scheduleApi.deleteLeave(id);
    toast.success('Leave removed.');
    loadLeaves();
  };

  const fmtDate = (d) => new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });

  return (
    <div className="page fade-in">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 28 }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 800, marginBottom: 4 }}>Schedule Management</h1>
          <p style={{ color: 'var(--text-muted)', fontSize: 14 }}>Manage employee availability templates and leave days</p>
        </div>
        {tab === 'templates' ? (
          <button className="btn btn-primary" onClick={() => setShowTemplateForm(true)}>
            <Plus size={15} /> Add Template
          </button>
        ) : (
          <button className="btn btn-primary" onClick={() => setShowLeaveForm(true)}>
            <Plus size={15} /> Add Leave
          </button>
        )}
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 4, marginBottom: 24, borderBottom: '1px solid var(--border)', paddingBottom: 0 }}>
        {['templates', 'leaves'].map(t => (
          <button key={t} onClick={() => setTab(t)}
            style={{ padding: '10px 20px', background: 'none', border: 'none', fontWeight: 600, fontSize: 14,
              color: tab === t ? 'var(--primary-light)' : 'var(--text-muted)',
              borderBottom: tab === t ? '2px solid var(--primary)' : '2px solid transparent',
              cursor: 'pointer', transition: 'var(--transition)' }}>
            {t === 'templates' ? '📋 Schedule Templates' : '🏖️ Leave Days'}
          </button>
        ))}
      </div>

      {/* Templates tab */}
      {tab === 'templates' && (
        <>
          {showTemplateForm && (
            <div className="card" style={{ marginBottom: 24 }}>
              <div className="card-header">
                <div className="card-title">New Schedule Template</div>
                <button className="btn btn-secondary btn-sm" onClick={() => setShowTemplateForm(false)}>Cancel</button>
              </div>
              <form onSubmit={handleCreateTemplate}>
                <div className="form-group">
                  <label className="form-label">Employee</label>
                  <select className="form-control" value={tForm.employeeId}
                    onChange={e => setTForm(f => ({ ...f, employeeId: e.target.value }))} required>
                    <option value="">Select employee…</option>
                    {employees.map(e => <option key={e.id} value={e.id}>{e.firstName} {e.lastName}</option>)}
                  </select>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Start Date</label>
                    <input type="date" className="form-control" value={tForm.startDate}
                      onChange={e => setTForm(f => ({ ...f, startDate: e.target.value }))} required />
                  </div>
                  <div className="form-group">
                    <label className="form-label">End Date</label>
                    <input type="date" className="form-control" value={tForm.endDate}
                      onChange={e => setTForm(f => ({ ...f, endDate: e.target.value }))} required />
                  </div>
                </div>
                <div className="form-group">
                  <label className="form-label">Working Days</label>
                  <div className="days-picker">
                    {DAY_NAMES.map((name, i) => (
                      <div key={name} className={`day-chip ${tForm.workingDays & DAY_BITS[i] ? 'selected' : ''}`}
                        onClick={() => toggleDay(DAY_BITS[i])}>
                        {name}
                      </div>
                    ))}
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Work Start</label>
                    <input type="time" className="form-control" value={tForm.workStartTime}
                      onChange={e => setTForm(f => ({ ...f, workStartTime: e.target.value }))} required />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Work End</label>
                    <input type="time" className="form-control" value={tForm.workEndTime}
                      onChange={e => setTForm(f => ({ ...f, workEndTime: e.target.value }))} required />
                  </div>
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Break Start (optional)</label>
                    <input type="time" className="form-control" value={tForm.breakStartTime}
                      onChange={e => setTForm(f => ({ ...f, breakStartTime: e.target.value }))} />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Break End (optional)</label>
                    <input type="time" className="form-control" value={tForm.breakEndTime}
                      onChange={e => setTForm(f => ({ ...f, breakEndTime: e.target.value }))} />
                  </div>
                </div>
                <button className="btn btn-primary" type="submit" disabled={saving}>
                  {saving ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Saving…</> : <><Plus size={15} /> Create Template</>}
                </button>
              </form>
            </div>
          )}

          <div className="card">
            <div className="table-wrapper">
              {templates.length === 0 ? (
                <div className="empty-state">
                  <div className="empty-icon"><Calendar size={40} /></div>
                  <h3>No templates yet</h3>
                  <p>Create a template to define an employee's recurring availability</p>
                </div>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Employee</th><th>Date Range</th><th>Working Days</th><th>Hours</th><th>Break</th><th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {templates.map(t => (
                      <tr key={t.id}>
                        <td style={{ fontWeight: 600 }}>{t.employeeName}</td>
                        <td style={{ fontSize: 13, color: 'var(--text-muted)' }}>{fmtDate(t.startDate)} – {fmtDate(t.endDate)}</td>
                        <td><span className="badge badge-primary">{decodeDays(t.workingDays)}</span></td>
                        <td style={{ fontSize: 13 }}>{t.workStartTime?.substring(0,5)} – {t.workEndTime?.substring(0,5)}</td>
                        <td style={{ fontSize: 13, color: 'var(--text-muted)' }}>
                          {t.breakStartTime ? `${t.breakStartTime?.substring(0,5)} – ${t.breakEndTime?.substring(0,5)}` : '—'}
                        </td>
                        <td>
                          <button className="btn btn-danger btn-sm btn-icon" onClick={() => handleDeleteTemplate(t.id)}>
                            <Trash2 size={14} />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </>
      )}

      {/* Leaves tab */}
      {tab === 'leaves' && (
        <>
          {showLeaveForm && (
            <div className="card" style={{ marginBottom: 24 }}>
              <div className="card-header">
                <div className="card-title">Record Leave Day</div>
                <button className="btn btn-secondary btn-sm" onClick={() => setShowLeaveForm(false)}>Cancel</button>
              </div>
              <form onSubmit={handleCreateLeave}>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Employee</label>
                    <select className="form-control" value={lForm.employeeId}
                      onChange={e => setLForm(f => ({ ...f, employeeId: e.target.value }))} required>
                      <option value="">Select employee…</option>
                      {employees.map(e => <option key={e.id} value={e.id}>{e.firstName} {e.lastName}</option>)}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Date</label>
                    <input type="date" className="form-control" value={lForm.date}
                      onChange={e => setLForm(f => ({ ...f, date: e.target.value }))} required />
                  </div>
                </div>
                <div className="form-group">
                  <label className="form-label">Reason (optional)</label>
                  <input className="form-control" value={lForm.reason} placeholder="e.g. Personal leave"
                    onChange={e => setLForm(f => ({ ...f, reason: e.target.value }))} />
                </div>
                <button className="btn btn-primary" type="submit" disabled={saving}>
                  {saving ? <><span className="spinner" style={{ width: 14, height: 14 }} /> Saving…</> : <><Plus size={15} /> Record Leave</>}
                </button>
              </form>
            </div>
          )}

          <div className="card">
            <div className="table-wrapper">
              {leaves.length === 0 ? (
                <div className="empty-state">
                  <div className="empty-icon">🏖️</div>
                  <h3>No leave days recorded</h3>
                  <p>Use this to block specific dates for an employee outside their normal schedule</p>
                </div>
              ) : (
                <table>
                  <thead>
                    <tr><th>Employee</th><th>Date</th><th>Reason</th><th>Actions</th></tr>
                  </thead>
                  <tbody>
                    {leaves.map(l => (
                      <tr key={l.id}>
                        <td style={{ fontWeight: 600 }}>{l.employeeName}</td>
                        <td>{fmtDate(l.date)}</td>
                        <td style={{ color: 'var(--text-muted)', fontSize: 13 }}>{l.reason || '—'}</td>
                        <td>
                          <button className="btn btn-danger btn-sm btn-icon" onClick={() => handleDeleteLeave(l.id)}>
                            <Trash2 size={14} />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
