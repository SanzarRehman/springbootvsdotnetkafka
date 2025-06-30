using Confluent.Kafka;
using DotNetConsumer.Data;
using DotNetConsumer.Models;

namespace DotNetConsumer.Services;

public class OrderedKafkaMessageHandler : KafkaMessageDispatcherBase<string>
{
    public OrderedKafkaMessageHandler(string topicName, IConsumer<string, string> consumer, IServiceProvider serviceProvider)
        : base(topicName, consumer, serviceProvider)
    {
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var consumeResult = Consumer.Consume(TimeSpan.FromMilliseconds(1000)); // Match Spring Boot's 1000ms
                if (consumeResult != null)
                {
                    await ProcessMessage(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            Consumer.Close();
        }
    }

    private async Task ProcessMessage(ConsumeResult<string, string> consumeResult)
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
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

            // Manual commit for better performance control
            Consumer.Commit(consumeResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            // Add retry logic if needed
        }
    }
}