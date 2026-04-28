import { format, formatDistanceToNow, isPast } from 'date-fns';
import { AlertTriangle, CheckCircle2, ClipboardList, Plus } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip as RechartsTooltip, XAxis, YAxis } from 'recharts';
import type { DashboardMyStatsDto, DashboardStatsDto } from '../types/api';
import { Avatar } from '../components/ui/Avatar';
import { Button } from '../components/ui/Button';
import { Skeleton } from '../components/ui/Skeleton';
import { useMe } from '../hooks/api/auth.hooks';
import { useDashboardStats, useMyStats } from '../hooks/api/dashboard.hooks';

const STATUS_COLORS: Record<string, string> = {
  Backlog: '#6B7280',
  Todo: '#6366F1',
  InProgress: '#F59E0B',
  Done: '#22C55E',
  Cancelled: '#9CA3AF',
};

const PRIORITY_COLORS: Record<number, string> = {
  0: '#9CA3AF',
  1: '#3B82F6',
  2: '#F59E0B',
  3: '#EF4444',
};

function timeOfDay() {
  const h = new Date().getHours();
  if (h < 12) return 'Good morning';
  if (h < 17) return 'Good afternoon';
  return 'Good evening';
}

function formatAction(action: string): string {
  const map: Record<string, string> = {
    'task.created': 'created task',
    'task.status_changed': 'updated status of',
    'task.assigned': 'was assigned',
    'task.commented': 'commented on',
    'task.deleted': 'deleted task',
    'project.created': 'created project',
    'project.deleted': 'deleted project',
    'member.invited': 'invited member to',
    'member.joined': 'joined',
  };
  return map[action] ?? action.replaceAll('.', ' ');
}

function trendClass(value: number) {
  if (value > 0) return 'text-green-600';
  if (value < 0) return 'text-red-500';
  return 'text-neutral-400';
}

function TrendText({ value }: { value: number }) {
  return (
    <p className={`mt-1 text-11 font-medium ${trendClass(value)}`}>
      {value > 0 ? `▲ ${Math.abs(value)}%` : value < 0 ? `▼ ${Math.abs(value)}%` : '→ 0%'}
    </p>
  );
}

function StatCard({
  icon: Icon,
  label,
  value,
  iconClassName,
  trendValue,
}: {
  icon: typeof ClipboardList;
  label: string;
  value: number;
  iconClassName?: string;
  trendValue?: number;
}) {
  return (
    <div className="rounded-md border border-neutral-200 bg-white p-4">
      <div className={`flex h-8 w-8 items-center justify-center rounded bg-primary-50 text-primary-600 ${iconClassName ?? ''}`}>
        <Icon className="h-4 w-4" />
      </div>
      <p className="mt-2 text-24 font-semibold text-neutral-800">{value}</p>
      <p className="mt-0.5 text-12 text-neutral-500">{label}</p>
      {typeof trendValue === 'number' ? <TrendText value={trendValue} /> : null}
    </div>
  );
}

function VelocityCard({ velocity }: { velocity: DashboardStatsDto['velocity'] }) {
  const data = [
    { label: 'Prev 7d', count: velocity.completedPrev7Days },
    { label: 'Last 7d', count: velocity.completedLast7Days },
  ];

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
            <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: '#868e96' }} />
            <YAxis axisLine={false} tickLine={false} width={24} tick={{ fontSize: 11, fill: '#868e96' }} />
            <RechartsTooltip
              cursor={{ fill: '#f8f9fa' }}
              formatter={(value) => [`${value ?? 0} tasks`, '']}
              contentStyle={{
                border: '1px solid #dee2e6',
                boxShadow: '0 3px 5px rgba(9,30,66,0.20), 0 0 1px rgba(9,30,66,0.31)',
                borderRadius: '6px',
                fontSize: '12px',
                padding: '4px 8px',
              }}
            />
            <Bar dataKey="count" fill="#6366f1" radius={[2, 2, 0, 0]} maxBarSize={40} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

