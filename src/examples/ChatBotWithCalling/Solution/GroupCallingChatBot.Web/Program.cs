using GroupCallingChatBot.Web.Extensions;
using GroupCallingChatBot.Web.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var config = new TeamsChatbotBotConfig(builder.Configuration);
builder.Services.AddSingleton(config);
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.ConfigureGraphClient(config);

// Add bot services
builder.Services.AddCallingBot(config);
builder.Services.AddChatBot();

var app = builder.Build();

app.UseHttpsRedirection(); 
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();
