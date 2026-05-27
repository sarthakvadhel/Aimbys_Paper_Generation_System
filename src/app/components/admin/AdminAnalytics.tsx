import { useState } from 'react';
import { Download, TrendingUp, TrendingDown, Users, Clock, Target, BarChart2 } from 'lucide-react';
import {
  AreaChart, Area, LineChart, Line, BarChart, Bar, RadarChart, Radar,
  PolarGrid, PolarAngleAxis, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Cell, PieChart, Pie, Legend
} from 'recharts';
import { AppState } from '../../App';

const weeklyData = [
  { day: 'Mon', users: 1240, completions: 890, newUsers: 42 },
  { day: 'Tue', users: 1680, completions: 1120, newUsers: 61 },
  { day: 'Wed', users: 1420, completions: 980, newUsers: 38 },
  { day: 'Thu', users: 1890, completions: 1340, newUsers: 74 },
  { day: 'Fri', users: 2100, completions: 1560, newUsers: 89 },
  { day: 'Sat', users: 980, completions: 720, newUsers: 28 },
  { day: 'Sun', users: 760, completions: 540, newUsers: 19 },
];

const retentionData = [
  { week: 'W1', rate: 100 }, { week: 'W2', rate: 68 }, { week: 'W3', rate: 54 },
  { week: 'W4', rate: 48 }, { week: 'W5', rate: 43 }, { week: 'W6', rate: 40 },
  { week: 'W7', rate: 38 }, { week: 'W8', rate: 36 },
];

const scoreDistData = [
  { range: '0-20', count: 42 }, { range: '21-40', count: 118 }, { range: '41-60', count: 285 },
  { range: '61-80', count: 620 }, { range: '81-100', count: 435 },
];

const categoryPerfData = [
  { subject: 'JavaScript', score: 78, fullMark: 100 },
  { subject: 'Python', score: 82, fullMark: 100 },
  { subject: 'System Design', score: 71, fullMark: 100 },
  { subject: 'Algorithms', score: 65, fullMark: 100 },
  { subject: 'Databases', score: 74, fullMark: 100 },
  { subject: 'DevOps', score: 69, fullMark: 100 },
];

const deviceData = [
  { name: 'Desktop', value: 48, color: '#6366f1' },
  { name: 'Mobile', value: 38, color: '#8b5cf6' },
  { name: 'Tablet', value: 14, color: '#06b6d4' },
];

const topQuizzes = [
  { title: 'Python Data Structures', attempts: 2100, avgScore: 81, completion: 89 },
  { title: 'CSS Grid & Flexbox', attempts: 1580, avgScore: 84, completion: 94 },
  { title: 'React Fundamentals', attempts: 1240, avgScore: 74, completion: 77 },
  { title: 'SQL Optimization', attempts: 890, avgScore: 79, completion: 82 },
  { title: 'Advanced JavaScript', attempts: 980, avgScore: 68, completion: 71 },
];

const CustomTooltip = ({ active, payload, label }: any) => {
  if (!active || !payload?.length) return null;
  return (
    <div className="bg-slate-900 border border-slate-700 rounded-xl p-3 shadow-xl">
      <div className="text-slate-400 text-sm mb-2">{label}</div>
      {payload.map((entry: any) => (
        <div key={entry.name} className="flex items-center gap-2 text-sm">
          <div className="w-2 h-2 rounded-full" style={{ background: entry.color || entry.fill }} />
          <span className="text-slate-300">{entry.name}:</span>
          <span className="text-white font-bold">{entry.value.toLocaleString()}</span>
        </div>
      ))}
    </div>
  );
};

