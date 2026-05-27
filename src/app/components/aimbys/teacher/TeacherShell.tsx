import { useState } from 'react';
import { LayoutDashboard, FileText, BookOpen, PenTool, BarChart3, Settings, Bell, Search, Menu, X, LogOut, ChevronRight, Sun, Moon, Code } from 'lucide-react';
import { AimbysState, AppView } from '../../../App';
import { TeacherDashboard } from './TeacherDashboard';
import { PaperGeneration } from './PaperGeneration';
import { EvaluationDesk } from './EvaluationDesk';
import { TeacherQuestionBank } from './TeacherQuestionBank';

const NAV = [
  { id: 'tchr-dashboard' as AppView, label: 'Dashboard', icon: LayoutDashboard, section: 'Overview' },
  { id: 'tchr-papergen' as AppView, label: 'Paper Generation', icon: FileText, section: 'Authoring' },
  { id: 'tchr-blueprint' as AppView, label: 'Blueprints', icon: BookOpen, section: 'Authoring' },
  { id: 'tchr-questionbank' as AppView, label: 'Question Bank', icon: BookOpen, section: 'Authoring' },
  { id: 'tchr-evaluation' as AppView, label: 'Evaluation Desk', icon: PenTool, section: 'Assessment' },
  { id: 'tchr-reports' as AppView, label: 'Reports', icon: BarChart3, section: 'Assessment' },
  { id: 'tchr-ide' as AppView, label: 'Coding IDE', icon: Code, section: 'Assessment' },
];

const sections = ['Overview', 'Authoring', 'Assessment'];

export function TeacherShell(props: AimbysState) {
  const { view, setView, darkMode, setDarkMode } = props;
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const renderContent = () => {
    if (view === 'tchr-papergen' || view === 'tchr-blueprint') return <PaperGeneration {...props} />;
    if (view === 'tchr-evaluation') return <EvaluationDesk {...props} />;
    if (view === 'tchr-questionbank') return <TeacherQuestionBank {...props} />;
    if (view === 'tchr-reports') return <TeacherReports {...props} />;
    if (view === 'tchr-ide') return <CodingIDEPage {...props} />;
    return <TeacherDashboard {...props} />;
  };

  return (
    <div className="flex h-screen overflow-hidden" style={{ background: '#f0f4f8' }}>
      {sidebarOpen && <div className="fixed inset-0 bg-black/50 z-30 lg:hidden" onClick={() => setSidebarOpen(false)} />}

      <aside className={`fixed lg:static top-0 left-0 h-full z-40 w-64 flex-shrink-0 flex flex-col transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`} style={{ background: '#0d1b2e' }}>
        <div className="px-5 py-5 border-b border-white/10">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded flex items-center justify-center flex-shrink-0" style={{ background: '#0369a1' }}>
                <span className="text-white font-black text-sm">A</span>
              </div>
              <div>
                <div className="text-white font-bold text-sm leading-none">AIMBYS</div>
                <div className="text-sky-300 text-xs mt-0.5">Teacher / Examiner</div>
              </div>
            </div>
            <button onClick={() => setSidebarOpen(false)} className="lg:hidden text-white/40 hover:text-white/80"><X className="w-4 h-4" /></button>
          </div>
          <div className="mt-3 px-2 py-2 rounded border border-white/10 text-xs text-slate-300" style={{ background: 'rgba(255,255,255,0.04)' }}>
            <div className="font-semibold truncate">Delhi Public School HQ</div>
            <div className="text-white/40 truncate">Mathematics • Senior Secondary</div>
          </div>
        </div>

        <nav className="flex-1 overflow-y-auto py-4 px-3">
          {sections.map(section => (
            <div key={section} className="mb-5">
              <div className="text-white/30 text-xs font-semibold uppercase tracking-widest px-3 mb-2">{section}</div>
              {NAV.filter(n => n.section === section).map(({ id, label, icon: Icon }) => {
                const active = view === id || (id === 'tchr-papergen' && view === 'tchr-blueprint');
                return (
                  <button key={id} onClick={() => { setView(id); setSidebarOpen(false); }}
                    className={`w-full flex items-center gap-3 px-3 py-2.5 rounded mb-0.5 text-left transition-all ${active ? 'text-white' : 'text-white/50 hover:text-white/80 hover:bg-white/5'}`}
                    style={active ? { background: '#0369a1' } : {}}
                  >
                    <Icon className="w-4 h-4 flex-shrink-0" />
                    <span className="text-sm font-medium">{label}</span>
                    {active && <ChevronRight className="w-3 h-3 ml-auto opacity-70" />}
                  </button>
                );
              })}
            </div>
          ))}
        </nav>

        <div className="px-3 py-4 border-t border-white/10">
          <div className="flex items-center gap-3 px-3 py-2.5 rounded hover:bg-white/5 cursor-pointer transition-colors">
            <div className="w-8 h-8 rounded-full flex items-center justify-center text-white text-sm font-bold flex-shrink-0" style={{ background: '#0369a1' }}>RK</div>
            <div className="flex-1 min-w-0">
              <div className="text-white text-sm font-semibold truncate">Mr. Rajiv Kumar</div>
              <div className="text-white/40 text-xs truncate">rajiv.kumar@dlps.edu.in</div>
            </div>
            <button onClick={() => props.setView('landing')} className="text-white/30 hover:text-white/70 transition-colors"><LogOut className="w-4 h-4" /></button>
          </div>
        </div>
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden">
        <header className="h-14 flex items-center justify-between px-4 md:px-6 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex-shrink-0">
          <div className="flex items-center gap-3">
            <button onClick={() => setSidebarOpen(true)} className="lg:hidden text-slate-500 p-1"><Menu className="w-5 h-5" /></button>
            <div className="hidden md:flex items-center gap-2 px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded w-72">
              <Search className="w-4 h-4 text-slate-400" />
              <input type="text" placeholder="Search questions, papers, blueprints..." className="bg-transparent text-slate-700 dark:text-slate-300 placeholder-slate-400 outline-none flex-1 text-sm" />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={() => setDarkMode(!darkMode)} className="p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </button>
            <button className="relative p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              <Bell className="w-4 h-4" />
              <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-sky-600 text-white text-xs rounded-full flex items-center justify-center font-bold">3</span>
            </button>
            <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-sky-50 dark:bg-sky-950/30 border border-sky-200 dark:border-sky-800 rounded text-sky-700 dark:text-sky-400 text-sm">
              Teacher
            </div>
          </div>
        </header>
        <main className="flex-1 overflow-y-auto p-4 md:p-6">{renderContent()}</main>
      </div>

      <nav className="lg:hidden fixed bottom-0 left-0 right-0 z-20 flex bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
        {NAV.slice(0, 5).map(({ id, label, icon: Icon }) => (
          <button key={id} onClick={() => setView(id)} className={`flex-1 flex flex-col items-center py-2 gap-0.5 text-xs transition-colors ${view === id ? 'text-sky-600 dark:text-sky-400' : 'text-slate-400 dark:text-slate-600'}`}>
            <Icon className="w-5 h-5" /><span className="truncate w-full text-center px-0.5">{label.split(' ')[0]}</span>
          </button>
        ))}
      </nav>
    </div>
  );
}

