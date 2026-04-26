using System.Text.Json.Nodes;
using MediatR;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.Activity;

public sealed record ActivityActorDto(Guid Id, string UserName);

public sealed record ActivityLogDto(
    Guid Id,
    string Action,
    ActivityActorDto Actor,
    DateTime OccurredAt,
    IReadOnlyDictionary<string, object?>? Metadata);

public sealed record GetTaskActivityQuery(Guid TaskId, int Page, int PageSize) : IRequest<PagedResultDto<ActivityLogDto>?>;

public sealed record GetProjectActivityQuery(Guid ProjectId, int Page, int PageSize) : IRequest<PagedResultDto<ActivityLogDto>?>;
