using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace CMS.Common.Messaging
{
    /// <summary>
    /// Base class for RabbitMQ message subscribers
    /// Runs as a background service
    /// </summary>
    public abstract class MessageSubscriber : BackgroundService
    {
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly string _connectionString;
        private readonly string _exchangeName;

        protected MessageSubscriber(string connectionString, string exchangeName)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int retryCount = 0;
            const int maxRetries = 5;
            
            while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"🔌 RabbitMQ: Attempting to connect (Attempt {retryCount + 1}/{maxRetries})...");
                    
                    // Setup Connection
                    var factory = new ConnectionFactory
                    {
                        Uri = new Uri(_connectionString),
                        AutomaticRecoveryEnabled = true,
                        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                        RequestedConnectionTimeout = TimeSpan.FromSeconds(30)
                    };

                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    
                    Console.WriteLine($"✅ RabbitMQ: Connected successfully to {_exchangeName}");

                    // Setup Exchange and Queue
                    await _channel.ExchangeDeclareAsync(
                        exchange: _exchangeName, 
                        type: ExchangeType.Fanout,
                        durable: true,
                        autoDelete: false,
                        cancellationToken: stoppingToken);

                    var queue = await _channel.QueueDeclareAsync(cancellationToken: stoppingToken);
                    await _channel.QueueBindAsync(
                        queue: queue.QueueName, 
                        exchange: _exchangeName, 
                        routingKey: string.Empty, 
                        cancellationToken: stoppingToken);
                    
                    Console.WriteLine($"✅ RabbitMQ: Queue '{queue.QueueName}' bound to exchange '{_exchangeName}'");

                    // Setup Consumer
                    var consumer = new AsyncEventingBasicConsumer(_channel);

                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"📩 RabbitMQ: Received from {_exchangeName}: {message}");

                        // Process the message in derived class
                        await ProcessMessageAsync(message);
                    };

                    await _channel.BasicConsumeAsync(
                        queue: queue.QueueName, 
                        autoAck: true, 
                        consumer: consumer, 
                        cancellationToken: stoppingToken);
                    
                    Console.WriteLine($"✅ RabbitMQ: Listening for messages on exchange '{_exchangeName}'...");

                    // Keep the task alive
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                    break; // Exit retry loop on success
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("🛑 RabbitMQ: Service is shutting down...");
                    throw; // Rethrow to stop the service
                }
                catch (Exception ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff: 2s, 4s, 8s, 16s, 32s
                    Console.WriteLine($"⚠️ RabbitMQ: Connection failed - {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine($"🔄 RabbitMQ: Retrying in {delay.TotalSeconds} seconds...");
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ RabbitMQ: Fatal error after {maxRetries} attempts - {ex.GetType().Name}: {ex.Message}");
                    throw; // Final failure, rethrow to stop the service
                }
            }
        }

        /// <summary>
        /// Override this method to process received messages
        /// </summary>
        protected abstract Task ProcessMessageAsync(string message);

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
