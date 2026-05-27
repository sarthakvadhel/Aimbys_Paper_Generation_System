import { useState, useRef } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import {
  Upload, FileSpreadsheet, CheckCircle2, AlertCircle, Download,
  Eye, Trash2, RefreshCw, Info, X, Plus
} from 'lucide-react';
import { AppState } from '../../App';

const excelColumns = [
  { col: 'A', header: 'Question No.', example: '1', required: true },
  { col: 'B', header: 'Question Text', example: 'What is React?', required: true },
  { col: 'C', header: 'Option A', example: 'A JavaScript library', required: true },
  { col: 'D', header: 'Option B', example: 'A CSS framework', required: true },
  { col: 'E', header: 'Option C', example: 'A Python package', required: false },
  { col: 'F', header: 'Option D', example: 'A database', required: false },
  { col: 'G', header: 'Correct Answer', example: 'A', required: true },
  { col: 'H', header: 'Category', example: 'JavaScript', required: false },
  { col: 'I', header: 'Difficulty', example: 'Medium', required: false },
  { col: 'J', header: 'Explanation', example: 'React is built by Meta...', required: false },
];

const samplePreview = [
  { no: 1, question: 'What does JSX stand for?', options: ['JavaScript XML', 'Java Syntax Extension', 'JS eXpress', 'None'], correct: 'A', difficulty: 'Easy', category: 'React' },
  { no: 2, question: 'Which hook manages local state?', options: ['useEffect', 'useState', 'useRef', 'useMemo'], correct: 'B', difficulty: 'Easy', category: 'React' },
  { no: 3, question: 'What is the virtual DOM?', options: ['A real DOM copy', 'A lightweight memory rep.', 'A browser API', 'CSS engine'], correct: 'B', difficulty: 'Medium', category: 'React' },
  { no: 4, question: 'When does useEffect with [] run?', options: ['Every render', 'Never', 'Once on mount', 'On unmount'], correct: 'C', difficulty: 'Medium', category: 'React Hooks' },
  { no: 5, question: 'What is prop drilling?', options: ['Passing props deep', 'Drilling APIs', 'DOM drilling', 'Event bubbling'], correct: 'A', difficulty: 'Medium', category: 'React' },
];

type UploadState = 'idle' | 'dragging' | 'uploading' | 'parsing' | 'preview' | 'success';

