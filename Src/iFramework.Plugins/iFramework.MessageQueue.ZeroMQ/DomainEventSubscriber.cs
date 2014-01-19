using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message;
using System.Threading.Tasks;
using IFramework.SysException;
using IFramework.UnitOfWork;
using IFramework.Message.Impl;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class DomainEventSubscriber : MessageConsumer<IMessageContext>
    {
        IHandlerProvider HandlerProvider { get; set; }
        string[] SubEndPoints { get; set; }

        public DomainEventSubscriber(IHandlerProvider handlerProvider, string[] subEndPoints)
        {
            HandlerProvider = handlerProvider;
            SubEndPoints = subEndPoints;
        }

        public override void Start()
        {
            SubEndPoints.ForEach(subEndPoint =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(subEndPoint))
                    {
                        // Receive messages
                        var messageReceiver = CreateSocket(subEndPoint);
                        Task.Factory.StartNew(ReceiveMessages, messageReceiver);

                        // Consume messages
                        Task.Factory.StartNew(ConsumeMessages);
                    }
                }
                catch (Exception e)
                {
                    _Logger.Error(e.GetBaseException().Message, e);
                }
            });
        }

        protected override ZmqSocket CreateSocket(string subEndPoint)
        {
            ZmqSocket receiver = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.SUB);
            receiver.SubscribeAll();
            receiver.Connect(subEndPoint);
            return receiver;
        }

        protected override void ReceiveMessage(Frame frame)
        {
            var messageContexts = System.Text.Encoding
                                            .GetEncoding("utf-8")
                                            .GetString(frame.Buffer)
                                            .ToJsonObject<List<MessageContext>>();

            messageContexts.ForEach(messageContext =>
            {
                MessageQueue.Add(messageContext);
                HandledMessageCount++;
            });
        }

        protected override void ConsumeMessage(IMessageContext messageContext)
        {
            var message = messageContext.Message;
            var messageHandlers = HandlerProvider.GetHandlers(message.GetType());
            messageHandlers.ForEach(messageHandler =>
            {
                try
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = messageContext;
                    messageHandler.Handle(message);
                }
                catch (Exception e)
                {
                    Console.Write(e.GetBaseException().Message);
                }
                finally
                {
                    messageContext.ClearItems();
                }
            });
        }
    }
}
