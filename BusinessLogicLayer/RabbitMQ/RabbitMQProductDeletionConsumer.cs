using BusinessLogicLayer.RabbitMQ.DTO;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductDeletionConsumer : IDisposable, IRabbitMQProductDeletionConsumer
{
    private readonly IConfiguration _configuration;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQProductDeletionConsumer> _logger;
    public RabbitMQProductDeletionConsumer(IConfiguration configuration, ILogger<RabbitMQProductDeletionConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
        try
        {
            string hostName = _configuration["RabbitMQ_HostName"]!;
            string port = Environment.GetEnvironmentVariable("RabbitMQ_Port")!;
            _logger.LogInformation($"RabbitMQ port: {port}");
            string userName = _configuration["RabbitMQ_UserName"]!;
            string password = _configuration["RabbitMQ_Password"]!;
            string connectionString = $"amqp://{userName}:{password}@{hostName}:5672";
            logger.LogInformation($"Attempting to connect to RabbitMQ with connection string: {connectionString}");

            ConnectionFactory connectionFactory = new ConnectionFactory()
            {
                HostName = hostName,
                Port = int.Parse(port),
                UserName = userName,
                Password = password
            };
            // Asynchronously create the connection
            _connection = connectionFactory.CreateConnection();  // Blocking for async method
            _channel = _connection.CreateModel();
            logger.LogInformation("Successfully connected to RabbitMQ");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }

    }


    public void Consume()
    {
        string routingKey = "product.delete";
        string queueName = "orders.product.delete.queue";//_configuration["RabbitMQ_Products_Queue"]!;
        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);
        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (sender, args) =>
        {
            byte[] data = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(data);
            ProductDeletionMessage? dto = JsonSerializer.Deserialize<ProductDeletionMessage>(message)!;
            _logger.LogInformation($"product with name {dto.ProductName}  has been deleted for product id {dto.ProductId}");
        };
        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
