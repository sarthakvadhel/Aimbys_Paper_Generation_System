import { Building2, Users, FileText, Activity, TrendingUp, TrendingDown, CheckCircle, Clock, AlertTriangle, XCircle, Server, Database, Cpu, HardDrive, Globe, Shield } from 'lucide-react';
import { AreaChart, Area, BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell, PieChart, Pie, Legend } from 'recharts';
import { AimbysState } from '../../../App';

const kpis = [
  { label: 'Total Institutes', value: '2,418', sub: '+12 this month', trend: 'up', icon: Building2, accent: '#1d4ed8', light: '#eff6ff' },
  { label: 'Registered Users', value: '12,84,302', sub: '+8,420 this week', trend: 'up', icon: Users, accent: '#0369a1', light: '#f0f9ff' },
  { label: 'Papers Generated', value: '8,51,240', sub: '+1,420 today', trend: 'up', icon: FileText, accent: '#7c3aed', light: '#f5f3ff' },
  { label: 'Active Exams Now', value: '284', sub: '12,400 candidates online', trend: 'up', icon: Activity, accent: '#15803d', light: '#f0fdf4' },
];

const systemHealth = [
  { service: 'API Gateway', status: 'operational', uptime: '99.98%', latency: '42ms' },
  { service: 'Database Cluster', status: 'operational', uptime: '99.99%', latency: '8ms' },
  { service: 'File Storage', status: 'operational', uptime: '99.97%', latency: '124ms' },
  { service: 'Auth Service', status: 'operational', uptime: '100%', latency: '18ms' },
  { service: 'Eval Engine', status: 'degraded', uptime: '98.4%', latency: '380ms' },
  { service: 'Notification Bus', status: 'operational', uptime: '99.92%', latency: '56ms' },
];

const platformActivity = [
  { time: '00:00', logins: 320, exams: 18, papers: 4 },
  { time: '04:00', logins: 80, exams: 2, papers: 1 },
  { time: '08:00', logins: 2400, exams: 140, papers: 68 },
  { time: '10:00', logins: 5800, exams: 284, papers: 142 },
  { time: '12:00', logins: 4200, exams: 196, papers: 88 },
  { time: '14:00', logins: 6100, exams: 310, papers: 165 },
  { time: '16:00', logins: 5400, exams: 268, papers: 130 },
  { time: '18:00', logins: 3200, exams: 148, papers: 74 },
  { time: '20:00', logins: 1800, exams: 82, papers: 42 },
  { time: '22:00', logins: 640, exams: 28, papers: 12 },
];

const regionData = [
  { state: 'Maharashtra', institutes: 342, students: 184200 },
  { state: 'Karnataka', institutes: 280, students: 142400 },
  { state: 'UP', institutes: 410, students: 218000 },
  { state: 'Tamil Nadu', institutes: 265, students: 136800 },
  { state: 'Gujarat', institutes: 198, students: 98400 },
  { state: 'Rajasthan', institutes: 187, students: 94200 },
  { state: 'West Bengal', institutes: 215, students: 108600 },
];

const pendingApprovals = [
  { id: 'INST-2024-0892', name: 'Govt. Polytechnic Nagpur', type: 'New Institute', submitted: '2 hrs ago', tier: 'Government' },
  { id: 'INST-2024-0891', name: 'Delhi Public School Chain', type: 'License Upgrade', submitted: '4 hrs ago', tier: 'Private' },
  { id: 'INST-2024-0890', name: 'IIT Entrance Academy', type: 'Feature Request', submitted: '6 hrs ago', tier: 'Coaching' },
  { id: 'INST-2024-0889', name: 'Maharashtra Board Cells', type: 'Data Migration', submitted: '1 day ago', tier: 'Government' },
];

const recentAlerts = [
  { level: 'critical', msg: 'Eval Engine latency spike detected — avg 380ms (threshold: 200ms)', time: '14 min ago' },
  { level: 'warning', msg: 'Institute INST-0881 has exceeded paper generation quota by 15%', time: '42 min ago' },
  { level: 'info', msg: 'Scheduled maintenance window tonight 02:00–04:00 IST', time: '2 hrs ago' },
  { level: 'warning', msg: '3 failed login attempts detected for admin@dlps.edu.in', time: '3 hrs ago' },
];

const licenseData = [
  { name: 'Government', value: 1180, color: '#1d4ed8' },
  { name: 'Private', value: 840, color: '#7c3aed' },
  { name: 'Coaching', value: 398, color: '#0369a1' },
];

const statusDot = (s: string) => s === 'operational' ? '🟢' : s === 'degraded' ? '🟡' : '🔴';

