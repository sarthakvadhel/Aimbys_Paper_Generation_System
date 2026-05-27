import { useState } from 'react';
import { Home, Search, Trophy, User, Bell, Brain, Sun, Moon, Zap, LogOut, Menu, X } from 'lucide-react';
import { AppState, AppView } from '../../App';
import { UserHome } from './UserHome';
import { QuizDiscovery } from './QuizDiscovery';
import { QuizInterface } from './QuizInterface';
import { QuizResults } from './QuizResults';
import { Leaderboard } from './Leaderboard';
import { UserProfile } from './UserProfile';

const navItems = [
  { id: 'user-home' as AppView, label: 'Home', icon: Home },
  { id: 'user-discover' as AppView, label: 'Discover', icon: Search },
  { id: 'user-leaderboard' as AppView, label: 'Rankings', icon: Trophy },
  { id: 'user-profile' as AppView, label: 'Profile', icon: User },
];

export function UserShell(props: AppState) {
  const { view, setView, darkMode, setDarkMode } = props;
  const [notifications] = useState(2);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const renderContent = () => {
    switch (view) {
      case 'user-home': return <UserHome {...props} />;
      case 'user-discover': return <QuizDiscovery {...props} />;
      case 'user-quiz': return <QuizInterface {...props} />;
      case 'user-results': return <QuizResults {...props} />;
      case 'user-leaderboard': return <Leaderboard {...props} />;
      case 'user-profile': return <UserProfile {...props} />;
      default: return <UserHome {...props} />;
    }
  };

  const isQuizView = view === 'user-quiz';

  if (isQuizView) {
    return (
      <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
        <QuizInterface {...props} />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col">
      {/* Top Nav */}
      <header className="sticky top-0 z-30 flex items-center justify-between px-4 md:px-6 h-16 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center shadow-md shadow-indigo-500/25">
            <Brain className="w-4 h-4 text-white" />
          </div>
          <span className="font-black text-slate-900 dark:text-white">QuizForge</span>
        </div>

        {/* Desktop Nav */}
        <nav className="hidden md:flex items-center gap-1">
          {navItems.map(({ id, label, icon: Icon }) => {
            const active = view === id;
            return (
              <button
                key={id}
                onClick={() => setView(id)}
                className={`flex items-center gap-2 px-4 py-2 rounded-xl transition-all font-medium ${
                  active
                    ? 'bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400'
                    : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-slate-100'
                }`}
              >
                <Icon className="w-4 h-4" />
                {label}
              </button>
            );
          })}
        </nav>

        <div className="flex items-center gap-2">
          {/* XP Indicator */}
          <div className="hidden sm:flex items-center gap-1.5 px-3 py-1.5 bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 rounded-xl">
            <Zap className="w-3.5 h-3.5 text-amber-500" />
            <span className="text-amber-700 dark:text-amber-400 font-bold text-sm">8,240 XP</span>
          </div>
          <button
            onClick={() => setDarkMode(!darkMode)}
            className="p-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
          >
            {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
          </button>
          <button className="relative p-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
            <Bell className="w-4 h-4" />
            {notifications > 0 && (
              <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-indigo-500 text-white text-xs rounded-full flex items-center justify-center font-bold">
                {notifications}
              </span>
            )}
          </button>
          <button
            onClick={() => setView('user-profile')}
            className="w-9 h-9 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500 flex items-center justify-center text-white font-bold text-sm cursor-pointer hover:ring-2 hover:ring-indigo-500 hover:ring-offset-2 dark:hover:ring-offset-slate-900 transition-all"
          >
            JD
          </button>
          <button
            onClick={() => props.setView('onboarding')}
            className="hidden md:flex p-2.5 rounded-xl text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
            title="Sign out"
          >
            <LogOut className="w-4 h-4" />
          </button>
          <button
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            className="md:hidden p-2 text-slate-500 dark:text-slate-400"
          >
            {mobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
          </button>
        </div>
      </header>

      {/* Mobile Menu */}
      {mobileMenuOpen && (
        <div className="md:hidden fixed inset-0 z-20 pt-16 bg-white dark:bg-slate-900">
          <div className="p-4 space-y-2">
            {navItems.map(({ id, label, icon: Icon }) => (
              <button
                key={id}
                onClick={() => { setView(id); setMobileMenuOpen(false); }}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl transition-all ${
                  view === id ? 'bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
                }`}
              >
                <Icon className="w-5 h-5" />
                <span className="font-medium">{label}</span>
              </button>
            ))}
            <button
              onClick={() => props.setView('onboarding')}
              className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all"
            >
              <LogOut className="w-5 h-5" />
              <span className="font-medium">Sign Out</span>
            </button>
          </div>
        </div>
      )}

      {/* Main Content */}
      <main className="flex-1 px-4 md:px-6 py-6 max-w-6xl mx-auto w-full pb-24 md:pb-6">
        {renderContent()}
      </main>

      {/* Mobile Bottom Nav */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-30 flex bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
        {navItems.map(({ id, label, icon: Icon }) => {
          const active = view === id;
          return (
            <button
              key={id}
              onClick={() => setView(id)}
              className={`flex-1 flex flex-col items-center py-2.5 gap-1 transition-colors ${
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
