namespace BusinessLogicLayer.RabbitMQ.DTO;
public record ProductDeletionMessage(Guid ProductId, string ProductName)
{
}
