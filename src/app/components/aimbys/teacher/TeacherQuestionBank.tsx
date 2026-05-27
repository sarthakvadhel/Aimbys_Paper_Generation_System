import { useState } from 'react';
import { Search, Plus, CheckSquare, AlignLeft, Code, BookOpen, Edit2, Trash2, Tag } from 'lucide-react';
import { AimbysState } from '../../../App';

const myQuestions = [
  { id: 'QB-00841', type: 'MCQ', topic: 'Calculus', difficulty: 'Hard', marks: 4, usageCount: 28, text: 'Find intervals where f\'(x) > 0 for f(x) = x³ − 3x² + 2x.' },
  { id: 'QB-00847', type: 'Descriptive', topic: 'Trigonometry', difficulty: 'Hard', marks: 8, usageCount: 17, text: 'Prove that sin(A+B) = sinA·cosB + cosA·sinB.' },
  { id: 'QB-00849', type: 'MCQ', topic: 'Probability', difficulty: 'Easy', marks: 1, usageCount: 61, text: 'A fair coin is tossed. What is the probability of heads?' },
  { id: 'QB-00851', type: 'MCQ', topic: 'Matrices', difficulty: 'Medium', marks: 2, usageCount: 22, text: 'If A is a 3×3 matrix with det(A) = 5, what is det(2A)?' },
  { id: 'QB-00852', type: 'Descriptive', topic: 'Integration', difficulty: 'Hard', marks: 6, usageCount: 14, text: 'Evaluate ∫ x·sin(x) dx using integration by parts.' },
  { id: 'QB-00855', type: 'Coding', topic: 'Number Theory', difficulty: 'Medium', marks: 6, usageCount: 8, text: 'Write a function to check if a number is prime.' },
];

const typeConfig: Record<string, { label: string; icon: any; color: string; bg: string }> = {
  MCQ: { label: 'MCQ', icon: CheckSquare, color: 'text-blue-700 dark:text-blue-400', bg: 'bg-blue-50 dark:bg-blue-950/30' },
  Descriptive: { label: 'Descriptive', icon: AlignLeft, color: 'text-violet-700 dark:text-violet-400', bg: 'bg-violet-50 dark:bg-violet-950/30' },
  Coding: { label: 'Coding', icon: Code, color: 'text-emerald-700 dark:text-emerald-400', bg: 'bg-emerald-50 dark:bg-emerald-950/30' },
  FillBlanks: { label: 'Fill Blanks', icon: BookOpen, color: 'text-amber-700 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/30' },
};
const diffColor: Record<string, string> = { Easy: 'text-green-600 dark:text-green-400', Medium: 'text-amber-600 dark:text-amber-400', Hard: 'text-red-600 dark:text-red-400' };

export function TeacherQuestionBank(_props: AimbysState) {
  const [search, setSearch] = useState('');

  const filtered = myQuestions.filter(q =>
    q.text.toLowerCase().includes(search.toLowerCase()) || q.topic.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Teacher / Authoring</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">My Question Bank</h1>
          <button className="flex items-center gap-1.5 px-3 py-2 text-white rounded text-sm" style={{ background: '#0369a1' }}>
            <Plus className="w-3.5 h-3.5" />Add Question
          </button>
        </div>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        {[
          { label: 'My Questions', value: myQuestions.length.toString(), color: 'text-slate-900 dark:text-white' },
          { label: 'MCQ', value: myQuestions.filter(q => q.type === 'MCQ').length.toString(), color: 'text-blue-600 dark:text-blue-400' },
          { label: 'Descriptive', value: myQuestions.filter(q => q.type === 'Descriptive').length.toString(), color: 'text-violet-600 dark:text-violet-400' },
          { label: 'Coding', value: myQuestions.filter(q => q.type === 'Coding').length.toString(), color: 'text-emerald-600 dark:text-emerald-400' },
        ].map(({ label, value, color }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className={`text-2xl font-black ${color}`}>{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm mt-0.5">{label}</div>
          </div>
        ))}
      </div>

      <div className="flex items-center gap-2 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded">
        <Search className="w-4 h-4 text-slate-400" />
        <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search questions..." className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 outline-none flex-1 text-sm" />
      </div>

      <div className="space-y-3">
        {filtered.map(q => {
          const tc = typeConfig[q.type];
          const TypeIcon = tc.icon;
          return (
            <div key={q.id} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5 hover:border-sky-300 dark:hover:border-sky-700 transition-colors">
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex flex-wrap items-center gap-2 mb-2">
                    <span className="font-mono text-sky-600 dark:text-sky-400 text-xs">{q.id}</span>
                    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${tc.bg} ${tc.color}`}><TypeIcon className="w-3 h-3" />{tc.label}</span>
                    <span className={`text-xs font-semibold ${diffColor[q.difficulty]}`}>{q.difficulty}</span>
                    <span className="text-xs text-slate-400">· {q.marks} marks</span>
                  </div>
                  <p className="text-slate-800 dark:text-slate-200 text-sm leading-relaxed">{q.text}</p>
                  <div className="flex items-center gap-3 mt-2">
                    <span className="flex items-center gap-1 text-xs text-slate-400"><Tag className="w-3 h-3" />{q.topic}</span>
                    <span className="text-xs text-slate-400">Used {q.usageCount}× in papers</span>
                  </div>
                </div>
                <div className="flex items-center gap-1">
                  <button className="p-1.5 text-slate-400 hover:text-sky-600 dark:hover:text-sky-400 hover:bg-sky-50 dark:hover:bg-sky-950/20 rounded transition-all"><Edit2 className="w-4 h-4" /></button>
                  <button className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/20 rounded transition-all"><Trash2 className="w-4 h-4" /></button>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
