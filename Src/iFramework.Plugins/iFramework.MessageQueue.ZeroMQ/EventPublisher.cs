using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using ZeroMQ;
using IFramework.Message.Impl;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;
using IFramework.Infrastructure.Logging;

namespace IFramework.MessageQueue.ZeroMQ
{
    class EventPublisher : IEventPublisher
    {
        protected ZmqSocket ZmqEventPublisher { get; set; }
        protected BlockingCollection<IMessageContext> MessageQueue { get; set; }
        protected ILogger _Logger;

        public EventPublisher(string pubEndPoint)
        {
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
            MessageQueue = new BlockingCollection<IMessageContext>();
            if (!string.IsNullOrWhiteSpace(pubEndPoint))
            {
                ZmqEventPublisher = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUB);
                ZmqEventPublisher.Bind(pubEndPoint);
            }
        }

        public IEnumerable<IMessageContext> Publish(params IDomainEvent[] events)
        {
            List<IMessageContext> messageContexts = new List<IMessageContext>();
            events.ForEach(@event =>
            {
                messageContexts.Add(new MessageContext(@event));
            });
            var sendStatus = ZmqEventPublisher.Send(messageContexts.ToJson(), Encoding.UTF8);
            if (sendStatus == SendStatus.Sent)
            {
                _Logger.DebugFormat("publish {0} events", messageContexts.Count);
            }
            else
            {
                _Logger.DebugFormat("publish {0} events {1}", messageContexts.Count, sendStatus.ToString());
            }
            return messageContexts;
        }
    }
}
