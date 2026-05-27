import { useState } from 'react';
import { LayoutDashboard, BookOpen, FileText, BarChart3, Bell, Menu, X, LogOut, ChevronRight, Sun, Moon } from 'lucide-react';
import { AimbysState, AppView } from '../../../App';
import { StudentDashboard } from './StudentDashboard';
import { ExamInterface } from './ExamInterface';
import { StudentResults } from './StudentResults';
import { ForcePasswordChange } from './ForcePasswordChange';

const NAV = [
  { id: 'std-dashboard' as AppView, label: 'Dashboard', icon: LayoutDashboard, section: 'Main' },
  { id: 'std-exam' as AppView, label: 'My Exams', icon: BookOpen, section: 'Main' },
  { id: 'std-results' as AppView, label: 'Results', icon: FileText, section: 'Main' },
  { id: 'std-analytics' as AppView, label: 'My Analytics', icon: BarChart3, section: 'Main' },
];

export function StudentShell(props: AimbysState) {
  const { view, setView, darkMode, setDarkMode, examActive, setExamActive, mustChangePassword } = props;
  const [sidebarOpen, setSidebarOpen] = useState(false);

  if (mustChangePassword) {
    return <ForcePasswordChange {...props} />;
  }

  if (examActive && view === 'std-exam') {
    return <ExamInterface {...props} />;
  }

  const renderContent = () => {
    if (view === 'std-exam') return <ExamLobby {...props} />;
    if (view === 'std-results') return <StudentResults {...props} />;
    if (view === 'std-analytics') return <StudentAnalytics {...props} />;
    return <StudentDashboard {...props} />;
  };

  return (
    <div className="flex h-screen overflow-hidden" style={{ background: '#f0f4f8' }}>
      {sidebarOpen && <div className="fixed inset-0 bg-black/50 z-30 lg:hidden" onClick={() => setSidebarOpen(false)} />}

      <aside className={`fixed lg:static top-0 left-0 h-full z-40 w-60 flex-shrink-0 flex flex-col transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`} style={{ background: '#0d1b2e' }}>
        <div className="px-5 py-5 border-b border-white/10">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded flex items-center justify-center flex-shrink-0" style={{ background: '#15803d' }}>
                <span className="text-white font-black text-sm">A</span>
              </div>
              <div>
                <div className="text-white font-bold text-sm leading-none">AIMBYS</div>
                <div className="text-green-300 text-xs mt-0.5">Student Portal</div>
              </div>
            </div>
            <button onClick={() => setSidebarOpen(false)} className="lg:hidden text-white/40 hover:text-white/80"><X className="w-4 h-4" /></button>
          </div>
          <div className="mt-3 px-2 py-2 rounded border border-white/10 text-xs text-slate-300" style={{ background: 'rgba(255,255,255,0.04)' }}>
            <div className="font-semibold truncate">Riya Patel</div>
            <div className="text-white/40 truncate">Class XII-A · Roll: 021</div>
          </div>
        </div>

        <nav className="flex-1 overflow-y-auto py-4 px-3">
          {NAV.map(({ id, label, icon: Icon }) => {
            const active = view === id;
            return (
              <button key={id} onClick={() => { setView(id); setSidebarOpen(false); }}
                className={`w-full flex items-center gap-3 px-3 py-2.5 rounded mb-0.5 text-left transition-all ${active ? 'text-white' : 'text-white/50 hover:text-white/80 hover:bg-white/5'}`}
                style={active ? { background: '#15803d' } : {}}
              >
                <Icon className="w-4 h-4 flex-shrink-0" />
                <span className="text-sm font-medium">{label}</span>
                {active && <ChevronRight className="w-3 h-3 ml-auto opacity-70" />}
              </button>
            );
          })}
        </nav>

        <div className="px-3 py-4 border-t border-white/10">
          <div className="flex items-center gap-3 px-3 py-2.5 rounded hover:bg-white/5 cursor-pointer transition-colors">
            <div className="w-8 h-8 rounded-full bg-green-700 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">RP</div>
            <div className="flex-1 min-w-0">
              <div className="text-white text-sm font-semibold truncate">Riya Patel</div>
              <div className="text-white/40 text-xs truncate">riya.patel.12a@dlps.edu.in</div>
            </div>
            <button onClick={() => props.setView('landing')} className="text-white/30 hover:text-white/70 transition-colors"><LogOut className="w-4 h-4" /></button>
          </div>
        </div>
      </aside>

      <div className="flex-1 flex flex-col overflow-hidden">
        <header className="h-14 flex items-center justify-between px-4 md:px-6 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex-shrink-0">
          <div className="flex items-center gap-3">
            <button onClick={() => setSidebarOpen(true)} className="lg:hidden text-slate-500 p-1"><Menu className="w-5 h-5" /></button>
            <div className="text-slate-700 dark:text-slate-300 text-sm font-medium hidden sm:block">Delhi Public School HQ · Class XII-A</div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={() => setDarkMode(!darkMode)} className="p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </button>
            <button className="relative p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              <Bell className="w-4 h-4" />
              <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-green-600 text-white text-xs rounded-full flex items-center justify-center font-bold">2</span>
            </button>
            <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-green-50 dark:bg-green-950/30 border border-green-200 dark:border-green-800 rounded text-green-700 dark:text-green-400 text-sm">
              Student
            </div>
          </div>
        </header>
        <main className="flex-1 overflow-y-auto p-4 md:p-6">{renderContent()}</main>
      </div>

      <nav className="lg:hidden fixed bottom-0 left-0 right-0 z-20 flex bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
        {NAV.map(({ id, label, icon: Icon }) => (
          <button key={id} onClick={() => setView(id)} className={`flex-1 flex flex-col items-center py-2 gap-0.5 text-xs transition-colors ${view === id ? 'text-green-600 dark:text-green-400' : 'text-slate-400 dark:text-slate-600'}`}>
            <Icon className="w-5 h-5" /><span className="truncate w-full text-center px-0.5">{label.split(' ')[0]}</span>
          </button>
        ))}
      </nav>
    </div>
  );
}

