export enum TaskStatus {
  Backlog = 0,
  Todo = 1,
  InProgress = 2,
  Done = 3,
  Cancelled = 4,
}

export enum TaskPriority {
  None = 0,
  Low = 1,
  Medium = 2,
  High = 3,
}

export const TaskStatusLabel: Record<TaskStatus, string> = {
  [TaskStatus.Backlog]: 'Backlog',
  [TaskStatus.Todo]: 'To Do',
  [TaskStatus.InProgress]: 'In Progress',
  [TaskStatus.Done]: 'Done',
  [TaskStatus.Cancelled]: 'Cancelled',
};

export const TaskStatusColor: Record<TaskStatus, string> = {
  [TaskStatus.Backlog]: '#6B7280',
  [TaskStatus.Todo]: '#6366F1',
  [TaskStatus.InProgress]: '#F59E0B',
  [TaskStatus.Done]: '#22C55E',
  [TaskStatus.Cancelled]: '#9CA3AF',
};

export const TaskPriorityLabel: Record<TaskPriority, string> = {
  [TaskPriority.None]: 'None',
  [TaskPriority.Low]: 'Low',
  [TaskPriority.Medium]: 'Medium',
  [TaskPriority.High]: 'High',
};

export const PriorityColor: Record<TaskPriority, string> = {
  [TaskPriority.None]: '#9CA3AF',
  [TaskPriority.Low]: '#3B82F6',
  [TaskPriority.Medium]: '#F59E0B',
  [TaskPriority.High]: '#EF4444',
};

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  userName: string;
  password: string;
  confirmPassword: string;
  organizationName: string;
}

