
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapGet("/api/test", () =>
{
    Log.Information("Test endpoint called at {Timestamp}", DateTime.UtcNow);

    return Results.Ok(new
    {
        Message = "Hello from Pet Project API",
        Timestamp = DateTime.UtcNow,
        Status = "Working"
    });
})
.WithName("TestEndpoint")
.WithOpenApi();

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Console.WriteLine($"Application started. Swagger: http://localhost:5050/swagger");
        Console.WriteLine($"Test endpoint: http://localhost:5050/api/test");
        Console.WriteLine($"Health check: http://localhost:5050/health");
    });
}

app.Run();