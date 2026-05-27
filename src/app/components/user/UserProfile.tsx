import { useState } from 'react';
import { Trophy, Flame, Zap, BookOpen, Target, Star, Edit2, Settings, Calendar, TrendingUp, Award } from 'lucide-react';
import { RadarChart, Radar, PolarGrid, PolarAngleAxis, ResponsiveContainer, Tooltip, AreaChart, Area, XAxis, YAxis, CartesianGrid } from 'recharts';
import { AppState } from '../../App';

const radarData = [
  { subject: 'JavaScript', score: 88, fullMark: 100 },
  { subject: 'React', score: 82, fullMark: 100 },
  { subject: 'Python', score: 75, fullMark: 100 },
  { subject: 'Algorithms', score: 71, fullMark: 100 },
  { subject: 'Databases', score: 79, fullMark: 100 },
  { subject: 'DevOps', score: 65, fullMark: 100 },
];

const progressHistory = [
  { month: 'Nov', xp: 4800 }, { month: 'Dec', xp: 5600 }, { month: 'Jan', xp: 6200 },
  { month: 'Feb', xp: 6900 }, { month: 'Mar', xp: 7400 }, { month: 'Apr', xp: 7900 },
  { month: 'May', xp: 8240 },
];

const badges = [
  { id: 1, name: 'Speed Demon', desc: 'Finished 5 quizzes under 10 min', icon: '⚡', earned: true, date: 'May 10' },
  { id: 2, name: 'Perfect Score', desc: 'Scored 100% on any quiz', icon: '💯', earned: true, date: 'Apr 28' },
  { id: 3, name: 'Streak Master', desc: 'Maintained a 14-day streak', icon: '🔥', earned: true, date: 'May 15' },
  { id: 4, name: 'Knowledge Seeker', desc: 'Completed 50 quizzes', icon: '📚', earned: true, date: 'Mar 22' },
  { id: 5, name: 'Category King', desc: 'Top 10 in any category', icon: '👑', earned: false, date: null },
  { id: 6, name: 'Social Butterfly', desc: 'Invited 5 friends', icon: '🦋', earned: false, date: null },
  { id: 7, name: 'Night Owl', desc: 'Completed 10 late-night quizzes', icon: '🦉', earned: true, date: 'Feb 14' },
  { id: 8, name: 'Comeback Kid', desc: 'Improved score by 30% on retry', icon: '🎯', earned: false, date: null },
];

const recentActivity = [
  { quiz: 'Advanced JavaScript', score: 82, date: '2 days ago', time: '18m 22s' },
  { quiz: 'System Design Basics', score: 64, date: '5 days ago', time: '24m 10s' },
  { quiz: 'SQL Fundamentals', score: 91, date: '1 week ago', time: '15m 45s' },
  { quiz: 'React Fundamentals', score: 78, date: '2 weeks ago', time: '20m 03s' },
  { quiz: 'CSS Grid & Flexbox', score: 95, date: '2 weeks ago', time: '12m 18s' },
];

const levelThresholds = [0, 500, 1200, 2000, 3000, 4500, 6000, 8000, 10500, 13500, 17000];
const currentLevel = 5;
const currentXP = 8240;
const nextLevelXP = levelThresholds[currentLevel + 1] || 20000;
const prevLevelXP = levelThresholds[currentLevel];
const levelPct = ((currentXP - prevLevelXP) / (nextLevelXP - prevLevelXP)) * 100;

