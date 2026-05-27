import { useState } from 'react';
import { Search, MoreHorizontal, UserPlus, ChevronLeft, ChevronRight, TrendingUp, Shield, Mail, Ban } from 'lucide-react';
import { AppState } from '../../App';

const users = [
  { id: 1, name: 'Sophia Martinez', email: 'sophia@example.com', avatar: 'SM', role: 'student', level: 12, xp: 9240, quizzes: 84, avgScore: 88, streak: 14, joined: 'Jan 12, 2026', status: 'active' },
  { id: 2, name: 'Liam Johnson', email: 'liam@example.com', avatar: 'LJ', role: 'student', level: 9, xp: 6820, quizzes: 61, avgScore: 76, streak: 7, joined: 'Feb 3, 2026', status: 'active' },
  { id: 3, name: 'Emma Davis', email: 'emma@example.com', avatar: 'ED', role: 'student', level: 15, xp: 12500, quizzes: 112, avgScore: 91, streak: 28, joined: 'Dec 5, 2025', status: 'active' },
  { id: 4, name: 'Noah Wilson', email: 'noah@example.com', avatar: 'NW', role: 'student', level: 6, xp: 4100, quizzes: 38, avgScore: 71, streak: 3, joined: 'Mar 20, 2026', status: 'active' },
  { id: 5, name: 'Ava Brown', email: 'ava@example.com', avatar: 'AB', role: 'student', level: 11, xp: 8640, quizzes: 77, avgScore: 84, streak: 10, joined: 'Jan 30, 2026', status: 'active' },
  { id: 6, name: 'James Lee', email: 'james@example.com', avatar: 'JL', role: 'student', level: 4, xp: 2200, quizzes: 21, avgScore: 64, streak: 0, joined: 'Apr 15, 2026', status: 'inactive' },
  { id: 7, name: 'Isabella Clark', email: 'isabella@example.com', avatar: 'IC', role: 'instructor', level: 20, xp: 18900, quizzes: 190, avgScore: 94, streak: 45, joined: 'Oct 12, 2025', status: 'active' },
  { id: 8, name: 'William Taylor', email: 'william@example.com', avatar: 'WT', role: 'student', level: 8, xp: 5900, quizzes: 55, avgScore: 79, streak: 5, joined: 'Feb 28, 2026', status: 'active' },
  { id: 9, name: 'Mia Anderson', email: 'mia@example.com', avatar: 'MA', role: 'student', level: 13, xp: 10200, quizzes: 95, avgScore: 87, streak: 19, joined: 'Jan 5, 2026', status: 'active' },
  { id: 10, name: 'Benjamin Thomas', email: 'ben@example.com', avatar: 'BT', role: 'student', level: 3, xp: 1500, quizzes: 14, avgScore: 58, streak: 0, joined: 'May 2, 2026', status: 'suspended' },
];

const avatarColors = [
  'from-indigo-400 to-indigo-600', 'from-violet-400 to-violet-600',
  'from-cyan-400 to-cyan-600', 'from-emerald-400 to-emerald-600',
  'from-amber-400 to-amber-600', 'from-rose-400 to-rose-600',
];

