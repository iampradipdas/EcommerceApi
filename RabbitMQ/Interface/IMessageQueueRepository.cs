using EcommerceApi.Dal.Entities;

namespace EcommerceApi.RabbitMQ.Interface
{
    public interface IMessageQueueRepository
    {
        Task<IEnumerable<PublishLog>> GetRecordsForQueueAsync(string queueName);
    }
}
