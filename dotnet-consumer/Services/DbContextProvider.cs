using DotNetConsumer.Data;
using Microsoft.EntityFrameworkCore;

namespace DotNetConsumer.Services;

public class DbContextProvider
{
    private readonly string _connectionString;

    public DbContextProvider(IConfiguration configuration)
    {
        _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                           ?? "Host=localhost;Database=benchmark_db;Username=benchmark_user;Password=benchmark_pass";
    }

    public BenchmarkContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<BenchmarkContext>();
        optionsBuilder.UseNpgsql(_connectionString);
        return new BenchmarkContext(optionsBuilder.Options);
    }
}