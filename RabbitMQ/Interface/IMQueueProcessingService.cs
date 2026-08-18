namespace EcommerceApi.RabbitMQ.Interface
{
    public interface IMQueueProcessingService
    {
        Task ProcessQueueAsync(string queueName);
    }
}
