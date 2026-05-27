import { Cpu, HardDrive, Database, Activity, Server, Wifi } from 'lucide-react';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { AimbysState } from '../../../App';

const cpuData = Array.from({length:12},(_,i)=>({ time:`${i*2}:00`, cpu: 30+Math.random()*40, mem: 55+Math.random()*20 }));
const reqData = Array.from({length:12},(_,i)=>({ time:`${i*2}:00`, rps: 800+Math.random()*1200, errors: Math.random()*15 }));

const services = [
  { name: 'API Gateway', status: 'healthy', cpu: '24%', mem: '1.2 GB', rps: '4,820/s', uptime: '99.98%', nodes: 4 },
  { name: 'Exam Engine', status: 'healthy', cpu: '61%', mem: '8.4 GB', rps: '2,140/s', uptime: '99.94%', nodes: 6 },
  { name: 'Eval Engine', status: 'degraded', cpu: '88%', mem: '14.2 GB', rps: '840/s', uptime: '98.40%', nodes: 3 },
  { name: 'Auth Service', status: 'healthy', cpu: '18%', mem: '0.8 GB', rps: '1,280/s', uptime: '100%', nodes: 2 },
  { name: 'Database Primary', status: 'healthy', cpu: '42%', mem: '24.0 GB', rps: '8,200/s', uptime: '99.99%', nodes: 3 },
  { name: 'File Storage', status: 'healthy', cpu: '15%', mem: '2.4 GB', rps: '380/s', uptime: '99.97%', nodes: 5 },
  { name: 'Notification Bus', status: 'healthy', cpu: '12%', mem: '0.6 GB', rps: '220/s', uptime: '99.92%', nodes: 2 },
  { name: 'Analytics Engine', status: 'healthy', cpu: '35%', mem: '6.8 GB', rps: '140/s', uptime: '99.88%', nodes: 2 },
];

const statusBadge = (s: string) => s === 'healthy'
  ? 'bg-green-50 dark:bg-green-950/30 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800'
  : s === 'degraded'
  ? 'bg-amber-50 dark:bg-amber-950/30 text-amber-700 dark:text-amber-400 border border-amber-200 dark:border-amber-800'
  : 'bg-red-50 dark:bg-red-950/30 text-red-700 dark:text-red-400 border border-red-200 dark:border-red-800';

export function SystemHealth(_props: AimbysState) {
  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Super Admin / System</div>
        <h1 className="text-slate-900 dark:text-white font-bold text-xl">System Health Dashboard</h1>
      </div>

      {/* Infra Metrics */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'CPU (Avg)', value: '44%', sub: '12 nodes active', Icon: Cpu, color: '#1d4ed8' },
          { label: 'Memory (Avg)', value: '71%', sub: '58.4 GB / 82 GB used', Icon: Activity, color: '#0369a1' },
          { label: 'DB Connections', value: '1,824', sub: 'Max: 4,000', Icon: Database, color: '#7c3aed' },
          { label: 'Total Requests/s', value: '4,820', sub: 'p95 latency: 142ms', Icon: Wifi, color: '#15803d' },
        ].map(({ label, value, sub, Icon, color }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <Icon className="w-5 h-5" style={{ color }} />
              <span className="text-slate-400 text-xs">Live</span>
            </div>
            <div className="text-2xl font-black text-slate-900 dark:text-white">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm font-medium mt-0.5">{label}</div>
            <div className="text-slate-400 text-xs mt-1">{sub}</div>
          </div>
        ))}
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">CPU & Memory (24h)</h2>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={cpuData}>
              <defs>
                <linearGradient id="gcpu" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#1d4ed8" stopOpacity={0.3} />
                  <stop offset="100%" stopColor="#1d4ed8" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="gmem" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#7c3aed" stopOpacity={0.2} />
                  <stop offset="100%" stopColor="#7c3aed" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="time" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} domain={[0, 100]} tickFormatter={v=>`${v}%`} />
              <Tooltip formatter={(v:any) => [`${v.toFixed(1)}%`]} />
              <Area type="monotone" dataKey="cpu" stroke="#1d4ed8" strokeWidth={2} fill="url(#gcpu)" name="CPU" />
              <Area type="monotone" dataKey="mem" stroke="#7c3aed" strokeWidth={2} fill="url(#gmem)" name="Memory" />
            </AreaChart>
          </ResponsiveContainer>
        </div>
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Request Rate & Errors (24h)</h2>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={reqData}>
              <defs>
                <linearGradient id="greq" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#15803d" stopOpacity={0.3} />
                  <stop offset="100%" stopColor="#15803d" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="gerr" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#dc2626" stopOpacity={0.2} />
                  <stop offset="100%" stopColor="#dc2626" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="time" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip formatter={(v:any) => [Math.round(v).toLocaleString()]} />
              <Area type="monotone" dataKey="rps" stroke="#15803d" strokeWidth={2} fill="url(#greq)" name="Req/s" />
              <Area type="monotone" dataKey="errors" stroke="#dc2626" strokeWidth={1.5} fill="url(#gerr)" name="Errors/s" />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Service Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="px-5 py-4 border-b border-slate-200 dark:border-slate-800">
          <h2 className="text-slate-900 dark:text-white font-semibold">Service Registry</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50 dark:bg-slate-800/50">
              <tr>
                {['Service', 'Status', 'CPU', 'Memory', 'RPS', 'Uptime', 'Nodes'].map(h => (
                  <th key={h} className="text-left px-5 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {services.map(svc => (
                <tr key={svc.name} className={`hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors ${svc.status==='degraded'?'bg-amber-50/40 dark:bg-amber-950/10':''}`}>
                  <td className="px-5 py-3.5">
                    <div className="flex items-center gap-2">
                      <Server className="w-3.5 h-3.5 text-slate-400" />
                      <span className="text-slate-900 dark:text-slate-100 font-medium text-sm">{svc.name}</span>
                    </div>
                  </td>
                  <td className="px-5 py-3.5">
                    <span className={`px-2.5 py-1 rounded text-xs font-semibold capitalize ${statusBadge(svc.status)}`}>{svc.status}</span>
                  </td>
                  <td className="px-5 py-3.5 text-slate-700 dark:text-slate-300 text-sm font-mono">{svc.cpu}</td>
                  <td className="px-5 py-3.5 text-slate-700 dark:text-slate-300 text-sm font-mono">{svc.mem}</td>
                  <td className="px-5 py-3.5 text-slate-700 dark:text-slate-300 text-sm font-mono">{svc.rps}</td>
                  <td className="px-5 py-3.5">
                    <span className={`text-sm font-semibold ${svc.uptime==='100%'?'text-green-600 dark:text-green-400':parseFloat(svc.uptime)>=99?'text-green-600 dark:text-green-400':'text-amber-600 dark:text-amber-400'}`}>{svc.uptime}</span>
                  </td>
                  <td className="px-5 py-3.5 text-slate-600 dark:text-slate-400 text-sm">{svc.nodes} active</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
