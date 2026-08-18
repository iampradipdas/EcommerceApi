namespace EcommerceApi.RabbitMQ
{
    public interface IRabbitMqService
    {
        Task PublishAsync<T>(string routingKey, string UniqueId, T message, string exchange = "");
    }
}
