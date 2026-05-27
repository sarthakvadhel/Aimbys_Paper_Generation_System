import { motion } from 'motion/react';
import { Clock, Users, Star, ArrowRight, Flame, Zap, Trophy, BookOpen, ChevronRight, Play } from 'lucide-react';
import { AppState } from '../../App';

const featuredQuizzes = [
  { id: 1, title: 'React Fundamentals', category: 'React', questions: 25, duration: 20, difficulty: 'Medium', participants: 1240, rating: 4.8, color: 'from-indigo-500 to-indigo-700', bgLight: 'bg-indigo-50 dark:bg-indigo-950/30', textColor: 'text-indigo-600 dark:text-indigo-400', emoji: '⚛️', live: true },
  { id: 2, title: 'Python Mastery', category: 'Python', questions: 35, duration: 25, difficulty: 'Hard', participants: 980, rating: 4.7, color: 'from-emerald-500 to-emerald-700', bgLight: 'bg-emerald-50 dark:bg-emerald-950/30', textColor: 'text-emerald-600 dark:text-emerald-400', emoji: '🐍', live: false },
  { id: 3, title: 'CSS Grid Magic', category: 'CSS', questions: 20, duration: 15, difficulty: 'Easy', participants: 1580, rating: 4.9, color: 'from-violet-500 to-violet-700', bgLight: 'bg-violet-50 dark:bg-violet-950/30', textColor: 'text-violet-600 dark:text-violet-400', emoji: '🎨', live: false },
];

const recentQuizzes = [
  { title: 'Advanced JavaScript', score: 82, total: 100, date: '2 days ago', correct: 41, total_q: 50, category: 'JavaScript' },
  { title: 'System Design Basics', score: 64, total: 100, date: '5 days ago', correct: 16, total_q: 25, category: 'System Design' },
  { title: 'SQL Fundamentals', score: 91, total: 100, date: '1 week ago', correct: 36, total_q: 40, category: 'Databases' },
];

const categories = [
  { name: 'JavaScript', quizzes: 42, icon: '🟨', color: 'bg-amber-50 dark:bg-amber-950/30 border-amber-200 dark:border-amber-800' },
  { name: 'Python', quizzes: 38, icon: '🐍', color: 'bg-emerald-50 dark:bg-emerald-950/30 border-emerald-200 dark:border-emerald-800' },
  { name: 'React', quizzes: 28, icon: '⚛️', color: 'bg-indigo-50 dark:bg-indigo-950/30 border-indigo-200 dark:border-indigo-800' },
  { name: 'Algorithms', quizzes: 54, icon: '🧮', color: 'bg-violet-50 dark:bg-violet-950/30 border-violet-200 dark:border-violet-800' },
  { name: 'Databases', quizzes: 31, icon: '🗄️', color: 'bg-cyan-50 dark:bg-cyan-950/30 border-cyan-200 dark:border-cyan-800' },
  { name: 'DevOps', quizzes: 22, icon: '🚀', color: 'bg-rose-50 dark:bg-rose-950/30 border-rose-200 dark:border-rose-800' },
];

const difficultyBadge = (d: string) =>
  d === 'Easy' ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400' :
  d === 'Medium' ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400' :
  'bg-rose-100 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400';

