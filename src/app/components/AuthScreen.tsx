import { useState } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import { Brain, Shield, Users, Eye, EyeOff, ArrowLeft, Sparkles, CheckCircle } from 'lucide-react';
import { AppState, UserRole } from '../App';

export function AuthScreen({ setView, setRole, role, darkMode, setDarkMode }: AppState) {
  const [isLogin, setIsLogin] = useState(true);
  const [selectedRole, setSelectedRole] = useState<UserRole>(role || 'user');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [name, setName] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setTimeout(() => {
      setRole(selectedRole);
      if (selectedRole === 'admin') {
        setView('admin-dashboard');
      } else {
        setView('user-home');
      }
      setLoading(false);
    }, 1200);
  };

  return (
    <div className="min-h-screen flex">
      {/* Left Panel */}
      <div className="hidden lg:flex flex-col justify-between w-1/2 bg-gradient-to-br from-indigo-600 via-violet-600 to-purple-700 p-12 relative overflow-hidden">
        <div className="absolute top-0 right-0 w-96 h-96 bg-white/10 rounded-full -translate-y-1/2 translate-x-1/2 blur-3xl" />
        <div className="absolute bottom-0 left-0 w-80 h-80 bg-black/10 rounded-full translate-y-1/2 -translate-x-1/2 blur-3xl" />

        <div className="relative">
          <div className="flex items-center gap-3 mb-16">
            <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur flex items-center justify-center">
              <Brain className="w-5 h-5 text-white" />
            </div>
            <span className="text-white font-bold text-xl">QuizForge</span>
          </div>

          <h1 className="text-5xl font-black text-white leading-tight mb-6">
            {selectedRole === 'admin' ? 'Manage Quizzes at Scale' : 'Learn Smarter, Not Harder'}
          </h1>
          <p className="text-indigo-200 text-lg leading-relaxed">
            {selectedRole === 'admin'
              ? 'Upload Excel files, manage question banks, track performance, and drive engagement across your entire organization.'
              : 'Take adaptive quizzes, compete on leaderboards, track your weaknesses, and level up with personalized insights.'
            }
          </p>
        </div>

        <div className="relative space-y-4">
          {(selectedRole === 'admin'
            ? ['Excel quiz upload in seconds', 'Real-time analytics dashboard', 'User performance tracking', 'Category & difficulty management']
            : ['500+ quiz categories', 'Adaptive difficulty system', 'Live competitive quizzes', 'Detailed performance insights']
          ).map(item => (
            <div key={item} className="flex items-center gap-3 text-white">
              <CheckCircle className="w-5 h-5 text-indigo-200 flex-shrink-0" />
              <span className="text-indigo-100">{item}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Right Panel */}
      <div className="flex-1 flex flex-col items-center justify-center p-8 bg-white dark:bg-slate-950">
        <div className="w-full max-w-md">
          {/* Back + Theme */}
          <div className="flex items-center justify-between mb-8">
            <button
              onClick={() => setView('onboarding')}
              className="flex items-center gap-2 text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors"
            >
              <ArrowLeft className="w-4 h-4" />
              Back
            </button>
            <button
              onClick={() => setDarkMode(!darkMode)}
              className="p-2 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors"
            >
              {darkMode ? '☀️' : '🌙'}
            </button>
          </div>

          {/* Role Toggle */}
          <div className="p-1 bg-slate-100 dark:bg-slate-900 rounded-2xl flex gap-1 mb-8">
            {(['admin', 'user'] as UserRole[]).map(r => (
              <button
                key={r}
                onClick={() => setSelectedRole(r)}
                className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-xl font-semibold transition-all ${
                  selectedRole === r
                    ? 'bg-white dark:bg-slate-800 text-slate-900 dark:text-white shadow-md'
                    : 'text-slate-500 dark:text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
                }`}
              >
                {r === 'admin' ? <Shield className="w-4 h-4" /> : <Users className="w-4 h-4" />}
                {r === 'admin' ? 'Admin' : 'Student'}
              </button>
            ))}
          </div>

          {/* Header */}
          <div className="mb-8">
            <div className="flex items-center gap-2 mb-2">
              <Sparkles className="w-5 h-5 text-indigo-500" />
              <span className="text-indigo-600 dark:text-indigo-400 font-semibold">
                {selectedRole === 'admin' ? 'Admin Portal' : 'Student Portal'}
              </span>
            </div>
            <h2 className="text-3xl font-black text-slate-900 dark:text-white">
              {isLogin ? 'Welcome back' : 'Create account'}
            </h2>
            <p className="text-slate-500 dark:text-slate-400 mt-1">
              {isLogin ? 'Sign in to your account' : 'Start your learning journey'}
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <AnimatePresence>
              {!isLogin && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  exit={{ opacity: 0, height: 0 }}
                >
                  <label className="block text-slate-700 dark:text-slate-300 mb-2">Full Name</label>
                  <input
                    type="text"
                    value={name}
                    onChange={e => setName(e.target.value)}
                    placeholder="Alex Johnson"
                    className="w-full px-4 py-3 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400 transition-all"
                  />
                </motion.div>
              )}
            </AnimatePresence>

            <div>
              <label className="block text-slate-700 dark:text-slate-300 mb-2">Email</label>
              <input
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder={selectedRole === 'admin' ? 'admin@school.edu' : 'student@school.edu'}
                className="w-full px-4 py-3 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400 transition-all"
              />
            </div>

            <div>
              <label className="block text-slate-700 dark:text-slate-300 mb-2">Password</label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full px-4 py-3 pr-12 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400 transition-all"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 transition-colors"
                >
                  {showPassword ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
                </button>
              </div>
            </div>

            {isLogin && (
              <div className="flex justify-end">
                <button type="button" className="text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300 transition-colors">
                  Forgot password?
                </button>
              </div>
            )}

            <motion.button
              type="submit"
              whileHover={{ scale: 1.01 }}
              whileTap={{ scale: 0.99 }}
              disabled={loading}
              className="w-full py-4 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl font-semibold hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25 disabled:opacity-70 flex items-center justify-center gap-2"
            >
              {loading ? (
                <>
                  <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  Signing in...
                </>
              ) : (
                isLogin ? `Sign in as ${selectedRole === 'admin' ? 'Admin' : 'Student'}` : 'Create Account'
              )}
            </motion.button>

            <div className="relative my-4">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-slate-200 dark:border-slate-800" />
              </div>
              <div className="relative flex justify-center">
                <span className="px-4 bg-white dark:bg-slate-950 text-slate-500 dark:text-slate-500">or continue with</span>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              {['Google', 'Microsoft'].map(provider => (
                <button
                  key={provider}
                  type="button"
                  className="py-3 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors font-medium"
                >
                  {provider}
                </button>
              ))}
            </div>
          </form>

          <p className="text-center text-slate-500 dark:text-slate-400 mt-6">
            {isLogin ? "Don't have an account? " : 'Already have an account? '}
            <button
              onClick={() => setIsLogin(!isLogin)}
              className="text-indigo-600 dark:text-indigo-400 font-semibold hover:text-indigo-700 dark:hover:text-indigo-300 transition-colors"
            >
              {isLogin ? 'Sign up' : 'Sign in'}
            </button>
          </p>

          <p className="text-center text-slate-400 dark:text-slate-600 text-sm mt-4">
            Demo: click "Sign in" to enter without credentials
          </p>
        </div>
      </div>
    </div>
  );
}
