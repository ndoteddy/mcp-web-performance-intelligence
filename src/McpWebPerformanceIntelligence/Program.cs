using McpWebPerformanceIntelligence.Configuration;
using McpWebPerformanceIntelligence.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// The MCP stdio transport uses stdout exclusively for JSON-RPC frames, so all
// logging must go to stderr or it will corrupt the protocol stream.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddOptions<PerformanceOptions>()
    .Bind(builder.Configuration.GetSection(PerformanceOptions.SectionName))
    .PostConfigure(options =>
    {
        options.PageSpeedApiKey =
            Environment.GetEnvironmentVariable("PAGESPEED_API_KEY") ?? options.PageSpeedApiKey;
        options.DatabasePath =
            Environment.GetEnvironmentVariable("DB_FILE") ?? options.DatabasePath;
    });

builder.Services.AddHttpClient<IPerformanceAnalyzer, PerformanceAnalyzer>();
builder.Services.AddSingleton<IPerformanceRepository, SqlitePerformanceRepository>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

var repository = host.Services.GetRequiredService<IPerformanceRepository>();
await repository.InitializeAsync();

await host.RunAsync();
