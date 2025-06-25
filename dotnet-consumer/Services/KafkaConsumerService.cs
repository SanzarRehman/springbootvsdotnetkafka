using Confluent.Kafka;
using DotNetConsumer.Data;
using DotNetConsumer.Models;
using Prometheus;

namespace DotNetConsumer.Services;

public class KafkaConsumerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly Counter _messagesProcessedCounter;
    private readonly ConsumerConfig _config;
    private static long _messageCount = 0;

    public KafkaConsumerService(IServiceProvider serviceProvider, ILogger<KafkaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        _messagesProcessedCounter = Metrics
            .CreateCounter("kafka_messages_processed_total", "Number of Kafka messages processed", new[] { "consumer" });

        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        
        _config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "dotnet-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        // Start 4 concurrent consumers
        var consumerTasks = new List<Task>();
        
        for (int i = 0; i < 4; i++)
        {
            var consumerId = i + 1;
            consumerTasks.Add(Task.Run(() => RunConsumerAsync(consumerId, cancellationToken), cancellationToken));
        }
        
        await Task.WhenAll(consumerTasks);
    }

    private async Task RunConsumerAsync(int consumerId, CancellationToken cancellationToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(_config).Build();
        
        consumer.Subscribe("benchmark-topic");
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    
                    if (consumeResult != null)
                    {
                        long currentCount = Interlocked.Increment(ref _messageCount);
                        
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<BenchmarkContext>();

                        var entity = new DotNetMessage
                        {
                            MessageId = Guid.NewGuid().ToString(),
                            Content = consumeResult.Message.Value,
                            Timestamp = DateTime.UtcNow,
                            ProcessedAt = DateTime.UtcNow
                        };

                        await context.DotNetMessages.AddAsync(entity);
                        await context.SaveChangesAsync();

                        _messagesProcessedCounter.WithLabels("dotnet").Inc();
                        
                        Console.WriteLine($".NET Consumer [{consumerId}] processed message #{currentCount} from partition {consumeResult.Partition}");
                    }
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($".NET Consumer [{consumerId}] error: {e.Error.Reason}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($".NET Consumer [{consumerId}] cancelled");
        }
        finally
        {
            consumer.Close();
        }
    }
}