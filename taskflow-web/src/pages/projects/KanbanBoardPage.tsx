import * as Popover from '@radix-ui/react-popover';
import { closestCorners, DndContext, DragOverlay, PointerSensor, useDroppable, useSensor, useSensors, type DragEndEvent, type DragStartEvent } from '@dnd-kit/core';
import { SortableContext, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { format } from 'date-fns';
import { Filter, MessageSquare, Plus, UserCircle } from 'lucide-react';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { useQueryClient } from '@tanstack/react-query';
import { ProjectSubNav } from '../../components/projects/ProjectSubNav';
import { CreateTaskModal } from '../../components/tasks/CreateTaskModal';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Select } from '../../components/ui/Select';
import { TaskDetailSlideOver } from '../../components/tasks/TaskDetailSlideOver';
import { useBoardData, useMoveTask } from '../../hooks/api/projects.hooks';
import { useTags } from '../../hooks/api/tags.hooks';
import { useCreateTask } from '../../hooks/api/tasks.hooks';
import { useMembers } from '../../hooks/api/workspace.hooks';
import { PriorityColor, type BoardTaskDto, type ProjectBoardResponse, TaskPriority, type TaskStatus } from '../../types/api';
import { cn } from '../../lib/utils';

interface BoardFilters {
  assigneeId?: string;
  tagId?: string;
  q?: string;
}

function TaskCard({
  task,
  columnStatusValue,
  onSelect,
}: {
  task: BoardTaskDto;
  columnStatusValue: TaskStatus;
  onSelect: (taskId: string) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: task.id,
    data: { task, columnStatusValue },
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    borderLeftColor: PriorityColor[task.priority],
  };

  const visibleTags = task.tags.slice(0, 2);
  const hiddenTagCount = Math.max(0, task.tags.length - visibleTags.length);
  const dueSoon = task.dueDateUtc ? new Date(task.dueDateUtc).getTime() - Date.now() < 1000 * 60 * 60 * 24 * 3 : false;
  const dueColor = task.isOverdue ? 'text-red-600' : dueSoon ? 'text-amber-600' : 'text-neutral-400';

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="cursor-grab rounded-md border border-neutral-200 bg-white p-3 shadow-e100 hover:shadow-e200 active:cursor-grabbing"
      {...attributes}
      {...listeners}
      onClick={() => onSelect(task.id)}
    >
      <div className="mb-2 flex items-center justify-between gap-2">
        <div className="flex min-w-0 items-center gap-1">
          {visibleTags.map((tag) => (
            <span key={tag.id} className="truncate rounded px-1.5 py-0.5 text-10 font-medium text-white" style={{ backgroundColor: tag.color }}>
              {tag.name}
            </span>
          ))}
          {hiddenTagCount > 0 ? <span className="text-10 text-neutral-500">+{hiddenTagCount}</span> : null}
        </div>
        {task.isOverdue ? <span className="h-2 w-2 rounded-full bg-orange-500" /> : null}
      </div>

      <p className="line-clamp-2 border-l-4 pl-2 text-13 font-medium text-neutral-800" style={{ borderLeftColor: PriorityColor[task.priority] }}>
        {task.title}
      </p>

      <div className="mt-2 flex items-center gap-2">
        <span className={cn('text-11', dueColor)}>{task.dueDateUtc ? format(new Date(task.dueDateUtc), 'MMM d') : ''}</span>
        <span className="flex items-center gap-1 text-11 text-neutral-400">
          <MessageSquare className="h-3 w-3" />
          {task.commentCount}
        </span>
        <span className="ml-auto">
          {task.assignee ? (
            <Avatar size="xs" name={task.assignee.displayName ?? task.assignee.userName} />
          ) : (
            <UserCircle className="h-4 w-4 text-neutral-300" />
          )}
        </span>
      </div>
    </div>
  );
}

function ColumnDropZone({
  statusValue,
  children,
}: {
  statusValue: TaskStatus;
  children: React.ReactNode;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: String(statusValue),
    data: { statusValue },
  });

  return (
    <div
      ref={setNodeRef}
      className={cn('min-h-[120px] rounded-md p-2 transition-colors', isOver && 'border-2 border-primary-400 bg-primary-50/30')}
    >
      {children}
    </div>
  );
}

