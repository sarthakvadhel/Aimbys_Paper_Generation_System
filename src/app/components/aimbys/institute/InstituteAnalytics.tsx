import { Download, TrendingUp } from 'lucide-react';
import { AreaChart, Area, BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { AimbysState } from '../../../App';

const monthlyData = [
  { month: 'Dec', papers: 280, exams: 42, pass: 74 },
  { month: 'Jan', papers: 320, exams: 58, pass: 76 },
  { month: 'Feb', papers: 290, exams: 51, pass: 72 },
  { month: 'Mar', papers: 410, exams: 74, pass: 78 },
  { month: 'Apr', papers: 380, exams: 68, pass: 75 },
  { month: 'May', papers: 440, exams: 82, pass: 81 },
];

const classPerformance = [
  { cls: 'Class X', avg: 72, pass: 88, students: 4200 },
  { cls: 'Class XI', avg: 68, pass: 82, students: 3800 },
  { cls: 'Class XII', avg: 74, pass: 84, students: 3600 },
  { cls: 'Class IX', avg: 76, pass: 91, students: 4100 },
  { cls: 'Class VIII', avg: 79, pass: 93, students: 3900 },
];

const subjectTrend = [
  { month: 'Jan', maths: 62, physics: 66, chemistry: 70, english: 81 },
  { month: 'Feb', maths: 60, physics: 64, chemistry: 68, english: 80 },
  { month: 'Mar', maths: 65, physics: 69, chemistry: 73, english: 83 },
  { month: 'Apr', maths: 63, physics: 67, chemistry: 71, english: 81 },
  { month: 'May', maths: 67, physics: 71, chemistry: 74, english: 85 },
];

export function InstituteAnalytics(_props: AimbysState) {
  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Institute / Analytics</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">Institute Analytics</h1>
          <button className="flex items-center gap-1.5 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 rounded text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
            <Download className="w-3.5 h-3.5" />Export Report
          </button>
        </div>
      </div>

      {/* KPI Row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Overall Pass Rate', value: '79.4%', change: '+4.2%', up: true },
          { label: 'Avg Score (All)', value: '71.8%', change: '+2.8%', up: true },
          { label: 'Papers Generated', value: '2,120', change: 'This academic year', up: true },
          { label: 'Exams Conducted', value: '375', change: 'This academic year', up: true },
        ].map(({ label, value, change, up }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className="text-2xl font-black text-slate-900 dark:text-white mb-0.5">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm mb-1">{label}</div>
            <span className={`flex items-center gap-1 text-xs font-semibold ${up ? 'text-green-600 dark:text-green-400' : 'text-red-500'}`}>
              <TrendingUp className="w-3 h-3" />{change}
            </span>
          </div>
        ))}
      </div>

      {/* Monthly Activity */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
        <h2 className="text-slate-900 dark:text-white font-semibold mb-1">Monthly Activity Trend</h2>
        <p className="text-slate-400 text-xs mb-4">Papers generated and exams conducted over last 6 months</p>
        <ResponsiveContainer width="100%" height={220}>
          <AreaChart data={monthlyData}>
            <defs>
              <linearGradient id="iGradPapers" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#1d4ed8" stopOpacity={0.25} />
                <stop offset="100%" stopColor="#1d4ed8" stopOpacity={0} />
              </linearGradient>
              <linearGradient id="iGradExams" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#7c3aed" stopOpacity={0.2} />
                <stop offset="100%" stopColor="#7c3aed" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
            <XAxis dataKey="month" tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
            <Tooltip />
            <Area type="monotone" dataKey="papers" stroke="#1d4ed8" strokeWidth={2} fill="url(#iGradPapers)" name="Papers" />
            <Area type="monotone" dataKey="exams" stroke="#7c3aed" strokeWidth={2} fill="url(#iGradExams)" name="Exams" />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
        {/* Class-wise Performance */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Class-wise Performance</h2>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={classPerformance} barGap={4}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="cls" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} domain={[0, 100]} tickFormatter={v => `${v}%`} />
              <Tooltip formatter={(v: any) => [`${v}%`]} />
              <Bar dataKey="avg" name="Avg Score" fill="#1d4ed8" radius={[3, 3, 0, 0]} opacity={0.85} />
              <Bar dataKey="pass" name="Pass Rate" fill="#15803d" radius={[3, 3, 0, 0]} opacity={0.85} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Subject Score Trend */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Subject Avg Score Trend</h2>
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={subjectTrend}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="month" tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} domain={[55, 90]} tickFormatter={v => `${v}%`} />
              <Tooltip formatter={(v: any) => [`${v}%`]} />
              <Line type="monotone" dataKey="maths" stroke="#1d4ed8" strokeWidth={2} dot={false} name="Mathematics" />
              <Line type="monotone" dataKey="physics" stroke="#0369a1" strokeWidth={2} dot={false} name="Physics" />
              <Line type="monotone" dataKey="chemistry" stroke="#7c3aed" strokeWidth={2} dot={false} name="Chemistry" />
              <Line type="monotone" dataKey="english" stroke="#15803d" strokeWidth={2} dot={false} name="English" />
            </LineChart>
          </ResponsiveContainer>
          <div className="flex flex-wrap gap-3 mt-3">
            {[['Mathematics', '#1d4ed8'], ['Physics', '#0369a1'], ['Chemistry', '#7c3aed'], ['English', '#15803d']].map(([name, color]) => (
              <div key={name} className="flex items-center gap-1.5">
                <div className="w-3 h-0.5 rounded" style={{ background: color }} />
                <span className="text-slate-500 dark:text-slate-400 text-xs">{name}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Class Details Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
          <h2 className="text-slate-900 dark:text-white font-semibold">Class Performance Summary</h2>
        </div>
        <table className="w-full">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>{['Class', 'Students', 'Avg Score', 'Pass Rate', 'Performance'].map(h => (
              <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider">{h}</th>
            ))}</tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {classPerformance.map(c => (
              <tr key={c.cls} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <td className="px-5 py-3.5 text-slate-900 dark:text-slate-100 font-semibold text-sm">{c.cls}</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{c.students.toLocaleString()}</td>
                <td className="px-5 py-3.5 text-slate-700 dark:text-slate-300 font-semibold text-sm">{c.avg}%</td>
                <td className="px-5 py-3.5">
                  <span className={`text-sm font-semibold ${c.pass >= 90 ? 'text-green-600 dark:text-green-400' : c.pass >= 80 ? 'text-blue-600 dark:text-blue-400' : 'text-amber-600 dark:text-amber-400'}`}>{c.pass}%</span>
                </td>
                <td className="px-5 py-3.5">
                  <div className="flex items-center gap-2">
                    <div className="flex-1 h-1.5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden max-w-24">
                      <div className="h-full rounded-full" style={{ width: `${c.pass}%`, background: '#1d4ed8' }} />
                    </div>
                    <span className="text-slate-400 text-xs">{c.pass >= 90 ? 'Excellent' : c.pass >= 80 ? 'Good' : 'Average'}</span>
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