const alertIcon = (l: string) => l === 'critical' ? <XCircle className="w-4 h-4 text-red-500 flex-shrink-0" /> : l === 'warning' ? <AlertTriangle className="w-4 h-4 text-amber-500 flex-shrink-0" /> : <CheckCircle className="w-4 h-4 text-blue-500 flex-shrink-0" />;
const alertBg = (l: string) => l === 'critical' ? 'bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-800' : l === 'warning' ? 'bg-amber-50 dark:bg-amber-950/20 border-amber-200 dark:border-amber-800' : 'bg-blue-50 dark:bg-blue-950/20 border-blue-200 dark:border-blue-800';
const alertText = (l: string) => l === 'critical' ? 'text-red-700 dark:text-red-300' : l === 'warning' ? 'text-amber-700 dark:text-amber-300' : 'text-blue-700 dark:text-blue-300';

const CustomTooltip = ({ active, payload, label }: any) => {
  if (!active || !payload?.length) return null;
  return (
    <div className="bg-slate-900 border border-slate-700 rounded p-3 text-sm shadow-xl">
      <div className="text-slate-400 mb-2 font-medium">{label}</div>
      {payload.map((e: any) => (
        <div key={e.name} className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full" style={{ background: e.color || e.fill }} />
          <span className="text-slate-300">{e.name}:</span>
          <span className="text-white font-bold">{e.value.toLocaleString()}</span>
        </div>
      ))}
    </div>
  );
};

