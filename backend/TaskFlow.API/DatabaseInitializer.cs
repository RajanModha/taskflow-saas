using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.API;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<TaskFlowDbContext>();
        await db.Database.MigrateAsync();
        await IdentityDataSeeder.SeedAsync(services);
    }
}
