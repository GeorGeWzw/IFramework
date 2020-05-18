using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.Message;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageStores.Relational
{
    public class MessageStoreDaemon : Abstracts.MessageStoreDaemon
    {
        protected override void RemoveUnSentCommands(Abstracts.MessageStore messageStore, string[] toRemoveCommands)
        {
            if (messageStore.InMemoryStore)
            {
                base.RemoveUnSentCommands(messageStore, toRemoveCommands);
                return;
            }
            var deleteCommandsSql = $"delete from msgs_UnSentCommands where Id in ({string.Join(",", toRemoveCommands.Select(rm => $"'{rm}'"))})";
            messageStore.Database.ExecuteSqlRaw(deleteCommandsSql);
        }

        protected override void RemoveUnPublishedEvents(Abstracts.MessageStore messageStore, string[] toRemoveEvents)
        {
            if (messageStore.InMemoryStore)
            {
                base.RemoveUnPublishedEvents(messageStore, toRemoveEvents);
                return;
            }
            var deleteEventsSql = $"delete from msgs_UnPublishedEvents where Id in ({string.Join(",", toRemoveEvents.Select(rm => $"'{rm}'"))})";
            messageStore.Database.ExecuteSqlRaw(deleteEventsSql);
        }

        public MessageStoreDaemon(ILogger<Abstracts.MessageStoreDaemon> logger) : base(logger) { }
    }
}