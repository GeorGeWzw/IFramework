using EQueue.Clients.Consumers;
using IFramework.Command;
using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue.EQueue;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ECommon.Autofac;
using ECommon.Log4Net;
using ECommon.JsonNet;
using EQueue.Broker;
using ECommon.Configurations;
using EQueue.Clients.Producers;
using EQueue.Configurations;
using ECommon.Scheduling;
using System.Threading;

namespace Sample.CommandService
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        ILogger _Logger;
        ILogger Logger
        {
            get
            {
                return _Logger ?? (_Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType()));
            }
        }

        // ZeroMQ Application_Start
        /*protected void Application_Start()
        {
            try
            {
                Configuration.Instance.UseLog4Net();

                var commandDistributor = new CommandDistributor("inproc://distributor",
                                                                new string[] { 
                                                                    "inproc://CommandConsumer1"
                                                                    , "inproc://CommandConsumer2"
                                                                    , "inproc://CommandConsumer3"
                                                                }
                                                               );

                Configuration.Instance.RegisterCommandConsumer(commandDistributor, "CommandDistributor")
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc();

                IoCFactory.Resolve<IEventPublisher>();
                IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer").Start();

                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer1 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://CommandConsumer1");
                var commandConsumer2 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://CommandConsumer2");
                var commandConsumer3 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://CommandConsumer3");


                commandConsumer1.Start();
                commandConsumer2.Start();
                commandConsumer3.Start();
                commandDistributor.Start();

                ICommandBus commandBus = IoCFactory.Resolve<ICommandBus>();
                commandBus.Start();

                AreaRegistration.RegisterAllAreas();
                WebApiConfig.Register(GlobalConfiguration.Configuration);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetBaseException().Message, ex);
            }
        }
        */

        // EQueue Application_Start

        public static List<CommandConsumer> CommandConsumers = new List<CommandConsumer>();
        
        protected void Application_Start()
        {
            try
            {
                IFramework.Config.Configuration.Instance.UseLog4Net()
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc();

                global::ECommon.Configurations.Configuration
                .Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterEQueueComponents();

                var brokerSetting = BrokerSetting.Default;
                brokerSetting.NotifyWhenMessageArrived = true;
                new BrokerController(brokerSetting).Initialize().Start();
                var consumerSettings = ConsumerSetting.Default;
                consumerSettings.MessageHandleMode = MessageHandleMode.Sequential;
                var producerPort = 5000;

              
                var eventHandlerProvider = IoCFactory.Resolve<IHandlerProvider>("AsyncDomainEventSubscriber");
                IMessageConsumer domainEventSubscriber = new DomainEventSubscriber("domainEventSubscriber1",
                                                                                   consumerSettings,
                                                                                   "DomainEventSubscriber",
                                                                                   "domainevent",
                                                                                   eventHandlerProvider);
                domainEventSubscriber.Start();
                IoCFactory.Instance.CurrentContainer.RegisterInstance("DomainEventConsumer", domainEventSubscriber);


                var producerSetting = ProducerSetting.Default;
                producerSetting.BrokerPort = 5000;

                IEventPublisher eventPublisher = new EventPublisher("domainevent", producerSetting);
                IoCFactory.Instance.CurrentContainer.RegisterInstance(typeof(IEventPublisher),
                                                                      eventPublisher,
                                                                      new ContainerControlledLifetimeManager());


                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer1 = new CommandConsumer("consumer1", consumerSettings, 
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer2 = new CommandConsumer("consumer2", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer3 = new CommandConsumer("consumer3", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer4 = new CommandConsumer("consumer4", consumerSettings,
                                                      "CommandConsumerGroup",
                                                      "Command",
                                                      consumerSettings.BrokerAddress,
                                                      producerPort,
                                                      commandHandlerProvider);
                commandConsumer1.Start();
                commandConsumer2.Start();
                commandConsumer3.Start();
                commandConsumer4.Start();

                CommandConsumers.Add(commandConsumer1);
                CommandConsumers.Add(commandConsumer2);
                CommandConsumers.Add(commandConsumer3);
                CommandConsumers.Add(commandConsumer4);

                ICommandBus commandBus = new CommandBus("CommandBus",
                                                        commandHandlerProvider,
                                                        IoCFactory.Resolve<ILinearCommandManager>(),
                                                        consumerSettings.BrokerAddress,
                                                        producerPort,
                                                        consumerSettings,
                                                        "CommandBus",
                                                        "Reply", 
                                                        "Command",
                                                        true);
                IoCFactory.Instance.CurrentContainer.RegisterInstance(typeof(ICommandBus),
                                                                      commandBus,
                                                                      new ContainerControlledLifetimeManager());
                commandBus.Start();


                //Below to wait for consumer balance.
                var scheduleService = ObjectContainer.Resolve<IScheduleService>();
                var waitHandle = new ManualResetEvent(false);
                var taskId = scheduleService.ScheduleTask(() =>
                {
                    var bAllocatedQueueIds = (commandBus as CommandBus).Consumer.GetCurrentQueues().Select(x => x.QueueId);
                    var c1AllocatedQueueIds = commandConsumer1.Consumer.GetCurrentQueues().Select(x => x.QueueId);
                    var c2AllocatedQueueIds = commandConsumer2.Consumer.GetCurrentQueues().Select(x => x.QueueId);
                    var c3AllocatedQueueIds = commandConsumer3.Consumer.GetCurrentQueues().Select(x => x.QueueId);
                    var c4AllocatedQueueIds = commandConsumer4.Consumer.GetCurrentQueues().Select(x => x.QueueId);
                    var eAllocatedQueueIds = (domainEventSubscriber as DomainEventSubscriber).Consumer.GetCurrentQueues().Select(x => x.QueueId);

                    Console.WriteLine(string.Format("Consumer message queue allocation result:bus:{0}, eventSubscriber:{1} c1:{2}, c2:{3}, c3:{4}, c4:{5}",
                          string.Join(",", bAllocatedQueueIds),
                          string.Join(",", eAllocatedQueueIds),
                          string.Join(",", c1AllocatedQueueIds),
                          string.Join(",", c2AllocatedQueueIds),
                          string.Join(",", c3AllocatedQueueIds),
                          string.Join(",", c4AllocatedQueueIds)));

                    if (eAllocatedQueueIds.Count() == 4
                        && bAllocatedQueueIds.Count() == 4
                        && c1AllocatedQueueIds.Count() == 1
                        && c2AllocatedQueueIds.Count() == 1
                        && c3AllocatedQueueIds.Count() == 1
                        && c4AllocatedQueueIds.Count() == 1)
                    {

                        waitHandle.Set();
                    }
                }, 1000, 1000);

                waitHandle.WaitOne();
                scheduleService.ShutdownTask(taskId);



                AreaRegistration.RegisterAllAreas();
                WebApiConfig.Register(GlobalConfiguration.Configuration);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {

            Exception ex = Server.GetLastError().GetBaseException(); //获取错误
            Logger.Debug(ex.Message, ex);
        }
    }
}