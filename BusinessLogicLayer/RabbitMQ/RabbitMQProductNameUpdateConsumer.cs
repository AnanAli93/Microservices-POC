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

public class RabbitMQProductNameUpdateConsumer : IDisposable, IRabbitMQProductNameUpdateConsumer
{
    private readonly IConfiguration _configuration;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQProductNameUpdateConsumer> _logger;
    public RabbitMQProductNameUpdateConsumer(IConfiguration configuration, ILogger<RabbitMQProductNameUpdateConsumer> logger)
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
        string routingKey = "product.update.name";
        string queueName = "orders.product.update.name.queue";//_configuration["RabbitMQ_Products_Queue"]!;
        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct);
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);
        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (sender, args) =>
        {
            byte[] data = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(data);
            ProductNameChangedMessage? dto = JsonSerializer.Deserialize<ProductNameChangedMessage>(message);
            _logger.LogInformation($"product name has been updated to {dto.NewName} for product id {dto.ProductId}");
        };
        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

    public void ConsumeFanout()
    {
        //string routingKey = "product.update.name";
        string queueName = "orders.product.update.name.queue";

        //Create exchange
        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);

        //Create message queue
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null); //x-message-ttl | x-max-length | x-expired 

        //Bind the message to exchange
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty);


        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (sender, args) =>
        {
            byte[] body = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            if (message != null)
            {
                ProductNameChangedMessage? productNameUpdateMessage = JsonSerializer.Deserialize<ProductNameChangedMessage>(message);

                if (productNameUpdateMessage != null)
                {
                    _logger.LogInformation($"Product name updated: {productNameUpdateMessage.ProductId}, New name: {productNameUpdateMessage.NewName}");
                }
            }
        };
        _channel.BasicConsume(queue: queueName, consumer: consumer, autoAck: true);
    }

    public void ConsumeTopic()
    {
        string routingKey = "product.update.*";
        string queueName = "orders.product.update.name.queue";

        //Create exchange
        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

        //Create message queue
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null); //x-message-ttl | x-max-length | x-expired 

        //Bind the message to exchange
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);


        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (sender, args) =>
        {
            byte[] body = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            if (message != null)
            {
                ProductNameChangedMessage? productNameUpdateMessage = JsonSerializer.Deserialize<ProductNameChangedMessage>(message);

                if (productNameUpdateMessage != null)
                {
                    _logger.LogInformation($"Product name updated: {productNameUpdateMessage.ProductId}, New name: {productNameUpdateMessage.NewName}");
                }
            }
        };

        _channel.BasicConsume(queue: queueName, consumer: consumer, autoAck: true);
    }

    public void ConsumeHeaders()
    {
        //string routingKey = "product.update.*";
        var headers = new Dictionary<string, object>()
      {
        { "x-match", "all" },
        { "event", "product.update" },
        { "field", "name" },
        { "RowCount", 1 }
      };

        string queueName = "orders.product.update.name.queue";

        //Create exchange
        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;
        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Headers, durable: true);

        //Create message queue
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null); //x-message-ttl | x-max-length | x-expired 

        //Bind the message to exchange
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty, arguments: headers);


        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (sender, args) =>
        {
            byte[] body = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            if (message != null)
            {
                ProductNameChangedMessage? productNameUpdateMessage = JsonSerializer.Deserialize<ProductNameChangedMessage>(message);

                if (productNameUpdateMessage != null)
                {
                    _logger.LogInformation($"Product name updated: {productNameUpdateMessage.ProductId}, New name: {productNameUpdateMessage.NewName}");
                }
            }
        };

        _channel.BasicConsume(queue: queueName, consumer: consumer, autoAck: true);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
