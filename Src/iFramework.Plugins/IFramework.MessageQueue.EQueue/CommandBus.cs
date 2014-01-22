using IFramework.Command;
using IFramework.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using System.Threading;
using IFramework.SysException;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.UnitOfWork;
using IFramework.Config;
using System.Collections.Concurrent;
using IFramework.MessageQueue.MessageFormat;
using EQueue.Protocols;
using EQueue.Clients.Producers;
using EQueue.Clients.Consumers;

namespace IFramework.MessageQueue.EQueue
{
    public class CommandBus : MessageConsumer<MessageReply>, ICommandBus
    {
        protected ICommandHandlerProvider HandlerProvider { get; set; }
        protected ILinearCommandManager LinearCommandManager { get; set; }
        protected Hashtable MessageStateQueue { get; set; }
        protected string CommandTopic { get; set; }
        protected string ReplyTopic { get; set; }
        protected Producer Producer { get; set; }
        IMessageStore _MessageStore;
        protected IMessageStore MessageStore
        {
            get
            {
                return _MessageStore ?? (_MessageStore = IoCFactory.Resolve<IMessageStore>());
            }
        }

        protected bool InProc { get; set; }

        public CommandBus(string name, ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          string brokerAddress,
                          int producerBrokerPort,
                          ConsumerSettings consumerSettings,
                          string groupName,
                          string replyTopic,
                          string commandTopic,
                          bool inProc)
            : base(name, consumerSettings, groupName, MessageModel.Clustering, replyTopic)
        {
            MessageStateQueue = Hashtable.Synchronized(new Hashtable());
            HandlerProvider = handlerProvider;
            LinearCommandManager = linearCommandManager;
            CommandTopic = commandTopic;
            ReplyTopic = replyTopic;
            InProc = inProc;
            Producer = new Producer(brokerAddress, producerBrokerPort);
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

        protected override void ConsumeMessage(MessageReply reply, QueueMessage queueMessage)
        {
            _Logger.DebugFormat("Handle reply:{0} content:{1}", reply.MessageID, reply.ToJson());
            var messageState = MessageStateQueue[reply.MessageID] as MessageState;
            if (messageState != null)
            {
                MessageStateQueue.TryRemove(reply.MessageID);
                if (reply.Exception != null)
                {
                    messageState.TaskCompletionSource.TrySetException(reply.Exception);
                }
                else
                {
                    if (reply.Result != null)
                    {
                        var command = messageState.MessageContext.Message;
                        command.SetValueByKey("Result", reply.Result);
                    }
                    messageState.TaskCompletionSource.TrySetResult(null);
                }
            }
        }



        protected MessageState BuildMessageState(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var pendingRequestsCts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource
                   .CreateLinkedTokenSource(cancellationToken, pendingRequestsCts.Token);
            cancellationToken = linkedCts.Token;
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            var state = new MessageState
            {
                MessageID = messageContext.MessageID,
                TaskCompletionSource = source,
                CancellationToken = cancellationToken,
                MessageContext = messageContext
            };
            return state;
        }

        public Task Send(ICommand command)
        {
            return Send(command, CancellationToken.None);
        }

        public Task Send(ICommand command, CancellationToken cancellationToken)
        {
            var currentMessageContext = PerMessageContextLifetimeManager.CurrentMessageContext;
            if (currentMessageContext != null && currentMessageContext.Message is ICommand)
            {
                // A command sent in a CommandContext is not allowed. We throw exception!!!
                throw new NotSupportedException("Command is not allowd to be sent in another command context!");
            }

            string commandKey = null;
            if (command is ILinearCommand)
            {
                commandKey = LinearCommandManager.GetLinearKey(command as ILinearCommand).ToString();
            }
            IMessageContext commandContext = new MessageContext(command, ReplyTopic, commandKey);

            Task task = null;
            if (InProc && currentMessageContext == null)
            {
                task = SendInProc(commandContext, cancellationToken);
            }
            else
            {
                task = SendAsync(commandContext, cancellationToken);
                if (currentMessageContext != null && Configuration.GetAppConfig<bool>("PersistanceMessage"))
                {
                    //TODO: persistance command with domain event ID
                    MessageStore.Save(commandContext, currentMessageContext.MessageID);
                }
            }
            return task;
        }

        protected virtual Task SendInProc(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            Task task = null;
            var command = commandContext.Message as ICommand;
            if (command is ILinearCommand)
            {
                task = SendAsync(commandContext, cancellationToken);
            }
            else //if not a linear command, we run synchronously.
            {
                task = new Task(() =>
                {
                    var commandHandler = HandlerProvider.GetHandler(command.GetType());
                    if (commandHandler == null)
                    {
                        throw new NoHandlerExists();
                    }

                    //MessageState messageState = BuildMessageState(commandContext, cancellationToken);
                    PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
                    try
                    {
                        var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                        commandHandler.Handle(command);
                        unitOfWork.Commit();
                    }
                    finally
                    {
                        commandContext.ClearItems();
                    }
                });
                task.RunSynchronously();
                if (task.Exception != null)
                {
                    throw task.Exception.GetBaseException();
                }
            }
            return task;
        }

        protected virtual Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var command = commandContext.Message as ICommand;
            MessageState commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(onCancel, commandState);
            MessageStateQueue.Add(commandState.MessageID, commandState);
            var commandKey = commandState.MessageContext.Key;

            if (string.IsNullOrWhiteSpace(commandKey))
            {
                commandKey = commandState.MessageID;
            }

            var messageBody = Encoding.UTF8.GetBytes(commandContext.ToJson());
            Producer.SendAsync(new global::EQueue.Protocols.Message(CommandTopic, messageBody), commandKey)
                .ContinueWith(task => {
                    if (task.Result.SendStatus == SendStatus.Success)
                    {
                        _Logger.DebugFormat("sent commandID {0}, body: {1}", commandContext.MessageID,
                                                                        commandContext.Message.ToJson());
                    }
                    else
                    {
                        _Logger.ErrorFormat("Send Command {0}", task.Result.SendStatus.ToString());
                    }
            });

           
            return commandState.TaskCompletionSource.Task;
        }

        protected void onCancel(object state)
        {
            var messageState = state as MessageState;
            MessageStateQueue.TryRemove(messageState.MessageID);
        }
    }
}
