import { FileText, Users, BookOpen, Calendar, TrendingUp, TrendingDown, AlertCircle, CheckCircle, Clock } from 'lucide-react';
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { AimbysState } from '../../../App';

const activityData = [
  { day: 'Mon', papers: 24, exams: 8 },
  { day: 'Tue', papers: 31, exams: 12 },
  { day: 'Wed', papers: 18, exams: 6 },
  { day: 'Thu', papers: 42, exams: 14 },
  { day: 'Fri', papers: 38, exams: 11 },
  { day: 'Sat', papers: 15, exams: 4 },
  { day: 'Sun', papers: 8, exams: 2 },
];

const subjectData = [
  { subject: 'Maths', avg: 64, pass: 78 },
  { subject: 'Physics', avg: 68, pass: 81 },
  { subject: 'Chemistry', avg: 71, pass: 84 },
  { subject: 'English', avg: 82, pass: 92 },
  { subject: 'Biology', avg: 74, pass: 87 },
  { subject: 'CS', avg: 78, pass: 89 },
];

const alerts = [
  { type: 'warning', message: 'Paper #P-2024-0841 awaiting approval since 48h', time: '2h ago' },
  { type: 'info', message: 'Exam Calendar updated: Mathematics XII on 22 May', time: '4h ago' },
  { type: 'critical', message: 'Evaluation Engine degraded — results may be delayed', time: '6h ago' },
  { type: 'success', message: 'Biology XI results published — 310 students', time: '1d ago' },
];

const teachers = [
  { name: 'Mr. Rajiv Kumar', subject: 'Mathematics', papers: 84, rating: 4.8 },
  { name: 'Mrs. Priya Nair', subject: 'Physics', papers: 62, rating: 4.6 },
  { name: 'Dr. Sunita Roy', subject: 'Chemistry', papers: 71, rating: 4.9 },
  { name: 'Mr. Arun Mehta', subject: 'Computer Sci.', papers: 48, rating: 4.5 },
];

export function InstituteDashboard(props: AimbysState) {
  const { setView } = props;

  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Institute Admin / Dashboard</div>
        <h1 className="text-slate-900 dark:text-white font-bold text-xl">Delhi Public School HQ — Overview</h1>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Total Students', value: '28,400', change: '+840', up: true, icon: Users, color: '#1d4ed8' },
          { label: 'Papers Generated', value: '6,820', change: '+124 this month', up: true, icon: FileText, color: '#0369a1' },
          { label: 'Question Bank', value: '48,240', change: '+1,820 this month', up: true, icon: BookOpen, color: '#7c3aed' },
          { label: 'Exams Scheduled', value: '18', change: '4 this week', up: true, icon: Calendar, color: '#15803d' },
        ].map(({ label, value, change, up, icon: Icon, color }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <div className="w-8 h-8 rounded flex items-center justify-center" style={{ background: `${color}15` }}>
                <Icon className="w-4 h-4" style={{ color }} />
              </div>
              <span className={`flex items-center gap-1 text-xs font-semibold ${up ? 'text-green-600 dark:text-green-400' : 'text-red-500'}`}>
                {up ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
              </span>
            </div>
            <div className="text-2xl font-black text-slate-900 dark:text-white">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm font-medium mt-0.5">{label}</div>
            <div className="text-slate-400 text-xs mt-1">{change}</div>
          </div>
        ))}
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Weekly Activity — Papers & Exams</h2>
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={activityData}>
              <defs>
                <linearGradient id="gPapers" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#1d4ed8" stopOpacity={0.25} />
                  <stop offset="100%" stopColor="#1d4ed8" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="gExams" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#15803d" stopOpacity={0.2} />
                  <stop offset="100%" stopColor="#15803d" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="day" tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
              <Tooltip />
              <Area type="monotone" dataKey="papers" stroke="#1d4ed8" strokeWidth={2} fill="url(#gPapers)" name="Papers" />
              <Area type="monotone" dataKey="exams" stroke="#15803d" strokeWidth={2} fill="url(#gExams)" name="Exams" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Alerts */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Recent Alerts</h2>
          <div className="space-y-3">
            {alerts.map((a, i) => {
              const Icon = a.type === 'critical' ? AlertCircle : a.type === 'warning' ? AlertCircle : a.type === 'success' ? CheckCircle : Clock;
              const cls = a.type === 'critical' ? 'text-red-500' : a.type === 'warning' ? 'text-amber-500' : a.type === 'success' ? 'text-green-500' : 'text-blue-500';
              return (
                <div key={i} className="flex gap-3">
                  <Icon className={`w-4 h-4 mt-0.5 flex-shrink-0 ${cls}`} />
                  <div>
                    <p className="text-slate-700 dark:text-slate-300 text-sm">{a.message}</p>
                    <p className="text-slate-400 text-xs mt-0.5">{a.time}</p>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Subject Performance */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
        <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Subject-wise Performance</h2>
        <ResponsiveContainer width="100%" height={200}>
          <BarChart data={subjectData} barGap={6}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
            <XAxis dataKey="subject" tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} domain={[0, 100]} tickFormatter={v => `${v}%`} />
            <Tooltip formatter={(v: any) => [`${v}%`]} />
            <Bar dataKey="avg" name="Avg Score" fill="#1d4ed8" radius={[3, 3, 0, 0]} opacity={0.85} />
            <Bar dataKey="pass" name="Pass Rate" fill="#15803d" radius={[3, 3, 0, 0]} opacity={0.85} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Top Teachers */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <h2 className="text-slate-900 dark:text-white font-semibold">Top Paper Contributors</h2>
          <button onClick={() => setView('inst-users')} className="text-blue-600 dark:text-blue-400 text-sm hover:underline">View all →</button>
        </div>
        <table className="w-full">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>{['Teacher', 'Subject', 'Papers Created', 'Avg Rating'].map(h => <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider">{h}</th>)}</tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {teachers.map(t => (
              <tr key={t.name} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <td className="px-5 py-3.5">
                  <div className="flex items-center gap-2.5">
                    <div className="w-7 h-7 rounded-full bg-blue-100 dark:bg-blue-950/40 flex items-center justify-center text-blue-700 dark:text-blue-400 text-xs font-bold">{t.name.split(' ').map(n => n[0]).join('').slice(0, 2)}</div>
                    <span className="text-slate-900 dark:text-slate-100 font-medium text-sm">{t.name}</span>
                  </div>
                </td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{t.subject}</td>
                <td className="px-5 py-3.5 text-slate-700 dark:text-slate-300 font-semibold text-sm">{t.papers}</td>
                <td className="px-5 py-3.5">
                  <div className="flex items-center gap-1.5">
                    <span className="text-amber-500 text-sm">★</span>
                    <span className="text-slate-700 dark:text-slate-300 text-sm font-semibold">{t.rating}</span>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
