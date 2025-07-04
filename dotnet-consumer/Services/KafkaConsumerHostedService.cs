namespace DotNetConsumer.Services;

using Confluent.Kafka;
using DotNetConsumer.Data;
using DotNetConsumer.Models;
using Microsoft.Extensions.Hosting;

internal class KafkaMessageConsumer : IHostedService
{
    private const int Concurrency = 50;
    private const string GroupId = "dotnet-consumer-group"; // Unique group for .NET
    private const string TopicName = "benchmark-topic";
    
    private readonly List<Task> kafkaListenerTasks = [];
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly DbContextProvider _dbContextProvider;
    private readonly ILogger<KafkaMessageConsumer> _logger;
    private readonly string _bootstrapServers;

    public KafkaMessageConsumer(DbContextProvider dbContextProvider, ILogger<KafkaMessageConsumer> logger)
    {
        _dbContextProvider = dbContextProvider;
        _logger = logger;
        _bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartConsumers(TopicName, _dbContextProvider, cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationTokenSource.Cancel();

            await Task.WhenAll(kafkaListenerTasks);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void StartConsumers(string topicName, DbContextProvider dbContextProvider, CancellationToken cancellationToken)
    {
        ConsumerConfig consumerConfig = new()
        {
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            GroupId = GroupId,
            AllowAutoCreateTopics = true,
            BootstrapServers = _bootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Latest,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
            // High-performance settings matching Java exactly
            FetchMinBytes = 1,
            FetchWaitMaxMs = 100,
            MaxPollIntervalMs = 300000,  // 5 minutes
            SessionTimeoutMs = 30000,    // 30 seconds - Match Spring Boot
            HeartbeatIntervalMs = 3000,
            // Batch processing optimizations  
            MaxPartitionFetchBytes = 1048576, // 1MB
        };

        IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        consumer.Subscribe(topicName);

        // Use single handler type for better performance
        KafkaMessageDispatcherBase<string> kafkaMessageHandler = new KafkaMessageHandler(topicName, Concurrency, consumer, dbContextProvider);

        Task kafkaListenerTask = kafkaMessageHandler.StartAsync(cancellationToken);

        kafkaListenerTasks.Add(kafkaListenerTask);

        Console.WriteLine($"Started Kafka consumer for topic '{topicName}' with concurrency {Concurrency} connecting to {_bootstrapServers}.");
    }
}