import { AnimatePresence, motion } from 'framer-motion';
import { ChevronDown, MoreHorizontal, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Avatar } from '../ui/Avatar';
import { Button } from '../ui/Button';
import { cn } from '../../lib/utils';

type TabKey = 'details' | 'comments' | 'activity';
type TaskStatus = 'todo' | 'progress' | 'done' | 'cancelled';
type TaskPriority = 'high' | 'medium' | 'low' | 'none';

export interface TaskDetailTask {
  id: string;
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  assignee?: string;
  dueDate?: string;
  tags?: string[];
  milestone?: string;
  checklist: Array<{ id: string; label: string; done: boolean }>;
  comments: Array<{ id: string; author: string; body: string; time: string }>;
  activity: Array<{ id: string; text: string; time: string; tone?: 'neutral' | 'primary' | 'success' | 'warning' }>;
}

export interface TaskDetailSlideOverProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  task: TaskDetailTask;
  breadcrumbLabel?: string;
}

function statusLabel(status: TaskStatus) {
  return status === 'todo'
    ? 'To Do'
    : status === 'progress'
      ? 'In Progress'
      : status === 'done'
        ? 'Done'
        : 'Cancelled';
}

function statusClass(status: TaskStatus) {
  if (status === 'todo') return 'bg-status-todo-bg text-status-todo-text';
  if (status === 'progress') return 'bg-status-progress-bg text-status-progress-text';
  if (status === 'done') return 'bg-status-done-bg text-status-done-text';
  return 'bg-status-cancelled-bg text-status-cancelled-text';
}

function priorityLabel(priority: TaskPriority) {
  return priority[0].toUpperCase() + priority.slice(1);
}

function ActivityDot({ tone = 'neutral' }: { tone?: 'neutral' | 'primary' | 'success' | 'warning' }) {
  const toneClass =
    tone === 'primary'
      ? 'border-primary-500'
      : tone === 'success'
        ? 'border-emerald-500'
        : tone === 'warning'
          ? 'border-amber-500'
          : 'border-neutral-300';

  return <span className={cn('absolute -left-[9px] h-4 w-4 rounded-full border-2 bg-white', toneClass)} />;
}

function StatusSelect({ value }: { value: TaskStatus }) {
  return (
    <button
      type="button"
      className={cn(
        'inline-flex h-6 items-center gap-1.5 rounded-sm px-2 text-12 font-medium hover:opacity-80',
        statusClass(value),
      )}
    >
      {statusLabel(value)}
      <ChevronDown className="h-3 w-3" />
    </button>
  );
}

function PropertyRow({
  label,
  children,
  last,
}: {
  label: string;
  children: React.ReactNode;
  last?: boolean;
}) {
  return (
    <div className={cn('flex min-h-[36px] items-center border-b border-neutral-100', last && 'border-b-0')}>
      <div className="w-28 flex-shrink-0 border-r border-neutral-100 bg-neutral-50 px-3 py-2 text-12 font-medium text-neutral-500">
        {label}
      </div>
      <div className="flex-1 px-3 py-2">{children}</div>
    </div>
  );
}

