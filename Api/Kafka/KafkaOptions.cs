using System.ComponentModel.DataAnnotations;
using Confluent.Kafka;

namespace OutboxProcessor.Kafka;

internal sealed class KafkaOptions
{
    [Required] public required ClientConfig ClientConfig { get; init; }
    [Required] public required ProducerOptions ProducerOptions { get; init; }
}

internal sealed class ProducerOptions
{
    [Required] public required string Topic { get; init; }
    [Required] public required ProducerConfig ProducerConfig { get; init; }
}