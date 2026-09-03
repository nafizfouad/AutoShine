import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import AppLayout from '../components/AppLayout';
import { packagesApi } from '../api/services';
import { Clock, Package, ArrowRight } from 'lucide-react';
import { PageLoader, EmptyState } from '../components/UI';

export default function ServicesPage() {
  const [packages, setPackages] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    packagesApi.getAll(true)
      .then(r => setPackages(r.data.data || []))
      .finally(() => setLoading(false));
  }, []);

  return (
    <AppLayout title="Our Services">
      {/* Page header */}
      <div className="page-header">
        <div className="page-header-info">
          <h1 className="page-title">Our Services</h1>
          <p className="page-subtitle">Browse our full service catalog. When you're ready, book an appointment in seconds.</p>
        </div>
        <button className="btn btn-primary" onClick={() => navigate('/book')}>
          <ArrowRight size={15} /> Book Now
        </button>
      </div>

      {loading ? (
        <PageLoader />
      ) : packages.length === 0 ? (
        <EmptyState icon="🔧" title="No services available" message="Check back soon — services are being configured." />
      ) : (
        <div className="package-grid">
          {packages.map(pkg => (
            <div key={pkg.id} className="package-card">
              <div style={{ marginBottom: 16 }}>
                <div className="package-name">{pkg.name}</div>
                <p className="package-desc">{pkg.description}</p>
              </div>

              <div style={{ display: 'flex', gap: 10, marginBottom: 16, flexWrap: 'wrap' }}>
                <span className="badge badge-primary" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                  <Clock size={11} /> {pkg.estimatedDurationMinutes} min
                </span>
                {pkg.packageItems?.length > 0 && (
                  <span className="badge badge-muted" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                    <Package size={11} /> {pkg.packageItems.length} materials
                  </span>
                )}
              </div>

              {pkg.packageItems?.length > 0 && (
                <div style={{ marginBottom: 16, padding: '12px 14px', background: 'var(--bg)', borderRadius: 'var(--radius-sm)' }}>
                  <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--text-dim)', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: 8 }}>
                    Includes
                  </div>
                  {pkg.packageItems.map((item, i) => (
                    <div key={i} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, color: 'var(--text-muted)', padding: '3px 0' }}>
                      <span>· {item.itemName || item.inventoryItemName || 'Material'}</span>
                      <span style={{ color: 'var(--text-dim)' }}>×{item.quantityRequired}</span>
                    </div>
                  ))}
                </div>
              )}

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 'auto' }}>
                <div className="package-price">
                  ${pkg.price}
                  <span style={{ fontSize: 13, fontWeight: 400, color: 'var(--text-muted)', marginLeft: 4 }}>USD</span>
                </div>
                <button
                  className="btn btn-primary btn-sm"
                  onClick={() => navigate(`/book?serviceId=${pkg.id}`)}
                  style={{ display: 'flex', alignItems: 'center', gap: 6 }}
                >
                  Book This <ArrowRight size={14} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </AppLayout>
  );
}
