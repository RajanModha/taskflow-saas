import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import * as Popover from '@radix-ui/react-popover';
import { DndContext, PointerSensor, closestCorners, useSensor, useSensors } from '@dnd-kit/core';
import { SortableContext, arrayMove, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { format, formatDistanceToNow, isPast } from 'date-fns';
import { AnimatePresence, motion } from 'framer-motion';
import { GripVertical, Lock, MoreHorizontal, Search, X } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import toast from 'react-hot-toast';
import { useMe } from '../../hooks/api/auth.hooks';
import { useTaskActivity } from '../../hooks/api/activity.hooks';
import { useAddChecklistItem, useChecklist, useDeleteChecklistItem, useReorderChecklist, useUpdateChecklistItem } from '../../hooks/api/checklist.hooks';
import { useComments, useCreateComment, useDeleteComment, useUpdateComment } from '../../hooks/api/comments.hooks';
import { useAddDependency, useRemoveDependency, useTaskDependencies } from '../../hooks/api/dependencies.hooks';
import { useMilestones } from '../../hooks/api/projects.hooks';
import { useAddTagToTask, useRemoveTagFromTask, useTags } from '../../hooks/api/tags.hooks';
import { useAssignTask, useTask, useTasks, useUpdateTask } from '../../hooks/api/tasks.hooks';
import { useMembers, useWorkspaceMe } from '../../hooks/api/workspace.hooks';
import { TaskPriority, TaskPriorityLabel, TaskStatus, TaskStatusColor, TaskStatusLabel, type ChecklistItemDto } from '../../types/api';
import { Avatar } from '../ui/Avatar';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { Select } from '../ui/Select';
import { Spinner } from '../ui/Spinner';

interface Props {
  taskId: string | null;
  onClose: () => void;
}

type TabKey = 'details' | 'comments' | 'activity' | 'deps';

function actionDotClass(action: string) {
  if (action.startsWith('project.')) return 'border-green-500';
  if (action.startsWith('member.')) return 'border-amber-500';
  return 'border-primary-500';
}

function formatAction(action: string): string {
  const map: Record<string, string> = {
    'task.created': 'created task',
    'task.status_changed': 'updated status',
    'task.assigned': 'assigned task',
    'task.commented': 'commented on task',
    'project.created': 'created project',
    'project.deleted': 'deleted project',
  };
  return map[action] ?? action.replaceAll('.', ' ');
}

function ChecklistRow({
  item,
  taskId,
  onToggle,
  onDelete,
}: {
  item: ChecklistItemDto;
  taskId: string;
  onToggle: (itemId: string, done: boolean) => void;
  onDelete: (itemId: string) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({
    id: item.id,
    data: { item, taskId },
  });

  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className="group flex h-8 items-center gap-2"
    >
      <button type="button" className="opacity-0 group-hover:opacity-100" {...attributes} {...listeners}>
        <GripVertical className="h-3.5 w-3.5 text-neutral-400" />
      </button>
      <input type="checkbox" checked={item.isCompleted} onChange={() => onToggle(item.id, !item.isCompleted)} />
      <span className={`flex-1 text-13 ${item.isCompleted ? 'text-neutral-400 line-through' : 'text-neutral-700'}`}>{item.title}</span>
      <button type="button" className="opacity-0 transition-opacity group-hover:opacity-100" onClick={() => onDelete(item.id)}>
        <X className="h-3.5 w-3.5 text-neutral-400" />
      </button>
    </div>
  );
}

export function TaskDetailSlideOver({ taskId, onClose }: Props) {
  const [activeTab, setActiveTab] = useState<TabKey>('details');
  const [titleEditing, setTitleEditing] = useState(false);
  const [descriptionEditing, setDescriptionEditing] = useState(false);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [comment, setComment] = useState('');
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editingCommentValue, setEditingCommentValue] = useState('');
  const [checklistDraft, setChecklistDraft] = useState('');
  const [depsSearch, setDepsSearch] = useState('');
  const [activityPage, setActivityPage] = useState(1);

  const { data: me } = useMe();
  const { data: task, isLoading } = useTask(taskId ?? null);
  const { data: checklist = [] } = useChecklist(taskId ?? null);
  const { data: workspace } = useWorkspaceMe();
  const { data: members } = useMembers({ page: 1, pageSize: 100 });
  const { data: tags } = useTags();
  const { data: milestones } = useMilestones(task?.projectId ?? null);
  const { data: commentsData } = useComments(taskId ?? null, { page: 1, pageSize: 50 });
  const { data: activityData } = useTaskActivity(taskId ?? null, { page: activityPage, pageSize: 20 });
  const { data: deps } = useTaskDependencies(taskId ?? null);
  const { data: addableDepsData } = useTasks({
    page: 1,
    pageSize: 20,
    projectId: task?.projectId,
    q: depsSearch || undefined,
  });

  const updateTask = useUpdateTask(taskId ?? '');
  const assignTask = useAssignTask(taskId ?? '');
  const addChecklistItem = useAddChecklistItem(taskId ?? '');
  const updateChecklistItem = useUpdateChecklistItem(taskId ?? '');
  const deleteChecklistItem = useDeleteChecklistItem(taskId ?? '');
  const reorderChecklist = useReorderChecklist(taskId ?? '');
  const addTag = useAddTagToTask(taskId ?? '');
  const removeTag = useRemoveTagFromTask(taskId ?? '');
  const createComment = useCreateComment(taskId ?? '');
  const updateComment = useUpdateComment(taskId ?? '');
  const deleteComment = useDeleteComment(taskId ?? '');
  const addDependency = useAddDependency(taskId ?? '');
  const removeDependency = useRemoveDependency(taskId ?? '');

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  useEffect(() => {
    if (!task) return;
    setTitle(task.title);
    setDescription(task.description ?? '');
  }, [task]);

  const sortedChecklist = useMemo(() => [...checklist].sort((a, b) => a.order - b.order), [checklist]);
  const doneCount = sortedChecklist.filter((item) => item.isCompleted).length;
  const progress = sortedChecklist.length ? Math.round((doneCount / sortedChecklist.length) * 100) : 0;
  const comments = commentsData?.items ?? [];
  const activity = activityData?.items ?? [];
  const addableDeps = (addableDepsData?.items ?? []).filter((candidate) => candidate.id !== taskId);

  if (!taskId) return null;

  const onTitleBlur = () => {
    setTitleEditing(false);
    if (task && title.trim() && title !== task.title) {
      updateTask.mutate({ title: title.trim() });
    }
  };

  const onDescriptionBlur = () => {
    setDescriptionEditing(false);
    if (!task) return;
    if ((task.description ?? '') !== description) {
      updateTask.mutate({ description: description || undefined });
    }
  };

  return (
    <AnimatePresence>
      <motion.div
        className="fixed inset-0 z-40 bg-surface-overlay"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
      />
      <motion.aside
        className="fixed right-0 top-0 z-50 h-full w-full max-w-[540px] bg-white shadow-e500"
        initial={{ x: '100%' }}
        animate={{ x: 0 }}
        exit={{ x: '100%' }}
        transition={{ type: 'spring', damping: 30, stiffness: 300 }}
      >
        <div className="flex h-[48px] items-center gap-2 border-b border-neutral-200 bg-white px-4">
          <button type="button" onClick={onClose}>
            <X className="h-4 w-4" />
          </button>
          <div className="min-w-0 flex-1 truncate text-12 text-neutral-500">
            <span>{workspace?.name ?? 'Project'}</span> <span className="mx-1">/</span>
            <span className="font-medium text-neutral-700">{task?.title ?? taskId}</span>
          </div>
          <DropdownMenu.Root>
            <DropdownMenu.Trigger asChild>
              <button type="button">
                <MoreHorizontal className="h-4 w-4 text-neutral-500" />
              </button>
            </DropdownMenu.Trigger>
            <DropdownMenu.Portal>
              <DropdownMenu.Content sideOffset={8} align="end" className="z-50 min-w-[140px] rounded-md border border-neutral-200 bg-white py-1 shadow-e200">
                <DropdownMenu.Item
                  onSelect={async () => {
                    await navigator.clipboard.writeText(window.location.href);
                    toast.success('Link copied');
                  }}
                  className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50"
                >
                  Copy link
                </DropdownMenu.Item>
                <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-red-600 outline-none data-[highlighted]:bg-red-50">
                  Delete
                </DropdownMenu.Item>
              </DropdownMenu.Content>
            </DropdownMenu.Portal>
          </DropdownMenu.Root>
        </div>

        <div className="sticky top-[48px] z-10 flex h-9 border-b border-neutral-200 bg-white">
          {[
            { key: 'details', label: 'Details' },
            { key: 'comments', label: `Comments (${task?.commentCount ?? 0})` },
            { key: 'activity', label: 'Activity' },
            { key: 'deps', label: 'Dependencies' },
          ].map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActiveTab(tab.key as TabKey)}
              className={`px-4 text-12 ${activeTab === tab.key ? 'border-b-2 border-primary-600 font-medium text-primary-700' : 'text-neutral-500'}`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {isLoading || !task ? (
          <div className="p-4">
            <Spinner size="sm" />
          </div>
        ) : (
          <>
            {activeTab === 'details' ? (
              <div className="flex h-[calc(100%-84px)] flex-col gap-4 overflow-y-auto px-4 py-3">
                <div>
                  {titleEditing ? (
                    <input
                      value={title}
                      onChange={(event) => setTitle(event.target.value)}
                      onBlur={onTitleBlur}
                      className="w-full border-0 border-b border-primary-400 bg-transparent pb-0.5 text-16 font-semibold text-neutral-800 outline-none"
                      autoFocus
                    />
                  ) : (
                    <div className="cursor-text text-16 font-semibold text-neutral-800" onClick={() => setTitleEditing(true)}>
                      {task.title}
                    </div>
                  )}
                </div>

                <div>
                  {descriptionEditing ? (
                    <textarea
                      value={description}
                      onChange={(event) => setDescription(event.target.value)}
                      onBlur={onDescriptionBlur}
                      className="min-h-[72px] w-full resize-none rounded border border-neutral-200 p-2 text-13 text-neutral-700 outline-none focus:border-primary-400"
                      autoFocus
                    />
                  ) : (
                    <div className={`cursor-text text-13 ${task.description ? 'text-neutral-700' : 'text-neutral-400'}`} onClick={() => setDescriptionEditing(true)}>
                      {task.description ?? 'Add description...'}
                    </div>
                  )}
                </div>

                <div className="overflow-hidden rounded-md border border-neutral-200">
                  <div className="flex h-9 items-center border-b border-neutral-100">
                    <div className="w-28 shrink-0 border-r border-neutral-100 bg-neutral-50 px-3 text-12 text-neutral-500">Status</div>
                    <div className="px-3">
                      <Select
                        className="w-40"
                        value={String(task.status)}
                        onChange={(value) => updateTask.mutate({ status: Number(value) as TaskStatus })}
                        options={Object.entries(TaskStatusLabel).map(([value, label]) => ({ value, label }))}
                      />
                    </div>
                  </div>
                  <div className="flex h-9 items-center border-b border-neutral-100">
                    <div className="w-28 shrink-0 border-r border-neutral-100 bg-neutral-50 px-3 text-12 text-neutral-500">Priority</div>
                    <div className="px-3">
                      <Select
                        className="w-40"
                        value={String(task.priority)}
                        onChange={(value) => updateTask.mutate({ priority: Number(value) as TaskPriority })}
                        options={Object.entries(TaskPriorityLabel).map(([value, label]) => ({ value, label }))}
                      />
                    </div>
                  </div>
                  <div className="flex h-9 items-center border-b border-neutral-100">
                    <div className="w-28 shrink-0 border-r border-neutral-100 bg-neutral-50 px-3 text-12 text-neutral-500">Assignee</div>
                    <div className="px-3">
                      <Select
                        className="w-56"
                        value={task.assignee?.id ?? ''}
                        onChange={(value) => assignTask.mutate({ assigneeId: value || null })}
                        options={[{ label: 'Unassigned', value: '' }, ...((members?.items ?? []).map((m) => ({ label: m.displayName ?? m.userName, value: m.id })))]}
                      />
                    </div>
                  </div>
                  <div className="flex h-9 items-center border-b border-neutral-100">
                    <div className="w-28 shrink-0 border-r border-neutral-100 bg-neutral-50 px-3 text-12 text-neutral-500">Due date</div>
                    <div className="flex items-center gap-2 px-3">
                      <span className={task.dueDateUtc && isPast(new Date(task.dueDateUtc)) ? 'text-red-600 text-12' : 'text-12 text-neutral-500'}>
                        {task.dueDateUtc ? format(new Date(task.dueDateUtc), 'MMM d, yyyy') : 'No due date'}
                      </span>
                      <input
                        type="date"
                        className="rounded border border-neutral-200 px-2 py-1 text-12"
                        value={task.dueDateUtc ? format(new Date(task.dueDateUtc), 'yyyy-MM-dd') : ''}
                        onChange={(event) => {
                          if (!event.target.value) {
                            updateTask.mutate({ dueDateUtc: undefined });
                            return;
                          }
                          updateTask.mutate({ dueDateUtc: new Date(event.target.value).toISOString() });
                        }}
                      />
                    </div>
                  </div>
                  <div className="flex h-9 items-center border-b border-neutral-100">
                    <div className="w-28 shrink-0 border-r border-neutral-100 bg-neutral-50 px-3 text-12 text-neutral-500">Milestone</div>
                    <div className="px-3">
                      <Select
                        className="w-56"
                        value={task.milestone?.id ?? ''}
                        onChange={(value) => updateTask.mutate({ milestoneId: value || undefined })}
                        options={[{ label: 'No milestone', value: '' }, ...((milestones ?? []).map((m) => ({ label: m.name, value: m.id })))]}
                      />
                    </div>
                  </div>
                  <div className="flex min-h-9 items-center">
                    <div className="w-28 shrink-0 self-stretch border-r border-neutral-100 bg-neutral-50 px-3 py-2 text-12 text-neutral-500">Tags</div>
                    <div className="flex flex-1 flex-wrap items-center gap-1.5 px-3 py-2">
                      {task.tags.map((tag) => (
                        <button
                          key={tag.id}
                          type="button"
                          className="rounded px-1.5 py-0.5 text-10 text-white"
                          style={{ backgroundColor: tag.color }}
                          onClick={() => removeTag.mutate(tag.id)}
                        >
                          {tag.name} ×
                        </button>
                      ))}
                      <Popover.Root>
                        <Popover.Trigger asChild>
                          <button type="button" className="text-12 text-primary-600">
                            + Add tag
                          </button>
                        </Popover.Trigger>
                        <Popover.Portal>
                          <Popover.Content className="z-50 w-52 rounded-md border border-neutral-200 bg-white p-2 shadow-e200" sideOffset={8}>
                            <div className="space-y-1">
                              {(tags ?? []).map((tag) => (
                                <button
                                  key={tag.id}
                                  type="button"
                                  className="flex w-full items-center gap-2 rounded px-2 py-1 text-left text-12 hover:bg-neutral-50"
                                  onClick={() => addTag.mutate(tag.id)}
                                >
                                  <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: tag.color }} />
                                  <span>{tag.name}</span>
                                </button>
                              ))}
                            </div>
                          </Popover.Content>
                        </Popover.Portal>
                      </Popover.Root>
                    </div>
                  </div>
                </div>

                <div>
                  <div className="mb-2 flex items-center justify-between">
                    <p className="text-13 font-semibold text-neutral-700">Checklist</p>
                    <p className="text-12 text-neutral-500">
                      {doneCount}/{sortedChecklist.length}
                    </p>
                  </div>
                  <div className="mb-2 h-1 rounded bg-neutral-100">
                    <div className="h-1 rounded bg-primary-500" style={{ width: `${progress}%` }} />
                  </div>

                  <DndContext
                    sensors={sensors}
                    collisionDetection={closestCorners}
                    onDragEnd={(event) => {
                      const { active, over } = event;
                      if (!over || active.id === over.id) return;
                      const oldIndex = sortedChecklist.findIndex((item) => item.id === active.id);
                      const newIndex = sortedChecklist.findIndex((item) => item.id === over.id);
                      if (oldIndex < 0 || newIndex < 0) return;
                      const reordered = arrayMove(sortedChecklist, oldIndex, newIndex).map((item) => item.id);
                      reorderChecklist.mutate({ orderedIds: reordered });
                    }}
                  >
                    <SortableContext items={sortedChecklist.map((item) => item.id)} strategy={verticalListSortingStrategy}>
                      <div className="space-y-1">
                        {sortedChecklist.map((item) => (
                          <ChecklistRow
                            key={item.id}
                            item={item}
                            taskId={task.id}
                            onToggle={(itemId, done) => updateChecklistItem.mutate({ itemId, payload: { isCompleted: done } })}
                            onDelete={(itemId) => deleteChecklistItem.mutate(itemId)}
                          />
                        ))}
                      </div>
                    </SortableContext>
                  </DndContext>

                  <div className="mt-2 flex items-center gap-2">
                    <Input
                      placeholder="Add item..."
                      value={checklistDraft}
                      onChange={(event) => setChecklistDraft(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' && checklistDraft.trim()) {
                          addChecklistItem.mutate({ title: checklistDraft.trim() });
                          setChecklistDraft('');
                        }
                      }}
                    />
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => {
                        if (!checklistDraft.trim()) return;
                        addChecklistItem.mutate({ title: checklistDraft.trim() });
                        setChecklistDraft('');
                      }}
                    >
                      Add
                    </Button>
                  </div>
                </div>

                {task.isBlocked ? (
                  <div className="rounded border border-amber-200 bg-amber-50 px-3 py-2 text-12 text-amber-700">
                    <div className="inline-flex items-center gap-1">
                      <Lock className="h-3.5 w-3.5" />
                      Blocked by {task.blockerCount} task(s)
                    </div>
                    <button type="button" className="ml-2 text-primary-600" onClick={() => setActiveTab('deps')}>
                      View dependencies
                    </button>
                  </div>
                ) : null}
              </div>
            ) : null}

            {activeTab === 'comments' ? (
              <div className="flex h-[calc(100%-84px)] flex-col">
                <div className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
                  {comments.map((item) => {
                    const authorName = item.author.displayName ?? item.author.userName;
                    const canEdit = item.author.id === me?.id;
                    return (
                      <div key={item.id} className="group flex gap-2.5">
                        <Avatar name={authorName} size="sm" />
                        <div className="min-w-0 flex-1">
                          <p className="text-13 font-medium text-neutral-700">
                            {authorName}
                            <span className="ml-1 text-11 font-normal text-neutral-400">
                              {formatDistanceToNow(new Date(item.createdAt), { addSuffix: true })}
                            </span>
                          </p>
                          {item.isDeleted ? (
                            <p className="text-13 italic text-neutral-400">[deleted]</p>
                          ) : editingCommentId === item.id ? (
                            <div className="space-y-2">
                              <textarea
                                className="w-full rounded border border-neutral-200 p-2 text-13"
                                value={editingCommentValue}
                                onChange={(event) => setEditingCommentValue(event.target.value)}
                              />
                              <div className="flex gap-2">
                                <Button
                                  size="sm"
                                  onClick={() => {
                                    updateComment.mutate({ commentId: item.id, payload: { content: editingCommentValue } });
                                    setEditingCommentId(null);
                                  }}
                                >
                                  Save
                                </Button>
                                <Button size="sm" variant="secondary" onClick={() => setEditingCommentId(null)}>
                                  Cancel
                                </Button>
                              </div>
                            </div>
                          ) : (
                            <>
                              <p className="text-13 text-neutral-700">{item.content}</p>
                              {canEdit ? (
                                <div className="mt-1 hidden gap-2 group-hover:flex">
                                  <button
                                    type="button"
                                    className="text-12 text-neutral-500 hover:text-neutral-700"
                                    onClick={() => {
                                      setEditingCommentId(item.id);
                                      setEditingCommentValue(item.content);
                                    }}
                                  >
                                    Edit
                                  </button>
                                  <button
                                    type="button"
                                    className="text-12 text-red-600"
                                    onClick={() => deleteComment.mutate(item.id)}
                                  >
                                    Delete
                                  </button>
                                </div>
                              ) : null}
                            </>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
                <div className="border-t border-neutral-200 bg-white p-3">
                  <div className="flex items-start gap-2">
                    <Avatar name={me?.displayName ?? me?.userName ?? 'Me'} size="sm" />
                    <textarea
                      rows={2}
                      className="min-h-[60px] flex-1 rounded border border-neutral-200 p-2 text-13"
                      value={comment}
                      onChange={(event) => setComment(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' && event.ctrlKey && comment.trim()) {
                          createComment.mutate(
                            { content: comment.trim() },
                            {
                              onSuccess: () => setComment(''),
                            },
                          );
                        }
                      }}
                    />
                  </div>
                  <div className="mt-2 flex justify-end">
                    <Button
                      size="sm"
                      onClick={() => {
                        if (!comment.trim()) return;
                        createComment.mutate(
                          { content: comment.trim() },
                          {
                            onSuccess: () => setComment(''),
                          },
                        );
                      }}
                    >
                      Comment
                    </Button>
                  </div>
                </div>
              </div>
            ) : null}

            {activeTab === 'activity' ? (
              <div className="h-[calc(100%-84px)] overflow-y-auto px-0">
                <div className="ml-5 border-l-2 border-neutral-150 pl-4">
                  {activity.map((event) => {
                    const metadata = (event.metadata ?? {}) as Record<string, unknown>;
                    return (
                      <div key={event.id} className="relative border-b border-neutral-100 py-2 last:border-b-0">
                        <span className={`absolute -left-[9px] h-4 w-4 rounded-full border-2 bg-white ${actionDotClass(event.action)}`} />
                        <p className="text-12 text-neutral-600">
                          {event.actor.userName} {formatAction(event.action)}
                          {metadata.from || metadata.to ? ` from ${String(metadata.from ?? '—')} to ${String(metadata.to ?? '—')}` : ''}
                        </p>
                        <p className="text-11 text-neutral-400">{formatDistanceToNow(new Date(event.occurredAt), { addSuffix: true })}</p>
                      </div>
                    );
                  })}
                </div>
                {activityData?.hasNextPage ? (
                  <div className="px-4 py-3">
                    <Button size="sm" variant="secondary" onClick={() => setActivityPage((value) => value + 1)}>
                      Load more
                    </Button>
                  </div>
                ) : null}
              </div>
            ) : null}

            {activeTab === 'deps' ? (
              <div className="h-[calc(100%-84px)] overflow-y-auto px-4 py-3">
                <div className="mb-5">
                  <h3 className="mb-2 text-13 font-semibold text-neutral-700">Blocked by</h3>
                  <div className="space-y-1">
                    {(deps?.blockedBy ?? []).map((dep) => (
                      <div key={dep.blockingTask.id} className="flex items-center gap-2 rounded border border-neutral-200 px-2 py-1.5">
                        <span className="rounded px-1.5 py-0.5 text-10" style={{ color: TaskStatusColor[dep.blockingTask.status], backgroundColor: `${TaskStatusColor[dep.blockingTask.status]}22` }}>
                          {TaskStatusLabel[dep.blockingTask.status]}
                        </span>
                        <span className="flex-1 truncate text-12 text-neutral-700">{dep.blockingTask.title}</span>
                        <button type="button" onClick={() => removeDependency.mutate(dep.blockingTask.id)}>
                          <X className="h-3.5 w-3.5 text-neutral-400" />
                        </button>
                      </div>
                    ))}
                  </div>

                  <Popover.Root>
                    <Popover.Trigger asChild>
                      <button type="button" className="mt-2 text-12 text-primary-600">
                        Add blocker
                      </button>
                    </Popover.Trigger>
                    <Popover.Portal>
                      <Popover.Content className="z-50 w-72 rounded-md border border-neutral-200 bg-white p-3 shadow-e200" sideOffset={8}>
                        <Input
                          placeholder="Search tasks"
                          leftIcon={<Search className="h-3.5 w-3.5" />}
                          value={depsSearch}
                          onChange={(event) => setDepsSearch(event.target.value)}
                        />
                        <div className="mt-2 max-h-52 space-y-1 overflow-y-auto">
                          {addableDeps.map((candidate) => (
                            <button
                              key={candidate.id}
                              type="button"
                              className="w-full rounded px-2 py-1.5 text-left text-12 hover:bg-neutral-50"
                              onClick={() =>
                                addDependency.mutate(
                                  { blockingTaskId: candidate.id },
                                  {
                                    onError: (error) => {
                                      const detail = (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
                                      toast.error(detail ?? 'Failed to add dependency');
                                    },
                                  },
                                )
                              }
                            >
                              {candidate.title}
                            </button>
                          ))}
                        </div>
                      </Popover.Content>
                    </Popover.Portal>
                  </Popover.Root>
                </div>

                <div>
                  <h3 className="mb-2 text-13 font-semibold text-neutral-700">Blocking</h3>
                  <div className="space-y-1">
                    {(deps?.blocking ?? []).map((dep) => (
                      <div key={dep.blockingTask.id} className="flex items-center gap-2 rounded border border-neutral-200 px-2 py-1.5">
                        <span className="rounded px-1.5 py-0.5 text-10" style={{ color: TaskStatusColor[dep.blockingTask.status], backgroundColor: `${TaskStatusColor[dep.blockingTask.status]}22` }}>
                          {TaskStatusLabel[dep.blockingTask.status]}
                        </span>
                        <span className="truncate text-12 text-neutral-700">{dep.blockingTask.title}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            ) : null}
          </>
        )}
      </motion.aside>
    </AnimatePresence>
  );
}
