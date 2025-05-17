namespace OutboxProcessor.Processor;

internal sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<Worker> _logger;
    private readonly int _maxConcurrency;
    private readonly int _delayMs;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory factory, ProcessorOptions options)
    {
        this._logger = logger;
        this._factory = factory;
        this._maxConcurrency = options.MaxConcurrency;
        this._delayMs = options.DelayMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxConcurrency,
            CancellationToken = stoppingToken
        };
        
        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, _maxConcurrency),
                parallelOptions,
                async (_, ct) => { await ProcessMessages(_delayMs, ct); });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "OOPS");
        }
    }

    private async Task ProcessMessages(int delayMs, CancellationToken ct)
    {
        using var scope = _factory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(delayMs));
        
        while (ct.IsCancellationRequested == false && await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await processor.ExecuteAsync(ct);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Scheduling execution error.");
            }
        }
    }
}