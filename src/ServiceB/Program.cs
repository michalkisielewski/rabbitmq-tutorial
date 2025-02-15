using System.Text;
using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = WebApplication.CreateBuilder(args);

// Register the consumer background service
builder.Services.AddHostedService<RabbitMqConsumerService>();

var app = builder.Build();

app.MapGet("/", () => "RabbitMQ Consumer running...");
app.Run();

public class RabbitMqConsumerService : BackgroundService
{
    private IConnection _connection;
    private IChannel _channel;

    public RabbitMqConsumerService()
    {
        var factory = new ConnectionFactory()
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"
        };

        _connection = factory.CreateConnectionAsync();
        _channel = _connection.CreateModel();

        // Ensure the queue exists (must match the publisher)
        _channel.QueueDeclare(
            queue: "test-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (sender, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"Received message: {message}");
        };

        _channel.BasicConsume(
            queue: "test-queue",
            autoAck: true,
            consumer: consumer);

        // Keep the service running until cancellation is requested
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
