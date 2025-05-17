using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OutboxProcessor.Database;
using OutboxProcessor.Kafka;
using OutboxProcessor.Processor;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddProcessor(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("insert", async ([FromServices] AppDbContext db) =>
{
    var outbox = new Outbox
    {
        Id = 1,
        Content = JsonSerializer.Serialize(new Model { Id = 1, Content = "HELLO WORLD", Time = DateTimeOffset.UtcNow }),
        Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };

    db.Add(outbox);
    await db.SaveChangesAsync();

    return Results.Ok();
});

app.MapGet("insert-many", async ([FromServices] AppDbContext db) =>
{
    var toAdd = new List<Outbox>(capacity: 50_000);
    
    for (long i = 1; i < 50_000; i++)
    {
        var outbox = new Outbox
        {
            Id = i,
            Content = JsonSerializer.Serialize(new Model { Id = i, Content = "HELLO WORLD", Time = DateTimeOffset.UtcNow }),
            Time = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds()
        };
        toAdd.Add(outbox);
    }
    
    db.AddRange(toAdd);
    await db.SaveChangesAsync();

    return Results.Ok();
});

app.MapGet("select-ef", async ([FromServices] AppDbContext db) => Results.Ok(await db.Outbox.ToListAsync()));

app.MapGet("select-dapper", async ([FromServices] NpgsqlDataSource source) =>
{
    var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    const int batchSize = 100;
    await using var connection = await source.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();

    var messages = (await connection.QueryAsync<Outbox>(
        """
        select * from outbox
        where time < @epoch
        limit @batchSize
        """,
        new { epoch, batchSize },
        transaction: transaction
    )).AsList();

    return Results.Ok(messages);
});

await app.RunAsync();