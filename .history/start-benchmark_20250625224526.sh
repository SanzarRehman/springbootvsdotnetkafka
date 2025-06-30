#!/bin/bash

echo "=== Spring Boot vs .NET Kafka Consumer Benchmark ==="
echo "This script will start all services and run the benchmark"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker first."
    exit 1
fi

echo "Building and starting infrastructure services..."
docker-compose up --build -d kafka zookeeper postgres

echo "Waiting for Kafka to be ready..."
sleep 15

echo "Setting up Kafka topic with 4 partitions..."
./scripts/setup-topic.sh

echo "Starting consumer services..."
docker-compose up --build -d spring-boot-consumer dotnet-consumer

echo "Waiting for consumers to be ready..."
sleep 10

echo "Starting message producer..."
docker-compose up --build -d message-producer

echo "Waiting for all services to be fully ready..."
sleep 15

echo "Services started! Monitoring URLs:"
echo "- Spring Boot Metrics: http://localhost:8080/actuator/metrics"
echo "- .NET Metrics: http://localhost:8081/metrics"
echo "- PostgreSQL: localhost:5432"
echo ""

echo "To stop the benchmark, run: docker-compose down"
echo "To view logs: docker-compose logs -f [service-name]"
echo ""

echo "Benchmark is now running. Check the performance with:"
echo "  ./scripts/check-performance.sh"