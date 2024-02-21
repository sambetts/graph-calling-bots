using PstnBot.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add logging
var logger = LoggerFactory.Create(config =>
{
    config.AddTraceSource(new System.Diagnostics.SourceSwitch("SourceSwitch"));
    config.AddConsole();
}).CreateLogger("Calling Bot");
builder.Services.AddSingleton(logger);

// Add services to the container.
builder.Services.AddCallingBot(options => builder.Configuration.Bind("Bot", options));
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();
