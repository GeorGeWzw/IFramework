using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using System.Threading.Tasks;
using IFramework.SysException;
using IFramework.UnitOfWork;
using IFramework.Message.Impl;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.MessageQueue.MessageFormat;
using EQueue.Clients.Consumers;

namespace IFramework.MessageQueue.EQueue
{
    public class DomainEventSubscriber : MessageConsumer<MessageContext>
    {
        IHandlerProvider HandlerProvider { get; set; }

        public DomainEventSubscriber(string name, ConsumerSettings consumerSettings, 
                                     string groupName, string subscribeTopic,
                                     IHandlerProvider handlerProvider)
            : base(groupName, consumerSettings, groupName, global::EQueue.Protocols.MessageModel.BroadCasting, subscribeTopic)
        {
            HandlerProvider = handlerProvider;
        }

        public override void Handle(global::EQueue.Protocols.QueueMessage message)
        {
            var messageContexts = message.Body.GetMessage<List<MessageContext>>();
            messageContexts.ForEach(messageContext =>
            {
                ConsumeMessage(messageContext);
                HandledMessageCount++;
            });
        }

        protected override void ConsumeMessage(MessageContext messageContext)
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
