using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Event.Impl
{
    class MockEventPublisher : IEventPublisher
    {
        public void Publish(params IMessageContext[] messageContexts)
        {
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
        }


        public void Publish(params IEvent[] events)
        {
            
        }
    }
}
