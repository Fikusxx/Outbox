using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace OutboxProcessor.Database;

internal static class DependencyInjection
{
    public static IServiceCollection AddDatabase(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabaseOptions(configuration);

        var dbOptions = configuration.GetSection(nameof(DatabaseOptions)).Get<DatabaseOptions>();

        services.AddSingleton(_ =>
        {
            return new NpgsqlDataSourceBuilder(dbOptions!.Connection).Build();
        });

        services.AddDbContext<AppDbContext>((sp, builder) =>
        {
            var databaseOptions = sp.GetRequiredService<DatabaseOptions>();

            builder.UseNpgsql(databaseOptions.Connection,
                options =>
                {
                    options.MigrationsHistoryTable("__EFMigrationsHistory", databaseOptions.Schema);
                    options.EnableRetryOnFailure(maxRetryCount: databaseOptions.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromMilliseconds(databaseOptions.MaxRetryDelayMs),
                        null);
                });
        });

        services.AddHostedService<MigrationHostedService>();

        return services;
    }

    private static void AddDatabaseOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(nameof(DatabaseOptions)))
            .AddOptionsWithValidateOnStart<DatabaseOptions>()
            .ValidateDataAnnotations();

        services.AddSingleton<DatabaseOptions>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();
            return options.Value;
        });
    }
}