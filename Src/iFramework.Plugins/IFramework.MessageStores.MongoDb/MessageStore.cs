﻿using IFramework.Domain;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageStores.Abstracts;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IFramework.MessageStores.MongoDb
{
    public abstract class MessageStore : Abstracts.MessageStore
    {
        public DbSet<HandledEventBase> HandledEvents { get; set; }

        protected MessageStore(DbContextOptions options) : base(options) { }

         public override Task HandleEventAsync(IMessageContext eventContext,
                                              string subscriptionName,
                                              IEnumerable<IMessageContext> commandContexts,
                                              IEnumerable<IMessageContext> messageContexts)
        {
            HandledEvents.Add(new HandledEvent(eventContext.MessageId, subscriptionName, DateTime.Now));
            commandContexts.ForEach(commandContext =>
            {
                commandContext.CorrelationId = eventContext.MessageId;
                // don't save command here like event that would be published to other bounded context
                UnSentCommands.Add(new UnSentCommand(commandContext));
            });
            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                Events.Add(BuildEvent(messageContext));
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            return SaveChangesAsync();
        }

        public override async Task<bool> HasEventHandledAsync(string eventId, string subscriptionName)
        {
            var handledEventId = $"{eventId}_{subscriptionName}";
            return await HandledEvents.CountAsync(@event => @event.Id == handledEventId)
                                      .ConfigureAwait(false) > 0;
        }

        public override Task SaveFailHandledEventAsync(IMessageContext eventContext,
                                                       string subscriptionName,
                                                       Exception e,
                                                       params IMessageContext[] messageContexts)
        {
            HandledEvents.Add(new FailHandledEvent(eventContext.MessageId, subscriptionName, DateTime.Now, e));

            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                Events.Add(BuildEvent(messageContext));
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            return SaveChangesAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<Abstracts.HandledEvent>();
            modelBuilder.Ignore<SagaInfo>();
            modelBuilder.Ignore<AggregateRoot>();
            modelBuilder.Ignore<Entity>();
            modelBuilder.Ignore<TimestampedAggregateRoot>();
            modelBuilder.Ignore<VersionedAggregateRoot>();
            modelBuilder.Ignore<ValueObject<SagaInfo>>();
            modelBuilder.Ignore<UnSentMessage>();
            modelBuilder.Ignore<Abstracts.Message>();

            modelBuilder.Entity<HandledEventBase>()
                        .HasKey(e => e.Id);
            modelBuilder.Entity<UnPublishedEvent>()
                        .HasKey(e => e.Id);
            modelBuilder.Entity<UnSentCommand>()
                        .HasKey(c => c.Id);
            modelBuilder.Entity<Abstracts.Command>()
                        .HasKey(c => c.Id);
            modelBuilder.Entity<Abstracts.Event>()
                        .HasKey(e => e.Id);
        }
    }
}