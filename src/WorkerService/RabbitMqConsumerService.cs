using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;
using WorkerService.Data;

namespace WorkerService.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection _connection;
    private IChannel _channel;

    public RabbitMqConsumerService(
        ILogger<RabbitMqConsumerService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service starting...");
        await InitializeRabbitMqAsync();
        _logger.LogInformation("RabbitMQ Consumer started. Waiting for messages...");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                await ProcessMessageAsync(ea, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        };

        _channel.BasicConsumeAsync(
            queue: _configuration["RabbitMQ:QueueName"]!,
            autoAck: false,
            consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("RabbitMQ Consumer Service stopping");
    }

    private async Task InitializeRabbitMqAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"],
            Port = int.Parse(_configuration["RabbitMQ:Port"]!),
            UserName = _configuration["RabbitMQ:Username"],
            Password = _configuration["RabbitMQ:Password"]
        };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: _configuration["RabbitMQ:QueueName"]!,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        _logger.LogInformation("Connected to RabbitMQ. Queue: {QueueName}",
            _configuration["RabbitMQ:QueueName"]);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        var body = ea.Body.ToArray();
        var messageJson = Encoding.UTF8.GetString(body);

        _logger.LogInformation("Received message: {Message}", messageJson);

        try
        {
            var message = JsonSerializer.Deserialize<UpdateValueMessage>(messageJson);
            if (message == null)
            {
                _logger.LogError("Failed to deserialize message: {MessageJson}", messageJson);
                _channel.BasicNackAsync(ea.DeliveryTag, false, false); // Отклоняем сообщение
                return;
            }

            await UpdateDatabaseAsync(message, cancellationToken);

            _channel.BasicAckAsync(ea.DeliveryTag, false);

            _logger.LogInformation("Message processed successfully. Updated value to: {NewValue}",
                message.NewValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            _channel.BasicNackAsync(ea.DeliveryTag, false, true);
        }
    }
    private async Task UpdateDatabaseAsync(UpdateValueMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkerAppDbContext>();

        var data = await dbContext.AppData
            .FirstOrDefaultAsync(d => d.Id == message.Id, cancellationToken);

        if (data == null)
        {
            _logger.LogError("Record with Id {Id} not found in database", message.Id);
            throw new Exception($"Record with Id {message.Id} not found");
        }
        data.Value = message.NewValue;
        data.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Database updated. Record Id: {Id}, New Value: {Value}, UbdatedAt: {UpdatedAt}",
            data.Id, data.Value, data.UpdatedAt);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service stopping...");

        if ( _channel != null )
            await _channel.CloseAsync();
        if (_connection != null )
            await _connection.CloseAsync();

        await base.StopAsync(cancellationToken);
    }
}
