import { useState } from 'react';
import { Trophy, Flame, Star, TrendingUp, TrendingDown, Minus, Crown, Medal } from 'lucide-react';
import { AppState } from '../../App';

const leaderboardData = [
  { rank: 1, name: 'Emma Davis', avatar: 'ED', xp: 12500, quizzes: 112, streak: 28, change: 0, badge: '👑', level: 15 },
  { rank: 2, name: 'Isabella Clark', avatar: 'IC', xp: 11800, quizzes: 98, streak: 45, change: 1, badge: '🏆', level: 14 },
  { rank: 3, name: 'Mia Anderson', avatar: 'MA', xp: 10200, quizzes: 95, streak: 19, change: -1, badge: '🥈', level: 13 },
  { rank: 4, name: 'Sophia Martinez', avatar: 'SM', xp: 9240, quizzes: 84, streak: 14, change: 0, badge: '🥉', level: 12 },
  { rank: 5, name: 'Liam Johnson', avatar: 'LJ', xp: 8640, quizzes: 77, streak: 10, change: 2, badge: '⭐', level: 11 },
  { rank: 6, name: 'Oliver Chen', avatar: 'OC', xp: 7900, quizzes: 71, streak: 8, change: -1, badge: '⭐', level: 10 },
  { rank: 7, name: 'Charlotte Brown', avatar: 'CB', xp: 7200, quizzes: 65, streak: 6, change: 3, badge: '⭐', level: 9 },
  { rank: 8, name: 'William Taylor', avatar: 'WT', xp: 6820, quizzes: 61, streak: 7, change: 0, badge: '⭐', level: 9 },
  { rank: 9, name: 'Amelia White', avatar: 'AW', xp: 6100, quizzes: 55, streak: 4, change: -2, badge: '⭐', level: 8 },
  { rank: 10, name: 'James Lee', avatar: 'JL', xp: 5580, quizzes: 50, streak: 0, change: 1, badge: '🎯', level: 8 },
  { rank: 11, name: 'Lucas Wilson', avatar: 'LW', xp: 5100, quizzes: 46, streak: 3, change: 0, badge: '🎯', level: 7 },
  { rank: 12, name: 'Harper Martinez', avatar: 'HM', xp: 4760, quizzes: 43, streak: 9, change: 2, badge: '🎯', level: 7 },
  { rank: 42, name: 'Alex Johnson (You)', avatar: 'AJ', xp: 3240, quizzes: 28, streak: 14, change: 4, badge: '🏅', level: 5, isCurrentUser: true },
];

const avatarGradients = [
  'from-indigo-400 to-indigo-600', 'from-violet-400 to-violet-600',
  'from-cyan-400 to-cyan-600', 'from-emerald-400 to-emerald-600',
  'from-amber-400 to-amber-600', 'from-rose-400 to-rose-600',
  'from-pink-400 to-pink-600', 'from-teal-400 to-teal-600',
];

const periods = ['Daily', 'Weekly', 'Monthly', 'All Time'];
const categories = ['All', 'React', 'JavaScript', 'Python', 'Algorithms'];

