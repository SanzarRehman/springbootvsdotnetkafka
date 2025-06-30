using Confluent.Kafka;

namespace DotNetConsumer.Services;

public abstract class KafkaMessageDispatcherBase<T>
{
    protected readonly string TopicName;
    protected readonly IConsumer<string, T> Consumer;
    protected readonly IServiceProvider ServiceProvider;

    protected KafkaMessageDispatcherBase(string topicName, IConsumer<string, T> consumer, IServiceProvider serviceProvider)
    {
        TopicName = topicName;
        Consumer = consumer;
        ServiceProvider = serviceProvider;
    }

    public abstract Task StartAsync(CancellationToken cancellationToken);
}