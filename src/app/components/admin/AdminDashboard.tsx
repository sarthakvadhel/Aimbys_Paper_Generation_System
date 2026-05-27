import { useState } from 'react';
import {
  BookOpen, Users, TrendingUp, Award, Upload, Plus,
  ArrowUpRight, ArrowDownRight, MoreHorizontal, Clock, CheckCircle2, AlertCircle
} from 'lucide-react';
import {
  AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend
} from 'recharts';
import { AppState } from '../../App';

const areaData = [
  { date: 'Jan', completions: 420, attempts: 650 },
  { date: 'Feb', completions: 580, attempts: 820 },
  { date: 'Mar', completions: 490, attempts: 730 },
  { date: 'Apr', completions: 720, attempts: 940 },
  { date: 'May', completions: 850, attempts: 1100 },
  { date: 'Jun', completions: 780, attempts: 1020 },
  { date: 'Jul', completions: 960, attempts: 1240 },
  { date: 'Aug', completions: 1100, attempts: 1350 },
  { date: 'Sep', completions: 1280, attempts: 1600 },
  { date: 'Oct', completions: 1450, attempts: 1780 },
  { date: 'Nov', completions: 1320, attempts: 1650 },
  { date: 'Dec', completions: 1680, attempts: 2050 },
];

const categoryData = [
  { name: 'JavaScript', score: 78, quizzes: 42 },
  { name: 'Python', score: 82, quizzes: 38 },
  { name: 'System Design', score: 71, quizzes: 29 },
  { name: 'Algorithms', score: 65, quizzes: 54 },
  { name: 'Databases', score: 74, quizzes: 31 },
  { name: 'DevOps', score: 69, quizzes: 22 },
];

const difficultyData = [
  { name: 'Easy', value: 35, color: '#10b981' },
  { name: 'Medium', value: 45, color: '#f59e0b' },
  { name: 'Hard', value: 20, color: '#ef4444' },
];

const recentActivity = [
  { type: 'upload', message: 'JavaScript Advanced Quiz uploaded (45 questions)', time: '2 min ago', status: 'success' },
  { type: 'user', message: 'New user batch registered: 124 students', time: '15 min ago', status: 'success' },
  { type: 'quiz', message: 'Algorithm Basics Quiz — 89 submissions', time: '1 hr ago', status: 'info' },
  { type: 'alert', message: 'Python Fundamentals — low engagement (32%)', time: '3 hr ago', status: 'warning' },
  { type: 'upload', message: 'System Design Q2 2026 uploaded', time: '5 hr ago', status: 'success' },
  { type: 'quiz', message: 'React Patterns Quiz activated', time: '8 hr ago', status: 'info' },
];

const kpis = [
  { label: 'Total Quizzes', value: '2,847', change: '+12.4%', up: true, icon: BookOpen, color: 'text-indigo-600 dark:text-indigo-400', bg: 'bg-indigo-50 dark:bg-indigo-950/40' },
  { label: 'Active Users', value: '50,341', change: '+8.7%', up: true, icon: Users, color: 'text-violet-600 dark:text-violet-400', bg: 'bg-violet-50 dark:bg-violet-950/40' },
  { label: 'Avg. Score', value: '74.2%', change: '+3.1%', up: true, icon: TrendingUp, color: 'text-cyan-600 dark:text-cyan-400', bg: 'bg-cyan-50 dark:bg-cyan-950/40' },
  { label: 'Completion Rate', value: '68.5%', change: '-1.2%', up: false, icon: Award, color: 'text-amber-600 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/40' },
];

const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload?.length) {
    return (
      <div className="bg-slate-900 border border-slate-700 rounded-xl p-3 shadow-xl">
        <div className="text-slate-400 text-sm mb-2">{label}</div>
        {payload.map((entry: any) => (
          <div key={entry.name} className="flex items-center gap-2 text-sm">
            <div className="w-2 h-2 rounded-full" style={{ background: entry.color }} />
            <span className="text-slate-300">{entry.name}:</span>
            <span className="text-white font-bold">{entry.value.toLocaleString()}</span>
          </div>
        ))}
      </div>
    );
  }
  return null;
};

