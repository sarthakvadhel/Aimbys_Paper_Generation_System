import { useState } from 'react';
import { Search, Plus, Filter, BookOpen, Code, AlignLeft, CheckSquare, Eye, Edit2, Trash2, ChevronLeft, ChevronRight, Tag } from 'lucide-react';
import { AimbysState } from '../../../App';

const questions = [
  { id: 'QB-00841', type: 'MCQ', subject: 'Mathematics', topic: 'Calculus', class: 'XII', difficulty: 'Hard', bloom: "Analyse", marks: 4, usageCount: 28, quality: 4.8, text: 'If f(x) = x³ − 3x² + 2x, find the intervals where f\'(x) > 0.' },
  { id: 'QB-00842', type: 'MCQ', subject: 'Physics', topic: 'Mechanics', class: 'XI', difficulty: 'Medium', bloom: 'Apply', marks: 3, usageCount: 42, quality: 4.5, text: 'A body of mass 2 kg is moving with velocity 5 m/s. What is the kinetic energy?' },
  { id: 'QB-00843', type: 'Descriptive', subject: 'Chemistry', topic: 'Organic', class: 'XII', difficulty: 'Medium', bloom: 'Understand', marks: 6, usageCount: 14, quality: 4.6, text: 'Explain the mechanism of SN2 reaction with a suitable example.' },
  { id: 'QB-00844', type: 'Coding', subject: 'Computer Science', topic: 'Arrays', class: 'XII', difficulty: 'Hard', bloom: 'Create', marks: 10, usageCount: 9, quality: 4.9, text: 'Write a program to find the maximum subarray sum using Kadane\'s Algorithm.' },
  { id: 'QB-00845', type: 'FillBlanks', subject: 'English', topic: 'Grammar', class: 'X', difficulty: 'Easy', bloom: 'Remember', marks: 1, usageCount: 84, quality: 4.3, text: 'The book _______ (lie/lay) on the table since morning.' },
  { id: 'QB-00846', type: 'MCQ', subject: 'Biology', topic: 'Cell Biology', class: 'XI', difficulty: 'Medium', bloom: 'Understand', marks: 2, usageCount: 38, quality: 4.4, text: 'Which organelle is known as the powerhouse of the cell?' },
  { id: 'QB-00847', type: 'Descriptive', subject: 'Mathematics', topic: 'Trigonometry', class: 'XI', difficulty: 'Hard', bloom: 'Evaluate', marks: 8, usageCount: 17, quality: 4.7, text: 'Prove that sin(A+B) = sinA·cosB + cosA·sinB using unit circle.' },
  { id: 'QB-00848', type: 'Coding', subject: 'Computer Science', topic: 'Sorting', class: 'XII', difficulty: 'Medium', bloom: 'Apply', marks: 6, usageCount: 22, quality: 4.6, text: 'Implement merge sort and analyze its time complexity.' },
  { id: 'QB-00849', type: 'MCQ', subject: 'Physics', topic: 'Thermodynamics', class: 'XII', difficulty: 'Easy', bloom: 'Remember', marks: 1, usageCount: 61, quality: 4.2, text: 'Which law of thermodynamics states that energy cannot be created or destroyed?' },
  { id: 'QB-00850', type: 'FillBlanks', subject: 'Chemistry', topic: 'Periodic Table', class: 'X', difficulty: 'Easy', bloom: 'Remember', marks: 1, usageCount: 92, quality: 4.1, text: 'The atomic number of Carbon is ______.' },
];

const typeConfig: Record<string, { label: string; icon: any; color: string; bg: string }> = {
  'MCQ': { label: 'MCQ', icon: CheckSquare, color: 'text-blue-700 dark:text-blue-400', bg: 'bg-blue-50 dark:bg-blue-950/30' },
  'Descriptive': { label: 'Descriptive', icon: AlignLeft, color: 'text-violet-700 dark:text-violet-400', bg: 'bg-violet-50 dark:bg-violet-950/30' },
  'Coding': { label: 'Coding', icon: Code, color: 'text-emerald-700 dark:text-emerald-400', bg: 'bg-emerald-50 dark:bg-emerald-950/30' },
  'FillBlanks': { label: 'Fill Blanks', icon: BookOpen, color: 'text-amber-700 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/30' },
};

const difficultyColor: Record<string, string> = {
  'Easy': 'text-green-600 dark:text-green-400',
  'Medium': 'text-amber-600 dark:text-amber-400',
  'Hard': 'text-red-600 dark:text-red-400',
};

