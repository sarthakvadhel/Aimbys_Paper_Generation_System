import { useState, useEffect } from 'react';
import { Flag, ChevronLeft, ChevronRight, X, AlertCircle } from 'lucide-react';
import { AimbysState } from '../../../App';

const questions = [
  {
    id: 1, type: 'MCQ', marks: 4, text: 'Find the intervals where f\'(x) > 0 for f(x) = x³ − 3x² + 2x.',
    options: ['(0, 1) and (2, ∞)', '(−∞, 0) and (1, 2)', '(−∞, 1) only', '(1, ∞) only'],
  },
  {
    id: 2, type: 'MCQ', marks: 3, text: 'If A and B are mutually exclusive events, then P(A ∪ B) equals:',
    options: ['P(A) · P(B)', 'P(A) + P(B) − P(A ∩ B)', 'P(A) + P(B)', 'P(A) / P(B)'],
  },
  {
    id: 3, type: 'MCQ', marks: 2, text: 'If det(A) = 5 for a 3×3 matrix A, what is det(2A)?',
    options: ['10', '40', '25', '80'],
  },
  {
    id: 4, type: 'Descriptive', marks: 8, text: 'Prove that sin(A+B) = sinA·cosB + cosA·sinB. Show all steps clearly.',
    options: [],
  },
  {
    id: 5, type: 'MCQ', marks: 1, text: 'A fair coin is tossed. What is the probability of getting heads?',
    options: ['1', '0', '1/2', '1/4'],
  },
  {
    id: 6, type: 'Descriptive', marks: 6, text: 'Evaluate ∫ x·sin(x) dx using integration by parts.',
    options: [],
  },
];

