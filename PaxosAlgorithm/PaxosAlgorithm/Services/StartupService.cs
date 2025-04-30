using Microsoft.Extensions.Options;
using PaxosAlgorithm.Models;

namespace PaxosAlgorithm.Services;

public class StartupService : IHostedService
{
    private readonly Node _node;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StartupService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public StartupService(
        Node node,
        IHttpClientFactory httpClientFactory,
        ILogger<StartupService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _node = node;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _appLifetime = appLifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                int delayMs = Random.Shared.Next(1000, 2000);
                Console.WriteLine($"{_node.NodeURL} will initialize after {delayMs} ms delay");

                try
                {
                    await Task.Delay(delayMs, cancellationToken);

                    var client = _httpClientFactory.CreateClient();
                    string selfUrl = $"http://{_node.NodeURL}/Paxos/initialize/{delayMs}";

                    Console.WriteLine($"Node {_node.NodeId} calling self-initialize at {selfUrl}");
                    var response = await client.GetAsync(selfUrl, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Node {_node.NodeId} initialization failed: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during node {_node.NodeId} initialization");
                }
            });
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Node {_node.NodeId} stopping");
        return Task.CompletedTask;
    }
}