export function UserHome({ setView, setSelectedQuizId, setQuizStarted, setQuizAnswers }: AppState) {
  const handleStartQuiz = (id: number) => {
    setSelectedQuizId(id);
    setQuizAnswers({});
    setQuizStarted(false);
    setView('user-quiz');
  };

  return (
    <div className="space-y-8">
      {/* Welcome Banner */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-indigo-600 via-violet-600 to-purple-700 p-8 text-white"
      >
        <div className="absolute top-0 right-0 w-64 h-64 bg-white/10 rounded-full translate-x-1/2 -translate-y-1/2" />
        <div className="absolute bottom-0 left-0 w-48 h-48 bg-black/10 rounded-full -translate-x-1/2 translate-y-1/2" />
        <div className="relative">
          <div className="flex items-start justify-between">
            <div>
              <p className="text-indigo-200 mb-1">Good morning, 👋</p>
              <h1 className="text-3xl font-black mb-2">Alex Johnson</h1>
              <p className="text-indigo-200 mb-6">You're on a <span className="text-white font-bold">🔥 14-day streak!</span> Keep it up!</p>
              <button
                onClick={() => setView('user-discover')}
                className="flex items-center gap-2 px-5 py-3 bg-white text-indigo-700 rounded-xl font-semibold hover:bg-indigo-50 transition-all shadow-lg"
              >
                Start Today's Quiz
                <ArrowRight className="w-4 h-4" />
              </button>
            </div>
            <div className="hidden sm:block text-right">
              <div className="flex flex-col items-end gap-3">
                <div className="px-4 py-3 bg-white/20 backdrop-blur rounded-2xl text-center">
                  <div className="text-3xl font-black">8,240</div>
                  <div className="text-indigo-200 text-sm">Total XP</div>
                </div>
                <div className="px-4 py-3 bg-white/20 backdrop-blur rounded-2xl text-center">
                  <div className="text-3xl font-black">#42</div>
                  <div className="text-indigo-200 text-sm">Global Rank</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </motion.div>

      {/* Quick Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Quizzes Done', value: '84', icon: BookOpen, color: 'text-indigo-600 dark:text-indigo-400', bg: 'bg-indigo-50 dark:bg-indigo-950/30' },
          { label: 'Avg Score', value: '78%', icon: Trophy, color: 'text-violet-600 dark:text-violet-400', bg: 'bg-violet-50 dark:bg-violet-950/30' },
          { label: 'Current Streak', value: '14d', icon: Flame, color: 'text-amber-600 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/30' },
          { label: 'XP Points', value: '8,240', icon: Zap, color: 'text-cyan-600 dark:text-cyan-400', bg: 'bg-cyan-50 dark:bg-cyan-950/30' },
        ].map(({ label, value, icon: Icon, color, bg }) => (
          <div key={label} className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-4">
            <div className={`w-9 h-9 ${bg} rounded-xl flex items-center justify-center mb-3`}>
              <Icon className={`w-4 h-4 ${color}`} />
            </div>
            <div className={`text-2xl font-black ${color} mb-0.5`}>{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm">{label}</div>
          </div>
        ))}
      </div>

      {/* Featured / Live Quizzes */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-black text-slate-900 dark:text-white">Featured Quizzes</h2>
          <button onClick={() => setView('user-discover')} className="flex items-center gap-1 text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300 transition-colors font-medium">
            View all <ChevronRight className="w-4 h-4" />
          </button>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {featuredQuizzes.map(quiz => (
            <motion.div
              key={quiz.id}
              whileHover={{ y: -2 }}
              className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden hover:shadow-xl transition-all cursor-pointer group"
              onClick={() => handleStartQuiz(quiz.id)}
            >
              <div className={`h-32 bg-gradient-to-br ${quiz.color} flex items-center justify-center relative`}>
                <span className="text-5xl">{quiz.emoji}</span>
                {quiz.live && (
                  <div className="absolute top-3 right-3 flex items-center gap-1.5 px-2.5 py-1 bg-white/20 backdrop-blur rounded-full">
                    <div className="w-2 h-2 bg-white rounded-full animate-pulse" />
                    <span className="text-white text-xs font-bold">LIVE</span>
                  </div>
                )}
              </div>
              <div className="p-4">
                <div className="flex items-start justify-between mb-2">
                  <h3 className="font-bold text-slate-900 dark:text-white group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">{quiz.title}</h3>
                  <span className={`px-2 py-0.5 rounded-full text-xs font-medium flex-shrink-0 ml-2 ${difficultyBadge(quiz.difficulty)}`}>{quiz.difficulty}</span>
                </div>
                <div className="flex items-center gap-3 text-slate-500 dark:text-slate-400 text-sm mb-3">
                  <span className="flex items-center gap-1"><BookOpen className="w-3.5 h-3.5" />{quiz.questions} Qs</span>
                  <span className="flex items-center gap-1"><Clock className="w-3.5 h-3.5" />{quiz.duration}m</span>
                  <span className="flex items-center gap-1"><Users className="w-3.5 h-3.5" />{quiz.participants.toLocaleString()}</span>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1">
                    <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                    <span className="text-slate-700 dark:text-slate-300 text-sm font-medium">{quiz.rating}</span>
                  </div>
                  <button className="flex items-center gap-1.5 px-3 py-1.5 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl text-sm font-semibold hover:from-indigo-600 hover:to-violet-700 transition-all">
                    <Play className="w-3.5 h-3.5" />
                    Start
                  </button>
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      </div>

      {/* Categories */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-black text-slate-900 dark:text-white">Browse Categories</h2>
          <button onClick={() => setView('user-discover')} className="flex items-center gap-1 text-indigo-600 dark:text-indigo-400 font-medium hover:text-indigo-700 dark:hover:text-indigo-300 transition-colors">
            All <ChevronRight className="w-4 h-4" />
          </button>
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-3">
          {categories.map(cat => (
            <button
              key={cat.name}
              onClick={() => setView('user-discover')}
              className={`flex flex-col items-center gap-2 p-4 rounded-2xl border ${cat.color} hover:shadow-md transition-all`}
            >
              <span className="text-2xl">{cat.icon}</span>
              <span className="text-slate-900 dark:text-white font-semibold text-sm">{cat.name}</span>
              <span className="text-slate-500 dark:text-slate-400 text-xs">{cat.quizzes} quizzes</span>
            </button>
          ))}
        </div>
      </div>

      {/* Recent Performance */}
      <div>
        <h2 className="text-xl font-black text-slate-900 dark:text-white mb-4">Recent Quizzes</h2>
        <div className="space-y-3">
          {recentQuizzes.map(q => (
            <div key={q.title} className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-4 flex items-center gap-4">
              <div className={`w-12 h-12 rounded-xl flex items-center justify-center text-white font-black text-lg flex-shrink-0 ${
                q.score >= 80 ? 'bg-gradient-to-br from-emerald-400 to-emerald-600' :
                q.score >= 60 ? 'bg-gradient-to-br from-amber-400 to-amber-600' :
                'bg-gradient-to-br from-rose-400 to-rose-600'
              }`}>
                {q.score >= 80 ? '🏆' : q.score >= 60 ? '📊' : '📉'}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between mb-1">
                  <span className="font-semibold text-slate-900 dark:text-white truncate">{q.title}</span>
                  <span className="text-slate-500 dark:text-slate-400 text-sm flex-shrink-0 ml-2">{q.date}</span>
                </div>
                <div className="flex items-center gap-3">
                  <div className="flex-1 h-2 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full ${q.score >= 80 ? 'bg-emerald-500' : q.score >= 60 ? 'bg-amber-500' : 'bg-rose-500'}`}
                      style={{ width: `${q.score}%` }}
                    />
                  </div>
                  <span className={`font-bold text-sm flex-shrink-0 ${q.score >= 80 ? 'text-emerald-600 dark:text-emerald-400' : q.score >= 60 ? 'text-amber-600 dark:text-amber-400' : 'text-rose-600 dark:text-rose-400'}`}>
                    {q.correct}/{q.total_q}
                  </span>
                  <span className={`font-bold text-sm flex-shrink-0 ${q.score >= 80 ? 'text-emerald-600 dark:text-emerald-400' : q.score >= 60 ? 'text-amber-600 dark:text-amber-400' : 'text-rose-600 dark:text-rose-400'}`}>
                    {q.score}%
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
