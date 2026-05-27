import { useState } from 'react';
import { Lock, Eye, EyeOff, ShieldAlert, CheckCircle } from 'lucide-react';
import { AimbysState } from '../../../App';

const rules = [
  { label: 'At least 8 characters', test: (p: string) => p.length >= 8 },
  { label: 'One uppercase letter', test: (p: string) => /[A-Z]/.test(p) },
  { label: 'One number', test: (p: string) => /[0-9]/.test(p) },
];

export function ForcePasswordChange({ setMustChangePassword, setView, darkMode }: AimbysState) {
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');

  const allRulesPassed = rules.every(r => r.test(newPassword));
  const passwordsMatch = newPassword === confirmPassword && confirmPassword.length > 0;
  const canSubmit = allRulesPassed && passwordsMatch;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) {
      if (!allRulesPassed) setError('Password does not meet the requirements.');
      else if (!passwordsMatch) setError('Passwords do not match.');
      return;
    }
    setError('');
    setSubmitted(true);
    setTimeout(() => {
      setMustChangePassword(false);
      setView('std-dashboard');
    }, 1500);
  };

  return (
    <div className={`min-h-screen flex items-center justify-center p-6 ${darkMode ? 'dark bg-slate-950' : 'bg-slate-100'}`}>
      <div className="w-full max-w-md">
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl shadow-lg overflow-hidden">
          {/* Header */}
          <div className="px-6 py-5 border-b border-slate-200 dark:border-slate-800 flex items-center gap-3" style={{ background: '#0d1b2e' }}>
            <div className="w-10 h-10 rounded flex items-center justify-center flex-shrink-0" style={{ background: '#15803d' }}>
              <ShieldAlert className="w-5 h-5 text-white" />
            </div>
            <div>
              <div className="text-white font-bold">Password Reset Required</div>
              <div className="text-slate-400 text-xs mt-0.5">You must set a new password before continuing</div>
            </div>
          </div>

          <div className="px-6 py-6">
            {submitted ? (
              <div className="flex flex-col items-center gap-3 py-6 text-center">
                <CheckCircle className="w-12 h-12 text-green-500" />
                <div className="text-slate-900 dark:text-white font-bold text-lg">Password Updated</div>
                <div className="text-slate-500 dark:text-slate-400 text-sm">Redirecting to your dashboard…</div>
              </div>
            ) : (
              <>
                <p className="text-slate-600 dark:text-slate-400 text-sm mb-5">
                  Your account was created with a temporary password. Please set a new secure password to access the platform.
                </p>

                <form onSubmit={handleSubmit} className="space-y-4">
                  <div>
                    <label className="block text-slate-700 dark:text-slate-300 text-sm font-semibold mb-1.5">New Password</label>
                    <div className="relative">
                      <input
                        type={showNew ? 'text' : 'password'}
                        value={newPassword}
                        onChange={e => { setNewPassword(e.target.value); setError(''); }}
                        placeholder="Enter new password"
                        className="w-full px-3.5 py-2.5 pr-10 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded text-slate-900 dark:text-white placeholder-slate-400 outline-none focus:border-green-500 focus:ring-2 focus:ring-green-500/20 transition-all text-sm"
                      />
                      <button type="button" onClick={() => setShowNew(v => !v)} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300">
                        {showNew ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                      </button>
                    </div>

                    {/* Password rules */}
                    {newPassword.length > 0 && (
                      <ul className="mt-2 space-y-1">
                        {rules.map(r => (
                          <li key={r.label} className={`flex items-center gap-1.5 text-xs ${r.test(newPassword) ? 'text-green-600 dark:text-green-400' : 'text-slate-400 dark:text-slate-500'}`}>
                            <CheckCircle className="w-3 h-3 flex-shrink-0" />
                            {r.label}
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  <div>
                    <label className="block text-slate-700 dark:text-slate-300 text-sm font-semibold mb-1.5">Confirm Password</label>
                    <div className="relative">
                      <input
                        type={showConfirm ? 'text' : 'password'}
                        value={confirmPassword}
                        onChange={e => { setConfirmPassword(e.target.value); setError(''); }}
                        placeholder="Re-enter new password"
                        className={`w-full px-3.5 py-2.5 pr-10 bg-white dark:bg-slate-800 border rounded text-slate-900 dark:text-white placeholder-slate-400 outline-none focus:ring-2 transition-all text-sm ${
                          confirmPassword.length > 0 && !passwordsMatch
                            ? 'border-red-400 focus:border-red-500 focus:ring-red-500/20'
                            : 'border-slate-300 dark:border-slate-700 focus:border-green-500 focus:ring-green-500/20'
                        }`}
                      />
                      <button type="button" onClick={() => setShowConfirm(v => !v)} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300">
                        {showConfirm ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                      </button>
                    </div>
                    {confirmPassword.length > 0 && !passwordsMatch && (
                      <p className="text-red-500 text-xs mt-1">Passwords do not match</p>
                    )}
                    {confirmPassword.length > 0 && passwordsMatch && (
                      <p className="text-green-600 dark:text-green-400 text-xs mt-1 flex items-center gap-1"><CheckCircle className="w-3 h-3" />Passwords match</p>
                    )}
                  </div>

                  {error && (
                    <div className="px-3 py-2 bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-800 rounded text-red-600 dark:text-red-400 text-sm">
                      {error}
                    </div>
                  )}

                  <button
                    type="submit"
                    disabled={!canSubmit}
                    className="w-full py-3 rounded font-semibold text-white transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                    style={{ background: '#15803d' }}
                  >
                    <Lock className="w-4 h-4" />
                    Set New Password & Continue
                  </button>
                </form>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
