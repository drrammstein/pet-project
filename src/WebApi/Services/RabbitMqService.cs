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
    private readonly Lazy<Task<IConnection>> _connection;
    private readonly Lazy<Task<IChannel>> _channel;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqService> _logger;
    private string QueueName => _configuration["RabbitMQ:QueueName"]!;

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _connection = new Lazy<Task<IConnection>>(async () =>
        {
            _logger.LogInformation("Initializing RabbitMQ connection...");

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"],
                Port = int.Parse(_configuration["RabbitMQ:Port"]!),
                UserName = _configuration["RabbitMQ:Username"],
                Password = _configuration["RabbitMQ:Password"]
            };

            var connection = await factory.CreateConnectionAsync();
            _logger.LogInformation("RabbitMQ connection established");

            return connection;
        });

        _channel = new Lazy<Task<IChannel>>(async () =>
        {
            _logger.LogInformation("Initializing RabbitMQ channel...");

            var connection = await _connection.Value;
            var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMQ channel initialized for queue: {QueueName}", QueueName);

            return channel;
        });
    }

    public async Task SendMessageAsync(UpdateValueMessage message)
    {
        try
        {
            var channel = await _channel.Value;

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                body: body);

            _logger.LogInformation("Message sent to RabbitMQ queue '{QueueName}': {@Message}",
                QueueName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to RabbitMQ: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing RabbitMQ service...");

        try
        {
            if (_channel.IsValueCreated)
            {
                var channel = await _channel.Value;
                await channel.CloseAsync();
                channel.Dispose();
            }

            if (_connection.IsValueCreated)
            {
                var connection = await _connection.Value;
                await connection.CloseAsync();
                connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ service");
        }

        GC.SuppressFinalize(this);
    }
}