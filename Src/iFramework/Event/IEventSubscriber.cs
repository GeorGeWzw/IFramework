﻿using IFramework.Message;

namespace IFramework.Event
{
    public interface IEventSubscriber<in TEvent> :
        IMessageHandler<TEvent>  { }
}