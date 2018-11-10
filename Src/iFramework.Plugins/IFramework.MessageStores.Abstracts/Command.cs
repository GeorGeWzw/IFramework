﻿
using System;
using IFramework.Infrastructure;
using IFramework.Message;

namespace IFramework.MessageStores.Abstracts
{
    public class Command : Message
    {
        public Command() { }

        public Command(IMessageContext messageContext, object result = null) :
            base(messageContext)
        {
            if (result != null)
            {
                Result = result.ToJson();
                ResultType = result.GetType().GetFullNameWithAssembly();
            }
        }

        public MessageStatus Status { get; set; }
        public string Result { get; set; }
        public string ResultType { get; set; }

        public object Reply
        {
            get
            {
                object reply = null;
                try
                {
                    if (!string.IsNullOrEmpty(Result) && !string.IsNullOrEmpty(ResultType))
                    {
                        reply = Result.ToJsonObject(System.Type.GetType(ResultType));
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
                return reply;
            }
        }

        //public Event Parent
        //{
        //    get
        //    {
        //        return ParentMessage as Event;
        //    }
        //}

        //public IEnumerable<Event> Children
        //{
        //    get
        //    {
        //        return ChildrenMessage.Cast<Event>();
        //    }
        //}
    }
}