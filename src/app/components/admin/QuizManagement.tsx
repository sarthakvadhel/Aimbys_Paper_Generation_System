import { useState } from 'react';
import { Search, Plus, Filter, Edit2, Trash2, Eye, MoreHorizontal, Clock, BookOpen, Users, ChevronLeft, ChevronRight, Upload } from 'lucide-react';
import { AppState } from '../../App';

const quizzes = [
  { id: 1, title: 'React Fundamentals Q1 2026', category: 'React', questions: 45, duration: 30, participants: 1240, avgScore: 74, status: 'active', created: 'May 15, 2026', difficulty: 'Medium' },
  { id: 2, title: 'Advanced JavaScript Patterns', category: 'JavaScript', questions: 60, duration: 45, participants: 980, avgScore: 68, status: 'active', created: 'May 10, 2026', difficulty: 'Hard' },
  { id: 3, title: 'Python Data Structures', category: 'Python', questions: 35, duration: 25, participants: 2100, avgScore: 81, status: 'active', created: 'May 5, 2026', difficulty: 'Easy' },
  { id: 4, title: 'System Design Principles', category: 'System Design', questions: 25, duration: 40, participants: 540, avgScore: 62, status: 'scheduled', created: 'May 3, 2026', difficulty: 'Hard' },
  { id: 5, title: 'Algorithm Complexity Analysis', category: 'Algorithms', questions: 50, duration: 35, participants: 770, avgScore: 71, status: 'active', created: 'Apr 28, 2026', difficulty: 'Hard' },
  { id: 6, title: 'Docker & Kubernetes Basics', category: 'DevOps', questions: 30, duration: 20, participants: 350, avgScore: 76, status: 'draft', created: 'Apr 25, 2026', difficulty: 'Medium' },
  { id: 7, title: 'SQL Query Optimization', category: 'Databases', questions: 40, duration: 30, participants: 890, avgScore: 79, status: 'active', created: 'Apr 20, 2026', difficulty: 'Medium' },
  { id: 8, title: 'TypeScript Advanced Types', category: 'TypeScript', questions: 35, duration: 25, participants: 620, avgScore: 65, status: 'active', created: 'Apr 15, 2026', difficulty: 'Hard' },
  { id: 9, title: 'CSS Grid & Flexbox Master', category: 'CSS', questions: 28, duration: 20, participants: 1580, avgScore: 84, status: 'archived', created: 'Apr 10, 2026', difficulty: 'Easy' },
  { id: 10, title: 'REST API Design Best Practices', category: 'Backend', questions: 32, duration: 25, participants: 720, avgScore: 77, status: 'active', created: 'Apr 8, 2026', difficulty: 'Medium' },
];