export function ExamInterface(props: AimbysState) {
  const { setExamActive } = props;
  const [currentQ, setCurrentQ] = useState(0);
  const [answers, setAnswers] = useState<Record<number, string>>({});
  const [flagged, setFlagged] = useState<Set<number>>(new Set());
  const [timeLeft, setTimeLeft] = useState(3 * 60 * 60); // 3h
  const [showSubmit, setShowSubmit] = useState(false);

  useEffect(() => {
    const t = setInterval(() => setTimeLeft(s => Math.max(0, s - 1)), 1000);
    return () => clearInterval(t);
  }, []);

  const h = Math.floor(timeLeft / 3600);
  const m = Math.floor((timeLeft % 3600) / 60);
  const s = timeLeft % 60;
  const timeStr = `${h}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  const timeUrgent = timeLeft < 15 * 60;

  const q = questions[currentQ];
  const answered = Object.keys(answers).length;

  if (showSubmit) {
    const total = questions.reduce((s, q) => s + q.marks, 0);
    return (
      <div className="fixed inset-0 flex items-center justify-center z-50" style={{ background: 'rgba(13,27,46,0.95)' }}>
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-8 max-w-md w-full mx-4 text-center shadow-2xl">
          <div className="w-16 h-16 rounded-full bg-green-100 dark:bg-green-950/40 flex items-center justify-center mx-auto mb-4">
            <AlertCircle className="w-8 h-8 text-green-600 dark:text-green-400" />
          </div>
          <h2 className="text-slate-900 dark:text-white font-bold text-xl mb-2">Submit Exam?</h2>
          <p className="text-slate-500 dark:text-slate-400 text-sm mb-6">
            You've answered {answered}/{questions.length} questions ({total} marks). Once submitted, you cannot change your answers.
          </p>
          {answered < questions.length && (
            <div className="bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 rounded p-3 mb-6 text-amber-700 dark:text-amber-400 text-sm text-left">
              {questions.length - answered} question(s) unanswered. Are you sure you want to submit?
            </div>
          )}
          <div className="flex gap-3">
            <button onClick={() => setShowSubmit(false)} className="flex-1 py-2.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded font-medium hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">Continue</button>
            <button onClick={() => { setExamActive(false); props.setView('std-results'); }} className="flex-1 py-2.5 bg-green-600 hover:bg-green-700 text-white rounded font-medium transition-colors">Submit</button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-screen overflow-hidden flex-col" style={{ background: '#f0f4f8' }}>
      {/* Exam Header */}
      <header className="h-14 flex items-center justify-between px-4 md:px-6 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex-shrink-0 z-20">
        <div className="text-slate-900 dark:text-white font-semibold text-sm">Mathematics XII — Unit Test 4</div>
        <div className="flex items-center gap-4">
          <div className={`font-mono font-bold text-lg ${timeUrgent ? 'text-red-600 dark:text-red-400' : 'text-slate-700 dark:text-slate-300'}`}>{timeStr}</div>
          <span className="text-slate-500 dark:text-slate-400 text-sm">{answered}/{questions.length} answered</span>
          <button onClick={() => setShowSubmit(true)} className="px-3 py-1.5 bg-green-600 hover:bg-green-700 text-white rounded text-sm font-medium transition-colors">Submit</button>
          <button onClick={() => setExamActive(false)} className="p-1.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 rounded hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"><X className="w-4 h-4" /></button>
        </div>
      </header>

      <div className="flex flex-1 overflow-hidden">
        {/* Question Panel */}
        <main className="flex-1 overflow-y-auto p-5 md:p-8">
          <div className="max-w-3xl mx-auto">
            <div className="flex items-center justify-between mb-5">
              <div className="flex items-center gap-3">
                <span className="text-slate-400 text-sm">Question {currentQ + 1} of {questions.length}</span>
                <span className={`px-2 py-0.5 rounded text-xs font-medium ${q.type === 'MCQ' ? 'bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-400' : 'bg-violet-50 dark:bg-violet-950/30 text-violet-700 dark:text-violet-400'}`}>{q.type}</span>
                <span className="text-slate-500 dark:text-slate-400 text-xs">{q.marks} marks</span>
              </div>
              <button onClick={() => setFlagged(prev => { const n = new Set(prev); n.has(currentQ) ? n.delete(currentQ) : n.add(currentQ); return n; })} className={`flex items-center gap-1.5 px-2.5 py-1.5 rounded text-xs font-medium transition-colors ${flagged.has(currentQ) ? 'bg-amber-100 dark:bg-amber-950/30 text-amber-700 dark:text-amber-400' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 hover:bg-amber-50'}`}>
                <Flag className="w-3.5 h-3.5" />{flagged.has(currentQ) ? 'Flagged' : 'Flag'}
              </button>
            </div>

            <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-6 mb-5">
              <p className="text-slate-900 dark:text-white text-base leading-relaxed">{q.text}</p>
            </div>

            {q.type === 'MCQ' ? (
              <div className="space-y-3">
                {q.options.map((opt, oi) => {
                  const letter = String.fromCharCode(65 + oi);
                  const selected = answers[currentQ] === opt;
                  return (
                    <button key={oi} onClick={() => setAnswers(prev => ({ ...prev, [currentQ]: opt }))}
                      className={`w-full flex items-start gap-3 p-4 rounded-lg border text-left transition-all ${selected ? 'border-green-500 dark:border-green-600' : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700'}`}
                      style={selected ? { background: '#f0fdf4', borderColor: '#16a34a' } : {}}>
                      <div className={`w-7 h-7 rounded-full border-2 flex items-center justify-center flex-shrink-0 text-sm font-bold transition-all ${selected ? 'border-green-600 bg-green-600 text-white' : 'border-slate-300 dark:border-slate-600 text-slate-500 dark:text-slate-400'}`}>
                        {letter}
                      </div>
                      <span className={`text-sm leading-relaxed mt-0.5 ${selected ? 'text-green-800 dark:text-green-300 font-medium' : 'text-slate-700 dark:text-slate-300'}`}>{opt}</span>
                    </button>
                  );
                })}
              </div>
            ) : (
              <div>
                <textarea
                  value={answers[currentQ] || ''}
                  onChange={e => setAnswers(prev => ({ ...prev, [currentQ]: e.target.value }))}
                  rows={8}
                  placeholder="Write your answer here..."
                  className="w-full px-4 py-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-slate-800 dark:text-slate-200 text-sm leading-relaxed outline-none resize-y focus:border-green-400"
                />
                <div className="text-slate-400 text-xs mt-1.5 text-right">{(answers[currentQ] || '').length} characters</div>
              </div>
            )}

            <div className="flex justify-between mt-6">
              <button onClick={() => setCurrentQ(Math.max(0, currentQ - 1))} disabled={currentQ === 0} className="flex items-center gap-2 px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-700 dark:text-slate-300 text-sm hover:bg-slate-50 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed"><ChevronLeft className="w-4 h-4" />Previous</button>
              <button onClick={() => setCurrentQ(Math.min(questions.length - 1, currentQ + 1))} disabled={currentQ === questions.length - 1} className="flex items-center gap-2 px-4 py-2.5 text-white rounded text-sm disabled:opacity-40 disabled:cursor-not-allowed" style={{ background: '#15803d' }}>Next<ChevronRight className="w-4 h-4" /></button>
            </div>
          </div>
        </main>

        {/* Question Navigator */}
        <aside className="w-52 flex-shrink-0 overflow-y-auto bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 hidden md:block">
          <div className="p-4">
            <div className="text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider mb-3">Question Navigator</div>
            <div className="grid grid-cols-4 gap-1.5 mb-4">
              {questions.map((q, i) => {
                const isAnswered = !!answers[i];
                const isFlagged = flagged.has(i);
                const isCurrent = currentQ === i;
                return (
                  <button key={i} onClick={() => setCurrentQ(i)}
                    className={`h-9 rounded text-sm font-semibold border-2 transition-all ${isCurrent ? 'border-green-600' : isFlagged ? 'border-amber-400' : isAnswered ? 'border-green-300 dark:border-green-800' : 'border-slate-200 dark:border-slate-700'}`}
                    style={{ background: isCurrent ? '#15803d' : isAnswered ? '#f0fdf4' : isFlagged ? '#fffbeb' : undefined, color: isCurrent ? 'white' : isAnswered ? '#15803d' : isFlagged ? '#b45309' : undefined }}>
                    {i + 1}
                  </button>
                );
              })}
            </div>
            <div className="space-y-2 text-xs">
              {[
                { color: 'bg-green-600', label: 'Current' },
                { color: 'bg-green-100 dark:bg-green-900 border border-green-300', label: 'Answered' },
                { color: 'bg-amber-100 dark:bg-amber-900 border border-amber-400', label: 'Flagged' },
                { color: 'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700', label: 'Not visited' },
              ].map(({ color, label }) => (
                <div key={label} className="flex items-center gap-2">
                  <div className={`w-5 h-5 rounded ${color}`} />
                  <span className="text-slate-500 dark:text-slate-400">{label}</span>
                </div>
              ))}
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
