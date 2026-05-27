import { AreaChart, Area, BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell, RadarChart, Radar, PolarGrid, PolarAngleAxis } from 'recharts';
import { Download, TrendingUp, TrendingDown } from 'lucide-react';
import { AimbysState } from '../../../App';

const monthlyTrend = [
  { month: 'Nov', papers: 62400, exams: 2840, students: 184000 },
  { month: 'Dec', papers: 71200, exams: 3120, students: 198000 },
  { month: 'Jan', papers: 84800, exams: 4200, students: 224000 },
  { month: 'Feb', papers: 78400, exams: 3680, students: 212000 },
  { month: 'Mar', papers: 96200, exams: 4820, students: 248000 },
  { month: 'Apr', papers: 88400, exams: 4280, students: 236000 },
  { month: 'May', papers: 102800, exams: 5140, students: 264000 },
];

const subjectBreakdown = [
  { subject: 'Mathematics', papers: 18400, avgScore: 64 },
  { subject: 'Physics', papers: 14200, avgScore: 68 },
  { subject: 'Chemistry', papers: 12800, avgScore: 71 },
  { subject: 'Biology', papers: 10600, avgScore: 74 },
  { subject: 'Computer Sci.', papers: 9400, avgScore: 78 },
  { subject: 'English', papers: 16200, avgScore: 82 },
  { subject: 'History', papers: 8200, avgScore: 76 },
];

const questionTypes = [
  { subject: 'MCQ', value: 42 }, { subject: 'Descriptive', value: 24 },
  { subject: 'Coding', value: 14 }, { subject: 'Fill Blanks', value: 10 },
  { subject: 'SQL/DB', value: 5 }, { subject: 'File Upload', value: 5 },
];

const passFail = [
  { month: 'Jan', pass: 74, fail: 26 }, { month: 'Feb', pass: 71, fail: 29 },
  { month: 'Mar', pass: 76, fail: 24 }, { month: 'Apr', pass: 73, fail: 27 },
  { month: 'May', pass: 78, fail: 22 },
];

const CustomTooltip = ({ active, payload, label }: any) => {
  if (!active || !payload?.length) return null;
  return (
    <div className="bg-slate-900 border border-slate-700 rounded p-3 text-sm shadow-xl">
      <div className="text-slate-400 mb-2">{label}</div>
      {payload.map((e: any) => (
        <div key={e.name} className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full" style={{ background: e.color || e.fill }} />
          <span className="text-slate-300">{e.name}:</span>
          <span className="text-white font-bold">{typeof e.value === 'number' ? e.value.toLocaleString() : e.value}</span>
        </div>
      ))}
    </div>
  );
};

export function GlobalAnalytics(_props: AimbysState) {
  return (
    <div className="space-y-5 pb-8">
      <div>
        <div className="text-slate-400 text-xs mb-1">Super Admin / Analytics</div>
        <div className="flex items-center justify-between">
          <h1 className="text-slate-900 dark:text-white font-bold text-xl">Global Platform Analytics</h1>
          <button className="flex items-center gap-1.5 px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 rounded text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
            <Download className="w-3.5 h-3.5" />Export Report
          </button>
        </div>
      </div>

      {/* Summary KPIs */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Total Papers (YTD)', value: '8,51,240', change: '+22.4%', up: true },
          { label: 'Exams Conducted', value: '42,180', change: '+18.7%', up: true },
          { label: 'Avg. Pass Rate', value: '74.2%', change: '+3.1%', up: true },
          { label: 'Avg. Score', value: '68.4%', change: '-0.8%', up: false },
        ].map(({ label, value, change, up }) => (
          <div key={label} className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-4">
            <div className="text-2xl font-black text-slate-900 dark:text-white mb-0.5">{value}</div>
            <div className="text-slate-500 dark:text-slate-400 text-sm mb-1">{label}</div>
            <span className={`flex items-center gap-1 text-xs font-semibold ${up ? 'text-green-600 dark:text-green-400' : 'text-red-500'}`}>
              {up ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}{change}
            </span>
          </div>
        ))}
      </div>

      {/* Monthly Trend */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
        <div className="mb-4">
          <h2 className="text-slate-900 dark:text-white font-semibold">Monthly Platform Activity</h2>
          <p className="text-slate-400 text-xs">Papers generated, exams conducted, student participation</p>
        </div>
        <ResponsiveContainer width="100%" height={240}>
          <AreaChart data={monthlyTrend}>
            <defs>
              <linearGradient id="gradPapers" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#1d4ed8" stopOpacity={0.25} />
                <stop offset="100%" stopColor="#1d4ed8" stopOpacity={0} />
              </linearGradient>
              <linearGradient id="gradExams" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#7c3aed" stopOpacity={0.2} />
                <stop offset="100%" stopColor="#7c3aed" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
            <XAxis dataKey="month" tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
            <Tooltip content={<CustomTooltip />} />
            <Area type="monotone" dataKey="papers" stroke="#1d4ed8" strokeWidth={2} fill="url(#gradPapers)" name="Papers" />
            <Area type="monotone" dataKey="exams" stroke="#7c3aed" strokeWidth={2} fill="url(#gradExams)" name="Exams" />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        {/* Subject Performance */}
        <div className="xl:col-span-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Papers by Subject & Avg Score</h2>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={subjectBreakdown} barGap={4}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
              <XAxis dataKey="subject" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis yAxisId="left" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis yAxisId="right" orientation="right" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} domain={[0,100]} tickFormatter={v=>`${v}%`} />
              <Tooltip content={<CustomTooltip />} />
              <Bar yAxisId="left" dataKey="papers" name="Papers" fill="#1d4ed8" radius={[3,3,0,0]} opacity={0.85} />
              <Bar yAxisId="right" dataKey="avgScore" name="Avg Score%" fill="#0369a1" radius={[3,3,0,0]} opacity={0.85} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Question Type Radar */}
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
          <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Question Type Mix</h2>
          <ResponsiveContainer width="100%" height={200}>
            <RadarChart data={questionTypes}>
              <PolarGrid stroke="#e2e8f0" />
              <PolarAngleAxis dataKey="subject" tick={{ fill: '#94a3b8', fontSize: 11 }} />
              <Radar name="Share %" dataKey="value" stroke="#1d4ed8" fill="#1d4ed8" fillOpacity={0.25} strokeWidth={2} />
              <Tooltip formatter={v=>[`${v}%`,'Share']} />
            </RadarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Pass/Fail trend */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg p-5">
        <h2 className="text-slate-900 dark:text-white font-semibold mb-4">Monthly Pass / Fail Rates (%)</h2>
        <ResponsiveContainer width="100%" height={200}>
          <BarChart data={passFail} barGap={8}>
            <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
            <XAxis dataKey="month" tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fill: '#94a3b8', fontSize: 12 }} axisLine={false} tickLine={false} domain={[0, 100]} tickFormatter={v=>`${v}%`} />
            <Tooltip content={<CustomTooltip />} />
            <Bar dataKey="pass" name="Pass Rate %" stackId="a" fill="#15803d" radius={[0,0,0,0]} />
            <Bar dataKey="fail" name="Fail Rate %" stackId="a" fill="#dc2626" radius={[3,3,0,0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
