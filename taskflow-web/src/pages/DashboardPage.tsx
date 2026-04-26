import { format } from 'date-fns';
import {
  Activity,
  BarChart3,
  Bell,
  CheckCircle2,
  ClipboardList,
  FolderOpen,
  Plus,
  Users,
} from 'lucide-react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Avatar } from '../components/ui/Avatar';
import { Button } from '../components/ui/Button';
import { EmptyState } from '../components/ui/EmptyState';
import { useDashboardStats } from '../hooks/useDashboardStats';

function getTimeOfDay() {
  const hour = new Date().getHours();
  if (hour < 12) return 'morning';
  if (hour < 17) return 'afternoon';
  return 'evening';
}

function TrendText({ value }: { value: number }) {
  if (value === 0) return <p className="mt-1 text-11 text-neutral-500">No change from last week</p>;
  return (
    <p className={`mt-1 text-11 ${value > 0 ? 'text-emerald-600' : 'text-red-600'}`}>
      {value > 0 ? '+' : ''}
      {value}% vs last week
    </p>
  );
}

function StatCard({
  icon: Icon,
  label,
  value,
  trend,
}: {
  icon: typeof ClipboardList;
  label: string;
  value: number;
  trend: number;
}) {
  return (
    <div className="rounded-md border border-neutral-200 bg-white p-4">
      <div className="flex h-8 w-8 items-center justify-center rounded bg-primary-50 text-primary-600">
        <Icon className="h-4 w-4" />
      </div>
      <p className="mt-2 text-24 font-semibold text-neutral-800">{value}</p>
      <p className="mt-0.5 text-12 text-neutral-500">{label}</p>
      <TrendText value={trend} />
    </div>
  );
}

function VelocityCard({ data }: { data: Array<{ name: string; completed: number }> }) {
  return (
    <div className="rounded-md border border-neutral-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-13 font-semibold text-neutral-800">Velocity</p>
        <p className="text-12 text-neutral-500">Tasks completed</p>
      </div>
      <div className="h-[200px]">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: -20 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#f1f3f5" vertical={false} />
            <XAxis dataKey="name" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: '#868e96' }} />
            <YAxis axisLine={false} tickLine={false} width={24} tick={{ fontSize: 11, fill: '#868e96' }} />
            <Tooltip
              cursor={{ fill: '#f8f9fa' }}
              contentStyle={{
                border: '1px solid #dee2e6',
                boxShadow: '0 3px 5px rgba(9,30,66,0.20), 0 0 1px rgba(9,30,66,0.31)',
                borderRadius: '6px',
                fontSize: '12px',
                padding: '4px 8px',
              }}
            />
            <Bar dataKey="completed" fill="#6366f1" radius={[2, 2, 0, 0]} maxBarSize={24} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

