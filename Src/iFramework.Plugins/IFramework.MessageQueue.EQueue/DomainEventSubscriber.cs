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
using EQueueClientsConsumers = EQueue.Clients.Consumers;
using EQueueProtocols = EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue
{
    public class DomainEventSubscriber : MessageConsumer<IFramework.MessageQueue.MessageFormat.MessageContext>
    {
        IHandlerProvider HandlerProvider { get; set; }

        public DomainEventSubscriber(string name, EQueueClientsConsumers.ConsumerSetting consumerSetting, 
                                     string groupName, string subscribeTopic,
                                     IHandlerProvider handlerProvider)
            : base(groupName, consumerSetting, groupName, subscribeTopic)
        {
            HandlerProvider = handlerProvider;
        }

        public override void Handle(EQueueProtocols.QueueMessage message, global::EQueue.Clients.Consumers.IMessageContext context)
        {
            var messageContexts = message.Body.GetMessage<List<IFramework.MessageQueue.MessageFormat.MessageContext>>();
            messageContexts.ForEach(messageContext =>
            {
                ConsumeMessage(messageContext, message);
                HandledMessageCount++;
            });
        }

        protected override void ConsumeMessage(IFramework.MessageQueue.MessageFormat.MessageContext messageContext, EQueueProtocols.QueueMessage queueMessage)
        {

            _Logger.DebugFormat("Start Handle event , messageContextID:{0} queueID:{1}", messageContext.MessageID, queueMessage.QueueId);

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
                    _Logger.DebugFormat("End Handle event , messageContextID:{0} queueID:{1}", messageContext.MessageID, queueMessage.QueueId);

                }
            });
        }
    }
}
