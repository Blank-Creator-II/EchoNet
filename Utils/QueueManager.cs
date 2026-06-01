using EchoNet.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EchoNet.Utils;

public class QueueManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueueManager> _logger;

    public QueueManager(IServiceProvider serviceProvider, ILogger<QueueManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
}