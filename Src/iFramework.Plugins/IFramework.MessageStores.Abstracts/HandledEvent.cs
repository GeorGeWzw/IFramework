using System;
using System.ComponentModel.DataAnnotations;
using IFramework.MessageQueue;

namespace IFramework.MessageStores.Abstracts
{
    public class HandledEvent
    {
        public HandledEvent()
        {
        }

        public HandledEvent(string id, string subscriptionName, MessageOffset messageOffset, DateTime handledTime)
        {
            Id = id;
            SubscriptionName = subscriptionName;
            MessageOffset = messageOffset;
            HandledTime = handledTime;
        }

        //[MaxLength(50)]
        public string Id { get; set; }
        public string SubscriptionName { get; set; }
        public DateTime HandledTime { get; set; }
        public virtual MessageOffset MessageOffset { get; set; }
    }

    public class FailHandledEvent : HandledEvent
    {
        public FailHandledEvent() { }

        public FailHandledEvent(string id, string subscriptionName, MessageOffset messageOffset, DateTime handledTime, Exception e)
            : base(id, subscriptionName, messageOffset, handledTime)
        {
            Error = e.GetBaseException().Message;
            StackTrace = e.StackTrace;
        }

        public string Error { get; set; }
        public string StackTrace { get; set; }
    }
}