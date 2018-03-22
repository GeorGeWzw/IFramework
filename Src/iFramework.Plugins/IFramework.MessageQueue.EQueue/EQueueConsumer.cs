﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.EQueue.MessageFormat;
using EQueueMessages = EQueue.Protocols;
using EQueueConsumers = EQueue.Clients.Consumers;

namespace IFramework.MessageQueue.EQueue
{
    public delegate void OnEQueueMessageReceived(EQueueConsumer consumer, EQueueMessages.QueueMessage message);

    public class EQueueConsumer: MessageConsumer
    {
        private readonly OnEQueueMessageReceived _onMessageReceived;
        public EQueueConsumer(string clusterName,
                              List<IPEndPoint> nameServerList,
                              string topic,
                              string groupId,
                              string consumerId,
                              OnEQueueMessageReceived onMessageReceived,
                              ConsumerConfig consumerConfig = null,
                              bool start = true)
        :base(topic, groupId, consumerId, consumerConfig)
        {
            _onMessageReceived = onMessageReceived;
            ClusterName = clusterName;
            NameServerList = nameServerList;
            SlidingDoors = new ConcurrentDictionary<int, SlidingDoor>();
        }

        public string ClusterName { get; protected set; }
        public List<IPEndPoint> NameServerList { get; protected set; }
       
        protected EQueueConsumers.Consumer Consumer { get; set; }

        public override void Start()
        {
            var setting = new EQueueConsumers.ConsumerSetting
            {
                AutoPull = false,
                ConsumeFromWhere = (ConsumerConfig.AutoOffsetReset == AutoOffsetReset.Smallest || ConsumerConfig.AutoOffsetReset == AutoOffsetReset.Earliest) ?
                EQueueMessages.ConsumeFromWhere.FirstOffset : EQueueMessages.ConsumeFromWhere.LastOffset,
                ClusterName = ClusterName,
                NameServerList = NameServerList
            };
            Consumer = new EQueueConsumers.Consumer(GroupId, setting)
                .Subscribe(Topic)
                .Start();
           base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            Consumer?.Stop();
        }

        protected override void PollMessages()
        {
            var messages = PullMessages(100, 2000, CancellationTokenSource.Token);
            messages.ForEach(message =>
            {
                AddMessageOffset(message.QueueId, message.QueueOffset);
                _onMessageReceived(this, message);
            });
        }

        public override Task CommitOffsetAsync(string broker, int partition, long offset)
        {
            Consumer.CommitConsumeOffset(broker, Topic, partition, offset);
            return Task.CompletedTask;
        }

       

        protected virtual IEnumerable<EQueueMessages.QueueMessage> PullMessages(int maxCount,
                                                                     int timeoutMilliseconds,
                                                                     CancellationToken cancellationToken)
        {
            return Consumer.PullMessages(maxCount, timeoutMilliseconds, cancellationToken);
        }


       
    }
}