import { CheckCircle, XCircle, TrendingUp } from 'lucide-react';
import { AimbysState } from '../../../App';

const results = [
  {
    exam: 'Mathematics XII — Unit Test 3', date: '15 May 2026', totalMarks: 80, scored: 62, pct: 77.5, rank: 14,
    breakdown: [
      { topic: 'Calculus', maxMarks: 24, scored: 18 },
      { topic: 'Algebra', maxMarks: 20, scored: 17 },
      { topic: 'Trigonometry', maxMarks: 20, scored: 14 },
      { topic: 'Probability', maxMarks: 16, scored: 13 },
    ],
  },
  {
    exam: 'Physics XII — Chapter Test 4', date: '10 May 2026', totalMarks: 50, scored: 44, pct: 88.0, rank: 6,
    breakdown: [
      { topic: 'Electrostatics', maxMarks: 20, scored: 18 },
      { topic: 'Current Electricity', maxMarks: 20, scored: 18 },
      { topic: 'Magnetism', maxMarks: 10, scored: 8 },
    ],
  },
];

export function StudentResults(_props: AimbysState) {
  const r = results[0];
  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Student / Results</div>
        <h1 className="text-slate-900 dark:text-white font-bold text-xl">My Results</h1>
      </div>

      {/* Latest Result Card */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
          <div className="text-slate-500 dark:text-slate-400 text-xs mb-0.5">Latest Result</div>
          <h2 className="text-slate-900 dark:text-white font-bold">{r.exam}</h2>
          <div className="text-slate-400 text-xs mt-0.5">{r.date}</div>
        </div>
        <div className="p-5">
          <div className="flex flex-wrap gap-6 mb-6">
            <div>
              <div className="text-4xl font-black" style={{ color: '#15803d' }}>{r.scored}/{r.totalMarks}</div>
              <div className="text-slate-500 dark:text-slate-400 text-sm">Total Score</div>
            </div>
            <div>
              <div className="text-4xl font-black text-slate-900 dark:text-white">{r.pct}%</div>
              <div className="text-slate-500 dark:text-slate-400 text-sm">Percentage</div>
            </div>
            <div>
              <div className="text-4xl font-black text-blue-600 dark:text-blue-400">#{r.rank}</div>
              <div className="text-slate-500 dark:text-slate-400 text-sm">Class Rank</div>
            </div>
          </div>

          <div className="space-y-3">
            <div className="text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider">Topic-wise Breakdown</div>
            {r.breakdown.map(b => (
              <div key={b.topic} className="flex items-center gap-3">
                <span className="text-slate-600 dark:text-slate-400 text-sm w-36 flex-shrink-0">{b.topic}</span>
                <div className="flex-1 h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                  <div className="h-full rounded-full" style={{ width: `${(b.scored / b.maxMarks) * 100}%`, background: b.scored / b.maxMarks >= 0.8 ? '#15803d' : b.scored / b.maxMarks >= 0.6 ? '#0369a1' : '#d97706' }} />
                </div>
                <span className="text-slate-700 dark:text-slate-300 font-semibold text-sm w-16 text-right">{b.scored}/{b.maxMarks}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* All Results Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
          <h2 className="text-slate-900 dark:text-white font-semibold">All Exam Results</h2>
        </div>
        <table className="w-full">
          <thead className="bg-slate-50 dark:bg-slate-800/50">
            <tr>{['Examination', 'Date', 'Score', 'Percentage', 'Rank', 'Result'].map(h => (
              <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider whitespace-nowrap">{h}</th>
            ))}</tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {[
              ...results,
              { exam: 'English Core — Essay Test', date: '05 May 2026', totalMarks: 30, scored: 28, pct: 93.3, rank: 3, breakdown: [] },
              { exam: 'Computer Sci. — Lab Test', date: '01 May 2026', totalMarks: 50, scored: 47, pct: 94.0, rank: 4, breakdown: [] },
            ].map((e, i) => (
              <tr key={i} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <td className="px-5 py-3.5 text-slate-900 dark:text-slate-100 font-medium text-sm">{e.exam}</td>
                <td className="px-5 py-3.5 text-slate-500 dark:text-slate-400 text-sm">{e.date}</td>
                <td className="px-5 py-3.5 text-slate-700 dark:text-slate-300 font-semibold text-sm">{e.scored}/{e.totalMarks}</td>
                <td className="px-5 py-3.5">
                  <span className={`text-sm font-semibold ${e.pct >= 85 ? 'text-green-600 dark:text-green-400' : e.pct >= 70 ? 'text-blue-600 dark:text-blue-400' : 'text-amber-600 dark:text-amber-400'}`}>{e.pct}%</span>
                </td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">#{e.rank}</td>
                <td className="px-5 py-3.5">
                  <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-semibold ${e.pct >= 33 ? 'bg-green-50 dark:bg-green-950/30 text-green-700 dark:text-green-400' : 'bg-red-50 dark:bg-red-950/30 text-red-700 dark:text-red-400'}`}>
                    {e.pct >= 33 ? <CheckCircle className="w-3 h-3" /> : <XCircle className="w-3 h-3" />}
                    {e.pct >= 33 ? 'Pass' : 'Fail'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
