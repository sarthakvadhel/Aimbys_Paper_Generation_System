import { useState } from 'react';
import { Plus, Search, CheckSquare, AlignLeft, Code, BookOpen, Trash2, GripVertical, Eye, Save, Send } from 'lucide-react';
import { AimbysState } from '../../../App';

const bankQuestions = [
  { id: 'QB-00841', type: 'MCQ', subject: 'Mathematics', topic: 'Calculus', difficulty: 'Hard', marks: 4, text: 'Find intervals where f\'(x) > 0 for f(x) = x³ − 3x² + 2x.' },
  { id: 'QB-00842', type: 'MCQ', subject: 'Mathematics', topic: 'Algebra', difficulty: 'Medium', marks: 3, text: 'Solve: 2x² + 5x − 3 = 0 using the quadratic formula.' },
  { id: 'QB-00844', type: 'Coding', subject: 'Mathematics', topic: 'Algorithms', difficulty: 'Hard', marks: 10, text: 'Write a program to find the maximum subarray sum.' },
  { id: 'QB-00847', type: 'Descriptive', subject: 'Mathematics', topic: 'Trigonometry', difficulty: 'Hard', marks: 8, text: 'Prove that sin(A+B) = sinA·cosB + cosA·sinB.' },
  { id: 'QB-00849', type: 'MCQ', subject: 'Mathematics', topic: 'Probability', difficulty: 'Easy', marks: 1, text: 'A fair coin is tossed. What is the probability of heads?' },
  { id: 'QB-00851', type: 'MCQ', subject: 'Mathematics', topic: 'Matrices', difficulty: 'Medium', marks: 2, text: 'If A is a 3×3 matrix with det(A) = 5, what is det(2A)?' },
  { id: 'QB-00852', type: 'Descriptive', subject: 'Mathematics', topic: 'Integration', difficulty: 'Hard', marks: 6, text: 'Evaluate ∫ x·sin(x) dx using integration by parts.' },
];

const typeIcons: Record<string, any> = {
  MCQ: CheckSquare, Descriptive: AlignLeft, Coding: Code, FillBlanks: BookOpen,
};
const typeColors: Record<string, { text: string; bg: string }> = {
  MCQ: { text: 'text-blue-700 dark:text-blue-400', bg: 'bg-blue-50 dark:bg-blue-950/30' },
  Descriptive: { text: 'text-violet-700 dark:text-violet-400', bg: 'bg-violet-50 dark:bg-violet-950/30' },
  Coding: { text: 'text-emerald-700 dark:text-emerald-400', bg: 'bg-emerald-50 dark:bg-emerald-950/30' },
  FillBlanks: { text: 'text-amber-700 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/30' },
};
const diffColor: Record<string, string> = { Easy: 'text-green-600 dark:text-green-400', Medium: 'text-amber-600 dark:text-amber-400', Hard: 'text-red-600 dark:text-red-400' };

