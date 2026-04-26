namespace TaskFlow.Application.Activity;

public static class ActivityActions
{
    public const string TaskCreated = "task.created";

    public const string TaskStatusChanged = "task.status_changed";

    public const string TaskPriorityChanged = "task.priority_changed";

    public const string TaskAssigned = "task.assigned";

    public const string TaskUnassigned = "task.unassigned";

    public const string TaskDueDateChanged = "task.due_date_changed";

    public const string TaskCommented = "task.commented";

    public const string TaskDeleted = "task.deleted";

    public const string TaskRestored = "task.restored";
    public const string TaskBulkUpdated = "task.bulk_updated";

    public const string TaskTagAdded = "task.tag_added";

    public const string TaskChecklistItemCompleted = "task.checklist_item_completed";

    public const string ProjectCreated = "project.created";

    public const string ProjectUpdated = "project.updated";

    public const string ProjectDeleted = "project.deleted";
}
