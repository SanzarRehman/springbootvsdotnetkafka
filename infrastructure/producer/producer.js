const { Kafka } = require('kafkajs');

class MessageProducer {
    constructor() {
        this.kafka = new Kafka({
            clientId: 'benchmark-producer',
            brokers: [process.env.KAFKA_BOOTSTRAP_SERVERS || 'localhost:9092']
        });
        
        this.producer = this.kafka.producer({
            maxInFlightRequests: 1,
            idempotent: false,
            transactionTimeout: 30000,
        });
        
        this.messagesPerSecond = parseInt(process.env.MESSAGES_PER_SECOND) || 1000;
        this.topicName = process.env.TOPIC_NAME || 'benchmark-topic';
        this.messageCount = 0;
        this.startTime = null;
    }

    async initialize() {
        console.log('Connecting to Kafka...');
        await this.producer.connect();
        console.log('Connected to Kafka');
        
        const admin = this.kafka.admin();
        await admin.connect();
        
        try {
            // Check if topic exists and get its metadata
            console.log(`Checking topic: ${this.topicName}`);
            const metadata = await admin.fetchTopicMetadata({ topics: [this.topicName] });
            const topicMetadata = metadata.topics.find(t => t.name === this.topicName);
            
            if (topicMetadata && topicMetadata.partitions.length !== 4) {
                console.log(`Topic ${this.topicName} exists with ${topicMetadata.partitions.length} partitions, but we need 4. Deleting and recreating...`);
                
                // Delete the existing topic with wrong partition count

                
                // Wait for deletion to complete
                await new Promise(resolve => setTimeout(resolve, 3000));
            } else if (topicMetadata && topicMetadata.partitions.length === 4) {
                console.log(`Topic ${this.topicName} already exists with 4 partitions - using existing topic`);
                await admin.disconnect();
                return;
            }
            
            // Create topic with 4 partitions
            
            
        } catch (error) {
            if (error.message.includes('UnknownTopicOrPartition') || error.message.includes('does not exist')) {
                // Topic doesn't exist, create it
                try {
                    await admin.createTopics({
                        topics: [{
                            topic: this.topicName,
                            numPartitions: 4,
                            replicationFactor: 1
                        }]
                    });
                    console.log(`Topic ${this.topicName} created with 4 partitions`);
                } catch (createError) {
                    console.log(`Topic creation result: ${createError.message}`);
                }
            } else {
                console.log(`Topic management result: ${error.message}`);
            }
        }
        
        await admin.disconnect();
        
        // Verify the topic has 4 partitions
        await admin.connect();
        try {
            const finalMetadata = await admin.fetchTopicMetadata({ topics: [this.topicName] });
            const finalTopicMetadata = finalMetadata.topics.find(t => t.name === this.topicName);
            if (finalTopicMetadata) {
                console.log(`✅ Verified: Topic ${this.topicName} has ${finalTopicMetadata.partitions.length} partitions`);
            }
        } catch (verifyError) {
            console.log(`Topic verification error: ${verifyError.message}`);
        }
        await admin.disconnect();
    }

    generateMessage() {
        const messageId = `msg-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
        const payload = {
            id: messageId,
            timestamp: new Date().toISOString(),
            data: `Benchmark message ${this.messageCount}`,
            payload: 'x'.repeat(100)
        };
        
        return JSON.stringify(payload);
    }

    async startProducing() {
        this.startTime = Date.now();
        console.log(`Starting to produce ${this.messagesPerSecond} messages per second to topic: ${this.topicName}`);
        
        const interval = 1000 / this.messagesPerSecond;
        let lastSent = Date.now();
        
        const sendMessages = async () => {
            const now = Date.now();
            const elapsed = now - lastSent;
            
            if (elapsed >= interval) {
                const messagesToSend = Math.floor(elapsed / interval);
                const messages = [];
                
                for (let i = 0; i < messagesToSend; i++) {
                    messages.push({
                        key: `key-${this.messageCount}`,
                        value: this.generateMessage()
                    });
                    this.messageCount++;
                }
                
                try {
                    await this.producer.send({
                        topic: this.topicName,
                        messages: messages
                    });
                    
                    lastSent = now;
                    
                    // Log stats every 10 seconds
                    if (this.messageCount % (this.messagesPerSecond * 10) === 0) {
                        const runtime = (Date.now() - this.startTime) / 1000;
                        const rate = this.messageCount / runtime;
                        console.log(`Sent ${this.messageCount} messages in ${runtime.toFixed(1)}s (${rate.toFixed(1)} msg/s)`);
                    }
                } catch (error) {
                    console.error('Error sending messages:', error);
                }
            }
            
            setImmediate(sendMessages);
        };
        
        sendMessages();
    }

    async shutdown() {
        console.log('Shutting down producer...');
        await this.producer.disconnect();
        const runtime = (Date.now() - this.startTime) / 1000;
        console.log(`Final stats: ${this.messageCount} messages in ${runtime.toFixed(1)}s (${(this.messageCount / runtime).toFixed(1)} msg/s)`);
    }
}

// Main execution
async function main() {
    const producer = new MessageProducer();
    
    // Handle graceful shutdown
    process.on('SIGINT', async () => {
        console.log('\nReceived SIGINT, shutting down gracefully...');
        await producer.shutdown();
        process.exit(0);
    });
    
    process.on('SIGTERM', async () => {
        console.log('\nReceived SIGTERM, shutting down gracefully...');
        await producer.shutdown();
        process.exit(0);
    });
    
    try {
        await producer.initialize();
        await producer.startProducing();
    } catch (error) {
        console.error('Error in producer:', error);
        process.exit(1);
    }
}

main();