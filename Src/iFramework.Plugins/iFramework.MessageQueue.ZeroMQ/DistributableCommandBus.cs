using IFramework.Command;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.MessageFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class DistributableCommandBus : CommandBus, ICommandBus
    {
        IInProcMessageConsumer _CommandConsumer;
        IInProcMessageConsumer _CommandDistributor;

        bool _IsDistributor;

        public DistributableCommandBus(ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          IMessageConsumer commandConsumer,
                          string receiveEndPoint,
                          bool inProc)
            : base(handlerProvider, linearCommandManager, receiveEndPoint, inProc)
        {
            _CommandConsumer = commandConsumer as IInProcMessageConsumer;
            _CommandDistributor = _CommandConsumer as IInProcMessageConsumer;
            _IsDistributor = _CommandDistributor is IMessageDistributor;
        }


        protected override void ConsumeMessage(IMessageReply reply)
        {
            base.ConsumeMessage(reply);
            if (_IsDistributor)
            {
                _CommandDistributor.EnqueueMessage(new MessageHandledNotification(reply.MessageID).GetFrame());
            }
        }

        protected override Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var command = commandContext.Message as ICommand;
            MessageState commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(onCancel, commandState);
            MessageStateQueue.Add(commandState.MessageID, commandState);
            Task.Factory.StartNew(() => {
                _CommandConsumer.EnqueueMessage(commandContext.GetFrame());
                _Logger.DebugFormat("send to distributor/consumer commandID:{0} payload:{1}",
                                         commandContext.MessageID, commandContext.ToJson());
            });
            return commandState.TaskCompletionSource.Task;
        }

        public new void Start()
        {
            (this as MessageConsumer<IMessageReply>).Start();
        }
    }
}
