using EQueue.Broker;
using EQueue.Clients.Consumers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.EQueue;
using IFramework.Event;
using IFramework.Command;
using Microsoft.Practices.Unity;
using System.Threading.Tasks;
using Sample.Command;
using System.Threading;
using IFramework.Config;
using IFramework.Log4Net;
using IFramework.Infrastructure.Logging;
using EQueue.Clients.Producers;
using ECommon.Scheduling;
using ECommon.Components;

namespace EQueueTest
{
    public class Program
    {
        static ICommandBus commandBus;
        static ILogger _logger;
         
        static void Main(string[] args)
        {
            try
            {

                Configuration.Instance.UseLog4Net()
                                      .InitliaizeEQueue()
                                      .CommandHandlerProviderBuild(null, "CommandHandlers");

                var consumerSetting = new ConsumerSetting();
                consumerSetting.MessageHandleMode = MessageHandleMode.Sequential;
                consumerSetting.BrokerPort = 5001;

                var producerSetting = new ProducerSetting();
                var producerPort = producerSetting.BrokerPort = 5000;

                var eventHandlerProvider = IoCFactory.Resolve<IHandlerProvider>("AsyncDomainEventSubscriber");
                IMessageConsumer domainEventSubscriber = new DomainEventSubscriber("domainEventSubscriber1",
                                                                                   consumerSetting,
                                                                                   "DomainEventSubscriber",
                                                                                   "domainevent",
                                                                                   eventHandlerProvider);
                domainEventSubscriber.Start();
                IoCFactory.Instance.CurrentContainer.RegisterInstance("DomainEventConsumer", domainEventSubscriber);

                IEventPublisher eventPublisher = new EventPublisher("EventPublisher", "domainevent",
                                                                  producerSetting);
                IoCFactory.Instance.CurrentContainer.RegisterInstance(typeof(IEventPublisher),
                                                                      eventPublisher,
                                                                      new ContainerControlledLifetimeManager());


                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer1 = new CommandConsumer("consumer1", consumerSetting,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSetting.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer2 = new CommandConsumer("consumer2", consumerSetting,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSetting.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer3 = new CommandConsumer("consumer3", consumerSetting,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSetting.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer4 = new CommandConsumer("consumer4", consumerSetting,
                                                          "CommandConsumerGroup",
                                                          "Command",
                                                          consumerSetting.BrokerAddress,
                                                          producerPort,
                                                          commandHandlerProvider);

                CommandConsumer.CommandConsumers.Add(commandConsumer1);
                CommandConsumer.CommandConsumers.Add(commandConsumer2);
                CommandConsumer.CommandConsumers.Add(commandConsumer3);
                CommandConsumer.CommandConsumers.Add(commandConsumer4);

                commandConsumer1.Start();
                commandConsumer2.Start();
                commandConsumer3.Start();
                commandConsumer4.Start();

                commandBus = new CommandBus("CommandBus",
                                                        commandHandlerProvider,
                                                        IoCFactory.Resolve<ILinearCommandManager>(),
                                                        consumerSetting.BrokerAddress,
                                                        producerPort,
                                                        consumerSetting,
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
                var taskId = scheduleService.ScheduleTask("consumer logs", () =>
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

                var worker = new Worker(commandBus);
                worker.StartTest(100);


                while (true)
                {
                    Console.WriteLine(CommandConsumer.GetConsumersStatus());
                    Console.WriteLine(domainEventSubscriber.GetStatus());
                    Console.WriteLine("please input batch count and rerun another test:");
                    var input = Console.ReadLine();
                    var batchCount = 0;
                    if (int.TryParse(input, out batchCount))
                    {
                        worker.StartTest(batchCount);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message, ex);
            }
            finally
            {
                Console.Read();
            }
        }
    }

}
