import { useState } from 'react';
import {
  LayoutDashboard, Upload, BookOpen, Users, BarChart3,
  Settings, Bell, Menu, X, LogOut, Brain, ChevronRight,
  Sun, Moon, Search
} from 'lucide-react';
import { AppState, AppView } from '../../App';
import { AdminDashboard } from './AdminDashboard';
import { QuizUpload } from './QuizUpload';
import { QuizManagement } from './QuizManagement';
import { UserManagement } from './UserManagement';
import { AdminAnalytics } from './AdminAnalytics';

const navItems = [
  { id: 'admin-dashboard' as AppView, label: 'Dashboard', icon: LayoutDashboard },
  { id: 'admin-upload' as AppView, label: 'Upload Quiz', icon: Upload },
  { id: 'admin-quizzes' as AppView, label: 'Quiz Bank', icon: BookOpen },
  { id: 'admin-users' as AppView, label: 'Users', icon: Users },
  { id: 'admin-analytics' as AppView, label: 'Analytics', icon: BarChart3 },
  { id: 'admin-settings' as AppView, label: 'Settings', icon: Settings },
];

export function AdminShell(props: AppState) {
  const { view, setView, darkMode, setDarkMode } = props;
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [notifications] = useState(3);

  const renderContent = () => {
    switch (view) {
      case 'admin-dashboard': return <AdminDashboard {...props} />;
      case 'admin-upload': return <QuizUpload {...props} />;
      case 'admin-quizzes': return <QuizManagement {...props} />;
      case 'admin-users': return <UserManagement {...props} />;
      case 'admin-analytics': return <AdminAnalytics {...props} />;
      case 'admin-settings': return <AdminSettingsPage {...props} />;
      default: return <AdminDashboard {...props} />;
    }
  };

  return (
    <div className="flex h-screen bg-slate-50 dark:bg-slate-950 overflow-hidden">
      {/* Sidebar Overlay (mobile) */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-30 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside className={`fixed lg:static top-0 left-0 h-full z-40 w-72 flex flex-col bg-white dark:bg-slate-900 border-r border-slate-200 dark:border-slate-800 transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`}>
        {/* Logo */}
        <div className="p-6 border-b border-slate-200 dark:border-slate-800">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center shadow-lg shadow-indigo-500/25">
                <Brain className="w-5 h-5 text-white" />
              </div>
              <div>
                <div className="font-black text-slate-900 dark:text-white">QuizForge</div>
                <div className="text-xs text-indigo-600 dark:text-indigo-400 font-medium">Admin Panel</div>
              </div>
            </div>
            <button onClick={() => setSidebarOpen(false)} className="lg:hidden text-slate-400 hover:text-slate-600 dark:hover:text-slate-200">
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Nav */}
        <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
          <div className="text-xs font-semibold text-slate-400 dark:text-slate-600 uppercase tracking-wider px-3 mb-3">Main Menu</div>
          {navItems.map(({ id, label, icon: Icon }) => {
            const active = view === id;
            return (
              <button
                key={id}
                onClick={() => { setView(id); setSidebarOpen(false); }}
                className={`w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all group ${
                  active
                    ? 'bg-gradient-to-r from-indigo-500 to-violet-600 text-white shadow-lg shadow-indigo-500/25'
                    : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-slate-100'
                }`}
              >
                <Icon className={`w-5 h-5 flex-shrink-0 ${active ? 'text-white' : ''}`} />
                <span className="flex-1 text-left font-medium">{label}</span>
                {active && <ChevronRight className="w-4 h-4 opacity-70" />}
              </button>
            );
          })}
        </nav>

        {/* User Profile */}
        <div className="p-4 border-t border-slate-200 dark:border-slate-800">
          <div className="flex items-center gap-3 p-3 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors cursor-pointer">
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500 flex items-center justify-center text-white font-bold text-sm flex-shrink-0">
              AD
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-slate-900 dark:text-white font-semibold truncate">Admin User</div>
              <div className="text-slate-500 dark:text-slate-400 text-sm truncate">admin@quizforge.io</div>
            </div>
            <button
              onClick={() => props.setView('onboarding')}
              className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors"
              title="Sign out"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top Bar */}
        <header className="h-16 flex items-center justify-between px-6 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex-shrink-0">
          <div className="flex items-center gap-4">
            <button
              onClick={() => setSidebarOpen(true)}
              className="lg:hidden text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
            >
              <Menu className="w-6 h-6" />
            </button>
            <div className="hidden md:flex items-center gap-2 px-4 py-2 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 w-80">
              <Search className="w-4 h-4 text-slate-400" />
              <input
                type="text"
                placeholder="Search quizzes, users..."
                className="bg-transparent text-slate-700 dark:text-slate-300 placeholder-slate-400 dark:placeholder-slate-600 outline-none flex-1 text-sm"
              />
              <kbd className="px-2 py-0.5 bg-slate-200 dark:bg-slate-700 rounded text-slate-500 dark:text-slate-400 text-xs">⌘K</kbd>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setDarkMode(!darkMode)}
              className="p-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
            >
              {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </button>
            <button className="relative p-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              <Bell className="w-4 h-4" />
              {notifications > 0 && (
                <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-rose-500 text-white text-xs rounded-full flex items-center justify-center font-bold">
                  {notifications}
                </span>
              )}
            </button>
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500 flex items-center justify-center text-white font-bold text-sm cursor-pointer">
              AD
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-y-auto p-6">
          {renderContent()}
        </main>
      </div>

      {/* Mobile Bottom Nav */}
      <nav className="lg:hidden fixed bottom-0 left-0 right-0 z-20 flex bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
        {navItems.slice(0, 5).map(({ id, label, icon: Icon }) => {
          const active = view === id;
          return (
            <button
              key={id}
              onClick={() => setView(id)}
              className={`flex-1 flex flex-col items-center py-2 gap-1 transition-colors ${
                active ? 'text-indigo-600 dark:text-indigo-400' : 'text-slate-400 dark:text-slate-600'
              }`}
            >
              <Icon className="w-5 h-5" />
              <span className="text-xs font-medium">{label}</span>
            </button>
          );
        })}
      </nav>
    </div>
  );
}

