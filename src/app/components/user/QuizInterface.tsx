import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import { X, Bookmark, Flag, ChevronLeft, ChevronRight, Clock, CheckCircle, Circle, AlertCircle } from 'lucide-react';
import { AppState } from '../../App';

const questions = [
  {
    id: 1, question: 'What does JSX stand for in React development?',
    options: ['JavaScript XML', 'Java Syntax Extension', 'JavaScript Expression', 'JSON XML Syntax'],
    correct: 0,
    explanation: 'JSX stands for JavaScript XML. It is a syntax extension for JavaScript that allows you to write HTML-like code within JavaScript files.',
  },
  {
    id: 2, question: 'Which React hook is used for managing local component state?',
    options: ['useEffect', 'useState', 'useRef', 'useContext'],
    correct: 1,
    explanation: 'useState is the primary hook for managing local state in a React functional component. It returns a state value and a setter function.',
  },
  {
    id: 3, question: 'What happens when you pass an empty array [] as the second argument to useEffect?',
    options: ['Effect runs on every render', 'Effect never runs', 'Effect runs once after initial render', 'Effect runs only on unmount'],
    correct: 2,
    explanation: 'An empty dependency array tells React to run the effect only once after the initial render, similar to componentDidMount in class components.',
  },
  {
    id: 4, question: 'What is the virtual DOM in React?',
    options: ['A real browser DOM instance', 'A lightweight in-memory representation of the real DOM', 'A CSS rendering engine', 'A JavaScript build tool'],
    correct: 1,
    explanation: 'The virtual DOM is a lightweight JavaScript representation of the actual DOM. React uses it to efficiently update the real DOM by computing the minimal diff.',
  },
  {
    id: 5, question: 'Which of the following correctly describes prop drilling?',
    options: ['Passing props through many nested component levels', 'Creating holes in component boundaries', 'Using refs to access child components', 'Directly mutating props in child components'],
    correct: 0,
    explanation: 'Prop drilling refers to the process of passing data through multiple layers of components, even when intermediate components do not need the data.',
  },
  {
    id: 6, question: 'What is the purpose of React.memo()?',
    options: ['To create memoized selector functions', 'To prevent re-renders if props haven\'t changed', 'To cache API responses', 'To manage side effects'],
    correct: 1,
    explanation: 'React.memo() is a higher-order component that prevents unnecessary re-renders of a component when its props have not changed, acting as a performance optimization.',
  },
  {
    id: 7, question: 'Which hook would you use to perform side effects in a function component?',
    options: ['useState', 'useCallback', 'useEffect', 'useMemo'],
    correct: 2,
    explanation: 'useEffect is specifically designed for performing side effects in function components, such as data fetching, subscriptions, or manually changing the DOM.',
  },
  {
    id: 8, question: 'What does the "key" prop do when rendering lists in React?',
    options: ['Styles each list item uniquely', 'Helps React identify which items changed', 'Sets the display order', 'Creates unique IDs for CSS targeting'],
    correct: 1,
    explanation: 'The key prop helps React identify which items in a list have changed, been added, or been removed. It enables efficient reconciliation of list items.',
  },
];

const optionLabels = ['A', 'B', 'C', 'D'];
const TOTAL_TIME = 30;