export function Leaderboard({ setView }: AppState) {
  const [period, setPeriod] = useState('Weekly');
  const [category, setCategory] = useState('All');

  const top3 = leaderboardData.slice(0, 3);
  const rest = leaderboardData.slice(3, 12);
  const currentUser = leaderboardData.find(u => (u as any).isCurrentUser);

  const rankIcon = (rank: number) => {
    if (rank === 1) return <Crown className="w-5 h-5 text-amber-500" />;
    if (rank === 2) return <Medal className="w-5 h-5 text-slate-400" />;
    if (rank === 3) return <Medal className="w-5 h-5 text-amber-600" />;
    return <span className="text-slate-500 dark:text-slate-400 font-bold text-sm w-5 text-center">{rank}</span>;
  };

  const changeIcon = (change: number) => {
    if (change > 0) return <div className="flex items-center gap-0.5 text-emerald-500 text-xs"><TrendingUp className="w-3 h-3" />{change}</div>;
    if (change < 0) return <div className="flex items-center gap-0.5 text-rose-500 text-xs"><TrendingDown className="w-3 h-3" />{Math.abs(change)}</div>;
    return <Minus className="w-3 h-3 text-slate-400" />;
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-black text-slate-900 dark:text-white">Leaderboard</h1>
        <p className="text-slate-500 dark:text-slate-400">Compete with learners worldwide</p>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-1 flex-1">
          {periods.map(p => (
            <button
              key={p}
              onClick={() => setPeriod(p)}
              className={`flex-1 py-2 rounded-lg text-sm font-medium transition-all ${period === p ? 'bg-gradient-to-r from-indigo-500 to-violet-600 text-white shadow' : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-300'}`}
            >
              {p}
            </button>
          ))}
        </div>
        <select
          value={category}
          onChange={e => setCategory(e.target.value)}
          className="px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white outline-none cursor-pointer"
        >
          {categories.map(c => <option key={c} value={c}>{c === 'All' ? 'All Categories' : c}</option>)}
        </select>
      </div>

      {/* Top 3 Podium */}
      <div className="bg-gradient-to-br from-indigo-600 via-violet-600 to-purple-700 rounded-3xl p-8">
        <div className="flex items-end justify-center gap-4">
          {/* 2nd Place */}
          <div className="flex flex-col items-center gap-2 flex-1">
            <div className="text-2xl">{top3[1]?.badge}</div>
            <div className="w-12 h-12 rounded-full bg-white/20 backdrop-blur flex items-center justify-center text-white font-bold border-4 border-white/30">
              {top3[1]?.avatar}
            </div>
            <span className="text-white font-bold text-sm text-center">{top3[1]?.name.split(' ')[0]}</span>
            <span className="text-indigo-200 text-xs">{top3[1]?.xp.toLocaleString()} XP</span>
            <div className="w-full h-16 bg-white/20 backdrop-blur rounded-t-xl flex items-center justify-center">
              <span className="text-white font-black text-2xl">2</span>
            </div>
          </div>
          {/* 1st Place */}
          <div className="flex flex-col items-center gap-2 flex-1">
            <div className="text-3xl">{top3[0]?.badge}</div>
            <div className="w-16 h-16 rounded-full bg-white/20 backdrop-blur flex items-center justify-center text-white font-bold text-lg border-4 border-amber-300">
              {top3[0]?.avatar}
            </div>
            <span className="text-white font-black text-sm text-center">{top3[0]?.name.split(' ')[0]}</span>
            <span className="text-indigo-200 text-xs">{top3[0]?.xp.toLocaleString()} XP</span>
            <div className="w-full h-24 bg-white/20 backdrop-blur rounded-t-xl flex items-center justify-center">
              <span className="text-white font-black text-3xl">1</span>
            </div>
          </div>
          {/* 3rd Place */}
          <div className="flex flex-col items-center gap-2 flex-1">
            <div className="text-2xl">{top3[2]?.badge}</div>
            <div className="w-12 h-12 rounded-full bg-white/20 backdrop-blur flex items-center justify-center text-white font-bold border-4 border-white/30">
              {top3[2]?.avatar}
            </div>
            <span className="text-white font-bold text-sm text-center">{top3[2]?.name.split(' ')[0]}</span>
            <span className="text-indigo-200 text-xs">{top3[2]?.xp.toLocaleString()} XP</span>
            <div className="w-full h-10 bg-white/20 backdrop-blur rounded-t-xl flex items-center justify-center">
              <span className="text-white font-black text-2xl">3</span>
            </div>
          </div>
        </div>
      </div>

      {/* Current User Banner */}
      {currentUser && (
        <div className="bg-indigo-50 dark:bg-indigo-950/30 border-2 border-indigo-300 dark:border-indigo-700 rounded-2xl p-4 flex items-center gap-4">
          <div className="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500 flex items-center justify-center text-white font-bold flex-shrink-0">
            {currentUser.avatar}
          </div>
          <div className="flex-1">
            <div className="flex items-center gap-2">
              <span className="font-bold text-indigo-700 dark:text-indigo-300">Your Position</span>
              <span className="px-2 py-0.5 bg-indigo-200 dark:bg-indigo-900 text-indigo-700 dark:text-indigo-300 rounded-full text-xs font-bold">#{currentUser.rank}</span>
            </div>
            <div className="text-indigo-600 dark:text-indigo-400 text-sm">{currentUser.xp.toLocaleString()} XP • {currentUser.quizzes} quizzes</div>
          </div>
          <div className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
            <TrendingUp className="w-4 h-4" />
            <span className="font-bold">+{(currentUser as any).change}</span>
          </div>
        </div>
      )}

      {/* Rankings Table */}
      <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div className="px-4 py-3 border-b border-slate-200 dark:border-slate-800 grid grid-cols-12 text-slate-500 dark:text-slate-400 text-sm font-semibold">
          <span className="col-span-1 text-center">#</span>
          <span className="col-span-5">Player</span>
          <span className="col-span-2 text-right">XP</span>
          <span className="col-span-2 text-right hidden sm:block">Quizzes</span>
          <span className="col-span-2 text-right">Streak</span>
        </div>
        <div>
          {rest.map((user, idx) => {
            const isUser = (user as any).isCurrentUser;
            return (
              <div
                key={user.rank}
                className={`grid grid-cols-12 items-center px-4 py-3.5 border-b border-slate-100 dark:border-slate-800 last:border-0 transition-colors ${isUser ? 'bg-indigo-50 dark:bg-indigo-950/20' : 'hover:bg-slate-50 dark:hover:bg-slate-800/30'}`}
              >
                <div className="col-span-1 flex items-center justify-center">
                  {rankIcon(user.rank)}
                </div>
                <div className="col-span-5 flex items-center gap-3">
                  <div className={`w-9 h-9 rounded-full bg-gradient-to-br ${avatarGradients[idx % avatarGradients.length]} flex items-center justify-center text-white font-bold text-sm flex-shrink-0`}>
                    {user.avatar}
                  </div>
                  <div className="min-w-0">
                    <div className={`font-semibold truncate text-sm ${isUser ? 'text-indigo-700 dark:text-indigo-300' : 'text-slate-900 dark:text-slate-100'}`}>
                      {user.name}
                    </div>
                    <div className="flex items-center gap-1.5">
                      <span className="text-xs text-slate-500 dark:text-slate-400">Lv.{user.level}</span>
                      <span className="text-slate-300 dark:text-slate-700">•</span>
                      {changeIcon(user.change)}
                    </div>
                  </div>
                </div>
                <div className="col-span-2 text-right">
                  <span className="font-bold text-slate-900 dark:text-white text-sm">{user.xp.toLocaleString()}</span>
                </div>
                <div className="col-span-2 text-right hidden sm:block">
                  <span className="text-slate-600 dark:text-slate-400 text-sm">{user.quizzes}</span>
                </div>
                <div className="col-span-2 text-right">
                  <span className={`font-semibold text-sm flex items-center justify-end gap-1 ${user.streak > 0 ? 'text-amber-500' : 'text-slate-400 dark:text-slate-600'}`}>
                    {user.streak > 0 ? <><Flame className="w-3.5 h-3.5" />{user.streak}d</> : '—'}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
