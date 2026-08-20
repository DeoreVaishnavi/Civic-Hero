import { useEffect, useMemo, useState } from 'react';
import { getCategories, getDepartments, getWards, saveCategory, saveDepartment, saveWard, setCategoryActive, setDepartmentActive, setWardActive } from '../../services/adminApi.js';

const blankCategory = { name: '', code: '', description: '', defaultPriority: 'Medium', icon: '', departmentId: '', isActive: true, sortOrder: 0 };
const blankDepartment = { name: '', code: '', description: '', isActive: true };
const blankWard = { departmentId: '', name: '', code: '', boundaryNorth: 0, boundarySouth: 0, boundaryEast: 0, boundaryWest: 0, isActive: true };

export default function Governance() {
  const [tab, setTab] = useState('categories');
  const [categories, setCategories] = useState([]); const [departments, setDepartments] = useState([]); const [wards, setWards] = useState([]);
  const [category, setCategory] = useState(blankCategory); const [department, setDepartment] = useState(blankDepartment); const [ward, setWard] = useState(blankWard);
  const [editing, setEditing] = useState(null); const [message, setMessage] = useState(''); const [busy, setBusy] = useState(false);
  const load = async () => { const [c, d, w] = await Promise.all([getCategories(), getDepartments(), getWards()]); setCategories(c); setDepartments(d); setWards(w); };
  useEffect(() => { load().catch((e) => setMessage(e.message)); }, []);
  const reset = () => { setEditing(null); setCategory(blankCategory); setDepartment(blankDepartment); setWard(blankWard); };
  const submit = async (event) => {
    event.preventDefault(); setBusy(true); setMessage('');
    try {
      if (tab === 'categories') await saveCategory(editing, { ...category, departmentId: category.departmentId ? Number(category.departmentId) : null, sortOrder: Number(category.sortOrder) });
      if (tab === 'departments') await saveDepartment(editing, department);
      if (tab === 'wards') await saveWard(editing, { ...ward, departmentId: Number(ward.departmentId), boundaryNorth: Number(ward.boundaryNorth), boundarySouth: Number(ward.boundarySouth), boundaryEast: Number(ward.boundaryEast), boundaryWest: Number(ward.boundaryWest) });
      setMessage(editing ? 'Record updated.' : 'Record created.'); reset(); await load();
    } catch (error) { setMessage(error.message); } finally { setBusy(false); }
  };
  const rows = tab === 'categories' ? categories : tab === 'departments' ? departments : wards;
  const edit = (item) => { setEditing(item.id); if (tab === 'categories') setCategory({ ...item, departmentId: item.departmentId ?? '' }); if (tab === 'departments') setDepartment(item); if (tab === 'wards') setWard(item); window.scrollTo({ top: 0, behavior: 'smooth' }); };
  const toggle = async (item) => { setBusy(true); try { if (tab === 'categories') await setCategoryActive(item.id, !item.isActive); if (tab === 'departments') await setDepartmentActive(item.id, !item.isActive); if (tab === 'wards') await setWardActive(item.id, !item.isActive); await load(); } catch (e) { setMessage(e.message); } finally { setBusy(false); } };
  return <section className="p-6 lg:p-10">
    <p className="text-xs font-black uppercase tracking-[0.2em] text-violet-300">Master data</p><h1 className="mt-2 text-3xl font-black text-white">Civic governance</h1><p className="mt-2 text-slate-400">Manage complaint categories, municipal departments and ward boundaries.</p>
    <div className="mt-6 flex flex-wrap gap-2">{['categories','departments','wards'].map((name) => <button key={name} onClick={() => { setTab(name); reset(); }} className={`rounded-xl px-4 py-2 text-sm font-bold capitalize ${tab === name ? 'bg-violet-500 text-white' : 'bg-white/5 text-slate-300'}`}>{name}</button>)}</div>
    {message && <div className="mt-5 rounded-xl border border-sky-400/20 bg-sky-400/10 p-3 text-sm text-sky-200">{message}</div>}
    <form onSubmit={submit} className="mt-6 rounded-2xl border border-white/10 bg-white/5 p-5"><h2 className="font-bold text-white">{editing ? 'Edit' : 'Add'} {tab.slice(0,-1)}</h2>
      <div className="mt-4 grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {tab === 'categories' && <><Field label="Name" value={category.name} onChange={(v) => setCategory({ ...category, name:v })}/><Field label="Code" value={category.code} onChange={(v) => setCategory({ ...category, code:v })}/><Select label="Default priority" value={category.defaultPriority} onChange={(v) => setCategory({ ...category, defaultPriority:v })} options={['Low','Medium','High','Critical'].map(x => ({value:x,label:x}))}/><Select label="Department" value={category.departmentId} onChange={(v) => setCategory({ ...category, departmentId:v })} options={[{value:'',label:'Not fixed'},...departments.map(x=>({value:x.id,label:x.name}))]}/><Field label="Icon" value={category.icon ?? ''} onChange={(v) => setCategory({ ...category, icon:v })}/><Field label="Sort order" type="number" value={category.sortOrder} onChange={(v) => setCategory({ ...category, sortOrder:v })}/><Field label="Description" value={category.description ?? ''} onChange={(v) => setCategory({ ...category, description:v })}/></>}
        {tab === 'departments' && <><Field label="Name" value={department.name} onChange={(v) => setDepartment({ ...department, name:v })}/><Field label="Code" value={department.code} onChange={(v) => setDepartment({ ...department, code:v })}/><Field label="Description" value={department.description ?? ''} onChange={(v) => setDepartment({ ...department, description:v })}/></>}
        {tab === 'wards' && <><Select label="Department" value={ward.departmentId} onChange={(v) => setWard({ ...ward, departmentId:v })} options={[{value:'',label:'Select department'},...departments.filter(x=>x.isActive).map(x=>({value:x.id,label:x.name}))]}/><Field label="Name" value={ward.name} onChange={(v) => setWard({ ...ward, name:v })}/><Field label="Code" value={ward.code} onChange={(v) => setWard({ ...ward, code:v })}/>{['boundaryNorth','boundarySouth','boundaryEast','boundaryWest'].map(k => <Field key={k} label={k.replace('boundary','')} type="number" step="0.000001" value={ward[k]} onChange={(v) => setWard({ ...ward, [k]:v })}/>)}</>}
      </div><div className="mt-4 flex gap-3"><button disabled={busy} className="rounded-xl bg-violet-500 px-5 py-2.5 font-bold text-white">{busy ? 'Saving…' : 'Save'}</button>{editing && <button type="button" onClick={reset} className="rounded-xl bg-white/10 px-5 py-2.5 font-bold text-white">Cancel</button>}</div>
    </form>
    <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10"><table className="min-w-full text-left text-sm"><thead className="bg-white/5 text-slate-400"><tr><th className="p-4">Name</th><th className="p-4">Code / scope</th><th className="p-4">Usage</th><th className="p-4">Status</th><th className="p-4">Actions</th></tr></thead><tbody>{rows.map(item => <tr key={item.id} className="border-t border-white/10 text-slate-300"><td className="p-4 font-bold text-white">{item.name}</td><td className="p-4">{item.code}{item.departmentName ? ` · ${item.departmentName}` : ''}</td><td className="p-4">{tab === 'departments' ? `${item.wardCount} wards · ${item.openComplaintCount} open` : tab === 'wards' ? `${item.activeUserCount} users · ${item.openComplaintCount} open` : item.defaultPriority}</td><td className="p-4"><span className={`rounded-full px-2 py-1 text-xs font-bold ${item.isActive ? 'bg-emerald-400/10 text-emerald-300' : 'bg-rose-400/10 text-rose-300'}`}>{item.isActive ? 'Active' : 'Inactive'}</span></td><td className="p-4"><button onClick={() => edit(item)} className="mr-3 text-sky-300">Edit</button><button disabled={busy} onClick={() => toggle(item)} className="text-amber-300">{item.isActive ? 'Deactivate' : 'Activate'}</button></td></tr>)}</tbody></table></div>
  </section>;
}
function Field({ label, value, onChange, type='text', step }) { return <label className="text-sm text-slate-400">{label}<input required={['Name','Code'].includes(label)} type={type} step={step} value={value} onChange={(e)=>onChange(e.target.value)} className="mt-1 w-full rounded-xl border border-white/10 bg-slate-950 px-3 py-2 text-white"/></label>; }
function Select({ label, value, onChange, options }) { return <label className="text-sm text-slate-400">{label}<select value={value} onChange={(e)=>onChange(e.target.value)} className="mt-1 w-full rounded-xl border border-white/10 bg-slate-950 px-3 py-2 text-white">{options.map(x=><option key={x.value} value={x.value}>{x.label}</option>)}</select></label>; }