function StatusDonutCard({ stats }: { stats: DashboardStatsDto }) {
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
              <Pie
                data={stats.tasksByStatus}
                dataKey="count"
                innerRadius={50}
                outerRadius={75}
                paddingAngle={2}
                labelLine={false}
                label={({ cx, cy }) => (
                  <text x={cx} y={cy} textAnchor="middle" dominantBaseline="central" className="fill-neutral-800 text-16 font-semibold">
                    {stats.completionRate.toFixed(1)}%
                  </text>
                )}
              >
                {stats.tasksByStatus.map((entry) => (
                  <Cell key={entry.status} fill={STATUS_COLORS[entry.status] ?? '#9CA3AF'} />
                ))}
              </Pie>
            </PieChart>
          </ResponsiveContainer>
        </div>
        <div className="grid min-w-0 flex-1 grid-cols-1 gap-y-1">
          {stats.tasksByStatus.map((item) => (
            <div key={item.status} className="flex min-w-0 items-center gap-1.5">
              <span className="h-2 w-2 shrink-0 rounded-full" style={{ backgroundColor: STATUS_COLORS[item.status] ?? '#9CA3AF' }} />
              <span className="truncate text-12 text-neutral-600">
                {item.status}: {item.count}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function DueDateBadge({ dueDateUtc }: { dueDateUtc: string | null }) {
  if (!dueDateUtc) return <span className="text-12 text-neutral-400">No due date</span>;

  const dueDate = new Date(dueDateUtc);
  const inMs = dueDate.getTime() - Date.now();
  const isWarning = inMs > 0 && inMs < 1000 * 60 * 60 * 24 * 3;
  const className = isPast(dueDate) ? 'text-red-600' : isWarning ? 'text-amber-600' : 'text-neutral-500';
  return <span className={`text-12 ${className}`}>{format(dueDate, 'MMM d')}</span>;
}

function UpcomingTasksCard({
  tasks,
  onOpenTask,
}: {
  tasks: DashboardStatsDto['upcomingTasks'];
  onOpenTask: (projectId: string, taskId: string) => void;
}) {
  return (
    <div className="rounded-md border border-neutral-200 bg-white">
      <div className="border-b border-neutral-100 px-4 py-3">
        <p className="text-13 font-semibold text-neutral-800">Upcoming Deadlines</p>
      </div>
      {tasks.length === 0 ? (
        <p className="px-4 py-6 text-13 text-neutral-500">No upcoming tasks.</p>
      ) : (
        tasks.map((task) => (
          <div
            key={task.id}
            className="flex h-9 cursor-pointer items-center gap-3 border-b border-neutral-100 px-4 text-13 last:border-b-0 hover:bg-neutral-50"
            onClick={() => onOpenTask(task.projectId, task.id)}
          >
            <span className="h-2 w-2 shrink-0 rounded-full" style={{ backgroundColor: PRIORITY_COLORS[task.priority] ?? '#9CA3AF' }} />
            <span className="min-w-0 flex-1 truncate text-neutral-800">{task.title}</span>
            <span className="w-24 shrink-0 truncate text-12 text-neutral-400">{task.projectName}</span>
            <DueDateBadge dueDateUtc={task.dueDateUtc} />
            {task.assignee ? <Avatar name={task.assignee.displayName ?? task.assignee.userName} size="xs" /> : <span className="w-5" />}
          </div>
        ))
      )}
    </div>
  );
}

function TopContributorsCard({ contributors }: { contributors: DashboardStatsDto['topContributors'] }) {
  const max = Math.max(...contributors.map((c) => c.tasksCompleted), 1);
  return (
    <div className="rounded-md border border-neutral-200 bg-white">
      <div className="border-b border-neutral-100 px-4 py-3">
        <p className="text-13 font-semibold text-neutral-800">Top contributors</p>
      </div>
      {contributors.length === 0 ? (
        <p className="px-4 py-6 text-13 text-neutral-500">No contributors yet.</p>
      ) : (
        contributors.map((person, index) => (
          <div key={person.userId} className="flex h-9 items-center gap-2.5 px-4">
            <span className="w-5 shrink-0 text-12 text-neutral-400">#{index + 1}</span>
            <Avatar name={person.displayName ?? person.userName} size="sm" />
            <span className="min-w-0 flex-1 truncate text-13 text-neutral-800">{person.displayName ?? person.userName}</span>
            <span className="shrink-0 text-12 font-medium text-neutral-700">{person.tasksCompleted} tasks</span>
            <div className="w-14 shrink-0 rounded-full bg-neutral-100">
              <div className="h-1 rounded-full bg-primary-500" style={{ width: `${Math.max(8, Math.round((person.tasksCompleted / max) * 100))}%` }} />
            </div>
          </div>
        ))
      )}
    </div>
  );
}

function actionDotColor(action: string) {
  if (action.startsWith('project.')) return 'bg-green-500';
  if (action.startsWith('member.')) return 'bg-amber-500';
  return 'bg-primary-500';
}

function ActivityFeed({ activity }: { activity: DashboardStatsDto['recentActivity'] }) {
  return (
    <div className="rounded-md border border-neutral-200 bg-white">
      <div className="border-b border-neutral-100 px-4 py-3">
        <p className="text-13 font-semibold text-neutral-800">Recent activity</p>
      </div>
      {activity.map((item, index) => (
        <div key={`${item.occurredAt}-${item.action}-${index}`} className="flex h-9 items-center gap-3 border-b border-neutral-100 px-4 last:border-b-0 hover:bg-neutral-50">
          <span className={`h-2 w-2 shrink-0 rounded-full ${actionDotColor(item.action)}`} />
          <span className="min-w-0 flex-1 truncate text-13 text-neutral-700">
            {item.actorName} {formatAction(item.action)} '{item.entityTitle}'
          </span>
          <span className="shrink-0 text-12 text-neutral-400">{formatDistanceToNow(new Date(item.occurredAt), { addSuffix: true })}</span>
        </div>
      ))}
    </div>
  );
}

function MyStatsSection({ myStats }: { myStats: DashboardMyStatsDto }) {
  const total = Math.max(myStats.myTasks.total, 1);
  return (
    <div className="mt-4 rounded-md border border-primary-100 bg-primary-50 p-4">
      <div className="flex flex-wrap items-center gap-2">
        <span className="rounded bg-white px-2 py-1 text-12 text-neutral-700">My Tasks: {myStats.myTasks.total}</span>
        <span className="rounded bg-white px-2 py-1 text-12 text-neutral-700">Completed: {myStats.myTasks.completed}</span>
        <span className="rounded bg-white px-2 py-1 text-12 text-neutral-700">Overdue: {myStats.myTasks.overdue}</span>
        <span className="rounded bg-white px-2 py-1 text-12 text-neutral-700">Due Soon: {myStats.myTasks.dueSoon}</span>
      </div>
      <div className="mt-3 flex h-2 overflow-hidden rounded-full bg-white">
        {myStats.myTasksByStatus.map((item) => (
          <div
            key={item.status}
            title={`${item.status}: ${item.count}`}
            style={{
              width: `${(item.count / total) * 100}%`,
              backgroundColor: STATUS_COLORS[item.status] ?? '#9CA3AF',
            }}
          />
        ))}
      </div>
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <>
      <div className="page-header">
        <div>
          <Skeleton className="h-7 w-56" />
          <Skeleton className="mt-2 h-4 w-72" />
        </div>
        <Skeleton className="h-7 w-24" />
      </div>

      <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <div key={index} className="rounded-md border border-neutral-200 bg-white p-4">
            <Skeleton className="h-8 w-8" />
            <Skeleton className="mt-3 h-6 w-20" />
            <Skeleton className="mt-2 h-3 w-24" />
            <Skeleton className="mt-2 h-3 w-16" />
          </div>
        ))}
      </div>

      <div className="mb-4 grid grid-cols-12 gap-3">
        <div className="col-span-12 lg:col-span-7">
          <Skeleton className="h-[260px] w-full rounded-md border border-neutral-200 bg-white" />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <Skeleton className="h-[260px] w-full rounded-md border border-neutral-200 bg-white" />
        </div>
      </div>

      <div className="mb-4 grid grid-cols-12 gap-3">
        <div className="col-span-12 lg:col-span-7">
          <Skeleton className="h-[280px] w-full rounded-md border border-neutral-200 bg-white" />
        </div>
        <div className="col-span-12 lg:col-span-5">
          <Skeleton className="h-[280px] w-full rounded-md border border-neutral-200 bg-white" />
        </div>
      </div>

      <Skeleton className="h-[320px] w-full rounded-md border border-neutral-200 bg-white" />
    </>
  );
}

export default function DashboardPage() {
  const navigate = useNavigate();
  const { data: stats, isLoading } = useDashboardStats();
  const { data: myStats } = useMyStats();
  const { data: user } = useMe();

  const openCreateTask = () => navigate('/projects');
  const openTask = (projectId: string, taskId: string) => navigate(`/projects/${projectId}/board?taskId=${taskId}`);

  return (
    <div className="page-wrapper">
      {isLoading || !stats ? (
        <DashboardSkeleton />
      ) : (
        <>
          <div className="page-header">
            <div>
              <h1 className="page-title">{timeOfDay()}, {user?.displayName?.split(' ')[0] ?? user?.userName}</h1>
              <p className="page-subtitle">
                {format(new Date(), 'EEEE, MMMM d')} · {user?.organizationName}
              </p>
            </div>
            <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={openCreateTask}>
              New task
            </Button>
          </div>

          <div className="mb-4 grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
            <StatCard icon={ClipboardList} label="Total Tasks" value={stats.totalTasks} />
            <StatCard icon={CheckCircle2} label="Completed" value={stats.completedTasks} trendValue={stats.velocity.trendPercent} />
            <StatCard icon={AlertTriangle} label="Overdue" value={stats.overdueCount} iconClassName="text-red-600" />
            <StatCard icon={AlertTriangle} label="Due This Week" value={stats.dueSoonCount} iconClassName="text-amber-600" />
          </div>

          <div className="mb-4 grid grid-cols-12 gap-3">
            <div className="col-span-12 lg:col-span-7">
              <VelocityCard velocity={stats.velocity} />
            </div>
            <div className="col-span-12 lg:col-span-5">
              <StatusDonutCard stats={stats} />
            </div>
          </div>

          <div className="mb-4 grid grid-cols-12 gap-3">
            <div className="col-span-12 lg:col-span-7">
              <UpcomingTasksCard tasks={stats.upcomingTasks} onOpenTask={openTask} />
            </div>
            <div className="col-span-12 lg:col-span-5">
              <TopContributorsCard contributors={stats.topContributors} />
            </div>
          </div>

          <ActivityFeed activity={stats.recentActivity} />

          {myStats && myStats.myTasks.total > 0 ? <MyStatsSection myStats={myStats} /> : null}
        </>
      )}
    </div>
  );
}
