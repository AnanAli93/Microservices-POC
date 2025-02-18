using eCommerce.OrdersMicroservice.DataAccessLayer;
using eCommerce.OrdersMicroservice.BusinessLogicLayer;
using FluentValidation.AspNetCore;
using eCommerce.OrdersMicroservice.API.Middleware;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;
using BusinessLogicLayer.Policies;
using BusinessLogicLayer.HttpClients;
using Consul;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using OrderMicroservice.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

//Add DAL and BLL services
builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddBusinessLogicLayer(builder.Configuration);

builder.Services.AddControllers();

//FluentValidations
builder.Services.AddFluentValidationAutoValidation();

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:4200")
      .AllowAnyMethod()
      .AllowAnyHeader();
    });
});

builder.Services.AddTransient<IPollyPolicies, PollyPolicies>();
builder.Services.AddTransient<IUserMicroservicePolicies, UserMicroservicePolicy>();
builder.Services.AddTransient<IProductsMicroservicePolicies, ProductsMicroservicePolicies>();


builder.Services.AddHttpClient<UserMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["UserMicroserviceName"]}:{builder.Configuration["UserMicroservicePort"]}");
}).AddPolicyHandler(builder.Services.BuildServiceProvider().GetRequiredService<IUserMicroservicePolicies>().GetCombinedPolicy());
  //.AddPolicyHandler(
  // builder.Services.BuildServiceProvider().GetRequiredService<IUserMicroservicePolicies>().GetRetryPolicy())
  //.AddPolicyHandler(
  // builder.Services.BuildServiceProvider().GetRequiredService<IUserMicroservicePolicies>().GetCircuitBreakerPolicy())
  //.AddPolicyHandler(
  // builder.Services.BuildServiceProvider().GetRequiredService<IUserMicroservicePolicies>().GetTimeoutPolicy())
  // ;



builder.Services.AddHttpClient<ProductsMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["ProductsMicroserviceName"]}:{builder.Configuration["ProductsMicroservicePort"]}");
}).AddPolicyHandler(
   builder.Services.BuildServiceProvider().GetRequiredService<IProductsMicroservicePolicies>().GetFallbackPolicy())
  .AddPolicyHandler(
   builder.Services.BuildServiceProvider().GetRequiredService<IProductsMicroservicePolicies>().GetBulkheadIsolationPolicy())
  ;
// Add services to the container.
builder.Services.AddHealthChecks(); // Add health check services (needed for Consul health check)

//builder.Services.AddOpenTelemetry()
//    .WithTracing(tracerProviderBuilder =>
//    {
//        tracerProviderBuilder
//            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("orders-microservice"))
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
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("orders-microservice"))
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

builder.Services.AddConsulServices(builder.Configuration);

var app = builder.Build();


//// Register service with Consul
//var consulClient = new ConsulClient(config =>
//{
//    config.Address = new Uri("http://consul:8500"); // Consul address (Docker or localhost)
//});

//var registration = new AgentServiceRegistration()
//{
//    ID = "orders-microservice", // Unique service ID
//    Name = "orders-microservice", // Service name
//    Address = "orders-microservice", // Hostname (typically Docker container name)
//    Port = 8080, // Port where the service is running
//    Tags = new[] { "api", "orders" },
//    Check = new AgentServiceCheck
//    {
//        HTTP = "http://orders-microservice:8080/health", // Health check endpoint
//        Interval = TimeSpan.FromSeconds(10) // Health check interval
//    }
//};

//// Register with Consul
//await consulClient.Agent.ServiceRegister(registration);

app.RegisterWithConsul(app.Lifetime, app.Configuration);

// Configure health check endpoint
app.MapHealthChecks("/health");

app.UseMiddleware<TraceMiddleware>();
app.UseExceptionHandlingMiddleware();
app.UseRouting();

//Cors
app.UseCors();

//Swagger
app.UseSwagger();
app.UseSwaggerUI();

//Auth
//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

//Endpoints
app.MapControllers();


app.Run();
