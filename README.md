# Spring Boot vs .NET Kafka Consumer Performance Benchmark

This project compares the performance of Spring Boot and .NET applications consuming Kafka messages and writing to PostgreSQL. Both applications are designed with similar architectures to provide a fair comparison of throughput (TPS - Transactions Per Second).

## Architecture

- **Kafka**: Message broker with 4 partitions for parallel processing
- **PostgreSQL**: Database for storing consumed messages
- **Spring Boot Consumer**: Java application using Spring Kafka
- **.NET Consumer**: C# application using Confluent.Kafka
- **Message Producer**: Node.js application generating test messages
- **Docker**: Containerized environment for consistent testing

## Features

- Batch processing for optimal performance
- Prometheus metrics integration
- Database performance monitoring
- Resource usage tracking
- Configurable message rates

## Quick Start

1. **Prerequisites**
   - Docker and Docker Compose installed
   - At least 4GB of available RAM

2. **Start the benchmark**
   ```bash
   ./start-benchmark.sh
   ```

3. **Monitor performance**
   ```bash
   ./scripts/check-performance.sh
   ```

4. **Stop the benchmark**
   ```bash
   docker-compose down
   ```

## Configuration

### Message Producer
- `MESSAGES_PER_SECOND`: Rate of message generation (default: 1000)
- `TOPIC_NAME`: Kafka topic name (default: benchmark-topic)

### Consumer Configuration Comparison

Both consumers are identically configured for fair performance comparison:

| Setting | Spring Boot | .NET | Status |
|---------|-------------|------|--------|
| **Concurrency** | 50 threads | 50 threads | ✅ Identical |
| **Consumer Group** | "simple" | "simple" | ✅ Identical |
| **Topic** | "benchmark-topic" | "benchmark-topic" | ✅ Identical |
| **Auto Commit** | false | false | ✅ Identical |
| **Auto Offset Reset** | "latest" | Latest | ✅ Identical |
| **Fetch Min Bytes** | 1 | 1 | ✅ Identical |
| **Fetch Wait Max Ms** | 100ms | 100ms | ✅ Identical |
| **Max Poll Interval** | 300000ms (5 min) | 300000ms (5 min) | ✅ Identical |
| **Session Timeout** | 30000ms (30 sec) | 30000ms (30 sec) | ✅ Identical |
| **Heartbeat Interval** | 3000ms | 3000ms | ✅ Identical |
| **Poll Timeout** | 1000ms | 1000ms | ✅ Identical |
| **Partition Strategy** | CooperativeSticky | CooperativeSticky | ✅ Identical |
| **Message Format** | String key/value | String key/value | ✅ Identical |
| **Commit Strategy** | Manual after DB save | Manual after DB save | ✅ Identical |

### Fair Comparison Features

- **Same Consumer Group**: Both consumers compete for the same messages
- **Identical Kafka Settings**: Same timeouts, fetch sizes, and polling behavior
- **Matched Concurrency**: Both use 50 concurrent message processors
- **Equivalent Processing**: Both save individual messages to database before committing
- **Same Message Format**: Both handle string keys and JSON string values

This ensures performance differences reflect only the runtime characteristics of:
- **Spring Boot/Java** with Hibernate + PostgreSQL driver
- **.NET Core/C#** with Entity Framework + Npgsql driver

### Spring Boot Consumer Details
- Concurrency: 50 processing threads
- Database: Hibernate with connection pool (20 connections)
- Commit: Manual sync commits after batch processing

### .NET Consumer Details  
- Concurrency: 50 processing threads via Channel-based dispatcher
- Database: Entity Framework Core with connection pooling
- Commit: Manual commits with thread-safe locking

## Monitoring

### Database Views
The project includes a `performance_comparison` view that shows:
- Total messages processed
- TPS (Transactions Per Second)
- Processing duration
- First and last message timestamps

### Metrics Endpoints
- Spring Boot: `http://localhost:8080/actuator/metrics`
- .NET: `http://localhost:8081/metrics`

### Live Monitoring
```bash
# Performance comparison
./scripts/check-performance.sh

# Container logs
docker-compose logs -f spring-boot-consumer
docker-compose logs -f dotnet-consumer
docker-compose logs -f message-producer

# Database access
docker exec -it postgres psql -U benchmark_user -d benchmark_db
```

## Performance Tuning

### Spring Boot Optimizations
- Hibernate batch inserts enabled
- Connection pooling with HikariCP
- Kafka batch acknowledgment
- JPA bulk operations

### .NET Optimizations
- Entity Framework batch operations
- Async/await patterns
- Optimized Kafka consumer configuration
- Connection pooling

### Database Optimizations
- Indexed columns for queries
- Batch inserts
- Connection pooling
- Optimized PostgreSQL settings

## Project Structure

```
├── docker-compose.yml              # Main orchestration file
├── start-benchmark.sh              # Startup script
├── infrastructure/
│   ├── init.sql                   # Database initialization
│   └── producer/                  # Message producer
├── spring-boot-consumer/          # Java Spring Boot application
├── dotnet-consumer/               # C# .NET application
└── scripts/
    └── check-performance.sh       # Performance monitoring script
```

## Sample Results

The benchmark will show results similar to:

```
Performance Results (Last Hour):
=================================
Consumer Type | Total Messages | TPS | Duration (s) | First Message | Last Message
-------------|----------------|-----|--------------|---------------|-------------
dotnet       | 45000         | 750 | 60.00        | 2025-01-01... | 2025-01-01...
spring_boot  | 42000         | 700 | 60.00        | 2025-01-01... | 2025-01-01...
```

## Troubleshooting

### Common Issues

1. **Services not starting**
   - Ensure Docker has enough memory allocated (4GB+)
   - Check if ports 5432, 8080, 8081, 9092 are available

2. **No performance data**
   - Wait for services to fully initialize (30+ seconds)
   - Check that message producer is sending messages

3. **Database connection issues**
   - Verify PostgreSQL container is running: `docker ps`
   - Check logs: `docker-compose logs postgres`

### Performance Tips

1. **Increase message rate**
   ```bash
   # Edit docker-compose.yml
   environment:
     MESSAGES_PER_SECOND: 2000
   ```

2. **Scale consumers**
   ```bash
   docker-compose up --scale spring-boot-consumer=2 --scale dotnet-consumer=2
   ```

```
   cd /Users/sanzar/Documents/springbootvsdotnetkafka && docker-compose build spring-boot-consumer
sleep 5 && docker exec kafka kafka-topics --bootstrap-server localhost:9092 --describe --topic benchmark-topic
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --create --topic benchmark-topic --partitions 4 --replication-factor 1

docker exec kafka kafka-topics --bootstrap-server localhost:9092 --delete --topic benchmark-topic                     

```

3. **Monitor resource usage**
   ```bash
   docker stats
   ```

## Contributing

Feel free to contribute improvements:
- Performance optimizations
- Additional metrics
- Different database backends
- Load testing scenarios

## License

This project is for benchmarking and educational purposes.