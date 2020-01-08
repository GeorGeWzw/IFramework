﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Event;
using IFramework.EventStore.Client;
using IFramework.EventStore.Redis;
using IFramework.JsonNet;
using IFramework.Log4Net;
using IFramework.Message;
using IFramework.Test.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IFramework.Test
{
    public class EventStoreTests
    {
        public EventStoreTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            var services = new ServiceCollection();
            services.AddAutofacContainer()
                    .AddConfiguration(configuration)
                    .AddCommonComponents()
                    //.UseMicrosoftDependencyInjection()
                    //.UseUnityContainer()
                    //.UseAutofacContainer()
                    //.UseConfiguration(configuration)
                    //.UseCommonComponents()
                    .AddJsonNet()
                    .AddLog4Net()
                    .AddRedisEventStore()
                    //.AddEventStoreClient()
                ;

            ObjectProviderFactory.Instance.Build(services);
        }

        [Fact]
        public async Task EventStreamAppendReadTest()
        {
            const string userId = "2";
            var name = $"ivan_{DateTime.Now.Ticks}";
            var correlationId = $"cmd{DateTime.Now.Ticks}";
           
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var messageTypeProvider = serviceScope.GetService<IMessageTypeProvider>();
                messageTypeProvider.Register(nameof(UserCreated), typeof(UserCreated))
                                   .Register(nameof(UserModified), typeof(UserModified))
                                   .Register(nameof(CreateUser), typeof(CreateUser));

                var eventStore = serviceScope.GetService<IEventStore>();
                await eventStore.Connect()
                                .ConfigureAwait(false);
                var events = (await eventStore.GetEvents(userId)
                                              .ConfigureAwait(false))
                             .Cast<IAggregateRootEvent>()
                             .ToArray();
                var expectedVersion = events.LastOrDefault()?.Version ?? -1;
                if (expectedVersion == -1)
                {
                    var command = new CreateUser {Id = correlationId, UserName = name, UserId = userId};
                    await eventStore.AppendEvents(userId, 
                                                  expectedVersion,
                                                  command.Id,
                                                  new UserCreated(userId, name, expectedVersion + 1))
                                    .ConfigureAwait(false);
                }
                else
                {
                    var command = new ModifyUser {Id = correlationId, UserName = name, UserId = userId};
                    await eventStore.AppendEvents(userId,
                                                  expectedVersion,
                                                  command.Id,
                                                  new UserModified(userId, name, expectedVersion + 1))
                                    .ConfigureAwait(false);
                }
            }
        }
    }
}