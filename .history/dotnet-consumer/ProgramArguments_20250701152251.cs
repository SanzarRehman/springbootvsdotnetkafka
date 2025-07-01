namespace DotNetConsumer;

public record ProgramArguments(string KafkaServers, string[] PostgreSqlParameters);