import { useState } from 'react';
import { Search, Filter, Download, Shield, AlertTriangle, CheckCircle, Info, User, FileText, Settings, Lock } from 'lucide-react';
import { AimbysState } from '../../../App';

const logs = [
  { id: 'EVT-001842', timestamp: '2026-05-21 14:42:18', user: 'admin@dlps.edu.in', institute: 'Delhi Public School', action: 'PAPER_PUBLISHED', resource: 'Paper #P-2024-0841', ip: '103.24.81.42', severity: 'info', module: 'Paper' },
  { id: 'EVT-001841', timestamp: '2026-05-21 14:38:02', user: 'admin@aimbys.in', institute: 'AIMBYS Platform', action: 'INSTITUTE_SUSPENDED', resource: 'INST-2024-0005', ip: '10.0.0.1', severity: 'critical', module: 'Admin' },
  { id: 'EVT-001840', timestamp: '2026-05-21 14:20:44', user: 'teacher.sharma@fiitjee.in', institute: 'FIITJEE National', action: 'EXAM_STARTED', resource: 'Exam #E-2024-1284', ip: '122.168.44.81', severity: 'info', module: 'Exam' },
  { id: 'EVT-001839', timestamp: '2026-05-21 13:58:11', user: 'sys@aimbys.in', institute: 'System', action: 'BACKUP_COMPLETED', resource: 'DB Cluster Node-1', ip: '10.0.0.8', severity: 'info', module: 'System' },
  { id: 'EVT-001838', timestamp: '2026-05-21 13:41:30', user: 'unknown', institute: 'Unknown', action: 'LOGIN_FAILED', resource: 'admin@dlps.edu.in', ip: '188.241.54.10', severity: 'warning', module: 'Auth' },
  { id: 'EVT-001837', timestamp: '2026-05-21 13:12:08', user: 'proctor@amity.edu', institute: 'Amity University', action: 'EXAM_PROCTORING_FLAG', resource: 'Candidate RN-28420', ip: '49.36.18.120', severity: 'warning', module: 'Proctoring' },
  { id: 'EVT-001836', timestamp: '2026-05-21 12:44:52', user: 'admin@karnataka.gov.in', institute: 'Karnataka Board', action: 'RESULTS_PUBLISHED', resource: 'Exam Batch #2026-B2', ip: '103.24.82.18', severity: 'info', module: 'Results' },
  { id: 'EVT-001835', timestamp: '2026-05-21 11:30:19', user: 'admin@aimbys.in', institute: 'AIMBYS Platform', action: 'PERMISSION_MODIFIED', resource: 'Role: Institute_Admin', ip: '10.0.0.1', severity: 'warning', module: 'RBAC' },
  { id: 'EVT-001834', timestamp: '2026-05-21 10:18:42', user: 'teacher.patel@amity.edu', institute: 'Amity University', action: 'QUESTION_DELETED', resource: 'QB Bank #QB-0284', ip: '49.36.18.55', severity: 'warning', module: 'Question Bank' },
  { id: 'EVT-001833', timestamp: '2026-05-21 09:04:17', user: 'sys@aimbys.in', institute: 'System', action: 'HEALTH_CHECK', resource: 'All Services', ip: '10.0.0.8', severity: 'info', module: 'System' },
];

const sevConfig: Record<string, { label: string; icon: any; textClass: string; bgClass: string }> = {
  critical: { label: 'CRITICAL', icon: AlertTriangle, textClass: 'text-red-700 dark:text-red-400', bgClass: 'bg-red-50 dark:bg-red-950/30' },
  warning: { label: 'WARNING', icon: Shield, textClass: 'text-amber-700 dark:text-amber-400', bgClass: 'bg-amber-50 dark:bg-amber-950/30' },
  info: { label: 'INFO', icon: Info, textClass: 'text-blue-700 dark:text-blue-400', bgClass: 'bg-blue-50 dark:bg-blue-950/30' },
};

const moduleIcons: Record<string, any> = {
  Paper: FileText, Admin: Shield, Exam: CheckCircle, System: Settings, Auth: Lock,
  Proctoring: User, Results: CheckCircle, RBAC: Lock, 'Question Bank': FileText,
};

