using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using System.Collections;
using IFramework.Event;
using System.Collections.Concurrent;

namespace IFramework.Event.Impl
{
    public class DomainEventBus : IDomainEventBus
    {
        protected ConcurrentQueue<IDomainEvent> DomainEventQueue;
        protected IEventSubscriberProvider EventSubscriberProvider { get; set; }
        protected IEventPublisher EventPublisher { get; set; }
        protected IEnumerable<IMessageContext> SentMessageContexts { get; set; }
        public DomainEventBus(IEventSubscriberProvider provider, IEventPublisher eventPublisher)
        {
            EventPublisher = eventPublisher;
            EventSubscriberProvider = provider;
            DomainEventQueue = new ConcurrentQueue<IDomainEvent>();
        }

        public virtual void Commit()
        {
            SentMessageContexts = EventPublisher.Publish(DomainEventQueue.ToArray());
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            var eventSubscribers = EventSubscriberProvider.GetHandlers(@event.GetType());
            eventSubscribers.ForEach(eventSubscriber => {
                eventSubscriber.Handle(@event);
            });
            DomainEventQueue.Enqueue(@event);
        }

        public void Publish<TEvent>(IEnumerable<TEvent> eventContexts) where TEvent : IDomainEvent
        {
            eventContexts.ForEach(@event => Publish(@event));
        }

        public virtual void Dispose()
        {
            
        }

        public IEnumerable<IMessageContext> GetMessageContexts()
        {
            return SentMessageContexts;
        }
    }
}
