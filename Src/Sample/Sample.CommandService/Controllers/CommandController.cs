using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using IFramework.Infrastructure;
using System.Threading.Tasks;
using System.Threading;
using IFramework.Message;
using Sample.Command;

namespace Sample.CommandService.Controllers
{
    public class CommandController : ApiController
    {
        ICommandBus CommandBus { get; set; }
        public CommandController(ICommandBus commandBus)
        {
            CommandBus = commandBus;
        }

        public ApiResult<TResult> ActionWithResult<TResult>(ICommand<TResult> command)
        {
            return ExceptionManager.Process<TResult>(() =>
            {
                CommandBus.Send(command).Wait();
                return command.Result;
            });
        }

        public ApiResult Action(ICommand command)
        {
            var commandGenericInterfaceType = command.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType);
            if (commandGenericInterfaceType != null)
            {
                var resultType = commandGenericInterfaceType.GetGenericArguments().First();
                var result = this.InvokeGenericMethod(resultType, "ActionWithResult", new object[] { command })
                            as ApiResult;
                return result;
            }
            else
            {
                return ExceptionManager.Process(() =>
                {
                    CommandBus.Send(command).Wait();
                });
            }
        }

        static List<ICommand> BatchCommands = new List<ICommand>();
        public ApiResult TestDistributor(ICommand command)
        {
            BatchCommands.Add(command);
            if (BatchCommands.Count >= 3)
            {
                var batchCount = 1;
                int.TryParse(CookiesHelper.GetCookie("batchCount").Value, out batchCount);
                int i = 0;
                try
                {
                    var commands = new List<ICommand>(BatchCommands);
                    Task.Factory.StartNew(() => {
                        while (i++ < batchCount)
                        {
                            DoCommand(commands);
                        }
                    });
                }
                catch (Exception e)
                {
                    return new ApiResult { ErrorCode = ErrorCode.UnknownError, Message = e.GetBaseException().Message };
                }
                BatchCommands.Clear();
            }
            return new ApiResult();
        }

        [HttpGet]
        public Task<string> CommandDistributorStatus()
        {
            return Task.Factory.StartNew(() =>
            {
                var commandConsumersStatus = string.Empty;
                WebApiApplication.CommandConsumers.ForEach(commandConsuemr => {
                    commandConsumersStatus += commandConsuemr.GetStatus() + "<br>";
                });

                var domainEventConsumer = IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer");
                commandConsumersStatus += domainEventConsumer.GetStatus() + "<br>";
                return commandConsumersStatus;
            });

        }

        public void DoCommand(List<ICommand> batchCommands)
        {
            batchCommands.ForEach(cmd =>
            {
                Task.Factory.StartNew(() =>
                {
                    Action(cmd);
                });
            });
        }
    }
}