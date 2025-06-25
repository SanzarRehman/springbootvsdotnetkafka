#!/bin/bash

# Script to ensure Kafka topic has 4 partitions before starting the benchmark

echo "🔧 Setting up Kafka topic with 4 partitions..."

# Wait for Kafka to be ready
echo "⏳ Waiting for Kafka to be ready..."
until docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list >/dev/null 2>&1; do
    echo "Waiting for Kafka..."
    sleep 2
done

echo "✅ Kafka is ready!"

# Check if topic exists and get partition count
TOPIC_INFO=$(docker exec kafka kafka-topics --bootstrap-server localhost:9092 --describe --topic benchmark-topic 2>/dev/null || echo "NOT_FOUND")

if [[ "$TOPIC_INFO" == "NOT_FOUND" ]]; then
    echo "📝 Topic doesn't exist, creating with 4 partitions..."
    docker exec kafka kafka-topics --bootstrap-server localhost:9092 --create --topic benchmark-topic --partitions 4 --replication-factor 1
    echo "✅ Topic created with 4 partitions"
else
    # Extract partition count
    PARTITION_COUNT=$(echo "$TOPIC_INFO" | grep "PartitionCount:" | sed 's/.*PartitionCount: \([0-9]*\).*/\1/')
    
    if [[ "$PARTITION_COUNT" != "4" ]]; then
        echo "⚠️  Topic exists with $PARTITION_COUNT partitions, but we need 4. Recreating..."
        docker exec kafka kafka-topics --bootstrap-server localhost:9092 --delete --topic benchmark-topic
        sleep 3
        docker exec kafka kafka-topics --bootstrap-server localhost:9092 --create --topic benchmark-topic --partitions 4 --replication-factor 1
        echo "✅ Topic recreated with 4 partitions"
    else
        echo "✅ Topic already exists with 4 partitions"
    fi
fi

# Verify final state
echo "🔍 Final verification:"
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --describe --topic benchmark-topic

echo "🚀 Topic setup complete!"