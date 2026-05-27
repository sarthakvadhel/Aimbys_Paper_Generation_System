import { useState } from 'react';
import { motion } from 'motion/react';
import { Trophy, RotateCcw, Home, Share2, CheckCircle, XCircle, ChevronDown, ChevronUp, Zap, Star, Clock, Target } from 'lucide-react';
import { RadarChart, Radar, PolarGrid, PolarAngleAxis, ResponsiveContainer, Tooltip } from 'recharts';
import { AppState } from '../../App';

const questions = [
  { question: 'What does JSX stand for in React development?', options: ['JavaScript XML', 'Java Syntax Extension', 'JavaScript Expression', 'JSON XML Syntax'], correct: 0, explanation: 'JSX stands for JavaScript XML. It is a syntax extension for JavaScript.' },
  { question: 'Which React hook is used for managing local component state?', options: ['useEffect', 'useState', 'useRef', 'useContext'], correct: 1, explanation: 'useState is the primary hook for managing local state in a React functional component.' },
  { question: 'What happens when you pass [] to useEffect?', options: ['Effect runs every render', 'Effect never runs', 'Effect runs once after initial render', 'Effect runs on unmount'], correct: 2, explanation: 'An empty dependency array tells React to run the effect once after the initial render.' },
  { question: 'What is the virtual DOM in React?', options: ['A real browser DOM', 'A lightweight in-memory representation', 'A CSS rendering engine', 'A JavaScript build tool'], correct: 1, explanation: 'The virtual DOM is a lightweight JavaScript representation of the actual DOM.' },
  { question: 'Which correctly describes prop drilling?', options: ['Passing props through nested levels', 'Creating holes in boundaries', 'Using refs for children', 'Directly mutating props'], correct: 0, explanation: 'Prop drilling refers to passing data through multiple layers of components.' },
  { question: 'What is the purpose of React.memo()?', options: ['Memoized selectors', 'Prevent re-renders if props unchanged', 'Cache API responses', 'Manage side effects'], correct: 1, explanation: 'React.memo() prevents unnecessary re-renders when props have not changed.' },
  { question: 'Which hook performs side effects?', options: ['useState', 'useCallback', 'useEffect', 'useMemo'], correct: 2, explanation: 'useEffect is designed for performing side effects in function components.' },
  { question: 'What does the "key" prop do in lists?', options: ['Styles list items', 'Helps React identify changed items', 'Sets display order', 'Creates unique CSS IDs'], correct: 1, explanation: 'The key prop helps React identify which items in a list have changed.' },
];

const radarData = [
  { subject: 'Hooks', score: 85, fullMark: 100 },
  { subject: 'State Mgmt', score: 90, fullMark: 100 },
  { subject: 'Lifecycle', score: 75, fullMark: 100 },
  { subject: 'Props', score: 80, fullMark: 100 },
  { subject: 'Performance', score: 70, fullMark: 100 },
  { subject: 'Patterns', score: 65, fullMark: 100 },
];

const getGrade = (pct: number) => {
  if (pct >= 90) return { grade: 'S', label: 'Exceptional', color: 'text-amber-500', bg: 'bg-amber-100 dark:bg-amber-950/40', emoji: '🏆' };
  if (pct >= 80) return { grade: 'A', label: 'Excellent', color: 'text-emerald-500', bg: 'bg-emerald-100 dark:bg-emerald-950/40', emoji: '⭐' };
  if (pct >= 70) return { grade: 'B', label: 'Good', color: 'text-indigo-500', bg: 'bg-indigo-100 dark:bg-indigo-950/40', emoji: '👍' };
  if (pct >= 60) return { grade: 'C', label: 'Average', color: 'text-violet-500', bg: 'bg-violet-100 dark:bg-violet-950/40', emoji: '📊' };
  return { grade: 'D', label: 'Needs Work', color: 'text-rose-500', bg: 'bg-rose-100 dark:bg-rose-950/40', emoji: '📚' };
};

