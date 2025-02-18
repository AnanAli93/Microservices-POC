using Consul;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Net;

public static class ConsulRegistration
{
    public static IServiceCollection AddConsulServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
        {
            var address = configuration["CONSUL_URL"];
            consulConfig.Address = new Uri(address);
        }));

        return services;
    }

    public static IApplicationBuilder RegisterWithConsul(this IApplicationBuilder app, IHostApplicationLifetime lifetime, IConfiguration configuration)
    {
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
        var loggingFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggingFactory.CreateLogger<IApplicationBuilder>();

        // Get the container's IP address
        var hostName = Dns.GetHostName();
        var ipAddresses = Dns.GetHostAddresses(hostName);
        var ipAddress = ipAddresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

        if (ipAddress == null)
        {
            logger.LogError("Unable to find container IP address");
            return app;
        }

        var serviceId = configuration["SERVICE_ID"];
        var serviceName = configuration["SERVICE_NAME"];
        var servicePort = int.Parse(configuration["SERVICE_PORT"]);

        // Register service with consul
        var registration = new AgentServiceRegistration()
        {
            ID = serviceId ?? $"{serviceName}-{ipAddress}:{servicePort}",
            Name = serviceName,
            Address = ipAddress.ToString(),
            Port = servicePort,
            //Check = new AgentServiceCheck()
            //{
            //    HTTP = $"http://{ipAddress}:{servicePort}/health",
            //    Interval = TimeSpan.FromSeconds(10)
            //}
        };

        logger.LogInformation($"Registering service {registration.Name} with Consul");
        consulClient.Agent.ServiceDeregister(registration.ID).Wait();
        consulClient.Agent.ServiceRegister(registration).Wait();

        lifetime.ApplicationStopping.Register(() => {
            logger.LogInformation($"Deregistering service {registration.Name} from Consul");
            consulClient.Agent.ServiceDeregister(registration.ID).Wait();
        });

        return app;
    }
}