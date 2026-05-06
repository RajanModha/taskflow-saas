namespace TaskFlow.Domain.Repositories;

// Backward-compatible aggregate contract; prefer focused interfaces in new code.
public interface ITaskRepository :
    ITaskExportRepository,
    ITaskReadRepository,
    ITaskWriteRepository,
    ITaskBulkRepository,
    ITaskChecklistRepository,
    ITaskCommentRepository,
    ITaskTagRepository,
    ITaskDependencyRepository
{
}
