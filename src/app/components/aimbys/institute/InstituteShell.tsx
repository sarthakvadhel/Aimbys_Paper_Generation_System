import { useState } from 'react';
import { LayoutDashboard, Users, BookOpen, Calendar, BarChart3, Settings, Bell, Search, Menu, X, LogOut, ChevronRight, Sun, Moon, FileText, CheckSquare } from 'lucide-react';
import { AimbysState, AppView } from '../../../App';
import { InstituteDashboard } from './InstituteDashboard';
import { InstituteUsers } from './InstituteUsers';
import { QuestionBank } from './QuestionBank';
import { InstituteAnalytics } from './InstituteAnalytics';

const NAV = [
  { id: 'inst-dashboard' as AppView, label: 'Dashboard', icon: LayoutDashboard, section: 'Overview' },
  { id: 'inst-users' as AppView, label: 'Users & Roles', icon: Users, section: 'Overview' },
  { id: 'inst-papers' as AppView, label: 'Paper Management', icon: FileText, section: 'Academic' },
  { id: 'inst-questionbank' as AppView, label: 'Question Bank', icon: BookOpen, section: 'Academic' },
  { id: 'inst-calendar' as AppView, label: 'Exam Calendar', icon: Calendar, section: 'Academic' },
  { id: 'inst-approvals' as AppView, label: 'Approvals', icon: CheckSquare, section: 'Workflow' },
  { id: 'inst-analytics' as AppView, label: 'Analytics', icon: BarChart3, section: 'Workflow' },
  { id: 'inst-settings' as AppView, label: 'Settings', icon: Settings, section: 'Workflow' },
];

const sections = ['Overview', 'Academic', 'Workflow'];

