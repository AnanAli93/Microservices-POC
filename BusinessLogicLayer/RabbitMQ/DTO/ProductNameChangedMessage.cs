namespace BusinessLogicLayer.RabbitMQ.DTO;
public record ProductNameChangedMessage(Guid ProductId, string NewName)
{
}

