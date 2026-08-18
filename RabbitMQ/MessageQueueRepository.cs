using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.RabbitMQ.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceApi.RabbitMQ
{
    public class MessageQueueRepository : IMessageQueueRepository
    {
        private readonly EcomDbContext _context;
        public MessageQueueRepository(EcomDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<PublishLog>> GetRecordsForQueueAsync(string queueName)
        {
            return await _context.PublishLogs
                .Where(x => x.QueueName == queueName)
                .ToListAsync();
        }
    }
}
