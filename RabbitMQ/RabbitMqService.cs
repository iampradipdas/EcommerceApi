using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace EcommerceApi.RabbitMQ
{
    public class RabbitMqService : IRabbitMqService
    {
        private readonly IConfiguration _configaration;
        private readonly ConnectionFactory _factory;
        public RabbitMqService(IConfiguration configaration)
        {
            _configaration = configaration;
            _factory = new ConnectionFactory()
            {
                HostName = _configaration["RabbitMQConnection:Host"],
                Port = int.Parse(_configaration["RabbitMQConnection:Port"]),
                UserName = _configaration["RabbitMQConnection:Username"],
                Password = _configaration["RabbitMQConnection:Password"]
            };
        }
        public async Task PublishAsync<T>(string routingKey, string UniqueId, T message, string exchange = "")
        {
            using (var connection = await _factory.CreateConnectionAsync())
            using (var channel = await connection.CreateChannelAsync())
            {
                await channel.QueueDeclareAsync(queue: routingKey,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
                );

                var prop = new BasicProperties()
                {
                    MessageId = UniqueId,
                    ReplyTo = $"{routingKey}_ack",
                    DeliveryMode = DeliveryModes.Persistent
                };

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: prop, body: body
                );
            }

        }
    }
}