function ExamLobby(props: AimbysState) {
  const { setExamActive, setView } = props;
  const upcomingExams = [
    { title: 'Mathematics XII — Unit Test 4', date: '22 May 2026', time: '10:00 AM', duration: '3 hours', marks: 80, status: 'upcoming' },
    { title: 'Physics XII — Chapter Test', date: '24 May 2026', time: '09:00 AM', duration: '3 hours', marks: 60, status: 'upcoming' },
    { title: 'English Core — Mid Term', date: '28 May 2026', time: '08:30 AM', duration: '3 hours', marks: 100, status: 'upcoming' },
  ];
  const completedExams = [
    { title: 'Mathematics XII — Unit Test 3', date: '15 May 2026', marks: 80, scored: 62, status: 'completed' },
    { title: 'Physics XI — Chapter 5 Test', date: '10 May 2026', marks: 50, scored: 44, status: 'completed' },
  ];

  return (
    <div className="space-y-5 pb-8">
      <div><div className="text-slate-400 text-xs mb-1">Student / Exams</div><h1 className="text-slate-900 dark:text-white font-bold text-xl">My Exams</h1></div>

      <div className="space-y-3">
        <h2 className="text-slate-700 dark:text-slate-300 font-semibold text-sm uppercase tracking-wider">Upcoming</h2>
        {upcomingExams.map((e, i) => (
          <div key={i} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5 flex flex-col sm:flex-row sm:items-center gap-4">
            <div className="flex-1">
              <div className="text-slate-900 dark:text-white font-semibold">{e.title}</div>
              <div className="text-slate-500 dark:text-slate-400 text-sm mt-1">{e.date} · {e.time} · {e.duration} · {e.marks} marks</div>
            </div>
            {i === 0 ? (
              <button onClick={() => { setExamActive(true); setView('std-exam'); }} className="px-4 py-2 text-white rounded text-sm font-medium flex-shrink-0" style={{ background: '#15803d' }}>
                Start Exam →
              </button>
            ) : (
              <span className="px-3 py-1.5 bg-amber-50 dark:bg-amber-950/30 text-amber-700 dark:text-amber-400 rounded text-xs font-semibold flex-shrink-0">Scheduled</span>
            )}
          </div>
        ))}
      </div>

      <div className="space-y-3">
        <h2 className="text-slate-700 dark:text-slate-300 font-semibold text-sm uppercase tracking-wider">Completed</h2>
        {completedExams.map((e, i) => (
          <div key={i} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5 flex flex-col sm:flex-row sm:items-center gap-4">
            <div className="flex-1">
              <div className="text-slate-900 dark:text-white font-semibold">{e.title}</div>
              <div className="text-slate-500 dark:text-slate-400 text-sm mt-1">{e.date}</div>
            </div>
            <div className="flex items-center gap-3">
              <div className="text-right">
                <div className="text-slate-900 dark:text-white font-bold">{e.scored}/{e.marks}</div>
                <div className="text-slate-400 text-xs">{Math.round(e.scored / e.marks * 100)}%</div>
              </div>
              <button onClick={() => setView('std-results')} className="px-3 py-1.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded text-xs font-medium hover:bg-slate-200 dark:hover:bg-slate-700">View Results</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function StudentAnalytics(_props: AimbysState) {
  const subjects = [
    { name: 'Mathematics', scores: [58, 62, 71, 68, 74, 78], avg: 68.5, pass: true },
    { name: 'Physics', scores: [72, 68, 75, 80, 76, 82], avg: 75.5, pass: true },
    { name: 'Chemistry', scores: [65, 70, 74, 78, 80, 83], avg: 75.0, pass: true },
    { name: 'English', scores: [82, 85, 88, 86, 90, 91], avg: 87.0, pass: true },
    { name: 'Computer Sci.', scores: [88, 90, 92, 91, 94, 95], avg: 91.7, pass: true },
  ];
  return (
    <div className="space-y-5 pb-8">
      <div><div className="text-slate-400 text-xs mb-1">Student / Analytics</div><h1 className="text-slate-900 dark:text-white font-bold text-xl">My Performance Analytics</h1></div>
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
          <h2 className="text-slate-900 dark:text-white font-semibold">Subject-wise Score Trend</h2>
        </div>
        <div className="p-5 space-y-5">
          {subjects.map(s => {
            const max = Math.max(...s.scores);
            const barW = 100 / max;
            return (
              <div key={s.name}>
                <div className="flex items-center justify-between mb-1.5">
                  <span className="text-slate-700 dark:text-slate-300 text-sm font-medium">{s.name}</span>
                  <span className="text-slate-900 dark:text-white font-bold text-sm">{s.avg.toFixed(1)}% avg</span>
                </div>
                <div className="flex items-end gap-1 h-8">
                  {s.scores.map((sc, i) => (
                    <div key={i} className="flex-1 rounded-sm transition-all hover:opacity-80" style={{ height: `${sc * barW}%`, background: sc >= 75 ? '#15803d' : sc >= 50 ? '#0369a1' : '#dc2626', opacity: 0.7 + (i / s.scores.length) * 0.3 }} title={`${sc}%`} />
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
        {[
          { label: 'Overall Rank', value: '12th', sub: 'In class XII-A (42 students)' },
          { label: 'Avg Score', value: '79.5%', sub: 'Across all subjects' },
          { label: 'Exams Attempted', value: '14', sub: 'This academic year' },
        ].map(({ label, value, sub }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className="text-2xl font-black text-green-600 dark:text-green-400">{value}</div>
            <div className="text-slate-700 dark:text-slate-300 text-sm font-medium mt-0.5">{label}</div>
            <div className="text-slate-400 text-xs mt-1">{sub}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
