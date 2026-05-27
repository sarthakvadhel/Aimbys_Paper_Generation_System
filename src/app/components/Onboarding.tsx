import { motion } from 'motion/react';
import {
  Brain, Upload, BarChart3, Trophy, Users, Zap,
  ArrowRight, Star, Clock, BookOpen, CheckCircle,
  Sparkles, Shield, Globe
} from 'lucide-react';
import { AppState } from '../App';

const features = [
  {
    icon: Upload,
    title: 'Excel-Powered Quizzes',
    desc: 'Upload quizzes instantly via pre-formatted Excel sheets. Auto-parse questions, answers, and explanations.',
    color: 'from-indigo-500 to-indigo-700',
    lightBg: 'bg-indigo-50 dark:bg-indigo-950/30',
    iconColor: 'text-indigo-500',
  },
  {
    icon: Brain,
    title: 'Adaptive Intelligence',
    desc: 'AI-powered recommendations identify weak areas and serve personalized practice sessions.',
    color: 'from-violet-500 to-violet-700',
    lightBg: 'bg-violet-50 dark:bg-violet-950/30',
    iconColor: 'text-violet-500',
  },
  {
    icon: BarChart3,
    title: 'Deep Analytics',
    desc: 'Real-time dashboards track performance trends, category breakdowns, and completion rates.',
    color: 'from-cyan-500 to-cyan-700',
    lightBg: 'bg-cyan-50 dark:bg-cyan-950/30',
    iconColor: 'text-cyan-500',
  },
  {
    icon: Trophy,
    title: 'Gamified Leaderboards',
    desc: 'Live rankings, XP points, achievement badges, and streak rewards keep users engaged.',
    color: 'from-amber-500 to-amber-700',
    lightBg: 'bg-amber-50 dark:bg-amber-950/30',
    iconColor: 'text-amber-500',
  },
  {
    icon: Clock,
    title: 'Timed & Scheduled',
    desc: 'Schedule quizzes for specific windows, set per-question timers, and auto-submit on expiry.',
    color: 'from-emerald-500 to-emerald-700',
    lightBg: 'bg-emerald-50 dark:bg-emerald-950/30',
    iconColor: 'text-emerald-500',
  },
  {
    icon: Shield,
    title: 'Role-Based Access',
    desc: 'Distinct admin and user dashboards with granular permissions and secure authentication.',
    color: 'from-rose-500 to-rose-700',
    lightBg: 'bg-rose-50 dark:bg-rose-950/30',
    iconColor: 'text-rose-500',
  },
];

const stats = [
  { value: '50K+', label: 'Active Users', icon: Users },
  { value: '2,400+', label: 'Quizzes Created', icon: BookOpen },
  { value: '98%', label: 'Satisfaction Rate', icon: Star },
  { value: '120+', label: 'Subject Categories', icon: Globe },
];

const testimonials = [
  { name: 'Sarah Chen', role: 'University Professor', text: 'Transformed how I run weekly assessments. The Excel upload feature saves me hours every week.', avatar: 'SC' },
  { name: 'Marcus Williams', role: 'Corporate Trainer', text: 'The analytics dashboard gives us insights we never had before. Engagement is up 340%.', avatar: 'MW' },
  { name: 'Priya Sharma', role: 'EdTech Startup', text: 'Best quiz platform we\'ve tried. The leaderboard feature drives incredible competition.', avatar: 'PS' },
];

