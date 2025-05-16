using System.ComponentModel.DataAnnotations;

namespace OutboxProcessor.Processor;

internal sealed class ProcessorOptions
{
    [Required] [Range(1, int.MaxValue)] public required int BatchSize { get; init; }

    [Required] [Range(1, int.MaxValue)] public required int MaxConcurrency { get; init; }
    [Required] [Range(0, int.MaxValue)] public required int DelayMs { get; init; }
}