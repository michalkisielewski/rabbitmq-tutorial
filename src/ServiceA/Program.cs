using System.Text;
using RabbitMQ.Client;
using Shared;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/publish", async (string message = "Message sent") =>
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

    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(message));

    await channel.BasicPublishAsync(
        exchange: "",
        routingKey: "test-queue",
        mandatory: false,
        basicProperties: new BasicProperties(),
        body: body);

    return Results.Ok($"Published message: {message}");
});

app.Run();
