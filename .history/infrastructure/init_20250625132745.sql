-- Create tables for both Spring Boot and .NET consumers

-- Table for Spring Boot consumer
CREATE TABLE spring_boot_messages (
    id BIGSERIAL PRIMARY KEY,
    message_id VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Table for .NET consumer
CREATE TABLE dotnet_messages (
    id BIGSERIAL PRIMARY KEY,
    message_id VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better performance
CREATE INDEX idx_spring_boot_message_id ON spring_boot_messages(message_id);
CREATE INDEX idx_spring_boot_timestamp ON spring_boot_messages(timestamp);
CREATE INDEX idx_dotnet_message_id ON dotnet_messages(message_id);
CREATE INDEX idx_dotnet_timestamp ON dotnet_messages(timestamp);

-- Create view for performance comparison
CREATE VIEW performance_comparison AS
SELECT 
    'spring_boot' as consumer_type,
    COUNT(*) as total_messages,
    MIN(processed_at) as first_message,
    MAX(processed_at) as last_message,
    EXTRACT(EPOCH FROM (MAX(processed_at) - MIN(processed_at))) as duration_seconds
FROM spring_boot_messages
WHERE processed_at > NOW() - INTERVAL '1 hour'
UNION ALL
SELECT 
    'dotnet' as consumer_type,
    COUNT(*) as total_messages,
    MIN(processed_at) as first_message,
    MAX(processed_at) as last_message,
    EXTRACT(EPOCH FROM (MAX(processed_at) - MIN(processed_at))) as duration_seconds
FROM dotnet_messages
WHERE processed_at > NOW() - INTERVAL '1 hour';