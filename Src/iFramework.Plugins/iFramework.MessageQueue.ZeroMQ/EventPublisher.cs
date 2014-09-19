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
        protected string _PubEndPoint;
        protected Task _WorkTask;

        public EventPublisher(string pubEndPoint)
        {
            _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
            MessageQueue = new BlockingCollection<IMessageContext>();
            _PubEndPoint = pubEndPoint;
           
        }

        public void Start()
        {
            if (!string.IsNullOrWhiteSpace(_PubEndPoint))
            {
                ZmqEventPublisher = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUB);
                ZmqEventPublisher.Bind(_PubEndPoint);
                _WorkTask = Task.Factory.StartNew(PublishEvent, TaskCreationOptions.LongRunning);
            }
        }

        public void Stop()
        {
            if (_WorkTask != null)
            {
                MessageQueue.CompleteAdding();
                if (_WorkTask.Wait(2000))
                {
                    ZmqEventPublisher.Close();
                    _WorkTask.Dispose();
                }
                else
                {
                    _Logger.ErrorFormat("consumer can't be stopped!");
                }
            }
        }

        public void Publish(params IMessageContext[] eventContexts)
        {
            eventContexts.ForEach(@messageContext =>
            {
                MessageQueue.Add(messageContext);
            });
        }

        public void Publish(params IEvent[] events)
        {
            events.ForEach(@event =>
            {
                MessageQueue.Add(new MessageContext(@event));
            });
        }

        void PublishEvent()
        {
            try
            {
                while (true)
                {
                    var eventContext = MessageQueue.Take();
                    ZmqEventPublisher.Send(eventContext.ToJson(), Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                _Logger.Debug("end publish working", ex);
            }
        }
    }
}
