using System;
using System.ComponentModel.DataAnnotations;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageStores.Abstracts
{
    public abstract class Message
    {
        protected Message() { }

        protected Message(IMessageContext messageContext)
        {
            Id = messageContext.MessageId;
            Topic = messageContext.Topic;
            CorrelationId = messageContext.CorrelationId;
            MessageBody = messageContext.Message.ToJson();
            CreateTime = messageContext.SentTime;
            SagaInfo = messageContext.SagaInfo?.Clone() ?? SagaInfo.Null;
            IP = messageContext.Ip;
            Producer = messageContext.Producer;
            if (messageContext.Message != null)
            {
                Name = messageContext.Message.GetType().Name;
                Type = messageContext.Message.GetType().GetFullNameWithAssembly();
            }
        }

        //[MaxLength(50)]
        public string Id { get; set; }
        public string CorrelationId { get; set; }
        public string MessageBody { get; set; }
        public DateTime CreateTime { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Topic { get; set; }
        public virtual SagaInfo SagaInfo { get; set; }
        public string IP { get; set; }
        public string Producer { get; set; }
    }
}