export function QuizResults({ setView, quizAnswers, setQuizAnswers, setQuizStarted }: AppState) {
  const [reviewOpen, setReviewOpen] = useState<number | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'review' | 'analysis'>('overview');

  const totalQ = questions.length;
  const answered = Object.keys(quizAnswers);
  const correct = answered.filter(i => quizAnswers[parseInt(i)] === questions[parseInt(i)].correct).length;
  const wrong = answered.length - correct;
  const skipped = totalQ - answered.length;
  const pct = Math.round((correct / totalQ) * 100);
  const grade = getGrade(pct);
  const xpEarned = correct * 15 + (pct >= 80 ? 50 : 0);
  const timeTaken = '14m 32s';

  const handleRetry = () => {
    setQuizAnswers({});
    setQuizStarted(false);
    setView('user-quiz');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-indigo-50/30 dark:from-slate-950 dark:to-indigo-950/10 p-4 md:p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        {/* Score Hero */}
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.5 }}
          className="bg-gradient-to-br from-indigo-600 via-violet-600 to-purple-700 rounded-3xl p-8 text-white text-center relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_70%_20%,rgba(255,255,255,0.1),transparent)]" />
          <div className="relative">
            <div className="text-5xl mb-4">{grade.emoji}</div>
            <div className="text-indigo-200 mb-1">Quiz Completed!</div>
            <h1 className="text-7xl font-black mb-1">{pct}%</h1>
            <div className="text-2xl font-bold text-indigo-200 mb-6">{grade.label}</div>

            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
              {[
                { label: 'Correct', value: correct, icon: CheckCircle, color: 'text-emerald-300' },
                { label: 'Wrong', value: wrong, icon: XCircle, color: 'text-rose-300' },
                { label: 'Time', value: timeTaken, icon: Clock, color: 'text-cyan-300' },
                { label: 'XP Earned', value: `+${xpEarned}`, icon: Zap, color: 'text-amber-300' },
              ].map(({ label, value, icon: Icon, color }) => (
                <div key={label} className="bg-white/10 backdrop-blur rounded-2xl p-4">
                  <Icon className={`w-5 h-5 ${color} mx-auto mb-2`} />
                  <div className="text-white font-black text-xl">{value}</div>
                  <div className="text-indigo-200 text-sm">{label}</div>
                </div>
              ))}
            </div>

            <div className="flex flex-col sm:flex-row items-center justify-center gap-3">
              <button onClick={handleRetry} className="flex items-center gap-2 px-6 py-3 bg-white/10 backdrop-blur border border-white/20 text-white rounded-xl hover:bg-white/20 transition-all font-semibold w-full sm:w-auto justify-center">
                <RotateCcw className="w-4 h-4" />
                Retry Quiz
              </button>
              <button onClick={() => setView('user-home')} className="flex items-center gap-2 px-6 py-3 bg-white text-indigo-700 rounded-xl hover:bg-indigo-50 transition-all font-semibold w-full sm:w-auto justify-center">
                <Home className="w-4 h-4" />
                Go Home
              </button>
              <button className="flex items-center gap-2 px-6 py-3 bg-white/10 backdrop-blur border border-white/20 text-white rounded-xl hover:bg-white/20 transition-all font-semibold w-full sm:w-auto justify-center">
                <Share2 className="w-4 h-4" />
                Share
              </button>
            </div>
          </div>
        </motion.div>

        {/* Tab Navigation */}
        <div className="flex bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-1">
          {(['overview', 'review', 'analysis'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`flex-1 py-3 rounded-xl font-semibold transition-all capitalize ${
                activeTab === tab ? 'bg-gradient-to-r from-indigo-500 to-violet-600 text-white shadow-md' : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-300'
              }`}
            >
              {tab}
            </button>
          ))}
        </div>

        {/* Overview Tab */}
        {activeTab === 'overview' && (
          <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="space-y-6">
            {/* Grade Card */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className={`${grade.bg} rounded-2xl p-6 flex flex-col items-center`}>
                <div className={`text-6xl font-black ${grade.color}`}>{grade.grade}</div>
                <div className="text-slate-700 dark:text-slate-300 font-bold mt-1">{grade.label}</div>
                <div className="text-slate-500 dark:text-slate-400 text-sm">Grade</div>
              </div>
              <div className="md:col-span-2 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
                <h3 className="font-bold text-slate-900 dark:text-white mb-4">Score Breakdown</h3>
                <div className="space-y-3">
                  {[
                    { label: 'Correct Answers', value: correct, total: totalQ, color: 'bg-emerald-500' },
                    { label: 'Wrong Answers', value: wrong, total: totalQ, color: 'bg-rose-500' },
                    { label: 'Skipped', value: skipped, total: totalQ, color: 'bg-slate-400 dark:bg-slate-600' },
                  ].map(item => (
                    <div key={item.label}>
                      <div className="flex justify-between mb-1">
                        <span className="text-slate-600 dark:text-slate-400 text-sm">{item.label}</span>
                        <span className="text-slate-900 dark:text-white font-bold text-sm">{item.value}/{item.total}</span>
                      </div>
                      <div className="h-3 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                        <div className={`h-full ${item.color} rounded-full transition-all`} style={{ width: `${(item.value / item.total) * 100}%` }} />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* Strengths & Weaknesses */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
                <h3 className="font-bold text-emerald-700 dark:text-emerald-400 flex items-center gap-2 mb-4">
                  <Target className="w-5 h-5" /> Strengths
                </h3>
                {['React State Management (90%)', 'JSX Syntax (85%)', 'Component Lifecycle (80%)'].map(s => (
                  <div key={s} className="flex items-center gap-2 py-2 border-b border-slate-100 dark:border-slate-800 last:border-0">
                    <CheckCircle className="w-4 h-4 text-emerald-500 flex-shrink-0" />
                    <span className="text-slate-700 dark:text-slate-300 text-sm">{s}</span>
                  </div>
                ))}
              </div>
              <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
                <h3 className="font-bold text-rose-700 dark:text-rose-400 flex items-center gap-2 mb-4">
                  <Target className="w-5 h-5" /> Areas to Improve
                </h3>
                {['Performance Optimization (65%)', 'Design Patterns (70%)', 'Advanced Hooks (75%)'].map(s => (
                  <div key={s} className="flex items-center gap-2 py-2 border-b border-slate-100 dark:border-slate-800 last:border-0">
                    <XCircle className="w-4 h-4 text-rose-500 flex-shrink-0" />
                    <span className="text-slate-700 dark:text-slate-300 text-sm">{s}</span>
                  </div>
                ))}
              </div>
            </div>
          </motion.div>
        )}

        {/* Review Tab */}
        {activeTab === 'review' && (
          <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="space-y-3">
            {questions.map((q, i) => {
              const userAns = quizAnswers[i];
              const isCorrect = userAns === q.correct;
              const isOpen = reviewOpen === i;
              return (
                <div key={i} className={`bg-white dark:bg-slate-900 rounded-2xl border-2 overflow-hidden transition-all ${isCorrect ? 'border-emerald-300 dark:border-emerald-800' : userAns !== undefined ? 'border-rose-300 dark:border-rose-800' : 'border-slate-200 dark:border-slate-800'}`}>
                  <button onClick={() => setReviewOpen(isOpen ? null : i)} className="w-full flex items-start gap-4 p-4 text-left">
                    <div className={`w-8 h-8 rounded-xl flex items-center justify-center flex-shrink-0 font-bold text-sm ${isCorrect ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400' : userAns !== undefined ? 'bg-rose-100 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-500'}`}>{i + 1}</div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <span className="text-slate-900 dark:text-slate-100 font-medium text-sm">{q.question}</span>
                        {isOpen ? <ChevronUp className="w-4 h-4 text-slate-400 flex-shrink-0 mt-0.5" /> : <ChevronDown className="w-4 h-4 text-slate-400 flex-shrink-0 mt-0.5" />}
                      </div>
                      <div className="flex items-center gap-3 mt-1">
                        {isCorrect ? (
                          <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400 text-xs"><CheckCircle className="w-3.5 h-3.5" />Correct</span>
                        ) : userAns !== undefined ? (
                          <span className="flex items-center gap-1 text-rose-600 dark:text-rose-400 text-xs"><XCircle className="w-3.5 h-3.5" />Wrong</span>
                        ) : (
                          <span className="text-slate-400 dark:text-slate-500 text-xs">Skipped</span>
                        )}
                      </div>
                    </div>
                  </button>
                  {isOpen && (
                    <div className="px-4 pb-4 space-y-2 border-t border-slate-100 dark:border-slate-800 pt-4">
                      {q.options.map((opt, oi) => (
                        <div key={oi} className={`flex items-center gap-3 p-3 rounded-xl ${oi === q.correct ? 'bg-emerald-50 dark:bg-emerald-950/20 border border-emerald-200 dark:border-emerald-800' : oi === userAns && userAns !== q.correct ? 'bg-rose-50 dark:bg-rose-950/20 border border-rose-200 dark:border-rose-800' : 'bg-slate-50 dark:bg-slate-800/50'}`}>
                          <span className={`w-6 h-6 rounded-lg flex items-center justify-center text-xs font-bold flex-shrink-0 ${oi === q.correct ? 'bg-emerald-500 text-white' : oi === userAns && userAns !== q.correct ? 'bg-rose-500 text-white' : 'bg-slate-200 dark:bg-slate-700 text-slate-600 dark:text-slate-400'}`}>
                            {['A', 'B', 'C', 'D'][oi]}
                          </span>
                          <span className={`text-sm ${oi === q.correct ? 'text-emerald-700 dark:text-emerald-300 font-medium' : oi === userAns && userAns !== q.correct ? 'text-rose-700 dark:text-rose-300' : 'text-slate-600 dark:text-slate-400'}`}>{opt}</span>
                          {oi === q.correct && <CheckCircle className="w-4 h-4 text-emerald-500 ml-auto flex-shrink-0" />}
                        </div>
                      ))}
                      <div className="mt-3 p-3 bg-indigo-50 dark:bg-indigo-950/20 border border-indigo-200 dark:border-indigo-800 rounded-xl">
                        <p className="text-indigo-700 dark:text-indigo-300 text-sm"><strong>Explanation:</strong> {q.explanation}</p>
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </motion.div>
        )}

        {/* Analysis Tab */}
        {activeTab === 'analysis' && (
          <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="space-y-6">
            <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
              <h3 className="font-bold text-slate-900 dark:text-white mb-4">Topic Performance Radar</h3>
              <ResponsiveContainer width="100%" height={280}>
                <RadarChart data={radarData}>
                  <PolarGrid stroke="#334155" />
                  <PolarAngleAxis dataKey="subject" tick={{ fill: '#64748b', fontSize: 12 }} />
                  <Radar name="Score" dataKey="score" stroke="#6366f1" fill="#6366f1" fillOpacity={0.3} strokeWidth={2} />
                  <Tooltip formatter={v => [`${v}%`, 'Score']} />
                </RadarChart>
              </ResponsiveContainer>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
                <h3 className="font-bold text-slate-900 dark:text-white mb-4">Your XP Earned</h3>
                <div className="flex items-center gap-4">
                  <div className="w-16 h-16 rounded-2xl bg-amber-100 dark:bg-amber-950/40 flex items-center justify-center">
                    <Zap className="w-8 h-8 text-amber-500" />
                  </div>
                  <div>
                    <div className="text-3xl font-black text-amber-600 dark:text-amber-400">+{xpEarned}</div>
                    <div className="text-slate-500 dark:text-slate-400">XP Points</div>
                    <div className="text-slate-500 dark:text-slate-400 text-sm">New total: 8,{240 + xpEarned}</div>
                  </div>
                </div>
              </div>
              <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
                <h3 className="font-bold text-slate-900 dark:text-white mb-4">Recommended Next</h3>
                <div className="space-y-2">
                  {['Advanced React Patterns', 'React Query Mastery', 'State Management with Zustand'].map(r => (
                    <button key={r} onClick={() => { setQuizAnswers({}); setQuizStarted(false); setView('user-quiz'); }} className="w-full flex items-center gap-2 p-3 bg-indigo-50 dark:bg-indigo-950/20 border border-indigo-200 dark:border-indigo-800 rounded-xl hover:bg-indigo-100 dark:hover:bg-indigo-950/40 transition-colors text-left">
                      <Star className="w-4 h-4 text-indigo-500 flex-shrink-0" />
                      <span className="text-indigo-700 dark:text-indigo-300 text-sm font-medium">{r}</span>
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </motion.div>
        )}
      </div>
    </div>
  );
}
