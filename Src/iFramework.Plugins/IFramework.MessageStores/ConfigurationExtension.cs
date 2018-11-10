﻿using IFramework.Config;
using IFramework.Message;
using IFramework.MessageStores.Abstracts;
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

      
    }
}