export function QuizUpload({ setView }: AppState) {
  const [uploadState, setUploadState] = useState<UploadState>('idle');
  const [progress, setProgress] = useState(0);
  const [fileName, setFileName] = useState('');
  const [showFormat, setShowFormat] = useState(false);
  const [quizTitle, setQuizTitle] = useState('React Fundamentals Q1 2026');
  const [quizCategory, setQuizCategory] = useState('React');
  const [timeLimitMin, setTimeLimitMin] = useState('20');
  const fileRef = useRef<HTMLInputElement>(null);

  const simulateUpload = (name: string) => {
    setFileName(name);
    setUploadState('uploading');
    setProgress(0);
    const interval = setInterval(() => {
      setProgress(p => {
        if (p >= 100) {
          clearInterval(interval);
          setUploadState('parsing');
          setTimeout(() => setUploadState('preview'), 800);
          return 100;
        }
        return p + Math.random() * 15 + 5;
      });
    }, 150);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setUploadState('idle');
    const file = e.dataTransfer.files[0];
    if (file) simulateUpload(file.name);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) simulateUpload(file.name);
  };

  const handlePublish = () => {
    setUploadState('success');
    setTimeout(() => setView('admin-quizzes'), 2000);
  };

  const difficultyColor = (d: string) =>
    d === 'Easy' ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400' :
    d === 'Medium' ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400' :
    'bg-rose-100 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400';

  return (
    <div className="max-w-5xl space-y-6 pb-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white">Upload Quiz</h1>
          <p className="text-slate-500 dark:text-slate-400">Import questions from an Excel file</p>
        </div>
        <button
          onClick={() => setShowFormat(!showFormat)}
          className="flex items-center gap-2 px-4 py-2.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
        >
          <Info className="w-4 h-4" />
          Format Guide
        </button>
      </div>

      {/* Format Guide */}
      <AnimatePresence>
        {showFormat && (
          <motion.div
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6"
          >
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-bold text-slate-900 dark:text-white">Excel File Format</h3>
              <div className="flex items-center gap-2">
                <button className="flex items-center gap-2 px-3 py-1.5 bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400 rounded-lg text-sm hover:bg-emerald-200 dark:hover:bg-emerald-950/60 transition-colors">
                  <Download className="w-3.5 h-3.5" />
                  Download Template
                </button>
                <button onClick={() => setShowFormat(false)} className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200">
                  <X className="w-4 h-4" />
                </button>
              </div>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 dark:border-slate-800">
                    <th className="text-left py-2 pr-4 text-slate-500 dark:text-slate-400 font-medium">Column</th>
                    <th className="text-left py-2 pr-4 text-slate-500 dark:text-slate-400 font-medium">Header</th>
                    <th className="text-left py-2 pr-4 text-slate-500 dark:text-slate-400 font-medium">Example</th>
                    <th className="text-left py-2 text-slate-500 dark:text-slate-400 font-medium">Required</th>
                  </tr>
                </thead>
                <tbody>
                  {excelColumns.map(({ col, header, example, required }) => (
                    <tr key={col} className="border-b border-slate-100 dark:border-slate-800/50">
                      <td className="py-2 pr-4">
                        <span className="inline-flex items-center justify-center w-7 h-7 bg-indigo-100 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-400 rounded-lg font-bold">{col}</span>
                      </td>
                      <td className="py-2 pr-4 text-slate-900 dark:text-slate-100 font-medium">{header}</td>
                      <td className="py-2 pr-4 text-slate-500 dark:text-slate-400 font-mono text-xs">{example}</td>
                      <td className="py-2">
                        {required
                          ? <span className="px-2 py-0.5 bg-rose-100 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400 rounded-full text-xs font-medium">Required</span>
                          : <span className="px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 rounded-full text-xs">Optional</span>
                        }
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Upload Area */}
      {(uploadState === 'idle' || uploadState === 'dragging') && (
        <motion.div
          onDragOver={e => { e.preventDefault(); setUploadState('dragging'); }}
          onDragLeave={() => setUploadState('idle')}
          onDrop={handleDrop}
          onClick={() => fileRef.current?.click()}
          className={`relative border-2 border-dashed rounded-3xl p-16 text-center cursor-pointer transition-all ${
            uploadState === 'dragging'
              ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-950/20 scale-[1.01]'
              : 'border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 hover:border-indigo-400 hover:bg-indigo-50/50 dark:hover:bg-indigo-950/10'
          }`}
        >
          <input ref={fileRef} type="file" accept=".xlsx,.xls,.csv" className="hidden" onChange={handleFileChange} />
          <div className={`w-20 h-20 mx-auto mb-6 rounded-2xl flex items-center justify-center ${
            uploadState === 'dragging' ? 'bg-indigo-100 dark:bg-indigo-950/50' : 'bg-slate-100 dark:bg-slate-800'
          }`}>
            <FileSpreadsheet className={`w-10 h-10 ${uploadState === 'dragging' ? 'text-indigo-600 dark:text-indigo-400' : 'text-slate-400 dark:text-slate-600'}`} />
          </div>
          <h3 className="text-slate-900 dark:text-white font-bold mb-2">
            {uploadState === 'dragging' ? 'Release to upload' : 'Drop your Excel file here'}
          </h3>
          <p className="text-slate-500 dark:text-slate-400 mb-4">or click to browse • Supports .xlsx, .xls, .csv</p>
          <button className="inline-flex items-center gap-2 px-6 py-3 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25">
            <Upload className="w-4 h-4" />
            Choose File
          </button>
        </motion.div>
      )}

      {/* Progress */}
      {(uploadState === 'uploading' || uploadState === 'parsing') && (
        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-8 text-center">
          <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-indigo-100 dark:bg-indigo-950/40 flex items-center justify-center">
            {uploadState === 'parsing'
              ? <RefreshCw className="w-10 h-10 text-indigo-600 dark:text-indigo-400 animate-spin" />
              : <FileSpreadsheet className="w-10 h-10 text-indigo-600 dark:text-indigo-400" />
            }
          </div>
          <h3 className="text-slate-900 dark:text-white font-bold mb-1">
            {uploadState === 'parsing' ? 'Parsing questions...' : `Uploading ${fileName}`}
          </h3>
          <p className="text-slate-500 dark:text-slate-400 mb-6">
            {uploadState === 'parsing' ? 'Extracting and validating all questions' : `${Math.round(Math.min(progress, 100))}% complete`}
          </p>
          <div className="max-w-sm mx-auto">
            <div className="h-3 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
              <motion.div
                className="h-full bg-gradient-to-r from-indigo-500 to-violet-600 rounded-full"
                initial={{ width: 0 }}
                animate={{ width: `${Math.min(progress, 100)}%` }}
                transition={{ ease: 'linear' }}
              />
            </div>
          </div>
        </div>
      )}

      {/* Preview */}
      {uploadState === 'preview' && (
        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} className="space-y-6">
          {/* Quiz Settings */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
            <div className="flex items-center gap-3 mb-6">
              <div className="w-10 h-10 bg-emerald-100 dark:bg-emerald-950/40 rounded-xl flex items-center justify-center">
                <CheckCircle2 className="w-5 h-5 text-emerald-600 dark:text-emerald-400" />
              </div>
              <div>
                <h3 className="font-bold text-slate-900 dark:text-white">Upload successful — {samplePreview.length} questions detected</h3>
                <p className="text-slate-500 dark:text-slate-400 text-sm">Configure quiz settings before publishing</p>
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-slate-700 dark:text-slate-300 text-sm mb-1.5">Quiz Title</label>
                <input
                  value={quizTitle}
                  onChange={e => setQuizTitle(e.target.value)}
                  className="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
                />
              </div>
              <div>
                <label className="block text-slate-700 dark:text-slate-300 text-sm mb-1.5">Category</label>
                <select
                  value={quizCategory}
                  onChange={e => setQuizCategory(e.target.value)}
                  className="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
                >
                  {['React', 'JavaScript', 'Python', 'Algorithms', 'System Design', 'DevOps'].map(c => (
                    <option key={c} value={c}>{c}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-slate-700 dark:text-slate-300 text-sm mb-1.5">Time Limit (min)</label>
                <input
                  type="number"
                  value={timeLimitMin}
                  onChange={e => setTimeLimitMin(e.target.value)}
                  className="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
                />
              </div>
            </div>
          </div>

          {/* Question Preview Table */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
            <div className="p-6 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
              <h3 className="font-bold text-slate-900 dark:text-white">Questions Preview</h3>
              <span className="text-slate-500 dark:text-slate-400 text-sm">Showing {samplePreview.length} of {samplePreview.length}</span>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-slate-50 dark:bg-slate-800/50">
                  <tr>
                    {['#', 'Question', 'Options', 'Answer', 'Category', 'Difficulty', ''].map(h => (
                      <th key={h} className="text-left px-4 py-3 text-slate-500 dark:text-slate-400 text-sm font-semibold">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {samplePreview.map(q => (
                    <tr key={q.no} className="border-t border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                      <td className="px-4 py-3 text-slate-500 dark:text-slate-400 text-sm">{q.no}</td>
                      <td className="px-4 py-3 text-slate-900 dark:text-slate-100 text-sm max-w-xs truncate">{q.question}</td>
                      <td className="px-4 py-3 text-slate-500 dark:text-slate-400 text-sm">{q.options.length} options</td>
                      <td className="px-4 py-3">
                        <span className="w-7 h-7 inline-flex items-center justify-center bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400 rounded-lg font-bold text-sm">{q.correct}</span>
                      </td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300 text-sm">{q.category}</td>
                      <td className="px-4 py-3">
                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${difficultyColor(q.difficulty)}`}>{q.difficulty}</span>
                      </td>
                      <td className="px-4 py-3">
                        <button className="p-1.5 text-slate-400 hover:text-rose-500 dark:hover:text-rose-400 transition-colors">
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="p-4 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between">
              <button className="flex items-center gap-2 px-4 py-2 text-indigo-600 dark:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-950/20 rounded-xl transition-colors">
                <Plus className="w-4 h-4" />
                Add Question
              </button>
              <div className="flex items-center gap-3">
                <button
                  onClick={() => setUploadState('idle')}
                  className="px-4 py-2 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition-colors"
                >
                  Re-upload
                </button>
                <button
                  className="flex items-center gap-2 px-4 py-2 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
                >
                  <Eye className="w-4 h-4" />
                  Preview Quiz
                </button>
                <button
                  onClick={handlePublish}
                  className="flex items-center gap-2 px-6 py-2 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25"
                >
                  <Upload className="w-4 h-4" />
                  Publish Quiz
                </button>
              </div>
            </div>
          </div>
        </motion.div>
      )}

      {/* Success */}
      {uploadState === 'success' && (
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-16 text-center"
        >
          <div className="w-24 h-24 mx-auto mb-6 rounded-full bg-emerald-100 dark:bg-emerald-950/40 flex items-center justify-center">
            <CheckCircle2 className="w-12 h-12 text-emerald-600 dark:text-emerald-400" />
          </div>
          <h2 className="text-2xl font-black text-slate-900 dark:text-white mb-2">Quiz Published!</h2>
          <p className="text-slate-500 dark:text-slate-400">"{quizTitle}" is now live with {samplePreview.length} questions.</p>
          <p className="text-slate-400 dark:text-slate-600 text-sm mt-4">Redirecting to Quiz Bank...</p>
        </motion.div>
      )}
    </div>
  );
}
