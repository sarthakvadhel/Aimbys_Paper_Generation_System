import { useState } from 'react';
import { CheckCircle, ChevronLeft, ChevronRight, Star } from 'lucide-react';
import { AimbysState } from '../../../App';

const submissions = [
  {
    student: 'Riya Patel', rollNo: 'XII-A-021', exam: 'Mathematics XII — Unit Test 3',
    answers: [
      {
        qno: 1, text: 'Prove that sin(A+B) = sinA·cosB + cosA·sinB.',
        maxMarks: 8,
        response: 'Consider the unit circle with angle A and B. We place a point P at angle (A+B). Using the rotation matrix approach, the x-coordinate of P is cos(A+B) = cosA·cosB − sinA·sinB and y-coordinate gives sin(A+B) = sinA·cosB + cosA·sinB. This can be verified by multiplying the rotation matrices for angle A and angle B separately and combining them to get the rotation matrix for (A+B).',
        rubric: [
          { criterion: 'Correct setup / diagram', maxPts: 2 },
          { criterion: 'Derivation steps shown', maxPts: 4 },
          { criterion: 'Correct conclusion', maxPts: 2 },
        ],
      },
      {
        qno: 2, text: 'Evaluate ∫ x·sin(x) dx using integration by parts.',
        maxMarks: 6,
        response: 'Using integration by parts: let u = x, dv = sin(x)dx. Then du = dx, v = −cos(x). ∫x·sin(x)dx = x·(−cos x) − ∫(−cos x)dx = −x·cos(x) + sin(x) + C.',
        rubric: [
          { criterion: 'Correct choice of u and dv', maxPts: 2 },
          { criterion: 'Integration by parts formula applied', maxPts: 2 },
          { criterion: 'Final answer with constant', maxPts: 2 },
        ],
      },
    ],
  },
];

