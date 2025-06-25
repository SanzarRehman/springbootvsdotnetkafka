package com.benchmark.springbootconsumer.repository;

import com.benchmark.springbootconsumer.entity.SpringBootMessage;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Repository
public interface SpringBootMessageRepository extends JpaRepository<SpringBootMessage, Long> {
    
    @Modifying
    @Transactional
    @Query("INSERT INTO SpringBootMessage (messageId, content, timestamp, processedAt) VALUES (?1, ?2, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)")
    void insertBatch(String messageId, String content);
    
    @Query(value = "SELECT COUNT(*) FROM spring_boot_messages WHERE processed_at >= NOW() - INTERVAL '1 hour'", nativeQuery = true)
    long countMessagesLastHour();
}