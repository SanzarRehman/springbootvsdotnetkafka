namespace DotNetConsumer.Services;

public class KafkaConsumerHostedService : BackgroundService
{
    private readonly KafkaConsumerService _kafkaConsumerService;
    private readonly ILogger<KafkaConsumerHostedService> _logger;

    public KafkaConsumerHostedService(
        KafkaConsumerService kafkaConsumerService,
        ILogger<KafkaConsumerHostedService> logger)
    {
        _kafkaConsumerService = kafkaConsumerService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Hosted Service starting");
        
        try
        {
            await _kafkaConsumerService.StartConsumingAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Kafka Consumer Hosted Service");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka Consumer Hosted Service stopping");
        await base.StopAsync(cancellationToken);
    }
}