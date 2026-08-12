import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import ComplaintCard from '../../components/common/ComplaintCard.jsx';
import Pagination from '../../components/common/Pagination.jsx';
import { complaintApi } from '../../services/complaintApi.js';

export default function MyComplaints() {
  const [result, setResult] = useState({ items: [], page: 1, totalPages: 0 });
  const [filters, setFilters] = useState({ page: 1, pageSize: 12, status: '', search: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    setLoading(true); setError('');
    const timer = setTimeout(() => complaintApi.mine(filters).then(setResult).catch((reason) => setError(reason.message)).finally(() => setLoading(false)), 250);
    return () => clearTimeout(timer);
  }, [filters]);

  return (
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Complaint tracking</p><h2>My complaints</h2><p>Search every issue you reported and open its status, timeline, evidence or verification actions.</p></div><Link to="/citizen/report" className="button primary">＋ Report complaint</Link></div>
      <div className="filter-bar"><input className="input" placeholder="Search title, reference number or address" value={filters.search} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value, page: 1 }))} /><select className="input" value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value, page: 1 }))}><option value="">All statuses</option>{['Created','Assigned','InProgress','Resolved','VerificationPending','Closed','Withdrawn'].map((item) => <option key={item}>{item}</option>)}</select></div>
      {error && <div className="alert error section-gap">{error}</div>}
      {loading ? <div className="surface section-gap"><div className="surface-body">Loading complaints…</div></div> : result.items.length ? <><div className="issue-card-grid section-gap">{result.items.map((item) => <ComplaintCard key={item.id} complaint={item} />)}</div><Pagination page={result.page} totalPages={result.totalPages} onPageChange={(page) => setFilters((current) => ({ ...current, page }))} /></> : <div className="surface section-gap"><div className="surface-body" style={{ padding: 42, textAlign: 'center' }}><h3>No matching complaints</h3><p className="muted" style={{ fontSize: 10 }}>Change your filters or submit a new civic issue.</p><Link to="/citizen/report" className="button primary">Report complaint</Link></div></div>}
    </section>
  );
}
