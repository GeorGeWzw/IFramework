using IFramework.Message;
using IFramework.Message.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using EQueueClients = EQueue.Clients;
using EQueue.Clients.Producers;
using EQueue.Clients.Consumers;
using EQueue.Protocols;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.EQueue
{
    public abstract class MessageConsumer<TMessage> : 
        IMessageConsumer, 
        EQueueClients.Consumers.IMessageHandler
    {

        public decimal MessageCount { get; protected set; }
        protected string Name { get; set; }
        protected decimal HandledMessageCount { get; set; }
        public Consumer Consumer { get; set; }
        protected readonly ILogger _Logger;

        public MessageConsumer()
        {
            
        }

        public MessageConsumer(string id, ConsumerSettings consumerSettings, string groupName, MessageModel messageModel, string subscribeTopic)
            : this()
        {
            Name = id;
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(Name);
            Consumer = new Consumer(id, consumerSettings, groupName, MessageModel.Clustering, this)
                .Subscribe(subscribeTopic);
        }

        public virtual void Start()
        {
            try
            {
                Consumer.Start();
            }
            catch (Exception e)
            {
                _Logger.Error(e.GetBaseException().Message, e);
            }
        }

        public virtual string GetStatus()
        {
            var queueIDs = string.Join(",", Consumer.GetCurrentQueues().Select(x => x.QueueId));
            return string.Format("{0} Handled command {1} queueID {2}\r\n", Name, HandledMessageCount, queueIDs);
        }

        public virtual void Handle(global::EQueue.Protocols.QueueMessage message)
        {
            ConsumeMessage(message.Body.GetMessage<TMessage>(), message);
            HandledMessageCount++;
        }

        protected abstract void ConsumeMessage(TMessage messageContext, QueueMessage message);
    }
}
