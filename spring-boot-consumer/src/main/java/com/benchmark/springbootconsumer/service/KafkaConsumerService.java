package com.benchmark.springbootconsumer.service;

import com.benchmark.springbootconsumer.entity.SpringBootMessage;
import com.benchmark.springbootconsumer.repository.SpringBootMessageRepository;
import org.apache.kafka.clients.consumer.Consumer;
import org.apache.kafka.clients.consumer.ConsumerRecord;
import org.apache.kafka.clients.consumer.ConsumerRecords;
import org.apache.kafka.clients.consumer.OffsetAndMetadata;
import org.apache.kafka.common.TopicPartition;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.kafka.core.ConsumerFactory;
import org.springframework.stereotype.Service;

import jakarta.annotation.PostConstruct;
import java.time.Duration;
import java.util.Collections;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

@Service
public class KafkaConsumerService {

    private final ExecutorService executor = Executors.newFixedThreadPool(4);
    private final ConsumerFactory<String, String> consumerFactory;
    private final SpringBootMessageRepository messageRepository;

    @Autowired
    public KafkaConsumerService(ConsumerFactory<String, String> consumerFactory,
                               SpringBootMessageRepository messageRepository) {
        this.consumerFactory = consumerFactory;
        this.messageRepository = messageRepository;
    }

    @PostConstruct
    public void start() {
        Thread listenerThread = new Thread(this::consume);
        listenerThread.setDaemon(true);
        listenerThread.start();
    }

    private void consume() {
        try (Consumer<String, String> consumer = consumerFactory.createConsumer()) {
            consumer.subscribe(List.of("benchmark-topic"));

            while (true) {
                ConsumerRecords<String, String> records = consumer.poll(Duration.ofMillis(100));
                for (ConsumerRecord<String, String> record : records) {
                    executor.submit(() -> processRecord(record, consumer));
                }
            }
        }
    }

    private void processRecord(ConsumerRecord<String, String> record, Consumer<String, String> consumer) {
        try {
            SpringBootMessage entity = new SpringBootMessage(UUID.randomUUID().toString(), record.value());
            messageRepository.save(entity);
            consumer.commitSync(Collections.singletonMap(
                new TopicPartition(record.topic(), record.partition()),
                new OffsetAndMetadata(record.offset() + 1)
            ));
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}