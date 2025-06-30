using DotNetConsumer.Data;
using DotNetConsumer.Services;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                       ?? "Host=localhost;Database=benchmark_db;Username=benchmark_user;Password=benchmark_pass";

builder.Services.AddDbContext<BenchmarkContext>(options =>
    options.UseNpgsql(connectionString));

// Add high-performance Kafka consumer service
builder.Services.AddHostedService<KafkaMessageConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpMetrics();
app.MapMetrics();
app.MapControllers();

app.Run();