const statusConfig: Record<string, { label: string; className: string }> = {
  active: { label: 'Active', className: 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400' },
  inactive: { label: 'Inactive', className: 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-500' },
  suspended: { label: 'Suspended', className: 'bg-rose-100 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400' },
};

const roleConfig: Record<string, { label: string; className: string }> = {
  student: { label: 'Student', className: 'bg-indigo-100 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-400' },
  instructor: { label: 'Instructor', className: 'bg-violet-100 dark:bg-violet-950/40 text-violet-700 dark:text-violet-400' },
  admin: { label: 'Admin', className: 'bg-amber-100 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400' },
};

export function UserManagement(_props: AppState) {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [roleFilter, setRoleFilter] = useState('all');
  const [page, setPage] = useState(1);
  const perPage = 8;

  const filtered = users.filter(u => {
    const matchSearch = u.name.toLowerCase().includes(search.toLowerCase()) ||
      u.email.toLowerCase().includes(search.toLowerCase());
    const matchStatus = statusFilter === 'all' || u.status === statusFilter;
    const matchRole = roleFilter === 'all' || u.role === roleFilter;
    return matchSearch && matchStatus && matchRole;
  });

  const paginated = filtered.slice((page - 1) * perPage, page * perPage);
  const totalPages = Math.max(1, Math.ceil(filtered.length / perPage));

  return (
    <div className="space-y-6 pb-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white">User Management</h1>
          <p className="text-slate-500 dark:text-slate-400">{users.length} registered users</p>
        </div>
        <button className="flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25">
          <UserPlus className="w-4 h-4" />
          <span className="hidden sm:block">Invite User</span>
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: 'Total Users', value: users.length, color: 'text-indigo-600 dark:text-indigo-400', bg: 'bg-indigo-50 dark:bg-indigo-950/40' },
          { label: 'Active', value: users.filter(u => u.status === 'active').length, color: 'text-emerald-600 dark:text-emerald-400', bg: 'bg-emerald-50 dark:bg-emerald-950/40' },
          { label: 'Inactive', value: users.filter(u => u.status === 'inactive').length, color: 'text-slate-600 dark:text-slate-400', bg: 'bg-slate-100 dark:bg-slate-800' },
          { label: 'Suspended', value: users.filter(u => u.status === 'suspended').length, color: 'text-rose-600 dark:text-rose-400', bg: 'bg-rose-50 dark:bg-rose-950/40' },
        ].map(card => (
          <div key={card.label} className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-4">
            <div className={`w-8 h-8 ${card.bg} rounded-xl flex items-center justify-center mb-3`}>
              <div className={`w-3 h-3 rounded-full ${card.color.replace('text-', 'bg-').replace('dark:text-', 'dark:bg-')}`} />
            </div>
            <div className={`text-2xl font-black ${card.color} mb-0.5`}>{card.value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm">{card.label}</div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 flex-1 px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl">
          <Search className="w-4 h-4 text-slate-400 flex-shrink-0" />
          <input
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }}
            placeholder="Search by name or email..."
            className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-600 outline-none flex-1"
          />
        </div>
        <select
          value={statusFilter}
          onChange={e => { setStatusFilter(e.target.value); setPage(1); }}
          className="px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white outline-none cursor-pointer"
        >
          {['all', 'active', 'inactive', 'suspended'].map(s => (
            <option key={s} value={s}>{s === 'all' ? 'All Status' : s.charAt(0).toUpperCase() + s.slice(1)}</option>
          ))}
        </select>
        <select
          value={roleFilter}
          onChange={e => { setRoleFilter(e.target.value); setPage(1); }}
          className="px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white outline-none cursor-pointer"
        >
          {['all', 'student', 'instructor'].map(r => (
            <option key={r} value={r}>{r === 'all' ? 'All Roles' : r.charAt(0).toUpperCase() + r.slice(1)}</option>
          ))}
        </select>
      </div>

      {/* Table */}
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-800">
              <tr>
                {['User', 'Role', 'Level', 'Quizzes', 'Avg Score', 'Streak', 'Joined', 'Status', ''].map(h => (
                  <th key={h} className="text-left px-4 py-3 text-slate-500 dark:text-slate-400 text-sm font-semibold whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {paginated.map((user, idx) => (
                <tr key={user.id} className="border-t border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-3">
                      <div className={`w-10 h-10 rounded-full bg-gradient-to-br ${avatarColors[idx % avatarColors.length]} flex items-center justify-center text-white font-bold text-sm flex-shrink-0`}>
                        {user.avatar}
                      </div>
                      <div>
                        <div className="font-semibold text-slate-900 dark:text-slate-100">{user.name}</div>
                        <div className="text-slate-500 dark:text-slate-400 text-sm">{user.email}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-4">
                    <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${roleConfig[user.role].className}`}>
                      {roleConfig[user.role].label}
                    </span>
                  </td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-2">
                      <div className="w-6 h-6 rounded-full bg-indigo-100 dark:bg-indigo-950/40 flex items-center justify-center">
                        <span className="text-indigo-700 dark:text-indigo-400 text-xs font-bold">{user.level}</span>
                      </div>
                      <span className="text-slate-500 dark:text-slate-400 text-sm">{user.xp.toLocaleString()} XP</span>
                    </div>
                  </td>
                  <td className="px-4 py-4 text-slate-700 dark:text-slate-300 text-sm">{user.quizzes}</td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-2">
                      <div className="w-16 h-2 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full ${user.avgScore >= 80 ? 'bg-emerald-500' : user.avgScore >= 65 ? 'bg-amber-500' : 'bg-rose-500'}`}
                          style={{ width: `${user.avgScore}%` }}
                        />
                      </div>
                      <span className="text-sm text-slate-700 dark:text-slate-300 font-medium">{user.avgScore}%</span>
                    </div>
                  </td>
                  <td className="px-4 py-4">
                    <span className={`text-sm font-semibold ${user.streak > 0 ? 'text-amber-600 dark:text-amber-400' : 'text-slate-400 dark:text-slate-600'}`}>
                      {user.streak > 0 ? `🔥 ${user.streak}d` : '—'}
                    </span>
                  </td>
                  <td className="px-4 py-4 text-slate-500 dark:text-slate-400 text-sm whitespace-nowrap">{user.joined}</td>
                  <td className="px-4 py-4">
                    <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${statusConfig[user.status].className}`}>
                      {statusConfig[user.status].label}
                    </span>
                  </td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-1">
                      <button className="p-1.5 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-950/20 rounded-lg transition-all" title="Message">
                        <Mail className="w-4 h-4" />
                      </button>
                      <button className="p-1.5 text-slate-400 hover:text-violet-600 dark:hover:text-violet-400 hover:bg-violet-50 dark:hover:bg-violet-950/20 rounded-lg transition-all" title="Promote">
                        <Shield className="w-4 h-4" />
                      </button>
                      <button className="p-1.5 text-slate-400 hover:text-rose-500 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/20 rounded-lg transition-all" title="Suspend">
                        <Ban className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="px-4 py-4 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <span className="text-slate-500 dark:text-slate-400 text-sm">
            {(page - 1) * perPage + 1}–{Math.min(page * perPage, filtered.length)} of {filtered.length} users
          </span>
          <div className="flex items-center gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 transition-colors">
              <ChevronLeft className="w-4 h-4" />
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
              <button key={p} onClick={() => setPage(p)} className={`w-8 h-8 rounded-lg text-sm font-medium transition-colors ${p === page ? 'bg-indigo-500 text-white' : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'}`}>{p}</button>
            ))}
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 transition-colors">
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
