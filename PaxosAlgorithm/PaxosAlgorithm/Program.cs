using PaxosAlgorithm.Models;
using PaxosAlgorithm.Services;

namespace PaxosAlgorithm;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Logging.ClearProviders();

        builder.Logging.AddConsole();

        builder.Logging.SetMinimumLevel(LogLevel.Error);
        builder.Logging.AddFilter("Microsoft", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.None);
        builder.Logging.AddFilter("System", LogLevel.None);
        
        builder.Services.AddControllers();
        
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddAuthorization();

        builder.Services.AddOpenApi();
        
        var node = new Node
        {
            NodeURL = builder.Configuration["NODE_URL"]!,
            NodeId = int.Parse(builder.Configuration["NODE_ID"]!),
            Clusters = builder.Configuration["CLUSTER_NODES"]?.Split(',').ToList() ?? new List<string>()
        };

        builder.Services.AddSingleton(node);

        builder.Services.AddHttpClient<IPaxosService, PaxosService>();
        //builder.Services.AddHttpClient<StartupService>();
        //builder.Services.AddHostedService<StartupService>();
        builder.Services.AddSingleton<IPaxosService, PaxosService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        //app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}