import { useState } from 'react';
import { Search, Filter, Building2, CheckCircle, Clock, XCircle, Eye, Edit2, MoreHorizontal, ChevronLeft, ChevronRight, Download, Plus } from 'lucide-react';
import { AimbysState } from '../../../App';

const institutes = [
  { id: 'INST-2024-0001', name: 'IIT Bombay Entrance Academy', city: 'Mumbai', state: 'Maharashtra', type: 'Coaching', users: 4820, papers: 1240, status: 'active', license: 'Enterprise', expiry: 'Dec 2026', admin: 'Dr. Ramesh Patil' },
  { id: 'INST-2024-0002', name: 'Delhi Public School HQ', city: 'New Delhi', state: 'Delhi', type: 'School Chain', users: 28400, papers: 6820, status: 'active', license: 'Premium', expiry: 'Mar 2027', admin: 'Mrs. Sunita Sharma' },
  { id: 'INST-2024-0003', name: 'Govt. Polytechnic Nagpur', city: 'Nagpur', state: 'Maharashtra', type: 'Government', users: 2140, papers: 480, status: 'active', license: 'Standard', expiry: 'Aug 2026', admin: 'Prof. A.K. Singh' },
  { id: 'INST-2024-0004', name: 'Karnataka Board Exam Cell', city: 'Bengaluru', state: 'Karnataka', type: 'State Board', users: 84200, papers: 24000, status: 'active', license: 'Enterprise', expiry: 'Jan 2028', admin: 'Dr. M.S. Reddy' },
  { id: 'INST-2024-0005', name: 'FIITJEE National Network', city: 'Hyderabad', state: 'Telangana', type: 'Coaching', users: 12400, papers: 3840, status: 'suspended', license: 'Premium', expiry: 'Sep 2025', admin: 'Mr. V.K. Gupta' },
  { id: 'INST-2024-0006', name: 'Amity University Online', city: 'Noida', state: 'UP', type: 'University', users: 38200, papers: 9200, status: 'active', license: 'Enterprise', expiry: 'Jun 2027', admin: 'Dr. Priya Agarwal' },
  { id: 'INST-2024-0007', name: 'Symbiosis International', city: 'Pune', state: 'Maharashtra', type: 'University', users: 24800, papers: 5600, status: 'active', license: 'Premium', expiry: 'Nov 2026', admin: 'Prof. S.B. Kale' },
  { id: 'INST-2024-0008', name: 'ALLEN Career Institute', city: 'Kota', state: 'Rajasthan', type: 'Coaching', users: 18600, papers: 5200, status: 'pending', license: 'Enterprise', expiry: 'Pending', admin: 'Mr. R.K. Maheshwari' },
];

const statusConfig: Record<string, { label: string; dot: string; text: string; bg: string }> = {
  active: { label: 'Active', dot: 'bg-green-500', text: 'text-green-700 dark:text-green-400', bg: 'bg-green-50 dark:bg-green-950/40' },
  suspended: { label: 'Suspended', dot: 'bg-red-500', text: 'text-red-700 dark:text-red-400', bg: 'bg-red-50 dark:bg-red-950/40' },
  pending: { label: 'Pending', dot: 'bg-amber-500', text: 'text-amber-700 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/40' },
};

const typeColors: Record<string, string> = {
  'Government': 'bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-400',
  'State Board': 'bg-indigo-50 dark:bg-indigo-950/30 text-indigo-700 dark:text-indigo-400',
  'University': 'bg-violet-50 dark:bg-violet-950/30 text-violet-700 dark:text-violet-400',
  'School Chain': 'bg-cyan-50 dark:bg-cyan-950/30 text-cyan-700 dark:text-cyan-400',
  'Coaching': 'bg-amber-50 dark:bg-amber-950/30 text-amber-700 dark:text-amber-400',
};

