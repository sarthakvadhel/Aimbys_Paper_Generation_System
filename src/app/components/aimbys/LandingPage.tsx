import { useState } from 'react';
import { Shield, Lock, Eye, EyeOff, ChevronRight, Server, Globe, Award, Users, BookOpen, FileText, BarChart3, CheckCircle, AlertTriangle } from 'lucide-react';
import { AimbysState, AppRole } from '../../App';

const roleConfig = [
  { id: 'superadmin' as AppRole, label: 'AIMBYS Super Admin', desc: 'Platform & tenant management', color: '#7c3aed', icon: Shield, firstView: 'sa-dashboard' as const },
  { id: 'institute' as AppRole, label: 'Institute Administrator', desc: 'Institution & academic management', color: '#1d4ed8', icon: Globe, firstView: 'inst-dashboard' as const },
  { id: 'teacher' as AppRole, label: 'Teacher / Examiner', desc: 'Paper generation & evaluation', color: '#0369a1', icon: FileText, firstView: 'tchr-dashboard' as const },
  { id: 'student' as AppRole, label: 'Student / Candidate', desc: 'Examination & result access', color: '#15803d', icon: BookOpen, firstView: 'std-dashboard' as const },
];

const stats = [
  { label: 'Institutions', value: '2,400+' },
  { label: 'Registered Users', value: '1.2M+' },
  { label: 'Papers Generated', value: '8,50,000+' },
  { label: 'Exams Conducted', value: '42,000+' },
];

const features = [
  'Blueprint-based paper generation',
  'AI-assisted question tagging',
  'Multi-type question support (MCQ, Descriptive, Coding)',
  'Real-time exam monitoring & integrity',
  'Automated & manual evaluation workflows',
  'Role-based access with full audit trail',
  'National-scale analytics & reporting',
  'Compliance-ready with data governance',
];

