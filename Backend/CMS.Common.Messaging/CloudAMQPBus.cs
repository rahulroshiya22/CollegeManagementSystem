using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CMS.Common.Messaging
{
    /// <summary>
    /// CloudAMQP implementation of message bus
    /// Supports cloud-hosted RabbitMQ instances
    /// </summary>
    public class CloudAMQPBus : IMessageBus, IDisposable
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly string _connectionString;

        public CloudAMQPBus(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_connectionString),
                    // CloudAMQP specific settings
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }
        }

        public async Task PublishAsync<T>(T message, string exchangeName) where T : class
        {
            await EnsureConnectionAsync();

            // Declare a Fanout exchange (broadcasts to all listeners)
            await _channel!.ExchangeDeclareAsync(
                exchange: exchangeName, 
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Publish message
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: string.Empty,
                body: body);

            Console.WriteLine($"---> Published to {exchangeName}: {json}");
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
