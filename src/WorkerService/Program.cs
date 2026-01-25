using WorkerService.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<RabbitMqConsumerService>();

var host = builder.Build();
host.Run();