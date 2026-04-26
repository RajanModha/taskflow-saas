using AutoMapper;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Mapping;

public sealed class TaskFlowMappingProfile : Profile
{
    public TaskFlowMappingProfile()
    {
        CreateMap<Project, ProjectDto>();
    }
}

