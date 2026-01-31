
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Contracts;
using Shared.Logging;
using WebApi.Data;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

var app = builder.Build();

app.UseCors("AllowFrontend");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations: {ex.Message}");
    }
}


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

app.MapGet("/api/value", async ([FromServices] AppDbContext context) =>
{
    var data = await context.AppData.FirstOrDefaultAsync(d => d.Key == "main_value");

    if (data == null)
    {
        return Results.NotFound("Value not found in database");
    }
    Log.Information("GET /api/value: Retrieved value '{Value}' from database", data.Value);

    return Results.Ok(new
    {
        data.Id,
        data.Key,
        data.Value,
        data.UpdatedAt
    });
})
.WithName("GetValue")
.WithOpenApi();

app.MapPost("/api/updateValue", async (UpdateValueRequest request,
    [FromServices] AppDbContext context,
    [FromServices] IRabbitMqService rabbitMqService) =>
{
    if (string.IsNullOrWhiteSpace(request.NewValue))
    {
        return Results.BadRequest("NewValue is required");
    }

    var currentData = await context.AppData.FirstOrDefaultAsync(d => d.Key == "main_value");
    if (currentData == null)
    {
        return Results.NotFound("Value not found in database");
    }

    var message = new UpdateValueMessage
    {
        Id = currentData.Id,
        NewValue = request.NewValue,
        RequestedAt = DateTime.UtcNow
    };

    await rabbitMqService.SendMessageAsync(message);

    Log.Information("POST /api/updateValue: Sent update request to RabbitMQ. New value: {NewValue}", request.NewValue);

    return Results.Accepted("/api/updateValue", new
    {
        Message = "Update request accepted and sent to queue",
        RequestId = Guid.NewGuid(),
        CurrentValue = currentData.Value,
        RequestedValue = request.NewValue,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("UpdateValue")
.WithOpenApi();

app.MapPost("/api/simple-test", (string testValue) =>
{
    return Results.Ok(new
    {
        message = "Simple test works!",
        value = testValue
    });
})
.WithName("SimpleTest")
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