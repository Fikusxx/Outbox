using MassTransit;
using Microsoft.Extensions.Options;
using OutboxProcessor.Database;

namespace OutboxProcessor.Kafka;

internal static class DependencyInjection
{
   public static IServiceCollection AddKafka(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddKafkaOptions(configuration);

        services.AddMassTransit(x =>
        {
            x.UsingInMemory();
            using var sp = x.BuildServiceProvider();
            var kafkaOptions = sp.GetRequiredService<KafkaOptions>();
        
            x.AddRider(rider =>
            {
                rider.AddProducer(kafkaOptions.ProducerOptions);
        
                rider.UsingKafka(kafkaOptions.ClientConfig, (_, _) =>
                {
                    
                });
            });
        });

        return services;
    }

    
    private static void AddProducer(this IRiderRegistrationConfigurator cfg,
        ProducerOptions options)
    {
        cfg.AddProducer<long, Model>(options.Topic,
            options.ProducerConfig,
            (_, _) =>
            {
                
            });
    }

    private static void AddKafkaOptions(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(nameof(KafkaOptions)))
            .AddOptionsWithValidateOnStart<KafkaOptions>()
            .ValidateDataAnnotations();
    
        services.AddSingleton<KafkaOptions>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaOptions>>();
            return options.Value;
        });
    }
}