export function QuestionBank(_props: AimbysState) {
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('all');
  const [subjectFilter, setSubjectFilter] = useState('all');
  const [diffFilter, setDiffFilter] = useState('all');
  const [page, setPage] = useState(1);
  const perPage = 6;

  const filtered = questions.filter(q => {
    const ms = q.text.toLowerCase().includes(search.toLowerCase()) || q.topic.toLowerCase().includes(search.toLowerCase()) || q.id.includes(search);
    const mt = typeFilter === 'all' || q.type === typeFilter;
    const msub = subjectFilter === 'all' || q.subject === subjectFilter;
    const md = diffFilter === 'all' || q.difficulty === diffFilter;
    return ms && mt && msub && md;
  });

  const paginated = filtered.slice((page - 1) * perPage, page * perPage);
  const totalPages = Math.max(1, Math.ceil(filtered.length / perPage));
  const subjects = Array.from(new Set(questions.map(q => q.subject)));

  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Institute / Academic</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">Question Bank</h1>
          <div className="flex items-center gap-2">
            <button className="flex items-center gap-1.5 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 rounded text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
              <Filter className="w-3.5 h-3.5" />Import from Excel
            </button>
            <button className="flex items-center gap-1.5 px-3 py-2 text-white rounded text-sm" style={{ background: '#1d4ed8' }}>
              <Plus className="w-3.5 h-3.5" />Add Question
            </button>
          </div>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        {[
          { label: 'Total Questions', value: '48,240', color: 'text-slate-900 dark:text-white' },
          { label: 'MCQ', value: '24,120', color: 'text-blue-600 dark:text-blue-400' },
          { label: 'Descriptive', value: '11,840', color: 'text-violet-600 dark:text-violet-400' },
          { label: 'Coding', value: '4,820', color: 'text-emerald-600 dark:text-emerald-400' },
        ].map(({ label, value, color }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className={`text-2xl font-black ${color}`}>{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm mt-0.5">{label}</div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 flex-1 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded">
          <Search className="w-4 h-4 text-slate-400" />
          <input value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} placeholder="Search by question text, topic, ID..." className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 outline-none flex-1 text-sm" />
        </div>
        <select value={typeFilter} onChange={e => { setTypeFilter(e.target.value); setPage(1); }} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-900 dark:text-white outline-none text-sm cursor-pointer">
          {['all', 'MCQ', 'Descriptive', 'Coding', 'FillBlanks'].map(t => <option key={t} value={t}>{t === 'all' ? 'All Types' : typeConfig[t]?.label || t}</option>)}
        </select>
        <select value={subjectFilter} onChange={e => { setSubjectFilter(e.target.value); setPage(1); }} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-900 dark:text-white outline-none text-sm cursor-pointer">
          <option value="all">All Subjects</option>
          {subjects.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <select value={diffFilter} onChange={e => { setDiffFilter(e.target.value); setPage(1); }} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-900 dark:text-white outline-none text-sm cursor-pointer">
          {['all', 'Easy', 'Medium', 'Hard'].map(d => <option key={d} value={d}>{d === 'all' ? 'All Difficulty' : d}</option>)}
        </select>
      </div>

      {/* Questions List */}
      <div className="space-y-3">
        {paginated.map(q => {
          const tc = typeConfig[q.type];
          const TypeIcon = tc.icon;
          return (
            <div key={q.id} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5 hover:border-blue-300 dark:hover:border-blue-700 transition-colors">
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex flex-wrap items-center gap-2 mb-2">
                    <span className="font-mono text-blue-600 dark:text-blue-400 text-xs">{q.id}</span>
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${tc.bg} ${tc.color}`}>
                      <TypeIcon className="w-3 h-3" />{tc.label}
                    </span>
                    <span className={`text-xs font-semibold ${difficultyColor[q.difficulty]}`}>{q.difficulty}</span>
                    <span className="text-xs text-slate-400">•</span>
                    <span className="text-xs text-slate-500 dark:text-slate-400">{q.marks} marks</span>
                  </div>
                  <p className="text-slate-800 dark:text-slate-200 text-sm leading-relaxed">{q.text}</p>
                  <div className="flex flex-wrap items-center gap-3 mt-2.5">
                    <span className="flex items-center gap-1 text-xs text-slate-400"><Tag className="w-3 h-3" />{q.subject} — {q.topic}</span>
                    <span className="text-xs text-slate-400">Class {q.class}</span>
                    <span className="text-xs text-slate-400">Bloom: {q.bloom}</span>
                    <span className="text-xs text-slate-400">Used {q.usageCount}× in papers</span>
                    <span className="text-xs text-amber-500">★ {q.quality}</span>
                  </div>
                </div>
                <div className="flex items-center gap-1 flex-shrink-0">
                  <button className="p-1.5 text-slate-400 hover:text-blue-600 dark:hover:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-950/20 rounded transition-all"><Eye className="w-4 h-4" /></button>
                  <button className="p-1.5 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded transition-all"><Edit2 className="w-4 h-4" /></button>
                  <button className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/20 rounded transition-all"><Trash2 className="w-4 h-4" /></button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between">
        <span className="text-slate-500 dark:text-slate-400 text-sm">Showing {(page - 1) * perPage + 1}–{Math.min(page * perPage, filtered.length)} of {filtered.length} questions</span>
        <div className="flex items-center gap-1.5">
          <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="p-1.5 rounded border border-slate-200 dark:border-slate-700 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed"><ChevronLeft className="w-4 h-4" /></button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
            <button key={p} onClick={() => setPage(p)} className={`w-8 h-8 rounded border text-sm font-medium transition-colors ${p === page ? 'border-blue-600 bg-blue-600 text-white' : 'border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'}`}>{p}</button>
          ))}
          <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="p-1.5 rounded border border-slate-200 dark:border-slate-700 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed"><ChevronRight className="w-4 h-4" /></button>
        </div>
      </div>
    </div>
  );
}
