using EQueue.Broker;
using EQueue.Clients.Consumers;
using IFramework.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EQueue.Autofac;
using EQueue.Log4Net;
using EQueue.JsonNet;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.EQueue;
using IFramework.Event;
using IFramework.Command;
using Microsoft.Practices.Unity;
using System.Threading.Tasks;
using Sample.Command;
using System.Threading;

namespace EQueueTest
{
    class Program
    {
        static ICommandBus commandBus;
        static void Main(string[] args)
        {
            try
            {
                Configuration.Instance.UseLog4Net();

                Configuration.Instance
                             .CommandHandlerProviderBuild(null, "CommandHandlers");

                global::EQueue.Configuration
                .Create()
                .UseAutofac()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterFrameworkComponents();

                new BrokerController().Initialize().Start();
                var consumerSettings = ConsumerSettings.Default;
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

                IEventPublisher eventPublisher = new EventPublisher("domainevent",
                                                                  consumerSettings.BrokerAddress,
                                                                  producerPort);
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

                commandConsumer1.Start();
                commandConsumer2.Start();
                commandConsumer3.Start();

                commandBus = new CommandBus("CommandBus",
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

                Thread.Sleep(10000);

                var worker = new Worker(commandBus);
                worker.StartTest();


                while (true)
                {
                    Console.ReadLine();
                    Console.WriteLine(commandConsumer1.GetStatus());
                    Console.WriteLine(commandConsumer2.GetStatus());
                    Console.WriteLine(commandConsumer3.GetStatus());
                    Console.WriteLine(domainEventSubscriber.GetStatus());
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message, ex);
            }
        }
    }

}
