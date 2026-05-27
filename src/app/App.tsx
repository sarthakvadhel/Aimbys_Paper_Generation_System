import { useState } from 'react';
import { LandingPage } from './components/aimbys/LandingPage';
import { SuperAdminShell } from './components/aimbys/superadmin/SuperAdminShell';
import { InstituteShell } from './components/aimbys/institute/InstituteShell';
import { TeacherShell } from './components/aimbys/teacher/TeacherShell';
import { StudentShell } from './components/aimbys/student/StudentShell';

export type AppRole = 'superadmin' | 'institute' | 'teacher' | 'student';

export type AppView =
  | 'landing'
  | 'sa-dashboard' | 'sa-institutes' | 'sa-analytics' | 'sa-audit' | 'sa-system' | 'sa-licenses' | 'sa-security' | 'sa-broadcasts'
  | 'inst-dashboard' | 'inst-users' | 'inst-questionbank' | 'inst-calendar' | 'inst-analytics' | 'inst-papers' | 'inst-settings' | 'inst-approvals'
  | 'tchr-dashboard' | 'tchr-papergen' | 'tchr-blueprint' | 'tchr-evaluation' | 'tchr-reports' | 'tchr-questionbank' | 'tchr-ide'
  | 'std-dashboard' | 'std-exam' | 'std-results' | 'std-analytics' | 'std-leaderboard';

export interface AimbysState {
  view: AppView;
  setView: (v: AppView) => void;
  role: AppRole | null;
  setRole: (r: AppRole) => void;
  darkMode: boolean;
  setDarkMode: (v: boolean) => void;
  examActive: boolean;
  setExamActive: (v: boolean) => void;
  mustChangePassword: boolean;
  setMustChangePassword: (v: boolean) => void;
}

export default function App() {
  const [view, setView] = useState<AppView>('landing');
  const [role, setRole] = useState<AppRole | null>(null);
  const [darkMode, setDarkMode] = useState(false);
  const [examActive, setExamActive] = useState(false);
  const [mustChangePassword, setMustChangePassword] = useState(false);

  const state: AimbysState = { view, setView, role, setRole, darkMode, setDarkMode, examActive, setExamActive, mustChangePassword, setMustChangePassword };

  return (
    <div className={darkMode ? 'dark' : ''} style={{ fontFamily: "'Inter', 'Segoe UI', Arial, sans-serif" }}>
      <div className="min-h-screen bg-slate-100 dark:bg-slate-950 text-slate-900 dark:text-slate-100">
        {view === 'landing' && <LandingPage {...state} />}
        {role === 'superadmin' && view.startsWith('sa-') && <SuperAdminShell {...state} />}
        {role === 'institute' && view.startsWith('inst-') && <InstituteShell {...state} />}
        {role === 'teacher' && view.startsWith('tchr-') && <TeacherShell {...state} />}
        {role === 'student' && view.startsWith('std-') && <StudentShell {...state} />}
      </div>
    </div>
  );
}
