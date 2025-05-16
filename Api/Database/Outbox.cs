namespace OutboxProcessor.Database;

public sealed record Outbox
{
    public required long Id { get; init; }
    public required string Content { get; init; }
    public required long Time { get; init; }
}

public sealed record Model
{
    public required long Id { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Time { get; init; }
}