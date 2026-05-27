import { FileText, Clock, CheckCircle, AlertCircle, TrendingUp } from 'lucide-react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { AimbysState } from '../../../App';

const recentPapers = [
  { id: 'P-2024-0841', title: 'Mathematics XII — Unit Test 3', created: '2026-05-20', status: 'approved', questions: 30, marks: 80 },
  { id: 'P-2024-0832', title: 'Mathematics XI — Trigonometry Mid-term', created: '2026-05-18', status: 'pending', questions: 40, marks: 100 },
  { id: 'P-2024-0820', title: 'Mathematics X — Algebra Chapter Test', created: '2026-05-15', status: 'approved', questions: 25, marks: 60 },
  { id: 'P-2024-0811', title: 'Mathematics XII — Calculus Final Exam', created: '2026-05-10', status: 'draft', questions: 50, marks: 120 },
];

const pendingEvals = [
  { student: 'Riya Patel', exam: 'Math XII Unit Test 3', submitted: '2026-05-21 10:00', questions: 3, type: 'Descriptive' },
  { student: 'Arjun Rao', exam: 'Math XII Unit Test 3', submitted: '2026-05-21 10:05', questions: 3, type: 'Descriptive' },
  { student: 'Meera Joshi', exam: 'Math XI Trigonometry', submitted: '2026-05-20 14:30', questions: 4, type: 'Descriptive' },
];

const classData = [
  { cls: 'XII-A', avg: 74 }, { cls: 'XII-B', avg: 68 }, { cls: 'XI-A', avg: 71 },
  { cls: 'XI-B', avg: 65 }, { cls: 'X-C', avg: 78 },
];

const statusBadge = (s: string) => {
  if (s === 'approved') return 'bg-green-50 dark:bg-green-950/30 text-green-700 dark:text-green-400';
  if (s === 'pending') return 'bg-amber-50 dark:bg-amber-950/30 text-amber-700 dark:text-amber-400';
  return 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400';
};

export function TeacherDashboard(props: AimbysState) {
  const { setView } = props;
  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Teacher / Dashboard</div>
        <h1 className="text-slate-900 dark:text-white font-bold text-xl">Welcome back, Mr. Rajiv Kumar</h1>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Papers Created', value: '84', sub: 'This year', icon: FileText, color: '#0369a1' },
          { label: 'Pending Approval', value: '3', sub: 'Awaiting review', icon: Clock, color: '#d97706' },
          { label: 'Pending Evaluations', value: '14', sub: 'Descriptive answers', icon: AlertCircle, color: '#dc2626' },
          { label: 'Avg Class Score', value: '71.2%', sub: '↑ 2.8% vs last term', icon: TrendingUp, color: '#15803d' },
        ].map(({ label, value, sub, icon: Icon, color }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <div className="w-8 h-8 rounded flex items-center justify-center" style={{ background: `${color}15` }}>
                <Icon className="w-4 h-4" style={{ color }} />
              </div>
            </div>
            <div className="text-2xl font-black text-slate-900 dark:text-white">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm font-medium mt-0.5">{label}</div>
            <div className="text-slate-400 text-xs mt-1">{sub}</div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        {/* Class Avg Bar Chart */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Class Avg Scores</h2>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={classData} barSize={28}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="cls" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} domain={[50, 85]} tickFormatter={v => `${v}%`} />
              <Tooltip formatter={(v: any) => [`${v}%`, 'Avg Score']} />
              <Bar dataKey="avg" fill="#0369a1" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Pending Evaluations */}
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
            <h2 className="text-slate-900 dark:text-white font-semibold">Pending Evaluations</h2>
            <button onClick={() => setView('tchr-evaluation')} className="text-sky-600 dark:text-sky-400 text-sm hover:underline">Open Desk →</button>
          </div>
          <div className="divide-y divide-slate-100 dark:divide-slate-800">
            {pendingEvals.map((e, i) => (
              <div key={i} className="px-5 py-3.5 flex items-center justify-between hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <div>
                  <div className="text-slate-900 dark:text-slate-100 font-medium text-sm">{e.student}</div>
                  <div className="text-slate-500 dark:text-slate-400 text-xs mt-0.5">{e.exam} · {e.questions} descriptive answers · {e.submitted}</div>
                </div>
                <button onClick={() => setView('tchr-evaluation')} className="flex items-center gap-1.5 px-3 py-1.5 text-white rounded text-xs font-medium flex-shrink-0" style={{ background: '#0369a1' }}>
                  <CheckCircle className="w-3.5 h-3.5" />Evaluate
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Recent Papers */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <h2 className="text-slate-900 dark:text-white font-semibold">Recent Papers</h2>
          <button onClick={() => setView('tchr-papergen')} className="text-sky-600 dark:text-sky-400 text-sm hover:underline">View all →</button>
        </div>
        <table className="w-full">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>{['Paper ID', 'Title', 'Created', 'Questions', 'Total Marks', 'Status'].map(h => (
              <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider whitespace-nowrap">{h}</th>
            ))}</tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {recentPapers.map(p => (
              <tr key={p.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <td className="px-5 py-3.5 font-mono text-sky-600 dark:text-sky-400 text-xs">{p.id}</td>
                <td className="px-5 py-3.5 text-slate-900 dark:text-slate-100 font-medium text-sm">{p.title}</td>
                <td className="px-5 py-3.5 text-slate-500 dark:text-slate-400 text-sm">{p.created}</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{p.questions} Qs</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{p.marks}</td>
                <td className="px-5 py-3.5">
                  <span className={`px-2.5 py-1 rounded text-xs font-semibold capitalize ${statusBadge(p.status)}`}>{p.status}</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
