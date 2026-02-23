using RabbitMQ.Client;

Console.WriteLine("=== RabbitMQ Connection Test ===\n");

var connectionString = "amqps://jxlnlvrr:aS15sCekjI13tw8SC4jEEuYi_cIq4ENS@duck.lmq.cloudamqp.com/jxlnlvrr";

var factory = new ConnectionFactory
{
    Uri = new Uri(connectionString),
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
};

try
{
    Console.WriteLine("📡 Attempting to connect to CloudAMQP...");
    var connection = await factory.CreateConnectionAsync();
    
    Console.WriteLine("✅ Connection successful!");
    Console.WriteLine($"   Connection Name: {connection.ClientProvidedName}");
    Console.WriteLine($"   Is Open: {connection.IsOpen}");
    
    Console.WriteLine("\n📡 Creating channel...");
    var channel = await connection.CreateChannelAsync();
    Console.WriteLine("✅ Channel created successfully!");
    
    Console.WriteLine("\n📡 Declaring exchange 'student-enrolled'...");
    await channel.ExchangeDeclareAsync(
        exchange: "student-enrolled",
        type: ExchangeType.Fanout,
        durable: true,
        autoDelete: false);
    Console.WriteLine("✅ Exchange declared successfully!");
    
    Console.WriteLine("\n🧹 Cleaning up...");
    await channel.CloseAsync();
    await connection.CloseAsync();
    
    Console.WriteLine("\n✅✅✅ ALL TESTS PASSED! ✅✅✅");
    Console.WriteLine("\nYour RabbitMQ configuration is working correctly.");
    Console.WriteLine("You can now use RabbitMQ for message publishing and subscribing.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Connection failed!");
    Console.WriteLine($"   Error Type: {ex.GetType().Name}");
    Console.WriteLine($"   Error Message: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
    }
    
    Console.WriteLine("\n🔧 Troubleshooting steps:");
    Console.WriteLine("   1. Check if CloudAMQP instance is active at https://customer.cloudamqp.com/");
    Console.WriteLine("   2. Verify your internet connection");
    Console.WriteLine("   3. Check if firewall is blocking port 5671");
    Console.WriteLine("   4. Verify the connection string is correct");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
