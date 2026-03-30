using AutoMapper;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;
using DomainTask = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Application.Mapping;

public sealed class TaskFlowMappingProfile : Profile
{
    public TaskFlowMappingProfile()
    {
        CreateMap<Project, ProjectDto>();
        CreateMap<DomainTask, TaskDto>();
    }
}

