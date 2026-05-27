import { BookOpen, FileText, BarChart3, Clock, TrendingUp } from 'lucide-react';
import { AimbysState } from '../../../App';

const recentScores = [
  { exam: 'Mathematics XII — Unit Test 3', date: '15 May', score: 62, max: 80, pct: 77.5 },
  { exam: 'Physics XII — Chapter Test 4', date: '10 May', score: 44, max: 50, pct: 88.0 },
  { exam: 'English Core — Essay Test', date: '05 May', score: 28, max: 30, pct: 93.3 },
  { exam: 'Computer Sci. — Lab Test', date: '01 May', score: 47, max: 50, pct: 94.0 },
];

export function StudentDashboard(props: AimbysState) {
  const { setView, setExamActive } = props;
  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Student / Dashboard</div>
        <h1 className="text-slate-900 dark:text-white font-bold text-xl">Welcome back, Riya Patel</h1>
        <p className="text-slate-500 dark:text-slate-400 text-sm mt-0.5">Class XII-A · Roll No. 021 · Delhi Public School HQ</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Exams Taken', value: '14', icon: BookOpen, color: '#15803d' },
          { label: 'Avg Score', value: '79.5%', icon: BarChart3, color: '#0369a1' },
          { label: 'Class Rank', value: '12th', icon: TrendingUp, color: '#7c3aed' },
          { label: 'Next Exam', value: '22 May', icon: Clock, color: '#d97706' },
        ].map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className="w-8 h-8 rounded flex items-center justify-center mb-3" style={{ background: `${color}15` }}>
              <Icon className="w-4 h-4" style={{ color }} />
            </div>
            <div className="text-2xl font-black text-slate-900 dark:text-white">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm mt-0.5">{label}</div>
          </div>
        ))}
      </div>

      {/* Upcoming Exam Banner */}
      <div className="rounded-lg p-5 flex flex-col sm:flex-row items-start sm:items-center gap-4 border" style={{ background: '#0d1b2e', borderColor: '#1d4ed8' }}>
        <div className="flex-1">
          <div className="text-green-300 text-xs font-semibold uppercase tracking-wider mb-1">Next Scheduled Exam</div>
          <div className="text-white font-bold text-lg">Mathematics XII — Unit Test 4</div>
          <div className="text-slate-300 text-sm mt-0.5">22 May 2026 · 10:00 AM · 3 hours · 80 marks</div>
        </div>
        <button onClick={() => { setExamActive(true); setView('std-exam'); }} className="px-5 py-2.5 bg-green-600 hover:bg-green-700 text-white rounded text-sm font-semibold transition-colors flex-shrink-0">
          Start Exam Now →
        </button>
      </div>

      {/* Recent Results */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <h2 className="text-slate-900 dark:text-white font-semibold">Recent Results</h2>
          <button onClick={() => setView('std-results')} className="text-green-600 dark:text-green-400 text-sm hover:underline">View all →</button>
        </div>
        <div className="divide-y divide-slate-100 dark:divide-slate-800">
          {recentScores.map((r, i) => (
            <div key={i} className="px-5 py-4 flex items-center gap-4">
              <div className="flex-1 min-w-0">
                <div className="text-slate-900 dark:text-slate-100 font-medium text-sm truncate">{r.exam}</div>
                <div className="text-slate-400 text-xs mt-0.5">{r.date}</div>
              </div>
              <div className="flex items-center gap-3">
                <div className="text-right">
                  <div className="text-slate-900 dark:text-white font-bold text-sm">{r.score}/{r.max}</div>
                  <div className={`text-xs font-semibold ${r.pct >= 85 ? 'text-green-600 dark:text-green-400' : r.pct >= 70 ? 'text-blue-600 dark:text-blue-400' : 'text-amber-600 dark:text-amber-400'}`}>{r.pct}%</div>
                </div>
                <div className="w-16 h-1.5 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                  <div className="h-full rounded-full" style={{ width: `${r.pct}%`, background: r.pct >= 85 ? '#15803d' : r.pct >= 70 ? '#1d4ed8' : '#d97706' }} />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Subject Progress */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
        <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Subject Performance</h2>
        <div className="space-y-3">
          {[
            { subject: 'Computer Science', score: 91.7, color: '#15803d' },
            { subject: 'English', score: 87.0, color: '#0369a1' },
            { subject: 'Physics', score: 75.5, color: '#0369a1' },
            { subject: 'Chemistry', score: 75.0, color: '#0369a1' },
            { subject: 'Mathematics', score: 68.5, color: '#d97706' },
          ].map(({ subject, score, color }) => (
            <div key={subject} className="flex items-center gap-3">
              <span className="text-slate-600 dark:text-slate-400 text-sm w-32 flex-shrink-0">{subject}</span>
              <div className="flex-1 h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                <div className="h-full rounded-full transition-all" style={{ width: `${score}%`, background: color }} />
              </div>
              <span className="text-slate-700 dark:text-slate-300 font-semibold text-sm w-12 text-right">{score}%</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
