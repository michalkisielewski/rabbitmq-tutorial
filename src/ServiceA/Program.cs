using System.Text;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/publish", async (string message = "Hello RabbitMQ!") =>
{
    // Read RabbitMQ host from an environment variable (default to "localhost")
    var factory = new ConnectionFactory()
    {
        HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"
    };

    // default in newest clients
    factory.AutomaticRecoveryEnabled = true;

    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    // Ensure the queue exists
    await channel.QueueDeclareAsync(
        queue: "test-queue",
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(message));

    await channel.BasicPublishAsync<BasicProperties>(
        exchange: "",
        routingKey: "test-queue",
        mandatory: false,
        basicProperties: null,
        body: body);

    return Results.Ok($"Published message: {message}");
});

app.Run();