export default function KanbanBoardPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<BoardFilters>({});
  const [activeTask, setActiveTask] = useState<BoardTaskDto | null>(null);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [quickAddStatus, setQuickAddStatus] = useState<number | null>(null);
  const [quickAddTitle, setQuickAddTitle] = useState('');

  const { data: board, refetch } = useBoardData(projectId ?? null, filters);
  const moveTask = useMoveTask(projectId ?? '');
  const createTask = useCreateTask();
  const { data: members } = useMembers({ page: 1, pageSize: 100 });
  const { data: tags } = useTags();

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const handleDragStart = (event: DragStartEvent) => {
    const task = event.active.data.current?.task as BoardTaskDto | undefined;
    if (task) {
      setActiveTask(task);
    }
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveTask(null);
    if (!board || !over) return;

    const task = active.data.current?.task as BoardTaskDto | undefined;
    const taskOldStatus = active.data.current?.columnStatusValue as TaskStatus | undefined;
    const newStatus = Number(over.data.current?.statusValue) as TaskStatus;
    if (!task || taskOldStatus === undefined || taskOldStatus === newStatus) return;

    queryClient.setQueryData<ProjectBoardResponse>(['board', projectId, filters], (old) => {
      if (!old) return old;
      return {
        ...old,
        columns: old.columns.map((column) => {
          if (column.statusValue === taskOldStatus) {
            return { ...column, tasks: column.tasks.filter((item) => item.id !== task.id) };
          }
          if (column.statusValue === newStatus) {
            return { ...column, tasks: [...column.tasks, task] };
          }
          return column;
        }),
      };
    });

    moveTask.mutate(
      { taskId: task.id, payload: { newStatus } },
      {
        onError: () => {
          refetch();
          toast.error('Failed to move task');
        },
      },
    );
  };

  const createQuickTask = async (statusValue: TaskStatus) => {
    const title = quickAddTitle.trim();
    if (!title || !projectId) {
      setQuickAddStatus(null);
      setQuickAddTitle('');
      return;
    }
    try {
      await createTask.mutateAsync({
        title,
        projectId,
        status: statusValue,
        priority: TaskPriority.None,
      });
      setQuickAddStatus(null);
      setQuickAddTitle('');
      toast.success('Task created');
    } catch {
      toast.error('Failed to create task');
    }
  };

  const boardColumns = board?.columns ?? [];

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="sticky top-0 z-10 border-b border-neutral-200 bg-white px-4 pt-2">
        <ProjectSubNav projectId={projectId ?? ''} activeTab="board" />
      </div>
      <div className="sticky top-12 z-10 flex h-9 items-center border-b border-neutral-200 bg-white px-4">
        <div className="ml-auto flex items-center gap-2">
          <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={() => setCreateOpen(true)}>
            Add task
          </Button>
          <Popover.Root>
            <Popover.Trigger asChild>
              <Button size="sm" variant="secondary" leftIcon={<Filter className="h-3.5 w-3.5" />}>
                Filters
              </Button>
            </Popover.Trigger>
            <Popover.Portal>
              <Popover.Content className="z-50 w-72 rounded-md border border-neutral-200 bg-white p-3 shadow-e200" sideOffset={8} align="end">
                <div className="space-y-3">
                  <Select
                    options={[{ label: 'All assignees', value: '' }, ...(members?.items ?? []).map((member) => ({ label: member.displayName ?? member.userName, value: member.id }))]}
                    value={filters.assigneeId ?? ''}
                    onChange={(value) => setFilters((prev) => ({ ...prev, assigneeId: value || undefined }))}
                    placeholder="Assignee"
                  />
                  <Select
                    options={[{ label: 'All tags', value: '' }, ...(tags ?? []).map((tag) => ({ label: tag.name, value: tag.id }))]}
                    value={filters.tagId ?? ''}
                    onChange={(value) => setFilters((prev) => ({ ...prev, tagId: value || undefined }))}
                    placeholder="Tag"
                  />
                  <Input
                    placeholder="Search tasks"
                    value={filters.q ?? ''}
                    onChange={(event) => setFilters((prev) => ({ ...prev, q: event.target.value || undefined }))}
                  />
                  <div className="flex justify-end">
                    <Button size="sm" variant="ghost" onClick={() => setFilters({})}>
                      Clear
                    </Button>
                  </div>
                </div>
              </Popover.Content>
            </Popover.Portal>
          </Popover.Root>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-x-auto p-4">
        <DndContext sensors={sensors} collisionDetection={closestCorners} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
          <div className="flex min-w-max gap-3 pb-4">
            {boardColumns.map((column) => (
              <div key={column.statusValue} className="w-[264px] flex-shrink-0 rounded-md border border-neutral-200 bg-neutral-50">
                <div className="flex items-center gap-2 border-b border-neutral-200 px-3 py-2">
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: column.color }} />
                  <span className="text-12 font-semibold text-neutral-700">{column.displayName}</span>
                  <span className="rounded bg-neutral-200 px-1.5 text-11 text-neutral-600">{column.taskCount}</span>
                  <button
                    type="button"
                    className="ml-auto text-12 text-neutral-500 hover:text-neutral-700"
                    onClick={() => {
                      setQuickAddStatus(column.statusValue);
                      setQuickAddTitle('');
                    }}
                  >
                    + Add
                  </button>
                </div>

                <ColumnDropZone statusValue={column.statusValue}>
                  {quickAddStatus === column.statusValue ? (
                    <div className="mb-2 rounded-md border border-neutral-200 bg-white p-2">
                      <input
                        autoFocus
                        className="w-full border-0 text-13 outline-none"
                        placeholder="Task title..."
                        value={quickAddTitle}
                        onChange={(event) => setQuickAddTitle(event.target.value)}
                        onBlur={() => {
                          if (!quickAddTitle.trim()) {
                            setQuickAddStatus(null);
                          }
                        }}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter') {
                            void createQuickTask(column.statusValue);
                          }
                          if (event.key === 'Escape') {
                            setQuickAddStatus(null);
                            setQuickAddTitle('');
                          }
                        }}
                      />
                    </div>
                  ) : null}

                  <SortableContext items={column.tasks.map((task) => task.id)} strategy={verticalListSortingStrategy}>
                    <div className="space-y-2">
                      {column.tasks.map((task) => (
                        <TaskCard
                          key={task.id}
                          task={task}
                          columnStatusValue={column.statusValue}
                          onSelect={(taskId) => setSelectedTaskId(taskId)}
                        />
                      ))}
                    </div>
                  </SortableContext>
                </ColumnDropZone>
              </div>
            ))}
          </div>

          <DragOverlay>
            {activeTask ? (
              <div className="w-[264px] rotate-1 rounded-md border border-neutral-200 bg-white p-3 opacity-90 shadow-e400">
                <p className="line-clamp-2 text-13 font-medium text-neutral-800">{activeTask.title}</p>
              </div>
            ) : null}
          </DragOverlay>
        </DndContext>
      </div>

      {selectedTaskId ? <TaskDetailSlideOver taskId={selectedTaskId} onClose={() => setSelectedTaskId(null)} /> : null}
      <CreateTaskModal open={createOpen} onOpenChange={setCreateOpen} projectId={projectId ?? ''} />
    </div>
  );
}
