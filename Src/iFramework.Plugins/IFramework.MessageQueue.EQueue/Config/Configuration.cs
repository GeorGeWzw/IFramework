using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using global::EQueue.Configurations;
using ECommon.Autofac;
using ECommon.Log4Net;
using ECommon.JsonNet;
using IFramework.Config;
using EQueue.Broker;

namespace IFramework.Config
{
    public static class ConfigurationEQueue
    {
        public static Configuration InitliaizeEQueue(this Configuration configuration, int brokePort = 1500)
        {
            ECommon.Configurations.Configuration
                .Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterEQueueComponents();

            var setting = new BrokerSetting();
            setting.NotifyWhenMessageArrived = false;
            setting.RemoveConsumedMessageInterval = 1000;
            new BrokerController(setting).Start();

            return configuration;
        }
    }
}