export function UserProfile({ setView }: AppState) {
  const [tab, setTab] = useState<'overview' | 'badges' | 'history'>('overview');

  return (
    <div className="space-y-6 pb-8">
      {/* Profile Header */}
      <div className="relative bg-gradient-to-br from-indigo-600 via-violet-600 to-purple-700 rounded-3xl overflow-hidden">
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_80%_20%,rgba(255,255,255,0.1),transparent)]" />
        <div className="relative p-6 md:p-8">
          <div className="flex items-start justify-between mb-4">
            <div className="flex items-center gap-4">
              <div className="relative">
                <div className="w-20 h-20 rounded-2xl bg-white/20 backdrop-blur border-4 border-white/30 flex items-center justify-center text-white font-black text-2xl">
                  AJ
                </div>
                <div className="absolute -bottom-1 -right-1 w-7 h-7 rounded-full bg-amber-400 border-2 border-white flex items-center justify-center">
                  <span className="text-slate-900 font-black text-xs">5</span>
                </div>
              </div>
              <div>
                <h1 className="text-2xl font-black text-white">Alex Johnson</h1>
                <p className="text-indigo-200">alex@example.com</p>
                <div className="flex items-center gap-2 mt-1">
                  <span className="px-2.5 py-0.5 bg-white/20 backdrop-blur rounded-full text-white text-xs font-medium">Level {currentLevel}</span>
                  <span className="px-2.5 py-0.5 bg-white/20 backdrop-blur rounded-full text-white text-xs font-medium">#42 Global</span>
                </div>
              </div>
            </div>
            <button className="p-2.5 bg-white/10 backdrop-blur rounded-xl text-white hover:bg-white/20 transition-colors">
              <Edit2 className="w-4 h-4" />
            </button>
          </div>

          {/* XP Progress */}
          <div className="mb-4">
            <div className="flex justify-between text-sm mb-2">
              <span className="text-indigo-200">Level {currentLevel} Progress</span>
              <span className="text-white font-bold">{currentXP.toLocaleString()} / {nextLevelXP.toLocaleString()} XP</span>
            </div>
            <div className="h-3 bg-white/20 rounded-full overflow-hidden">
              <div className="h-full bg-white rounded-full" style={{ width: `${levelPct}%` }} />
            </div>
            <div className="text-indigo-200 text-xs mt-1">{(nextLevelXP - currentXP).toLocaleString()} XP until Level {currentLevel + 1}</div>
          </div>

          {/* Quick Stats */}
          <div className="grid grid-cols-4 gap-3">
            {[
              { label: 'Quizzes', value: '84', icon: BookOpen },
              { label: 'Avg Score', value: '78%', icon: Target },
              { label: 'Streak', value: '14d', icon: Flame },
              { label: 'XP', value: '8.2K', icon: Zap },
            ].map(({ label, value, icon: Icon }) => (
              <div key={label} className="bg-white/10 backdrop-blur rounded-xl p-3 text-center">
                <Icon className="w-4 h-4 text-white/80 mx-auto mb-1" />
                <div className="text-white font-black">{value}</div>
                <div className="text-indigo-200 text-xs">{label}</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-1">
        {(['overview', 'badges', 'history'] as const).map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`flex-1 py-2.5 rounded-xl font-semibold transition-all capitalize ${tab === t ? 'bg-gradient-to-r from-indigo-500 to-violet-600 text-white shadow-md' : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-300'}`}
          >
            {t}
          </button>
        ))}
      </div>

      {/* Overview */}
      {tab === 'overview' && (
        <div className="space-y-6">
          {/* XP Growth Chart */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
            <h3 className="font-bold text-slate-900 dark:text-white mb-4">XP Growth</h3>
            <ResponsiveContainer width="100%" height={180}>
              <AreaChart data={progressHistory}>
                <defs>
                  <linearGradient id="xpGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
                <XAxis dataKey="month" tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fill: '#64748b', fontSize: 12 }} axisLine={false} tickLine={false} />
                <Tooltip formatter={v => [v.toLocaleString(), 'XP']} />
                <Area type="monotone" dataKey="xp" stroke="#6366f1" strokeWidth={2} fill="url(#xpGrad)" name="XP" />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          {/* Category Radar */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
            <h3 className="font-bold text-slate-900 dark:text-white mb-4">Knowledge Radar</h3>
            <ResponsiveContainer width="100%" height={260}>
              <RadarChart data={radarData}>
                <PolarGrid stroke="#334155" />
                <PolarAngleAxis dataKey="subject" tick={{ fill: '#64748b', fontSize: 12 }} />
                <Radar name="Score" dataKey="score" stroke="#6366f1" fill="#6366f1" fillOpacity={0.3} strokeWidth={2} />
                <Tooltip formatter={v => [`${v}%`, 'Score']} />
              </RadarChart>
            </ResponsiveContainer>
          </div>

          {/* Category Scores */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
            <h3 className="font-bold text-slate-900 dark:text-white mb-4">Category Breakdown</h3>
            <div className="space-y-3">
              {radarData.map(({ subject, score }) => (
                <div key={subject}>
                  <div className="flex justify-between mb-1.5">
                    <span className="text-slate-700 dark:text-slate-300">{subject}</span>
                    <span className={`font-bold ${score >= 80 ? 'text-emerald-600 dark:text-emerald-400' : score >= 70 ? 'text-amber-600 dark:text-amber-400' : 'text-rose-600 dark:text-rose-400'}`}>{score}%</span>
                  </div>
                  <div className="h-2.5 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                    <div className={`h-full rounded-full ${score >= 80 ? 'bg-emerald-500' : score >= 70 ? 'bg-amber-500' : 'bg-rose-500'}`} style={{ width: `${score}%` }} />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Badges */}
      {tab === 'badges' && (
        <div className="space-y-4">
          <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400">
            <Award className="w-5 h-5" />
            <span>{badges.filter(b => b.earned).length} / {badges.length} badges earned</span>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {badges.map(badge => (
              <div key={badge.id} className={`p-4 rounded-2xl border-2 text-center transition-all ${badge.earned ? 'bg-white dark:bg-slate-900 border-indigo-200 dark:border-indigo-800 hover:shadow-md' : 'bg-slate-50 dark:bg-slate-900/50 border-dashed border-slate-300 dark:border-slate-700 opacity-50'}`}>
                <div className={`text-4xl mb-2 ${!badge.earned ? 'grayscale' : ''}`}>{badge.icon}</div>
                <div className={`font-bold text-sm mb-1 ${badge.earned ? 'text-slate-900 dark:text-white' : 'text-slate-500 dark:text-slate-600'}`}>{badge.name}</div>
                <div className="text-slate-500 dark:text-slate-400 text-xs mb-2">{badge.desc}</div>
                {badge.earned && badge.date && (
                  <div className="text-indigo-600 dark:text-indigo-400 text-xs font-medium">{badge.date}</div>
                )}
                {!badge.earned && (
                  <div className="text-slate-400 dark:text-slate-600 text-xs">Locked</div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* History */}
      {tab === 'history' && (
        <div className="space-y-3">
          {recentActivity.map((activity, idx) => (
            <div key={idx} className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-4 flex items-center gap-4">
              <div className={`w-12 h-12 rounded-xl flex items-center justify-center font-black text-white flex-shrink-0 ${activity.score >= 80 ? 'bg-gradient-to-br from-emerald-400 to-emerald-600' : activity.score >= 60 ? 'bg-gradient-to-br from-amber-400 to-amber-600' : 'bg-gradient-to-br from-rose-400 to-rose-600'}`}>
                {activity.score}%
              </div>
              <div className="flex-1 min-w-0">
                <div className="font-semibold text-slate-900 dark:text-white truncate">{activity.quiz}</div>
                <div className="flex items-center gap-3 text-slate-500 dark:text-slate-400 text-sm mt-0.5">
                  <span className="flex items-center gap-1"><Calendar className="w-3.5 h-3.5" />{activity.date}</span>
                  <span className="flex items-center gap-1"><Trophy className="w-3.5 h-3.5" />{activity.time}</span>
                </div>
              </div>
              <button
                onClick={() => setView('user-quiz')}
                className="px-3 py-1.5 bg-indigo-50 dark:bg-indigo-950/30 text-indigo-600 dark:text-indigo-400 rounded-xl text-sm font-medium hover:bg-indigo-100 dark:hover:bg-indigo-950/50 transition-colors flex-shrink-0"
              >
                Retry
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