export function AdminAnalytics(_props: AppState) {
  const [range, setRange] = useState<'7d' | '30d' | '3m' | '1y'>('7d');

  return (
    <div className="space-y-6 pb-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white">Analytics</h1>
          <p className="text-slate-500 dark:text-slate-400">Platform-wide performance insights</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex bg-slate-100 dark:bg-slate-800 rounded-xl p-1">
            {(['7d', '30d', '3m', '1y'] as const).map(r => (
              <button key={r} onClick={() => setRange(r)} className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-all ${range === r ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-white shadow' : 'text-slate-500 dark:text-slate-400'}`}>{r}</button>
            ))}
          </div>
          <button className="flex items-center gap-2 px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
            <Download className="w-4 h-4" />
            Export
          </button>
        </div>
      </div>

      {/* KPI Summary */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: 'Total Attempts', value: '9,470', change: '+18.2%', up: true, icon: Target },
          { label: 'Unique Learners', value: '6,284', change: '+12.4%', up: true, icon: Users },
          { label: 'Avg Time/Quiz', value: '18.4 min', change: '-1.2%', up: false, icon: Clock },
          { label: 'Avg Score', value: '74.6%', change: '+3.1%', up: true, icon: BarChart2 },
        ].map(({ label, value, change, up, icon: Icon }) => (
          <div key={label} className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5">
            <div className="flex items-center justify-between mb-3">
              <Icon className="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
              <span className={`text-sm font-semibold flex items-center gap-1 ${up ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'}`}>
                {up ? <TrendingUp className="w-3.5 h-3.5" /> : <TrendingDown className="w-3.5 h-3.5" />}
                {change}
              </span>
            </div>
            <div className="text-2xl font-black text-slate-900 dark:text-white">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm mt-0.5">{label}</div>
          </div>
        ))}
      </div>

      {/* Daily Activity + Retention */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="mb-4">
            <h2 className="font-bold text-slate-900 dark:text-white">Daily Activity</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm">Active users and completions per day</p>
          </div>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={weeklyData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
              <XAxis dataKey="day" tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <Tooltip content={<CustomTooltip />} />
              <Bar dataKey="users" name="Active Users" fill="#6366f1" radius={[4, 4, 0, 0]} opacity={0.8} />
              <Bar dataKey="completions" name="Completions" fill="#8b5cf6" radius={[4, 4, 0, 0]} opacity={0.8} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="mb-4">
            <h2 className="font-bold text-slate-900 dark:text-white">Device Breakdown</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm">How users access the platform</p>
          </div>
          <ResponsiveContainer width="100%" height={180}>
            <PieChart>
              <Pie data={deviceData} cx="50%" cy="50%" outerRadius={75} dataKey="value" paddingAngle={3}>
                {deviceData.map((entry, i) => <Cell key={i} fill={entry.color} />)}
              </Pie>
              <Tooltip formatter={(v) => [`${v}%`]} />
            </PieChart>
          </ResponsiveContainer>
          <div className="space-y-2">
            {deviceData.map(({ name, value, color }) => (
              <div key={name} className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full" style={{ background: color }} />
                  <span className="text-slate-700 dark:text-slate-300 text-sm">{name}</span>
                </div>
                <span className="text-slate-900 dark:text-white font-bold text-sm">{value}%</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Retention + Score Distribution */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="mb-4">
            <h2 className="font-bold text-slate-900 dark:text-white">User Retention</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm">% of users still active by week</p>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={retentionData}>
              <defs>
                <linearGradient id="retGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#06b6d4" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#06b6d4" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
              <XAxis dataKey="week" tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} domain={[0, 100]} tickFormatter={v => `${v}%`} />
              <Tooltip formatter={(v) => [`${v}%`, 'Retention']} />
              <Area type="monotone" dataKey="rate" stroke="#06b6d4" strokeWidth={2} fill="url(#retGrad)" name="Retention %" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="mb-4">
            <h2 className="font-bold text-slate-900 dark:text-white">Score Distribution</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm">Number of submissions by score range</p>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={scoreDistData} barSize={36}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
              <XAxis dataKey="range" tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <Tooltip content={<CustomTooltip />} />
              <Bar dataKey="count" name="Submissions" radius={[6, 6, 0, 0]}>
                {scoreDistData.map((entry, i) => (
                  <Cell key={i} fill={['#ef4444', '#f97316', '#f59e0b', '#22c55e', '#10b981'][i]} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Category Radar + Top Quizzes */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="mb-4">
            <h2 className="font-bold text-slate-900 dark:text-white">Category Radar</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm">Average score by subject area</p>
          </div>
          <ResponsiveContainer width="100%" height={250}>
            <RadarChart data={categoryPerfData}>
              <PolarGrid stroke="#334155" />
              <PolarAngleAxis dataKey="subject" tick={{ fill: '#64748b', fontSize: 11 }} />
              <Radar name="Avg Score" dataKey="score" stroke="#6366f1" fill="#6366f1" fillOpacity={0.3} strokeWidth={2} />
              <Tooltip formatter={(v) => [`${v}%`, 'Avg Score']} />
            </RadarChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="mb-4">
            <h2 className="font-bold text-slate-900 dark:text-white">Top Performing Quizzes</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm">By number of attempts</p>
          </div>
          <div className="space-y-3">
            {topQuizzes.map((q, idx) => (
              <div key={q.title} className="flex items-center gap-3">
                <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-black flex-shrink-0 ${
                  idx === 0 ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400' :
                  idx === 1 ? 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300' :
                  idx === 2 ? 'bg-orange-100 dark:bg-orange-950/40 text-orange-700 dark:text-orange-400' :
                  'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-500'
                }`}>{idx + 1}</span>
                <div className="flex-1 min-w-0">
                  <div className="text-slate-900 dark:text-slate-100 font-medium text-sm truncate">{q.title}</div>
                  <div className="flex items-center gap-3 mt-1">
                    <div className="flex-1 h-1.5 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                      <div className="h-full bg-indigo-500 rounded-full" style={{ width: `${q.completion}%` }} />
                    </div>
                    <span className="text-slate-500 dark:text-slate-400 text-xs whitespace-nowrap">{q.completion}% done</span>
                  </div>
                </div>
                <div className="text-right flex-shrink-0">
                  <div className="text-slate-900 dark:text-white font-bold text-sm">{q.avgScore}%</div>
                  <div className="text-slate-400 dark:text-slate-500 text-xs">{q.attempts.toLocaleString()} tries</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
