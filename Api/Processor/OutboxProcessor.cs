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
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly int _batchSize;

    public OutboxProcessor(NpgsqlDataSource source, ITopicProducer<long, Model> producer,
        ProcessorOptions options, ILogger<OutboxProcessor> logger)
    {
        this._source = source;
        this._producer = producer;
        this._logger = logger;
        this._batchSize = options.BatchSize;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await using var connection = await _source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        List<Outbox> messages = [];

        try
        {
            messages = (await connection.QueryAsync<Outbox>(
                """
                select * from outbox
                where time < @Epoch
                limit @BatchSize
                for update skip locked
                """,
                new { Epoch = epoch, BatchSize = _batchSize },
                transaction: transaction
            )).AsList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error fetching messages.");
        }

        if (messages.Count == 0)
        {
            _logger.LogInformation("No messages fetched.");
            return;
        }

        var tasks = new List<Task>(capacity: messages.Count);

        foreach (var message in messages)
        {
            var deserialized = JsonSerializer.Deserialize<Model>(message.Content);
            tasks.Add(_producer.Produce(deserialized!.Id, deserialized, ct));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("Published messages.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Publishing error.");
        }

        var idsToDelete = messages.Select(x => x.Id).ToArray();
        // var idsList = messages
        //     .Select(x => string.Join(",", $"'{x.Id}'"))
        //     .ToArray();
        
        var idsList = string.Join(",", idsToDelete.Select(id => $"'{id}'"));
        
        var sql = $"""
                   delete from outbox
                   where id in ({idsList})
                   """;
        
        try
        {
            await connection.ExecuteAsync(sql, transaction: transaction);
            _logger.LogInformation("Deleted messages.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Deleting records error.");
        }

        await transaction.CommitAsync(ct);
    }
}