export function TaskDetailSlideOver({ open, onOpenChange, task, breadcrumbLabel = 'Tasks' }: TaskDetailSlideOverProps) {
  const [activeTab, setActiveTab] = useState<TabKey>('details');
  const [titleDraft, setTitleDraft] = useState(task.title);
  const [editingTitle, setEditingTitle] = useState(false);
  const doneCount = useMemo(() => task.checklist.filter((c) => c.done).length, [task.checklist]);
  const progressPct = task.checklist.length === 0 ? 0 : Math.round((doneCount / task.checklist.length) * 100);

  return (
    <AnimatePresence>
      {open ? (
        <>
          <motion.div
            className="fixed inset-0 z-40 bg-surface-overlay"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => onOpenChange(false)}
          />
          <motion.aside
            className="fixed right-0 top-0 z-50 flex h-full w-full max-w-[540px] flex-col border-l border-neutral-200 bg-white"
            initial={{ x: 540 }}
            animate={{ x: 0 }}
            exit={{ x: 540 }}
            transition={{ type: 'spring', damping: 30, stiffness: 300 }}
          >
            <div className="sticky top-0 z-20 flex h-12 items-center gap-2 border-b border-neutral-200 bg-white px-4">
              <button
                type="button"
                className="flex h-7 w-7 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100 hover:text-neutral-700"
                onClick={() => onOpenChange(false)}
                aria-label="Close details"
              >
                <X className="h-4 w-4" />
              </button>
              <div className="min-w-0 flex-1 truncate text-12">
                <span className="text-neutral-400">{breadcrumbLabel}</span>
                <span className="mx-1 text-neutral-300">/</span>
                <span className="font-medium text-neutral-700">{task.id}</span>
              </div>
              <button
                type="button"
                className="flex h-7 w-7 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100 hover:text-neutral-700"
                aria-label="More actions"
              >
                <MoreHorizontal className="h-4 w-4" />
              </button>
            </div>

            <div className="sticky top-12 z-10 flex border-b border-neutral-200 bg-white">
              {(['details', 'comments', 'activity'] as const).map((tab) => (
                <button
                  key={tab}
                  type="button"
                  onClick={() => setActiveTab(tab)}
                  className={cn(
                    'h-9 px-4 text-13 text-neutral-500 hover:text-neutral-800',
                    activeTab === tab && ' -mb-px border-b-2 border-primary-600 font-medium text-primary-700',
                  )}
                >
                  {tab[0].toUpperCase() + tab.slice(1)}
                </button>
              ))}
            </div>

            {activeTab === 'details' ? (
              <div className="flex-1 overflow-y-auto px-4 py-3">
                {editingTitle ? (
                  <input
                    value={titleDraft}
                    onChange={(e) => setTitleDraft(e.target.value)}
                    onBlur={() => setEditingTitle(false)}
                    className="mb-3 w-full border-0 border-b border-primary-400 bg-transparent pb-0.5 text-16 font-semibold text-neutral-800 focus:outline-none"
                    autoFocus
                  />
                ) : (
                  <button
                    type="button"
                    onClick={() => setEditingTitle(true)}
                    className="mb-3 text-left text-16 font-semibold leading-snug text-neutral-800"
                  >
                    {titleDraft}
                  </button>
                )}

                {task.description ? (
                  <p className="mb-4 text-13 leading-relaxed text-neutral-600">{task.description}</p>
                ) : (
                  <button type="button" className="mb-4 cursor-text text-13 text-neutral-400">
                    Add description...
                  </button>
                )}

                <div className="mb-4 grid gap-0 overflow-hidden rounded-md border border-neutral-200">
                  <PropertyRow label="Status">
                    <StatusSelect value={task.status} />
                  </PropertyRow>
                  <PropertyRow label="Priority">
                    <span className="text-12 text-neutral-700">{priorityLabel(task.priority)}</span>
                  </PropertyRow>
                  <PropertyRow label="Assignee">
                    {task.assignee ? (
                      <div className="flex items-center gap-2">
                        <Avatar name={task.assignee} size="sm" />
                        <span className="text-12 text-neutral-700">{task.assignee}</span>
                      </div>
                    ) : (
                      <span className="text-12 text-neutral-400">Unassigned</span>
                    )}
                  </PropertyRow>
                  <PropertyRow label="Due Date">
                    <span className="text-12 text-neutral-700">{task.dueDate ?? 'No due date'}</span>
                  </PropertyRow>
                  <PropertyRow label="Tags">
                    <span className="text-12 text-neutral-700">{task.tags?.join(', ') || 'None'}</span>
                  </PropertyRow>
                  <PropertyRow label="Milestone" last>
                    <span className="text-12 text-neutral-700">{task.milestone || 'None'}</span>
                  </PropertyRow>
                </div>

                <div>
                  <div className="mb-2 flex items-center justify-between">
                    <p className="text-12 font-semibold text-neutral-600">Checklist</p>
                    <p className="text-12 text-neutral-400">
                      {doneCount}/{task.checklist.length}
                    </p>
                  </div>
                  <div className="mb-2 h-1 overflow-hidden rounded-full bg-neutral-100">
                    <div className="h-1 rounded-full bg-primary-500" style={{ width: `${progressPct}%` }} />
                  </div>
                  <div className="space-y-0.5">
                    {task.checklist.map((item) => (
                      <div key={item.id} className="group flex h-8 items-center gap-2">
                        <input type="checkbox" checked={item.done} readOnly className="h-3.5 w-3.5 rounded border-neutral-300" />
                        <span className="flex-1 text-13 text-neutral-700">{item.label}</span>
                        <button
                          type="button"
                          className="hidden h-5 w-5 items-center justify-center rounded text-neutral-400 hover:bg-neutral-100 hover:text-neutral-600 group-hover:flex"
                        >
                          <X className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    ))}
                  </div>
                  <button type="button" className="mt-1 cursor-pointer text-13 text-primary-600 hover:text-primary-700">
                    + Add item
                  </button>
                </div>
              </div>
            ) : null}

            {activeTab === 'comments' ? (
              <div className="flex flex-1 flex-col">
                <div className="flex-1 overflow-y-auto px-4 py-3">
                  <div className="space-y-3">
                    {task.comments.map((comment) => (
                      <div key={comment.id} className="group flex gap-2.5">
                        <Avatar name={comment.author} size="sm" />
                        <div className="min-w-0 flex-1">
                          <p className="text-13 font-medium text-neutral-700">
                            {comment.author}
                            <span className="ml-1 text-12 font-normal text-neutral-400">{comment.time}</span>
                          </p>
                          <p className="mt-0.5 text-13 text-neutral-700">{comment.body}</p>
                          <div className="mt-1 hidden gap-2 group-hover:flex">
                            <button type="button" className="text-12 text-neutral-500 hover:text-neutral-700">
                              Reply
                            </button>
                            <button type="button" className="text-12 text-neutral-500 hover:text-neutral-700">
                              Edit
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
                <div className="sticky bottom-0 border-t border-neutral-200 bg-white p-3">
                  <textarea
                    className="h-20 min-h-[80px] w-full resize-none rounded border border-neutral-300 p-2 text-13 text-neutral-700 focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-100"
                    placeholder="Write a comment..."
                  />
                  <div className="mt-2 flex items-center justify-between">
                    <p className="text-11 text-neutral-400">Ctrl+Enter to submit</p>
                    <Button size="sm">Post</Button>
                  </div>
                </div>
              </div>
            ) : null}

            {activeTab === 'activity' ? (
              <div className="flex-1 overflow-y-auto px-0 py-0">
                <div className="ml-6 border-l-2 border-neutral-150">
                  {task.activity.map((event) => (
                    <div key={event.id} className="relative border-b border-neutral-100 py-2 pl-3 last:border-b-0">
                      <ActivityDot tone={event.tone} />
                      <p className="text-12 text-neutral-600">{event.text}</p>
                      <p className="text-11 text-neutral-400">{event.time}</p>
                    </div>
                  ))}
                </div>
              </div>
            ) : null}
          </motion.aside>
        </>
      ) : null}
    </AnimatePresence>
  );
}
