﻿using IFramework.Config;
using IFramework.Message;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.MessageStores.Relational
{
    public static class ConfigurationExtension
    {
        public static Configuration UseRelationalMessageStore<TMessageStore>(this Configuration configuration,
                                                                                                  ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
        {
            return configuration.UseRelationalMessageStore<TMessageStore, MessageStoreDaemon>(lifetime);
        }

        public static Configuration UseRelationalMessageStore<TMessageStore, TMessageStoreDaemon>(this Configuration configuration,
                                                                             ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
            where TMessageStoreDaemon: class, IMessageStoreDaemon
        {
            return configuration.UseMessageStore<TMessageStore>(lifetime)
                                .UseMessageStoreDaemon<TMessageStoreDaemon>();
        }
    }
}