export function PaperGeneration(_props: AimbysState) {
  const [search, setSearch] = useState('');
  const [selectedIds, setSelectedIds] = useState<string[]>(['QB-00841', 'QB-00847', 'QB-00849']);
  const [tab, setTab] = useState<'bank' | 'blueprint'>('bank');
  const [paperTitle, setPaperTitle] = useState('Mathematics XII — Unit Test 4');
  const [previewOpen, setPreviewOpen] = useState(false);

  const filtered = bankQuestions.filter(q =>
    q.text.toLowerCase().includes(search.toLowerCase()) || q.topic.toLowerCase().includes(search.toLowerCase())
  );

  const selected = bankQuestions.filter(q => selectedIds.includes(q.id));
  const totalMarks = selected.reduce((s, q) => s + q.marks, 0);

  const toggle = (id: string) => setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);

  if (previewOpen) {
    return (
      <div className="space-y-5 pb-8">
        <div className="flex items-center justify-between">
          <div><div className="text-slate-400 text-xs mb-1">Teacher / Paper Generation / Preview</div><h1 className="text-slate-900 dark:text-white font-bold text-xl">Paper Preview</h1></div>
          <button onClick={() => setPreviewOpen(false)} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50">← Back to Editor</button>
        </div>
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-8 max-w-3xl mx-auto">
          <div className="text-center mb-8 pb-6 border-b border-slate-200 dark:border-slate-700">
            <div className="text-slate-500 dark:text-slate-400 text-sm mb-1">Delhi Public School HQ</div>
            <h2 className="text-slate-900 dark:text-white font-bold text-xl mb-1">{paperTitle}</h2>
            <div className="text-slate-500 dark:text-slate-400 text-sm">Total Marks: {totalMarks} | Duration: 3 Hours | Date: 22 May 2026</div>
          </div>
          <div className="space-y-6">
            {selected.map((q, i) => {
              const TypeIcon = typeIcons[q.type] || CheckSquare;
              const tc = typeColors[q.type] || typeColors.MCQ;
              return (
                <div key={q.id} className="border-b border-slate-100 dark:border-slate-800 pb-5 last:border-0">
                  <div className="flex items-start gap-3">
                    <span className="font-bold text-slate-900 dark:text-white text-sm flex-shrink-0">Q{i + 1}.</span>
                    <div className="flex-1">
                      <p className="text-slate-800 dark:text-slate-200 text-sm leading-relaxed">{q.text}</p>
                      <div className="flex items-center gap-3 mt-2">
                        <span className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs ${tc.bg} ${tc.text}`}><TypeIcon className="w-3 h-3" />{q.type}</span>
                        <span className="text-slate-400 text-xs">[{q.marks} marks]</span>
                        <span className={`text-xs ${diffColor[q.difficulty]}`}>{q.difficulty}</span>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Teacher / Authoring</div>
        <h1 className="text-slate-900 dark:text-white font-bold text-xl">Paper Generation</h1>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
        {/* Left: Question Bank Browser */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden flex flex-col" style={{ minHeight: 560 }}>
          <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
            <div className="flex items-center gap-2 mb-3">
              {['bank', 'blueprint'].map(t => (
                <button key={t} onClick={() => setTab(t as any)} className={`px-3 py-1.5 rounded text-sm font-medium capitalize transition-colors ${tab === t ? 'text-white' : 'text-slate-600 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700'}`} style={tab === t ? { background: '#0369a1' } : {}}>{t === 'bank' ? 'Question Bank' : 'Auto Blueprint'}</button>
              ))}
            </div>
            <div className="flex items-center gap-2 px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded">
              <Search className="w-4 h-4 text-slate-400" />
              <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search questions..." className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 outline-none flex-1 text-sm" />
            </div>
          </div>

          {tab === 'bank' ? (
            <div className="flex-1 overflow-y-auto divide-y divide-slate-100 dark:divide-slate-800">
              {filtered.map(q => {
                const TypeIcon = typeIcons[q.type] || CheckSquare;
                const tc = typeColors[q.type] || typeColors.MCQ;
                const isSelected = selectedIds.includes(q.id);
                return (
                  <div key={q.id} onClick={() => toggle(q.id)} className={`px-5 py-3.5 cursor-pointer transition-colors ${isSelected ? 'bg-sky-50 dark:bg-sky-950/20 border-l-2 border-sky-500' : 'hover:bg-slate-50 dark:hover:bg-slate-800/30'}`}>
                    <div className="flex items-start gap-3">
                      <div className={`w-4 h-4 rounded border-2 flex items-center justify-center flex-shrink-0 mt-0.5 transition-colors ${isSelected ? 'bg-sky-600 border-sky-600' : 'border-slate-300 dark:border-slate-600'}`}>
                        {isSelected && <div className="w-2 h-2 bg-white rounded-sm" />}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-slate-800 dark:text-slate-200 text-sm leading-relaxed">{q.text}</p>
                        <div className="flex flex-wrap items-center gap-2 mt-1.5">
                          <span className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs ${tc.bg} ${tc.text}`}><TypeIcon className="w-3 h-3" />{q.type}</span>
                          <span className="text-xs text-slate-400">{q.topic}</span>
                          <span className={`text-xs font-semibold ${diffColor[q.difficulty]}`}>{q.difficulty}</span>
                          <span className="text-xs text-slate-400 ml-auto">{q.marks}m</span>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="flex-1 p-5 space-y-4">
              <div className="text-slate-600 dark:text-slate-400 text-sm">Configure blueprint parameters to auto-select questions:</div>
              {[
                { label: 'Total Marks', value: '80' },
                { label: 'Duration (min)', value: '180' },
                { label: 'MCQ Questions', value: '20' },
                { label: 'Descriptive', value: '4' },
                { label: 'Difficulty Mix', value: 'Easy 30% / Medium 50% / Hard 20%' },
              ].map(({ label, value }) => (
                <div key={label} className="flex items-center justify-between py-2 border-b border-slate-100 dark:border-slate-800">
                  <span className="text-slate-600 dark:text-slate-400 text-sm">{label}</span>
                  <span className="text-slate-900 dark:text-white font-medium text-sm">{value}</span>
                </div>
              ))}
              <button className="w-full py-2.5 text-white rounded text-sm font-medium" style={{ background: '#0369a1' }}>Generate from Blueprint</button>
            </div>
          )}
        </div>

        {/* Right: Paper Editor */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden flex flex-col" style={{ minHeight: 560 }}>
          <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
            <div className="flex items-center justify-between mb-3">
              <span className="text-slate-900 dark:text-white font-semibold text-sm">Paper Editor</span>
              <div className="flex items-center gap-1.5">
                <button onClick={() => setPreviewOpen(true)} className="flex items-center gap-1 px-2.5 py-1.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded text-xs hover:bg-slate-200 dark:hover:bg-slate-700"><Eye className="w-3.5 h-3.5" />Preview</button>
                <button className="flex items-center gap-1 px-2.5 py-1.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded text-xs hover:bg-slate-200 dark:hover:bg-slate-700"><Save className="w-3.5 h-3.5" />Draft</button>
                <button className="flex items-center gap-1 px-2.5 py-1.5 text-white rounded text-xs" style={{ background: '#0369a1' }}><Send className="w-3.5 h-3.5" />Submit</button>
              </div>
            </div>
            <input value={paperTitle} onChange={e => setPaperTitle(e.target.value)} className="w-full px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-slate-900 dark:text-white text-sm outline-none focus:border-sky-400" placeholder="Paper title..." />
          </div>

          <div className="flex-1 overflow-y-auto">
            {selected.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-slate-400">
                <Plus className="w-10 h-10 mb-2 opacity-40" />
                <p className="text-sm">Select questions from the bank</p>
              </div>
            ) : (
              <div className="divide-y divide-slate-100 dark:divide-slate-800">
                {selected.map((q, i) => {
                  const TypeIcon = typeIcons[q.type] || CheckSquare;
                  const tc = typeColors[q.type] || typeColors.MCQ;
                  return (
                    <div key={q.id} className="px-5 py-3.5 flex items-start gap-3 group">
                      <GripVertical className="w-4 h-4 text-slate-300 dark:text-slate-600 mt-0.5 flex-shrink-0 cursor-grab" />
                      <div className="w-5 h-5 rounded bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-500 dark:text-slate-400 text-xs font-bold flex-shrink-0">{i + 1}</div>
                      <div className="flex-1 min-w-0">
                        <p className="text-slate-800 dark:text-slate-200 text-sm leading-relaxed">{q.text}</p>
                        <div className="flex items-center gap-2 mt-1.5">
                          <span className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs ${tc.bg} ${tc.text}`}><TypeIcon className="w-3 h-3" />{q.type}</span>
                          <span className="text-xs text-slate-400">{q.marks} marks</span>
                        </div>
                      </div>
                      <button onClick={() => toggle(q.id)} className="opacity-0 group-hover:opacity-100 p-1 text-slate-300 hover:text-red-500 transition-all"><Trash2 className="w-4 h-4" /></button>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          <div className="px-5 py-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50 flex items-center justify-between">
            <span className="text-slate-500 dark:text-slate-400 text-sm">{selected.length} questions selected</span>
            <span className="text-slate-900 dark:text-white font-bold text-sm">Total: {totalMarks} marks</span>
          </div>
        </div>
      </div>
    </div>
  );
}