export function Onboarding({ setView, setRole, darkMode, setDarkMode }: AppState) {
  return (
    <div className="min-h-screen overflow-x-hidden">
      {/* Navigation */}
      <nav className="fixed top-0 left-0 right-0 z-50 flex items-center justify-between px-6 py-4 bg-white/80 dark:bg-slate-950/80 backdrop-blur-xl border-b border-slate-200 dark:border-slate-800">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center">
            <Brain className="w-4 h-4 text-white" />
          </div>
          <span className="font-bold text-slate-900 dark:text-white">QuizForge</span>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => setDarkMode(!darkMode)}
            className="p-2 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
          >
            {darkMode ? '☀️' : '🌙'}
          </button>
          <button
            onClick={() => setView('auth')}
            className="px-4 py-2 text-slate-700 dark:text-slate-300 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
          >
            Sign In
          </button>
          <button
            onClick={() => setView('auth')}
            className="px-5 py-2 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl hover:from-indigo-600 hover:to-violet-700 transition-all shadow-lg shadow-indigo-500/25"
          >
            Get Started
          </button>
        </div>
      </nav>

      {/* Hero */}
      <section className="relative pt-32 pb-24 px-6 overflow-hidden">
        {/* Background blobs */}
        <div className="absolute top-20 left-1/4 w-96 h-96 bg-indigo-500/20 dark:bg-indigo-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute top-40 right-1/4 w-96 h-96 bg-violet-500/20 dark:bg-violet-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute bottom-0 left-1/2 -translate-x-1/2 w-[600px] h-64 bg-cyan-500/10 dark:bg-cyan-500/5 rounded-full blur-3xl pointer-events-none" />

        <div className="max-w-5xl mx-auto text-center relative">
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
          >
            <div className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-50 dark:bg-indigo-950/60 border border-indigo-200 dark:border-indigo-800 rounded-full text-indigo-700 dark:text-indigo-300 mb-8">
              <Sparkles className="w-4 h-4" />
              <span>Enterprise-Grade Quiz Platform</span>
            </div>
            <h1 className="text-6xl md:text-7xl lg:text-8xl font-black leading-tight mb-6">
              <span className="bg-gradient-to-r from-indigo-600 via-violet-600 to-cyan-500 bg-clip-text text-transparent">
                Quiz & Q&A
              </span>
              <br />
              <span className="text-slate-900 dark:text-white">Platform</span>
            </h1>
            <p className="text-xl text-slate-600 dark:text-slate-400 max-w-2xl mx-auto mb-12 leading-relaxed">
              Create, manage, and deliver quizzes at scale. Upload via Excel, track performance with
              rich analytics, and drive engagement through gamification.
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <motion.button
                whileHover={{ scale: 1.03 }}
                whileTap={{ scale: 0.98 }}
                onClick={() => { setRole('admin'); setView('auth'); }}
                className="flex items-center gap-2 px-8 py-4 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-2xl hover:from-indigo-600 hover:to-violet-700 transition-all shadow-xl shadow-indigo-500/30 w-full sm:w-auto justify-center"
              >
                <Shield className="w-5 h-5" />
                Admin Demo
                <ArrowRight className="w-4 h-4" />
              </motion.button>
              <motion.button
                whileHover={{ scale: 1.03 }}
                whileTap={{ scale: 0.98 }}
                onClick={() => { setRole('user'); setView('auth'); }}
                className="flex items-center gap-2 px-8 py-4 bg-slate-900 dark:bg-white text-white dark:text-slate-900 rounded-2xl hover:bg-slate-800 dark:hover:bg-slate-100 transition-all shadow-xl w-full sm:w-auto justify-center"
              >
                <Users className="w-5 h-5" />
                User Demo
                <ArrowRight className="w-4 h-4" />
              </motion.button>
            </div>
          </motion.div>

          {/* Hero Preview Cards */}
          <motion.div
            initial={{ opacity: 0, y: 40 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8, delay: 0.3 }}
            className="mt-20 grid grid-cols-1 md:grid-cols-3 gap-4 max-w-4xl mx-auto"
          >
            <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-xl text-left">
              <div className="flex items-center gap-3 mb-4">
                <div className="w-10 h-10 rounded-xl bg-indigo-100 dark:bg-indigo-950/50 flex items-center justify-center">
                  <Trophy className="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
                </div>
                <div>
                  <div className="text-slate-500 dark:text-slate-400">Global Rank</div>
                  <div className="text-slate-900 dark:text-white font-bold">#42 of 50,431</div>
                </div>
              </div>
              <div className="text-3xl font-black text-slate-900 dark:text-white">8,240 XP</div>
              <div className="mt-3 h-2 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                <div className="h-full w-3/4 bg-gradient-to-r from-indigo-500 to-violet-500 rounded-full" />
              </div>
              <div className="text-slate-500 dark:text-slate-400 mt-1">760 XP to next level</div>
            </div>

            <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-xl text-left">
              <div className="text-slate-500 dark:text-slate-400 mb-2">Today's Quiz</div>
              <div className="text-slate-900 dark:text-white font-bold mb-1">Advanced React Patterns</div>
              <div className="flex items-center gap-2 mb-4">
                <span className="px-2 py-0.5 bg-emerald-100 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-400 rounded-full text-sm">Live</span>
                <span className="text-slate-500 dark:text-slate-400 text-sm">• 25 questions • 20 min</span>
              </div>
              <div className="flex -space-x-2">
                {['JD', 'KL', 'MN', 'OP', 'QR'].map((i, idx) => (
                  <div key={idx} className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500 border-2 border-white dark:border-slate-900 flex items-center justify-center text-white text-xs font-bold">{i}</div>
                ))}
                <div className="w-8 h-8 rounded-full bg-slate-200 dark:bg-slate-800 border-2 border-white dark:border-slate-900 flex items-center justify-center text-slate-600 dark:text-slate-400 text-xs">+42</div>
              </div>
            </div>

            <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-xl text-left">
              <div className="text-slate-500 dark:text-slate-400 mb-3">Performance Snapshot</div>
              <div className="space-y-2">
                {[{ label: 'JavaScript', pct: 92, color: 'bg-amber-500' }, { label: 'System Design', pct: 74, color: 'bg-indigo-500' }, { label: 'Algorithms', pct: 61, color: 'bg-violet-500' }].map(item => (
                  <div key={item.label}>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-slate-700 dark:text-slate-300">{item.label}</span>
                      <span className="text-slate-500 dark:text-slate-400">{item.pct}%</span>
                    </div>
                    <div className="h-2 bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                      <div className={`h-full ${item.color} rounded-full`} style={{ width: `${item.pct}%` }} />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </motion.div>
        </div>
      </section>

      {/* Stats */}
      <section className="py-16 px-6 bg-gradient-to-r from-indigo-600 to-violet-600">
        <div className="max-w-5xl mx-auto grid grid-cols-2 md:grid-cols-4 gap-8">
          {stats.map(({ value, label, icon: Icon }) => (
            <div key={label} className="text-center text-white">
              <Icon className="w-7 h-7 mx-auto mb-3 opacity-80" />
              <div className="text-4xl font-black mb-1">{value}</div>
              <div className="text-indigo-200">{label}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Features Grid */}
      <section className="py-24 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-black text-slate-900 dark:text-white mb-4">
              Everything You Need to
              <br />
              <span className="bg-gradient-to-r from-indigo-600 to-violet-600 bg-clip-text text-transparent">
                Scale Education
              </span>
            </h2>
            <p className="text-xl text-slate-600 dark:text-slate-400 max-w-2xl mx-auto">
              From a single classroom to enterprise-scale deployments — QuizForge handles it all.
            </p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {features.map(({ icon: Icon, title, desc, lightBg, iconColor }, idx) => (
              <motion.div
                key={title}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: idx * 0.08 }}
                className={`p-6 rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 hover:shadow-xl transition-all group cursor-default`}
              >
                <div className={`w-12 h-12 ${lightBg} rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform`}>
                  <Icon className={`w-6 h-6 ${iconColor}`} />
                </div>
                <h3 className="text-slate-900 dark:text-white font-bold mb-2">{title}</h3>
                <p className="text-slate-600 dark:text-slate-400 leading-relaxed">{desc}</p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* Dual Role Section */}
      <section className="py-24 px-6 bg-slate-50 dark:bg-slate-900/50">
        <div className="max-w-5xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl font-black text-slate-900 dark:text-white mb-4">Built for Both Roles</h2>
            <p className="text-slate-600 dark:text-slate-400">Powerful tools for admins, delightful experience for users.</p>
          </div>
          <div className="grid md:grid-cols-2 gap-8">
            <div className="p-8 bg-white dark:bg-slate-900 rounded-3xl border-2 border-indigo-200 dark:border-indigo-900 shadow-xl shadow-indigo-100/50 dark:shadow-indigo-950/50">
              <div className="w-12 h-12 bg-indigo-100 dark:bg-indigo-950 rounded-2xl flex items-center justify-center mb-6">
                <Shield className="w-6 h-6 text-indigo-600 dark:text-indigo-400" />
              </div>
              <h3 className="text-2xl font-black text-slate-900 dark:text-white mb-4">Admin Panel</h3>
              <ul className="space-y-3 mb-8">
                {['Upload quizzes via Excel', 'Manage question banks', 'Real-time analytics dashboard', 'User & role management', 'Quiz scheduling & activation', 'Performance reports'].map(item => (
                  <li key={item} className="flex items-center gap-3 text-slate-600 dark:text-slate-400">
                    <CheckCircle className="w-5 h-5 text-indigo-500 flex-shrink-0" />
                    {item}
                  </li>
                ))}
              </ul>
              <button onClick={() => { setRole('admin'); setView('auth'); }} className="w-full py-3 bg-gradient-to-r from-indigo-500 to-violet-600 text-white rounded-xl font-semibold hover:from-indigo-600 hover:to-violet-700 transition-all flex items-center justify-center gap-2">
                Explore Admin Panel <ArrowRight className="w-4 h-4" />
              </button>
            </div>

            <div className="p-8 bg-white dark:bg-slate-900 rounded-3xl border-2 border-violet-200 dark:border-violet-900 shadow-xl shadow-violet-100/50 dark:shadow-violet-950/50">
              <div className="w-12 h-12 bg-violet-100 dark:bg-violet-950 rounded-2xl flex items-center justify-center mb-6">
                <Users className="w-6 h-6 text-violet-600 dark:text-violet-400" />
              </div>
              <h3 className="text-2xl font-black text-slate-900 dark:text-white mb-4">User Panel</h3>
              <ul className="space-y-3 mb-8">
                {['Browse & take quizzes', 'Timed quiz experience', 'Instant score & review', 'Strength/weakness analysis', 'Competitive leaderboards', 'Achievements & badges'].map(item => (
                  <li key={item} className="flex items-center gap-3 text-slate-600 dark:text-slate-400">
                    <CheckCircle className="w-5 h-5 text-violet-500 flex-shrink-0" />
                    {item}
                  </li>
                ))}
              </ul>
              <button onClick={() => { setRole('user'); setView('auth'); }} className="w-full py-3 bg-gradient-to-r from-violet-500 to-purple-600 text-white rounded-xl font-semibold hover:from-violet-600 hover:to-purple-700 transition-all flex items-center justify-center gap-2">
                Explore User Panel <ArrowRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Testimonials */}
      <section className="py-24 px-6">
        <div className="max-w-5xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl font-black text-slate-900 dark:text-white mb-4">Loved by Educators</h2>
          </div>
          <div className="grid md:grid-cols-3 gap-6">
            {testimonials.map(({ name, role, text, avatar }) => (
              <div key={name} className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-lg">
                <div className="flex items-center gap-1 mb-4">
                  {[...Array(5)].map((_, i) => <Star key={i} className="w-4 h-4 fill-amber-400 text-amber-400" />)}
                </div>
                <p className="text-slate-600 dark:text-slate-400 italic mb-4">"{text}"</p>
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-400 to-violet-500 flex items-center justify-center text-white font-bold text-sm">{avatar}</div>
                  <div>
                    <div className="text-slate-900 dark:text-white font-semibold">{name}</div>
                    <div className="text-slate-500 dark:text-slate-400 text-sm">{role}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-24 px-6 bg-gradient-to-br from-indigo-600 via-violet-600 to-purple-700">
        <div className="max-w-3xl mx-auto text-center">
          <Zap className="w-12 h-12 text-white mx-auto mb-6 opacity-90" />
          <h2 className="text-5xl font-black text-white mb-4">Ready to Transform Learning?</h2>
          <p className="text-indigo-200 text-xl mb-10">Start building smarter quizzes today. No credit card required.</p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <button onClick={() => { setRole('admin'); setView('auth'); }} className="px-8 py-4 bg-white text-indigo-700 rounded-2xl font-bold hover:bg-indigo-50 transition-all shadow-xl w-full sm:w-auto">
              Admin Dashboard →
            </button>
            <button onClick={() => { setRole('user'); setView('auth'); }} className="px-8 py-4 bg-white/10 backdrop-blur border border-white/20 text-white rounded-2xl font-bold hover:bg-white/20 transition-all w-full sm:w-auto">
              Try as Student
            </button>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-8 px-6 border-t border-slate-200 dark:border-slate-800">
        <div className="max-w-5xl mx-auto flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 rounded-lg bg-gradient-to-br from-indigo-500 to-violet-600 flex items-center justify-center">
              <Brain className="w-3.5 h-3.5 text-white" />
            </div>
            <span className="font-bold text-slate-900 dark:text-white">QuizForge</span>
          </div>
          <div className="text-slate-500 dark:text-slate-400 text-sm">© 2026 QuizForge. Enterprise Quiz & Q&A Platform.</div>
        </div>
      </footer>
    </div>
  );
}
