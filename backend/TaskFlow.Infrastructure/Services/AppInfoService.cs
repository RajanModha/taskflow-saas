using TaskFlow.Application.Abstractions;

namespace TaskFlow.Infrastructure.Services;

public sealed class AppInfoService : IAppInfo
{
    public string ApplicationName => "TaskFlow";
}
