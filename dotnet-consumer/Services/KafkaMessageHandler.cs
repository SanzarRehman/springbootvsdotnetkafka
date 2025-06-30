using Confluent.Kafka;
using DotNetConsumer.Data;
using DotNetConsumer.Models;
using System.Threading.Channels;

namespace DotNetConsumer.Services;

public class KafkaMessageHandler : KafkaMessageDispatcherBase<string>
{
    private readonly int _concurrency;
    private readonly Channel<ConsumeResult<string, string>> _messageChannel;
    private readonly ChannelWriter<ConsumeResult<string, string>> _channelWriter;
    private readonly ChannelReader<ConsumeResult<string, string>> _channelReader;

    public KafkaMessageHandler(string topicName, int concurrency, IConsumer<string, string> consumer, IServiceProvider serviceProvider)
        : base(topicName, consumer, serviceProvider)
    {
        _concurrency = concurrency;
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };
        _messageChannel = Channel.CreateBounded<ConsumeResult<string, string>>(options);
        _channelWriter = _messageChannel.Writer;
        _channelReader = _messageChannel.Reader;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var consumerTask = Task.Run(() => ConsumeMessages(cancellationToken), cancellationToken);
        
        var processingTasks = new List<Task>();
        for (int i = 0; i < _concurrency; i++)
        {
            processingTasks.Add(Task.Run(() => ProcessMessages(cancellationToken), cancellationToken));
        }

        try
        {
            await Task.WhenAll([consumerTask, .. processingTasks]);
        }
        finally
        {
            Consumer.Close();
        }
    }

    private async Task ConsumeMessages(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var consumeResult = Consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    await _channelWriter.WriteAsync(consumeResult, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            _channelWriter.Complete();
        }
    }

    private async Task ProcessMessages(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _channelReader.ReadAllAsync(cancellationToken))
            {
                await ProcessMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
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

            // Manual commit - using lock for thread safety like Java
            lock (Consumer)
            {
                Consumer.Commit(consumeResult);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            // Add retry logic if needed
        }
    }
}