const statusConfig: Record<string, { label: string; className: string }> = {
  active: { label: 'Active', className: 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400' },
  scheduled: { label: 'Scheduled', className: 'bg-indigo-100 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-400' },
  draft: { label: 'Draft', className: 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400' },
  archived: { label: 'Archived', className: 'bg-amber-100 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400' },
};

const difficultyColors: Record<string, string> = {
  Easy: 'text-emerald-600 dark:text-emerald-400',
  Medium: 'text-amber-600 dark:text-amber-400',
  Hard: 'text-rose-600 dark:text-rose-400',
};

export function QuizManagement({ setView }: AppState) {
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [page, setPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const perPage = 8;

  const categories = ['all', ...Array.from(new Set(quizzes.map(q => q.category)))];
  const statuses = ['all', 'active', 'scheduled', 'draft', 'archived'];

  const filtered = quizzes.filter(q => {
    const matchSearch = q.title.toLowerCase().includes(search.toLowerCase()) ||
      q.category.toLowerCase().includes(search.toLowerCase());
    const matchStatus = statusFilter === 'all' || q.status === statusFilter;
    const matchCat = categoryFilter === 'all' || q.category === categoryFilter;
    return matchSearch && matchStatus && matchCat;
  });

  const paginated = filtered.slice((page - 1) * perPage, page * perPage);
  const totalPages = Math.ceil(filtered.length / perPage);

  const toggleSelect = (id: number) =>
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);

  const toggleAll = () =>
    setSelectedIds(selectedIds.length === paginated.length ? [] : paginated.map(q => q.id));

  return (
    <div className="space-y-6 pb-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white">Quiz Bank</h1>
          <p className="text-slate-500 dark:text-slate-400">{quizzes.length} quizzes total</p>
        </div>
        <button
          onClick={() => setView('admin-upload')}
          className="flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25"
        >
          <Upload className="w-4 h-4" />
          <span className="hidden sm:block">Upload Quiz</span>
          <span className="sm:hidden">Upload</span>
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 flex-1 px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl">
          <Search className="w-4 h-4 text-slate-400 flex-shrink-0" />
          <input
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }}
            placeholder="Search quizzes..."
            className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-600 outline-none flex-1"
          />
        </div>
        <select
          value={statusFilter}
          onChange={e => { setStatusFilter(e.target.value); setPage(1); }}
          className="px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white outline-none cursor-pointer"
        >
          {statuses.map(s => <option key={s} value={s}>{s === 'all' ? 'All Status' : s.charAt(0).toUpperCase() + s.slice(1)}</option>)}
        </select>
        <select
          value={categoryFilter}
          onChange={e => { setCategoryFilter(e.target.value); setPage(1); }}
          className="px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white outline-none cursor-pointer"
        >
          {categories.map(c => <option key={c} value={c}>{c === 'all' ? 'All Categories' : c}</option>)}
        </select>
      </div>

      {/* Bulk Actions */}
      {selectedIds.length > 0 && (
        <div className="flex items-center gap-3 px-4 py-3 bg-indigo-50 dark:bg-indigo-950/30 border border-indigo-200 dark:border-indigo-800 rounded-xl">
          <span className="text-indigo-700 dark:text-indigo-300 font-medium">{selectedIds.length} selected</span>
          <div className="flex-1" />
          <button className="px-3 py-1.5 text-slate-600 dark:text-slate-400 hover:bg-indigo-100 dark:hover:bg-indigo-950/50 rounded-lg transition-colors text-sm">Archive</button>
          <button className="px-3 py-1.5 text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/20 rounded-lg transition-colors text-sm">Delete</button>
        </div>
      )}

      {/* Table */}
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-800">
              <tr>
                <th className="px-4 py-3 w-10">
                  <input
                    type="checkbox"
                    checked={selectedIds.length === paginated.length && paginated.length > 0}
                    onChange={toggleAll}
                    className="rounded"
                  />
                </th>
                {['Quiz', 'Category', 'Questions', 'Duration', 'Participants', 'Avg Score', 'Difficulty', 'Status', 'Created', ''].map(h => (
                  <th key={h} className="text-left px-4 py-3 text-slate-500 dark:text-slate-400 text-sm font-semibold whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {paginated.map(quiz => (
                <tr key={quiz.id} className="border-t border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                  <td className="px-4 py-4">
                    <input
                      type="checkbox"
                      checked={selectedIds.includes(quiz.id)}
                      onChange={() => toggleSelect(quiz.id)}
                      className="rounded"
                    />
                  </td>
                  <td className="px-4 py-4">
                    <div className="font-semibold text-slate-900 dark:text-slate-100 max-w-xs truncate">{quiz.title}</div>
                  </td>
                  <td className="px-4 py-4">
                    <span className="px-2 py-1 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 rounded-lg text-sm">{quiz.category}</span>
                  </td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-1 text-slate-600 dark:text-slate-400">
                      <BookOpen className="w-3.5 h-3.5" />
                      <span className="text-sm">{quiz.questions}</span>
                    </div>
                  </td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-1 text-slate-600 dark:text-slate-400">
                      <Clock className="w-3.5 h-3.5" />
                      <span className="text-sm">{quiz.duration}m</span>
                    </div>
                  </td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-1 text-slate-600 dark:text-slate-400">
                      <Users className="w-3.5 h-3.5" />
                      <span className="text-sm">{quiz.participants.toLocaleString()}</span>
                    </div>
                  </td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-2">
                      <div className="w-20 h-2 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full ${quiz.avgScore >= 75 ? 'bg-emerald-500' : quiz.avgScore >= 60 ? 'bg-amber-500' : 'bg-rose-500'}`}
                          style={{ width: `${quiz.avgScore}%` }}
                        />
                      </div>
                      <span className="text-sm font-medium text-slate-700 dark:text-slate-300">{quiz.avgScore}%</span>
                    </div>
                  </td>
                  <td className="px-4 py-4">
                    <span className={`text-sm font-medium ${difficultyColors[quiz.difficulty]}`}>{quiz.difficulty}</span>
                  </td>
                  <td className="px-4 py-4">
                    <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${statusConfig[quiz.status].className}`}>
                      {statusConfig[quiz.status].label}
                    </span>
                  </td>
                  <td className="px-4 py-4 text-slate-500 dark:text-slate-400 text-sm whitespace-nowrap">{quiz.created}</td>
                  <td className="px-4 py-4">
                    <div className="flex items-center gap-1">
                      <button className="p-1.5 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-950/20 rounded-lg transition-all">
                        <Eye className="w-4 h-4" />
                      </button>
                      <button className="p-1.5 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-all">
                        <Edit2 className="w-4 h-4" />
                      </button>
                      <button className="p-1.5 text-slate-400 hover:text-rose-500 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/20 rounded-lg transition-all">
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="px-4 py-4 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <span className="text-slate-500 dark:text-slate-400 text-sm">
            Showing {(page - 1) * perPage + 1}–{Math.min(page * perPage, filtered.length)} of {filtered.length}
          </span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
              <button
                key={p}
                onClick={() => setPage(p)}
                className={`w-8 h-8 rounded-lg text-sm font-medium transition-colors ${
                  p === page ? 'bg-indigo-500 text-white' : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'
                }`}
              >
                {p}
              </button>
            ))}
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="p-2 rounded-lg text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
