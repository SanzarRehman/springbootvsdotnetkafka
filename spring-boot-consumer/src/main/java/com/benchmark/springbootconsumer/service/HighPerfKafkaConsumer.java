package com.benchmark.springbootconsumer.service;

import com.benchmark.springbootconsumer.entity.SpringBootMessage;
import com.benchmark.springbootconsumer.repository.SpringBootMessageRepository;
import org.apache.kafka.clients.consumer.Consumer;
import org.apache.kafka.clients.consumer.ConsumerConfig;
import org.apache.kafka.clients.consumer.ConsumerRecord;
import org.apache.kafka.clients.consumer.ConsumerRecords;
import org.apache.kafka.common.serialization.StringDeserializer;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.core.DefaultKafkaConsumerFactory;
import org.springframework.stereotype.Service;

import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import java.time.Duration;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicBoolean;

@Service
public class HighPerfKafkaConsumer {

    // High-performance configuration matching .NET
    private static final int CONCURRENCY = 50;
    private static final String GROUP_ID = "simple";
    private static final String TOPIC_NAME = "benchmark-topic";
    
    private final ExecutorService processingExecutor = Executors.newFixedThreadPool(CONCURRENCY);
    private final AtomicBoolean isRunning = new AtomicBoolean(false);
    private final SpringBootMessageRepository messageRepository;
    private final String bootstrapServers;
    private Consumer<String, String> consumer;
    private Thread consumerThread;

    @Autowired
    public HighPerfKafkaConsumer(SpringBootMessageRepository messageRepository) {
        this.messageRepository = messageRepository;
        this.bootstrapServers = System.getenv().getOrDefault("SPRING_KAFKA_BOOTSTRAP_SERVERS", "localhost:9092");
    }

    @PostConstruct
    public void start() {
        isRunning.set(true);
        
        // Create consumer with high-performance config
        Map<String, Object> props = new HashMap<>();
        props.put(ConsumerConfig.BOOTSTRAP_SERVERS_CONFIG, bootstrapServers);
        props.put(ConsumerConfig.GROUP_ID_CONFIG, GROUP_ID);
        props.put(ConsumerConfig.KEY_DESERIALIZER_CLASS_CONFIG, StringDeserializer.class);
        props.put(ConsumerConfig.VALUE_DESERIALIZER_CLASS_CONFIG, StringDeserializer.class);
        props.put(ConsumerConfig.ENABLE_AUTO_COMMIT_CONFIG, false);
        props.put(ConsumerConfig.AUTO_OFFSET_RESET_CONFIG, "latest");
        props.put(ConsumerConfig.ALLOW_AUTO_CREATE_TOPICS_CONFIG, true);
        props.put(ConsumerConfig.PARTITION_ASSIGNMENT_STRATEGY_CONFIG, "org.apache.kafka.clients.consumer.CooperativeStickyAssignor");
        props.put(ConsumerConfig.FETCH_MIN_BYTES_CONFIG, 1);
        props.put(ConsumerConfig.FETCH_MAX_WAIT_MS_CONFIG, 100);
        props.put(ConsumerConfig.MAX_POLL_RECORDS_CONFIG, 1000);
        props.put(ConsumerConfig.MAX_POLL_INTERVAL_MS_CONFIG, 300000); // 5 minutes
        props.put(ConsumerConfig.SESSION_TIMEOUT_MS_CONFIG, 30000); // 30 seconds
        
        DefaultKafkaConsumerFactory<String, String> factory = new DefaultKafkaConsumerFactory<>(props);
        consumer = factory.createConsumer();
        consumer.subscribe(List.of(TOPIC_NAME));
        
        // Start single consumer thread (thread-safe approach)
        consumerThread = new Thread(this::consumeMessages, "kafka-consumer-thread");
        consumerThread.setDaemon(true);
        consumerThread.start();
        
        System.out.println("Started Kafka consumer for topic '" + TOPIC_NAME + "' with concurrency " + CONCURRENCY + " connecting to " + bootstrapServers + ".");
    }

    @PreDestroy
    public void stop() {
        isRunning.set(false);
        
        if (consumerThread != null) {
            consumerThread.interrupt();
        }
        
        if (consumer != null) {
            consumer.close();
        }
        
        processingExecutor.shutdown();
    }

    private void consumeMessages() {
        try {
            while (isRunning.get()) {
                try {
                    // Poll for records - this is done in a single thread to avoid concurrency issues
                    ConsumerRecords<String, String> records = consumer.poll(Duration.ofMillis(1000));
                    
                    if (!records.isEmpty()) {
                        // Process records in parallel using the thread pool
                        CompletableFuture<?>[] futures = new CompletableFuture[records.count()];
                        int index = 0;
                        
                        for (ConsumerRecord<String, String> record : records) {
                            futures[index++] = CompletableFuture.runAsync(() -> processRecord(record), processingExecutor);
                        }
                        
                        // Wait for all processing to complete before committing
                        CompletableFuture.allOf(futures).join();
                        
                        // Commit offsets after all messages are processed
                        consumer.commitSync();
                    }
                } catch (Exception e) {
                    if (isRunning.get()) {
                        System.err.println("Error in consumer polling: " + e.getMessage());
                        e.printStackTrace();
                        // Sleep briefly before retrying
                        Thread.sleep(1000);
                    }
                }
            }
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            System.out.println("Consumer thread interrupted");
        } catch (Exception e) {
            if (isRunning.get()) {
                System.err.println("Fatal error in consumer thread: " + e.getMessage());
                e.printStackTrace();
            }
        }
    }

    private void processRecord(ConsumerRecord<String, String> record) {
        try {
            SpringBootMessage entity = new SpringBootMessage(UUID.randomUUID().toString(), record.value());
            messageRepository.save(entity);
        } catch (Exception e) {
            System.err.println("Error processing record: " + e.getMessage());
        }
    }
}