export function LandingPage(props: AimbysState) {
  const { setView, setRole, darkMode, setDarkMode } = props;
  const [selectedRole, setSelectedRole] = useState<AppRole>('institute');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [step, setStep] = useState<'role' | 'credentials'>('role');

  const selected = roleConfig.find(r => r.id === selectedRole)!;

  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setTimeout(() => {
      setRole(selectedRole);
      if (selectedRole === 'student') {
        props.setMustChangePassword(true);
      }
      setView(selected.firstView);
      setLoading(false);
    }, 1000);
  };

  return (
    <div className="min-h-screen flex" style={{ background: '#f0f4f8' }}>
      {/* Left institutional panel */}
      <div className="hidden lg:flex flex-col w-[52%] relative overflow-hidden" style={{ background: '#0d1b2e' }}>
        {/* Top bar */}
        <div className="flex items-center justify-between px-10 pt-8 pb-6 border-b border-white/10">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded flex items-center justify-center" style={{ background: '#1d4ed8' }}>
              <span className="text-white font-black text-lg">A</span>
            </div>
            <div>
              <div className="text-white font-bold tracking-wide">AIMBYS Solutions</div>
              <div className="text-slate-400 text-xs tracking-widest uppercase">Assessment Platform v1</div>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />
            <span className="text-slate-400 text-xs">All Systems Operational</span>
          </div>
        </div>

        {/* Hero content */}
        <div className="flex-1 px-10 pt-12 pb-6">
          <div className="mb-3">
            <span className="inline-flex items-center gap-2 px-3 py-1.5 border border-blue-500/40 text-blue-400 text-xs font-semibold tracking-wider uppercase rounded" style={{ background: 'rgba(29,78,216,0.12)' }}>
              <Shield className="w-3 h-3" />
              Government-Grade Secure Platform
            </span>
          </div>
          <h1 className="text-white font-black leading-tight mb-4" style={{ fontSize: '2.4rem' }}>
            National Assessment &<br />
            <span style={{ color: '#60a5fa' }}>Paper Generation</span><br />
            Ecosystem
          </h1>
          <p className="text-slate-300 text-base leading-relaxed mb-10 max-w-lg">
            A comprehensive, enterprise-grade platform for paper creation, examination conduct, evaluation, and advanced analytics — trusted by institutions across the country.
          </p>

          {/* Stats row */}
          <div className="grid grid-cols-4 gap-4 mb-10">
            {stats.map(s => (
              <div key={s.label} className="text-center py-4 rounded border border-white/10" style={{ background: 'rgba(255,255,255,0.04)' }}>
                <div className="text-white font-black text-xl">{s.value}</div>
                <div className="text-slate-400 text-xs mt-1">{s.label}</div>
              </div>
            ))}
          </div>

          {/* Feature list */}
          <div className="grid grid-cols-2 gap-2">
            {features.map(f => (
              <div key={f} className="flex items-start gap-2">
                <CheckCircle className="w-4 h-4 text-blue-400 flex-shrink-0 mt-0.5" />
                <span className="text-slate-300 text-sm leading-snug">{f}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Footer bar */}
        <div className="px-10 py-4 border-t border-white/10 flex items-center justify-between">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-1.5 text-slate-400 text-xs"><Server className="w-3.5 h-3.5" />ISO 27001 Certified</div>
            <div className="flex items-center gap-1.5 text-slate-400 text-xs"><Lock className="w-3.5 h-3.5" />256-bit TLS Encryption</div>
            <div className="flex items-center gap-1.5 text-slate-400 text-xs"><Award className="w-3.5 h-3.5" />CERT-In Compliant</div>
          </div>
          <span className="text-slate-500 text-xs">© 2026 AIMBYS Solutions Pvt. Ltd.</span>
        </div>
      </div>

      {/* Right login panel */}
      <div className="flex-1 flex flex-col">
        {/* Mobile logo */}
        <div className="lg:hidden flex items-center justify-between px-6 pt-6 pb-4" style={{ background: '#0d1b2e' }}>
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded flex items-center justify-center" style={{ background: '#1d4ed8' }}>
              <span className="text-white font-black">A</span>
            </div>
            <span className="text-white font-bold">AIMBYS Solutions</span>
          </div>
        </div>

        <div className="flex-1 flex items-center justify-center p-6 md:p-12">
          <div className="w-full max-w-md">
            {/* Header */}
            <div className="mb-8">
              <h2 className="text-slate-900 dark:text-white font-black mb-1" style={{ fontSize: '1.75rem' }}>Secure Sign In</h2>
              <p className="text-slate-500 dark:text-slate-400 text-sm">AIMBYS National Assessment Platform</p>
            </div>

            {/* Role Selection */}
            <div className="mb-6">
              <label className="block text-slate-700 dark:text-slate-300 text-sm font-semibold mb-3 uppercase tracking-wide">Select Your Role</label>
              <div className="space-y-2">
                {roleConfig.map(role => (
                  <button
                    key={role.id}
                    onClick={() => setSelectedRole(role.id)}
                    className={`w-full flex items-center gap-3 p-3 rounded border-2 transition-all text-left ${selectedRole === role.id ? 'border-blue-600 bg-blue-50 dark:bg-blue-950/30' : 'border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 hover:border-slate-300 dark:hover:border-slate-600'}`}
                  >
                    <div className="w-8 h-8 rounded flex items-center justify-center flex-shrink-0" style={{ background: selectedRole === role.id ? role.color : '#e2e8f0' }}>
                      <role.icon className={`w-4 h-4 ${selectedRole === role.id ? 'text-white' : 'text-slate-500'}`} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className={`font-semibold text-sm ${selectedRole === role.id ? 'text-blue-700 dark:text-blue-400' : 'text-slate-800 dark:text-slate-200'}`}>{role.label}</div>
                      <div className="text-slate-400 text-xs">{role.desc}</div>
                    </div>
                    {selectedRole === role.id && <ChevronRight className="w-4 h-4 text-blue-600 dark:text-blue-400 flex-shrink-0" />}
                  </button>
                ))}
              </div>
            </div>

            {/* Credentials Form */}
            <form onSubmit={handleLogin} className="space-y-4">
              <div>
                <label className="block text-slate-700 dark:text-slate-300 text-sm font-semibold mb-1.5">Username / Employee ID</label>
                <input
                  type="text"
                  value={username}
                  onChange={e => setUsername(e.target.value)}
                  placeholder={selectedRole === 'student' ? 'Student Roll No. / ID' : 'Username or Employee ID'}
                  className="w-full px-3.5 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded text-slate-900 dark:text-white placeholder-slate-400 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 transition-all text-sm"
                />
              </div>
              <div>
                <label className="block text-slate-700 dark:text-slate-300 text-sm font-semibold mb-1.5">Password</label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    placeholder="••••••••••"
                    className="w-full px-3.5 py-2.5 pr-10 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded text-slate-900 dark:text-white placeholder-slate-400 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 transition-all text-sm"
                  />
                  <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300">
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              <div className="flex items-center justify-between text-sm">
                <label className="flex items-center gap-2 text-slate-600 dark:text-slate-400 cursor-pointer">
                  <input type="checkbox" className="rounded border-slate-300" />
                  Remember this device
                </label>
                <button type="button" className="text-blue-600 dark:text-blue-400 hover:underline">Forgot password?</button>
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full py-3 rounded font-semibold text-white transition-all disabled:opacity-70 flex items-center justify-center gap-2"
                style={{ background: selected.color }}
              >
                {loading ? (
                  <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />Authenticating...</>
                ) : (
                  <><Lock className="w-4 h-4" />Sign In Securely</>
                )}
              </button>

              <p className="text-center text-slate-400 text-xs">Demo: click "Sign In" without credentials to enter</p>
            </form>

            {/* Security badges */}
            <div className="mt-8 pt-6 border-t border-slate-200 dark:border-slate-800 flex items-center justify-center gap-6">
              {[['🔒', 'End-to-End Encrypted'], ['🛡️', 'CERT-In Compliant'], ['📋', 'Audit Logged']].map(([icon, label]) => (
                <div key={label} className="flex flex-col items-center gap-1">
                  <span className="text-lg">{icon}</span>
                  <span className="text-slate-400 text-xs text-center">{label}</span>
                </div>
              ))}
            </div>

            <div className="mt-4 flex items-center gap-2 p-3 bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800 rounded text-amber-700 dark:text-amber-400 text-xs">
              <AlertTriangle className="w-4 h-4 flex-shrink-0" />
              This system is for authorized personnel only. All access is logged and monitored.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