export function AuditLogs(_props: AimbysState) {
  const [search, setSearch] = useState('');
  const [severity, setSeverity] = useState('all');
  const [module, setModule] = useState('all');

  const filtered = logs.filter(l => {
    const ms = l.action.includes(search.toUpperCase()) || l.user.toLowerCase().includes(search.toLowerCase()) || l.institute.toLowerCase().includes(search.toLowerCase());
    const msev = severity === 'all' || l.severity === severity;
    const mmod = module === 'all' || l.module === module;
    return ms && msev && mmod;
  });

  const modules = ['all', ...Array.from(new Set(logs.map(l => l.module)))];

  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Super Admin / Audit</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">Audit Logs</h1>
          <button className="flex items-center gap-1.5 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 rounded text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
            <Download className="w-3.5 h-3.5" />Export Logs
          </button>
        </div>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Critical Events (24h)', value: '2', class: 'text-red-600 dark:text-red-400', bg: 'bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-800' },
          { label: 'Warnings (24h)', value: '14', class: 'text-amber-600 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/20 border-amber-200 dark:border-amber-800' },
          { label: 'Total Events (24h)', value: '1,842', class: 'text-blue-600 dark:text-blue-400', bg: 'bg-blue-50 dark:bg-blue-950/20 border-blue-200 dark:border-blue-800' },
        ].map(item => (
          <div key={item.label} className={`border rounded-lg p-4 ${item.bg}`}>
            <div className={`text-2xl font-black ${item.class}`}>{item.value}</div>
            <div className="text-slate-600 dark:text-slate-400 text-sm mt-0.5">{item.label}</div>
          </div>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex items-center gap-2 flex-1 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded">
          <Search className="w-4 h-4 text-slate-400" />
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by user, action, institute..." className="bg-transparent text-slate-900 dark:text-white placeholder-slate-400 outline-none flex-1 text-sm" />
        </div>
        <select value={severity} onChange={e => setSeverity(e.target.value)} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-900 dark:text-white outline-none text-sm cursor-pointer">
          {['all','critical','warning','info'].map(s => <option key={s} value={s}>{s==='all'?'All Severity':s.charAt(0).toUpperCase()+s.slice(1)}</option>)}
        </select>
        <select value={module} onChange={e => setModule(e.target.value)} className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded text-slate-900 dark:text-white outline-none text-sm cursor-pointer">
          {modules.map(m => <option key={m} value={m}>{m==='all'?'All Modules':m}</option>)}
        </select>
      </div>

      {/* Logs Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-800">
              <tr>
                {['Timestamp', 'Event ID', 'Severity', 'Module', 'Action', 'User', 'Institute', 'Resource', 'IP Address'].map(h => (
                  <th key={h} className="text-left px-4 py-3 text-slate-500 dark:text-slate-400 text-xs font-semibold uppercase tracking-wider whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800 font-mono">
              {filtered.map(log => {
                const sc = sevConfig[log.severity];
                const ModIcon = moduleIcons[log.module] || Info;
                return (
                  <tr key={log.id} className={`hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors ${log.severity==='critical'?'bg-red-50/30 dark:bg-red-950/10':''}`}>
                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400 text-xs whitespace-nowrap">{log.timestamp}</td>
                    <td className="px-4 py-3 text-blue-600 dark:text-blue-400 text-xs whitespace-nowrap">{log.id}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-bold ${sc.bgClass} ${sc.textClass}`}>
                        <sc.icon className="w-3 h-3" />{sc.label}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1.5">
                        <ModIcon className="w-3.5 h-3.5 text-slate-400" />
                        <span className="text-slate-600 dark:text-slate-400 text-xs">{log.module}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-slate-900 dark:text-slate-100 text-xs font-semibold whitespace-nowrap">{log.action}</td>
                    <td className="px-4 py-3 text-slate-600 dark:text-slate-400 text-xs whitespace-nowrap">{log.user}</td>
                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400 text-xs">{log.institute}</td>
                    <td className="px-4 py-3 text-slate-500 dark:text-slate-400 text-xs">{log.resource}</td>
                    <td className="px-4 py-3 text-slate-400 dark:text-slate-500 text-xs whitespace-nowrap">{log.ip}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <div className="px-4 py-3 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/30 text-slate-500 dark:text-slate-400 text-sm">
          Showing {filtered.length} of {logs.length} events — Real-time log stream active
        </div>
      </div>
    </div>
  );
}
