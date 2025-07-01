using Confluent.Kafka;

namespace DotNetConsumer.Services;

public abstract class KafkaMessageDispatcherBase<T>
{
    protected readonly string TopicName;
    protected readonly IConsumer<string, T> Consumer;
    protected readonly DbContextProvider DbContextProvider;

    protected KafkaMessageDispatcherBase(string topicName, IConsumer<string, T> consumer, DbContextProvider dbContextProvider)
    {
        TopicName = topicName;
        Consumer = consumer;
        DbContextProvider = dbContextProvider;
    }

    public abstract Task StartAsync(CancellationToken cancellationToken);
}