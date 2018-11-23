﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.Client.Abstracts;
using IFramework.MessageQueue.RabbitMQ.MessageFormat;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace IFramework.MessageQueue.RabbitMQ
{
    public class RabbitMQProducer: IMessageProducer
    {
        private readonly IModel _channel;
        private readonly string _exchange;
        private readonly string _topic;
        private readonly IBasicProperties _properties;
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger<RabbitMQProducer>();

        public RabbitMQProducer(IModel channel, string exchange, string topic, ProducerConfig config = null)
        {
            _channel = channel;
            _exchange = exchange;
            _topic = topic;
            _properties = _channel.CreateBasicProperties();
            _properties.Persistent = true;
        }

        public void Stop()
        {
            _channel.Dispose();
        }

        public Task SendAsync(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var message = ((MessageContext)messageContext).RabbitMQMessage;
            try
            {
                _channel.BasicPublish(_exchange, _topic, true, _properties, Encoding.UTF8.GetBytes(message.ToJson()));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "send message failed");
                throw;
            }
            return Task.CompletedTask;
        }
    }
}