export function EvaluationDesk(_props: AimbysState) {
  const [subIdx, setSubIdx] = useState(0);
  const [ansIdx, setAnsIdx] = useState(0);
  const [scores, setScores] = useState<Record<string, Record<number, number>>>({});
  const [feedback, setFeedback] = useState<Record<string, string>>({});

  const sub = submissions[subIdx];
  const ans = sub.answers[ansIdx];
  const subKey = `${sub.student}-${ansIdx}`;

  const rubricScores = scores[subKey] || {};
  const totalForQ = Object.values(rubricScores).reduce((s, v) => s + v, 0);
  const allAnswersTotal = sub.answers.reduce((s, a, i) => {
    const k = `${sub.student}-${i}`;
    return s + (scores[k] ? Object.values(scores[k]).reduce((ss, v) => ss + v, 0) : 0);
  }, 0);

  return (
    <div className="space-y-4 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Teacher / Evaluation Desk</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">Evaluation Desk</h1>
          <div className="flex items-center gap-2">
            <button onClick={() => setSubIdx(Math.max(0, subIdx - 1))} disabled={subIdx === 0} className="p-1.5 rounded border border-slate-200 dark:border-slate-700 text-slate-500 disabled:opacity-40"><ChevronLeft className="w-4 h-4" /></button>
            <span className="text-slate-600 dark:text-slate-400 text-sm px-2">Student {subIdx + 1} / {submissions.length}</span>
            <button onClick={() => setSubIdx(Math.min(submissions.length - 1, subIdx + 1))} disabled={subIdx === submissions.length - 1} className="p-1.5 rounded border border-slate-200 dark:border-slate-700 text-slate-500 disabled:opacity-40"><ChevronRight className="w-4 h-4" /></button>
          </div>
        </div>
      </div>

      {/* Student Info Bar */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg px-5 py-3.5 flex flex-wrap items-center gap-4">
        <div>
          <div className="text-slate-900 dark:text-white font-semibold">{sub.student}</div>
          <div className="text-slate-400 text-xs">Roll: {sub.rollNo} · {sub.exam}</div>
        </div>
        <div className="ml-auto flex items-center gap-4">
          <div>
            <div className="text-xs text-slate-400">Questions</div>
            <div className="text-slate-900 dark:text-white font-bold text-lg">{ansIdx + 1}/{sub.answers.length}</div>
          </div>
          <div>
            <div className="text-xs text-slate-400">Marks So Far</div>
            <div className="font-bold text-lg" style={{ color: '#0369a1' }}>{allAnswersTotal}/{sub.answers.reduce((s, a) => s + a.maxMarks, 0)}</div>
          </div>
        </div>
      </div>

      {/* Answer Tabs */}
      <div className="flex gap-2">
        {sub.answers.map((a, i) => (
          <button key={i} onClick={() => setAnsIdx(i)} className={`px-3 py-1.5 rounded text-sm font-medium transition-colors ${ansIdx === i ? 'text-white' : 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-50'}`} style={ansIdx === i ? { background: '#0369a1' } : {}}>
            Q{a.qno} — {a.maxMarks}M
          </button>
        ))}
      </div>

      {/* Split Panel */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
        {/* Student Answer */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
            <h2 className="text-slate-900 dark:text-white font-semibold text-sm">Question {ans.qno}</h2>
            <p className="text-slate-500 dark:text-slate-400 text-sm mt-1">{ans.text}</p>
          </div>
          <div className="p-5">
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-3">Student Response</div>
            <div className="bg-slate-50 dark:bg-slate-800/50 rounded border border-slate-200 dark:border-slate-700 p-4 text-slate-700 dark:text-slate-300 text-sm leading-relaxed">
              {ans.response}
            </div>
          </div>
        </div>

        {/* Rubric Scoring */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
            <h2 className="text-slate-900 dark:text-white font-semibold text-sm">Rubric Scoring</h2>
            <span className="font-bold text-slate-900 dark:text-white">{totalForQ} / {ans.maxMarks}</span>
          </div>
          <div className="p-5 space-y-4">
            {ans.rubric.map((r, ri) => {
              const pts = rubricScores[ri] ?? 0;
              return (
                <div key={ri} className="border border-slate-200 dark:border-slate-700 rounded-lg p-4">
                  <div className="flex items-center justify-between mb-3">
                    <span className="text-slate-800 dark:text-slate-200 text-sm font-medium">{r.criterion}</span>
                    <span className="text-slate-500 dark:text-slate-400 text-xs">Max: {r.maxPts}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    {Array.from({ length: r.maxPts + 1 }, (_, v) => (
                      <button key={v} onClick={() => setScores(prev => ({ ...prev, [subKey]: { ...rubricScores, [ri]: v } }))}
                        className={`w-8 h-8 rounded text-sm font-bold transition-all ${pts === v ? 'text-white shadow-md' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'}`}
                        style={pts === v ? { background: '#0369a1' } : {}}>
                        {v}
                      </button>
                    ))}
                  </div>
                  <div className="flex gap-1 mt-3">
                    {[1, 2, 3, 4, 5].map(s => (
                      <Star key={s} className="w-4 h-4 text-slate-200 dark:text-slate-700 cursor-pointer hover:text-amber-400 transition-colors" fill={s <= Math.ceil(pts / r.maxPts * 5) ? '#f59e0b' : 'transparent'} />
                    ))}
                    <span className="text-xs text-slate-400 ml-2">Criterion quality</span>
                  </div>
                </div>
              );
            })}

            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-400 mb-2 block">Examiner Feedback</label>
              <textarea
                value={feedback[subKey] || ''}
                onChange={e => setFeedback(prev => ({ ...prev, [subKey]: e.target.value }))}
                rows={3}
                placeholder="Add feedback or remarks for this question..."
                className="w-full px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-slate-800 dark:text-slate-200 text-sm outline-none resize-none focus:border-sky-400"
              />
            </div>

            <div className="flex gap-2">
              {ansIdx < sub.answers.length - 1 ? (
                <button onClick={() => setAnsIdx(ansIdx + 1)} className="flex-1 py-2.5 text-white rounded text-sm font-medium" style={{ background: '#0369a1' }}>Next Answer →</button>
              ) : (
                <button className="flex-1 py-2.5 text-white rounded text-sm font-medium flex items-center justify-center gap-2 bg-green-600 hover:bg-green-700 transition-colors">
                  <CheckCircle className="w-4 h-4" />Submit Evaluation
                </button>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