function AdminSettingsPage({ setDarkMode, darkMode }: AppState) {
  const [emailNotifs, setEmailNotifs] = useState(true);
  const [twoFA, setTwoFA] = useState(false);
  const [autoApprove, setAutoApprove] = useState(true);

  const Toggle = ({ value, onChange }: { value: boolean; onChange: () => void }) => (
    <button
      onClick={onChange}
      className={`w-12 h-6 rounded-full transition-colors relative ${value ? 'bg-indigo-500' : 'bg-slate-300 dark:bg-slate-700'}`}
    >
      <div className={`absolute top-1 w-4 h-4 bg-white rounded-full shadow transition-transform ${value ? 'left-7' : 'left-1'}`} />
    </button>
  );

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-black text-slate-900 dark:text-white">Settings</h1>
        <p className="text-slate-500 dark:text-slate-400">Manage your platform configuration</p>
      </div>
      {[
        { title: 'Appearance', items: [{ label: 'Dark Mode', desc: 'Switch to dark theme', value: darkMode, onChange: () => setDarkMode(!darkMode) }] },
        { title: 'Notifications', items: [{ label: 'Email Notifications', desc: 'Receive quiz submission alerts', value: emailNotifs, onChange: () => setEmailNotifs(!emailNotifs) }] },
        { title: 'Security', items: [{ label: 'Two-Factor Authentication', desc: 'Secure your admin account', value: twoFA, onChange: () => setTwoFA(!twoFA) }] },
        { title: 'Quiz Settings', items: [{ label: 'Auto-Approve Uploads', desc: 'Publish quizzes immediately after upload', value: autoApprove, onChange: () => setAutoApprove(!autoApprove) }] },
      ].map(section => (
        <div key={section.title} className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6">
          <h3 className="font-bold text-slate-900 dark:text-white mb-4">{section.title}</h3>
          {section.items.map(item => (
            <div key={item.label} className="flex items-center justify-between">
              <div>
                <div className="text-slate-900 dark:text-white font-medium">{item.label}</div>
                <div className="text-slate-500 dark:text-slate-400 text-sm">{item.desc}</div>
              </div>
              <Toggle value={item.value} onChange={item.onChange} />
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
