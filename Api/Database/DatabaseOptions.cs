using System.ComponentModel.DataAnnotations;

namespace OutboxProcessor.Database;

internal sealed class DatabaseOptions
{
    [Required] public required string Connection { get; init; }
    
    [Required] public required string Schema { get; init; }

    [Required] [Range(1, int.MaxValue)] public required int MaxRetryCount { get; init; }

    [Required] [Range(10, 10000)] public required int MaxRetryDelayMs { get; init; }
}