export interface RegisterPendingResponse {
  message: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
  refreshToken: string | null;
  refreshTokenExpiresAt: string | null;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface ResendVerificationRequest {
  email: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ResetPasswordResponse {
  message: string;
}

export interface RefreshSessionRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface GetSessionsRequest {
  refreshToken: string;
}

export interface UserSessionItemDto {
  id: string;
  deviceInfo: string | null;
  ipAddress: string | null;
  createdAt: string;
  expiresAt: string;
  isCurrent: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  refreshToken: string;
}

export interface ChangePasswordResponse {
  message: string;
}

export interface UpdateProfileRequest {
  userName?: string;
  displayName?: string;
}

export interface UserProfileResponse {
  id: string;
  email: string;
  userName: string;
  roles: string[] | null;
  role: string | null;
  organizationId: string;
  organizationName: string | null;
  organizationJoinCode: string | null;
  displayName: string | null;
  avatarUrl: string | null;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  from: number;
  to: number;
}

export interface ProjectDto {
  id: string;
  name: string;
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateProjectCommand {
  name: string;
  description?: string;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
}

export type ProjectDtoPagedResultDto = PagedResult<ProjectDto>;

export interface TaskAssigneeDto {
  id: string;
  userName: string;
  displayName: string | null;
}

export interface TagDto {
  id: string;
  name: string;
  color: string;
}

export interface BoardTaskDto {
  id: string;
  title: string;
  priority: TaskPriority;
  dueDateUtc: string | null;
  isOverdue: boolean;
  assignee: TaskAssigneeDto | null;
  tags: TagDto[];
  commentCount: number;
  createdAt: string;
}

export interface BoardColumnDto {
  status: string;
  statusValue: TaskStatus;
  displayName: string;
  color: string;
  taskCount: number;
  tasks: BoardTaskDto[];
}

export interface ProjectBoardResponse {
  projectId: string;
  projectName: string;
  columns: BoardColumnDto[];
}

export interface MoveBoardTaskRequest {
  newStatus: TaskStatus;
}

export interface TaskMilestoneDto {
  id: string;
  name: string;
}

export interface TaskDto {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDateUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  assignee: TaskAssigneeDto | null;
  milestone: TaskMilestoneDto | null;
  isBlocked: boolean;
  blockerCount: number;
  commentCount: number;
  tags: TagDto[];
  checklistTotal: number;
  checklistCompleted: number;
  checklistProgress: number;
  isDeleted: boolean;
  deletedAt: string | null;
  templateId: string | null;
  rowVersion: number;
}

export type TaskDtoPagedResultDto = PagedResult<TaskDto>;

export interface CreateTaskCommand {
  projectId: string;
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  dueDateUtc?: string;
  assigneeId?: string;
  tagIds?: string[];
  milestoneId?: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  status?: TaskStatus;
  priority?: TaskPriority;
  dueDateUtc?: string;
  assigneeId?: string;
  tagIds?: string[];
  milestoneId?: string;
}

export interface AssignTaskRequest {
  assigneeId: string | null;
}

export interface BulkDeleteRequest {
  taskIds: string[];
}

export interface BulkAssignRequest {
  taskIds: string[];
  assigneeId: string | null;
}

export interface BulkTaskFailureDto {
  taskId: string;
  reason: string;
}

export interface BulkTaskOperationResultDto {
  succeeded: number;
  failed: BulkTaskFailureDto[];
}

export interface BulkTaskDeleteResultDto {
  deleted: number;
  notFound: string[];
}

export interface ChecklistItemDto {
  id: string;
  title: string;
  isCompleted: boolean;
  order: number;
  completedAt: string | null;
}

export interface AddChecklistItemRequest {
  title: string;
  insertAfterOrder?: number;
}

export interface UpdateChecklistItemRequest {
  title?: string;
  isCompleted?: boolean;
}

export interface ReorderChecklistRequest {
  orderedIds: string[];
}

export interface CommentDto {
  id: string;
  content: string;
  isEdited: boolean;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
  author: TaskAssigneeDto;
}

export type CommentDtoPagedResultDto = PagedResult<CommentDto>;

export interface CreateCommentRequest {
  content: string;
}

export interface UpdateCommentRequest {
  content: string;
}

export interface ActivityActorDto {
  id: string;
  userName: string;
}

export interface ActivityLogDto {
  id: string;
  action: string;
  actor: ActivityActorDto;
  occurredAt: string;
  metadata: Record<string, unknown> | null;
}

export type ActivityLogDtoPagedResultDto = PagedResult<ActivityLogDto>;

export interface TaskBlockingSummaryDto {
  id: string;
  title: string;
  status: TaskStatus;
}

export interface DependencyDto {
  blockedTaskId: string;
  blockingTask: TaskBlockingSummaryDto;
}

export interface TaskDependenciesResponse {
  blockedBy: DependencyDto[];
  blocking: DependencyDto[];
}

export interface AddTaskDependencyRequest {
  blockingTaskId: string;
}

export interface MilestoneDto {
  id: string;
  projectId: string;
  name: string;
  description: string | null;
  dueDateUtc: string | null;
  taskCount: number;
  completedTaskCount: number;
  progress: number;
  createdAt: string;
}

export interface CreateMilestoneRequest {
  name: string;
  description?: string;
  dueDateUtc?: string;
}

export interface UpdateMilestoneRequest {
  name?: string;
  description?: string;
  dueDateUtc?: string;
}

export interface DashboardVelocityDto {
  completedLast7Days: number;
  completedPrev7Days: number;
  trendPercent: number;
}

export interface TasksByStatusDto {
  status: string;
  count: number;
}

export interface TasksByPriorityDto {
  priority: string;
  count: number;
}

export interface DashboardUpcomingTaskDto {
  id: string;
  title: string;
  projectId: string;
  projectName: string;
  dueDateUtc: string | null;
  priority: TaskPriority;
  assignee: TaskAssigneeDto | null;
}

export interface DashboardRecentActivityDto {
  action: string;
  actorName: string;
  occurredAt: string;
  entityTitle: string;
}

export interface DashboardProjectSummaryDto {
  projectId: string;
  projectName: string;
  totalTasks: number;
  completedTasks: number;
  overdueCount: number;
  progress: number;
}

export interface DashboardTopContributorDto {
  userId: string;
  userName: string;
  displayName: string | null;
  tasksCompleted: number;
}

export interface DashboardStatsDto {
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  inProgressTasks: number;
  cancelledTasks: number;
  tasksByStatus: TasksByStatusDto[];
  tasksByPriority: TasksByPriorityDto[];
  overdueCount: number;
  dueSoonCount: number;
  completionRate: number;
  velocity: DashboardVelocityDto;
  upcomingTasks: DashboardUpcomingTaskDto[];
  recentActivity: DashboardRecentActivityDto[];
  projectSummaries: DashboardProjectSummaryDto[];
  topContributors: DashboardTopContributorDto[];
}

export interface MyTasksSummaryDto {
  total: number;
  completed: number;
  overdue: number;
  dueSoon: number;
}

export interface DashboardMyStatsDto {
  myTasks: MyTasksSummaryDto;
  myTasksByStatus: TasksByStatusDto[];
  myTasksByPriority: TasksByPriorityDto[];
  myRecentActivity: DashboardRecentActivityDto[];
}

export interface NotificationDto {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
  entityType: string | null;
  entityId: string | null;
}

export type NotificationDtoPagedResultDto = PagedResult<NotificationDto>;

export interface UnreadCountResponse {
  count: number;
}

export interface ReadAllResponse {
  updatedCount: number;
}

export interface MyWorkspaceResponse {
  id: string;
  name: string;
  memberCount: number;
  joinCode: string;
  createdAt: string;
  currentUserRole: string;
}

export interface WorkspaceMemberRowDto {
  id: string;
  userName: string;
  displayName: string | null;
  email: string;
  role: string;
  joinedAt: string;
}

export interface WorkspaceMembersPageResponse {
  items: WorkspaceMemberRowDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface WorkspaceInviteRowDto {
  id: string;
  email: string;
  role: string;
  sentAt: string;
  expiresAt: string;
  resendCount: number;
  status: string;
}

export interface InviteMemberRequest {
  email: string;
  role: 'Admin' | 'Member';
}

export interface ResendInviteRequest {
  email: string;
}

export interface AcceptInviteRequest {
  token: string;
}

export interface UpdateMemberRoleRequest {
  role: 'Admin' | 'Member';
}

export interface RegenerateJoinCodeResponse {
  joinCode: string;
}

export interface UpdateWorkspaceRequest {
  name: string;
}

export interface UpdateWorkspaceResponse {
  message: string;
}

export interface CreateWorkspaceRequest {
  name: string;
}

export interface JoinWorkspaceRequest {
  code: string;
}

export interface CreateWorkspaceTagRequest {
  name: string;
  color: string;
}

export interface UpdateWorkspaceTagRequest {
  name?: string;
  color?: string;
}

export interface WebhookDto {
  id: string;
  url: string;
  events: string[];
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateWorkspaceWebhookRequest {
  url: string;
  events: string[];
  secret: string;
}

export interface UpdateWorkspaceWebhookRequest {
  url?: string;
  events?: string[];
  isActive?: boolean;
  secret?: string;
}

export interface WebhookDeliveryLogDto {
  id: string;
  eventType: string;
  status: string;
  attemptCount: number;
  lastAttemptAt: string | null;
  responseStatus: number | null;
}

export type WebhookDeliveryLogDtoPagedResultDto = PagedResult<WebhookDeliveryLogDto>;

export interface WebhookTestResponse {
  delivered: boolean;
  responseStatus: number | null;
}

export interface TaskTemplateChecklistItemDto {
  id: string;
  title: string;
  order: number;
}

export interface TaskTemplateCreatedByDto {
  id: string;
  userName: string;
}

export interface TaskTemplateDto {
  id: string;
  name: string;
  description: string | null;
  defaultTitle: string;
  defaultDescription: string | null;
  defaultPriority: TaskPriority;
  defaultDueDaysFromNow: number | null;
  checklistItems: TaskTemplateChecklistItemDto[];
  tags: TagDto[];
  createdBy: TaskTemplateCreatedByDto;
  createdAtUtc: string;
}

export interface CreateTaskTemplateRequest {
  name: string;
  description?: string;
  defaultTitle: string;
  defaultDescription?: string;
  defaultPriority: TaskPriority;
  defaultDueDaysFromNow?: number;
  checklistItems?: string[];
  tagIds?: string[];
}

export interface CreateTaskFromTemplateRequest {
  templateId: string;
  projectId: string;
  overrides?: {
    title?: string;
    description?: string;
    priority?: TaskPriority;
    dueDateUtc?: string;
    assigneeId?: string;
  };
}

export interface SearchHitDto {
  id: string;
  type: 'task' | 'project' | 'comment';
  title: string;
  snippet: string;
  score: number;
  metadata: unknown;
}

export interface SearchResultDto {
  query: string;
  totalResults: number;
  tasks: SearchHitDto[];
  projects: SearchHitDto[];
  comments: SearchHitDto[];
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
}

export interface ValidationProblemDetails extends ProblemDetails {
  errors?: Record<string, string[]>;
}
