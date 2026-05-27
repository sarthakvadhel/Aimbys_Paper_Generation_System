import { useState } from 'react';
import {
  LayoutDashboard, Building2, BarChart3, Shield, Server, CreditCard,
  Bell, Search, Menu, X, LogOut, ChevronRight, Sun, Moon,
  Radio, Activity, Settings, Users
} from 'lucide-react';
import { AimbysState, AppView } from '../../../App';
import { SuperAdminDashboard } from './SuperAdminDashboard';
import { InstituteManagement } from './InstituteManagement';
import { GlobalAnalytics } from './GlobalAnalytics';
import { AuditLogs } from './AuditLogs';
import { SystemHealth } from './SystemHealth';

const NAV = [
  { id: 'sa-dashboard' as AppView, label: 'Dashboard', icon: LayoutDashboard, section: 'Main' },
  { id: 'sa-institutes' as AppView, label: 'Institutes', icon: Building2, section: 'Main' },
  { id: 'sa-analytics' as AppView, label: 'Global Analytics', icon: BarChart3, section: 'Main' },
  { id: 'sa-licenses' as AppView, label: 'Licenses', icon: CreditCard, section: 'Management' },
  { id: 'sa-security' as AppView, label: 'Security Monitor', icon: Shield, section: 'Management' },
  { id: 'sa-system' as AppView, label: 'System Health', icon: Server, section: 'Management' },
  { id: 'sa-audit' as AppView, label: 'Audit Logs', icon: Activity, section: 'Governance' },
  { id: 'sa-broadcasts' as AppView, label: 'Broadcasts', icon: Radio, section: 'Governance' },
];

const sections = ['Main', 'Management', 'Governance'];

export function SuperAdminShell(props: AimbysState) {
  const { view, setView, darkMode, setDarkMode } = props;
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [alerts] = useState(4);

  const renderContent = () => {
    if (view === 'sa-institutes') return <InstituteManagement {...props} />;
    if (view === 'sa-analytics') return <GlobalAnalytics {...props} />;
    if (view === 'sa-audit') return <AuditLogs {...props} />;
    if (view === 'sa-system') return <SystemHealth {...props} />;
    return <SuperAdminDashboard {...props} />;
  };

  const SidebarContent = () => (
    <div className="flex flex-col h-full">
      {/* Brand */}
      <div className="px-5 py-5 border-b border-white/10">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded flex items-center justify-center flex-shrink-0" style={{ background: '#7c3aed' }}>
              <span className="text-white font-black text-sm">A</span>
            </div>
            <div>
              <div className="text-white font-bold text-sm leading-none">AIMBYS</div>
              <div className="text-purple-300 text-xs mt-0.5">Super Administrator</div>
            </div>
          </div>
          <button onClick={() => setSidebarOpen(false)} className="lg:hidden text-white/40 hover:text-white/80">
            <X className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto py-4 px-3">
        {sections.map(section => {
          const items = NAV.filter(n => n.section === section);
          return (
            <div key={section} className="mb-5">
              <div className="text-white/30 text-xs font-semibold uppercase tracking-widest px-3 mb-2">{section}</div>
              {items.map(({ id, label, icon: Icon }) => {
                const active = view === id;
                return (
                  <button key={id} onClick={() => { setView(id); setSidebarOpen(false); }}
                    className={`w-full flex items-center gap-3 px-3 py-2.5 rounded mb-0.5 text-left transition-all ${active ? 'text-white' : 'text-white/50 hover:text-white/80 hover:bg-white/5'}`}
                    style={active ? { background: '#7c3aed' } : {}}
                  >
                    <Icon className="w-4 h-4 flex-shrink-0" />
                    <span className="text-sm font-medium">{label}</span>
                    {active && <ChevronRight className="w-3 h-3 ml-auto opacity-70" />}
                  </button>
                );
              })}
            </div>
          );
        })}
      </nav>

      {/* User */}
      <div className="px-3 py-4 border-t border-white/10">
        <div className="flex items-center gap-3 px-3 py-2.5 rounded hover:bg-white/5 transition-colors cursor-pointer">
          <div className="w-8 h-8 rounded-full flex items-center justify-center text-white text-sm font-bold flex-shrink-0" style={{ background: '#7c3aed' }}>SA</div>
          <div className="flex-1 min-w-0">
            <div className="text-white text-sm font-semibold truncate">Super Admin</div>
            <div className="text-white/40 text-xs truncate">admin@aimbys.in</div>
          </div>
          <button onClick={() => props.setView('landing')} className="text-white/30 hover:text-white/70 transition-colors" title="Sign out">
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  );

  return (
    <div className="flex h-screen overflow-hidden" style={{ background: '#f0f4f8' }}>
      {sidebarOpen && <div className="fixed inset-0 bg-black/50 z-30 lg:hidden" onClick={() => setSidebarOpen(false)} />}

      {/* Sidebar */}
      <aside className={`fixed lg:static top-0 left-0 h-full z-40 w-64 flex-shrink-0 transition-transform duration-300 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}`} style={{ background: '#0d1b2e' }}>
        <SidebarContent />
      </aside>

      {/* Main */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top bar */}
        <header className="h-14 flex items-center justify-between px-4 md:px-6 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 flex-shrink-0">
          <div className="flex items-center gap-3">
            <button onClick={() => setSidebarOpen(true)} className="lg:hidden text-slate-500 p-1">
              <Menu className="w-5 h-5" />
            </button>
            <div className="hidden md:flex items-center gap-2 px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded w-72">
              <Search className="w-4 h-4 text-slate-400" />
              <input type="text" placeholder="Search institutes, users, logs..." className="bg-transparent text-slate-700 dark:text-slate-300 placeholder-slate-400 outline-none flex-1 text-sm" />
              <kbd className="px-1.5 py-0.5 bg-slate-200 dark:bg-slate-700 rounded text-slate-400 text-xs">⌘K</kbd>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button onClick={() => setDarkMode(!darkMode)} className="p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </button>
            <button className="relative p-2 rounded bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
              <Bell className="w-4 h-4" />
              {alerts > 0 && <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-red-500 text-white text-xs rounded-full flex items-center justify-center font-bold">{alerts}</span>}
            </button>
            <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 bg-purple-50 dark:bg-purple-950/30 border border-purple-200 dark:border-purple-800 rounded text-purple-700 dark:text-purple-400 text-sm">
              <Shield className="w-3.5 h-3.5" />
              Super Admin
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-4 md:p-6">{renderContent()}</main>
      </div>

      {/* Mobile bottom nav */}
      <nav className="lg:hidden fixed bottom-0 left-0 right-0 z-20 flex bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-800">
        {NAV.slice(0, 5).map(({ id, label, icon: Icon }) => (
          <button key={id} onClick={() => setView(id)} className={`flex-1 flex flex-col items-center py-2 gap-0.5 text-xs transition-colors ${view === id ? 'text-purple-600 dark:text-purple-400' : 'text-slate-400 dark:text-slate-600'}`}>
            <Icon className="w-5 h-5" />
            <span className="truncate w-full text-center px-0.5">{label.split(' ')[0]}</span>
          </button>
        ))}
      </nav>
    </div>
  );
}
