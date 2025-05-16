using Microsoft.EntityFrameworkCore;

namespace OutboxProcessor.Database;

internal sealed class MigrationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MigrationHostedService> _logger;
    private readonly string _schema;

    public MigrationHostedService(IServiceScopeFactory scopeFactory, ILogger<MigrationHostedService> logger,
        DatabaseOptions options)
    {
        this._scopeFactory = scopeFactory;
        this._logger = logger;
        this._schema = options.Schema;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return ExecuteMigrationsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ExecuteMigrationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var command = $"CREATE SCHEMA IF NOT EXISTS {_schema}";
            await context.Database.ExecuteSqlRawAsync(command, cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}