export function AdminDashboard({ setView }: AppState) {
  const [period, setPeriod] = useState<'7d' | '30d' | '12m'>('12m');

  return (
    <div className="space-y-6 pb-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white">Dashboard</h1>
          <p className="text-slate-500 dark:text-slate-400">Welcome back, Admin. Here's what's happening.</p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => setView('admin-upload')}
            className="flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25"
          >
            <Upload className="w-4 h-4" />
            <span className="hidden sm:block">Upload Quiz</span>
          </button>
          <button
            onClick={() => setView('admin-quizzes')}
            className="flex items-center gap-2 px-4 py-2.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
          >
            <Plus className="w-4 h-4" />
            <span className="hidden sm:block">New Quiz</span>
          </button>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        {kpis.map(({ label, value, change, up, icon: Icon, color, bg }) => (
          <div key={label} className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between mb-4">
              <div className={`w-10 h-10 ${bg} rounded-xl flex items-center justify-center`}>
                <Icon className={`w-5 h-5 ${color}`} />
              </div>
              <span className={`flex items-center gap-1 text-sm font-semibold ${up ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'}`}>
                {up ? <ArrowUpRight className="w-4 h-4" /> : <ArrowDownRight className="w-4 h-4" />}
                {change}
              </span>
            </div>
            <div className="text-3xl font-black text-slate-900 dark:text-white mb-1">{value}</div>
            <div className="text-slate-500 dark:text-slate-400">{label}</div>
          </div>
        ))}
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* Area Chart */}
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="font-bold text-slate-900 dark:text-white">Quiz Activity</h2>
              <p className="text-slate-500 dark:text-slate-400 text-sm">Completions vs attempts over time</p>
            </div>
            <div className="flex bg-slate-100 dark:bg-slate-800 rounded-xl p-1">
              {(['7d', '30d', '12m'] as const).map(p => (
                <button
                  key={p}
                  onClick={() => setPeriod(p)}
                  className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-all ${
                    period === p ? 'bg-white dark:bg-slate-700 text-slate-900 dark:text-white shadow' : 'text-slate-500 dark:text-slate-400'
                  }`}
                >
                  {p}
                </button>
              ))}
            </div>
          </div>
          <ResponsiveContainer width="100%" height={240}>
            <AreaChart data={areaData}>
              <defs>
                <linearGradient id="completionsGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="attemptsGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.2} />
                  <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
              <XAxis dataKey="date" tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <Tooltip content={<CustomTooltip />} />
              <Area type="monotone" dataKey="attempts" stroke="#8b5cf6" strokeWidth={2} fill="url(#attemptsGrad)" name="Attempts" />
              <Area type="monotone" dataKey="completions" stroke="#6366f1" strokeWidth={2} fill="url(#completionsGrad)" name="Completions" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Pie Chart */}
        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="mb-6">
            <h2 className="font-bold text-slate-900 dark:text-white">Difficulty Split</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm">Questions by difficulty level</p>
          </div>
          <ResponsiveContainer width="100%" height={180}>
            <PieChart>
              <Pie data={difficultyData} cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={3} dataKey="value">
                {difficultyData.map((entry, index) => (
                  <Cell key={index} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip formatter={(value) => [`${value}%`, 'Share']} />
            </PieChart>
          </ResponsiveContainer>
          <div className="space-y-2">
            {difficultyData.map(({ name, value, color }) => (
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

      {/* Bottom Row */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* Category Bar Chart */}
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="font-bold text-slate-900 dark:text-white">Category Performance</h2>
              <p className="text-slate-500 dark:text-slate-400 text-sm">Average score per subject</p>
            </div>
            <button className="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200">
              <MoreHorizontal className="w-4 h-4" />
            </button>
          </div>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={categoryData} barSize={32}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
              <XAxis dataKey="name" tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} domain={[0, 100]} />
              <Tooltip content={<CustomTooltip />} />
              <Bar dataKey="score" name="Avg Score %" fill="url(#barGrad)" radius={[6, 6, 0, 0]} />
              <defs>
                <linearGradient id="barGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#6366f1" />
                  <stop offset="100%" stopColor="#8b5cf6" />
                </linearGradient>
              </defs>
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Activity Feed */}
        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <h2 className="font-bold text-slate-900 dark:text-white mb-4">Recent Activity</h2>
          <div className="space-y-3">
            {recentActivity.map(({ message, time, status }, idx) => (
              <div key={idx} className="flex gap-3">
                <div className={`w-8 h-8 rounded-xl flex-shrink-0 flex items-center justify-center ${
                  status === 'success' ? 'bg-emerald-100 dark:bg-emerald-950/40' :
                  status === 'warning' ? 'bg-amber-100 dark:bg-amber-950/40' :
                  'bg-indigo-100 dark:bg-indigo-950/40'
                }`}>
                  {status === 'success' ? <CheckCircle2 className="w-4 h-4 text-emerald-600 dark:text-emerald-400" /> :
                   status === 'warning' ? <AlertCircle className="w-4 h-4 text-amber-600 dark:text-amber-400" /> :
                   <Clock className="w-4 h-4 text-indigo-600 dark:text-indigo-400" />}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-slate-700 dark:text-slate-300 text-sm leading-snug">{message}</p>
                  <p className="text-slate-400 dark:text-slate-600 text-xs mt-0.5">{time}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
