var builder = WebApplication.CreateBuilder(args);
// Add YARP services
builder.Services.AddReverseProxy()
       .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();
// Enable YARP reverse proxy
app.MapReverseProxy();

app.Run();
