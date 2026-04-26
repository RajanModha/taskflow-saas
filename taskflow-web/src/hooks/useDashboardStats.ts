export interface DashboardStatsDto {
  workspaceName: string;
  user: { displayName: string };
  kpis: {
    totalTasks: number;
    completedTasks: number;
    overdueTasks: number;
    activeProjects: number;
    trends: {
      totalTasks: number;
      completedTasks: number;
      overdueTasks: number;
      activeProjects: number;
    };
  };
  velocity: Array<{ name: string; completed: number }>;
  statusBreakdown: Array<{ name: 'To Do' | 'In Progress' | 'Done' | 'Blocked'; value: number; color: string }>;
  upcomingTasks: Array<{
    id: string;
    title: string;
    project: string;
    dueLabel: string;
    dueTone: 'neutral' | 'warning' | 'danger';
    priority: 'high' | 'medium' | 'low';
    assigneeName: string;
  }>;
  contributors: Array<{ id: string; name: string; completed: number }>;
  activity: Array<{ id: string; type: 'task' | 'project' | 'comment' | 'member'; text: string; time: string }>;
}

const mockStats: DashboardStatsDto = {
  workspaceName: 'Product Engineering',
  user: { displayName: 'Alex Johnson' },
  kpis: {
    totalTasks: 128,
    completedTasks: 76,
    overdueTasks: 9,
    activeProjects: 12,
    trends: {
      totalTasks: 12,
      completedTasks: 9,
      overdueTasks: -3,
      activeProjects: 2,
    },
  },
  velocity: [
    { name: 'Mon', completed: 8 },
    { name: 'Tue', completed: 11 },
    { name: 'Wed', completed: 9 },
    { name: 'Thu', completed: 13 },
    { name: 'Fri', completed: 10 },
    { name: 'Sat', completed: 6 },
    { name: 'Sun', completed: 7 },
  ],
  statusBreakdown: [
    { name: 'To Do', value: 28, color: '#dfe1e6' },
    { name: 'In Progress', value: 34, color: '#6366f1' },
    { name: 'Done', value: 58, color: '#22c55e' },
    { name: 'Blocked', value: 8, color: '#f97316' },
  ],
  upcomingTasks: [
    {
      id: 'task-1',
      title: 'Finalize sprint planning board interaction states',
      project: 'Web App',
      dueLabel: 'Today',
      dueTone: 'danger',
      priority: 'high',
      assigneeName: 'Priya Sharma',
    },
    {
      id: 'task-2',
      title: 'Add optimistic updates for project reorder',
      project: 'Frontend',
      dueLabel: 'Tomorrow',
      dueTone: 'warning',
      priority: 'medium',
      assigneeName: 'Alex Johnson',
    },
    {
      id: 'task-3',
      title: 'Refine onboarding empty states across dashboard modules',
      project: 'UX',
      dueLabel: 'Apr 29',
      dueTone: 'neutral',
      priority: 'low',
      assigneeName: 'Chris Lee',
    },
    {
      id: 'task-4',
      title: 'QA regression pass for notification panel',
      project: 'QA',
      dueLabel: 'Apr 30',
      dueTone: 'neutral',
      priority: 'medium',
      assigneeName: 'Nina Patel',
    },
  ],
  contributors: [
    { id: 'c1', name: 'Priya Sharma', completed: 27 },
    { id: 'c2', name: 'Alex Johnson', completed: 23 },
    { id: 'c3', name: 'Chris Lee', completed: 19 },
    { id: 'c4', name: 'Nina Patel', completed: 14 },
    { id: 'c5', name: 'Sam Wilson', completed: 11 },
  ],
  activity: [
    { id: 'a1', type: 'task', text: 'Priya moved "Release checklist" to Done', time: '5m ago' },
    { id: 'a2', type: 'comment', text: 'Alex commented on "Search performance audit"', time: '14m ago' },
    { id: 'a3', type: 'project', text: 'New project "Billing Revamp" was created', time: '38m ago' },
    { id: 'a4', type: 'task', text: 'Nina marked "Notification cache cleanup" complete', time: '1h ago' },
    { id: 'a5', type: 'member', text: 'Chris invited Jordan to Product Engineering', time: '2h ago' },
    { id: 'a6', type: 'task', text: 'Sam set due date for "CI flaky test triage"', time: '3h ago' },
    { id: 'a7', type: 'comment', text: 'Priya replied in "Roadmap alignment thread"', time: '4h ago' },
    { id: 'a8', type: 'project', text: 'Project "Mobile v2" moved to In Progress', time: '6h ago' },
  ],
};

export function useDashboardStats() {
  return {
    data: mockStats,
    isLoading: false,
    isError: false,
  };
}
