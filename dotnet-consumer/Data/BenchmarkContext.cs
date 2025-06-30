using DotNetConsumer.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetConsumer.Data;

public class BenchmarkContext : DbContext
{
    public BenchmarkContext(DbContextOptions<BenchmarkContext> options) : base(options)
    {
    }

    public DbSet<DotNetMessage> DotNetMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DotNetMessage>(entity =>
        {
            entity.ToTable("dotnet_messages"); // Map to the correct table name
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}