function StatusDonutCard({
  data,
  completionRate,
}: {
  data: Array<{ name: string; value: number; color: string }>;
  completionRate: number;
}) {
  return (
    <div className="rounded-md border border-neutral-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-13 font-semibold text-neutral-800">Task status</p>
        <p className="text-12 text-neutral-500">Completion rate</p>
      </div>
      <div className="flex h-[160px] items-center gap-3">
        <div className="relative h-[160px] w-[160px] shrink-0">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie data={data} dataKey="value" innerRadius={42} outerRadius={64} paddingAngle={2}>
                {data.map((entry) => (
                  <Cell key={entry.name} fill={entry.color} />
                ))}
              </Pie>
            </PieChart>
          </ResponsiveContainer>
          <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
            <span className="text-16 font-bold text-neutral-800">{completionRate}%</span>
          </div>
        </div>
        <div className="grid min-w-0 flex-1 grid-cols-2 gap-x-3 gap-y-1">
          {data.map((item) => (
            <div key={item.name} className="flex min-w-0 items-center gap-1.5">
              <span className="h-2 w-2 shrink-0 rounded-full" style={{ backgroundColor: item.color }} />
              <span className="truncate text-12 text-neutral-600">{item.name}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function DueTag({ tone, label }: { tone: 'neutral' | 'warning' | 'danger'; label: string }) {
  const cls =
    tone === 'danger'
      ? 'bg-red-50 text-red-700 border-red-200'
      : tone === 'warning'
        ? 'bg-amber-50 text-amber-700 border-amber-200'
        : 'bg-neutral-50 text-neutral-600 border-neutral-200';
  return <span className={`inline-flex h-[18px] items-center rounded-sm border px-2 text-11 ${cls}`}>{label}</span>;
}

function PriorityDot({ priority }: { priority: 'high' | 'medium' | 'low' }) {
  const color = priority === 'high' ? 'bg-red-500' : priority === 'medium' ? 'bg-amber-500' : 'bg-emerald-500';
  return <span className={`h-2 w-2 shrink-0 rounded-full ${color}`} aria-hidden />;
}

function UpcomingTasksCard({
  tasks,
}: {
  tasks: Array<{
    id: string;
    title: string;
    project: string;
    dueLabel: string;
    dueTone: 'neutral' | 'warning' | 'danger';
    priority: 'high' | 'medium' | 'low';
    assigneeName: string;
  }>;
}) {
  return (
    <div className="rounded-md border border-neutral-200 bg-white">
      <div className="border-b border-neutral-100 px-4 py-3">
        <p className="text-13 font-semibold text-neutral-800">Upcoming tasks</p>
      </div>
      {tasks.length === 0 ? (
        <EmptyState.NoTasks size="sm" />
      ) : (
        <>
          {tasks.map((task) => (
            <div
              key={task.id}
              className="flex h-9 items-center gap-3 border-b border-neutral-100 px-4 text-13 last:border-b-0 hover:bg-neutral-50"
            >
              <PriorityDot priority={task.priority} />
              <span className="min-w-0 flex-1 truncate text-neutral-800">{task.title}</span>
              <span className="w-28 shrink-0 truncate text-12 text-neutral-400">{task.project}</span>
              <DueTag tone={task.dueTone} label={task.dueLabel} />
              <Avatar name={task.assigneeName} size="xs" />
            </div>
          ))}
          <a className="block border-t border-neutral-100 px-4 py-2 text-center text-12 text-primary-600 hover:text-primary-700" href="#">
            View all
          </a>
        </>
      )}
    </div>
  );
}

function TopContributorsCard({ contributors }: { contributors: Array<{ id: string; name: string; completed: number }> }) {
  const max = Math.max(...contributors.map((c) => c.completed), 1);
  return (
    <div className="rounded-md border border-neutral-200 bg-white">
      <div className="border-b border-neutral-100 px-4 py-3">
        <p className="text-13 font-semibold text-neutral-800">Top contributors</p>
      </div>
      {contributors.length === 0 ? (
        <EmptyState.NoMembers size="sm" />
      ) : (
        contributors.map((person, i) => (
          <div key={person.id} className="flex h-9 items-center gap-2.5 px-4">
            <span className="w-5 shrink-0 text-12 text-neutral-400">#{i + 1}</span>
            <Avatar name={person.name} size="sm" />
            <span className="min-w-0 flex-1 truncate text-13 text-neutral-800">{person.name}</span>
            <span className="shrink-0 text-12 font-medium text-neutral-700">{person.completed}</span>
            <div className="w-14 shrink-0 rounded-full bg-neutral-100">
              <div
                className="h-1 rounded-full bg-primary-500"
                style={{ width: `${Math.max(8, Math.round((person.completed / max) * 100))}%` }}
              />
            </div>
          </div>
        ))
      )}
    </div>
  );
}

function ActivityFeed({
  activity,
}: {
  activity: Array<{ id: string; type: 'task' | 'project' | 'comment' | 'member'; text: string; time: string }>;
}) {
  const iconMap = {
    task: { icon: CheckCircle2, color: 'text-emerald-600' },
    project: { icon: FolderOpen, color: 'text-primary-600' },
    comment: { icon: Bell, color: 'text-amber-600' },
    member: { icon: Users, color: 'text-indigo-600' },
  };

  return (
    <div className="rounded-md border border-neutral-200 bg-white">
      <div className="border-b border-neutral-100 px-4 py-3">
        <p className="text-13 font-semibold text-neutral-800">Recent activity</p>
      </div>
      {activity.slice(0, 8).map((item) => {
        const Icon = iconMap[item.type].icon;
        return (
          <div key={item.id} className="flex h-9 items-center gap-3 border-b border-neutral-100 px-4 last:border-b-0 hover:bg-neutral-50">
            <Icon className={`h-4 w-4 shrink-0 ${iconMap[item.type].color}`} />
            <span className="min-w-0 flex-1 truncate text-13 text-neutral-700">{item.text}</span>
            <span className="shrink-0 text-12 text-neutral-400">{item.time}</span>
          </div>
        );
      })}
      <a className="block border-t border-neutral-100 px-4 py-2 text-center text-12 text-primary-600 hover:text-primary-700" href="#">
        View all →
      </a>
    </div>
  );
}

export default function DashboardPage() {
  const { data } = useDashboardStats();
  const userFirstName = data.user.displayName?.split(' ')[0] ?? 'there';
  const done = data.statusBreakdown.find((s) => s.name === 'Done')?.value ?? 0;
  const total = data.statusBreakdown.reduce((acc, s) => acc + s.value, 0);
  const completionRate = total ? Math.round((done / total) * 100) : 0;

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <div>
          <h1 className="page-title">
            Good {getTimeOfDay()}, {userFirstName}
          </h1>
          <p className="page-subtitle">
            {format(new Date(), 'EEEE, MMMM d')} · {data.workspaceName}
          </p>
        </div>
        <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />}>
          New task
        </Button>
      </div>

      <div className="mb-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatCard icon={ClipboardList} label="Total tasks" value={data.kpis.totalTasks} trend={data.kpis.trends.totalTasks} />
        <StatCard icon={CheckCircle2} label="Completed" value={data.kpis.completedTasks} trend={data.kpis.trends.completedTasks} />
        <StatCard icon={Activity} label="Overdue" value={data.kpis.overdueTasks} trend={data.kpis.trends.overdueTasks} />
        <StatCard icon={BarChart3} label="Active projects" value={data.kpis.activeProjects} trend={data.kpis.trends.activeProjects} />
      </div>

      <div className="mb-4 grid grid-cols-12 gap-3">
        <div className="col-span-12 lg:col-span-7">
          <VelocityCard data={data.velocity} />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <StatusDonutCard data={data.statusBreakdown} completionRate={completionRate} />
        </div>
      </div>

      <div className="mb-4 grid grid-cols-12 gap-3">
        <div className="col-span-12 lg:col-span-7">
          <UpcomingTasksCard tasks={data.upcomingTasks} />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <TopContributorsCard contributors={data.contributors} />
        </div>
      </div>

      <ActivityFeed activity={data.activity} />
    </div>
  );
}
