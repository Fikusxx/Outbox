using Microsoft.Extensions.Options;

namespace OutboxProcessor.Processor;

internal static class DependencyInjection
{
    public static IServiceCollection AddProcessor(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddProcessorOptions(configuration);
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<Worker>();

        return services;
    }

    private static void AddProcessorOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProcessorOptions>(configuration.GetSection(nameof(ProcessorOptions)))
            .AddOptionsWithValidateOnStart<ProcessorOptions>()
            .ValidateDataAnnotations();

        services.AddSingleton<ProcessorOptions>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ProcessorOptions>>();
            return options.Value;
        });
    }
}