export function InstituteShell(props: AimbysState) {
  const { view, setView, darkMode, setDarkMode } = props;
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const renderContent = () => {
    if (view === 'inst-users') return <InstituteUsers {...props} />;
    if (view === 'inst-questionbank') return <QuestionBank {...props} />;
    if (view === 'inst-analytics') return <InstituteAnalytics {...props} />;
    if (view === 'inst-calendar') return <ExamCalendarPage {...props} />;
    return <InstituteDashboard {...props} />;
  };

  return (
    <div className="flex h-screen overflow-hidden" style={{ background: '#f0f4f8' }}>
      {sidebarOpen && <div className="fixed inset-0 bg-black/50 z-30 lg:hidden" onClick={() => setSidebarOpen(false)} />}

      <aside className={`fixed lg:static top-0 left-0 h-full z-40 w-64 flex-shrink-0 flex flex-col transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`} style={{ background: '#0d1b2e' }}>
        <div className="px-5 py-5 border-b border-white/10">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded flex items-center justify-center flex-shrink-0" style={{ background: '#1d4ed8' }}>
                <span className="text-white font-black text-sm">A</span>
              </div>
              <div>
                <div className="text-white font-bold text-sm leading-none">AIMBYS</div>
                <div className="text-blue-300 text-xs mt-0.5">Institute Admin</div>
              </div>
            </div>
            <button onClick={() => setSidebarOpen(false)} className="lg:hidden text-white/40 hover:text-white/80"><X className="w-4 h-4" /></button>
          </div>
          <div className="mt-3 px-2 py-2 rounded border border-white/10 text-xs text-slate-300" style={{ background: 'rgba(255,255,255,0.04)' }}>
            <div className="font-semibold truncate">Delhi Public School HQ</div>
            <div className="text-white/40 truncate">28,400 students • 840 teachers</div>
          </div>
        </div>

        <nav className="flex-1 overflow-y-auto py-4 px-3">
          {sections.map(section => (
            <div key={section} className="mb-5">
              <div className="text-white/30 text-xs font-semibold uppercase tracking-widest px-3 mb-2">{section}</div>
              {NAV.filter(n => n.section === section).map(({ id, label, icon: Icon }) => {
                const active = view === id;
                return (
                  <button key={id} onClick={() => { setView(id); setSidebarOpen(false); }}
                    className={`w-full flex items-center gap-3 px-3 py-2.5 rounded mb-0.5 text-left transition-all ${active ? 'text-white' : 'text-white/50 hover:text-white/80 hover:bg-white/5'}`}
                    style={active ? { background: '#1d4ed8' } : {}}
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
            <div className="w-8 h-8 rounded-full flex items-center justify-center text-white text-sm font-bold flex-shrink-0" style={{ background: '#1d4ed8' }}>IA</div>
            <div className="flex-1 min-w-0">
              <div className="text-white text-sm font-semibold truncate">Mrs. Sunita Sharma</div>
              <div className="text-white/40 text-xs truncate">admin@dlps.edu.in</div>
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
              <input type="text" placeholder="Search students, papers, reports..." className="bg-transparent text-slate-700 dark:text-slate-300 placeholder-slate-400 outline-none flex-1 text-sm" />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={() => setDarkMode(!darkMode)} className="p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </button>
            <button className="relative p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              <Bell className="w-4 h-4" />
              <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-blue-600 text-white text-xs rounded-full flex items-center justify-center font-bold">7</span>
            </button>
            <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800 rounded text-blue-700 dark:text-blue-400 text-sm">
              Institute Admin
            </div>
          </div>
        </header>
        <main className="flex-1 overflow-y-auto p-4 md:p-6">{renderContent()}</main>
      </div>

      <nav className="lg:hidden fixed bottom-0 left-0 right-0 z-20 flex bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
        {NAV.slice(0, 5).map(({ id, label, icon: Icon }) => (
          <button key={id} onClick={() => setView(id)} className={`flex-1 flex flex-col items-center py-2 gap-0.5 text-xs transition-colors ${view===id?'text-blue-600 dark:text-blue-400':'text-slate-400 dark:text-slate-600'}`}>
            <Icon className="w-5 h-5" /><span className="truncate w-full text-center px-0.5">{label.split(' ')[0]}</span>
          </button>
        ))}
      </nav>
    </div>
  );
}

function ExamCalendarPage(_props: AimbysState) {
  const exams = [
    { date: '22 May', subject: 'Mathematics – Class X', time: '10:00 AM', duration: '3h', students: 840, hall: 'Main Block A', status: 'upcoming' },
    { date: '24 May', subject: 'Physics – Class XII', time: '09:00 AM', duration: '3h', students: 420, hall: 'Science Block', status: 'upcoming' },
    { date: '26 May', subject: 'Chemistry – Class XII', time: '09:00 AM', duration: '3h', students: 418, hall: 'Science Block', status: 'upcoming' },
    { date: '28 May', subject: 'English Core – All Classes', time: '08:30 AM', duration: '3h', students: 2840, hall: 'All Blocks', status: 'upcoming' },
    { date: '18 May', subject: 'Biology – Class XI', time: '10:00 AM', duration: '3h', students: 310, hall: 'Bio Lab Block', status: 'completed' },
    { date: '15 May', subject: 'Computer Science – Class XII', time: '10:00 AM', duration: '3h', students: 240, hall: 'Computer Lab', status: 'completed' },
  ];
  return (
    <div className="space-y-5 pb-8">
      <div><div className="text-slate-400 text-xs mb-1">Institute / Calendar</div><h1 className="text-slate-900 dark:text-white font-bold text-xl">Exam Calendar</h1></div>
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <table className="w-full">
          <thead className="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-800">
            <tr>{['Date','Examination','Time','Duration','Students','Venue','Status'].map(h=><th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider whitespace-nowrap">{h}</th>)}</tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {exams.map((e,i)=>(
              <tr key={i} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <td className="px-5 py-3.5 text-slate-900 dark:text-slate-100 font-semibold text-sm">{e.date}</td>
                <td className="px-5 py-3.5 text-slate-800 dark:text-slate-200 text-sm">{e.subject}</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{e.time}</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{e.duration}</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{e.students.toLocaleString()}</td>
                <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{e.hall}</td>
                <td className="px-5 py-3.5">
                  <span className={`px-2.5 py-1 rounded text-xs font-semibold ${e.status==='upcoming'?'bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-400':'bg-green-50 dark:bg-green-950/30 text-green-700 dark:text-green-400'}`}>{e.status==='upcoming'?'Upcoming':'Completed'}</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
