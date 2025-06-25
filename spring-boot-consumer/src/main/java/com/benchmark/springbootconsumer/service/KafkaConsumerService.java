package com.benchmark.springbootconsumer.service;

import com.benchmark.springbootconsumer.entity.SpringBootMessage;
import com.benchmark.springbootconsumer.repository.SpringBootMessageRepository;
import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.MeterRegistry;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.support.KafkaHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Service;

import java.util.UUID;
import java.util.concurrent.atomic.AtomicLong;

@Service
public class KafkaConsumerService {
    
    private final SpringBootMessageRepository messageRepository;
    private final Counter messagesProcessedCounter;
    private final AtomicLong messageCount = new AtomicLong(0);
    
    @Autowired
    public KafkaConsumerService(SpringBootMessageRepository messageRepository, MeterRegistry meterRegistry) {
        this.messageRepository = messageRepository;
        this.messagesProcessedCounter = Counter.builder("kafka.messages.processed")
                .description("Number of Kafka messages processed")
                .tag("consumer", "spring-boot")
                .register(meterRegistry);
    }
    
    @KafkaListener(
        topics = "benchmark-topic", 
        groupId = "spring-boot-group",
        concurrency = "4",
        containerFactory = "kafkaListenerContainerFactory"
    )
    public void consumeMessage(@Payload String message, 
                              @Header(KafkaHeaders.RECEIVED_PARTITION) int partition,
                              @Header(KafkaHeaders.RECEIVED_TOPIC) String topic) {
        
        String consumerThread = Thread.currentThread().getName();
        long currentCount = messageCount.incrementAndGet();
        
        SpringBootMessage messageEntity = new SpringBootMessage(UUID.randomUUID().toString(), message);
        messageRepository.save(messageEntity);
        messagesProcessedCounter.increment();
        
        // Enhanced logging to show partition distribution
        if (currentCount % 100 == 0) {
            System.out.println("Spring Boot Consumer [" + consumerThread + "] processed message #" + currentCount + 
                             " from partition " + partition + " of topic " + topic);
        }
    }
}