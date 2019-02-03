﻿using System.Collections.Generic;
using System.Linq;
using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.DependencyInjection;

namespace IFramework.Event.Impl
{
    public class EventBus : IEventBus
    {
        protected readonly IObjectProvider Container;
        protected readonly IEventSubscriberProvider EventSubscriberProvider;

        protected List<ICommand> CommandQueue;
        protected List<IEvent> EventQueue;
        protected List<object> SagaResultQueue;
        protected List<IEvent> ToPublishAnywayEventQueue;

        //protected IEventSubscriberProvider EventSubscriberProvider { get; set; }
        public EventBus(IObjectProvider objectProvider, SyncEventSubscriberProvider eventSubscriberProvider)
        {
            Container = objectProvider;
            EventSubscriberProvider = eventSubscriberProvider;
            EventQueue = new List<IEvent>();
            CommandQueue = new List<ICommand>();
            SagaResultQueue = new List<object>();
            ToPublishAnywayEventQueue = new List<IEvent>();
        }


        public void Publish<TTMessage>(TTMessage @event) where TTMessage : IEvent
        {
            EventQueue.Add(@event);
            //HandleEvent(@event);
        }

        //private void HandleEvent<TEvent>(TEvent @event) where TEvent : IEvent
        //{
        //    if (EventSubscriberProvider != null)
        //    {
        //        var eventSubscriberTypes = EventSubscriberProvider.GetHandlerTypes(@event.GetType());
        //        eventSubscriberTypes.ForEach(eventSubscriberType =>
        //        {
        //            var eventSubscriber = Container.GetService(eventSubscriberType.Type);
        //            ((dynamic)eventSubscriber).Handle((dynamic)@event);
        //        });
        //    }
        //}

        public void Publish<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent
        {
            events.ForEach(Publish);
        }

        public virtual void Dispose() { }

        public IEnumerable<IEvent> GetEvents()
        {
            return EventQueue;
        }

        public IEnumerable<object> GetSagaResults()
        {
            return SagaResultQueue;
        }

        public void ClearMessages(bool clearPublishAnywayMessages = true)
        {
            SagaResultQueue.Clear();
            EventQueue.Clear();
            CommandQueue.Clear();
            if (clearPublishAnywayMessages)
            {
                ToPublishAnywayEventQueue.Clear();
            }
        }

        public void PublishAnyway(params IEvent[] events)
        {
            ToPublishAnywayEventQueue.AddRange(events);
            //events.ForEach(HandleEvent);
        }

        public IEnumerable<IEvent> GetToPublishAnywayMessages()
        {
            return ToPublishAnywayEventQueue;
        }

        public void SendCommand(ICommand command)
        {
            CommandQueue.Add(command);
        }

        public IEnumerable<ICommand> GetCommands()
        {
            return CommandQueue;
        }

        public void FinishSaga(object sagaResult)
        {
            SagaResultQueue.Add(sagaResult);
        }
    }
}