export function InstituteManagement(_props: AimbysState) {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [typeFilter, setTypeFilter] = useState('all');
  const [page, setPage] = useState(1);
  const perPage = 6;

  const filtered = institutes.filter(i => {
    const ms = i.name.toLowerCase().includes(search.toLowerCase()) || i.city.toLowerCase().includes(search.toLowerCase());
    const mst = statusFilter === 'all' || i.status === statusFilter;
    const mt = typeFilter === 'all' || i.type === typeFilter;
    return ms && mst && mt;
  });
  const paginated = filtered.slice((page - 1) * perPage, page * perPage);
  const totalPages = Math.max(1, Math.ceil(filtered.length / perPage));

  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Super Admin / Institutes</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">Institute Management</h1>
          <div className="flex items-center gap-2">
            <button className="flex items-center gap-1.5 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 rounded text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
              <Download className="w-3.5 h-3.5" />Export
            </button>
            <button className="flex items-center gap-1.5 px-3 py-2 text-white rounded text-sm" style={{ background: '#1d4ed8' }}>
              <Plus className="w-3.5 h-3.5" />Onboard Institute
            </button>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 flex-1 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded">
          <Search className="w-4 h-4 text-slate-400" />
          <input value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} placeholder="Search by name, city..." className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 outline-none flex-1 text-sm" />
        </div>
        <select value={statusFilter} onChange={e => { setStatusFilter(e.target.value); setPage(1); }} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-900 dark:text-white outline-none text-sm cursor-pointer">
          {['all', 'active', 'pending', 'suspended'].map(s => <option key={s} value={s}>{s === 'all' ? 'All Status' : s.charAt(0).toUpperCase() + s.slice(1)}</option>)}
        </select>
        <select value={typeFilter} onChange={e => { setTypeFilter(e.target.value); setPage(1); }} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-900 dark:text-white outline-none text-sm cursor-pointer">
          {['all', 'Government', 'State Board', 'University', 'School Chain', 'Coaching'].map(t => <option key={t} value={t}>{t === 'all' ? 'All Types' : t}</option>)}
        </select>
      </div>

      {/* Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-800">
              <tr>
                {['Institute ID', 'Name', 'Location', 'Type', 'Users', 'Papers', 'License', 'Status', 'Institute Admin', 'Actions'].map(h => (
                  <th key={h} className="text-left px-4 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {paginated.map(inst => {
                const sc = statusConfig[inst.status];
                return (
                  <tr key={inst.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                    <td className="px-4 py-3.5">
                      <span className="font-mono text-blue-600 dark:text-blue-400 text-xs">{inst.id}</span>
                    </td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded bg-slate-100 dark:bg-slate-800 flex items-center justify-center flex-shrink-0">
                          <Building2 className="w-3.5 h-3.5 text-slate-500 dark:text-slate-400" />
                        </div>
                        <span className="text-slate-900 dark:text-slate-100 font-medium text-sm">{inst.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 text-slate-500 dark:text-slate-400 text-sm">{inst.city}, {inst.state}</td>
                    <td className="px-4 py-3.5">
                      <span className={`px-2 py-0.5 rounded text-xs font-medium ${typeColors[inst.type] || 'bg-slate-100 text-slate-600'}`}>{inst.type}</span>
                    </td>
                    <td className="px-4 py-3.5 text-slate-700 dark:text-slate-300 text-sm">{inst.users.toLocaleString()}</td>
                    <td className="px-4 py-3.5 text-slate-700 dark:text-slate-300 text-sm">{inst.papers.toLocaleString()}</td>
                    <td className="px-4 py-3.5">
                      <div>
                        <span className="text-slate-900 dark:text-slate-100 text-sm font-medium">{inst.license}</span>
                        <div className="text-slate-400 text-xs">Exp: {inst.expiry}</div>
                      </div>
                    </td>
                    <td className="px-4 py-3.5">
                      <div className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded ${sc.bg}`}>
                        <div className={`w-1.5 h-1.5 rounded-full ${sc.dot}`} />
                        <span className={`text-xs font-medium ${sc.text}`}>{sc.label}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{inst.admin}</td>
                    <td className="px-4 py-3.5">
                      <div className="flex items-center gap-1">
                        <button className="p-1.5 text-slate-400 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-950/20 rounded transition-all"><Eye className="w-3.5 h-3.5" /></button>
                        <button className="p-1.5 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded transition-all"><Edit2 className="w-3.5 h-3.5" /></button>
                        <button className="p-1.5 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded transition-all"><MoreHorizontal className="w-3.5 h-3.5" /></button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <div className="px-4 py-3.5 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between bg-slate-50 dark:bg-slate-800/30">
          <span className="text-slate-500 dark:text-slate-400 text-sm">Showing {(page-1)*perPage+1}–{Math.min(page*perPage,filtered.length)} of {filtered.length}</span>
          <div className="flex items-center gap-1.5">
            <button onClick={() => setPage(p => Math.max(1,p-1))} disabled={page===1} className="p-1.5 rounded border border-slate-200 dark:border-slate-700 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed"><ChevronLeft className="w-4 h-4" /></button>
            {Array.from({length:totalPages},(_,i)=>i+1).map(p=>(
              <button key={p} onClick={()=>setPage(p)} className={`w-8 h-8 rounded border text-sm font-medium transition-colors ${p===page?'border-blue-600 bg-blue-600 text-white':'border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'}`}>{p}</button>
            ))}
            <button onClick={() => setPage(p => Math.min(totalPages,p+1))} disabled={page===totalPages} className="p-1.5 rounded border border-slate-200 dark:border-slate-700 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed"><ChevronRight className="w-4 h-4" /></button>
          </div>
        </div>
      </div>
    </div>
  );
}
