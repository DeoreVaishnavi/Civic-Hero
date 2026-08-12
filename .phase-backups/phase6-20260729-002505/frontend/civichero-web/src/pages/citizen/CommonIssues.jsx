import { useState } from 'react';
import ComplaintCard from '../../components/common/ComplaintCard.jsx';
import EmptyState from '../../components/common/EmptyState.jsx';
import { useGeoLocation } from '../../hooks/useGeoLocation.js';
import { complaintApi } from '../../services/complaintApi.js';

export default function CommonIssues() {
  const geo = useGeoLocation();
  const [radiusKm, setRadiusKm] = useState(5);
  const [items, setItems] = useState([]);
  const [error, setError] = useState('');
  const search = async () => {
    try { const coordinates = await geo.locate(); setItems(await complaintApi.nearby({ ...coordinates, radiusKm })); setError(''); }
    catch (reason) { setError(reason.message); }
  };
  return <section className="p-6 lg:p-10"><h2 className="text-3xl font-black text-white">Nearby civic issues</h2><p className="mt-2 text-slate-400">Discover active issues around your current position and support them with an upvote.</p><div className="mt-6 flex flex-wrap gap-3 rounded-2xl border border-white/10 bg-white/[0.04] p-4"><select value={radiusKm} onChange={(e) => setRadiusKm(Number(e.target.value))} className="input mt-0 max-w-44"><option value="1">Within 1 km</option><option value="5">Within 5 km</option><option value="10">Within 10 km</option><option value="25">Within 25 km</option></select><button onClick={search} disabled={geo.loading} className="rounded-xl bg-sky-500 px-5 py-3 font-black text-white">{geo.loading ? 'Locating…' : 'Find nearby issues'}</button></div>{(error || geo.error) && <div className="mt-5 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error || geo.error}</div>}<div className="mt-8 grid gap-5 xl:grid-cols-2">{items.map((item) => <div key={item.id}><ComplaintCard complaint={item} /><p className="mt-2 text-right text-xs text-slate-500">{item.distanceKm} km away</p></div>)}</div>{items.length === 0 && !geo.loading && <div className="mt-8"><EmptyState title="Search your area" message="Use your current location to find civic issues nearby." /></div>}</section>;
}
