using Bot;
using Bot.Extensions;
using Engine;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var config = new Config(builder.Configuration);
builder.Services.AddSingleton(config);
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.ConfigureGraphClient(config);

builder.Services.AddCallingBot(config);
builder.Services.AddChatBot();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