function TeacherReports(_props: AimbysState) {
  const data = [
    { class: 'XII-A (Maths)', avgScore: 74, passRate: 88, topScore: 96, attempts: 42 },
    { class: 'XII-B (Maths)', avgScore: 68, passRate: 82, topScore: 94, attempts: 40 },
    { class: 'XI-A (Maths)', avgScore: 71, passRate: 85, topScore: 98, attempts: 38 },
    { class: 'XI-B (Maths)', avgScore: 65, passRate: 79, topScore: 91, attempts: 36 },
    { class: 'X-C (Maths)', avgScore: 78, passRate: 92, topScore: 100, attempts: 44 },
  ];
  return (
    <div className="space-y-5 pb-8">
      <div><div className="text-slate-400 text-xs mb-1">Teacher / Assessment</div><h1 className="text-slate-900 dark:text-white font-bold text-xl">My Class Reports</h1></div>
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <table className="w-full">
          <thead className="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-800">
            <tr>{['Class / Section', 'Avg Score', 'Pass Rate', 'Top Score', 'Students'].map(h => <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider">{h}</th>)}</tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {data.map(d => (
              <tr key={d.class} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <td className="px-5 py-3.5 text-slate-900 dark:text-slate-100 font-semibold text-sm">{d.class}</td>
                <td className="px-5 py-3.5 text-slate-700 dark:text-slate-300 text-sm">{d.avgScore}%</td>
                <td className="px-5 py-3.5">
                  <span className={`text-sm font-semibold ${d.passRate >= 90 ? 'text-green-600 dark:text-green-400' : d.passRate >= 80 ? 'text-blue-600 dark:text-blue-400' : 'text-amber-600 dark:text-amber-400'}`}>{d.passRate}%</span>
                </td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{d.topScore}%</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{d.attempts}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function CodingIDEPage(_props: AimbysState) {
  return (
    <div className="space-y-5 pb-8">
      <div><div className="text-slate-400 text-xs mb-1">Teacher / Coding IDE</div><h1 className="text-slate-900 dark:text-white font-bold text-xl">Coding Question IDE</h1></div>
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4 h-[600px]">
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden flex flex-col">
          <div className="px-4 py-3 border-b border-slate-200 dark:border-slate-800 flex items-center gap-3">
            <span className="text-slate-900 dark:text-white font-semibold text-sm">Question Editor</span>
            <select className="ml-auto px-2 py-1 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-slate-700 dark:text-slate-300 text-xs outline-none">
              {['Python', 'Java', 'C++', 'JavaScript', 'SQL'].map(l => <option key={l}>{l}</option>)}
            </select>
          </div>
          <div className="flex-1 p-4 font-mono text-sm bg-slate-950 text-green-400 overflow-auto">
            <div className="text-slate-500 text-xs mb-3"># Problem: Maximum Subarray Sum</div>
            <div className="text-slate-400"># Constraints: -2^31 ≤ nums[i] ≤ 2^31 - 1</div>
            <div className="text-slate-400 mb-4"># Time Limit: 2 seconds</div>
            <div><span className="text-blue-400">def</span> <span className="text-yellow-400">max_subarray</span>(nums):</div>
            <div className="ml-4 text-slate-400">    <span className="text-slate-500"># Write your solution here</span></div>
            <div className="ml-4">    max_sum = nums[0]</div>
            <div className="ml-4">    current_sum = nums[0]</div>
            <div className="ml-4 mt-2">    <span className="text-blue-400">for</span> num <span className="text-blue-400">in</span> nums[1:]:</div>
            <div className="ml-8">        current_sum = <span className="text-yellow-400">max</span>(num, current_sum + num)</div>
            <div className="ml-8">        max_sum = <span className="text-yellow-400">max</span>(max_sum, current_sum)</div>
            <div className="ml-4 mt-2">    <span className="text-blue-400">return</span> max_sum</div>
            <div className="mt-2 animate-pulse text-slate-300">|</div>
          </div>
          <div className="px-4 py-2 border-t border-slate-800 flex gap-2">
            <button className="px-3 py-1.5 bg-green-700 hover:bg-green-600 text-white rounded text-xs font-medium transition-colors">▶ Run</button>
            <button className="px-3 py-1.5 bg-slate-700 hover:bg-slate-600 text-white rounded text-xs font-medium transition-colors">Test Cases</button>
            <button className="px-3 py-1.5 text-white rounded text-xs font-medium transition-colors ml-auto" style={{ background: '#1d4ed8' }}>Save to Bank</button>
          </div>
        </div>
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden flex flex-col">
          <div className="px-4 py-3 border-b border-slate-200 dark:border-slate-800">
            <span className="text-slate-900 dark:text-white font-semibold text-sm">Test Results & Output</span>
          </div>
          <div className="flex-1 p-4 space-y-3 overflow-auto">
            {[
              { case: 'Test Case 1', input: '[-2,1,-3,4,-1,2,1,-5,4]', expected: '6', actual: '6', pass: true },
              { case: 'Test Case 2', input: '[1]', expected: '1', actual: '1', pass: true },
              { case: 'Test Case 3', input: '[5,4,-1,7,8]', expected: '23', actual: '23', pass: true },
              { case: 'Edge Case', input: '[-1,-2,-3,-4]', expected: '-1', actual: '-1', pass: true },
            ].map(tc => (
              <div key={tc.case} className={`rounded border p-3 ${tc.pass ? 'border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-950/20' : 'border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-950/20'}`}>
                <div className="flex items-center justify-between mb-2">
                  <span className="font-semibold text-sm text-slate-900 dark:text-slate-100">{tc.case}</span>
                  <span className={`text-xs font-bold px-2 py-0.5 rounded ${tc.pass ? 'bg-green-600 text-white' : 'bg-red-600 text-white'}`}>{tc.pass ? 'PASS' : 'FAIL'}</span>
                </div>
                <div className="font-mono text-xs space-y-1">
                  <div className="text-slate-500 dark:text-slate-400">Input: <span className="text-slate-700 dark:text-slate-300">{tc.input}</span></div>
                  <div className="text-slate-500 dark:text-slate-400">Expected: <span className="text-green-700 dark:text-green-400">{tc.expected}</span></div>
                  <div className="text-slate-500 dark:text-slate-400">Actual: <span className={tc.pass ? 'text-green-700 dark:text-green-400' : 'text-red-600 dark:text-red-400'}>{tc.actual}</span></div>
                </div>
              </div>
            ))}
          </div>
          <div className="px-4 py-3 border-t border-slate-200 dark:border-slate-800 bg-green-50 dark:bg-green-950/20">
            <span className="text-green-700 dark:text-green-400 font-semibold text-sm">4/4 test cases passed · Runtime: 42ms · Memory: 14.2 MB</span>
          </div>
        </div>
      </div>
    </div>
  );
}
