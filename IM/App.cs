using IM.Core.Interfaces;

namespace IM;

public class App : IHostedService
{
    private readonly ILogger<App> _logger;
    private readonly IEnumerable<IInit> _initializers;
    private readonly IManager _manager;
    
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public App(ILogger<App> logger,
        IEnumerable<IInit> initializers,
        IManager manager)
    {
        _logger = logger;
        _initializers = initializers;
        _manager = manager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitServices(cancellationToken);
        
        _ = Task.Factory.StartNew(Loop, TaskCreationOptions.LongRunning);
    }

    private async Task Loop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await _manager.Process();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while processing emails");
            }
            finally
            {
                _logger.LogInformation("Sleeping for 5 minutes");
                await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
            }
        }
    }
    
    private async Task InitServices(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InitServices");
        
        foreach (var initializer in _initializers)
        {
            _logger.LogInformation("Initializing {Name}", initializer.GetType().Name);
            
            await initializer.Init(cancellationToken);
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        
        return Task.CompletedTask;
    }
}