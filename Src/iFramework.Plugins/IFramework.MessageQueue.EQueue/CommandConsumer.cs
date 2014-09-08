using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.UnitOfWork;
using IFramework.SysException;
using System.Collections.Concurrent;
using EQueueClientsProducers = EQueue.Clients.Producers;
using EQueueClientsConsumers = EQueue.Clients.Consumers;
using EQueueProtocols = EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue
{
    public class CommandConsumer : MessageConsumer<IFramework.MessageQueue.MessageFormat.MessageContext>
    {
        public static List<CommandConsumer> CommandConsumers = new List<CommandConsumer>();
        public static ConcurrentDictionary<string, string> _handledCommandDict = new ConcurrentDictionary<string, string>();
        public static string GetConsumersStatus()
        {
            var status = string.Empty;

            status += CommandConsumer.CommandConsumers[0].GetStatus();
            status += CommandConsumer.CommandConsumers[1].GetStatus();
            status += CommandConsumer.CommandConsumers[2].GetStatus();
            status += CommandConsumer.CommandConsumers[3].GetStatus();
            return status;
        }
        protected IHandlerProvider HandlerProvider { get; set; }
        protected EQueueClientsProducers.Producer Producer { get; set; }

        public CommandConsumer(string name, EQueueClientsConsumers.ConsumerSetting consumerSetting, string groupName,
                               string subscribeTopic,  string brokerAddress, int producerBrokerPort,
                               IHandlerProvider handlerProvider)
            : base(name, consumerSetting, groupName, subscribeTopic)
        {
            HandlerProvider = handlerProvider;
            var producerSetting = new EQueueClientsProducers.ProducerSetting();
            producerSetting.BrokerAddress = brokerAddress;
            producerSetting.BrokerPort = producerBrokerPort;
            Producer = new EQueueClientsProducers.Producer(string.Format("{0}-Reply-Producer", name), producerSetting);
        }

        public override void Start()
        {
            base.Start();
            try
            {
                Producer.Start();
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        void OnMessageHandled(IFramework.Message.IMessageContext messageContext, IMessageReply reply)
        {
            if (!string.IsNullOrWhiteSpace(messageContext.ReplyToEndPoint))
            {
                var messageBody = reply.GetMessageBytes();
                Producer.SendAsync(new global::EQueue.Protocols.Message(messageContext.ReplyToEndPoint, messageBody), string.Empty)
                        .ContinueWith(task => {
                            if (task.Result.SendStatus == EQueueClientsProducers.SendStatus.Success)
                            {
                                _Logger.DebugFormat("send reply, commandID:{0}", reply.MessageID);
                            }
                            else
                            {
                                _Logger.ErrorFormat("Send Reply {0}", task.Result.SendStatus.ToString());
                            }
                        });
                
            }
        }

        protected override void ConsumeMessage(IFramework.MessageQueue.MessageFormat.MessageContext messageContext, EQueueProtocols.QueueMessage queueMessage)
        {
            IMessageReply messageReply = null;
            if (messageContext == null || messageContext.Message == null)
            {
                return;
            }
            var currentName = string.Format("consumer:{0} queueID:{1}", this.Name, queueMessage.QueueId);
            if (!_handledCommandDict.TryAdd(messageContext.MessageID, currentName))
            {
                _Logger.ErrorFormat("Duplicated command [{0}] to be processed in QueueID:{1}.", messageContext.MessageID, currentName);
            }


            _Logger.DebugFormat("Start Handle command, commandID:{0} queueID:{1}", messageContext.MessageID, queueMessage.QueueId);
            var message = messageContext.Message;
            var messageHandlers = HandlerProvider.GetHandlers(message.GetType());
            try
            {
                if (messageHandlers.Count == 0)
                {
                    messageReply = new MessageReply(messageContext.MessageID, new NoHandlerExists());
                }
                else
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = messageContext;
                    var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                    messageHandlers[0].Handle(message);
                    unitOfWork.Commit();
                    messageReply = new MessageReply(messageContext.MessageID, message.GetValueByKey("Result"));
                }

            }
            catch (Exception e)
            {
                messageReply = new MessageReply(messageContext.MessageID, e.GetBaseException());
                // need log
            }
            finally
            {
                messageContext.ClearItems();
                OnMessageHandled(messageContext, messageReply);
                _Logger.DebugFormat("End Handle command, commandID:{0} queueID{1}", messageContext.MessageID, queueMessage.QueueId);

            }
        }
    }
}
