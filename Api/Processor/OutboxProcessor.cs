using System.Text.Json;
using Dapper;
using MassTransit;
using Npgsql;
using OutboxProcessor.Database;

namespace OutboxProcessor.Processor;

internal sealed class OutboxProcessor
{
    private readonly NpgsqlDataSource _source;
    private readonly ITopicProducer<long, Model> _producer;
    private readonly int _batchSize;

    public OutboxProcessor(NpgsqlDataSource source, 
        ITopicProducer<long, Model> producer,
        ProcessorOptions options)
    {
        this._source = source;
        this._producer = producer;
        this._batchSize = options.BatchSize;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var messages = (await connection.QueryAsync<Outbox>(
            """
            select * from outbox
            where time < @Epoch
            limit @BatchSize
            for update skip locked
            """,
            new { Epoch = epoch, BatchSize = _batchSize },
            transaction: transaction
        )).AsList();

        if (messages.Count == 0)
            return;

        var tasks = new List<Task>(capacity: messages.Count);

        foreach (var message in messages)
        {
            var deserialized = JsonSerializer.Deserialize<Model>(message.Content);
            tasks.Add(_producer.Produce(deserialized!.Id, deserialized, ct));
        }

        await Task.WhenAll(tasks);

        var idsList = string.Join(",", messages.Select(x => $"'{x.Id}'"));

        var sql = $"""
                   delete from outbox
                   where id in ({idsList})
                   """;

        await connection.ExecuteAsync(sql, transaction: transaction);
        await transaction.CommitAsync(ct);
    }
}