export function QuizInterface({ setView, quizAnswers, setQuizAnswers, quizStarted, setQuizStarted, selectedQuizId }: AppState) {
  const [currentQ, setCurrentQ] = useState(0);
  const [bookmarked, setBookmarked] = useState<Set<number>>(new Set());
  const [flagged, setFlagged] = useState<Set<number>>(new Set());
  const [timeLeft, setTimeLeft] = useState(TOTAL_TIME);
  const [timerActive, setTimerActive] = useState(false);
  const [showConfirmExit, setShowConfirmExit] = useState(false);
  const [direction, setDirection] = useState(1);

  const question = questions[currentQ];
  const totalQuestions = questions.length;
  const answered = Object.keys(quizAnswers).length;

  useEffect(() => {
    if (!quizStarted) {
      setQuizStarted(true);
      setTimerActive(true);
    }
  }, []);

  useEffect(() => {
    if (!timerActive) return;
    if (timeLeft <= 0) {
      handleNextQuestion();
      return;
    }
    const t = setTimeout(() => setTimeLeft(prev => prev - 1), 1000);
    return () => clearTimeout(t);
  }, [timeLeft, timerActive]);

  const handleSelectOption = (optionIdx: number) => {
    setQuizAnswers(prev => ({ ...prev, [currentQ]: optionIdx }));
  };

  const handleNextQuestion = () => {
    if (currentQ < totalQuestions - 1) {
      setDirection(1);
      setCurrentQ(prev => prev + 1);
      setTimeLeft(TOTAL_TIME);
    } else {
      setView('user-results');
    }
  };

  const handlePrevQuestion = () => {
    if (currentQ > 0) {
      setDirection(-1);
      setCurrentQ(prev => prev - 1);
      setTimeLeft(TOTAL_TIME);
    }
  };

  const handleSubmit = () => {
    setView('user-results');
  };

  const progressPct = ((currentQ + 1) / totalQuestions) * 100;
  const timerPct = (timeLeft / TOTAL_TIME) * 100;
  const timerColor = timeLeft > 15 ? 'text-emerald-500' : timeLeft > 8 ? 'text-amber-500' : 'text-rose-500';
  const timerBg = timeLeft > 15 ? 'stroke-emerald-500' : timeLeft > 8 ? 'stroke-amber-500' : 'stroke-rose-500';
  const r = 28;
  const circ = 2 * Math.PI * r;

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-indigo-50/30 to-violet-50/30 dark:from-slate-950 dark:via-indigo-950/10 dark:to-violet-950/10 flex flex-col">
      {/* Confirm Exit Modal */}
      <AnimatePresence>
        {showConfirmExit && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4"
          >
            <motion.div
              initial={{ scale: 0.9, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.9, opacity: 0 }}
              className="bg-white dark:bg-slate-900 rounded-3xl p-8 max-w-sm w-full border border-slate-200 dark:border-slate-700 shadow-2xl"
            >
              <div className="w-14 h-14 bg-amber-100 dark:bg-amber-950/40 rounded-2xl flex items-center justify-center mx-auto mb-4">
                <AlertCircle className="w-7 h-7 text-amber-600 dark:text-amber-400" />
              </div>
              <h3 className="text-xl font-black text-slate-900 dark:text-white text-center mb-2">Exit Quiz?</h3>
              <p className="text-slate-500 dark:text-slate-400 text-center mb-6">Your progress will be lost. Are you sure?</p>
              <div className="grid grid-cols-2 gap-3">
                <button onClick={() => setShowConfirmExit(false)} className="py-3 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded-xl font-semibold hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
                  Continue
                </button>
                <button onClick={() => setView('user-home')} className="py-3 bg-rose-500 text-white rounded-xl font-semibold hover:bg-rose-600 transition-colors">
                  Exit
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Header */}
      <div className="px-4 md:px-8 pt-6 pb-4">
        <div className="max-w-3xl mx-auto flex items-center justify-between">
          <button
            onClick={() => setShowConfirmExit(true)}
            className="flex items-center gap-2 text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors"
          >
            <X className="w-5 h-5" />
            <span className="hidden sm:block">Exit</span>
          </button>

          <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400">
            <span className="font-semibold">{currentQ + 1}</span>
            <span>/</span>
            <span>{totalQuestions}</span>
          </div>

          {/* Circular Timer */}
          <div className="relative w-16 h-16">
            <svg className="w-16 h-16 -rotate-90" viewBox="0 0 72 72">
              <circle cx="36" cy="36" r={r} fill="none" className="stroke-slate-200 dark:stroke-slate-800" strokeWidth="4" />
              <circle cx="36" cy="36" r={r} fill="none" className={timerBg} strokeWidth="4" strokeLinecap="round"
                strokeDasharray={circ}
                strokeDashoffset={circ * (1 - timerPct / 100)}
                style={{ transition: 'stroke-dashoffset 1s linear' }}
              />
            </svg>
            <div className={`absolute inset-0 flex items-center justify-center font-black text-lg ${timerColor}`}>
              {timeLeft}
            </div>
          </div>
        </div>

        {/* Progress Bar */}
        <div className="max-w-3xl mx-auto mt-4">
          <div className="h-2 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
            <motion.div
              className="h-full bg-gradient-to-r from-indigo-500 to-violet-600 rounded-full"
              initial={false}
              animate={{ width: `${progressPct}%` }}
              transition={{ duration: 0.4 }}
            />
          </div>
        </div>

        {/* Question dots */}
        <div className="max-w-3xl mx-auto mt-3 flex items-center gap-1 flex-wrap">
          {questions.map((_, i) => (
            <button
              key={i}
              onClick={() => { setCurrentQ(i); setTimeLeft(TOTAL_TIME); }}
              className={`w-6 h-6 rounded-full transition-all text-xs font-bold flex items-center justify-center ${
                i === currentQ ? 'bg-indigo-500 text-white scale-110' :
                quizAnswers[i] !== undefined ? 'bg-emerald-500 text-white' :
                flagged.has(i) ? 'bg-amber-500 text-white' :
                'bg-slate-200 dark:bg-slate-800 text-slate-500 dark:text-slate-500'
              }`}
            >
              {i + 1}
            </button>
          ))}
        </div>
      </div>

      {/* Question Card */}
      <div className="flex-1 px-4 md:px-8 flex flex-col">
        <div className="max-w-3xl mx-auto w-full flex-1 flex flex-col">
          <AnimatePresence mode="wait" custom={direction}>
            <motion.div
              key={currentQ}
              custom={direction}
              initial={{ opacity: 0, x: direction * 60 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: direction * -60 }}
              transition={{ duration: 0.25 }}
              className="flex-1 flex flex-col"
            >
              {/* Question */}
              <div className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-6 md:p-8 mb-4 shadow-sm">
                <div className="flex items-start justify-between gap-4 mb-6">
                  <div>
                    <span className="text-indigo-600 dark:text-indigo-400 font-semibold text-sm mb-2 block">Question {currentQ + 1}</span>
                    <h2 className="text-slate-900 dark:text-white font-bold leading-relaxed">{question.question}</h2>
                  </div>
                  <div className="flex items-center gap-2 flex-shrink-0">
                    <button
                      onClick={() => setBookmarked(prev => { const n = new Set(prev); n.has(currentQ) ? n.delete(currentQ) : n.add(currentQ); return n; })}
                      className={`p-2 rounded-xl transition-all ${bookmarked.has(currentQ) ? 'bg-indigo-100 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400' : 'text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}`}
                    >
                      <Bookmark className={`w-4 h-4 ${bookmarked.has(currentQ) ? 'fill-indigo-600 dark:fill-indigo-400' : ''}`} />
                    </button>
                    <button
                      onClick={() => setFlagged(prev => { const n = new Set(prev); n.has(currentQ) ? n.delete(currentQ) : n.add(currentQ); return n; })}
                      className={`p-2 rounded-xl transition-all ${flagged.has(currentQ) ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-600 dark:text-amber-400' : 'text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'}`}
                    >
                      <Flag className={`w-4 h-4 ${flagged.has(currentQ) ? 'fill-amber-600 dark:fill-amber-400' : ''}`} />
                    </button>
                  </div>
                </div>

                {/* Options */}
                <div className="grid grid-cols-1 gap-3">
                  {question.options.map((option, idx) => {
                    const selected = quizAnswers[currentQ] === idx;
                    return (
                      <motion.button
                        key={idx}
                        whileHover={{ scale: 1.005 }}
                        whileTap={{ scale: 0.995 }}
                        onClick={() => handleSelectOption(idx)}
                        className={`flex items-center gap-4 p-4 rounded-2xl border-2 text-left transition-all ${
                          selected
                            ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-950/30'
                            : 'border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 hover:border-indigo-300 dark:hover:border-indigo-700 hover:bg-indigo-50/50 dark:hover:bg-indigo-950/10'
                        }`}
                      >
                        <div className={`w-8 h-8 rounded-xl flex items-center justify-center font-bold text-sm flex-shrink-0 transition-all ${
                          selected ? 'bg-indigo-500 text-white' : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-400 border border-slate-300 dark:border-slate-600'
                        }`}>
                          {optionLabels[idx]}
                        </div>
                        <span className={`font-medium flex-1 ${selected ? 'text-indigo-700 dark:text-indigo-300' : 'text-slate-700 dark:text-slate-300'}`}>
                          {option}
                        </span>
                        {selected && (
                          <motion.div
                            initial={{ scale: 0 }}
                            animate={{ scale: 1 }}
                          >
                            <CheckCircle className="w-5 h-5 text-indigo-500 flex-shrink-0" />
                          </motion.div>
                        )}
                      </motion.button>
                    );
                  })}
                </div>
              </div>
            </motion.div>
          </AnimatePresence>

          {/* Navigation */}
          <div className="pb-6 flex items-center justify-between gap-4">
            <button
              onClick={handlePrevQuestion}
              disabled={currentQ === 0}
              className="flex items-center gap-2 px-5 py-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
            >
              <ChevronLeft className="w-4 h-4" />
              Previous
            </button>

            <div className="text-center">
              <span className="text-slate-500 dark:text-slate-400 text-sm">{answered}/{totalQuestions} answered</span>
            </div>

            {currentQ === totalQuestions - 1 ? (
              <button
                onClick={handleSubmit}
                className="flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-emerald-500 to-emerald-700 text-white rounded-xl font-semibold hover:from-emerald-600 hover:to-emerald-800 transition-all shadow-lg shadow-emerald-500/25"
              >
                Submit Quiz
                <CheckCircle className="w-4 h-4" />
              </button>
            ) : (
              <button
                onClick={handleNextQuestion}
                className="flex items-center gap-2 px-5 py-3 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl font-semibold hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25"
              >
                Next
                <ChevronRight className="w-4 h-4" />
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
