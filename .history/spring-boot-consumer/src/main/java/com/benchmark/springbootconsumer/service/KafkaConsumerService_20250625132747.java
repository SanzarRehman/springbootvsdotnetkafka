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
import org.springframework.kafka.support.Acknowledgment;
import org.springframework.kafka.support.KafkaHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.UUID;
import java.util.stream.Collectors;

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
    public void consumeMessages(@Payload List<String> messages, 
                               @Header(KafkaHeaders.RECEIVED_PARTITION) List<Integer> partitions,
                               Acknowledgment acknowledgment) {
        
        Timer.Sample sample = Timer.start();
        
        try {
            List<SpringBootMessage> messageEntities = messages.stream()
                    .map(content -> new SpringBootMessage(UUID.randomUUID().toString(), content))
                    .collect(Collectors.toList());
            
            messageRepository.saveAll(messageEntities);
            messagesProcessedCounter.increment(messages.size());
            
            logger.debug("Processed {} messages", messages.size());
            
        } catch (Exception e) {
            logger.error("Error processing messages: {}", e.getMessage(), e);
            throw e;
        } finally {
            sample.stop(processingTimer);
            acknowledgment.acknowledge();
        }
    }
}