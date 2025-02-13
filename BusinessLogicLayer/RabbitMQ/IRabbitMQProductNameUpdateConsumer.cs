namespace BusinessLogicLayer.RabbitMQ;

public interface IRabbitMQProductNameUpdateConsumer
{
    void Consume();
    void Dispose();
}