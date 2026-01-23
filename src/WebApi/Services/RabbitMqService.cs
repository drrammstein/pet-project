using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Contracts;

namespace WebApi.Services;

public interface IRabbitMqService
{
    Task SendMessageAsync(UpdateValueMessage message);
}

public class RabbitMqService : IRabbitMqService, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqService> _logger;

    private RabbitMqService(IConnection connection, IChannel channel, IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _connection = connection;
        _channel = channel;
        _configuration = configuration;
        _logger = logger;
    }

    public static async Task<RabbitMqService> CreateAsync(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"],
            Port = int.Parse(configuration["RabbitMQ:Port"]!),
            UserName = configuration["RabbitMQ:Username"],
            Password = configuration["RabbitMQ:Password"]
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: configuration["RabbitMQ:QueueName"]!,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        logger.LogInformation("RabbitMQ service initialized");

        return new RabbitMqService(connection, channel, configuration, logger);
    }

    public async Task SendMessageAsync(UpdateValueMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: _configuration["RabbitMQ:QueueName"]!,
            body: body);

        _logger.LogInformation("Message sent to RabbitMQ: {@Message}", message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.CloseAsync();

        if (_connection != null)
            await _connection.CloseAsync();

        _channel?.Dispose();
        _connection?.Dispose();
    }
}