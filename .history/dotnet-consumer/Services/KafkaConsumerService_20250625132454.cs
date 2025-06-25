using Confluent.Kafka;
using DotNetConsumer.Data;
using DotNetConsumer.Models;
using Prometheus;
using System.Diagnostics;

namespace DotNetConsumer.Services;

public class KafkaConsumerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly Counter _messagesProcessedCounter;
    private readonly Histogram _processingTimeHistogram;
    private readonly ConsumerConfig _config;

    public KafkaConsumerService(IServiceProvider serviceProvider, ILogger<KafkaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        _messagesProcessedCounter = Metrics
            .CreateCounter("kafka_messages_processed_total", "Number of Kafka messages processed", new[] { "consumer" });
        
        _processingTimeHistogram = Metrics
            .CreateHistogram("kafka_message_processing_duration_seconds", "Time taken to process Kafka messages", new[] { "consumer" });

        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        
        _config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "dotnet-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 30000,
            FetchMaxBytes = 52428800,
            MaxPartitionFetchBytes = 1048576
        };
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(_config).Build();
        
        consumer.Subscribe("benchmark-topic");
        _logger.LogInformation("Started consuming from benchmark-topic");

        var messages = new List<ConsumeResult<string, string>>();
        const int batchSize = 500;
        var batchTimeout = TimeSpan.FromMilliseconds(100);
        var lastBatchTime = DateTime.UtcNow;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    
                    if (consumeResult != null)
                    {
                        messages.Add(consumeResult);
                    }

                    var shouldProcessBatch = messages.Count >= batchSize ||
                                           (messages.Count > 0 && DateTime.UtcNow - lastBatchTime > batchTimeout);

                    if (shouldProcessBatch)
                    {
                        await ProcessBatchAsync(messages);
                        
                        foreach (var message in messages)
                        {
                            consumer.Commit(message);
                        }
                        
                        messages.Clear();
                        lastBatchTime = DateTime.UtcNow;
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogError(e, "Error consuming message: {Error}", e.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer cancelled");
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer closed");
        }
    }

    private async Task ProcessBatchAsync(List<ConsumeResult<string, string>> messages)
    {
        using var timer = _processingTimeHistogram.WithLabels("dotnet").NewTimer();
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BenchmarkContext>();

        try
        {
            var entities = messages.Select(msg => new DotNetMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                Content = msg.Message.Value,
                Timestamp = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            }).ToList();

            await context.DotNetMessages.AddRangeAsync(entities);
            await context.SaveChangesAsync();

            _messagesProcessedCounter.WithLabels("dotnet").Inc(messages.Count);
            _logger.LogDebug("Processed batch of {Count} messages", messages.Count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing batch of {Count} messages", messages.Count);
            throw;
        }
    }
}