#!/bin/bash

echo "=== Performance Comparison Report ==="
echo "Timestamp: $(date)"
echo ""

# Check if PostgreSQL is accessible
if ! docker exec postgres psql -U benchmark_user -d benchmark_db -c "\q" 2>/dev/null; then
    echo "Error: Cannot connect to PostgreSQL. Make sure services are running."
    exit 1
fi

echo "Querying database for performance metrics..."
echo ""

# Get performance data from the database
PERFORMANCE_DATA=$(docker exec postgres psql -U benchmark_user -d benchmark_db -t -c "
SELECT 
    consumer_type,
    total_messages,
    ROUND(total_messages / GREATEST(duration_seconds, 1), 2) as tps,
    ROUND(duration_seconds, 2) as duration_seconds,
    first_message,
    last_message
FROM performance_comparison 
ORDER BY consumer_type;
")

if [ -z "$PERFORMANCE_DATA" ]; then
    echo "No performance data available yet. Make sure the benchmark is running and messages are being processed."
    exit 1
fi

echo "Performance Results (Last Hour):"
echo "================================="
echo "Consumer Type | Total Messages | TPS | Duration (s) | First Message | Last Message"
echo "-------------|----------------|-----|--------------|---------------|-------------"

while IFS='|' read -r consumer_type total_messages tps duration first_message last_message; do
    # Trim whitespace
    consumer_type=$(echo "$consumer_type" | xargs)
    total_messages=$(echo "$total_messages" | xargs)
    tps=$(echo "$tps" | xargs)
    duration=$(echo "$duration" | xargs)
    first_message=$(echo "$first_message" | xargs)
    last_message=$(echo "$last_message" | xargs)
    
    if [ ! -z "$consumer_type" ]; then
        printf "%-12s | %-14s | %-3s | %-12s | %-13s | %s\n" \
            "$consumer_type" "$total_messages" "$tps" "$duration" "$first_message" "$last_message"
    fi
done <<< "$PERFORMANCE_DATA"

echo ""
echo "Live Message Counts:"
echo "===================="

SPRING_BOOT_COUNT=$(docker exec postgres psql -U benchmark_user -d benchmark_db -t -c "SELECT COUNT(*) FROM spring_boot_messages;")
DOTNET_COUNT=$(docker exec postgres psql -U benchmark_user -d benchmark_db -t -c "SELECT COUNT(*) FROM dotnet_messages;")

echo "Spring Boot Total Messages: $(echo $SPRING_BOOT_COUNT | xargs)"
echo ".NET Total Messages: $(echo $DOTNET_COUNT | xargs)"

echo ""
echo "Container Resource Usage:"
echo "========================"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" \
    spring-boot-consumer dotnet-consumer message-producer 2>/dev/null || echo "Docker stats not available"

echo ""
echo "To view live logs:"
echo "  docker-compose logs -f spring-boot-consumer"
echo "  docker-compose logs -f dotnet-consumer"
echo "  docker-compose logs -f message-producer"