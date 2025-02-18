using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Consul;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add logging
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

//builder.Services.AddOpenTelemetry()
//    .WithTracing(tracerProviderBuilder =>
//    {
//        tracerProviderBuilder
//            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("apigateway"))
//            .AddAspNetCoreInstrumentation()
//            .AddHttpClientInstrumentation()
//            .AddOtlpExporter(opt =>
//            {
//                opt.Endpoint = new Uri("http://jaeger:4317");
//                opt.Protocol = OtlpExportProtocol.Grpc;
//            });
//    });

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("apigateway"))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = httpContext => httpContext.Request.Path != "/health"; // Ignore health check endpoints
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddSqlClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.SetDbStatementForText = true; // Log SQL queries
            })
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri("http://jaeger:4317"); // Use gRPC for better performance
                opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    });

builder.Services.AddOcelot()
                .AddConsul()
                .AddPolly();
builder.Services.AddConsulServices(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

// Add a health check endpoint
app.MapGet("/health", async (HttpContext httpContext) =>
{
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Health check requested");

    try
    {
        // Check Consul connection
        var consulClient = httpContext.RequestServices.GetRequiredService<IConsulClient>();
        var agentInfo = await consulClient.Agent.Self();
        logger.LogInformation($"Consul agent info retrieved: {agentInfo.Response["Config"]["NodeName"]}");

        // You can add more health checks here

        return Results.Ok(new { status = "Healthy", message = "API Gateway is functioning correctly" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check failed");
        return Results.StatusCode(500);
    }
});// Use Ocelot
await app.UseOcelot();

app.RegisterWithConsul(app.Lifetime, app.Configuration);

app.Run();