export function SuperAdminDashboard({ setView }: AimbysState) {
  return (
    <div className="space-y-5 pb-8">
      {/* Breadcrumb + Header */}
      <div>
        <div className="text-slate-400 dark:text-slate-500 text-xs mb-1">AIMBYS Platform / Super Admin</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">Platform Overview</h1>
          <div className="flex items-center gap-2">
            <span className="text-slate-400 text-sm">Today: 21 May 2026</span>
            <div className="h-4 border-l border-slate-300 dark:border-slate-700" />
            <div className="flex items-center gap-1.5 text-green-600 dark:text-green-400 text-sm font-medium">
              <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
              System Operational
            </div>
          </div>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        {kpis.map(({ label, value, sub, trend, icon: Icon, accent, light }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
            <div className="flex items-center justify-between mb-4">
              <div className="w-9 h-9 rounded flex items-center justify-center" style={{ background: light }}>
                <Icon className="w-4.5 h-4.5" style={{ color: accent }} />
              </div>
              <span className={`flex items-center gap-1 text-xs font-semibold ${trend === 'up' ? 'text-green-600 dark:text-green-400' : 'text-red-500'}`}>
                {trend === 'up' ? <TrendingUp className="w-3.5 h-3.5" /> : <TrendingDown className="w-3.5 h-3.5" />}
              </span>
            </div>
            <div className="text-2xl font-black text-slate-900 dark:text-white mb-0.5">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm font-medium">{label}</div>
            <div className="text-slate-400 dark:text-slate-500 text-xs mt-1">{sub}</div>
          </div>
        ))}
      </div>

      {/* Platform Activity + Alerts */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        {/* Activity Chart */}
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-slate-900 dark:text-white font-semibold">Platform Activity — Today</h2>
              <p className="text-slate-400 text-xs">Real-time logins, exams, and paper events</p>
            </div>
          </div>
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={platformActivity}>
              <defs>
                <linearGradient id="lgLogins" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#1d4ed8" stopOpacity={0.25} />
                  <stop offset="100%" stopColor="#1d4ed8" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="lgExams" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#7c3aed" stopOpacity={0.2} />
                  <stop offset="100%" stopColor="#7c3aed" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="time" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip content={<CustomTooltip />} />
              <Area type="monotone" dataKey="logins" stroke="#1d4ed8" strokeWidth={2} fill="url(#lgLogins)" name="Logins" />
              <Area type="monotone" dataKey="exams" stroke="#7c3aed" strokeWidth={2} fill="url(#lgExams)" name="Active Exams" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Alerts */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-slate-900 dark:text-white font-semibold">System Alerts</h2>
            <span className="px-2 py-0.5 bg-red-100 dark:bg-red-950/40 text-red-600 dark:text-red-400 rounded text-xs font-semibold">4 Active</span>
          </div>
          <div className="space-y-2.5">
            {recentAlerts.map(({ level, msg, time }, i) => (
              <div key={i} className={`flex gap-2.5 p-3 border rounded ${alertBg(level)}`}>
                {alertIcon(level)}
                <div>
                  <p className={`text-xs font-medium ${alertText(level)}`}>{msg}</p>
                  <span className="text-slate-400 text-xs">{time}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Regional + License + System Health */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        {/* Regional Breakdown */}
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
            <h2 className="text-slate-900 dark:text-white font-semibold">Regional Breakdown</h2>
            <button onClick={() => setView('sa-analytics')} className="text-blue-600 dark:text-blue-400 text-xs hover:underline">Full Report →</button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50 dark:bg-slate-800/50">
                <tr>
                  {['State / Region', 'Institutes', 'Students', 'Usage'].map(h => (
                    <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {regionData.map(r => (
                  <tr key={r.state} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-2">
                        <Globe className="w-3.5 h-3.5 text-slate-400" />
                        <span className="text-slate-900 dark:text-slate-100 font-medium text-sm">{r.state}</span>
                      </div>
                    </td>
                    <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{r.institutes.toLocaleString()}</td>
                    <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{r.students.toLocaleString()}</td>
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-2">
                        <div className="flex-1 h-1.5 bg-slate-200 dark:bg-slate-700 rounded-full overflow-hidden">
                          <div className="h-full bg-blue-500 rounded-full" style={{ width: `${Math.round((r.institutes / 410) * 100)}%` }} />
                        </div>
                        <span className="text-slate-500 dark:text-slate-400 text-xs w-8">{Math.round((r.institutes / 410) * 100)}%</span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* License Distribution + System Health */}
        <div className="space-y-4">
          <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
            <h2 className="text-slate-900 dark:text-white font-semibold mb-3">License Distribution</h2>
            <ResponsiveContainer width="100%" height={140}>
              <PieChart>
                <Pie data={licenseData} cx="50%" cy="50%" outerRadius={58} innerRadius={32} dataKey="value" paddingAngle={2}>
                  {licenseData.map((e, i) => <Cell key={i} fill={e.color} />)}
                </Pie>
                <Tooltip formatter={(v) => [v.toLocaleString(), 'Institutes']} />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-1.5 mt-2">
              {licenseData.map(({ name, value, color }) => (
                <div key={name} className="flex items-center justify-between text-sm">
                  <div className="flex items-center gap-2"><div className="w-2.5 h-2.5 rounded-full" style={{ background: color }} /><span className="text-slate-600 dark:text-slate-400">{name}</span></div>
                  <span className="text-slate-900 dark:text-white font-semibold">{value.toLocaleString()}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
            <h2 className="text-slate-900 dark:text-white font-semibold mb-3">Service Status</h2>
            <div className="space-y-2">
              {systemHealth.map(({ service, status, uptime, latency }) => (
                <div key={service} className="flex items-center justify-between py-1.5 border-b border-slate-100 dark:border-slate-800 last:border-0">
                  <div className="flex items-center gap-2">
                    <span>{statusDot(status)}</span>
                    <span className="text-slate-700 dark:text-slate-300 text-sm">{service}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-slate-400 text-xs">{latency}</span>
                    <span className="text-green-600 dark:text-green-400 text-xs font-medium">{uptime}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Pending Approvals */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <h2 className="text-slate-900 dark:text-white font-semibold">Pending Approvals</h2>
            <span className="px-2 py-0.5 bg-amber-100 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400 rounded text-xs font-semibold">4 Pending</span>
          </div>
          <button onClick={() => setView('sa-institutes')} className="text-blue-600 dark:text-blue-400 text-xs hover:underline">View All →</button>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50 dark:bg-slate-800/50">
              <tr>
                {['Request ID', 'Institution Name', 'Request Type', 'Tier', 'Submitted', 'Action'].map(h => (
                  <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {pendingApprovals.map(row => (
                <tr key={row.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                  <td className="px-5 py-3.5 text-blue-600 dark:text-blue-400 font-mono text-sm">{row.id}</td>
                  <td className="px-5 py-3.5 text-slate-900 dark:text-slate-100 font-medium text-sm">{row.name}</td>
                  <td className="px-5 py-3.5"><span className="px-2.5 py-1 bg-indigo-50 dark:bg-indigo-950/30 text-indigo-700 dark:text-indigo-400 rounded text-xs font-medium">{row.type}</span></td>
                  <td className="px-5 py-3.5">
                    <span className={`px-2.5 py-1 rounded text-xs font-medium ${row.tier === 'Government' ? 'bg-blue-50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-400' : row.tier === 'Private' ? 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400' : 'bg-violet-50 dark:bg-violet-950/30 text-violet-700 dark:text-violet-400'}`}>{row.tier}</span>
                  </td>
                  <td className="px-5 py-3.5 text-slate-500 dark:text-slate-400 text-sm">{row.submitted}</td>
                  <td className="px-5 py-3.5">
                    <div className="flex items-center gap-2">
                      <button className="px-3 py-1.5 bg-green-600 text-white rounded text-xs font-medium hover:bg-green-700 transition-colors">Approve</button>
                      <button className="px-3 py-1.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 rounded text-xs font-medium hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">Review</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
