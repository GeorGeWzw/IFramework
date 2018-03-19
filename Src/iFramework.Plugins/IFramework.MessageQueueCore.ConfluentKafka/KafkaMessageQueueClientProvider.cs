﻿using System;
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka.Serialization;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueueCore.ConfluentKafka.MessageFormat;

namespace IFramework.MessageQueueCore.ConfluentKafka
{
    public class KafkaMessageQueueClientProvider : IMessageQueueClientProvider
    {
        private readonly string _brokerList;
        public KafkaMessageQueueClientProvider(string brokerList)
        {
            _brokerList = brokerList;
        }


        public IMessageConsumer CreateQueueConsumer(string queue, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig)
        {
            return new KafkaConsumer<string, KafkaMessage>(_brokerList, queue, $"{queue}.consumer", consumerId,
                                                                        BuildOnKafkaMessageReceived(onMessagesReceived),
                                                                        new StringDeserializer(Encoding.UTF8),
                                                                        new KafkaMessageDeserializer(),
                                                                        consumerConfig);
        }

        public IMessageProducer CreateQueueProducer(string queue)
        {
            return new KafkaProducer(queue, _brokerList, new StringSerializer(Encoding.UTF8), new KafkaMessageSerializer());

        }

        public IMessageProducer CreateTopicProducer(string topic)
        {
            return new KafkaProducer(topic, _brokerList, new StringSerializer(Encoding.UTF8), new KafkaMessageSerializer());
        }

        public IMessageConsumer CreateTopicSubscription(string topic, string subscriptionName, OnMessagesReceived onMessagesReceived, string consumerId, ConsumerConfig consumerConfig)
        {
            return new KafkaConsumer<string, KafkaMessage>(_brokerList, topic, subscriptionName, consumerId,
                                                           BuildOnKafkaMessageReceived(onMessagesReceived),
                                                           new StringDeserializer(Encoding.UTF8),
                                                           new KafkaMessageDeserializer(),
                                                           consumerConfig);
        }

        public IMessageContext WrapMessage(object message, string correlationId = null, string topic = null, string key = null, string replyEndPoint = null, string messageId = null, SagaInfo sagaInfo = null, string producer = null)
        {
            var messageContext = new MessageContext(message, messageId)
            {
                Producer = producer,
                Ip = Utility.GetLocalIPV4()?.ToString()
            };
            if (!string.IsNullOrEmpty(correlationId))
            {
                messageContext.CorrelationId = correlationId;
            }
            if (!string.IsNullOrEmpty(topic))
            {
                messageContext.Topic = topic;
            }
            if (!string.IsNullOrEmpty(key))
            {
                messageContext.Key = key;
            }
            if (!string.IsNullOrEmpty(replyEndPoint))
            {
                messageContext.ReplyToEndPoint = replyEndPoint;
            }
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId))
            {
                messageContext.SagaInfo = sagaInfo;
            }
            return messageContext;
        }

        private OnKafkaMessageReceived<string, KafkaMessage> BuildOnKafkaMessageReceived(OnMessagesReceived onMessagesReceived)
        {
            return (consumer, message) =>
            {
                var kafkaMessage = message.Value;
                var messageContext = new MessageContext(kafkaMessage, message.Partition, message.Offset);
                onMessagesReceived(messageContext);
            };
        }
    }
}