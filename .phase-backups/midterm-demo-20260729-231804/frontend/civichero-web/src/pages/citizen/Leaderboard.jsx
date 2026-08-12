import { useEffect, useState } from 'react';
import { rewardApi } from '../../services/rewardApi.js';

export default function Leaderboard() {
  const [rows, setRows] = useState([]);
  const [error, setError] = useState('');
  useEffect(() => { rewardApi.leaderboard(50).then(setRows).catch((reason) => setError(reason.message)); }, []);
  return <section className="p-6 lg:p-10"><p className="text-sm font-bold uppercase tracking-wider text-amber-300">Positive civic competition</p><h2 className="mt-2 text-3xl font-black text-white">Citizen leaderboard</h2><p className="mt-2 text-slate-400">Points are awarded only after valid complaint closure.</p>{error && <div className="mt-5 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}<div className="mt-8 overflow-hidden rounded-2xl border border-white/10"><table className="w-full text-left"><thead className="bg-white/5 text-xs uppercase tracking-wider text-slate-400"><tr><th className="p-4">Rank</th><th className="p-4">Citizen</th><th className="p-4">Tier</th><th className="p-4">Closed issues</th><th className="p-4 text-right">Points</th></tr></thead><tbody className="divide-y divide-white/10">{rows.map((row) => <tr key={row.userId} className={row.isCurrentUser ? 'bg-sky-400/10' : ''}><td className="p-4 text-2xl font-black text-amber-300">#{row.rank}</td><td className="p-4 font-bold text-white">{row.citizenName}{row.isCurrentUser && <span className="ml-2 rounded-full bg-sky-500 px-2 py-1 text-xs">You</span>}</td><td className="p-4 text-slate-300">{row.tier}</td><td className="p-4 text-slate-300">{row.closedComplaints}</td><td className="p-4 text-right text-xl font-black text-white">{row.points}</td></tr>)}</tbody></table></div></section>;
}
