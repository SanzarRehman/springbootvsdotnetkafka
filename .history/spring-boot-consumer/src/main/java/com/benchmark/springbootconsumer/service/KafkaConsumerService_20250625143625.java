package com.benchmark.springbootconsumer.service;

import com.benchmark.springbootconsumer.entity.SpringBootMessage;
import com.benchmark.springbootconsumer.repository.SpringBootMessageRepository;
import io.micrometer.core.instrument.Counter;
import io.micrometer.core.instrument.MeterRegistry;
import io.micrometer.core.instrument.Timer;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.support.KafkaHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.UUID;

@Service
public class KafkaConsumerService {
    
    private static final Logger logger = LoggerFactory.getLogger(KafkaConsumerService.class);
    
    private final SpringBootMessageRepository messageRepository;
    private final Counter messagesProcessedCounter;
    private final Timer processingTimer;
    
    @Autowired
    public KafkaConsumerService(SpringBootMessageRepository messageRepository, MeterRegistry meterRegistry) {
        this.messageRepository = messageRepository;
        this.messagesProcessedCounter = Counter.builder("kafka.messages.processed")
                .description("Number of Kafka messages processed")
                .tag("consumer", "spring-boot")
                .register(meterRegistry);
        this.processingTimer = Timer.builder("kafka.message.processing.time")
                .description("Time taken to process Kafka messages")
                .tag("consumer", "spring-boot")
                .register(meterRegistry);
    }
    
    @KafkaListener(topics = "benchmark-topic", groupId = "spring-boot-group")
    @Transactional
    public void consumeMessage(@Payload String message, 
                              @Header(KafkaHeaders.RECEIVED_PARTITION) int partition) {
        
        Timer.Sample sample = Timer.start();
        
        try {
            SpringBootMessage messageEntity = new SpringBootMessage(UUID.randomUUID().toString(), message);
            messageRepository.save(messageEntity);
            messagesProcessedCounter.increment();
            
            logger.debug("Processed message from partition {}", partition);
            
        } catch (Exception e) {
            logger.error("Error processing message: {}", e.getMessage(), e);
            throw e;
        } finally {
            sample.stop(processingTimer);
        }
    }
}