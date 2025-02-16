using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Register the RabbitMQ consumer as a hosted background service
builder.Services.AddHostedService<RabbitMqConsumerService>();

var app = builder.Build();

app.MapGet("/", () => "RabbitMQ Consumer running...");
app.Run();

public class RabbitMqConsumerService : BackgroundService
{
    private IConnection _connection;
    private IChannel _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Read RabbitMQ host from an environment variable (default to "localhost")
        var factory = new ConnectionFactory()
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
        };

        if (int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out int port))
        {
            factory.Port = port;
        }

        // default in newest clients
        factory.AutomaticRecoveryEnabled = true;

        var clientCertPath = Environment.GetEnvironmentVariable("RABBITMQ_CLIENT_CERT_PATH");
        if (!string.IsNullOrEmpty(clientCertPath))
        {
            var clientCertPassword = Environment.GetEnvironmentVariable("RABBITMQ_CLIENT_CERT_PASSWORD") ?? string.Empty;
            var clientCertificate = CertUtils.LoadCerts(clientCertPath, clientCertPassword);
            factory.Ssl = new SslOption
            {
                Enabled = true,
                Version = System.Security.Authentication.SslProtocols.Tls12,
                Certs = clientCertificate,
                CertPassphrase = clientCertPassword,
                CertificateValidationCallback = CertUtils.Validate
            };
            factory.Ssl.ServerName = factory.HostName;
        }

        // Asynchronously create the connection and channel
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Declare the queue asynchronously
        await _channel.QueueDeclareAsync(
            queue: "test-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Use the asynchronous eventing consumer
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"Received message: {message}");
            // Await something if needed; here we simply yield control.
            await Task.Yield();
        };

        // Start consuming messages asynchronously
        await _channel.BasicConsumeAsync(
            queue: "test-queue",
            autoAck: true,
            consumer: consumer);

        // Keep the service running until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.CloseAsync();
        _connection?.CloseAsync();
        base.Dispose();
    }
}
